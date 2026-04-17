using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Runtime.Localization
{
    public static class GoogleSheetsDownloader
    {
        public static async UniTask<bool> DownloadAndCacheFormatCSV(LocalizationConfig config = null)
        {
            if (config == null) config = LocalizationManager.Config;
            
            if (config == null || string.IsNullOrEmpty(config.GoogleSheetsCSVUrl))
            {
                Debug.LogError("Localization: GoogleSheetsCSVUrl bị trống hoặc không tìm thấy LocalizationConfig.");
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
                
#if UNITY_EDITOR
                // Trong Editor, lưu trực tiếp vào Resources để Board có thể đọc được ngay
                string editorPath = Application.dataPath + "/Resources/localization_data.csv";
                // Đảm bảo thư mục Resources tồn tại
                if (!System.IO.Directory.Exists(Application.dataPath + "/Resources"))
                    System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
                path = editorPath;
#endif
                try
                {
                    File.WriteAllText(path, csvData);
                    Debug.Log($"Localization: Đã lưu cache file CSV tại {path}");
                    
#if UNITY_EDITOR
                    UnityEditor.AssetDatabase.Refresh();
#endif
                    
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
