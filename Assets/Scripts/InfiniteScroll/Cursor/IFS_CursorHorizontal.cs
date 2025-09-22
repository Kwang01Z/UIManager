using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IFS_CursorHorizontal : IIFS_Cursor
{
    private List<IFS_PlaceHolder> _placeHolders;
    private IFS_Data _scrollData;
    private Vector4D Padding => _scrollData.Padding;
    private Vector2 Spacing => _scrollData.Spacing;
    private float ViewPortHeight => _scrollData.ViewportHeight;
    public Vector2 CalculateAnchoredPosition(List<IFS_PlaceHolder> placeHolders, IFS_Data scrollData)
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
            if (placeHolder is IFS_PlaceSpace space)
            {
                if (i >= _placeHolders.Count - 1)
                {
                    contentWidth += space.Spacing;
                    break;
                }
                var nextElement = _placeHolders[i + 1];
                var columnHeight = ColumnHeight(nextElement);
                cursorPos.y = -(ViewPortHeight - columnHeight) / 2f;
                cursorPos.x += Spacing.x + space.Spacing;
                colItemIndex = 1;
                contentWidth += space.Spacing;
                continue;
            }

            bool isStretchHeight = placeHolder.IsStretchHeight;
            var itemAnchor = CalculateNewAnchor(isStretchHeight,placeHolder, cursorPos, colItemIndex);
            _placeHolders[i].SetPositionData(itemAnchor, Padding);
            
            contentWidth = Mathf.Max(contentWidth, Mathf.Abs(cursorPos.x) + placeHolder.ItemWidth
                + Padding.right);
            
            if (i < _placeHolders.Count - 1)
            {
                var currentElement = _placeHolders[i];
                var nextElement = _placeHolders[i + 1];
                bool isStretchOne = currentElement.IsStretchHeight || nextElement.IsStretchHeight;
                if (isStretchOne)
                {
                    InitNewColumn(currentElement,nextElement, ref cursorPos, ref colItemIndex);
                    continue;
                }
                TryInitNewCol(currentElement, nextElement, ref cursorPos, ref colItemIndex);
            }
        }
        return new Vector2(contentWidth, _scrollData.ContentSize.y);
    }
    private Vector2 CalculateNewAnchor(bool isStretchHeight, IFS_PlaceHolder placeHolder, Vector2 cursorPos,int colItemIndex)
    {
        var newAnchor = isStretchHeight 
            ? new Vector2(cursorPos.x,-(Padding.top - Padding.bottom) / 2f 
                                      + (Mathf.Abs(placeHolder.ItemHeight) 
                                         * placeHolder.Pivot.y
                                         + placeHolder.RootAnchoredPosition.y)) 
            : cursorPos;
            
        float itemHeight = isStretchHeight 
            ? ViewPortHeight - Padding.top - Padding.bottom + placeHolder.ItemHeight
            : placeHolder.ItemHeight;
        newAnchor.y -= (colItemIndex - 1) * (itemHeight + Spacing.y);
        return newAnchor;
    }

    void TryInitNewCol(IFS_PlaceHolder holder, IFS_PlaceHolder nextElement, ref Vector2 cursorPos, ref int colItemIndex)
    {
        if (nextElement is IFS_PlaceSpace)
        {
            cursorPos.x += Spacing.x + holder.ItemWidth;
            return;
        }
        
        var elementRect = holder.BaseRectTransform;
        if(!elementRect) return;
        bool isStretchWidth = holder.IsStretchWidth;
        bool isOnlyPerRow = holder.BaseElement.ElementType == IFS_ElementType.Fixed 
                            && holder.BaseElement.NumberFixed == 1;
            
        if (isStretchWidth || isOnlyPerRow)
        {
            InitNewColumn(holder,nextElement, ref cursorPos, ref colItemIndex);
            return;
        }

        if (colItemIndex >= MaxItemPerColumn(holder))
        {
            InitNewColumn(holder,nextElement, ref cursorPos, ref colItemIndex);
            return;
        }
        colItemIndex++;
    }

    private void InitNewColumn(IFS_PlaceHolder currentHolder,IFS_PlaceHolder nextHolder, ref Vector2 cursorPos, ref int colItemIndex)
    {
        if (nextHolder is IFS_PlaceSpace)
        {
            cursorPos.x += Spacing.x + currentHolder.ItemWidth;
            return;
        }
        var columnHeight = ColumnHeight(nextHolder);
        cursorPos.y = -(ViewPortHeight - columnHeight) / 2f;
        cursorPos.x += Spacing.x + currentHolder.ItemWidth;
        colItemIndex = 1;
    }

    private float ColumnHeight(IFS_PlaceHolder holder)
    {
        int maxItemPerColumn = MaxItemPerColumn(holder);
        bool isStretchHeight = holder.IsStretchHeight;
        float itemHeight = isStretchHeight 
            ? ViewPortHeight - Padding.top - Padding.bottom + holder.ItemHeight
            : holder.ItemHeight;
        return itemHeight * maxItemPerColumn + Spacing.y * (maxItemPerColumn - 1);
    }
    private int MaxItemPerColumn(IFS_PlaceHolder holder)
    {
        bool isStretchHeight = holder.IsStretchHeight;
        if (isStretchHeight) return 1;
        var marginHeight = Mathf.Max(Padding.top, Padding.bottom) * 2f;
        int maxItemPerColumn = Mathf.FloorToInt((ViewPortHeight - marginHeight + Spacing.y) 
                                                / (holder.ItemHeight + Spacing.y));
        return holder.BaseElement.ElementType == IFS_ElementType.Flexible
            ? maxItemPerColumn : Mathf.Min(holder.BaseElement.NumberFixed, maxItemPerColumn);
    }
}
