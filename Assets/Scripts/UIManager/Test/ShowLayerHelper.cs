using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class LayerManager
{
    public async UniTask ShowLayer01()
    {
        var group = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.Layer01);
        var result = await ShowGroupLayer(group);
        if (result == null) return;
        if (result.GetLayerBase(LayerType.Layer01, out var layer01))
        {
            
        }
    }
}
