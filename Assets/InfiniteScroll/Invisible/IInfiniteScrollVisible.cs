using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInfiniteScrollVisible
{
    public bool IsVisible(InfiniteScrollPlaceHolder placeHolder, InfiniteScrollData scrollData);
}
