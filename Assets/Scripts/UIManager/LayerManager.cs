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
    
    public int spaceBetweenLayer = 100;
    private CancellationToken _destroyCt;
    private Dictionary<LayerType, LayerBase> _createdLayerBases = new();
    private HashSet<LayerType> _showingLayerTypes = new();
    private Stack<ShowLayerGroupData> _showingLayerGroups = new();

    private void Reset()
    {
        if (!layerParent) layerParent = transform as RectTransform;
    }

    protected override void Awake()
    {
        base.Awake();
        _destroyCt = this.GetCancellationTokenOnDestroy();
    }

    public bool IsShowing { get; private set; }

    public async UniTask<LayerGroup> ShowGroupLayerAsync(ShowLayerGroupData showData, Func<LayerGroup, UniTask> onInitData = null)
    {
        await UniTask.WhenAny(UniTask.WaitUntil(() => !IsShowing, cancellationToken: _destroyCt),
            UniTask.WaitForSeconds(5, cancellationToken: _destroyCt));
        if (_destroyCt.IsCancellationRequested) return new();
        if (IsShowing)
        {
            Debug.Log(
                $"[TryShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}  not success");
            return new();
        }

        await UniTask.NextFrame();
        Debug.Log(
            $"[ShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}");
        IsShowing = true;
        LayerGroup result = null;
        try
        {
            result = await InitLayerGroup(showData);
            await UniTask.Yield();
            if(onInitData != null) await onInitData.Invoke(result);
            await UniTask.NextFrame();
            await HideLayerRequired(showData);
            _showingLayerGroups.Push(showData);
            _showingLayerTypes.UnionWith(showData.LayerTypes);
            await UniTask.NextFrame();
            await DisplayLayerGroup(result);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        IsShowing = false;

        return result;
    }

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

    private async UniTask<LayerGroup> InitLayerGroup(ShowLayerGroupData showData)
    {
        var layerGroup = new LayerGroup();
        foreach (var layerType in showData.LayerTypes)
        {
            var layerBase = await InitLayerBase(layerType);
            if (!layerBase) continue;

            layerGroup.AddLayer(layerType, layerBase);
        }

        return layerGroup;
    }

    public async UniTask<LayerBase> InitLayerBase(LayerType layerType)
    {
        var layerBase = GetLayerBase(layerType);
        if (layerBase) return layerBase;
        layerBase = await AddressableLoadLayer(layerType);
        if (!layerBase) return null;
        _createdLayerBases.Add(layerType, layerBase);
        return layerBase;
    }

    private async UniTask<LayerBase> AddressableLoadLayer(LayerType layerType)
    {
        if (!layerParent)
        {
            Debug.LogError("[LayerManager.AddressableLoadLayer] LayerParent is null");
            return null;
        }

        string loadPath = LayerSourcePath.GetPath(layerType.ToString());
        if (string.IsNullOrEmpty(loadPath))
        {
            Debug.LogError("[LayerManager.AddressableLoadLayer] Source Path is null");
            return null;
        }

        var insLayerBaseHandle = Addressables.InstantiateAsync(loadPath, layerParent);
        await insLayerBaseHandle;
        if (insLayerBaseHandle.Status == AsyncOperationStatus.Succeeded)
        {
            var insLayerBase = insLayerBaseHandle.Result.GetComponent<LayerBase>();
            return insLayerBase;
        }
        else
        {
            Debug.LogError($"[AddressableLoadLayer] Failed to instantiate prefab type {layerType} from Addressables.");
            return null;
        }
    }

    public LayerBase GetLayerBase(LayerType layerType)
    {
        return _createdLayerBases.GetValueOrDefault(layerType);
    }

    public async UniTask HideLayerRequired(ShowLayerGroupData showData)
    {
        if (showData == null) return;

        if (showData.CloseAllOtherLayer)
        {
            await CloseAllLayerExist(showData);
            return;
        }

        var task = new List<UniTask>();
        if (showData.CloseOtherLayerOver)
        {
            task.Add(CloseOtherLayerOver(showData));
        }

        if (showData.HideAllOtherLayer)
        {
            task.Add(HideAllLayerExist(showData));
        }

        if (showData.CloseAllPopup)
        {
            task.Add(CloseAllPopupExist(showData));
        }

        await UniTask.WhenAll(task);
    }

    private async UniTask CloseOtherLayerOver(ShowLayerGroupData showData)
    {
        var groupLayerProjectId = GetGroupLayerProjectId(showData);
        if (groupLayerProjectId == -1) return;
        HashSet<LayerType> overLayerTypes = new();
        while (_showingLayerGroups.Count > 0 && _showingLayerGroups.First().ID != groupLayerProjectId)
        {
            overLayerTypes.UnionWith(_showingLayerGroups.Pop().LayerTypes);
        }

        if (_showingLayerGroups.Count > 0) overLayerTypes.UnionWith(_showingLayerGroups.Pop().LayerTypes);
        overLayerTypes = new(overLayerTypes.Except(showData.LayerTypes));
        if (overLayerTypes.Count == 0) return;
        var closeTasks = new List<UniTask>();
        foreach (var overLayerType in overLayerTypes)
        {
            closeTasks.Add(CloseLayerAsync(overLayerType));
        }

        _showingLayerTypes = new(_showingLayerTypes.Except(overLayerTypes));
        await UniTask.WhenAll(closeTasks);
    }

    private int GetGroupLayerProjectId(ShowLayerGroupData showData)
    {
        int id = -1;
        foreach (var showLayerGroupData in _showingLayerGroups)
        {
            if (showLayerGroupData.ID == showData.ID) return id;
            if (showLayerGroupData.LayerTypes.Count != showData.LayerTypes.Count) continue;
            if (showLayerGroupData.LayerTypes.Except(showData.LayerTypes).Any()) continue;
            id = showLayerGroupData.ID;
        }

        return id;
    }

    private async UniTask CloseAllPopupExist(ShowLayerGroupData showData)
    {
        var layerPopup = new List<LayerType>(_showingLayerGroups
            .Where(x => x.LayerGroupType == LayerGroupType.Popup)
            .SelectMany(x => x.LayerTypes)
            .Except(showData.LayerTypes)
            .Distinct());
        if(layerPopup.Count == 0) return;
        for (var i = 0; i < layerPopup.Count; i++)
        {
            _hideTasks.Add(CloseLayerAsync(layerPopup[i], true));
        }

        await UniTask.WhenAll(_hideTasks);
        _hideTasks.Clear();
        _showingLayerTypes = new(_showingLayerTypes.Except(layerPopup));
        var showingLayerGroups = new List<ShowLayerGroupData>(_showingLayerGroups);
        _showingLayerGroups = new(showingLayerGroups
            .RemoveAll(x => x.LayerGroupType == LayerGroupType.Popup));
    }

    private async UniTask HideAllLayerExist(ShowLayerGroupData showData)
    {
        var layerTypeToHide = new List<LayerType>(_showingLayerTypes.Except(showData.LayerTypes));
        
        // Filter out persistent layers - they should never be hidden
        layerTypeToHide = FilterOutPersistentLayers(layerTypeToHide);
        
        for (var i = 0; i < layerTypeToHide.Count; i++)
        {
            _hideTasks.Add(HideLayerAsync(layerTypeToHide[i]));
        }

        await UniTask.WhenAll(_hideTasks);
        _hideTasks.Clear();
    }

    private List<LayerType> _layerTypeToClose = new();
    private List<UniTask> _hideTasks = new();
    private async UniTask CloseAllLayerExist(ShowLayerGroupData showData)
    {
        var layersToConsider = showData.IgnoreHideThisLayer
            ? _showingLayerTypes.Except(showData.LayerTypes)
            : _showingLayerTypes;
            
        _layerTypeToClose.AddRange(layersToConsider);
        
        // Filter out persistent layers - they should never be closed by other operations
        _layerTypeToClose = FilterOutPersistentLayers(_layerTypeToClose);
        
        for (var i = 0; i < _layerTypeToClose.Count; i++)
        {
            _hideTasks.Add(CloseLayerAsync(_layerTypeToClose[i], true));
        }

        await UniTask.WhenAll(_hideTasks);
        
        // Only clear groups that are not persistent
        var persistentGroups = _showingLayerGroups.Where(g => g.LayerGroupType == LayerGroupType.Persistent).ToList();
        var persistentLayers = new HashSet<LayerType>(persistentGroups.SelectMany(g => g.LayerTypes));
        
        _showingLayerGroups.Clear();
        // Restore persistent groups
        foreach (var persistentGroup in persistentGroups)
        {
            _showingLayerGroups.Push(persistentGroup);
        }
        
        // Update showing layer types - keep persistent layers
        _showingLayerTypes = new HashSet<LayerType>(persistentLayers);
        
        _layerTypeToClose.Clear();
        _hideTasks.Clear();
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
    
    /// <summary>
    /// Filter out layers that belong to Persistent groups - they should never be hidden/closed by other operations
    /// </summary>
    private List<LayerType> FilterOutPersistentLayers(List<LayerType> layerTypes)
    {
        var filteredLayers = new List<LayerType>();
        
        foreach (var layerType in layerTypes)
        {
            // Find the group this layer belongs to
            var persistentGroup = _showingLayerGroups.FirstOrDefault(group => 
                group.LayerGroupType == LayerGroupType.Persistent && 
                group.LayerTypes.Contains(layerType));
                
            // Only add to filtered list if it's NOT in a persistent group
            if (persistentGroup == null)
            {
                filteredLayers.Add(layerType);
            }
            else
            {
                Debug.Log($"[LayerManager] Skipping {layerType} - it belongs to a Persistent group and cannot be hidden");
            }
        }
        
        return filteredLayers;
    }
    
    /// <summary>
    /// Check if a layer type belongs to a Persistent group
    /// </summary>
    private bool IsLayerPersistent(LayerType layerType)
    {
        return _showingLayerGroups.Any(group => 
            group.LayerGroupType == LayerGroupType.Persistent && 
            group.LayerTypes.Contains(layerType));
    }
}

public static class LayerGroupBuilder
{
    public static ShowLayerGroupData Build(LayerGroupType groupType, params LayerType[] layerTypes)
    {
        var data = new ShowLayerGroupData
        {
            ID = Guid.NewGuid().GetHashCode(),
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
    public List<LayerType> LayerTypes = new List<LayerType>();
    public LayerGroupType LayerGroupType;

    public bool CloseAllOtherLayer;
    public bool HideAllOtherLayer;
    public bool CloseAllPopup;
    public bool CloseOtherLayerOver = true;

    public bool IgnoreHideThisLayer = true;

    public void ValidateData()
    {
        if (LayerGroupType == LayerGroupType.Custom) return;
        CloseAllOtherLayer = LayerGroupType == LayerGroupType.Root;
        HideAllOtherLayer = LayerGroupType == LayerGroupType.FullScreen;
        CloseAllPopup = LayerGroupType == LayerGroupType.FullScreen;
    }
}

public enum LayerGroupType
{
    Custom = 0,
    Root = 1,
    FullScreen = 2,
    Popup = 3,
    Notify = 4,
    Persistent = 5,  // Never hidden by other layer operations
}
