using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "UIManager/LayerReferenceSO", fileName = "LayerReferenceSO", order = 1)]
public class LayerReferenceSO : ScriptableObject
{
    public List<LayerReferenceData> layerReferenceList;
    private Dictionary<LayerType, LayerBase> _layerReferenceDic;

    public Dictionary<LayerType, LayerBase> LayerBaseDictionary
    {
        get
        {
            if (_layerReferenceDic == null || _layerReferenceDic.Count == 0)
            {
                _layerReferenceDic = new Dictionary<LayerType, LayerBase>();
                foreach (var layerReferenceData in layerReferenceList)
                {
                    _layerReferenceDic[layerReferenceData.layerType] = layerReferenceData.layerBase;
                }
            }
            return _layerReferenceDic;
        }
    }

    public LayerBase GetLayerBase(LayerType layerType)
    {
        return LayerBaseDictionary.TryGetValue(layerType, out var layerBase) ? layerBase : null;
    }
}

[Serializable]
public class LayerReferenceData
{
    public LayerType layerType;
    public LayerBase layerBase;
}
