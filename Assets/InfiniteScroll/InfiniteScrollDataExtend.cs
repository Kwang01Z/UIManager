using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class InfiniteScrollData
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GridLayoutGroup.Axis scrollType;
    [SerializeField] private Vector4D padding;
    [SerializeField] private Vector2 spacing;
    [SerializeField] private GridLayoutGroup.Corner startCorner;
    [SerializeField] private TextAnchor childAlignment;
    
    RectTransform ContentRect => scrollRect.content;
    private void OnValidate()
    {
        if (scrollRect)
        {
            scrollRect.horizontal = scrollType == GridLayoutGroup.Axis.Horizontal;
            scrollRect.vertical = scrollType == GridLayoutGroup.Axis.Vertical;
        }
    }
}
