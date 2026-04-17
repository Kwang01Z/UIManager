using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Runtime.Localization.Editor
{
    public static class GeminiTranslatorHelper
    {
        public static async UniTask<string> RequestGeminiAPI(string prompt, string apiKey, string modelName, int retryCount = 3)
        {
            if (string.IsNullOrEmpty(apiKey)) return "Lỗi: Chưa có API Key.";
            
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
            string jsonPayload = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{EscapeJson(prompt)}\"}}]}}]}}";
            
            for (int i = 0; i < retryCount; i++)
            {
                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    request.timeout = 30; // Timeout 30 giây
                    await request.SendWebRequest().ToUniTask();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = request.downloadHandler.text;
                        return ExtractTextFromJsonPattern(responseText);
                    }

                    // Nếu lỗi 503 (Overloaded) hoặc 429 (Too Many Requests), đợi và thử lại
                    if (request.responseCode == 503 || request.responseCode == 429)
                    {
                        if (i < retryCount - 1)
                        {
                            // Đối với 429 (Rate Limit), cần đợi lâu hơn một chút (ví dụ 10-15s)
                            int waitTime = request.responseCode == 429 ? 15000 : (i + 1) * 2000;
                            Debug.LogWarning($"Gemini API rate limited/overloaded ({request.responseCode}). Retrying in {waitTime/1000}s... (Attempt {i+1}/{retryCount})");
                            await UniTask.Delay(waitTime);
                            continue;
                        }
                    }

                    return "Lỗi: " + request.error + "\n" + request.downloadHandler.text;
                }
            }
            return "Lỗi: Quá số lần thử lại (API không khả dụng).";
        }

        private static string EscapeJson(string str)
        {
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }

        private static string ExtractTextFromJsonPattern(string json)
        {
            string searchKey = "\"text\": \"";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) return "Lỗi: Không lấy được kết quả JSON trả về.";
            
            startIndex += searchKey.Length;
            int endIndex = json.IndexOf("\"", startIndex);
            while (endIndex > 0 && json[endIndex - 1] == '\\')
            {
                endIndex = json.IndexOf("\"", endIndex + 1);
            }

            if (endIndex == -1) return "Lỗi: Parse JSON thất bại.";

            string rawText = json.Substring(startIndex, endIndex - startIndex);
            return rawText.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
