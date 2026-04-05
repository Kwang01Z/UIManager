using System;
using Cysharp.Threading.Tasks;
using MemoryPack;
using System.IO;
using UnityEngine;

public static class LocalDatabaseManager
{
    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "PlayerData.dat");

    // Save PlayerData locally as binary using MemoryPack
    public static async UniTask SavePlayerData(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogError($"[LocalDatabaseManager] PlayerData is null");
            return;
        }
        data.LastUpdateTime = DateTime.UtcNow.ToDefaultString();
        byte[] binary = MemoryPackSerializer.Serialize(data);
        await File.WriteAllBytesAsync(SavePath, binary).AsUniTask();
        Debug.Log($"Data saved to local at: {SavePath}");
    }

    // Load PlayerData from local binary using MemoryPack
    public static async UniTask<PlayerData> LoadPlayerData()
    {
        if (!File.Exists(SavePath))
            return null;

        byte[] binary = await File.ReadAllBytesAsync(SavePath).AsUniTask();
        return MemoryPackSerializer.Deserialize<PlayerData>(binary);
    }

    // Delete local save
    public static void DeleteLocalSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public static PlayerData CreateNewPlayerData()
    {
        var deviceId = GameConstant.DeviceID;
        PlayerData data = new PlayerData();
        data.CreateTime = DateTime.UtcNow.ToDefaultString();
        data.PlayerID = deviceId;
        data.NickName = "Player_" + deviceId.Substring(4);
        return data;
    }
}
