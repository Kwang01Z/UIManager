using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IFS_VisibleHorizontal : IIFS_Visible
{
    private Vector2 _contentAnchor;
    private float _viewportWidth;
    public bool IsVisible(IFS_PlaceHolder placeHolder, IFS_Data scrollData)
    {
        _contentAnchor = scrollData.ContentAnchor;
        _viewportWidth = scrollData.ViewportWidth;
        return CalculateVisibleHorizontal(placeHolder);
    }
    
    private bool CalculateVisibleHorizontal(IFS_PlaceHolder placeHolder)
    {
        var anchorPosX = placeHolder.AnchoredPosition.x;
        var itemWidth = placeHolder.ItemWidth;
        var pivotX = placeHolder.Pivot.x;
        var contentAnchorXAbs = -(_contentAnchor.x);
        bool overLeft = anchorPosX + itemWidth * (1 - pivotX)
                        >= contentAnchorXAbs;
        bool overRight = anchorPosX - itemWidth * pivotX
                         <= contentAnchorXAbs + _viewportWidth;
        return overLeft && overRight;
    }
}
