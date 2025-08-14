using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class LayerManager
{
    public async UniTask ShowLayer01(int index)
    {
        var group = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.Layer01);
        await ShowGroupLayerAsync(group,SetupDataLayer01);
        return;

        UniTask SetupDataLayer01(LayerGroup layerGroup)
        {
            if(layerGroup == null) return UniTask.CompletedTask;
            if (layerGroup.GetLayerBase(LayerType.Layer01, out var layerBase))
            {
                
            }
            return UniTask.CompletedTask;
        }
    }
}
