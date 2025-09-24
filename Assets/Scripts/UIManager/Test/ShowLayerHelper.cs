using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class LayerManager
{
    private ShowLayerGroupData _showLayer01Data;
    public void ShowLayer01(LayerGroupType layerGroupType, Action<LayerGroup> onDone = null)
    {
        _showLayer01Data = LayerGroupBuilder.Build(layerGroupType, LayerType.Layer01);
        _showLayer01Data.OnInitData = SetupDataLayer01;
        _showLayer01Data.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showLayer01Data);
        return;

        void SetupDataLayer01(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.Layer01, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }
    private ShowLayerGroupData _showLayer02Data;
    public void ShowLayer02(LayerGroupType layerGroupType, Action<LayerGroup> onDone = null)
    {
        _showLayer02Data ??= LayerGroupBuilder.Build(layerGroupType, LayerType.Layer02);
        _showLayer02Data.OnInitData = SetupDataLayer02;
        _showLayer02Data.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showLayer02Data);
        return;

        void SetupDataLayer02(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.Layer02, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }
}
