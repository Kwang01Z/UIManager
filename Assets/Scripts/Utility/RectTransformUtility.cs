

using UnityEngine;

public class RectTransformUtility
{
    public static bool IsStretchWidth(RectTransform rectTransform)
    {
        return !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x);
    }

    public static bool IsStretchHeight(RectTransform rectTransform)
    {
        return !Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
    }
}
