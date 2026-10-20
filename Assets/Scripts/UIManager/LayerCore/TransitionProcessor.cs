using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public enum LayerAnimType
{
    None = 0,               // Giữ nguyên (không chạy animation)
    SlideRightToLeft = 1,   // Trượt từ Phải sang Trái
    SlideLeftToRight = 2,   // Trượt từ Trái sang Phải
    SlideBottomToTop = 3,   // Trượt từ Dưới lên Trên
    SlideTopToBottom = 4,   // Trượt từ Trên xuống Dưới
    ZoomIn = 5,             // Zoom từ 0.7 -> 1.0 (Alpha 0 -> 1)
    ZoomOut = 6,            // Zoom từ 1.0 -> 0.7 (Alpha 1 -> 0)
    FadeIn = 7,             // Fade alpha từ 0 -> 1
    FadeOut = 8             // Fade alpha từ 1 -> 0
}

public struct TransitionRule
{
    public LayerAnimType NewGroupAnim;
    public LayerAnimType OldGroupAnim;

    public TransitionRule(LayerAnimType newGroupAnim, LayerAnimType oldGroupAnim)
    {
        NewGroupAnim = newGroupAnim;
        OldGroupAnim = oldGroupAnim;
    }
}

public static class TransitionMappingBoard
{
    private static readonly Dictionary<TransitionStyle, TransitionRule> Rules = new()
    {
        { TransitionStyle.None,             new TransitionRule(LayerAnimType.None,             LayerAnimType.None) },
        { TransitionStyle.SlideRightToLeft, new TransitionRule(LayerAnimType.SlideRightToLeft, LayerAnimType.SlideRightToLeft) },
        { TransitionStyle.SlideLeftToRight, new TransitionRule(LayerAnimType.SlideLeftToRight, LayerAnimType.SlideLeftToRight) },
        { TransitionStyle.SlideBottomToTop, new TransitionRule(LayerAnimType.SlideBottomToTop, LayerAnimType.None) },
        { TransitionStyle.SlideTopToBottom, new TransitionRule(LayerAnimType.SlideTopToBottom, LayerAnimType.None) },
        { TransitionStyle.ZoomInOut,        new TransitionRule(LayerAnimType.ZoomIn,           LayerAnimType.None) },
        { TransitionStyle.Fade,             new TransitionRule(LayerAnimType.FadeIn,           LayerAnimType.None) },
    };

    public static TransitionRule GetRule(TransitionStyle style)
    {
        if (Rules.TryGetValue(style, out var rule))
        {
            return rule;
        }
        return new TransitionRule(LayerAnimType.None, LayerAnimType.None);
    }
}

public static class TransitionProcessor
{
    private const float DEFAULT_DURATION = 0.35f;

    public static async UniTask ProcessTransitionAsync(
        List<LayerBase> oldLayersToHide,
        LayerGroup newGroup,
        TransitionStyle style,
        HashSet<LayerType> sharedLayerTypes = null,
        float duration = DEFAULT_DURATION,
        CancellationToken cancellationToken = default)
    {
        if (style == TransitionStyle.None) return;

        TransitionRule rule = TransitionMappingBoard.GetRule(style);
        bool hasAnyMotion = false;

        // 1. Animate NEW layers (excluding shared layers)
        if (newGroup != null && rule.NewGroupAnim != LayerAnimType.None)
        {
            foreach (var kvp in newGroup.LayerBases)
            {
                var layerType = kvp.Key;
                var newLayer = kvp.Value;
                if (!newLayer) continue;

                // Skip shared layers
                if (sharedLayerTypes != null && sharedLayerTypes.Contains(layerType)) continue;

                if (AnimateSingleLayer(newLayer, rule.NewGroupAnim, isNewLayer: true, duration, cancellationToken))
                {
                    hasAnyMotion = true;
                }
            }
        }

        // 2. Animate OLD layers (excluding shared layers)
        if (oldLayersToHide != null && rule.OldGroupAnim != LayerAnimType.None)
        {
            for (int i = 0; i < oldLayersToHide.Count; i++)
            {
                var oldLayer = oldLayersToHide[i];
                if (!oldLayer) continue;

                if (AnimateSingleLayer(oldLayer, rule.OldGroupAnim, isNewLayer: false, duration, cancellationToken))
                {
                    hasAnyMotion = true;
                }
            }
        }

        if (hasAnyMotion)
        {
            await UniTask.WaitForSeconds(duration, cancellationToken: cancellationToken);
        }

        // Reset positions & scale after animation completes (for clean state)
        if (oldLayersToHide != null)
        {
            for (int i = 0; i < oldLayersToHide.Count; i++)
            {
                var oldLayer = oldLayersToHide[i];
                if (!oldLayer) continue;
                if (oldLayer.RectTransform)
                {
                    oldLayer.RectTransform.anchoredPosition = Vector2.zero;
                    oldLayer.RectTransform.localScale = Vector3.one;
                }
            }
        }
    }

    private static bool AnimateSingleLayer(LayerBase layer, LayerAnimType animType, bool isNewLayer, float duration, CancellationToken cancellationToken)
    {
        if (!layer || animType == LayerAnimType.None) return false;

        var rectTransform = layer.RectTransform;
        var canvasGroup = layer.CanvasGroup;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        bool ranMotion = false;

        switch (animType)
        {
            case LayerAnimType.SlideRightToLeft:
                if (rectTransform)
                {
                    float startX = isNewLayer ? screenWidth : 0f;
                    float endX = isNewLayer ? 0f : -screenWidth;
                    rectTransform.anchoredPosition = new Vector2(startX, rectTransform.anchoredPosition.y);
                    LMotion.Create(startX, endX, duration)
                        .WithEase(Ease.OutCubic)
                        .BindToAnchoredPositionX(rectTransform);
                    ranMotion = true;
                }
                break;

            case LayerAnimType.SlideLeftToRight:
                if (rectTransform)
                {
                    float startX = isNewLayer ? -screenWidth : 0f;
                    float endX = isNewLayer ? 0f : screenWidth;
                    rectTransform.anchoredPosition = new Vector2(startX, rectTransform.anchoredPosition.y);
                    LMotion.Create(startX, endX, duration)
                        .WithEase(Ease.OutCubic)
                        .BindToAnchoredPositionX(rectTransform);
                    ranMotion = true;
                }
                break;

            case LayerAnimType.SlideBottomToTop:
                if (rectTransform)
                {
                    float startY = isNewLayer ? -screenHeight : 0f;
                    float endY = isNewLayer ? 0f : screenHeight;
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startY);
                    LMotion.Create(startY, endY, duration)
                        .WithEase(Ease.OutCubic)
                        .BindToAnchoredPositionY(rectTransform);
                    ranMotion = true;
                }
                break;

            case LayerAnimType.SlideTopToBottom:
                if (rectTransform)
                {
                    float startY = isNewLayer ? screenHeight : 0f;
                    float endY = isNewLayer ? 0f : -screenHeight;
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startY);
                    LMotion.Create(startY, endY, duration)
                        .WithEase(Ease.OutCubic)
                        .BindToAnchoredPositionY(rectTransform);
                    ranMotion = true;
                }
                break;

            case LayerAnimType.ZoomIn:
                if (rectTransform)
                {
                    Vector3 startScale = isNewLayer ? new Vector3(0.7f, 0.7f, 1f) : Vector3.one;
                    Vector3 endScale = isNewLayer ? Vector3.one : new Vector3(1.3f, 1.3f, 1f);
                    rectTransform.localScale = startScale;
                    LMotion.Create(startScale, endScale, duration)
                        .WithEase(isNewLayer ? Ease.OutBack : Ease.InCubic)
                        .BindToLocalScale(rectTransform);
                    ranMotion = true;
                }
                if (canvasGroup)
                {
                    float startAlpha = isNewLayer ? 0f : 1f;
                    float endAlpha = isNewLayer ? 1f : 0f;
                    canvasGroup.alpha = startAlpha;
                    LMotion.Create(startAlpha, endAlpha, duration)
                        .WithEase(Ease.OutCubic)
                        .BindToAlpha(canvasGroup);
                    ranMotion = true;
                }
                break;

            case LayerAnimType.ZoomOut:
                if (rectTransform)
                {
                    Vector3 startScale = isNewLayer ? new Vector3(1.3f, 1.3f, 1f) : Vector3.one;
                    Vector3 endScale = isNewLayer ? Vector3.one : new Vector3(0.7f, 0.7f, 1f);
                    rectTransform.localScale = startScale;
                    LMotion.Create(startScale, endScale, duration)
                        .WithEase(isNewLayer ? Ease.OutBack : Ease.InCubic)
                        .BindToLocalScale(rectTransform);
                    ranMotion = true;
                }
                if (canvasGroup)
                {
                    float startAlpha = isNewLayer ? 0f : 1f;
                    float endAlpha = isNewLayer ? 1f : 0f;
                    canvasGroup.alpha = startAlpha;
                    LMotion.Create(startAlpha, endAlpha, duration)
                        .WithEase(Ease.OutCubic)
                        .BindToAlpha(canvasGroup);
                    ranMotion = true;
                }
                break;

            case LayerAnimType.FadeIn:
                if (canvasGroup)
                {
                    canvasGroup.alpha = 0f;
                    LMotion.Create(0f, 1f, duration)
                        .WithEase(Ease.OutCubic)
                        .BindToAlpha(canvasGroup);
                    ranMotion = true;
                }
                break;

            case LayerAnimType.FadeOut:
                if (canvasGroup)
                {
                    canvasGroup.alpha = 1f;
                    LMotion.Create(1f, 0f, duration)
                        .WithEase(Ease.InCubic)
                        .BindToAlpha(canvasGroup);
                    ranMotion = true;
                }
                break;
        }

        return ranMotion;
    }
}
