using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteScrollPlaceHolder
{
    private Vector2 _anchoredPosition;
    private InfiniteScrollElement _element;
    private object _data;
    private Vector4D _margin;

    public float ItemHeight { get;private set; }
    public float ItemWidth { get;private set; }
    public InfiniteScrollElement BaseElement { get; private set; }
    public bool IsVisible { get; private set; }
    public RectTransform BaseRectTransform => BaseElement?.RectTransform;
    public object Data => _data;
    public Vector2 AnchoredPosition => _anchoredPosition;
    public Vector2 Pivot { get;private set; }

    public InfiniteScrollPlaceHolder(InfiniteScrollElement element, object data)
    {
        BaseElement = element;
        _data = data;
        ItemHeight = BaseElement.RectTransform.rect.height;
        ItemWidth = BaseElement.RectTransform.rect.width;
        Pivot = BaseElement.RectTransform.pivot;
    }

    public void SetPositionData(Vector2 anchoredPosition, Vector4D margin)
    {
        var newPosition = anchoredPosition;
        newPosition.x += BaseRectTransform.pivot.x * BaseRectTransform.rect.width;
        newPosition.y += (BaseRectTransform.pivot.y - 1)  * BaseRectTransform.rect.height;
        _anchoredPosition = newPosition;
        _margin = margin;
    }

    public void SetVisible(bool visible)
    {
        IsChangeState = visible ^ IsVisible;
        if(!IsChangeState) return;
        IsVisible = visible;
    }

    public bool IsChangeState { get; private set; }

    public void UpdateData(Transform parent)
    {
        if (!IsVisible)
        {
            ReleaseData();
            return;
        }

        _element = PoolHolder.Instance.Get(BaseElement,parent) as InfiniteScrollElement;
        _element?.SetupData(_anchoredPosition, _margin, _data);
    }

    public void ReleaseData()
    {
        if (_element)
        {
            PoolHolder.Instance.Release(_element);
            _element = null;
        }
    }
}
