using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class LayerManager
{
    private ShowLayerGroupData _showLayer01Data;
    public async Task ShowLayer01(LayerGroupType layerGroupType)
    {
        _showLayer01Data ??= LayerGroupBuilder.Build(layerGroupType, LayerType.Layer01);
        await ShowGroupLayerAsync(_showLayer01Data,SetupDataLayer01);
        return;

        Task SetupDataLayer01(LayerGroup layerGroup)
        {
            if(layerGroup == null) return Task.CompletedTask;
            if (layerGroup.GetLayerBase(LayerType.Layer01, out var layerBase))
            {
                
            }
            return Task.CompletedTask;
        }
    }
    private ShowLayerGroupData _showLayer02Data;
    public async Task ShowLayer02(LayerGroupType layerGroupType)
    {
        _showLayer02Data ??= LayerGroupBuilder.Build(layerGroupType, LayerType.Layer02);
        await ShowGroupLayerAsync(_showLayer02Data,SetupDataLayer02);
        return;

        Task SetupDataLayer02(LayerGroup layerGroup)
        {
            if(layerGroup == null) return Task.CompletedTask;
            if (layerGroup.GetLayerBase(LayerType.Layer02, out var layerBase))
            {
                
            }
            return Task.CompletedTask;
        }
    }
}
