using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Localization
{
    public static class LocalizationManager
    {
        // Sự kiện được gọi khi ngôn ngữ thay đổi
        public static event Action OnLanguageChanged;

        private static LanguageCode _currentLanguage;
        private static LocalizationConfig _config;

        // Cache lưu Text theo mô hình [Key] = [Value]
        // Zero GC lookup bằng cách sử dụng Dictionary thay vì duyệt Array
        private static Dictionary<string, string> _textCache;
        private static Dictionary<string, LocalizedAssetEntry> _assetCache;

        public static LanguageCode CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage == value) return;
                _currentLanguage = value;
                PlayerPrefs.SetInt("Localization_Lang", (int)_currentLanguage);
                PlayerPrefs.Save();
                
                // Nạp từ điển mới
                LoadTextCacheForLanguage(_currentLanguage);
                OnLanguageChanged?.Invoke();
            }
        }

        public static LocalizationConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = Resources.Load<LocalizationConfig>("LocalizationConfig");
#if UNITY_EDITOR
                    if (_config == null)
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:LocalizationConfig");
                        if (guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            _config = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationConfig>(path);
                        }
                    }
                    
                    // Đảm bảo cache được khởi tạo trong Editor
                    if (_textCache == null)
                    {
                        _textCache = new Dictionary<string, string>();
                        // Nạp dữ liệu từ CSV trong Editor để các hàm GetText hoạt động ngay
                        LoadTextCacheForLanguage(Config.DefaultLanguage);
                    }
                    if (_assetCache == null)
                    {
                        _assetCache = new Dictionary<string, LocalizedAssetEntry>();
                        if (_config != null && _config.AssetEntries != null)
                        {
                            foreach (var entry in _config.AssetEntries)
                            {
                                if (!string.IsNullOrEmpty(entry.Key))
                                    _assetCache[entry.Key] = entry;
                            }
                        }
                    }
#endif
                }
                return _config;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            _textCache = new Dictionary<string, string>(1024); // Đặt capacity ban đầu để tránh resize array 内部
            _assetCache = new Dictionary<string, LocalizedAssetEntry>(256);
            
            if (Config != null && Config.AssetEntries != null)
            {
                foreach (var entry in Config.AssetEntries)
                {
                    if (!string.IsNullOrEmpty(entry.Key))
                        _assetCache[entry.Key] = entry;
                }
            }

            int savedLang = PlayerPrefs.GetInt("Localization_Lang", (int)LanguageCode.Auto);
            LanguageCode targetLang = (LanguageCode)savedLang;
            
            if (targetLang == LanguageCode.Auto)
            {
                targetLang = LanguageUtility.GetSystemLanguage();
            }

            // Fallback nếu System Language không được support
            if (Config != null && !Config.SupportedLanguages.Contains(targetLang))
            {
                targetLang = Config.DefaultLanguage;
            }

            _currentLanguage = targetLang;
            LoadTextCacheForLanguage(_currentLanguage);
        }

        public static string GetText(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

#if UNITY_EDITOR
            if (_textCache == null || _textCache.Count == 0) 
            {
                var c = Config; // Trigger initialization
            }

            // Trong Editor, nếu Cache chưa có hoặc không tìm thấy, thử tìm trực tiếp trong Config Board
            if (_textCache == null || !_textCache.ContainsKey(key))
            {
                if (Config != null && Config.LocalDataBoard != null)
                {
                    if (Config.LocalDataBoard.TryGetValue(key, out var entry))
                    {
                        // Lấy ngôn ngữ hiện tại hoặc mặc định
                        string langStr = _currentLanguage == LanguageCode.Auto ? Config.DefaultLanguage.ToString() : _currentLanguage.ToString();
                        if (entry.Values != null && entry.Values.TryGetValue(langStr, out string val))
                        {
                            return val;
                        }
                    }
                }
            }
#endif

            if (_textCache != null && _textCache.TryGetValue(key, out string value))
            {
                return value;
            }
            return $"[{key}]"; // Fallback nếu không có Key
        }

        public static T GetAsset<T>(string key) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key)) return null;

#if UNITY_EDITOR
            if (_assetCache == null) { var c = Config; } // Trigger initialization
#endif

            if (_assetCache != null && _assetCache.TryGetValue(key, out var entry))
            {
                if (entry.Assets.TryGetValue(_currentLanguage, out var asset))
                {
                    return asset as T;
                }
                
                // Fallback về default lang
                if (Config != null && entry.Assets.TryGetValue(Config.DefaultLanguage, out var defaultAsset))
                {
                    return defaultAsset as T;
                }
            }
            return null;
        }

        private static void LoadTextCacheForLanguage(LanguageCode lang)
        {
            _textCache.Clear();
            // Việc nạp vào Cache có thể từ File CSV tải xuống được lưu trong persistentDataPath
            // Hoặc từ Resources tuỳ vào cấu hình
            
            string filePath = Application.persistentDataPath + "/localization_data.csv";
            if (System.IO.File.Exists(filePath))
            {
                string csvContent = System.IO.File.ReadAllText(filePath);
                ParseCSVToCache(csvContent, lang);
            }
            else
            {
                // Fallback tìm trong Resources
                TextAsset fallbackFile = Resources.Load<TextAsset>("localization_data");
                if (fallbackFile != null)
                {
                    ParseCSVToCache(fallbackFile.text, lang);
                }
            }
        }

        // Parse CSV siêu cơ bản (cần mở rộng nếu dùng dấu phẩy trong chuỗi)
        // Cột 0: Key, Cột 1..N: Ngôn ngữ theo thứ tự SupportedLanguages
        public static void ParseCSVToCache(string csvContent, LanguageCode lang)
        {
            if (string.IsNullOrEmpty(csvContent)) return;

            string[] lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            string[] headers = lines[0].Split(',');
            int colIndex = -1;

            string langStr = lang.ToString();
            for (int i = 1; i < headers.Length; i++)
            {
                if (headers[i].Trim().Equals(langStr, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex = i;
                    break;
                }
            }

            if (colIndex == -1)
            {
                Debug.LogWarning($"Localization: Không tìm thấy cột cho ngôn ngữ {langStr} trong file dữ liệu. Nội dung có thể bị sai định dạng CSV.");
                return;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                // Cần 1 parser xịn hơn ở thực tế (VD: xử lý dấu phẩy trong cặp ngoặc kép)
                // Đây là bản rút gọn
                string currentLine = lines[i];
                if (string.IsNullOrWhiteSpace(currentLine)) continue;
                
                List<string> row = ParseCSVLine(currentLine);
                if (row.Count > colIndex)
                {
                    string key = row[0].Trim();
                    string val = row[colIndex].Trim();
                    // Loại bỏ xử lý ngoặc kép dư thừa nếu có
                    if (val.StartsWith("\"") && val.EndsWith("\""))
                    {
                        val = val.Substring(1, val.Length - 2).Replace("\"\"", "\"");
                    }
                    _textCache[key] = val;
                }
            }
        }
        
        // Hỗ trợ parse CSV chuẩn (có dấu phẩy nằm trong ngoặc kép)
        private static List<string> ParseCSVLine(string line)
        {
            List<string> row = new List<string>(10);
            bool inQuotes = false;
            int startIndex = 0;
            
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    row.Add(line.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                }
            }
            row.Add(line.Substring(startIndex));
            return row;
        }
    }
}
