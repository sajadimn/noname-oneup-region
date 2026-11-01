using System.Globalization;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RegionManager : MonoSingleton<RegionManager>
{
    [SerializeField] private UserRegion userRegion = UserRegion.Unknown;
    [SerializeField] private string detectedCountryCode = "";
    [SerializeField] private string detectedTimeZone = "";
    [SerializeField] private string detectedIP = "";
    private bool isDetecting;
    private Action OnDone;

    public void Init(Action onDone)
    {
        OnDone = onDone;
        RegionDetection();
    }

    private void RegionDetection()
    {
        if (isDetecting)
        {
            OnDone?.Invoke();
            return;
        }

        StartCoroutine(DetectRegion());
    }

    private IEnumerator DetectRegion()
    {
        isDetecting = true;
        detectedTimeZone = GetDeviceTimeZone();
        bool isIranTimeZone = IsIranTimeZone(detectedTimeZone);
        Debug.Log($"RegionManager - Time Zone: {detectedTimeZone} - " + $"Iran Time Zone: {isIranTimeZone}");

        bool ipRequestFinished = false;
        bool ipRequestSuccessful = false;
        yield return StartCoroutine(GetCountryIP(success =>
            {
                ipRequestSuccessful = success;
                ipRequestFinished = true;
            }
        ));

        while (!ipRequestFinished)
            yield return null;


        bool isIranIP = ipRequestSuccessful &&
                        string.Equals(detectedCountryCode, "IR", StringComparison.OrdinalIgnoreCase);

        if (isIranIP || isIranTimeZone)
        {
            userRegion = UserRegion.Iran;
        }
        else
        {
            userRegion = UserRegion.Global;
        }

        Debug.Log($"RegionManager - FINAL RESULT: {userRegion}\n" + $"IP: {detectedIP}\n" +
                  $"Country: {detectedCountryCode}\n" + $"TimeZone: {detectedTimeZone}");

        OnDone?.Invoke();
    }

    private IEnumerator GetCountryIP(Action<bool> callback)
    {
        const string url = "https://ipwho.is/";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 3;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"RegionManager - IP request failed: {request.error}");
            callback?.Invoke(false);
            yield break;
        }

        string json = request.downloadHandler.text;
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("RegionManager - IP API returned empty response.");
            callback?.Invoke(false);
            yield break;
        }

        try
        {
            IPWhoIsResponse response = JsonUtility.FromJson<IPWhoIsResponse>(json);
            if (response == null)
            {
                callback?.Invoke(false);
                yield break;
            }

            if (!response.success)
            {
                Debug.LogWarning("RegionManager - IP API reported failure.");
                callback?.Invoke(false);
                yield break;
            }

            detectedIP = response.ip;
            detectedCountryCode = response.country_code;
            Debug.Log($"RegionManager - IP: {detectedIP} - " + $"Country: {response.country} - " +
                      $"Country Code: {detectedCountryCode}");
            callback?.Invoke(true);
        }
        catch (Exception exception)
        {
            Debug.LogError($"RegionManager - Failed to parse IP response:\n{exception}");
            callback?.Invoke(false);
        }
    }

    private string GetDeviceTimeZone()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaClass timeZoneClass = new AndroidJavaClass("java.util.TimeZone");
            using AndroidJavaObject defaultTimeZone = timeZoneClass.CallStatic<AndroidJavaObject>("getDefault");
            string id = defaultTimeZone.Call<string>("getID");
            return id;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[RegionManager] Failed to get Android Time Zone: {exception}");
        }

#endif

        // Editor / fallback
        return TimeZoneInfo.Local.Id;
    }

    private bool IsIranTimeZone(string timeZone)
    {
        if (string.IsNullOrEmpty(timeZone))
            return false;

        return string.Equals(timeZone, "Asia/Tehran", StringComparison.OrdinalIgnoreCase);
    }

    public UserRegion GetUserRegion()
    {
        return userRegion;
    }
}

public enum UserRegion
{
    Unknown,
    Iran,
    Global,
}

[Serializable]
public class IPWhoIsResponse
{
    public bool success;
    public string ip;
    public string country;
    public string country_code;
}