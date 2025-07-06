using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class InfiniteScrollCursorFactory 
{
    public static IInfiniteScrollCursor Build(GridLayoutGroup.Axis axisType)
    {
        switch (axisType)
        {
            case GridLayoutGroup.Axis.Horizontal:
                return new InfiniteScrollCursorHorizontal();
            case GridLayoutGroup.Axis.Vertical:
                return new InfiniteScrollCursorVertical();
        }

        return new InfiniteScrollCursorDefault();
    }
}
public class InfiniteScrollCursorDefault : IInfiniteScrollCursor
{
    public Vector2 CalculateAnchoredPosition(List<InfiniteScrollPlaceHolder> placeHolders, InfiniteScrollData scrollData)
    {
        return scrollData.ContentSize;
    }
}
