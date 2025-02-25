using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class InfiniteScrollVisibleFactory
{
    public static IInfiniteScrollVisible Build(GridLayoutGroup.Axis axisType)
    {
        switch (axisType)
        {
            case GridLayoutGroup.Axis.Horizontal:
                return new InfiniteScrollVisibleHorizontal();
            case GridLayoutGroup.Axis.Vertical:
                return new InfiniteScrollVisibleVertical();
        }
        return new InfiniteScrollVisibleDefault();
    }
}

public class InfiniteScrollVisibleDefault : IInfiniteScrollVisible
{
    public bool IsVisible(InfiniteScrollPlaceHolder placeHolder, InfiniteScrollData scrollData)
    {
        return true;
    }
}
