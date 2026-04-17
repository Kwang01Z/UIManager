using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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

        private void OnEnable()
        {
            _config = (LocalizationConfig)target;
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
            EditorGUILayout.LabelField("Data Board & Search", EditorStyles.boldLabel);
            
            _searchFilter = EditorGUILayout.TextField("Search Key/Value", _searchFilter);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(400));
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Key", GUILayout.Width(150));
            EditorGUILayout.LabelField("Values (JSON-like browse)");
            EditorGUILayout.EndHorizontal();

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
                        if (val.Contains(_searchFilter, System.StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }
                    }
                }

                if (!match) continue;

                EditorGUILayout.BeginHorizontal();
                entry.Key = EditorGUILayout.TextField(entry.Key, GUILayout.Width(150));
                
                if (GUILayout.Button("Edit Values", GUILayout.Width(100)))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var lang in _config.SupportedLanguages)
                    {
                        string langStr = lang.ToString();
                        _config.LocalDataBoard[i].Values.TryGetValue(langStr, out string currentVal);
                        // Thực tế Editor phức tạp hơn, đây là bản đơn giản để xem
                    }
                    Debug.Log("Sửa trực tiếp trong bảng List bên dưới nếu cần chi tiết hơn.");
                }
                
                // Hiển thị tóm tắt nội dung
                string preview = "";
                foreach(var kvp in entry.Values) preview += $"[{kvp.Key}:{kvp.Value}] ";
                EditorGUILayout.LabelField(preview, EditorStyles.miniLabel);
                
                EditorGUILayout.EndHorizontal();
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

    }
}
