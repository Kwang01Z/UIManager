using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class InfiniteScrollControllerFactory
{
    public static IInfiniteScrollController Build(GridLayoutGroup.Axis axis)
    {
        switch (axis)
        {
            case GridLayoutGroup.Axis.Horizontal:
                return new InfiniteScrollControllerHorizontal();
            case GridLayoutGroup.Axis.Vertical:
                return new InfiniteScrollVertical();
        }

        return new InfiniteScrollControllerDefault();
    }
}

public class InfiniteScrollControllerDefault : IInfiniteScrollController
{
    
}
