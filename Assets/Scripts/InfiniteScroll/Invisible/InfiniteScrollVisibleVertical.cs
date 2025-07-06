using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteScrollVisibleVertical : IInfiniteScrollVisible
{
    private Vector2 _contentAnchor;
    private float _viewportHeight;

    public bool IsVisible(InfiniteScrollPlaceHolder placeHolder, InfiniteScrollData scrollData)
    {
        _contentAnchor = scrollData.ContentAnchor;
        _viewportHeight = scrollData.ViewportHeight;
        return CalculateVisibleVertical(placeHolder);
    }

    private bool CalculateVisibleVertical(InfiniteScrollPlaceHolder placeHolder)
    {
        if (placeHolder.BaseElement == null) return false;

        bool belowTop = Mathf.Abs(placeHolder.AnchoredPosition.y
                                  - placeHolder.ItemHeight * placeHolder.Pivot.y)
                        >= _contentAnchor.y;
        bool overBottom = Mathf.Abs(placeHolder.AnchoredPosition.y + placeHolder.ItemHeight * (1 - placeHolder.Pivot.y))
                          <= _contentAnchor.y + _viewportHeight;
        return belowTop && overBottom;
    }
}