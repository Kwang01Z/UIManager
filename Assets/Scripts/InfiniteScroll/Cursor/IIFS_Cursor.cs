using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IIFS_Cursor
{
    public Vector2 CalculateAnchoredPosition(List<IFS_PlaceHolder> placeHolders, IFS_Data scrollData);
}
