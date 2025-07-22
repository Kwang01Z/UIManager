using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class LayerManager : MonoSingleton<LayerManager>
{
    private CancellationToken _destroyCTS;
    protected override void Awake()
    {
        base.Awake();
        _destroyCTS = this.GetCancellationTokenOnDestroy();
    }

    public bool IsShowing { get; private set; }
    private async UniTask<LayerGroup> ShowGroupLayer(ShowLayerGroupData showData)
    {
        try
        {
            LayerGroup result = new LayerGroup();
            await UniTask.WhenAny(UniTask.WaitUntil(() => !IsShowing,cancellationToken:_destroyCTS)
                , UniTask.WaitForSeconds(5, cancellationToken: _destroyCTS));
            IsShowing = true;
            
            IsShowing = false;
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
    }
}

public class ShowLayerGroupData
{
    public int ID;
    public List<LayerType> LayerTypes = new List<LayerType>();
    public LayerGroupType LayerGroupType;

    public static ShowLayerGroupData Build(LayerGroupType groupType, params LayerType[] layerTypes)
    {
        var data = new ShowLayerGroupData();
        data.ID = Guid.NewGuid().GetHashCode();
        data.LayerGroupType = groupType;
        data.LayerTypes.AddRange(layerTypes);
        return data;
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
