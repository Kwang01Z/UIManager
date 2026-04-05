using Cysharp.Threading.Tasks;
using MemoryPack;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameApiClient
{
    private readonly string _baseUrl;

    public GameApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    // 1. Lưu dữ liệu (Nén binary qua MemoryPack)
    public async UniTask SaveData(string gameName, string playerId, byte[] rawData)
    {
        var gameData = new GameData { GameName = gameName, PlayerId = playerId, Data = rawData };
        byte[] binary = MemoryPackSerializer.Serialize(gameData);

        using var request = new UnityWebRequest($"{_baseUrl}/api/save", UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(binary);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/x-memorypack");

        await request.SendWebRequest().ToUniTask();
        HandleResponse(request);
    }

    // 2. Lấy dữ liệu (Dùng POST binary qua MemoryPack)
    public async UniTask<GameData> GetData(string gameName, string playerId)
    {
        var dataRequest = new GetRequest(gameName, playerId);
        byte[] binary = MemoryPackSerializer.Serialize(dataRequest);

        using var request = new UnityWebRequest($"{_baseUrl}/api/get", UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(binary);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/x-memorypack");

        await request.SendWebRequest().ToUniTask();
        HandleResponse(request);

        byte[] responseBinary = request.downloadHandler.data;
        return MemoryPackSerializer.Deserialize<GameData>(responseBinary);
    }

    // 3. Xóa dữ liệu (Dùng POST binary qua MemoryPack)
    public async UniTask DeleteData(string gameName, string playerId, string password)
    {
        var deleteRequest = new DeleteRequest(gameName, playerId, password);
        byte[] binary = MemoryPackSerializer.Serialize(deleteRequest);

        using var request = new UnityWebRequest($"{_baseUrl}/api/delete", UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(binary);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/x-memorypack");

        await request.SendWebRequest().ToUniTask();
        HandleResponse(request);
    }

    private void HandleResponse(UnityWebRequest request)
    {
        if (request.responseCode == 429)
        {
            throw new Exception($"Rate Limited: {request.downloadHandler.text}");
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            throw new Exception($"Server Error ({request.responseCode}): {request.error}\n{request.downloadHandler.text}");
        }
    }
}
