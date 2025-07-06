using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteScrollCursorHorizontal : IInfiniteScrollCursor
{
    private List<InfiniteScrollPlaceHolder> _placeHolders;
    private InfiniteScrollData _scrollData;
    private Vector4D Padding => _scrollData.Padding;
    private Vector2 Spacing => _scrollData.Spacing;
    private float ViewPortHeight => _scrollData.ViewportHeight;
    public Vector2 CalculateAnchoredPosition(List<InfiniteScrollPlaceHolder> placeHolders, InfiniteScrollData scrollData)
    {
        _placeHolders = placeHolders;
        _scrollData = scrollData;
        return CalculateAnchoredPositionHorizontal();
    }
    
    private Vector2 CalculateAnchoredPositionHorizontal()
    {
        if(_placeHolders.Count == 0) return _scrollData.ContentSize;
        
        var cursorPos = new Vector2(Padding.left, Padding.top);
        var colFistHeight = ColumnHeight(_placeHolders[0]);
        cursorPos.y = -(ViewPortHeight - colFistHeight) / 2f;
        int colItemIndex = 1;
        float contentWidth = 0f;
        
        for (var i = 0; i < _placeHolders.Count; i++)
        {
            var placeHolder = _placeHolders[i];

            bool isStretchHeight = RectTransformUtility.IsStretchHeight(placeHolder.BaseRectTransform);
            var itemAnchor = CalculateNewAnchor(isStretchHeight,placeHolder, cursorPos, colItemIndex);
            _placeHolders[i].SetPositionData(itemAnchor, Padding);
            
            contentWidth = Mathf.Max(contentWidth, Mathf.Abs(cursorPos.x) + placeHolder.BaseRectTransform.rect.width
                + Padding.right);
            
            if (i < _placeHolders.Count - 1)
            {
                var currentElement = _placeHolders[i];
                var nextElement = _placeHolders[i + 1];
                bool isNewType = currentElement.BaseElement.GetHashCode() != nextElement.BaseElement.GetHashCode();
                if (isNewType)
                {
                    InitNewColumn(nextElement, ref cursorPos, ref colItemIndex);
                    continue;
                }
                TryInitNewCol(currentElement, nextElement, ref cursorPos, ref colItemIndex);
            }
        }
        return new Vector2(contentWidth, _scrollData.ContentSize.y);
    }
    private Vector2 CalculateNewAnchor(bool isStretchHeight, InfiniteScrollPlaceHolder placeHolder, Vector2 cursorPos,int colItemIndex)
    {
        var newAnchor = isStretchHeight 
            ? new Vector2(cursorPos.x,-(Padding.top - Padding.bottom) / 2f 
                                      + (Mathf.Abs(placeHolder.BaseRectTransform.rect.height) 
                                         * placeHolder.BaseRectTransform.pivot.y
                                         + placeHolder.BaseRectTransform.anchoredPosition.y)) 
            : cursorPos;
            
        float itemHeight = isStretchHeight 
            ? ViewPortHeight - Padding.top - Padding.bottom + placeHolder.BaseRectTransform.rect.height
            : placeHolder.BaseRectTransform.rect.height;
        newAnchor.y -= (colItemIndex - 1) * (itemHeight + Spacing.y);
        return newAnchor;
    }

    void TryInitNewCol(InfiniteScrollPlaceHolder holder, InfiniteScrollPlaceHolder nextElement, ref Vector2 cursorPos, ref int colItemIndex)
    {
        var elementRect = holder.BaseRectTransform;
        if(!elementRect) return;
        bool isStretchWidth = RectTransformUtility.IsStretchWidth(elementRect);
        bool isOnlyPerRow = holder.BaseElement.ElementType == IFS_ElementType.Fixed 
                            && holder.BaseElement.NumberFixed == 1;
            
        if (isStretchWidth || isOnlyPerRow)
        {
            InitNewColumn(nextElement, ref cursorPos, ref colItemIndex);
            return;
        }

        if (colItemIndex >= MaxItemPerColumn(holder))
        {
            InitNewColumn(nextElement, ref cursorPos, ref colItemIndex);
            return;
        }
        colItemIndex++;
    }

    private void InitNewColumn(InfiniteScrollPlaceHolder holder, ref Vector2 cursorPos, ref int colItemIndex)
    {
        var columnHeight = ColumnHeight(holder);
        cursorPos.y = -(ViewPortHeight - columnHeight) / 2f;
        cursorPos.x += Spacing.x + holder.BaseRectTransform.rect.width;
        colItemIndex = 1;
    }

    private float ColumnHeight(InfiniteScrollPlaceHolder holder)
    {
        int maxItemPerColumn = MaxItemPerColumn(holder);
        bool isStretchHeight = RectTransformUtility.IsStretchHeight(holder.BaseRectTransform);
        float itemHeight = isStretchHeight 
            ? ViewPortHeight - Padding.top - Padding.bottom + holder.BaseRectTransform.rect.height
            : holder.BaseRectTransform.rect.height;
        return itemHeight * maxItemPerColumn + Spacing.y * (maxItemPerColumn - 1);
    }
    private int MaxItemPerColumn(InfiniteScrollPlaceHolder holder)
    {
        bool isStretchHeight = RectTransformUtility.IsStretchHeight(holder.BaseRectTransform);
        if (isStretchHeight) return 1;
        var marginHeight = Mathf.Max(Padding.top, Padding.bottom) * 2f;
        int maxItemPerColumn = Mathf.FloorToInt((ViewPortHeight - marginHeight + Spacing.y) 
                                                / (holder.BaseRectTransform.rect.height + Spacing.y));
        return holder.BaseElement.ElementType == IFS_ElementType.Flexible
            ? maxItemPerColumn : Mathf.Min(holder.BaseElement.NumberFixed, maxItemPerColumn);
    }
}
