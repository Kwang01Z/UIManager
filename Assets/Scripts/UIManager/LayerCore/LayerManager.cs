using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public partial class LayerManager : MonoSingleton<LayerManager>
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool hasLayerRoot = true;
    [SerializeField] private RectTransform layerParent;
    [SerializeField] private LayerReferenceSO layerReferenceSO;
    [SerializeField] private List<LayerReferenceData> layerPreload;
    public static int LimitLayer = 64;
    public int spaceBetweenLayer = 100;
    private Dictionary<LayerType, LayerBase> _createdLayerBases = new(LimitLayer);
    private HashSet<LayerType> _showingLayerTypes = new(LimitLayer);
    private Stack<ShowLayerGroupData> _showingLayerGroups = new(LimitLayer);

    private void Reset()
    {
        if (!layerParent) layerParent = transform as RectTransform;
    }

    public void SetActiveLayerAll(bool active)
    {
        if(canvasGroup) canvasGroup.SetActive(true);
    }

    protected override void Awake()
    {
        base.Awake();
        PreloadLayer();
    }

    private void PreloadLayer()
    {
        foreach (var layerReferenceData in layerPreload)
        {
            _createdLayerBases.TryAdd(layerReferenceData.layerType, layerReferenceData.layerBase);
        }
    }

    private readonly Queue<ShowLayerGroupData> _showDataQueue = new(8);
    public bool IsShowing { get; private set; }

    public async void ShowGroupLayerAsync(ShowLayerGroupData showData)
    {
        await UniTask.SwitchToMainThread();
        _showDataQueue.Enqueue(showData);

        if (IsShowing)
        {
            Debug.Log($"[TryShowGroupLayer] [Frame:{Time.frameCount}] {string.Join("|", showData.LayerTypes)} - {showData.LayerGroupType} enqueued (Queue count: {_showDataQueue.Count})");
            return;
        }

        IsShowing = true;
        try
        {
            while (_showDataQueue.Count > 0)
            {
                var currentData = _showDataQueue.Dequeue();
                await InternalProcessShowGroupAsync(currentData);
            }
        }
        finally
        {
            IsShowing = false;
        }
    }

    private async UniTask InternalProcessShowGroupAsync(ShowLayerGroupData showData)
    {
        Debug.Log($"[ShowGroupLayer] [Frame:{Time.frameCount}] {string.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}");

        var result = InitLayerGroup(showData);
        await UniTask.NextFrame();
        
        showData.OnInitData?.Invoke(result);
        await UniTask.NextFrame();
        
        HideLayerRequired(showData);
        SetSortingLayer(result);
        
        if (showData.AddToStack)
        {
            _showingLayerGroups.Push(showData);
            _showingLayerTypes.UnionWith(showData.LayerTypes);
        }
        else if (!showData.FixedLayer)
        {
            _layerNotInStack.AddRange(showData.LayerTypes);
        }
        
        if (showData.DisplayImmediately) result.ShowGroupAsync();

        await UniTask.NextFrame();
        showData.OnShowComplete?.Invoke(result);
    }
    private readonly List<LayerType> _layerNotInStack = new();

    public void CloseLastLayerGroup()
    {
        int initialCount = _showingLayerGroups.Count;
        Debug.Log($"[CloseLastLayerGroup] [Frame:{Time.frameCount}] Current count: {initialCount}");
        
        if (initialCount == 0) 
        {
            Debug.LogWarning("[CloseLastLayerGroup] No groups to close.");
            return;
        }
        
        if (initialCount <= 1 && hasLayerRoot) 
        {
            Debug.Log("[CloseLastLayerGroup] Cannot close root layer group.");
            return;
        }

        var lastGroup = _showingLayerGroups.Pop();
        Debug.Log($"[CloseLastLayerGroup] Closing Group ID:{lastGroup.ID} - Types: {string.Join("|", lastGroup.LayerTypes)} - GroupType: {lastGroup.LayerGroupType}");

        // Cập nhật _showingLayerTypes để loại bỏ các layer đã đóng
        foreach (var layerType in lastGroup.LayerTypes)
        {
            var layerBase = GetLayerBase(layerType);
            if (!layerBase) 
            {
                Debug.LogWarning($"[CloseLastLayerGroup] LayerBase not found for {layerType}");
                continue;
            }
            
            Debug.Log($"[CloseLastLayerGroup] Closing Layer: {layerType}");
            layerBase.CloseLayerAsync();
            
            if (!layerBase.IsActive()) 
            {
                _showingLayerTypes.Remove(layerType);
                Debug.Log($"[CloseLastLayerGroup] Removed {layerType} from showing types");
            }
        }

        // Restore previous groups
        Debug.Log($"[CloseLastLayerGroup] Restoring background layers. Remaining groups: {_showingLayerGroups.Count}");
        foreach (var showLayerGroupData in _showingLayerGroups)
        {
            Debug.Log($"[CloseLastLayerGroup] Restoring Group: {string.Join("|", showLayerGroupData.LayerTypes)}");
            foreach (var layerType in showLayerGroupData.LayerTypes)
            {
                var layerBase = GetLayerBase(layerType);
                if(layerBase) 
                {
                    layerBase.ShowLayerWithoutEvent();
                }
            }
            if(showLayerGroupData.LayerGroupType == LayerGroupType.FullScreen || showLayerGroupData.LayerGroupType == LayerGroupType.Root) 
            {
                Debug.Log($"[CloseLastLayerGroup] Stop restoration at {showLayerGroupData.LayerGroupType} group");
                break;
            }
        }

        OnCloseLayerGroup?.InvokeAsync(lastGroup);
        Debug.Log($"[CloseLastLayerGroup] [Frame:{Time.frameCount}] Completed.");
    }
    public static ActionSealed<ShowLayerGroupData> OnCloseLayerGroup = new();

    public void CloseAllLayerGroups()
    {
        while (_showingLayerGroups.Count > 1)
        {
            CloseLastLayerGroup();
        }
    }

    private void SetSortingLayer(LayerGroup layerGroup)
    {
        int bestOrder = GetBestLayerSorting(layerGroup) + spaceBetweenLayer;
        layerGroup.SetSortOrder(bestOrder);
    }

    private int GetBestLayerSorting(LayerGroup layerGroup)
    {
        int bestLayerSorting = 0;
        // Direct iteration without temp collection
        foreach (var layerType in _showingLayerTypes)
        {
            if (layerGroup.LayerTypes.Contains(layerType)) continue;
            var layerBase = GetLayerBase(layerType);
            if (!layerBase) continue;
            bestLayerSorting = Mathf.Max(bestLayerSorting, layerBase.GetSortingOrder());
        }

        return bestLayerSorting / spaceBetweenLayer * spaceBetweenLayer;
    }

    private LayerGroup InitLayerGroup(ShowLayerGroupData showData)
    {
        var layerGroup = new LayerGroup();
        foreach (var layerType in showData.LayerTypes)
        {
            var layerBase = InitLayerBase(layerType);
            if (!layerBase) continue;

            layerGroup.AddLayer(layerType, layerBase);
        }

        return layerGroup;
    }

    private LayerBase InitLayerBase(LayerType layerType)
    {
        var layerBase = GetLayerBase(layerType);
        if (layerBase) return layerBase;
        layerBase = layerReferenceSO.GetLayerBase(layerType);
        if (!layerBase) return null;
        var layerBaseGo = Instantiate(layerBase, layerParent);
        _createdLayerBases.Add(layerType, layerBaseGo);
        return layerBaseGo;
    }

    private LayerBase GetLayerBase(LayerType layerType)
    {
        if (_createdLayerBases.TryGetValue(layerType, out var layerBase)) return layerBase;
        return null;
    }

    private void CloseNotInStackAll()
    {
        foreach (var layerType in _layerNotInStack)
        {
            var layerBase = GetLayerBase(layerType);
            if (!layerBase) continue;
            layerBase.CloseLayerAsync();
        }
        _layerNotInStack.Clear();
    }

    private void HideLayerRequired(ShowLayerGroupData showData)
    {
        CloseNotInStackAll();
        if (showData.CloseAllOtherLayer)
        {
            CloseAllLayerExist(showData);
            return;
        }

        if (showData.CloseOtherLayerOver)
        {
            CloseOtherLayerOver(showData);
        }

        if (showData.HideAllOtherLayer)
        {
            HideAllLayerExist(showData);
        }

        if (showData.CloseAllPopup)
        {
            CloseAllPopupExist(showData);
        }
    }

    private readonly HashSet<LayerType> _overLayerTypeTotals = new(LimitLayer);
    private readonly HashSet<LayerType> _overLayerTypes = new(LimitLayer);
    private void CloseOtherLayerOver(ShowLayerGroupData showData)
    {
        var groupLayerProjectId = GetGroupLayerProjectId(showData);
        if (groupLayerProjectId == -1) return;
        _overLayerTypeTotals.Clear();
        while (_showingLayerGroups.Count > 0 && _showingLayerGroups.Peek().ID != groupLayerProjectId)
        {
            _overLayerTypeTotals.UnionWith(_showingLayerGroups.Pop().LayerTypes);
        }

        if (_showingLayerGroups.Count > 0) _overLayerTypeTotals.UnionWith(_showingLayerGroups.Pop().LayerTypes);
        _overLayerTypes.Clear();
        foreach (var layerType in _overLayerTypeTotals)
        {
            if (!showData.LayerTypes.Contains(layerType))
            {
                _overLayerTypes.Add(layerType);
            }
        }
        if (_overLayerTypes.Count == 0) return;
        foreach (var overLayerType in _overLayerTypes)
        {
            CloseLayerAsync(overLayerType);
        }

        _showingLayerTypes.ExceptWith(_overLayerTypes);
    }

    private int GetGroupLayerProjectId(ShowLayerGroupData showData)
    {
        foreach (var showLayerGroupData in _showingLayerGroups)
        {
            if (showLayerGroupData.ID == showData.ID) return showData.ID;
            if (showLayerGroupData.LayerTypes.Count != showData.LayerTypes.Count) continue;

            // Check if all LayerTypes match
            bool allMatch = true;
            for (var i = 0; i < showLayerGroupData.LayerTypes.Count; i++)
            {
                if (showLayerGroupData.LayerTypes[i] != showData.LayerTypes[i])
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch) return showLayerGroupData.ID;
        }

        return -1;
    }

    private readonly HashSet<LayerType> _layerPopupSet = new(LimitLayer);
    private readonly List<ShowLayerGroupData> _showingLayerGroupsTemp = new(LimitLayer);
    private void CloseAllPopupExist(ShowLayerGroupData showData)
    {
        _layerPopupSet.Clear();

        // Collect popup layers without LINQ
        foreach (var group in _showingLayerGroups)
        {
            if (group.LayerGroupType != LayerGroupType.Popup) continue;
            foreach (var layerType in group.LayerTypes)
            {
                if (!showData.LayerTypes.Contains(layerType))
                {
                    _layerPopupSet.Add(layerType);
                }
            }
        }

        if (_layerPopupSet.Count == 0) return;

        // Iterate directly on HashSet
        foreach (var layerType in _layerPopupSet)
        {
            CloseLayerAsync(layerType, true);
        }

        _showingLayerTypes.ExceptWith(_layerPopupSet);

        // Rebuild stack without allocating new Stack
        _showingLayerGroupsTemp.Clear();
        foreach (var group in _showingLayerGroups)
        {
            if (group.LayerGroupType != LayerGroupType.Popup)
            {
                _showingLayerGroupsTemp.Add(group);
            }
        }
        _showingLayerGroupsTemp.Reverse();
        _showingLayerGroups.Clear();
        for (int i = 0; i < _showingLayerGroupsTemp.Count; i++)
        {
            _showingLayerGroups.Push(_showingLayerGroupsTemp[i]);
        }
    }

    private readonly List<LayerType> _layerTypeToHide = new(LimitLayer);
    private void HideAllLayerExist(ShowLayerGroupData showData)
    {
        _layerTypeToHide.Clear();
        foreach (var layerType in _showingLayerTypes)
        {
            if (!showData.LayerTypes.Contains(layerType))
            {
                _layerTypeToHide.Add(layerType);
            }
        }
        foreach (var layerType in _layerTypeToHide)
        {
            HideLayerAsync(layerType);
        }
    }

    private readonly List<LayerType> _layerTypeToClose = new(LimitLayer);
    private void CloseAllLayerExist(ShowLayerGroupData showData)
    {
        _layerTypeToClose.Clear();
        foreach (var layerType in _showingLayerTypes)
        {
            if (!showData.LayerTypes.Contains(layerType))
            {
                _layerTypeToClose.Add(layerType);
            }
        }
        foreach (var layerType in _layerTypeToClose)
        {
            CloseLayerAsync(layerType, true);
        }
        _showingLayerGroups.Clear();
        _showingLayerTypes.Clear();
    }

    private void CloseLayerAsync(LayerType layerType, bool force = false)
    {
        var layerBase = GetLayerBase(layerType);
        if (!layerBase) return;
        layerBase.CloseLayerAsync(force);
    }

    private void HideLayerAsync(LayerType layerType)
    {
        var layerBase = GetLayerBase(layerType);
        if (!layerBase) return;
        layerBase.HideLayerAsync();
    }

    /// <summary>
    /// Lấy danh sách các loại layer đang hiển thị (Editor only)
    /// </summary>
    public HashSet<LayerType> GetShowingLayerTypes()
    {
        return new HashSet<LayerType>(_showingLayerTypes);
    }

    public HashSet<LayerType> GetLastGroupShowingLayerTypes()
    {
        var lastGroup = _showingLayerGroups.FirstOrDefault();
        if(lastGroup == null) return new HashSet<LayerType>();
        return new HashSet<LayerType>(lastGroup.LayerTypes);
    }

    // === DEBUG METHODS - Chỉ dành cho Editor/Development ===
#if UNITY_EDITOR
    /// <summary>
    /// Lấy stack các group đang hiển thị (Editor only)
    /// </summary>
    public Stack<ShowLayerGroupData> GetShowingLayerGroups()
    {
        return new Stack<ShowLayerGroupData>(_showingLayerGroups);
    }

    /// <summary>
    /// Lấy danh sách các layer đã tạo (Editor only)
    /// </summary>
    public Dictionary<LayerType, LayerBase> GetCreatedLayerBases()
    {
        return new Dictionary<LayerType, LayerBase>(_createdLayerBases);
    }
#endif
    
}

public static class LayerGroupBuilder
{
    private static int _idCounter;
    public static ShowLayerGroupData Build(LayerGroupType groupType, LayerType layerTypes)
    {
        var data = new ShowLayerGroupData
        {
            ID = Interlocked.Increment(ref _idCounter),
            LayerGroupType = groupType
        };
        data.LayerTypes.Add(layerTypes);
        data.ValidateData();
        return data;
    }

    [Obsolete]
    [Tooltip("Hàm này có thể gây hiệu năng không mong muốn")]
    public static ShowLayerGroupData Build(LayerGroupType groupType, List<LayerType> layerTypes)
    {
        var data = new ShowLayerGroupData
        {
            ID = Interlocked.Increment(ref _idCounter),
            LayerGroupType = groupType
        };
        data.LayerTypes.AddRange(layerTypes);
        data.ValidateData();
        return data;
    }
}

public class ShowLayerGroupData
{
    public int ID;
    public readonly List<LayerType> LayerTypes = new List<LayerType>(3);
    public LayerGroupType LayerGroupType;

    public bool CloseAllOtherLayer;
    public bool HideAllOtherLayer;
    public bool CloseAllPopup;
    public bool CloseOtherLayerOver = true;

    public bool AddToStack = true;
    public bool FixedLayer = false;

    public Action<LayerGroup> OnInitData;
    public Action<LayerGroup> OnShowComplete;
    public readonly bool DisplayImmediately = true;
    public void ValidateData()
    {
        if (LayerGroupType == LayerGroupType.Custom) return;
        CloseAllOtherLayer = LayerGroupType == LayerGroupType.Root;
        CloseOtherLayerOver = LayerGroupType == LayerGroupType.FullScreen;
        HideAllOtherLayer = LayerGroupType == LayerGroupType.FullScreen;
        CloseAllPopup = LayerGroupType == LayerGroupType.FullScreen;
        AddToStack = LayerGroupType != LayerGroupType.Fixed && LayerGroupType != LayerGroupType.Notify;
        FixedLayer = LayerGroupType == LayerGroupType.Fixed;
    }
    public ShowLayerGroupData AddLayer(LayerType layerType)
    {
        LayerTypes.Add(layerType);
        return this;
    }
}

public enum LayerGroupType
{
    None = -1,
    Custom = 0,
    Root = 1,
    FullScreen = 2,
    Popup = 3,
    Notify = 4,
    Fixed = 5
}
