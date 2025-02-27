using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteScrollVisibleHorizontal : IInfiniteScrollVisible
{
    private Vector2 _contentAnchor;
    private float _viewportWidth;
    public bool IsVisible(InfiniteScrollPlaceHolder placeHolder, InfiniteScrollData scrollData)
    {
        _contentAnchor = scrollData.ContentAnchor;
        _viewportWidth = scrollData.ViewportWidth;
        return CalculateVisibleHorizontal(placeHolder);
    }
    
    private bool CalculateVisibleHorizontal(InfiniteScrollPlaceHolder placeHolder)
    {
        if (placeHolder.BaseElement == null) return false;
        bool overLeft = placeHolder.AnchoredPosition.x + placeHolder.ItemWidth * (1 - placeHolder.Pivot.x)
                        >= Mathf.Abs(_contentAnchor.x);
        bool overRight = placeHolder.AnchoredPosition.x - placeHolder.ItemWidth * placeHolder.Pivot.x
                         <= Mathf.Abs(_contentAnchor.x) + _viewportWidth;
        return overLeft && overRight;
    }
}
