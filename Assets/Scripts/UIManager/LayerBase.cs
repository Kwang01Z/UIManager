using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(RectTransform))]
public class LayerBase : MonoBehaviour
{
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected CanvasGroup canvasGroup;

    private List<int> _sortOrders = new ();

    protected virtual void OnValidate()
    {
        gameObject.SetActive(false);
    }

    protected virtual void Reset()
    {
        canvas ??= GetComponent<Canvas>();
        canvas.overrideSorting = true;
        
        canvasGroup ??= GetComponent<CanvasGroup>();
        canvasGroup.SetActive(false);
    }
    
    public int GetSortingOrder()
    {
        return _sortOrders.Count > 0 ? _sortOrders[^1] : 0;
    }

    public virtual async UniTask ShowLayerAsync()
    {
        await UniTask.Yield();
        canvasGroup.SetActive(true);
        if(!gameObject.activeInHierarchy) gameObject.SetActive(true);
    }

    public virtual async UniTask HideLayerAsync()
    {
        await UniTask.Yield();
        canvasGroup.SetActive(false);
    }
    public virtual async UniTask CloseLayerAsync(bool force = false)
    {
        await HideLayerAsync();
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
        if(save) _sortOrders.Add(order);
    }
}
public class LayerGroup
{
    private Dictionary<LayerType, LayerBase> _layerBases = new ();

    public List<LayerType> LayerTypes => new (_layerBases.Keys);
    public async UniTask CloseGroupAsync()
    {
        var tasks = new List<UniTask>();
        foreach (var layerBase in _layerBases.Values)
        {
            tasks.Add(layerBase.CloseLayerAsync());
        }
        await UniTask.WhenAll(tasks);
    }
    public void AddLayer(LayerType layerType ,LayerBase layerBase)
    {
        _layerBases.Add(layerType, layerBase);
    }
    public bool GetLayerBase(LayerType layerType , out LayerBase layerBase)
    {
        layerBase = _layerBases.GetValueOrDefault(layerType);
        return layerBase != null;
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
    public async UniTask ShowGroupAsync()
    {
        var tasks = new List<UniTask>();
        foreach (var layerBase in _layerBases.Values)
        {
            tasks.Add(layerBase.ShowLayerAsync());
        }
        await UniTask.WhenAll(tasks);
    }
}