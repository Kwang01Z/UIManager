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

    public int GetSortingOrder()
    {
        return _sortOrders.Count > 0 ? _sortOrders.Last() : 0;
    }

    protected virtual void Reset()
    {
        if(!canvas) canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;
        if(!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
    }
    protected virtual void Awake()
    {
        canvasGroup.SetActive(false);
    }

    public virtual async UniTask ShowLayerAsync(CancellationToken cancellationToken)
    {
        canvasGroup.SetActive(true);
    }

    public virtual async UniTask HideLayerAsync(CancellationToken cancellationToken)
    {
        canvasGroup.SetActive(false);
    }
    public virtual async UniTask CloseLayerAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        await HideLayerAsync(cancellationToken);
        if(force) _sortOrders.Clear();
        var order = -10000;
        if (_sortOrders.Count > 1)
        {
            _sortOrders.RemoveAt(_sortOrders.Count - 1);
            order = _sortOrders.Last();
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

    public List<LayerType> LayerTypes => _layerBases.Keys.ToList();
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
    public LayerBase GetLayerBase(LayerType layerType)
    {
        return _layerBases.GetValueOrDefault(layerType);
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
    public async UniTask ShowGroupAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<UniTask>();
        foreach (var layerBase in _layerBases.Values)
        {
            tasks.Add(layerBase.ShowLayerAsync(cancellationToken));
        }
        await UniTask.WhenAll(tasks);
    }
}