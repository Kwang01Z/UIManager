using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
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

    private List<int> _sortOrders = new();

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

    public virtual void InitData()
    {

    }

    public UnityEvent OnShowLayer = new();
    public UnityEvent OnHideLayer = new();
    public virtual void ShowLayerAsync()
    {
        canvasGroup.SetActive(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        OnShowLayer?.Invoke();
    }

    public void ShowLayerWithoutEvent()
    {
        canvasGroup.SetActive(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
    }

    public virtual void HideLayerAsync()
    {
        canvasGroup.SetActive(false);
        OnHideLayer?.Invoke();
    }
    public virtual void CloseLayerAsync(bool force = false)
    {
        if (force)
        {
            _sortOrders.Clear();
            OnStackClear();
        }
        var order = -10000;
        if (_sortOrders.Count > 1)
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
        if(canvas.sortingOrder == order) return;
        canvas.sortingOrder = order;
        if (save && (_sortOrders.Count == 0 || _sortOrders[^1] < order))
        {
            _sortOrders.Add(order);
            OnAddToStack();
        }
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
        canvasGroup.alpha = highlight ? 1 : 0;
        canvasGroup.interactable = !highlight;
        canvasGroup.blocksRaycasts = !highlight;
        canvas.sortingOrder = highlight ? 32767 : (_sortOrders.Count > 0 ? _sortOrders[^1] : -10000);
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
