using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class LayerManager : MonoSingleton<LayerManager>
{
    [SerializeField] private bool hasLayerRoot = true;
    [SerializeField] private RectTransform layerParent;
    public int SpaceBetweenLayer = 100;
    private CancellationToken _destroyCTS;
    private Dictionary<LayerType, LayerBase> _showedLayerBases = new();
    private Dictionary<LayerType, LayerBase> _showingLayerBases = new();
    private Stack<ShowLayerGroupData> _showingLayerGroups = new();

    private void Reset()
    {
        if (!layerParent) layerParent = transform as RectTransform;
    }

    protected override void Awake()
    {
        base.Awake();
        _destroyCTS = this.GetCancellationTokenOnDestroy();
    }

    public bool IsShowing { get; private set; }

    public async UniTask<LayerGroup> ShowGroupLayer(ShowLayerGroupData showData, bool isShow = true)
    {
        await UniTask.WhenAny(UniTask.WaitUntil(() => !IsShowing, cancellationToken: _destroyCTS)
            , UniTask.WaitForSeconds(5, cancellationToken: _destroyCTS));
        if (IsShowing)
        {
            Debug.Log(
                $"[TryShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}  not success");
            return null;
        }

        await UniTask.NextFrame();
        Debug.Log(
            $"[ShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|", showData.LayerTypes)} - {showData.LayerGroupType}");
        IsShowing = true;
        LayerGroup result = null;
        try
        {
            result = await InitLayerGroup(showData);
            await HideLayerExist(showData);
            SetSortingLayer(result);
            _showingLayerGroups.Push(showData);
            if (isShow) await DisplayLayerGroup(result);
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
        int bestOrder = GetBestLayerSorting(layerGroup) + SpaceBetweenLayer;
        layerGroup.SetSortOrder(bestOrder);
    }

    private int GetBestLayerSorting(LayerGroup layerGroup)
    {
        var layerTypeShowings = new List<LayerType>(_showingLayerBases.Keys.Except(layerGroup.LayerTypes));
        var layerBaseShowings = new List<LayerBase>();
        foreach (var layerType in layerTypeShowings)
        {
            var layerBase = GetLayerBase(layerType);
            if (!layerBase) continue;
            layerBaseShowings.Add(layerBase);
        }

        var bestLayerSorting = layerBaseShowings.Count > 0 ? layerBaseShowings.Max(x => x.GetSortingOrder()) : 0;
        return bestLayerSorting / SpaceBetweenLayer * SpaceBetweenLayer;
    }

    private async UniTask<LayerGroup> InitLayerGroup(ShowLayerGroupData showData)
    {
        var layerGroup = new LayerGroup();
        foreach (var layerType in showData.LayerTypes)
        {
            var layerBase = GetLayerBase(layerType);
            if (!layerBase)
            {
                layerBase = await AddressableLoadLayer(layerType);
                if (!layerBase) continue;
                _showedLayerBases.Add(layerType, layerBase);
            }

            layerGroup.AddLayer(layerType, layerBase);
        }

        return layerGroup;
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
        await insLayerBaseHandle.Task.AsUniTask().AttachExternalCancellation(destroyCancellationToken);
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
        return _showedLayerBases.GetValueOrDefault(layerType);
    }

    private async UniTask HideLayerExist(ShowLayerGroupData showData)
    {
        if (showData == null) return;


        if (showData.CloseAllOtherLayer)
        {
            await CloseAllLayerExist(showData);
            return;
        }

        if (showData.CloseOtherLayerOver)
        {
            await CloseOtherLayerExist(showData);
        }

        if (showData.HideAllOtherLayer)
        {
            await HideAllLayerExist(showData);
        }

        if (showData.CloseAllPopup)
        {
            await CloseAllPopupExist(showData);
        }
    }

    private async UniTask CloseOtherLayerExist(ShowLayerGroupData showData)
    {
        throw new NotImplementedException();
    }

    private async UniTask CloseAllPopupExist(ShowLayerGroupData showData)
    {
        var layerPopup = new List<LayerType>(_showingLayerGroups
            .Where(x => x.LayerGroupType == LayerGroupType.Popup)
            .SelectMany(x => x.LayerTypes)
            .Except(showData.LayerTypes));

        var closeTasks = new List<UniTask>();
        for (var i = 0; i < layerPopup.Count; i++)
        {
            closeTasks.Add(CloseLayerAsync(layerPopup[i], true));
        }

        await UniTask.WhenAll(closeTasks);
        var showingLayerGroups = new List<ShowLayerGroupData>(_showingLayerGroups);
        _showingLayerGroups = new(showingLayerGroups
            .RemoveAll(x => x.LayerGroupType == LayerGroupType.Popup));
    }

    private async UniTask HideAllLayerExist(ShowLayerGroupData showData)
    {
        var layerTypeToHide = new List<LayerType>(_showingLayerBases.Keys.Except(showData.LayerTypes));
        var hideTasks = new List<UniTask>();
        for (var i = 0; i < layerTypeToHide.Count; i++)
        {
            hideTasks.Add(HideLayerAsync(layerTypeToHide[i]));
        }

        await UniTask.WhenAll(hideTasks);
    }

    private async UniTask CloseAllLayerExist(ShowLayerGroupData showData)
    {
        var layerTypeToClose = new List<LayerType>(_showingLayerBases.Keys.Except(showData.LayerTypes));
        var hideTasks = new List<UniTask>();
        for (var i = 0; i < layerTypeToClose.Count; i++)
        {
            hideTasks.Add(CloseLayerAsync(layerTypeToClose[i], true));
        }

        await UniTask.WhenAll(hideTasks);
        _showingLayerGroups.Clear();
        _showingLayerBases.Clear();
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
    public static ShowLayerGroupData Build(LayerGroupType groupType, params LayerType[] layerTypes)
    {
        var data = new ShowLayerGroupData();
        data.ID = Guid.NewGuid().GetHashCode();
        data.LayerGroupType = groupType;
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
}