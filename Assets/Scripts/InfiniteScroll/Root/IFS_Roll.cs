using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public partial class IFS_Data
{
    [Button]
    public void JumpToTop()
    {
        if (scrollRect == null) return;
        
        if (ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            // For vertical scroll, top means anchoredPosition.y = 0
            scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, 1f);
        }
        else
        {
            // For horizontal scroll, top means anchoredPosition.x = 0
            scrollRect.normalizedPosition = new Vector2(0f, scrollRect.normalizedPosition.y);
        }
    }
    [Button]
    public void JumpToBottom()
    {
        if (scrollRect == null) return;
        if (ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            // For vertical scroll, bottom means anchoredPosition.y = negative max
            scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, 0f);
        }
        else
        {
            // For horizontal scroll, bottom means anchoredPosition.x = negative max
            scrollRect.normalizedPosition = new Vector2(1f, scrollRect.normalizedPosition.y);
        }
    }
    
    public Vector2 GetPlaceholderTopAnchorPosition(IFS_PlaceHolder holder)
    {
        if (holder == null || scrollRect == null) return Vector2.zero;
        
        Vector2 targetAnchor = ContentRect.anchoredPosition;
        
        if (ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            // For vertical scroll, position the holder at the top of the viewport
            // The top of the holder should align with the top of the viewport
            float holderTop = holder.AnchoredPosition.y + (1-holder.Pivot.y) * holder.ItemHeight;
            targetAnchor.y = -holderTop;
        }
        else
        {
            // For horizontal scroll, position the holder at the left of the viewport
            // The left of the holder should align with the left of the viewport
            float holderLeft = holder.AnchoredPosition.x - (holder.Pivot.x * holder.ItemWidth);
            targetAnchor.x =- holderLeft;
        }
        
        return ValidateContentAnchor(targetAnchor);
    }
    
    public Vector2 GetPlaceholderBottomAnchorPosition(IFS_PlaceHolder holder)
    {
        if (holder == null || scrollRect == null) return Vector2.zero;
        
        Vector2 targetAnchor = ContentRect.anchoredPosition;
        
        if (ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            // For vertical scroll, position the holder at the bottom of the viewport
            // The bottom of the holder should align with the bottom of the viewport
            float holderBottom = holder.AnchoredPosition.y - (holder.ItemHeight);
            
            float offset = -holderBottom + ViewportHeight;
            targetAnchor.y = offset;
        }
        else
        {
            // For horizontal scroll, position the holder at the right of the viewport
            // The right of the holder should align with the right of the viewport
            float holderRight = holder.AnchoredPosition.x + (1 - holder.Pivot.x) * holder.ItemWidth;
            
            float offset = holderRight - ViewportWidth;
            targetAnchor.x = -offset;
        }
        
        return ValidateContentAnchor(targetAnchor);
    }
    
    public Vector2 GetPlaceholderMiddleAnchorPosition(IFS_PlaceHolder holder)
    {
        if (holder == null || scrollRect == null) return Vector2.zero;
        
        Vector2 targetAnchor = ContentRect.anchoredPosition;
        
        if (ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            // For vertical scroll, position the holder in the middle of the viewport
            // The center of the holder should align with the center of the viewport
            float holderTop = holder.AnchoredPosition.y + (1-holder.Pivot.y) * holder.ItemHeight;
            targetAnchor.y = -holderTop - ViewportHeight / 2f + holder.ItemHeight / 2f;
        }
        else
        {
            // For horizontal scroll, position the holder in the middle of the viewport
            // The center of the holder should align with the center of the viewport
            float holderLeft = holder.AnchoredPosition.x - (holder.Pivot.x * holder.ItemWidth);
            targetAnchor.x =- (holderLeft + holder.ItemWidth / 2f - ViewportWidth / 2f);
        }
        
        return ValidateContentAnchor(targetAnchor);
    }

    private Vector2 ValidateContentAnchor(Vector2 result)
    {
        if (ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            result.y = Mathf.Clamp(result.y, 0, ContentSize.y);
        }
        else
        {
            result.x = Mathf.Clamp(result.x, -ContentSize.x, 0);
        }
        return result;
    }

    public void JumpToPlaceholderTop(IFS_PlaceHolder holder)
    {
        if (holder == null || scrollRect == null) return;
        
        Vector2 targetAnchor = GetPlaceholderTopAnchorPosition(holder);
        SetContentAnchor(targetAnchor);
    }
    
    public void JumpToPlaceholderBottom(IFS_PlaceHolder holder)
    {
        if (holder == null || scrollRect == null) return;
        
        Vector2 targetAnchor = GetPlaceholderBottomAnchorPosition(holder);
        SetContentAnchor(targetAnchor);
    }
    
    public void JumpToPlaceholderMiddle(IFS_PlaceHolder holder)
    {
        if (holder == null || scrollRect == null) return;
        
        Vector2 targetAnchor = GetPlaceholderMiddleAnchorPosition(holder);
        SetContentAnchor(targetAnchor);
    }

    public void JumpToTopElementIndex(int index)
    {
        var element = _placeHolders[index];
        JumpToPlaceholderTop(element);
    }

    public void JumpToBottomElementIndex(int index)
    {
        var element = _placeHolders[index];
        JumpToPlaceholderBottom(element);
    }

    public void JumpToMiddleElementIndex(int index)
    {
        var element = _placeHolders[index];
        JumpToPlaceholderMiddle(element);
    }

    public int TestIndex;

    [Button]
    public void JumpToElementTopTest()
    {
        JumpToTopElementIndex(TestIndex);
    }
    [Button]
    public void JumpToElementBottomTest()
    {
        JumpToBottomElementIndex(TestIndex);
    }
    [Button]
    public void JumpToElementMiddleTest()
    {
        JumpToMiddleElementIndex(TestIndex);
    }
}
