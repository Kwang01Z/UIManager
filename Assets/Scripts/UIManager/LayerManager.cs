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
    private CancellationToken _destroyCt;
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
        _destroyCt = this.GetCancellationTokenOnDestroy();
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

    public bool IsShowing;

    public async Task<LayerGroup> ShowGroupLayerAsync(ShowLayerGroupData showData, Func<LayerGroup, Task> onInitData = null, bool displayImmediately = true)
    {
        if (IsShowing)
        {
            Debug.Log(
                $"[TryShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}  not success");
            return new();
        }
        
        Debug.Log(
            $"[ShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}");
        IsShowing = true;
        LayerGroup result = null;
        try
        {
            result = InitLayerGroup(showData);
            await UniTask.NextFrame();
            if(onInitData != null) await onInitData.Invoke(result);
            await UniTask.NextFrame();
            await HideLayerRequired(showData);
            if (showData.AddToStack)
            {
                _showingLayerGroups.Push(showData);
                _showingLayerTypes.UnionWith(showData.LayerTypes);
            }
            else if(!showData.FixedLayer)
            {
                _layerNotInStack.AddRange(showData.LayerTypes);
            }
            
            await UniTask.NextFrame();
            if(displayImmediately) await DisplayLayerGroup(result);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        IsShowing = false;

        return result;
    }
    private List<LayerType> _layerNotInStack = new();

    public async UniTask DisplayLayerGroup(LayerGroup group)
    {
        SetSortingLayer(group);
        await group.ShowGroupAsync();
    }

    public async UniTask CloseLastLayerGroup()
    {
        if (_showingLayerGroups.Count == 0) return;
        if (_showingLayerGroups.Count <= 1 && hasLayerRoot) return;
        var lastGroup = _showingLayerGroups.Pop();
        var closeTask = new List<UniTask>();
        foreach (var layerType in lastGroup.LayerTypes)
        {
            var layerBase = GetLayerBase(layerType);
            if (!layerBase) continue;
            closeTask.Add(layerBase.CloseLayerAsync());
        }

        await UniTask.WhenAll(closeTask);
    }

    private void SetSortingLayer(LayerGroup layerGroup)
    {
        int bestOrder = GetBestLayerSorting(layerGroup) + spaceBetweenLayer;
        layerGroup.SetSortOrder(bestOrder);
    }

    private List<LayerType> _layerTypeShowingTemps = new();
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
    
    public LayerBase InitLayerBase(LayerType layerType)
    {
        var layerBase = GetLayerBase(layerType);
        if (layerBase) return layerBase;
        layerBase = layerReferenceSO.GetLayerBase(layerType);
        if (!layerBase) return null;
        var layerBaseGo = Instantiate(layerBase, layerParent);
        _createdLayerBases.Add(layerType, layerBaseGo);
        return layerBaseGo;
    }

    public LayerBase GetLayerBase(LayerType layerType)
    {
        return _createdLayerBases.GetValueOrDefault(layerType);
    }

    private List<UniTask> _hideTaskRequiredTemps = new(LimitLayer);
    public async UniTask HideLayerRequired(ShowLayerGroupData showData)
    {
        if (showData == null) return;

        foreach (var layerType in _layerNotInStack)
        {
            var layerBase = GetLayerBase(layerType);
            if (!layerBase) continue;
            layerBase.CloseLayerAsync().Forget();
        }
        if (showData.CloseAllOtherLayer)
        {
            await CloseAllLayerExist(showData);
            return;
        }
        
        if (showData.CloseOtherLayerOver)
        {
            _hideTaskRequiredTemps.Add(CloseOtherLayerOver(showData));
        }

        if (showData.HideAllOtherLayer)
        {
            _hideTaskRequiredTemps.Add(HideAllLayerExist(showData));
        }

        if (showData.CloseAllPopup)
        {
            _hideTaskRequiredTemps.Add(CloseAllPopupExist(showData));
        }

        await UniTask.WhenAll(_hideTaskRequiredTemps);
        _hideTaskRequiredTemps.Clear();
    }

    HashSet<LayerType> _overLayerTypeTotals = new(LimitLayer);
    HashSet<LayerType> _overLayerTypes = new(LimitLayer);
    private async UniTask CloseOtherLayerOver(ShowLayerGroupData showData)
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
            _closeTasks.Add(CloseLayerAsync(overLayerType));
        }

        _showingLayerTypes = new(_showingLayerTypes.Except(_overLayerTypes));
        await UniTask.WhenAll(_closeTasks);
        _closeTasks.Clear();
    }

    private int GetGroupLayerProjectId(ShowLayerGroupData showData)
    {
        int id = -1;
        int similarCount = 0;
        foreach (var showLayerGroupData in _showingLayerGroups)
        {
            if (showLayerGroupData.ID == showData.ID) return id;
            if (showLayerGroupData.LayerTypes.Count != showData.LayerTypes.Count) continue;
            similarCount = 0;
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

    private List<LayerType> _layerPopupTemp = new(LimitLayer);
    private List<LayerType> _showingLayerTypeTemp = new(LimitLayer); 
    private List<ShowLayerGroupData> _showingLayerGroupsTemp = new(LimitLayer);
    private async UniTask CloseAllPopupExist(ShowLayerGroupData showData)
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
            _closeTasks.Add(CloseLayerAsync(layerType, true));
        }

        await UniTask.WhenAll(_closeTasks);
        _closeTasks.Clear();
        _showingLayerTypeTemp.Clear();
        _showingLayerTypeTemp.AddRange(_showingLayerTypes);
        _showingLayerTypes.Clear();
        _showingLayerTypes.AddRange(_showingLayerTypeTemp.Except(_layerPopupTemp));
        _showingLayerGroupsTemp.Clear();
        _showingLayerGroupsTemp.AddRange(_showingLayerGroups);
        _showingLayerGroups = new(_showingLayerGroupsTemp.RemoveAll(x => x.LayerGroupType == LayerGroupType.Popup));
        
        _showingLayerGroupsTemp.Clear();
        _showingLayerTypeTemp.Clear();
    }

    private List<LayerType> _layerTypeToHide = new(LimitLayer);
    private async UniTask HideAllLayerExist(ShowLayerGroupData showData)
    {
        _layerTypeToHide.Clear();
        _layerTypeToHide.AddRange(_showingLayerTypes.Except(showData.LayerTypes));
        foreach (var layerType in _layerTypeToHide)
        {
            _hideTasks.Add(HideLayerAsync(layerType));
        }

        await UniTask.WhenAll(_hideTasks);
        _layerTypeToHide.Clear();
        _hideTasks.Clear();
    }

    private List<LayerType> _layerTypeToClose = new(LimitLayer);
    private List<UniTask> _closeTasks = new(LimitLayer);
    private List<UniTask> _hideTasks = new(LimitLayer);
    private async UniTask CloseAllLayerExist(ShowLayerGroupData showData)
    {
        _layerTypeToClose.AddRange(_showingLayerTypes.Except(showData.LayerTypes));
        foreach (var layerType in _layerTypeToClose)
        {
            _closeTasks.Add(CloseLayerAsync(layerType, true));
        }

        await UniTask.WhenAll(_closeTasks);
        _showingLayerGroups.Clear();
        _showingLayerTypes.Clear();
        _layerTypeToClose.Clear();
        _closeTasks.Clear();
    }

    private async UniTask CloseLayerAsync(LayerType layerType, bool force = false)
    {
        var layerBase = GetLayerBase(layerType);
        if (!layerBase) return;
        await layerBase.CloseLayerAsync(force);
    }

    private UniTask HideLayerAsync(LayerType layerType)
    {
        var layerBase = GetLayerBase(layerType);
        if (!layerBase) return UniTask.CompletedTask;
        return layerBase.HideLayerAsync();
    }
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
}

public class ShowLayerGroupData
{
    public int ID;
    public List<LayerType> LayerTypes = new List<LayerType>();
    public LayerGroupType LayerGroupType;

    public bool CloseAllOtherLayer;
    public bool HideAllOtherLayer;
    public bool CloseAllPopup;
    public bool CloseOtherLayerOver = true;
    
    public bool AddToStack = true;
    public bool FixedLayer = false;

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
    public void AddLayer(LayerType layerType)
    {
        LayerTypes.Add(layerType);
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