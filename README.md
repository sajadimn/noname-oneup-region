Region Unity Package
===

## Description
This package detects the player's region from the device time zone and IP address.

## Dependencies
- [noname-oneup-singleton](https://github.com/sajadimn/noname-oneup-singleton)

## How to use
```csharp
RegionManager.Instance.Init(() => Debug.Log(RegionManager.Instance.GetUserRegion()));
```
