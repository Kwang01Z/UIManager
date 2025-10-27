using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UIManager.Editor
{
    public class LayerManagerDebugger : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _lastRefreshTime = 0f;
        private const float REFRESH_INTERVAL = 0.5f; // Refresh mỗi 0.5 giây

        private List<LayerGroupInfo> _groupHistory = new List<LayerGroupInfo>();
        private Dictionary<LayerType, LayerInfo> _layerHistory = new Dictionary<LayerType, LayerInfo>();

        private List<LayerType> _selectedLayerTypes = new List<LayerType>();
        private LayerGroupType _selectedGroupType = default(LayerGroupType);
        private string _searchText = "";
        private int _selectedIndex = 0;

        [MenuItem("Window/UI Manager/Layer Manager Debugger")]
        public static void ShowWindow()
        {
            GetWindow<LayerManagerDebugger>("Layer Manager Debugger");
        }

        private void OnEnable()
        {
            _groupHistory.Clear();
            _layerHistory.Clear();
        }

        private void OnDisable()
        {
            _groupHistory.Clear();
            _layerHistory.Clear();
        }

        private void Update()
        {
            if (_autoRefresh && Time.realtimeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                Repaint();
                _lastRefreshTime = Time.realtimeSinceStartup;
            }
        }

        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                DrawRuntimeInfo();
            }
            else
            {
                EditorGUILayout.HelpBox("Layer Manager Debugger chỉ hoạt động khi đang chạy game.", MessageType.Info);
            }
        }

        private void DrawRuntimeInfo()
        {
            EditorGUILayout.BeginHorizontal();
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh);
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                Repaint();
            }
            if (GUILayout.Button("Clear History", GUILayout.Width(100)))
            {
                var tracker = FindObjectOfType<LayerManagerDebugTracker>();
                tracker?.ClearHistory();
            }
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawControls();
            EditorGUILayout.Space(20);
            DrawCurrentState();
            EditorGUILayout.Space(20);
            DrawGroupHistory();
            EditorGUILayout.Space(20);
            DrawLayerHistory();

            EditorGUILayout.EndScrollView();
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("=== CONTROLS ===", EditorStyles.boldLabel);

            DrawShowGroupControls();
            EditorGUILayout.Space(10);
            DrawCloseGroupControls();

            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentState()
        {
            EditorGUILayout.LabelField("=== TRẠNG THÁI HIỆN TẠI ===", EditorStyles.boldLabel);

            var layerManager = FindObjectOfType<LayerManager>();
            if (layerManager == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy LayerManager trong scene.", MessageType.Warning);
                return;
            }

            // Hiển thị thông tin LayerManager
            EditorGUILayout.LabelField($"Is Showing: {layerManager.IsShowing}");
            EditorGUILayout.LabelField($"Space Between Layer: {layerManager.spaceBetweenLayer}");
            EditorGUILayout.LabelField($"Limit Layer: {LayerManager.LimitLayer}");

            EditorGUILayout.Space(10);

            // Hiển thị các layer đang hiển thị
            EditorGUILayout.LabelField("Layers đang hiển thị:", EditorStyles.boldLabel);
#if UNITY_EDITOR
            var showingLayerTypes = layerManager.GetShowingLayerTypes();
            if (showingLayerTypes != null && showingLayerTypes.Count > 0)
            {
                foreach (var layerType in showingLayerTypes.OrderByDescending(t =>
                {
                    var layerBase = GetLayerBaseFromManager(layerManager, t);
                    return layerBase?.GetSortingOrder() ?? 0;
                }))
#else
            if (false)
            {
                foreach (var layerType in new HashSet<LayerType>())
#endif
                {
                    var layerBase = GetLayerBaseFromManager(layerManager, layerType);
                    var sortingOrder = layerBase?.GetSortingOrder() ?? 0;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {layerType}", GUILayout.Width(150));
                    EditorGUILayout.LabelField($"Sorting: {sortingOrder}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Active: {layerBase?.gameObject.activeInHierarchy ?? false}");
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Không có layer nào đang hiển thị.");
            }

            EditorGUILayout.Space(10);

            // Hiển thị stack các group
            EditorGUILayout.LabelField("Stack Groups:", EditorStyles.boldLabel);
#if UNITY_EDITOR
            var showingGroups = layerManager.GetShowingLayerGroups();
            if (showingGroups != null && showingGroups.Count > 0)
            {
                var groups = showingGroups.ToList();
                for (int i = groups.Count - 1; i >= 0; i--)
                {
                    var group = groups[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• Group {i + 1}: {group.LayerGroupType}", GUILayout.Width(200));
                    EditorGUILayout.LabelField($"ID: {group.ID}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Layers: {string.Join(", ", group.LayerTypes)}");
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Stack trống.");
            }
#else
            EditorGUILayout.LabelField("Stack trống.");
#endif
        }

        private void DrawGroupHistory()
        {
            EditorGUILayout.LabelField("=== LỊCH SỬ GROUPS ===", EditorStyles.boldLabel);

            var tracker = FindObjectOfType<LayerManagerDebugTracker>();
            if (tracker == null)
            {
                EditorGUILayout.LabelField("DebugTracker chưa được khởi tạo.");
                return;
            }

            var groupHistory = tracker.GetGroupHistory();
            if (groupHistory.Count == 0)
            {
                EditorGUILayout.LabelField("Chưa có lịch sử groups.");
                return;
            }

            // Hiển thị lịch sử groups
            for (int i = groupHistory.Count - 1; i >= 0; i--)
            {
                var groupInfo = groupHistory[i];

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Frame {groupInfo.FrameCount}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Action: {groupInfo.Action}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Type: {groupInfo.GroupType}", GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Layers: {string.Join(", ", groupInfo.LayerTypes)}");
                EditorGUILayout.LabelField($"ID: {groupInfo.GroupId}");

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawLayerHistory()
        {
            EditorGUILayout.LabelField("=== LỊCH SỬ LAYERS ===", EditorStyles.boldLabel);

            var tracker = FindObjectOfType<LayerManagerDebugTracker>();
            if (tracker == null)
            {
                EditorGUILayout.LabelField("DebugTracker chưa được khởi tạo.");
                return;
            }

            var layerHistory = tracker.GetLayerHistory();
            if (layerHistory.Count == 0)
            {
                EditorGUILayout.LabelField("Chưa có lịch sử layers.");
                return;
            }

            // Hiển thị lịch sử từng layer
            foreach (var kvp in layerHistory.OrderByDescending(k => k.Value.LastFrameCount))
            {
                var layerType = kvp.Key;
                var layerInfo = kvp.Value;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Layer: {layerType}", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Current Sorting: {layerInfo.CurrentSortingOrder}", GUILayout.Width(150));
                EditorGUILayout.LabelField($"Last Action: {layerInfo.LastAction}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Frame: {layerInfo.LastFrameCount}", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                if (layerInfo.SortingHistory.Count > 0)
                {
                    EditorGUILayout.LabelField($"Sorting History: {string.Join(" → ", layerInfo.SortingHistory.Select(s => s.ToString()))}");
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawShowGroupControls()
        {
            EditorGUILayout.LabelField("=== SHOW GROUP LAYER ===", EditorStyles.boldLabel);

            var layerManager = FindObjectOfType<LayerManager>();
            if (layerManager == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy LayerManager trong scene.", MessageType.Warning);
                return;
            }

            _selectedGroupType = (LayerGroupType)EditorGUILayout.EnumPopup("Layer Group Type:", _selectedGroupType);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Add Layer Types:");

            _searchText = EditorGUILayout.TextField("Search Layer Type:", _searchText);

            var allLayerTypes = Enum.GetValues(typeof(LayerType)).Cast<LayerType>().ToList();
            var filteredLayerTypes = allLayerTypes.Where(lt => string.IsNullOrEmpty(_searchText) || lt.ToString().ToLower().Contains(_searchText.ToLower())).ToList();
            var names = filteredLayerTypes.Select(lt => lt.ToString()).ToArray();

            if (names.Length > 0)
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, names.Length - 1);
                _selectedIndex = EditorGUILayout.Popup("Select Layer Type to Add:", _selectedIndex, names);

                if (filteredLayerTypes.Count > 0)
                {
                    var selectedType = filteredLayerTypes[_selectedIndex];
                    if (filteredLayerTypes.Count == 1)
                    {
                        if (GUILayout.Button($"Auto Add {selectedType}"))
                        {
                            if (!_selectedLayerTypes.Contains(selectedType))
                            {
                                _selectedLayerTypes.Add(selectedType);
                            }
                        }
                    }
                    else
                    {
                        if (GUILayout.Button($"Add {selectedType}"))
                        {
                            if (!_selectedLayerTypes.Contains(selectedType))
                            {
                                _selectedLayerTypes.Add(selectedType);
                            }
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No matching LayerTypes found.");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Selected Layer Types:");
            for (int i = 0; i < _selectedLayerTypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _selectedLayerTypes[i] = (LayerType)EditorGUILayout.EnumPopup("Layer Type:", _selectedLayerTypes[i]);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    _selectedLayerTypes.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Show Group Layer Async"))
            {
                if (_selectedLayerTypes.Count > 0)
                {
                    var showData = LayerGroupBuilder.Build(_selectedGroupType, _selectedLayerTypes);
                    layerManager.ShowGroupLayerAsync(showData);
                }
                else
                {
                    Debug.LogWarning("No LayerTypes selected.");
                }
            }
        }

        private void DrawCloseGroupControls()
        {
            EditorGUILayout.LabelField("=== CLOSE GROUP ===", EditorStyles.boldLabel);

            var layerManager = FindObjectOfType<LayerManager>();
            if (layerManager == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy LayerManager trong scene.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Close Last Layer Group"))
            {
                layerManager.CloseLastLayerGroup();
            }
        }

        // Helper methods để lấy thông tin từ LayerManager
        private LayerBase GetLayerBaseFromManager(LayerManager manager, LayerType layerType)
        {
#if UNITY_EDITOR
            var createdLayerBases = manager.GetCreatedLayerBases();
            if (createdLayerBases != null && createdLayerBases.TryGetValue(layerType, out var layerBase))
            {
                return layerBase;
            }
            return null;
#else
            return null;
#endif
            return null; // Fallback để đảm bảo luôn có return value
        }

        // Các struct để lưu trữ thông tin
        private struct LayerGroupInfo
        {
            public int FrameCount;
            public string Action;
            public LayerGroupType GroupType;
            public List<LayerType> LayerTypes;
            public int GroupId;
        }

        private struct LayerInfo
        {
            public int CurrentSortingOrder;
            public string LastAction;
            public int LastFrameCount;
            public List<int> SortingHistory;
        }
    }
}
