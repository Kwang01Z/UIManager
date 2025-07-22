using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(RectTransform))]
public class LayerBase : MonoBehaviour
{
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected CanvasGroup canvasGroup;

    protected virtual void Reset()
    {
        if(!canvas) canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;
        if(!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual void ShowLayer()
    {
        canvasGroup.SetActive(true);
    }

    public virtual void HideLayer()
    {
        canvasGroup.SetActive(false);
    }
    public virtual void CloseLayer()
    {
        SetSortOrder(-10000);
        canvasGroup.SetActive(false);
    }
    public virtual void SetSortOrder(int order)
    {
        canvas.sortingOrder = order;
    }
}
public class LayerGroup
{
    public LayerType Type;
    public List<LayerBase> Layers = new List<LayerBase>();

    public void HideGroup()
    {
        foreach (var layerBase in Layers)
        {
            layerBase.CloseLayer();
        }
    }
}