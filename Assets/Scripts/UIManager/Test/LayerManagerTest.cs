using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LayerManagerTest : MonoBehaviour
{
    private void Awake()
    {
        Application.targetFrameRate = 120;
    }

    async void Start()
    {
        var showData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.Layer01);
        var layerGroup = await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            showData, 
            onInitData: async (group) =>
            {
                // Setup data for layer
                if (group.GetLayerBase(LayerType.Layer01, out var layer))
                {
                    // Initialize layer với custom data
                    Debug.Log("Setting up Layer01 data...");
                }
            },
            transition: TransitionProfile.ScaleTransition
        );
    }

    
}
