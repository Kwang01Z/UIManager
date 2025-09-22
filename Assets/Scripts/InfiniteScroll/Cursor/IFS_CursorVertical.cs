using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IFS_CursorVertical : IIFS_Cursor
{
    private List<IFS_PlaceHolder> _placeHolders;
    private IFS_Data _scrollData;
    private Vector4D Padding => _scrollData.Padding;
    private Vector2 Spacing => _scrollData.Spacing;
    private float ViewPortWidth => _scrollData.ViewportWidth;
    public Vector2 CalculateAnchoredPosition(List<IFS_PlaceHolder> placeHolders, IFS_Data scrollData)
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
            if (placeHolder is IFS_PlaceSpace space)
            {
                if (i >= _placeHolders.Count - 1)
                {
                    contentHeight += space.Spacing;
                    break;
                }
                var nextHolder = _placeHolders[i + 1];
                
                var rowWidth = RowWidth(nextHolder);
                cursorPos.x = (ViewPortWidth - rowWidth) / 2f;
                cursorPos.y -= Spacing.y + space.Spacing;
                rowItemIndex = 1;
                contentHeight += space.Spacing;
                continue;
            }
            bool isStretchWidth = placeHolder.IsStretchWidth;
            var itemAnchor = CalculateNewAnchor(isStretchWidth,placeHolder,cursorPos,rowItemIndex);
            _placeHolders[i].SetPositionData(itemAnchor, Padding);
            
            contentHeight = Mathf.Max(contentHeight, Mathf.Abs(cursorPos.y) + placeHolder.ItemHeight
                                    + Padding.bottom);

            if (i >= _placeHolders.Count - 1) continue;
            var currentElement = _placeHolders[i];
            var nextElement = _placeHolders[i + 1];
            bool isStretchOne = currentElement.IsStretchWidth || nextElement.IsStretchWidth;
            if (isStretchOne)
            {
                InitNewRow(currentElement,nextElement, ref cursorPos, ref rowItemIndex);
                continue;
            }
            TryInitNewRow(currentElement, nextElement, ref cursorPos, ref rowItemIndex);
        }

        return new Vector2(_scrollData.ContentSize.x, contentHeight);
    }
    private Vector2 CalculateNewAnchor(bool isStretchWidth, IFS_PlaceHolder placeHolder, Vector2 cursorPos, int rowItemIndex)
    {
        var newAnchor = isStretchWidth 
            ? new Vector2((Padding.left - Padding.right) / 2f 
                          + (Mathf.Abs(placeHolder.ItemWidth) 
                             * placeHolder.Pivot.x 
                             + placeHolder.RootAnchoredPosition.x)
                ,cursorPos.y) 
            : cursorPos;
            
        float itemWidth = isStretchWidth 
            ? ViewPortWidth - Padding.left - Padding.right + placeHolder.ItemWidth
            : placeHolder.ItemWidth;
        newAnchor.x += (rowItemIndex - 1) * (itemWidth + Spacing.x);
        return newAnchor;
    }
    private void TryInitNewRow(IFS_PlaceHolder holder, IFS_PlaceHolder nextElement
        , ref Vector2 cursorPos, ref int rowItemIndex)
    {
        if (nextElement is IFS_PlaceSpace)
        {
            cursorPos.y -= Spacing.y + holder.ItemHeight;
            return;
        }
        
        var elementRect = holder.BaseRectTransform;
        if(!elementRect) return;
        bool isStretchWidth = holder.IsStretchWidth;
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
    private void InitNewRow(IFS_PlaceHolder currentHolder , IFS_PlaceHolder nextHolder, ref Vector2 cursorPos, ref int rowItemIndex)
    {
        if (nextHolder is IFS_PlaceSpace)
        {
            cursorPos.y -= Spacing.y + currentHolder.ItemHeight;
            return;
        }

        var rowWidth = RowWidth(nextHolder);
        cursorPos.x = (ViewPortWidth - rowWidth) / 2f;
        cursorPos.y -= Spacing.y + currentHolder.ItemHeight;
        rowItemIndex = 1;
    }

    private int MaxItemPerRow(IFS_PlaceHolder holder)
    {
        bool isStretchWidth = holder.IsStretchWidth;
        if (isStretchWidth) return 1;
        var marginWidth = Mathf.Max(Padding.left, Padding.right) * 2f;
        int maxItemPerRow = Mathf.FloorToInt((ViewPortWidth - marginWidth + Spacing.x) 
                                             / (holder.ItemWidth + Spacing.x));
        return holder.BaseElement.ElementType == IFS_ElementType.Flexible
            ? maxItemPerRow : Mathf.Min(holder.BaseElement.NumberFixed, maxItemPerRow);
    }

    private float RowWidth(IFS_PlaceHolder holder)
    {
        int maxItemPerRow = MaxItemPerRow(holder);
        bool isStretchWidth = holder.IsStretchWidth;
        float itemWidth = isStretchWidth 
            ? ViewPortWidth - Padding.left - Padding.right + holder.ItemWidth
            : holder.ItemWidth;
        return itemWidth * maxItemPerRow + Spacing.x * (maxItemPerRow - 1);
    }
}
