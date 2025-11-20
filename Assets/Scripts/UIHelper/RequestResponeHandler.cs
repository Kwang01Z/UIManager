using Cysharp.Threading.Tasks;
using MemoryPack;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[MemoryPackable]
public partial class PayloadData
{
    public string DeviceId { get; set; }
    public string Json { get; set; }
}

[MemoryPackable]
public partial class VerifyIAPRequestData
{
    public ReceiptClient Receipt { get; set; }

    public int ItemID { get; set; }

    public string AccountID { get; set; }

    /// <summary>
    /// 2 = subscription
    /// 
    /// </summary>
    public int ProductType { get; set; }


    public string ProductID { get; set; }
}

[MemoryPackable]
public partial class ReceiptClient
{

    public string store { get; set; } = string.Empty;


    public string transactionID { get; set; } = string.Empty;


    public string payload { get; set; } = string.Empty;
}

public static class RequestResponseHandler
{
    private static readonly string baseAddress = "";

    private const string API_KEY = "";
    private const string API_KEY_HEADER = "X-API-Key";

    private const string DEVICE_ID = "DeviceId";

    private const string CONTENT_TYPE = "Content-Type";
    private const string MEMORYPACK_CONTENT_TYPE = "application/x-memorypack";

    private const string ACCEPT = "Accept";
    private const string ACCEPT_VALUE = "application/json";

    private const string ApiRemove = "api/player/remove";
    private const string VerifyIAP = "api/pay/verifyIAP";
    private const string ConfirmIAP = "api/pay/confirmIAP";

    #region API Payload Class
    // Generic method for API calls
    public static async Task MakeApiRequest<T>(string endpoint, string deviceId, string jsonData = ""
        , Action<T> onDone = null, Action onFail = null)
        where T : class
    {
        if (string.IsNullOrEmpty(baseAddress))
        {
            return;
        }
        var payload = new PayloadData { DeviceId = deviceId, Json = jsonData };
        byte[] bytes = MemoryPackSerializer.Serialize(payload);

        using var uwr = new UnityWebRequest(baseAddress + endpoint, "POST");
        uwr.uploadHandler = new UploadHandlerRaw(bytes);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader(API_KEY_HEADER, API_KEY);
        uwr.SetRequestHeader(CONTENT_TYPE, MEMORYPACK_CONTENT_TYPE);
        uwr.SetRequestHeader(ACCEPT, ACCEPT_VALUE);
        //uwr.SetRequestHeader(DEVICE_ID, GameConfig.DeviceId);
        try
        {
            await SendWebRequestAsync(uwr);

#if UNITY_2020_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                Debug.LogError($"[MakeApiRequest] [{endpoint}] Error: {uwr.error} | Status: {uwr.responseCode} | Body: {uwr.downloadHandler.text}");
                onFail?.Invoke();
                return;
            }

            string json = uwr.downloadHandler.text;
            var result = JsonUtility.FromJson<T>(json);
            onDone?.Invoke(result);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MakeApiRequest] [{endpoint}] Error processing response: {e}");
            onFail?.Invoke();
        }
    }

    // Generic method for saving data to API
    public static async Task<bool> SaveToApi(string endpoint, string deviceId, string jsonData)
    {
        try
        {
            if (string.IsNullOrEmpty(baseAddress))
            {
                return false;
            }

            var payload = new PayloadData { DeviceId = deviceId, Json = jsonData };
            byte[] bytes = MemoryPackSerializer.Serialize(payload);

            using var uwr = new UnityWebRequest(baseAddress + endpoint, "POST");
            uwr.uploadHandler = new UploadHandlerRaw(bytes);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader(API_KEY_HEADER, API_KEY);
            uwr.SetRequestHeader(CONTENT_TYPE, MEMORYPACK_CONTENT_TYPE);
            //uwr.SetRequestHeader(DEVICE_ID, GameConfig.DeviceId);

            await SendWebRequestAsync(uwr);

#if UNITY_2020_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                Debug.LogError($"[SaveToApi] [{endpoint}] Save Error: {uwr.error}");
                return false;
            }

            Debug.Log($"[SaveToApi] [{endpoint}] Save successful");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveToApi] [{endpoint}] Error saving data: {e}");
            return false;
        }
    }

    public static async UniTask<bool> VerifyIAPAsync(VerifyIAPRequestData payload)
    {
        try
        {
            if (string.IsNullOrEmpty(baseAddress))
            {
                return false;
            }

            byte[] bytes = MemoryPackSerializer.Serialize(payload);
            var (response, statusCode, isSuccess, error) = await PostMemoryPackAsync(VerifyIAP, bytes);
            if (statusCode != 200 && statusCode != 625)
            {
                Debug.LogError($"[VerifyIAPAsync] Error: {error}");
                return false;
            }

            Debug.Log($"[VerifyIAPAsync] Save successful");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[VerifyIAPAsync] Error saving data: {e}");
            return false;
        }
    }

    /// <summary>
    /// Gọi API confirm IAP trên server (expects application/x-memorypack, returns 204 NoContent on success)
    /// </summary>
    public static async UniTask<bool> ConfirmIAPAsync(VerifyIAPRequestData payload)
    {
        try
        {
            if (string.IsNullOrEmpty(baseAddress))
                return false;

            if (payload == null || payload.Receipt == null || string.IsNullOrEmpty(payload.Receipt.transactionID))
            {
                Debug.LogError("[ConfirmIAPAsync] Invalid payload: missing transactionID");
                return false;
            }

            byte[] bytes = MemoryPackSerializer.Serialize(payload);
            var (response, statusCode, isSuccess, error) = await PostMemoryPackAsync(ConfirmIAP, bytes);
            if (!isSuccess)
            {
                Debug.LogError($"[ConfirmIAPAsync] Error: {error}");
                return false;
            }

            if (statusCode == 204)
            {
                Debug.Log("[ConfirmIAPAsync] Confirmed successful");
                return true;
            }

            Debug.LogError($"[ConfirmIAPAsync] Unexpected status: {statusCode} body: {response}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfirmIAPAsync] Exception: {e}");
            return false;
        }
    }

    /// <summary>
    /// Helper để gửi POST với MemoryPack payload và header API key.
    /// Trả về (responseText, statusCode, isSuccess, errorMessage)
    /// </summary>
    private static async UniTask<(string responseText, long statusCode, bool isSuccess, string errorMessage)>
        PostMemoryPackAsync(string endpoint, byte[] requestData)
    {
        try
        {
            string url = $"{baseAddress}{endpoint}";
            using var uwr = new UnityWebRequest(url, "POST");
            uwr.uploadHandler = new UploadHandlerRaw(requestData);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader(API_KEY_HEADER, API_KEY);
            uwr.SetRequestHeader(CONTENT_TYPE, MEMORYPACK_CONTENT_TYPE);
            //uwr.SetRequestHeader(DEVICE_ID, GameConfig.DeviceId);

            await SendWebRequestAsync(uwr);
            // Log chi tiết request/response

            var logMessage = new StringBuilder();
            logMessage.AppendLine("[POST] Chi tiết Yêu Cầu và Phản Hồi:");
            logMessage.AppendLine($"  URL: {url}");
            logMessage.AppendLine($"  Status: {uwr.responseCode}");
            logMessage.AppendLine(
                $"  Response: {uwr.downloadHandler?.text ?? string.Empty}");
            Debug.Log(logMessage.ToString());

#if UNITY_2020_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                return (uwr.downloadHandler?.text ?? string.Empty, uwr.responseCode, false, uwr.error);
            }

            return (uwr.downloadHandler?.text ?? string.Empty, uwr.responseCode, true, string.Empty);
        }
        catch (Exception e)
        {
            Debug.LogError($"PostMemoryPackAsync error: {e}");
            return (string.Empty, 0, false, e.Message);
        }
    }

#if UNITY_EDITOR
    public static async Task<bool> RemovePlayerOnServer(string deviceId)
    {
        // Tạo payload đúng định dạng JSON
        var payload = new PayloadData
        {
            DeviceId = deviceId,
            Json = "7btfNlxK0BY0vqRmvMRvVatG7puHcc0z"
        };
        string jsonPayload = JsonUtility.ToJson(payload);

        using var uwr = new UnityWebRequest(baseAddress + "api/player/remove", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("X-API-Key", "b2c3d4e5-f6g7-8901-bcde-f23456789012");
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Accept", "application/json");

        try
        {
            await SendWebRequestAsync(uwr);
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[RemovePlayerOnServer] Success: " + uwr.downloadHandler.text);
                return true;
            }
            else
            {
                Debug.LogError("[RemovePlayerOnServer] Error: " + uwr.error + " | " + uwr.downloadHandler.text);
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[RemovePlayerOnServer] Exception: " + e);
            return false;
        }
    }
#endif
    #endregion

    private static Task<UnityWebRequest> SendWebRequestAsync(UnityWebRequest uwr)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();
        var operation = uwr.SendWebRequest();
        operation.completed += _ => tcs.SetResult(uwr);
        return tcs.Task;
    }
}
