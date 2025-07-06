using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInfiniteScrollCursor
{
    public Vector2 CalculateAnchoredPosition(List<InfiniteScrollPlaceHolder> placeHolders, InfiniteScrollData scrollData);
}
