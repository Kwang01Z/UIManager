using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public partial class LayerManager : MonoSingleton<LayerManager>
{
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

    private static Queue<Action> _showQueue = new(4);
    public bool IsShowing { get; private set; }

    public async void ShowGroupLayerAsync(ShowLayerGroupData showData)
    {
        if (IsShowing)
        {
            Debug.Log(
                $"[TryShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}  not success");
            _showQueue.Enqueue(() => ShowGroupLayerAsync(showData));
            return;
        }
        Debug.Log(
            $"[ShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}");

        IsShowing = true;
        var result = InitLayerGroup(showData);
        await UniTask.NextFrame();
        showData.OnInitData?.Invoke(result);
        await UniTask.NextFrame();
        HideLayerRequired(showData);
        await UniTask.NextFrame();
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

        IsShowing = false;
        if (_showQueue.Count > 0)
        {
            await UniTask.NextFrame();
            _showQueue.Dequeue().Invoke();
        }
    }
    private readonly List<LayerType> _layerNotInStack = new();

    public void CloseLastLayerGroup()
    {
        Debug.Log($"[{Time.frameCount}] CloseLastLayerGroup");
        if (_showingLayerGroups.Count == 0) return;
        if (_showingLayerGroups.Count <= 1 && hasLayerRoot) return;
        
        ShowLayerGroupData lastGroup = null;
        int index = 0;
        foreach (var layer in _showingLayerGroups)
        {
            if (index == 1)
            {
                lastGroup = layer;
                break;
            }
            index++;
        }
        ShowGroupLayerAsync(lastGroup);
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
