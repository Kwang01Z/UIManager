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

    private Vector2 _contentAnchor;
    void UpdateData()
    {
        _contentAnchor = ContentRect.anchoredPosition;
        /*Parallel.ForEach(_placeHolders, (placeHolder) =>
        {
            bool isVisible = IsVisible(placeHolder);
            placeHolder.SetVisible(isVisible);
        });*/
        foreach (var placeHolder in _placeHolders)
        {
            bool isVisible = IsVisible(placeHolder);
            placeHolder.SetVisible(isVisible);
        }

        var placeHolderChange = _placeHolders.FindAll(x => x.IsChangeState);
        foreach (var infiniteScrollPlaceHolder in placeHolderChange)
        {
            infiniteScrollPlaceHolder.UpdateData(scrollRect.content);
        }
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
        
    }

    

    bool IsVisible(InfiniteScrollPlaceHolder placeHolder)
    {
        switch (scrollType)
        {
            case GridLayoutGroup.Axis.Horizontal:
                return true;
            case GridLayoutGroup.Axis.Vertical:
                return CalculateVisibleVertical(placeHolder);
        }

        return true;
    }

    private bool CalculateVisibleVertical(InfiniteScrollPlaceHolder placeHolder)
    {
        if (placeHolder.BaseRectTransform == null) return false;

        bool overTop = Mathf.Abs(placeHolder.AnchoredPosition.y 
                                 - placeHolder.BaseRectTransform.rect.height)
                       >= _contentAnchor.y;
        bool overBottom = Mathf.Abs(placeHolder.AnchoredPosition.y)
                          <= _contentAnchor.y + ViewportRect.rect.height;
        return overTop && overBottom;
    }
}
