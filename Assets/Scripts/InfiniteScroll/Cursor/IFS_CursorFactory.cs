using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class IFS_CursorFactory 
{
    public static IIFS_Cursor Build(GridLayoutGroup.Axis axisType)
    {
        switch (axisType)
        {
            case GridLayoutGroup.Axis.Horizontal:
                return new IFS_CursorHorizontal();
            case GridLayoutGroup.Axis.Vertical:
                return new IFS_CursorVertical();
        }

        return new IifsCursorDefault();
    }
}
public class IifsCursorDefault : IIFS_Cursor
{
    public Vector2 CalculateAnchoredPosition(List<IFS_PlaceHolder> placeHolders, IFS_Data scrollData)
    {
        return scrollData.ContentSize;
    }
}
