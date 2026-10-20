using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(RectTransform))]
public class LayerBase : MonoBehaviour
{
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected CanvasGroup canvasGroup;

    public CanvasGroup CanvasGroup => canvasGroup;
    public RectTransform RectTransform => transform as RectTransform;
    private List<int> _sortOrders = new();
    private bool _renderingEnabled = true;

    internal void SetVisible(bool visible)
    {
        float alpha = visible ? 1f : 0f;
        if (canvasGroup.alpha != alpha) canvasGroup.alpha = alpha;
        if (canvasGroup.blocksRaycasts != visible) canvasGroup.blocksRaycasts = visible;
        if (canvasGroup.interactable != visible) canvasGroup.interactable = visible;
        bool render = visible && _renderingEnabled;
        if (canvas.enabled != render) canvas.enabled = render;
    }

    internal void SetRenderingEnabled(bool enabled)
    {
        _renderingEnabled = enabled;
        bool render = enabled && IsActive();
        if (canvas.enabled != render) canvas.enabled = render;
    }

    protected virtual void Reset()
    {
        canvas ??= GetComponent<Canvas>();
        canvas.overrideSorting = true;

        canvasGroup ??= GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    public int GetSortingOrder()
    {
        return _sortOrders.Count > 0 ? _sortOrders[^1] : 0;
    }

    public virtual void InitData()
    {

    }

    public UnityEvent OnShowLayer = new();
    public UnityEvent OnHideLayer = new();
    public virtual void ShowLayerAsync()
    {
        SetVisible(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        OnShowLayer?.Invoke();
    }

    public void ShowLayerWithoutEvent()
    {
        SetVisible(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
    }

    public virtual void HideLayerAsync()
    {
        SetVisible(false);
        OnHideLayer?.Invoke();
    }
    public virtual void CloseLayerAsync(bool force = false)
    {
        var order = -10000;
        if (!force && _sortOrders.Count > 1)
        {
            _sortOrders.RemoveAt(_sortOrders.Count - 1);
            order = _sortOrders[^1];
            OnRemoveFromStack();
        }
        else
        {
            _sortOrders.Clear();
            OnStackClear();
        }
        SetSortOrder(order, false);
        if(order <= 0) HideLayerAsync();
    }
    public virtual void SetSortOrder(int order, bool save = true)
    {
        if (canvas.sortingOrder != order) canvas.sortingOrder = order;
        if (save && (_sortOrders.Count == 0 || _sortOrders[^1] < order))
        {
            _sortOrders.Add(order);
            OnAddToStack();
        }
    }

    // Only the manager reconciles ownership; standalone CloseLayerAsync remains supported.
    internal void SynchronizeSortOrders(List<int> orders)
    {
        int previousCount = _sortOrders.Count;
        if (previousCount == orders.Count)
        {
            bool unchanged = true;
            for (int i = 0; i < orders.Count; i++)
            {
                if (_sortOrders[i] == orders[i]) continue;
                unchanged = false;
                break;
            }
            if (unchanged)
            {
                SetSortOrder(previousCount > 0 ? _sortOrders[^1] : -10000, false);
                return;
            }
        }
        _sortOrders.Clear();
        for (int i = 0; i < orders.Count; i++) _sortOrders.Add(orders[i]);
        SetSortOrder(_sortOrders.Count > 0 ? _sortOrders[^1] : -10000, false);
        for (int i = previousCount; i < orders.Count; i++) OnAddToStack();
        for (int i = orders.Count; i < previousCount; i++) OnRemoveFromStack();
    }

    public bool IsActive()
    {
        return canvasGroup.alpha > 0;
    }

    public virtual void OnAddToStack()
    {
    }

    public virtual void OnRemoveFromStack()
    {
    }

    public virtual void OnStackClear()
    {
    }

    public void SetHighLight(bool highlight)
    {
        SetSortOrder(highlight ? 32767 : (_sortOrders.Count > 0 ? _sortOrders[^1] : -10000), false);
        bool isActive = IsActive();
        SetVisible(isActive);
    }
}
public class LayerGroup
{
    private Dictionary<LayerType, LayerBase> _layerBases = new (4);
    public Dictionary<LayerType, LayerBase> LayerBases => _layerBases;

    public List<LayerType> LayerTypes => new (_layerBases.Keys);
    public void CopyLayerTypesTo(List<LayerType> destination)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        destination.Clear();
        foreach (var type in _layerBases.Keys) destination.Add(type);
    }
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
