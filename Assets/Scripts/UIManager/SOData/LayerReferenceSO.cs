using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "UIManager/LayerReferenceSO", fileName = "LayerReferenceSO", order = 1)]
public class LayerReferenceSO : ScriptableObject
{
    public List<LayerReferenceData> layerReferenceList;
    public Dictionary<LayerType, LayerBase> LayerBaseDictionary = new ();

    public LayerBase GetLayerBase(LayerType layerType)
    {
        if (LayerBaseDictionary.TryGetValue(layerType, out var layerBase)) return layerBase;
        return null;
    }

    public void InitLayerBase()
    {
        foreach (var layerReferenceData in layerReferenceList)
        {
            LayerBaseDictionary.TryAdd(layerReferenceData.layerType, layerReferenceData.layerBase);
        }
    }
}

[Serializable]
public class LayerReferenceData
{
    public LayerType layerType;
    public LayerBase layerBase;
}