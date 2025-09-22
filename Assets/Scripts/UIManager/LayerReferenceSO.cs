using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "UIManager/LayerReferenceSO", fileName = "LayerReferenceSO", order = 1)]
public class LayerReferenceSO : ScriptableObject
{
    public List<LayerReferenceGroup> layerGroupList;
    public Dictionary<LayerType, LayerBase> LayerBaseDictionary = new Dictionary<LayerType, LayerBase>(64);

    public LayerBase GetLayerBase(LayerType layerType)
    {
        return LayerBaseDictionary.GetValueOrDefault(layerType);
    }

    public void InitLayerBase()
    {
        foreach (var group in layerGroupList)
        {
            foreach (var layerReferenceData in group.layerReferenceList)
            {
                LayerBaseDictionary.TryAdd(layerReferenceData.layerType, layerReferenceData.layerBase);
            }
        }
    }
}

[Serializable]
public class LayerReferenceGroup
{
    public string groupName;
    public List<LayerReferenceData> layerReferenceList;
}

[Serializable]
public class LayerReferenceData
{
    public LayerType layerType;
    public LayerBase layerBase;
}