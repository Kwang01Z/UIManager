using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class JailbreakDetector
{
#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int IsDeviceJailbrokenDetailed();
#else
    private static int IsDeviceJailbrokenDetailed() { return 0; }
#endif

    public static bool IsJailBroken()
    {
        try
        {
            int flags = IsDeviceJailbrokenDetailed();
            Debug.Log($"Jailbreak flags: {flags}");
            return flags != 0;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return false;
    }
}
