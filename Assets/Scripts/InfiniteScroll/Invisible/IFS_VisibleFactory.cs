using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class IFS_VisibleFactory
{
    public static IIFS_Visible Build(GridLayoutGroup.Axis axisType)
    {
        switch (axisType)
        {
            case GridLayoutGroup.Axis.Horizontal:
                return new IFS_VisibleHorizontal();
            case GridLayoutGroup.Axis.Vertical:
                return new IFS_VisibleVertical();
        }
        return new IifsVisibleDefault();
    }
}

public class IifsVisibleDefault : IIFS_Visible
{
    public bool IsVisible(IFS_PlaceHolder placeHolder, IFS_Data scrollData)
    {
        return true;
    }
}
