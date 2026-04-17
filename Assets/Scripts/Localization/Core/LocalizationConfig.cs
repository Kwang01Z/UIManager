using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Localization
{
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Localization/Localization Config")]
    public class LocalizationConfig : ScriptableObject
    {
        [Header("General Settings")]
        public LanguageCode DefaultLanguage = LanguageCode.en;

        public List<LanguageCode> SupportedLanguages = new List<LanguageCode>() 
        { 
            LanguageCode.en, LanguageCode.vi, LanguageCode.ja, LanguageCode.ko, 
            LanguageCode.zh, LanguageCode.fr, LanguageCode.de, LanguageCode.es, 
            LanguageCode.pt, LanguageCode.ru 
        };

        public void ResetSupportedLanguages()
        {
            if (SupportedLanguages == null) SupportedLanguages = new List<LanguageCode>();
            SupportedLanguages.Clear();
            foreach (LanguageCode lang in Enum.GetValues(typeof(LanguageCode)))
            {
                if (lang != LanguageCode.Auto) SupportedLanguages.Add(lang);
            }
        }

        private void Reset()
        {
            ResetSupportedLanguages();
        }

        [Tooltip("URL của file Google Sheets dạng Export CSV (để lấy dữ liệu Text).")]
        public string GoogleSheetsCSVUrl;

        [Header("Gemini AI Translation")]
        public string GeminiApiKey;

        [Header("Asset Localization")]
        public List<LocalizedAssetEntry> AssetEntries = new List<LocalizedAssetEntry>();

        [HideInInspector]
        public List<LocalizationEntry> LocalDataBoard = new List<LocalizationEntry>();

        public void LoadFromLocalCSV()
        {
            LocalDataBoard.Clear();
            TextAsset csvFile = Resources.Load<TextAsset>("localization_data");
            if (csvFile == null)
            {
                Debug.LogWarning("Localization: Không tìm thấy file Resources/localization_data.csv");
                return;
            }

            string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            string[] headers = lines[0].Split(',');
            // Map header index to language code (Hỗ trợ cả "en" và "English")
            Dictionary<int, string> colMap = new Dictionary<int, string>();
            var availableLangs = new List<string>();
            foreach (LanguageCode l in Enum.GetValues(typeof(LanguageCode))) availableLangs.Add(l.ToString().ToLower());

            for (int i = 1; i < headers.Length; i++)
            {
                string h = headers[i].Trim().ToLower();
                foreach (var lang in availableLangs)
                {
                    // Nếu cột là "en" hoặc "english" (bắt đầu bằng en)
                    if (h == lang || h.StartsWith(lang))
                    {
                        colMap[i] = lang;
                        break;
                    }
                }
            }

            for (int i = 1; i < lines.Length; i++)
            {
                // Simple CSV parse (Không handle nested commas tốt như parser chuyên dụng nhưng tạm ổn)
                string[] row = ParseCSVLine(lines[i]);
                if (row.Length == 0) continue;

                var entry = new LocalizationEntry { Key = row[0].Trim() };
                for (int j = 1; j < row.Length; j++)
                {
                    if (colMap.TryGetValue(j, out var lang))
                        entry.Values[lang] = row[j].Trim();
                }
                LocalDataBoard.Add(entry);
            }
            Debug.Log($"Localization: Đã nạp {LocalDataBoard.Count} dòng dữ liệu. Nhớ nhấn 'Save To Local CSV' nếu bạn muốn lưu các thay đổi này.");
        }

        private string[] ParseCSVLine(string line)
        {
            List<string> row = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\"') inQuotes = !inQuotes;
                else if (line[i] == ',' && !inQuotes)
                {
                    row.Add(line.Substring(startIndex, i - startIndex).Trim('\"'));
                    startIndex = i + 1;
                }
            }
            row.Add(line.Substring(startIndex).Trim('\"'));
            return row.ToArray();
        }

        public void SaveToLocalCSV()
        {
#if UNITY_EDITOR
            if (LocalDataBoard.Count == 0) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Key");
            var langs = new List<string>();
            foreach (var lang in SupportedLanguages) langs.Add(lang.ToString());
            
            foreach (var lang in langs) sb.Append($",{lang}");
            sb.AppendLine();

            foreach (var row in LocalDataBoard)
            {
                sb.Append(row.Key);
                foreach (var lang in langs)
                {
                    row.Values.TryGetValue(lang, out string val);
                    // Wrap in quotes if contains comma
                    if (val != null && val.Contains(",")) val = $"\"{val}\"";
                    sb.Append($",{val}");
                }
                sb.AppendLine();
            }

            string path = "Assets/Resources/localization_data.csv";
            System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("<color=green>Localization: Đã lưu dữ liệu vào CSV cục bộ!</color>");
#endif
        }

        public void ClearAllLocalData()
        {
#if UNITY_EDITOR
            LocalDataBoard.Clear();
            string path = "Assets/Resources/localization_data.csv";
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                UnityEditor.AssetDatabase.Refresh();
            }
            
            PlayerPrefs.DeleteKey("Localization_Lang");
            PlayerPrefs.Save();
            
            Debug.Log("<color=orange>Localization: Đã xóa toàn bộ dữ liệu Local (Board, CSV, PlayerPrefs).</color>");
#endif
        }
    }

    [Serializable]
    public class LocalizedAssetEntry
    {
        public string Key;
        public UnitySerializedDictionary<LanguageCode, UnityEngine.Object> Assets = new UnitySerializedDictionary<LanguageCode, UnityEngine.Object>();
    }

    [Serializable]
    public class LocalizationEntry
    {
        public string Key;
        public UnitySerializedDictionary<string, string> Values = new UnitySerializedDictionary<string, string>();
    }

    [Serializable]
    public class UnitySerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keyData = new List<TKey>();
        [SerializeField] private List<TValue> valueData = new List<TValue>();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            this.Clear();
            for (int i = 0; i < keyData.Count && i < valueData.Count; i++)
            {
                this[keyData[i]] = valueData[i];
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            keyData.Clear();
            valueData.Clear();
            foreach (var kvp in this)
            {
                keyData.Add(kvp.Key);
                valueData.Add(kvp.Value);
            }
        }
    }
}
