using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Runtime.Localization
{
    public static class GoogleSheetsDownloader
    {
        public static async UniTask<bool> DownloadAndCacheFormatCSV()
        {
            var config = LocalizationManager.Config;
            if (config == null || string.IsNullOrEmpty(config.GoogleSheetsCSVUrl))
            {
                Debug.LogError("Localization: GoogleSheetsCSVUrl bị trống.");
                return false;
            }

            Debug.Log("Localization: Đang tải lại dữ liệu từ Google Sheets...");
            
            using (UnityWebRequest webRequest = UnityWebRequest.Get(config.GoogleSheetsCSVUrl))
            {
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Localization: Tải dữ liệu lỗi: {webRequest.error}");
                    return false;
                }

                string csvData = webRequest.downloadHandler.text;
                
                // Lưu vào cache
                string path = Application.persistentDataPath + "/localization_data.csv";
                try
                {
                    File.WriteAllText(path, csvData);
                    Debug.Log($"Localization: Đã lưu cache file CSV tại {path}");
                    
                    // Force update lại RAM
                    LocalizationManager.ParseCSVToCache(csvData, LocalizationManager.CurrentLanguage);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Localization: Không thể ghi file tải về. Lỗi: {e.Message}");
                    return false;
                }
            }
        }
    }
}
