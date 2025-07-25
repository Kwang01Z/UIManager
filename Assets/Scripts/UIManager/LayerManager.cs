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
    private CancellationToken _destroyCTS;
    private Dictionary<LayerType, LayerBase> _showedLayerBases = new();
    private Dictionary<LayerType, LayerBase> _showingLayerBases = new();
    protected override void Awake()
    {
        base.Awake();
        _destroyCTS = this.GetCancellationTokenOnDestroy();
    }

    public bool IsShowing { get; private set; }
    private async UniTask<LayerGroup> ShowGroupLayer(ShowLayerGroupData showData)
    {
        await UniTask.WhenAny(UniTask.WaitUntil(() => !IsShowing,cancellationToken:_destroyCTS)
            , UniTask.WaitForSeconds(5, cancellationToken: _destroyCTS));
        if (IsShowing)
        {
            Debug.Log($"[TryShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|",showData.LayerTypes)} - {showData.LayerGroupType}  not success");
            return null;
        }
        Debug.Log($"[ShowGroupLayer] [Frame:{Time.frameCount}] {String.Join("|",showData.LayerTypes)} - {showData.LayerGroupType}");
        IsShowing = true;
        LayerGroup result = null;
        try
        {
            result = await InitLayerGroup(showData);
            await HideLayerExist(showData);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
            
        IsShowing = false;
        return result;
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
        if(string.IsNullOrEmpty(loadPath))
        {
            Debug.LogError("[LayerManager.AddressableLoadLayer] Source Path is null");
            return null;
        }
        var insLayerBaseHandle = Addressables.InstantiateAsync(loadPath, layerParent);
        await insLayerBaseHandle.Task;
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
        if(showData == null) return;
        if (showData.CloseAllOtherLayer)
        {
            await CloseAllLayerExist(showData);
        }
        else if (showData.HideAllOtherLayer)
        {
            await HideAllLayerExist();
        }
        if (showData.CloseAllPopup)
        {
            await CloseAllPopupExist();
        }
    }

    private async UniTask CloseAllPopupExist()
    {
        
    }

    private async UniTask HideAllLayerExist()
    {
        
    }

    private async UniTask CloseAllLayerExist(ShowLayerGroupData showData)
    {
        var layerTypeToHide = _showingLayerBases.Keys.Except(showData.LayerTypes).ToList();
        var hideTasks = new List<UniTask>();
        for (var i = 0; i < layerTypeToHide.Count; i++)
        {
            hideTasks.Add(CloseLayerAsync(layerTypeToHide[i]));
        }
        await UniTask.WhenAll(hideTasks);
    }

    private async UniTask CloseLayerAsync(LayerType layerType)
    {
        var layerBase = GetLayerBase(layerType);
        if (!layerBase) return;
        await layerBase.CloseLayerAsync();
    }
    private UniTask HideLayerAsync(LayerType layerType)
    {
        var layerBase = GetLayerBase(layerType);
        if (!layerBase) return UniTask.CompletedTask;
        return layerBase.HideLayerAsync();
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
    public static ShowLayerGroupData Build(LayerGroupType groupType, params LayerType[] layerTypes)
    {
        var data = new ShowLayerGroupData();
        data.ID = Guid.NewGuid().GetHashCode();
        data.LayerGroupType = groupType;
        data.LayerTypes.AddRange(layerTypes);
        data.ValidateData();
        return data;
    }

    private void ValidateData()
    {
        if(LayerGroupType == LayerGroupType.Custom) return;
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
