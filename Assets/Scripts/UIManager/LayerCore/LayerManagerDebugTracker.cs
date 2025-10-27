using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace UIManager.Editor
{
    /// <summary>
    /// Tracker để thu thập thông tin debug cho LayerManager
    /// Chỉ hoạt động trong Editor và không ảnh hưởng hiệu năng runtime
    /// </summary>
    public class LayerManagerDebugTracker : MonoSingleton<LayerManagerDebugTracker>
    {
        [SerializeField] private LayerManager layerManager;
        // Thông tin lịch sử (giới hạn để tránh memory leak)
        private const int MAX_HISTORY = 10;

        private List<GroupActionInfo> _groupHistory = new List<GroupActionInfo>();
        private Dictionary<LayerType, LayerActionInfo> _layerHistory = new Dictionary<LayerType, LayerActionInfo>();

        // Thông tin hiện tại để so sánh
        private HashSet<LayerType> _previousShowingLayers = new HashSet<LayerType>();
        private List<ShowLayerGroupData> _previousGroups = new List<ShowLayerGroupData>();

        private void OnValidate()
        {
            if (layerManager == null)
            {
                layerManager = FindObjectOfType<LayerManager>();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            
            if (layerManager == null) return;

            // Track group changes
            TrackGroupChanges(layerManager);

            // Track layer changes
            TrackLayerChanges(layerManager);
        }

        private void TrackGroupChanges(LayerManager layerManager)
        {
#if UNITY_EDITOR
            var currentGroups = layerManager.GetShowingLayerGroups()?.ToList() ?? new List<ShowLayerGroupData>();
#else
            var currentGroups = new List<ShowLayerGroupData>();
#endif

            // Phát hiện group mới được thêm
            foreach (var currentGroup in currentGroups)
            {
                if (!_previousGroups.Any(g => g.ID == currentGroup.ID))
                {
                    AddGroupHistory("Show", currentGroup);
                }
            }

            // Phát hiện group bị đóng
            foreach (var previousGroup in _previousGroups)
            {
                if (!currentGroups.Any(g => g.ID == previousGroup.ID))
                {
                    AddGroupHistory("Close", previousGroup);
                }
            }

            _previousGroups = new List<ShowLayerGroupData>(currentGroups);
        }

        private void TrackLayerChanges(LayerManager layerManager)
        {
#if UNITY_EDITOR
            var currentShowingLayers = layerManager.GetShowingLayerTypes();
#else
            var currentShowingLayers = new HashSet<LayerType>();
#endif

            // Phát hiện layer mới được hiển thị
            foreach (var layerType in currentShowingLayers)
            {
                if (!_previousShowingLayers.Contains(layerType))
                {
                    AddLayerHistory(layerType, "Show", GetLayerSortingOrder(layerManager, layerType));
                }
                else
                {
                    // Cập nhật sorting order nếu thay đổi
                    var currentOrder = GetLayerSortingOrder(layerManager, layerType);
                    if (_layerHistory.TryGetValue(layerType, out var layerInfo))
                    {
                        if (layerInfo.CurrentSortingOrder != currentOrder)
                        {
                            AddLayerHistory(layerType, "Hide", currentOrder);
                        }
                    }
                }
            }

            // Phát hiện layer bị ẩn
            foreach (var layerType in _previousShowingLayers)
            {
                if (!currentShowingLayers.Contains(layerType))
                {
                    // Lấy sorting order thực tế từ layer base thay vì hard-code -10000
                    //var sortingOrder = GetLayerSortingOrder(layerManager, layerType);
                    AddLayerHistory(layerType, "Close", -10000);
                }
            }

            _previousShowingLayers = new HashSet<LayerType>(currentShowingLayers);
        }

        private void AddGroupHistory(string action, ShowLayerGroupData groupData)
        {
            var groupInfo = new GroupActionInfo
            {
                FrameCount = Time.frameCount,
                Action = action,
                GroupType = groupData.LayerGroupType,
                LayerTypes = new List<LayerType>(groupData.LayerTypes),
                GroupId = groupData.ID
            };

            _groupHistory.Add(groupInfo);

            // Giới hạn số lượng lịch sử
            if (_groupHistory.Count > MAX_HISTORY)
            {
                _groupHistory.RemoveRange(0, _groupHistory.Count - MAX_HISTORY);
            }
        }

        private void AddLayerHistory(LayerType layerType, string action, int sortingOrder)
        {
            if (!_layerHistory.TryGetValue(layerType, out var layerInfo))
            {
                layerInfo = new LayerActionInfo
                {
                    SortingHistory = new List<int>()
                };
            }

            layerInfo.CurrentSortingOrder = sortingOrder;
            layerInfo.LastAction = action;
            layerInfo.LastFrameCount = Time.frameCount;

            if (layerInfo.SortingHistory.Count == 0 || layerInfo.SortingHistory.Last() != sortingOrder)
            {
                layerInfo.SortingHistory.Add(sortingOrder);

                // Giới hạn lịch sử sorting order
                if (layerInfo.SortingHistory.Count > 10)
                {
                    layerInfo.SortingHistory.RemoveRange(0, layerInfo.SortingHistory.Count - 10);
                }
            }

            _layerHistory[layerType] = layerInfo;
        }

        private int GetLayerSortingOrder(LayerManager layerManager, LayerType layerType)
        {
#if UNITY_EDITOR
            var createdLayerBases = layerManager.GetCreatedLayerBases();
            if (createdLayerBases != null && createdLayerBases.TryGetValue(layerType, out var layerBase))
            {
                return layerBase.GetSortingOrder();
            }
            return 0;
#else
            return 0;
#endif
        }

        // Public methods để Editor Window truy cập
        public List<GroupActionInfo> GetGroupHistory()
        {
            return new List<GroupActionInfo>(_groupHistory);
        }

        public Dictionary<LayerType, LayerActionInfo> GetLayerHistory()
        {
            return new Dictionary<LayerType, LayerActionInfo>(_layerHistory);
        }

        public void ClearHistory()
        {
            _groupHistory.Clear();
            _layerHistory.Clear();
            _previousShowingLayers.Clear();
            _previousGroups.Clear();
        }

        // Thông tin cho Editor Window
        [Serializable]
        public struct GroupActionInfo
        {
            public int FrameCount;
            public string Action;
            public LayerGroupType GroupType;
            public List<LayerType> LayerTypes;
            public int GroupId;
        }

        [Serializable]
        public struct LayerActionInfo
        {
            public int CurrentSortingOrder;
            public string LastAction;
            public int LastFrameCount;
            public List<int> SortingHistory;
        }
    }
}
