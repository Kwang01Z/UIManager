using UnityEngine;

public static class GameConstant
{
    public static string GameName = "DefendersOfTheDawn";

    public static string DeviceID => GetDeviceID();
    private static string GetDeviceID()
    {
        return SystemInfo.deviceUniqueIdentifier;
    }
}
