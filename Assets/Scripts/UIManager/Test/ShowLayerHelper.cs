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
        if (_showLayer01Data == null)
        {
            _showLayer01Data = LayerGroupBuilder.Build(layerGroupType, LayerType.Layer01);
            _showLayer01Data.AddLayer(LayerType.Layer02).AddLayer(LayerType.Layer03);
            _showLayer01Data.OnInitData = SetupDataLayer01;
            _showLayer01Data.OnShowComplete = onDone;
        }
        ShowGroupLayerAsync(_showLayer01Data);

        void SetupDataLayer01(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.Layer01, out var layerBase))
            {
                layerBase.InitData();
                /*for (int i = -100000; i < 100000; i++)
                {
                    var t = Mathf.Abs(i);
                }*/
            }
        }
    }
    private ShowLayerGroupData _showLayer02Data;
    public void ShowLayer02(LayerGroupType layerGroupType, Action<LayerGroup> onDone = null)
    {
        if (_showLayer02Data == null)
        {
            _showLayer02Data ??= LayerGroupBuilder.Build(layerGroupType, LayerType.Layer02);
            _showLayer02Data.OnInitData = SetupDataLayer02;
            _showLayer02Data.OnShowComplete = onDone;
        }
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
    private ShowLayerGroupData _showLayer03Data;
    public void ShowLayer03(LayerGroupType layerGroupType, Action<LayerGroup> onDone = null)
    {
        if (_showLayer03Data == null)
        {
            _showLayer03Data ??= LayerGroupBuilder.Build(layerGroupType, LayerType.Layer03);
            _showLayer03Data.OnInitData = SetupDataLayer03;
            _showLayer03Data.OnShowComplete = onDone;
        }
        
        ShowGroupLayerAsync(_showLayer03Data);
        return;

        void SetupDataLayer03(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.Layer03, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }
    private ShowLayerGroupData _showLayer04Data;
    public void ShowLayer04(LayerGroupType layerGroupType, Action<LayerGroup> onDone = null)
    {
        if (_showLayer04Data == null)
        {
            _showLayer04Data ??= LayerGroupBuilder.Build(layerGroupType, LayerType.Layer04);
            _showLayer04Data.OnInitData = SetupDataLayer04;
            _showLayer04Data.OnShowComplete = onDone;
        }
        ShowGroupLayerAsync(_showLayer04Data);
        return;

        void SetupDataLayer04(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.Layer04, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }
    private ShowLayerGroupData _showLayer05Data;
    public void ShowLayer05(LayerGroupType layerGroupType, Action<LayerGroup> onDone = null)
    {
        if (_showLayer05Data == null)
        {
            _showLayer05Data = LayerGroupBuilder.Build(layerGroupType, LayerType.Layer05);
            _showLayer05Data.AddLayer(LayerType.Layer02);
            _showLayer05Data.AddLayer(LayerType.Layer03);
            _showLayer05Data.OnInitData = SetupDataLayer05;
            _showLayer05Data.OnShowComplete = onDone;
        }
        ShowGroupLayerAsync(_showLayer05Data);
        return;

        void SetupDataLayer05(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.Layer05, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }
    private ShowLayerGroupData _showLayer06Data;
    public void ShowLayer06(LayerGroupType layerGroupType, Action<LayerGroup> onDone = null)
    {
        if (_showLayer06Data == null)
        {
            _showLayer06Data ??= LayerGroupBuilder.Build(layerGroupType, LayerType.Layer06);
            _showLayer06Data.OnInitData = SetupDataLayer06;
            _showLayer06Data.OnShowComplete = onDone;
        }
        ShowGroupLayerAsync(_showLayer06Data);
        return;

        void SetupDataLayer06(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.Layer06, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }
}
