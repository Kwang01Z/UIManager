using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MemoryPack;
using Sirenix.Utilities;
using UnityEngine;

public static class RequestResponseHandle
{
    private static readonly string _baseUrl = "http://165.1.76.155";
    private static readonly string _adminPassWord = "admin@123";
    private static GameApiClient _apiClient = new GameApiClient(_baseUrl);

    public static async UniTask SavePlayerData(PlayerData playerData)
    {
        if (playerData == null || playerData.PlayerID.IsNullOrWhitespace()) return;
        // 2. Prepare binary for Server (MemoryPack)
        byte[] rawData = MemoryPackSerializer.Serialize(playerData);

        // 3. Save to server via ApiClient
        try 
        {
            await _apiClient.SaveData(GameConstant.GameName, playerData.PlayerID, rawData);
            Debug.Log("Successfully saved data to local and server.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Server Save Error: {ex.Message}");
        }
    }
    
    public static async UniTask<PlayerData> LoadPlayerData(string playerId)
    {
        try 
        {
            // 1. Try load from server
            GameData serverData = await _apiClient.GetData(GameConstant.GameName, playerId);

            if (serverData != null && serverData.Data != null)
            {
                var pd = MemoryPackSerializer.Deserialize<PlayerData>(serverData.Data);
                
                Debug.Log($"Loaded data for {pd.NickName} from server.");
                return pd;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not load from server ({ex.Message}), trying local save instead.");
        }

        Debug.Log("No data found on server.");
        return null;
    }
    
    public static async UniTask DeletePlayerData(string playerId)
    {
        try 
        {
            // 1. Delete on server
            await _apiClient.DeleteData(GameConstant.GameName, playerId, _adminPassWord);
            
            Debug.Log("Successfully deleted data from server.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Deletion failed: {ex.Message}");
        }
    }
}
