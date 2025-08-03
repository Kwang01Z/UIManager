using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IFS_PlaceHolder
{
    private Vector2 _anchoredPosition;
    private IFS_Element _element;
    private object _data;
    private Vector4D _margin;

    public float ItemHeight { get;private set; }
    public float ItemWidth { get;private set; }
    public IFS_Element BaseElement { get; private set; }
    public bool IsVisible { get; private set; }
    public RectTransform BaseRectTransform => BaseElement?.RectTransform;
    public object Data => _data;
    public Vector2 AnchoredPosition => _anchoredPosition;
    public Vector2 Pivot { get;private set; }
    public bool IsStretchHeight;
    public bool IsStretchWidth;
    public Vector2 RootAnchoredPosition;

    public IFS_PlaceHolder(IFS_Element element, object data)
    {
        BaseElement = element;
        _data = data;
        if (element == null) return;
        var rectTransform = element.RectTransform;
        IsStretchHeight = rectTransform.IsStretchHeight();
        IsStretchWidth = rectTransform.IsStretchWidth();
        ItemHeight = rectTransform.rect.height;
        ItemWidth = rectTransform.rect.width;
        Pivot = rectTransform.pivot;
        RootAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void SetPositionData(Vector2 anchoredPosition, Vector4D margin)
    {
        var newPosition = anchoredPosition;
        newPosition.x += Pivot.x * ItemWidth;
        newPosition.y += (Pivot.y - 1)  * ItemHeight;
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

        _element = PoolHolder.Instance.Get(BaseElement,parent) as IFS_Element;
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
