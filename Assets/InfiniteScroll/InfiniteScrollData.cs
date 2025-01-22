using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public partial class InfiniteScrollData : MonoBehaviour
{
    List<InfiniteScrollPlaceHolder> _placeHolders = new List<InfiniteScrollPlaceHolder>();

    public void InitData(List<InfiniteScrollPlaceHolder> placeHolders)
    {
        ClearData();
        AddDataRange(placeHolders);
    }
    
    public void ClearData()
    {
        Parallel.ForEach(_placeHolders, (placeHolder) =>
        {
            placeHolder.ReleaseData();
        });
        _placeHolders.Clear();
    }

    public void AddDataRange(List<InfiniteScrollPlaceHolder> placeHolders)
    {
        _placeHolders.AddRange(placeHolders);
        
        if(_placeHolders.Count == 0) return;
        CalculateAnchoredPosition();
        UpdateData();
    }

    
    private void UpdateData()
    {
        SetupBaseData();
        Parallel.ForEach(_placeHolders, (placeHolder) =>
        {
            bool isVisible = IsVisible(placeHolder);
            placeHolder.SetVisible(isVisible);
        });
        var placeHolderChange = _placeHolders.FindAll(x => x.IsChangeState);
        foreach (var infiniteScrollPlaceHolder in placeHolderChange)
        {
            infiniteScrollPlaceHolder.UpdateData(scrollRect.content);
        }
    }

    private void SetupBaseData()
    {
        _tempContentAnchor = ContentRect.anchoredPosition;
        ViewportWidth = ViewportRect.rect.width;
        ViewportHeight = ViewportRect.rect.height;
    }

    private void CalculateAnchoredPosition()
    {
        switch (scrollType)
        {
            case GridLayoutGroup.Axis.Horizontal:
                CalculateAnchoredPositionHorizontal();
                break;
            case GridLayoutGroup.Axis.Vertical:
                CalculateAnchoredPositionVertical();
                break;
        }
    }

    private void CalculateAnchoredPositionVertical()
    {
        if(_placeHolders.Count == 0) return;
        
        var cursorPos = new Vector2(padding.left, padding.top);
        var rowFistWidth = RowWidth(_placeHolders[0]);
        cursorPos.x = (ViewportRect.rect.width - rowFistWidth) / 2f;
        float rowItemIndex = 1;
        float contentHeight = 0f;
        
        for (var i = 0; i < _placeHolders.Count; i++)
        {
            var placeHolder = _placeHolders[i];

            bool isStretchWidth = RectTransformUtility.IsStretchWidth(placeHolder.BaseRectTransform);
            var itemAnchor = CalculateNewAnchor(isStretchWidth,placeHolder);
            _placeHolders[i].SetPositionData(itemAnchor, padding);
            
            contentHeight = Mathf.Max(contentHeight, Mathf.Abs(cursorPos.y) + placeHolder.BaseRectTransform.rect.height);
            
            if (i < _placeHolders.Count - 1)
            {
                var currentElement = _placeHolders[i];
                var nextElement = _placeHolders[i + 1];
                bool isNewType = currentElement.BaseElement.GetHashCode() != nextElement.BaseElement.GetHashCode();
                if (isNewType)
                {
                    InitNewRow(nextElement);
                    continue;
                }
                TryInitNewRow(currentElement, nextElement);
            }
        }

        UpdateContentSize(new Vector2(ContentRect.sizeDelta.x, contentHeight));

        Vector2 CalculateNewAnchor(bool isStretchWidth, InfiniteScrollPlaceHolder placeHolder)
        {
            var newAnchor = isStretchWidth 
                ? new Vector2((padding.left - padding.right) / 2f 
                              + (Mathf.Abs(placeHolder.BaseRectTransform.rect.width) 
                                 * placeHolder.BaseRectTransform.pivot.x 
                                 + placeHolder.BaseRectTransform.anchoredPosition.x)
                    ,cursorPos.y) 
                : cursorPos;
            
            float itemWidth = isStretchWidth 
                ? ViewportRect.rect.width - padding.left - padding.right + placeHolder.BaseRectTransform.rect.width
                : placeHolder.BaseRectTransform.rect.width;
            newAnchor.x += (rowItemIndex - 1) * (itemWidth + spacing.x);
            return newAnchor;
        }

        void TryInitNewRow(InfiniteScrollPlaceHolder holder, InfiniteScrollPlaceHolder nextElement)
        {
            var elementRect = holder.BaseRectTransform;
            if(!elementRect) return;
            bool isStretchWidth = RectTransformUtility.IsStretchWidth(elementRect);
            bool isOnlyPerRow = holder.BaseElement.ElementType == IFS_ElementType.Fixed 
                                && holder.BaseElement.NumberFixed == 1;
            
            if (isStretchWidth || isOnlyPerRow)
            {
                InitNewRow(nextElement);
                return;
            }

            if (rowItemIndex >= MaxItemPerRow(holder))
            {
                InitNewRow(nextElement);
                return;
            }
            rowItemIndex++;
        }

        void InitNewRow(InfiniteScrollPlaceHolder holder)
        {
            var rowWidth = RowWidth(holder);
            cursorPos.x = (ViewportRect.rect.width - rowWidth) / 2f;
            cursorPos.y -= spacing.y + holder.BaseRectTransform.rect.height;
            rowItemIndex = 1;
        }

        int MaxItemPerRow(InfiniteScrollPlaceHolder holder)
        {
            bool isStretchWidth = RectTransformUtility.IsStretchWidth(holder.BaseRectTransform);
            if (isStretchWidth) return 1;
            var marginWidth = Mathf.Max(padding.left, padding.right) * 2f;
            int maxItemPerRow = Mathf.FloorToInt((ViewportRect.rect.width - marginWidth + spacing.x) 
                                                 / (holder.BaseRectTransform.rect.width + spacing.x));
            return holder.BaseElement.ElementType == IFS_ElementType.Flexible
            ? maxItemPerRow : Mathf.Min(holder.BaseElement.NumberFixed, maxItemPerRow);
        }

        float RowWidth(InfiniteScrollPlaceHolder holder)
        {
            int maxItemPerRow = MaxItemPerRow(holder);
            bool isStretchWidth = RectTransformUtility.IsStretchWidth(holder.BaseRectTransform);
            float itemWidth = isStretchWidth 
                ? ViewportRect.rect.width - padding.left - padding.right + holder.BaseRectTransform.rect.width
                : holder.BaseRectTransform.rect.width;
            return itemWidth * maxItemPerRow + spacing.x * (maxItemPerRow - 1);
        }
    }

    private void CalculateAnchoredPositionHorizontal()
    {
        if(_placeHolders.Count == 0) return;
        
        var cursorPos = new Vector2(padding.left, padding.top);
        var colFistHeight = ColumnHeight(_placeHolders[0]);
        cursorPos.y = (ViewportRect.rect.height - colFistHeight) / 2f;
        float colItemIndex = 1;
        float contentWidth = 0f;
        
        for (var i = 0; i < _placeHolders.Count; i++)
        {
            var placeHolder = _placeHolders[i];

            bool isStretchHeight = RectTransformUtility.IsStretchHeight(placeHolder.BaseRectTransform);
            var itemAnchor = CalculateNewAnchor(isStretchHeight,placeHolder);
            _placeHolders[i].SetPositionData(itemAnchor, padding);
            
            contentWidth = Mathf.Max(contentWidth, Mathf.Abs(cursorPos.x) + placeHolder.BaseRectTransform.rect.width);
            
            if (i < _placeHolders.Count - 1)
            {
                var currentElement = _placeHolders[i];
                var nextElement = _placeHolders[i + 1];
                bool isNewType = currentElement.BaseElement.GetHashCode() != nextElement.BaseElement.GetHashCode();
                if (isNewType)
                {
                    InitNewColumn(nextElement);
                    continue;
                }
                TryInitNewRow(currentElement, nextElement);
            }
        }

        UpdateContentSize(new Vector2(contentWidth,ContentRect.sizeDelta.y));

        Vector2 CalculateNewAnchor(bool isStretchHeight, InfiniteScrollPlaceHolder placeHolder)
        {
            var newAnchor = isStretchHeight 
                ? new Vector2(cursorPos.x,(padding.top - padding.bottom) / 2f 
                                          + (Mathf.Abs(placeHolder.BaseRectTransform.rect.height) 
                                             * placeHolder.BaseRectTransform.pivot.y
                                             + placeHolder.BaseRectTransform.anchoredPosition.y)) 
                : cursorPos;
            
            float itemHeight = isStretchHeight 
                ? ViewportRect.rect.height - padding.top - padding.bottom + placeHolder.BaseRectTransform.rect.height
                : placeHolder.BaseRectTransform.rect.height;
            newAnchor.y += (colItemIndex - 1) * (itemHeight + spacing.y);
            return newAnchor;
        }

        void TryInitNewRow(InfiniteScrollPlaceHolder holder, InfiniteScrollPlaceHolder nextElement)
        {
            var elementRect = holder.BaseRectTransform;
            if(!elementRect) return;
            bool isStretchWidth = RectTransformUtility.IsStretchWidth(elementRect);
            bool isOnlyPerRow = holder.BaseElement.ElementType == IFS_ElementType.Fixed 
                                && holder.BaseElement.NumberFixed == 1;
            
            if (isStretchWidth || isOnlyPerRow)
            {
                InitNewColumn(nextElement);
                return;
            }

            if (colItemIndex >= MaxItemPerColumn(holder))
            {
                InitNewColumn(nextElement);
                return;
            }
            colItemIndex++;
        }

        void InitNewColumn(InfiniteScrollPlaceHolder holder)
        {
            var columnHeight = ColumnHeight(holder);
            cursorPos.y = (ViewportRect.rect.height - columnHeight) / 2f;
            cursorPos.x -= spacing.x + holder.BaseRectTransform.rect.height;
            colItemIndex = 1;
        }

        int MaxItemPerColumn(InfiniteScrollPlaceHolder holder)
        {
            bool isStretchHeight = RectTransformUtility.IsStretchHeight(holder.BaseRectTransform);
            if (isStretchHeight) return 1;
            var marginHeight = Mathf.Max(padding.top, padding.bottom) * 2f;
            int maxItemPerColumn = Mathf.FloorToInt((ViewportRect.rect.height - marginHeight + spacing.y) 
                                                 / (holder.BaseRectTransform.rect.height + spacing.y));
            return holder.BaseElement.ElementType == IFS_ElementType.Flexible
            ? maxItemPerColumn : Mathf.Min(holder.BaseElement.NumberFixed, maxItemPerColumn);
        }

        float ColumnHeight(InfiniteScrollPlaceHolder holder)
        {
            int maxItemPerColumn = MaxItemPerColumn(holder);
            bool isStretchHeight = RectTransformUtility.IsStretchHeight(holder.BaseRectTransform);
            float itemHeight = isStretchHeight 
                ? ViewportRect.rect.height - padding.top - padding.bottom + holder.BaseRectTransform.rect.height
                : holder.BaseRectTransform.rect.height;
            return itemHeight * maxItemPerColumn + spacing.x * (maxItemPerColumn - 1);
        }
    }

    

    bool IsVisible(InfiniteScrollPlaceHolder placeHolder)
    {
        switch (scrollType)
        {
            case GridLayoutGroup.Axis.Horizontal:
                return CalculateVisibleHorizontal(placeHolder);
            case GridLayoutGroup.Axis.Vertical:
                return CalculateVisibleVertical(placeHolder);
        }

        return true;
    }

    private bool CalculateVisibleHorizontal(InfiniteScrollPlaceHolder placeHolder)
    {
        if (placeHolder.BaseElement == null) return false;
        bool overLeft = Mathf.Abs(placeHolder.AnchoredPosition.x 
                                  - placeHolder.ItemWidth)
                        >= _tempContentAnchor.x;
        bool overRight = Mathf.Abs(placeHolder.AnchoredPosition.x)
                         <= _tempContentAnchor.x + ViewportWidth;
        return overLeft && overRight;
    }

    private bool CalculateVisibleVertical(InfiniteScrollPlaceHolder placeHolder)
    {
        if (placeHolder.BaseElement == null) return false;

        bool overTop = Mathf.Abs(placeHolder.AnchoredPosition.y 
                                 - placeHolder.ItemHeight)
                       >= _tempContentAnchor.y;
        bool overBottom = Mathf.Abs(placeHolder.AnchoredPosition.y)
                          <= _tempContentAnchor.y + ViewportHeight;
        return overTop && overBottom;
    }
}
