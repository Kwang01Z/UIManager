using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserBlocker : MonoSingleton<UserBlocker>
{
    private bool InitChecker = false;
    private bool _isBlocked = false;
    
    public bool IsBlocked
    {
        get
        {
            if (!InitChecker)
            {
                InitBlock();
            }
            return _isBlocked;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        InitBlock();
    }

    private void InitBlock()
    {
        if (!InitChecker)
        {
#if UNITY_ANDROID
            _isBlocked |= !StoreInstallerChecker();
            _isBlocked |= IsAndroidRooted();
#elif UNITY_IOS || UNITY_IPHONE
            _isBlocked |= JailbreakDetector.IsJailBroken();
#endif
            _isBlocked |= ConfigHolder.Instance.ContainNativeDevice(GameConfig.DeviceId);
            var ggAdsId = GetIDFA();
            _isBlocked |= ConfigHolder.Instance.ContainGoogleAds(ggAdsId);
            
            InitChecker = true;
        }
    }


    //True khi cài game từ nguồn chính thống 
    bool StoreInstallerChecker()
    {
        try
        {
            string installer = null;
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var pm       = activity.Call<AndroidJavaObject>("getPackageManager");
                installer = pm.Call<string>("getInstallerPackageName", Application.identifier);
            }

            if (installer == "com.android.vending")
            {
                Debug.Log("Cài từ Google Play");
                return true;
            }
            else if (installer == "com.miui.packageinstaller" || installer == "com.android.packageinstaller")
            {
                Debug.Log("Sideload qua file APK");
            }
            else if (installer == null)
            {
                Debug.Log("Không xác định nguồn cài");
            }
            else
            {
                Debug.Log("Nguồn khác: " + installer);
            }
        }
        catch { }

        return false;
    }

    private bool IsAndroidRooted()
    {
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // 1. Kiểm tra các file nhị phân su / magisk mặc định
            string[] paths = {
                "/system/app/Superuser.apk",
                "/sbin/su",
                "/system/bin/su",
                "/system/xbin/su",
                "/data/local/xbin/su",
                "/data/local/bin/su",
                "/system/sd/xbin/su",
                "/system/bin/failsafe/su",
                "/data/local/su",
                "/su/bin/su",
                "/system/app/Magisk.apk",
                "/system/app/MagiskManager.apk"
            };

            foreach (string path in paths)
            {
                if (System.IO.File.Exists(path)) return true;
            }

            // 2. Chặn các bản ROM test/custom thường được dùng để root
            using (var buildClass = new AndroidJavaClass("android.os.Build"))
            {
                string tags = buildClass.GetStatic<string>("TAGS");
                if (tags != null && tags.Contains("test-keys")) return true;
            }
#endif
        }
        catch { }

        return false;
    }
    
    
    public string GetIDFA()
    {
        try
        {
#if UNITY_IOS && !UNITY_EDITOR
        return UnityEngine.iOS.Device.advertisingIdentifier;
#elif UNITY_ANDROID && !UNITY_EDITOR
        return GetGoogleAdId();
#else
            return "";
#endif
        }
        catch (Exception e)
        {
        }

        return "";
    }
    string GetGoogleAdId()
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                using (AndroidJavaClass adIdClient = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
                {
                    AndroidJavaObject adInfo = adIdClient.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity);

                    if (adInfo == null) return "";

                    string id = adInfo.Call<string>("getId");

                    if (string.IsNullOrEmpty(id)) return "";

                    return id;
                }
            }
        }
        catch { }
        return "";
    }
}
