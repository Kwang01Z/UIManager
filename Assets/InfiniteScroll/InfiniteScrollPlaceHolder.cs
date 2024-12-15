using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteScrollPlaceHolder
{
    private Vector2 _anchoredPosition;
    private InfiniteScrollElement _element;
    private object _data;

    public InfiniteScrollElement BaseElement { get; private set; }
    public bool IsVisible { get; private set; }
    public RectTransform BaseRectTransform => BaseElement?.RectTransform;
    public object Data => _data;

    public InfiniteScrollPlaceHolder(InfiniteScrollElement element, object data)
    {
        BaseElement = element;
        _data = data;
    }

    public void SetPosition(Vector2 anchoredPosition)
    {
        var newPosition = anchoredPosition;
        newPosition.x += BaseRectTransform.pivot.x * BaseRectTransform.rect.width;
        newPosition.y += (BaseRectTransform.pivot.y - 1)  * BaseRectTransform.rect.height;
        _anchoredPosition = newPosition;
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
        _element?.SetupData(_anchoredPosition, _data);
    }

    public void ReleaseData()
    {
        if (_element)
        {
            PoolHolder.Instance.Release(_element);
            _element = null;
        }

        BaseElement = null;
    }
}
