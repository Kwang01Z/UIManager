using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UI_Utility
{
    #region Canvas Group

    public static void SetActive(this CanvasGroup canvasGroup, bool active)
    {
        canvasGroup.alpha = active ? 1 : 0;
        canvasGroup.blocksRaycasts = active;
        canvasGroup.interactable = active;
    }

    #endregion

    #region Rect Transform

    public static bool IsStretchWidth(this RectTransform rectTransform)
    {
        return !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x);
    }

    public static bool IsStretchHeight(this RectTransform rectTransform)
    {
        return !Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
    }

    #endregion
}
