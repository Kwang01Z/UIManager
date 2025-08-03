using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IFS_VisibleVertical : IIFS_Visible
{
    private Vector2 _contentAnchor;
    private float _viewportHeight;

    public bool IsVisible(IFS_PlaceHolder placeHolder, IFS_Data scrollData)
    {
        _contentAnchor = scrollData.ContentAnchor;
        _viewportHeight = scrollData.ViewportHeight;
        return CalculateVisibleVertical(placeHolder);
    }

    private bool CalculateVisibleVertical(IFS_PlaceHolder placeHolder)
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