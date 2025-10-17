using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(RectTransform))]
public class LayerBase : MonoBehaviour
{
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected CanvasGroup canvasGroup;

    [SerializeField] public bool keepPreActive;
    private List<int> _sortOrders = new ();

    public bool IsActive()
    {
        return canvas.sortingOrder > 0;
    }

    protected virtual void Reset()
    {
        canvas ??= GetComponent<Canvas>();
        canvas.overrideSorting = true;
        
        canvasGroup ??= GetComponent<CanvasGroup>();
        canvasGroup.SetActive(false);
    }

    protected virtual void OnValidate()
    {
        if(!keepPreActive) gameObject.SetActive(false);
    }

    public int GetSortingOrder()
    {
        return _sortOrders.Count > 0 ? _sortOrders[^1] : 0;
    }

    public virtual void InitData()
    {
        
    }

    public ActionSealed OnShowLayer = new();
    public ActionSealed OnHideLayer = new();
    public virtual void ShowLayerAsync()
    {
        canvasGroup.SetActive(true);
        if(!gameObject.activeInHierarchy) gameObject.SetActive(true);
        OnShowLayer?.Invoke();
    }

    public virtual void HideLayerAsync()
    {
        canvasGroup.SetActive(false);
        OnHideLayer?.Invoke();
    }
    public virtual void CloseLayerAsync(bool force = false)
    {
        HideLayerAsync();
        if(force) _sortOrders.Clear();
        var order = -10000;
        if (_sortOrders.Count > 1)
        {
            _sortOrders.RemoveAt(_sortOrders.Count - 1);
            order = _sortOrders[^1];
        }
        SetSortOrder(order, false);
    }
    public virtual void SetSortOrder(int order, bool save = true)
    {
        canvas.sortingOrder = order;
        if(save && (_sortOrders.Count == 0 || _sortOrders[^1] < order)) _sortOrders.Add(order);
    }
}
public class LayerGroup
{
    private Dictionary<LayerType, LayerBase> _layerBases = new (4);

    public List<LayerType> LayerTypes => new (_layerBases.Keys);
    public void CloseGroupAsync()
    {
        foreach (var layerBase in _layerBases.Values)
        {
            layerBase.CloseLayerAsync();
        }
    }
    public void AddLayer(LayerType layerType ,LayerBase layerBase)
    {
        _layerBases.Add(layerType, layerBase);
    }
    public bool GetLayerBase(LayerType layerType , out LayerBase layerBase)
    {
        layerBase = _layerBases.GetValueOrDefault(layerType);
        return layerBase;
    }

    public void SetSortOrder(int order)
    {
        int subOrder = 1;
        foreach (var layerBase in _layerBases.Values)
        {
            layerBase.SetSortOrder(order + subOrder);
            subOrder++;
        }
    }
    public void ShowGroupAsync()
    {
        foreach (var layerBase in _layerBases.Values)
        {
            layerBase.ShowLayerAsync();
        }
    }
}