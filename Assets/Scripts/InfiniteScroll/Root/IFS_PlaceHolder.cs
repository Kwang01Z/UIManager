using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IFS_PlaceHolder
{
    private IFS_Element _element;
    private Vector4D _margin;

    public float ItemHeight;
    public float ItemWidth;
    public IFS_Element BaseElement;
    public bool IsVisible;
    public RectTransform BaseRectTransform;
    public object Data;
    public Vector2 AnchoredPosition;
    public Vector2 Pivot;
    public bool IsStretchHeight;
    public bool IsStretchWidth;
    public Vector2 RootAnchoredPosition;
    public string ObjectKey;

    public IFS_PlaceHolder()
    {
    }

    public IFS_PlaceHolder(IFS_Element element, object data)
    {
        BaseElement = element;
        Data = data;
        BaseRectTransform = BaseElement?.RectTransform;
        if (element == null) return;
        var rectTransform = element.RectTransform;
        IsStretchHeight = rectTransform.IsStretchHeight();
        IsStretchWidth = rectTransform.IsStretchWidth();
        ItemHeight = rectTransform.rect.height;
        ItemWidth = rectTransform.rect.width;
        Pivot = rectTransform.pivot;
        RootAnchoredPosition = rectTransform.anchoredPosition;
        ObjectKey = PoolHolder.GetKey(BaseElement);
    }

    public void SetPositionData(Vector2 anchoredPosition, Vector4D margin)
    {
        var newPosition = anchoredPosition;
        newPosition.x += Pivot.x * ItemWidth;
        newPosition.y += (Pivot.y - 1)  * ItemHeight;
        AnchoredPosition = newPosition;
        _margin = margin;
    }

    public void SetVisible(bool visible)
    {
        IsChangeState = visible ^ IsVisible;
        if(!IsChangeState) return;
        IsVisible = visible;
    }

    public bool IsChangeState;

    public void UpdateData(Transform parent)
    {
        if (!IsVisible)
        {
            ReleaseData();
            return;
        }

        _element = PoolHolder.Instance.Get(BaseElement,parent,customKey:ObjectKey) as IFS_Element;
        _element?.SetupData(AnchoredPosition, _margin, Data);
    }

    public void ReleaseData()
    {
        if (_element)
        {
            PoolHolder.Instance.Release(_element, customKey: ObjectKey);
            _element = null;
        }
    }
}

public class IFS_PlaceSpace : IFS_PlaceHolder
{
    public float Spacing;
    public IFS_PlaceSpace(float spacing)
    {
        Spacing = spacing;
    }
}
