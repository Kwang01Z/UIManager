using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;

namespace Runtime.Localization
{
    [CustomEditor(typeof(LocalizationConfig))]
    public class LocalizationConfigEditor : UnityEditor.Editor
    {
        private LocalizationConfig _config;
        private string _searchFilter = "";
        private Vector2 _scrollPos;
        private int _selectedTab = 0;
        private readonly string[] _tabs = { "Settings", "Operations", "Data Board" };
        private LanguageCode _sourceLanguage = LanguageCode.en;

        private float _keyWidth = 150;
        private float _langWidth = 150;

        private void OnEnable()
        {
            _config = (LocalizationConfig)target;
            _keyWidth = EditorPrefs.GetFloat("Loc_KeyWidth", 150);
            _langWidth = EditorPrefs.GetFloat("Loc_LangWidth", 150);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            EditorGUILayout.Space();

            switch (_selectedTab)
            {
                case 0:
                    DrawSettingsTab();
                    break;
                case 1:
                    DrawOperationsTab();
                    break;
                case 2:
                    DrawDataBoardTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettingsTab()
        {
            if (GUILayout.Button("Reset Supported Languages (Add All)"))
            {
                Undo.RecordObject(_config, "Reset Supported Languages");
                _config.ResetSupportedLanguages();
                EditorUtility.SetDirty(_config);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector(); // Hiển thị các field cơ bản
        }

        private void DrawOperationsTab()
        {
            EditorGUILayout.LabelField("Sync Operations", EditorStyles.boldLabel);
            
            GUI.color = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("Sync From Web Publish (CSV)", GUILayout.Height(30)))
            {
                TriggerSync();
            }
            
            GUI.color = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Local CSV"))
            {
                _config.LoadFromLocalCSV();
            }
            if (GUILayout.Button("Save Local CSV"))
            {
                _config.SaveToLocalCSV();
            }
            EditorGUILayout.EndHorizontal();

            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear All Local Data"))
            {
                if (EditorUtility.DisplayDialog("Cảnh báo", "Bạn có chắc chắn muốn xóa sạch dữ liệu Local (Board, CSV và PlayerPrefs) không?", "Xóa sạch", "Hủy"))
                {
                    _config.ClearAllLocalData();
                }
            }
            GUI.color = Color.white;
        }

        private void DrawDataBoardTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Data Board & Search", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add New Key", GUILayout.Width(120)))
            {
                Undo.RecordObject(_config, "Add Localization Entry");
                _config.LocalDataBoard.Add(new LocalizationEntry { Key = "NEW_KEY_" + _config.LocalDataBoard.Count });
                EditorUtility.SetDirty(_config);
            }
            if (GUILayout.Button("Save To CSV", GUILayout.Width(100)))
            {
                _config.SaveToLocalCSV();
            }
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("Auto Translate Empty", GUILayout.Width(150)))
            {
                AutoTranslateEmptyRows();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Auto Translate Source:", GUILayout.Width(140));
            _sourceLanguage = (LanguageCode)EditorGUILayout.EnumPopup(_sourceLanguage, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _keyWidth = EditorGUILayout.Slider("Key Width", _keyWidth, 50, 500);
            _langWidth = EditorGUILayout.Slider("Lang Width", _langWidth, 50, 500);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetFloat("Loc_KeyWidth", _keyWidth);
                EditorPrefs.SetFloat("Loc_LangWidth", _langWidth);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Tính toán chiều rộng cột dựa trên số lượng ngôn ngữ
            float keyWidth = _keyWidth;
            float langWidth = _langWidth;
            float totalWidth = keyWidth + (_config.SupportedLanguages.Count * langWidth) + 120;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, true, true, GUILayout.Height(500));
            
            EditorGUILayout.BeginVertical(GUILayout.Width(totalWidth));
            
            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Key", EditorStyles.toolbarButton, GUILayout.Width(keyWidth));
            foreach (var lang in _config.SupportedLanguages)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(langWidth));
                EditorGUILayout.LabelField(lang.ToString().ToUpper(), EditorStyles.toolbarButton, GUILayout.Width(langWidth));
                if (lang != _sourceLanguage && GUILayout.Button("Trans All", EditorStyles.miniButton, GUILayout.Width(langWidth)))
                {
                    TranslateColumn(lang.ToString());
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.LabelField("Action", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (_config.LocalDataBoard.Count == 0)
            {
                EditorGUILayout.LabelField("No data in Local Board. Try 'Sync' or 'Load Local CSV' in Operations tab.", EditorStyles.centeredGreyMiniLabel);
            }

            for (int i = 0; i < _config.LocalDataBoard.Count; i++)
            {
                var entry = _config.LocalDataBoard[i];
                
                // Filter logic
                bool match = string.IsNullOrEmpty(_searchFilter) || 
                             entry.Key.Contains(_searchFilter, System.StringComparison.OrdinalIgnoreCase);
                
                if (!match)
                {
                    foreach (var val in entry.Values.Values)
                    {
                        if (val != null && val.Contains(_searchFilter, System.StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }
                    }
                }

                if (!match) continue;

                EditorGUILayout.BeginHorizontal();
                
                // Key cell
                EditorGUI.BeginChangeCheck();
                string newKey = EditorGUILayout.TextField(entry.Key, GUILayout.Width(keyWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_config, "Change Localization Key");
                    entry.Key = newKey;
                    EditorUtility.SetDirty(_config);
                }

                // Language cells
                foreach (var lang in _config.SupportedLanguages)
                {
                    string langStr = lang.ToString();
                    if (!entry.Values.ContainsKey(langStr))
                    {
                        entry.Values[langStr] = "";
                    }

                    // Draw cell with a small translation button
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(langWidth));
                    EditorGUI.BeginChangeCheck();
                    string newVal = EditorGUILayout.TextField(entry.Values[langStr], GUILayout.Width(langWidth - 25));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_config, "Change Localization Value");
                        entry.Values[langStr] = newVal;
                        EditorUtility.SetDirty(_config);
                    }

                    if (GUILayout.Button("G", EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        _ = TranslateCell(entry, langStr);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // Delete & Trans row button
                EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
                GUI.color = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("Tr", EditorStyles.miniButton, GUILayout.Width(30)))
                {
                    TranslateRow(entry);
                }
                GUI.color = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Del", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    if (EditorUtility.DisplayDialog("Delete Key", $"Confirm deleting key: {entry.Key}?", "Delete", "Cancel"))
                    {
                        Undo.RecordObject(_config, "Delete Localization Entry");
                        _config.LocalDataBoard.RemoveAt(i);
                        EditorUtility.SetDirty(_config);
                        i--;
                    }
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndHorizontal();

                // Alternating background
                if (Event.current.type == EventType.Repaint && i % 2 == 0)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    EditorGUI.DrawRect(lastRect, new Color(1, 1, 1, 0.07f));
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private async void TriggerSync()
        {
            bool success = await GoogleSheetsDownloader.DownloadAndCacheFormatCSV(_config);
            if (success)
            {
                Debug.Log("<color=green>Localization: Đồng bộ thành công!</color>");
                AssetDatabase.Refresh();
                _config.LoadFromLocalCSV();
            }
            else
            {
                Debug.LogError("Localization: Đồng bộ thất bại.");
            }
        }

        private async void AutoTranslateEmptyRows()
        {
            if (string.IsNullOrEmpty(_config.GeminiApiKey))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng thiết lập GeminiApiKey trước khi tự động dịch.", "OK");
                return;
            }

            string sourceLangKey = _sourceLanguage.ToString(); 
            var languagesToTranslate = new List<string>();
            foreach (var lang in _config.SupportedLanguages)
            {
                if (lang.ToString() != sourceLangKey)
                    languagesToTranslate.Add(lang.ToString());
            }

            // Thu thập các ô trống theo ngôn ngữ
            var missingTranslations = new Dictionary<string, List<LocalizationEntry>>();
            int totalEmptyCells = 0;

            foreach (var langKey in languagesToTranslate)
            {
                missingTranslations[langKey] = new List<LocalizationEntry>();
                foreach (var entry in _config.LocalDataBoard)
                {
                    if (string.IsNullOrEmpty(entry.Key)) continue;
                    
                    // Có text nguồn mới dịch được
                    if (entry.Values.TryGetValue(sourceLangKey, out string sourceVal) && !string.IsNullOrEmpty(sourceVal))
                    {
                        if (!entry.Values.ContainsKey(langKey) || string.IsNullOrEmpty(entry.Values[langKey]))
                        {
                            missingTranslations[langKey].Add(entry);
                            totalEmptyCells++;
                        }
                    }
                }
            }

            if (totalEmptyCells == 0)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy ô nào còn trống để dịch.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Xác nhận", $"Tìm thấy {totalEmptyCells} ô trống. Hệ thống sẽ dịch theo đợt (Batch 10 dòng) để tránh quá tải API. Bắt đầu?", "Bắt đầu", "Hủy"))
            {
                return;
            }

            int processedCount = 0;
            int batchSize = Mathf.Max(1, _config.GeminiBatchSize);
            
            try
            {
                foreach (var langPair in missingTranslations)
                {
                    string targetLang = langPair.Key;
                    var entries = langPair.Value;

                    for (int i = 0; i < entries.Count; i += batchSize)
                    {
                        var batch = entries.GetRange(i, Math.Min(batchSize, entries.Count - i));
                        
                        processedCount += batch.Count;
                        float progress = (float)processedCount / totalEmptyCells;
                        if (EditorUtility.DisplayCancelableProgressBar("Đang dịch tự động...", $"Đang dịch {targetLang}: {processedCount}/{totalEmptyCells} ô", progress))
                        {
                            return;
                        }

                        // Tạo prompt cho batch
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"Dịch danh sách các chuỗi sau sang tiếng {targetLang}.");
                        sb.AppendLine("Quy tắc: Mỗi chuỗi kết quả trên một dòng, chỉ trả về nội dung đã dịch, giữ nguyên thứ tự và tuyệt đối không thêm số thứ tự hay text dư thừa.");
                        sb.AppendLine("---");
                        foreach (var b in batch) sb.AppendLine(b.Values[sourceLangKey]);

                        string result = await Runtime.Localization.Editor.GeminiTranslatorHelper.RequestGeminiAPI(sb.ToString(), _config.GeminiApiKey, _config.GeminiModel);
                        
                        if (!result.StartsWith("Lỗi:"))
                        {
                            string[] results = result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int j = 0; j < batch.Count; j++)
                            {
                                if (j < results.Length)
                                {
                                    Undo.RecordObject(_config, "Auto Translate");
                                    batch[j].Values[targetLang] = results[j].Trim();
                                }
                            }
                            EditorUtility.SetDirty(_config);
                        }
                        else
                        {
                            Debug.LogError($"Gemini Error tại {targetLang} (Batch {i/batchSize}): {result}");
                            if (result.Contains("503") || result.Contains("429"))
                            {
                                bool retry = EditorUtility.DisplayDialog("Lỗi API Overloaded", "Server Gemini đang quá tải. Bạn muốn thử lại lượt này hay dừng lại?", "Thử lại", "Dừng");
                                if (retry)
                                {
                                    i -= batchSize; // Reset i để thử lại batch này
                                    processedCount -= batch.Count;
                                    await UniTask.Delay(5000); // Đợi 5s
                                    continue;
                                }
                                return;
                            }
                        }

                        // Nghỉ một chút giữa các request để giảm áp lực API (Tăng lên 3s để tránh 429)
                        await UniTask.Delay(3000);
                    }
                }
                
                EditorUtility.DisplayDialog("Hoàn tất", $"Đã hoàn thành dịch {processedCount} ô.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }
        }

        private async void TranslateRow(LocalizationEntry entry)
        {
            string sourceLang = _sourceLanguage.ToString();
            if (!entry.Values.TryGetValue(sourceLang, out string sourceVal) || string.IsNullOrEmpty(sourceVal)) return;

            foreach (var lang in _config.SupportedLanguages)
            {
                string langKey = lang.ToString();
                if (langKey == sourceLang) continue;

                if (!entry.Values.ContainsKey(langKey) || string.IsNullOrEmpty(entry.Values[langKey]))
                {
                    await TranslateCell(entry, langKey);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private async void TranslateColumn(string targetLang)
        {
            string sourceLang = _sourceLanguage.ToString();
            var entriesToTranslate = new List<LocalizationEntry>();

            foreach (var entry in _config.LocalDataBoard)
            {
                if (entry.Values.TryGetValue(sourceLang, out string sourceVal) && !string.IsNullOrEmpty(sourceVal))
                {
                    if (!entry.Values.ContainsKey(targetLang) || string.IsNullOrEmpty(entry.Values[targetLang]))
                    {
                        entriesToTranslate.Add(entry);
                    }
                }
            }

            if (entriesToTranslate.Count == 0) return;

            int batchSize = Mathf.Max(1, _config.GeminiBatchSize);
            for (int i = 0; i < entriesToTranslate.Count; i += batchSize)
            {
                var batch = entriesToTranslate.GetRange(i, Math.Min(batchSize, entriesToTranslate.Count - i));
                
                if (EditorUtility.DisplayCancelableProgressBar("Translating Column...", $"Translating {targetLang}: {i}/{entriesToTranslate.Count}", (float)i/entriesToTranslate.Count))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Dịch danh sách các chuỗi sau sang tiếng {targetLang}.");
                sb.AppendLine("Quy tắc: Mỗi chuỗi kết quả trên một dòng, đúng thứ tự, không text dư thừa.");
                sb.AppendLine("---");
                foreach (var b in batch) sb.AppendLine(b.Values[sourceLang]);

                string result = await Runtime.Localization.Editor.GeminiTranslatorHelper.RequestGeminiAPI(sb.ToString(), _config.GeminiApiKey, _config.GeminiModel);
                if (!result.StartsWith("Lỗi:"))
                {
                    string[] results = result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < batch.Count; j++)
                    {
                        if (j < results.Length)
                        {
                            Undo.RecordObject(_config, "Auto Translate");
                            batch[j].Values[targetLang] = results[j].Trim();
                        }
                    }
                }
                await UniTask.Delay(3000);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        private async System.Threading.Tasks.Task TranslateCell(LocalizationEntry entry, string targetLang)
        {
            string sourceLang = _sourceLanguage.ToString();
            if (!entry.Values.TryGetValue(sourceLang, out string sourceText) || string.IsNullOrEmpty(sourceText))
            {
                Debug.LogWarning($"Localization: Không có dữ liệu nguồn ({sourceLang}) để dịch cho key '{entry.Key}'.");
                return;
            }

            Debug.Log($"Localization: Đang dịch '{entry.Key}' ({sourceLang} -> {targetLang})...");

            string prompt = $"Dịch đoạn văn bản sau sang tiếng {targetLang}:\n\n\"{sourceText}\"\n\nChú ý: Chỉ trả về nội dung đã dịch, không in thêm thông tin dư thừa nào khác, không kèm ngoặc kép bọc ngoài.";
            string translatedText = await Runtime.Localization.Editor.GeminiTranslatorHelper.RequestGeminiAPI(prompt, _config.GeminiApiKey, _config.GeminiModel);

            if (!translatedText.StartsWith("Lỗi:"))
            {
                Undo.RecordObject(_config, "Auto Translate Cell");
                entry.Values[targetLang] = translatedText.Trim();
                EditorUtility.SetDirty(_config);
                Debug.Log($"Localization: Dịch thành công '{entry.Key}' sang {targetLang}!");
                Repaint(); // Cập nhật lại giao diện Inspector ngay lập tức
            }
            else
            {
                Debug.LogError($"Localization Error khi dịch '{entry.Key}': {translatedText}");
            }
        }

    }
}
