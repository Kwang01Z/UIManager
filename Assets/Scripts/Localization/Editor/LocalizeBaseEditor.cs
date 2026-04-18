using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Runtime.Localization
{
    [CustomEditor(typeof(LocalizeBase), true)]
    [CanEditMultipleObjects]
    public class LocalizeBaseEditor : UnityEditor.Editor
    {
        private string _searchQuery = "";
        private List<string> _filteredKeys = new List<string>();
        private bool _showSearch = false;
        private Vector2 _scrollPos;
        private LocalizationConfig _config;

        private void OnEnable()
        {
            _config = LocalizationManager.Config;
            if (_config != null && _config.LocalDataBoard.Count == 0)
            {
                _config.LoadFromLocalCSV();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            LocalizeBase targetComponent = (LocalizeBase)target;

            // Header info
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Language Source Asset", _config, typeof(LocalizationConfig), false);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Target Text (Specific for LocalizeTMP)
            if (targetComponent is LocalizeTMP)
            {
                SerializedProperty tmpProp = serializedObject.FindProperty("_tmpText");
                if (tmpProp != null)
                {
                    EditorGUILayout.PropertyField(tmpProp, new GUIContent("Target Text"));
                }
            }

            // Hiển thị Key field mặc định
            SerializedProperty keyProp = serializedObject.FindProperty("Key");
            EditorGUILayout.PropertyField(keyProp);

            if (_config == null)
            {
                _config = LocalizationManager.Config;
            }

            if (_config == null)
            {
                EditorGUILayout.HelpBox("LocalizationConfig not found! Vui lòng tạo LocalizationConfig hoặc kiểm tra lại tên file.", MessageType.Error);
                return;
            }

            // Kiểm tra xem có nằm trong folder 'Resources' không (cần cho Runtime)
            string assetPath = AssetDatabase.GetAssetPath(_config).Replace("\\", "/");
            if (!assetPath.Contains("/Resources/"))
            {
                EditorGUILayout.HelpBox("Cảnh báo: LocalizationConfig đang nằm ngoài folder 'Resources'. Hệ thống sẽ không hoạt động khi Build game. Hãy rename folder 'Resource' thành 'Resources'!", MessageType.Warning);
            }

            EditorGUILayout.Space();
            
            // Search Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            Color defaultColor = GUI.color;
            GUI.color = new Color(0.7f, 0.9f, 1f);
            EditorGUILayout.LabelField("Search:", EditorStyles.boldLabel);
            GUI.color = defaultColor;

            EditorGUI.BeginChangeCheck();
            _searchQuery = EditorGUILayout.TextField(_searchQuery);
            if (EditorGUI.EndChangeCheck() || (_filteredKeys.Count == 0 && !string.IsNullOrEmpty(_searchQuery)))
            {
                UpdateSearch();
            }

            if (!string.IsNullOrEmpty(_searchQuery))
            {
                GUI.color = new Color(0.3f, 0.6f, 1f);
                EditorGUILayout.LabelField($"Found: {_filteredKeys.Count} terms", EditorStyles.boldLabel);
                GUI.color = Color.white;
                
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(250));
                
                foreach (var key in _filteredKeys)
                {
                    if (GUILayout.Button(key, EditorStyles.miniButtonLeft, GUILayout.Height(20)))
                    {
                        Undo.RecordObject(targetComponent, "Select Localization Key");
                        targetComponent.Key = key;
                        _searchQuery = "";
                        GUI.FocusControl(null);
                        EditorUtility.SetDirty(targetComponent);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            // Preview Section
            if (!string.IsNullOrEmpty(targetComponent.Key))
            {
                var entry = _config.LocalDataBoard.FirstOrDefault(e => e.Key == targetComponent.Key);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
                GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;

                if (entry != null)
                {
                    foreach (var lang in _config.SupportedLanguages)
                    {
                        string langStr = lang.ToString();
                        entry.Values.TryGetValue(langStr, out string val);
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(lang.ToString().ToUpper() + ":", GUILayout.Width(60));
                        EditorGUILayout.LabelField(val ?? "-", EditorStyles.wordWrappedLabel);
                        EditorGUILayout.EndHorizontal();
                        
                        if (lang != _config.SupportedLanguages.Last())
                        {
                            Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
                            lineRect.height = 1;
                            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                            EditorGUILayout.Space(2);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"Key '{targetComponent.Key}' not found.", EditorStyles.centeredGreyMiniLabel);
                }
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Force Update Component"))
                {
                    targetComponent.OnLocalize();
                }
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                if (!Application.isPlaying)
                {
                    // Update preview if needed
                }
            }
        }

        private void UpdateSearch()
        {
            _filteredKeys.Clear();
            if (string.IsNullOrEmpty(_searchQuery)) return;

            string query = _searchQuery.ToLower();
            foreach (var entry in _config.LocalDataBoard)
            {
                bool match = entry.Key.ToLower().Contains(query);
                if (!match)
                {
                    foreach (var val in entry.Values.Values)
                    {
                        if (val != null && val.ToLower().Contains(query))
                        {
                            match = true;
                            break;
                        }
                    }
                }

                if (match)
                {
                    _filteredKeys.Add(entry.Key);
                    if (_filteredKeys.Count > 100) break; // Limit search results
                }
            }
        }
    }
}
