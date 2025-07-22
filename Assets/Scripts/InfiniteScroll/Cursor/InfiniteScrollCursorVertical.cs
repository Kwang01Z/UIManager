using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteScrollCursorVertical : IInfiniteScrollCursor
{
    private List<InfiniteScrollPlaceHolder> _placeHolders;
    private InfiniteScrollData _scrollData;
    private Vector4D Padding => _scrollData.Padding;
    private Vector2 Spacing => _scrollData.Spacing;
    private float ViewPortWidth => _scrollData.ViewportWidth;
    public Vector2 CalculateAnchoredPosition(List<InfiniteScrollPlaceHolder> placeHolders, InfiniteScrollData scrollData)
    {
        _placeHolders = placeHolders;
        _scrollData = scrollData;
        return CalculateAnchoredPositionVertical();
    }
    private Vector2 CalculateAnchoredPositionVertical()
    {
        if(_placeHolders.Count == 0) return _scrollData.ContentSize;
        
        var cursorPos = new Vector2(Padding.left, -Padding.top);
        var rowFistWidth = RowWidth(_placeHolders[0]);
        cursorPos.x = (ViewPortWidth - rowFistWidth) / 2f;
        int rowItemIndex = 1;
        float contentHeight = 0;
        
        for (var i = 0; i < _placeHolders.Count; i++)
        {
            var placeHolder = _placeHolders[i];

            bool isStretchWidth = placeHolder.BaseRectTransform.IsStretchWidth();
            var itemAnchor = CalculateNewAnchor(isStretchWidth,placeHolder,cursorPos,rowItemIndex);
            _placeHolders[i].SetPositionData(itemAnchor, Padding);
            
            contentHeight = Mathf.Max(contentHeight, Mathf.Abs(cursorPos.y) + placeHolder.BaseRectTransform.rect.height
                                    + Padding.bottom);

            if (i >= _placeHolders.Count - 1) continue;
            var currentElement = _placeHolders[i];
            var nextElement = _placeHolders[i + 1];
            bool isNewType = currentElement.BaseElement.GetHashCode() != nextElement.BaseElement.GetHashCode();
            if (isNewType)
            {
                InitNewRow(currentElement,nextElement, ref cursorPos, ref rowItemIndex);
                continue;
            }
            TryInitNewRow(currentElement, nextElement, ref cursorPos, ref rowItemIndex);
        }

        return new Vector2(_scrollData.ContentSize.x, contentHeight);
    }
    private Vector2 CalculateNewAnchor(bool isStretchWidth, InfiniteScrollPlaceHolder placeHolder, Vector2 cursorPos, int rowItemIndex)
    {
        var newAnchor = isStretchWidth 
            ? new Vector2((Padding.left - Padding.right) / 2f 
                          + (Mathf.Abs(placeHolder.BaseRectTransform.rect.width) 
                             * placeHolder.BaseRectTransform.pivot.x 
                             + placeHolder.BaseRectTransform.anchoredPosition.x)
                ,cursorPos.y) 
            : cursorPos;
            
        float itemWidth = isStretchWidth 
            ? ViewPortWidth - Padding.left - Padding.right + placeHolder.BaseRectTransform.rect.width
            : placeHolder.BaseRectTransform.rect.width;
        newAnchor.x += (rowItemIndex - 1) * (itemWidth + Spacing.x);
        return newAnchor;
    }
    private void TryInitNewRow(InfiniteScrollPlaceHolder holder, InfiniteScrollPlaceHolder nextElement
        , ref Vector2 cursorPos, ref int rowItemIndex)
    {
        var elementRect = holder.BaseRectTransform;
        if(!elementRect) return;
        bool isStretchWidth = elementRect.IsStretchWidth();
        bool isOnlyPerRow = holder.BaseElement.ElementType == IFS_ElementType.Fixed 
                            && holder.BaseElement.NumberFixed == 1;
            
        if (isStretchWidth || isOnlyPerRow)
        {
            InitNewRow(holder,nextElement, ref cursorPos, ref rowItemIndex);
            return;
        }

        if (rowItemIndex >= MaxItemPerRow(holder))
        {
            InitNewRow(holder,nextElement, ref cursorPos, ref rowItemIndex);
            return;
        }
        rowItemIndex++;
    }
    private void InitNewRow(InfiniteScrollPlaceHolder currentHolder , InfiniteScrollPlaceHolder nextHolder, ref Vector2 cursorPos, ref int rowItemIndex)
    {
        var rowWidth = RowWidth(nextHolder);
        cursorPos.x = (ViewPortWidth - rowWidth) / 2f;
        cursorPos.y -= Spacing.y + currentHolder.BaseRectTransform.rect.height;
        rowItemIndex = 1;
    }

    private int MaxItemPerRow(InfiniteScrollPlaceHolder holder)
    {
        bool isStretchWidth = holder.BaseRectTransform.IsStretchWidth();
        if (isStretchWidth) return 1;
        var marginWidth = Mathf.Max(Padding.left, Padding.right) * 2f;
        int maxItemPerRow = Mathf.FloorToInt((ViewPortWidth - marginWidth + Spacing.x) 
                                             / (holder.BaseRectTransform.rect.width + Spacing.x));
        return holder.BaseElement.ElementType == IFS_ElementType.Flexible
            ? maxItemPerRow : Mathf.Min(holder.BaseElement.NumberFixed, maxItemPerRow);
    }

    private float RowWidth(InfiniteScrollPlaceHolder holder)
    {
        int maxItemPerRow = MaxItemPerRow(holder);
        bool isStretchWidth = holder.BaseRectTransform.IsStretchWidth();
        float itemWidth = isStretchWidth 
            ? ViewPortWidth - Padding.left - Padding.right + holder.BaseRectTransform.rect.width
            : holder.BaseRectTransform.rect.width;
        return itemWidth * maxItemPerRow + Spacing.x * (maxItemPerRow - 1);
    }
}
