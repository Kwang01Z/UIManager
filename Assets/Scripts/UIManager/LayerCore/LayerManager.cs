using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

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
        layerReferenceSO.InitLayerBase();
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
            _showQueue.Enqueue(()=>ShowGroupLayerAsync(showData));
            return;
        }
        Debug.Log(
            $"[ShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}");
        
        IsShowing = true;
        try
        {
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
            else if(!showData.FixedLayer)
            {
                _layerNotInStack.AddRange(showData.LayerTypes);
            }
            if(showData.DisplayImmediately) result.ShowGroupAsync();
            
            await UniTask.NextFrame();
            showData.OnShowComplete?.Invoke(result);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

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
        if (_showingLayerGroups.Count == 0) return;
        if (_showingLayerGroups.Count <= 1 && hasLayerRoot) return;
        var lastGroup = _showingLayerGroups.Pop();

        // Cập nhật _showingLayerTypes để loại bỏ các layer đã đóng
        foreach (var layerType in lastGroup.LayerTypes)
        {
            var layerBase = GetLayerBase(layerType);
            if (!layerBase) continue;
            layerBase.CloseLayerAsync();
            if(!layerBase.IsActive()) _showingLayerTypes.Remove(layerType);
        }
    }

    private void SetSortingLayer(LayerGroup layerGroup)
    {
        int bestOrder = GetBestLayerSorting(layerGroup) + spaceBetweenLayer;
        layerGroup.SetSortOrder(bestOrder);
    }

    private readonly List<LayerType> _layerTypeShowingTemps = new();
    private int GetBestLayerSorting(LayerGroup layerGroup)
    {
        int bestLayerSorting = 0;
        _layerTypeShowingTemps.Clear();
        _layerTypeShowingTemps.AddRange(_showingLayerTypes.Except(layerGroup.LayerTypes));
        foreach (var layerType in _layerTypeShowingTemps)
        {
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
        while (_showingLayerGroups.Count > 0 && _showingLayerGroups.First().ID != groupLayerProjectId)
        {
            _overLayerTypeTotals.UnionWith(_showingLayerGroups.Pop().LayerTypes);
        }

        if (_showingLayerGroups.Count > 0) _overLayerTypeTotals.UnionWith(_showingLayerGroups.Pop().LayerTypes);
        _overLayerTypes.Clear();
        _overLayerTypes.AddRange(_overLayerTypeTotals.Except(showData.LayerTypes));
        if (_overLayerTypes.Count == 0) return;
        foreach (var overLayerType in _overLayerTypes)
        {
            CloseLayerAsync(overLayerType);
        }

        _showingLayerTypes = new(_showingLayerTypes.Except(_overLayerTypes));
    }

    private int GetGroupLayerProjectId(ShowLayerGroupData showData)
    {
        var id = -1;
        foreach (var showLayerGroupData in _showingLayerGroups)
        {
            if (showLayerGroupData.ID == showData.ID) return showData.ID;
            if (showLayerGroupData.LayerTypes.Count != showData.LayerTypes.Count) continue;
            var similarCount = 0;
            for (var i = 0; i < showLayerGroupData.LayerTypes.Count; i++)
            {
                if(showLayerGroupData.LayerTypes[i] != showData.LayerTypes[i]) break;
                similarCount++;
            }
            if (similarCount != showLayerGroupData.LayerTypes.Count) continue;
            id = showLayerGroupData.ID;
        }

        return id;
    }

    private readonly List<LayerType> _layerPopupTemp = new(LimitLayer);
    private readonly List<LayerType> _showingLayerTypeTemp = new(LimitLayer); 
    private readonly List<ShowLayerGroupData> _showingLayerGroupsTemp = new(LimitLayer);
    private void CloseAllPopupExist(ShowLayerGroupData showData)
    {
        _layerPopupTemp.Clear();
        _layerPopupTemp.AddRange(_showingLayerGroups
            .Where(x => x.LayerGroupType == LayerGroupType.Popup)
            .SelectMany(x => x.LayerTypes)
            .Except(showData.LayerTypes)
            .Distinct());
        if(_layerPopupTemp.Count == 0) return;
        foreach (var layerType in _layerPopupTemp)
        {
            CloseLayerAsync(layerType, true);
        }
        
        _showingLayerTypeTemp.Clear();
        _showingLayerTypeTemp.AddRange(_showingLayerTypes);
        _showingLayerTypes.Clear();
        _showingLayerTypes.AddRange(_showingLayerTypeTemp.Except(_layerPopupTemp));
        _showingLayerGroupsTemp.Clear();
        _showingLayerGroupsTemp.AddRange(_showingLayerGroups);
        _showingLayerGroupsTemp.RemoveAll(x => x.LayerGroupType == LayerGroupType.Popup);
        _showingLayerGroupsTemp.Reverse();
        _showingLayerGroups = new(_showingLayerGroupsTemp);
    }

    private readonly List<LayerType> _layerTypeToHide = new(LimitLayer);
    private void HideAllLayerExist(ShowLayerGroupData showData)
    {
        _layerTypeToHide.Clear();
        _layerTypeToHide.AddRange(_showingLayerTypes.Except(showData.LayerTypes));
        foreach (var layerType in _layerTypeToHide)
        {
            HideLayerAsync(layerType);
        }
    }

    private readonly List<LayerType> _layerTypeToClose = new(LimitLayer);
    private void CloseAllLayerExist(ShowLayerGroupData showData)
    {
        _layerTypeToClose.Clear();
        _layerTypeToClose.AddRange(_showingLayerTypes.Except(showData.LayerTypes));
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

        // Cập nhật _showingLayerTypes khi đóng layer
        _showingLayerTypes.Remove(layerType);

        layerBase.CloseLayerAsync(force);
    }

    private void HideLayerAsync(LayerType layerType)
    {
        var layerBase = GetLayerBase(layerType);
        if (!layerBase) return;
        layerBase.HideLayerAsync();
    }

    // === DEBUG METHODS - Chỉ dành cho Editor/Development ===
#if UNITY_EDITOR
    /// <summary>
    /// Lấy danh sách các loại layer đang hiển thị (Editor only)
    /// </summary>
    public HashSet<LayerType> GetShowingLayerTypes()
    {
        return new HashSet<LayerType>(_showingLayerTypes);
    }

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
    Custom = 0,
    Root = 1,
    FullScreen = 2,
    Popup = 3,
    Notify = 4,
    Fixed = 5
}
