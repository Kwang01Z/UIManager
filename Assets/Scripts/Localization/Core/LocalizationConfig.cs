using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Localization
{
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Localization/Localization Config")]
    public class LocalizationConfig : ScriptableObject
    {
        [Title("General Settings")]
        [LabelText("Default Language")]
        public LanguageCode DefaultLanguage = LanguageCode.en;

        [LabelText("Supported Languages")]
        public List<LanguageCode> SupportedLanguages = new List<LanguageCode>() 
        { 
            LanguageCode.en, LanguageCode.vi, LanguageCode.ja, LanguageCode.ko, 
            LanguageCode.zh, LanguageCode.fr, LanguageCode.de, LanguageCode.es, 
            LanguageCode.pt, LanguageCode.ru 
        };

        private void Reset()
        {
            // Tự động điền tất cả ngôn ngữ khi tạo mới hoặc Reset (trừ Auto)
            if (SupportedLanguages == null) SupportedLanguages = new List<LanguageCode>();
            SupportedLanguages.Clear();
            foreach (LanguageCode lang in Enum.GetValues(typeof(LanguageCode)))
            {
                if (lang != LanguageCode.Auto) SupportedLanguages.Add(lang);
            }
        }

        [Title("Google Sheets Sync")]
        [LabelText("CSV Export URL")]
        [InfoBox("URL của file Google Sheets dạng Export CSV (để lấy dữ liệu Text).")]
        public string GoogleSheetsCSVUrl;

        [Title("Gemini AI Translation")]
        [LabelText("API Key")]
        public string GeminiApiKey;

        [Title("Asset Localization")]
        [InfoBox("Mapping các từ khóa (key) tới Asset tương ứng (Sprite, Audio,...) theo cấu trúc cục bộ.")]
        public List<LocalizedAssetEntry> AssetEntries = new List<LocalizedAssetEntry>();
    }

    [Serializable]
    public class LocalizedAssetEntry
    {
        [HorizontalGroup("Key", 0.3f), HideLabel]
        public string Key;
        
        [HorizontalGroup("Key", 0.7f), HideLabel]
        [DictionaryDrawerSettings(KeyLabel = "Lang", ValueLabel = "Asset")]
        public UnitySerializedDictionary<LanguageCode, UnityEngine.Object> Assets = new UnitySerializedDictionary<LanguageCode, UnityEngine.Object>();
    }

    [Serializable]
    public class UnitySerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private List<TKey> keyData = new List<TKey>();
        
        [SerializeField, HideInInspector]
        private List<TValue> valueData = new List<TValue>();

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
