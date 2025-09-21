using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        var anchorPosY = placeHolder.AnchoredPosition.y;
        var pivotY = placeHolder.Pivot.y;
        var itemHeight = placeHolder.ItemHeight;
        bool belowTop = anchorPosY - itemHeight * pivotY
                        <= -_contentAnchor.y;
        bool overBottom = anchorPosY + itemHeight * (1 - pivotY)
                          >= (_contentAnchor.y + _viewportHeight) * -1;
        return belowTop && overBottom;
    }
    
}