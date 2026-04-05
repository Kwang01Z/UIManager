using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using MemoryPack;

public partial class DataManager
{
    public static PlayerData PlayerData;
    public static string SyncPlayerDataResultKey = "SyncPlayerDataResult";
    
    public static async UniTask SavePlayerData()
    {
        await LocalDatabaseManager.SavePlayerData(PlayerData);
        if (PlayerPrefs.HasKey(SyncPlayerDataResultKey))
        {
            await RequestResponseHandle.SavePlayerData(PlayerData);
        }
    }

    public static async UniTask LoadPlayerDataFromServer(Action<PlayerData> onSuccess, Action onError = null)
    {
        try
        {
            PlayerData = await RequestResponseHandle.LoadPlayerData(GameConstant.DeviceID);
            if (PlayerData != null)
            {
                onSuccess?.Invoke(PlayerData);
            }
            else
            {
                onError?.Invoke();
            }
        }
        catch (Exception ex)
        {
            onError?.Invoke();
        }
    }

    public static async UniTask LoadPlayerDataFromLocal(Action<PlayerData> onSuccess, Action onFail = null)
    {
        PlayerData = await LocalDatabaseManager.LoadPlayerData();
        if (PlayerData != null)
        {
            onSuccess?.Invoke(PlayerData);
        }
        else
        {
            onFail?.Invoke();
        }
    }
}
