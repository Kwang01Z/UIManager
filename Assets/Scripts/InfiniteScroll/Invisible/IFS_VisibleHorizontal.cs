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
        bool overLeft = placeHolder.AnchoredPosition.x + placeHolder.ItemWidth * (1 - placeHolder.Pivot.x)
                        >= Math_Utility.FastAbs(_contentAnchor.x);
        bool overRight = placeHolder.AnchoredPosition.x - placeHolder.ItemWidth * placeHolder.Pivot.x
                         <= Math_Utility.FastAbs(_contentAnchor.x) + _viewportWidth;
        return overLeft && overRight;
    }
}
