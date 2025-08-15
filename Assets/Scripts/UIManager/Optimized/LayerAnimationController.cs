using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// High-performance animation system cho layer transitions
/// Sử dụng DOTween với object pooling và batch operations
/// </summary>
public class LayerAnimationController : MonoSingleton<LayerAnimationController>
{
    [Header("Animation Settings")]
    [SerializeField] private float defaultAnimationDuration = 0.3f;
    [SerializeField] private AnimationProfile defaultProfile = AnimationProfile.Fade;
    [SerializeField] private Ease defaultEase = Ease.OutQuart;
    [SerializeField] private int maxConcurrentAnimations = 10;
    
    [Header("Performance")]
    [SerializeField] private bool enableAnimationPooling = true;
    [SerializeField] private int tweenPoolSize = 20;
    
    // Animation pools để tránh allocations
    private readonly Queue<Tween> _tweenPool = new();
    private readonly List<LayerAnimationInfo> _activeAnimations = new();
    private readonly Dictionary<LayerBase, LayerAnimationInfo> _layerAnimations = new();
    
    protected override void Awake()
    {
        base.Awake();
        InitializeAnimationSystem();
    }

    private void InitializeAnimationSystem()
    {
        // Configure DOTween for performance
        DOTween.SetTweensCapacity(tweenPoolSize * 2, tweenPoolSize);
        DOTween.defaultAutoPlay = AutoPlay.All;
        DOTween.defaultAutoKill = false;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LayerAnimationController] Initialized with pool size: {tweenPoolSize}");
        #endif
    }

    /// <summary>
    /// Animate layer show với customizable profile
    /// </summary>
    public async UniTask AnimateShowAsync(LayerBase layer, AnimationProfile profile = AnimationProfile.Default, float duration = -1f)
    {
        if (layer == null) return;
        if(!layer.gameObject.activeInHierarchy) layer.gameObject.SetActive(true);
        
        duration = duration > 0 ? duration : defaultAnimationDuration;
        profile = profile == AnimationProfile.Default ? defaultProfile : profile;
        
        await AnimateLayerAsync(layer, profile, duration, true);
    }

    /// <summary>
    /// Animate layer hide với customizable profile  
    /// </summary>
    public async UniTask AnimateHideAsync(LayerBase layer, AnimationProfile profile = AnimationProfile.Default, float duration = -1f)
    {
        if (layer == null) return;
        
        duration = duration > 0 ? duration : defaultAnimationDuration;
        profile = profile == AnimationProfile.Default ? defaultProfile : profile;
        
        await AnimateLayerAsync(layer, profile, duration, false);
    }

    /// <summary>
    /// Animate group transition với parallel execution
    /// </summary>
    public async UniTask AnimateGroupTransitionAsync(
        LayerGroup hideGroup,
        LayerGroup showGroup,
        TransitionProfile transitionProfile = TransitionProfile.Crossfade)
    {
        var animationTasks = new List<UniTask>();
        
        // Hide current group
        if (hideGroup != null)
        {
            var hideProfile = GetHideProfileFromTransition(transitionProfile);
            foreach (var layerType in hideGroup.LayerTypes)
            {
                if (hideGroup.GetLayerBase(layerType, out var layer))
                {
                    animationTasks.Add(AnimateHideAsync(layer, hideProfile.profile, hideProfile.duration));
                }
            }
        }
        
        // Show new group với delay nếu cần
        if (showGroup != null)
        {
            var showProfile = GetShowProfileFromTransition(transitionProfile);
            var showTasks = new List<UniTask>();
            
            foreach (var layerType in showGroup.LayerTypes)
            {
                if (showGroup.GetLayerBase(layerType, out var layer))
                {
                    showTasks.Add(AnimateShowAsync(layer, showProfile.profile, showProfile.duration));
                }
            }
            
            if (showProfile.delay > 0)
            {
                animationTasks.Add(DelayedAnimations(showTasks, showProfile.delay));
            }
            else
            {
                animationTasks.AddRange(showTasks);
            }
        }
        
        await UniTask.WhenAll(animationTasks);
    }

    /// <summary>
    /// Cancel animation for specific layer
    /// </summary>
    public void CancelAnimation(LayerBase layer)
    {
        if (_layerAnimations.TryGetValue(layer, out var animInfo))
        {
            animInfo.AnimationSequence?.Kill();
            CleanupAnimationInfo(animInfo);
            _layerAnimations.Remove(layer);
        }
    }

    /// <summary>
    /// Cancel all active animations
    /// </summary>
    public void CancelAllAnimations()
    {
        foreach (var animInfo in _activeAnimations)
        {
            animInfo.AnimationSequence?.Kill();
            CleanupAnimationInfo(animInfo);
        }
        
        _activeAnimations.Clear();
        _layerAnimations.Clear();
    }

    /// <summary>
    /// Get animation statistics
    /// </summary>
    public AnimationStats GetStats()
    {
        return new AnimationStats
        {
            ActiveAnimations = _activeAnimations.Count,
            PooledTweens = _tweenPool.Count,
            LayersAnimating = _layerAnimations.Count
        };
    }

    private async UniTask AnimateLayerAsync(LayerBase layer, AnimationProfile profile, float duration, bool isShow)
    {
        // Cancel existing animation nếu có
        CancelAnimation(layer);
        
        var animInfo = CreateAnimationInfo(layer, profile, duration, isShow);
        _activeAnimations.Add(animInfo);
        _layerAnimations[layer] = animInfo;
        
        try
        {
            // Setup initial state
            SetupInitialState(layer, profile, isShow);
            
            // Create animation sequence
            animInfo.AnimationSequence = CreateAnimationSequence(layer, profile, duration, isShow);
            
            // Play animation
            animInfo.AnimationSequence.Play();
            
            // Wait for completion
            await animInfo.AnimationSequence.AsyncWaitForCompletion();
            
            // Finalize state
            FinalizeAnimationState(layer, profile, isShow);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LayerAnimationController] Animation failed for {layer.name}: {e}");
        }
        finally
        {
            CleanupAnimationInfo(animInfo);
            _activeAnimations.Remove(animInfo);
            _layerAnimations.Remove(layer);
        }
    }

    private LayerAnimationInfo CreateAnimationInfo(LayerBase layer, AnimationProfile profile, float duration, bool isShow)
    {
        return new LayerAnimationInfo
        {
            Layer = layer,
            Profile = profile,
            Duration = duration,
            IsShow = isShow,
            StartTime = Time.time
        };
    }

    private void SetupInitialState(LayerBase layer, AnimationProfile profile, bool isShow)
    {
        var canvasGroup = layer.GetComponent<CanvasGroup>();
        var rectTransform = layer.transform as RectTransform;
        
        switch (profile)
        {
            case AnimationProfile.Fade:
                canvasGroup.alpha = isShow ? 0f : 1f;
                break;
                
            case AnimationProfile.SlideFromLeft:
                if (isShow)
                {
                    var pos = rectTransform.anchoredPosition;
                    pos.x = -Screen.width;
                    rectTransform.anchoredPosition = pos;
                }
                break;
                
            case AnimationProfile.SlideFromRight:
                if (isShow)
                {
                    var pos = rectTransform.anchoredPosition;
                    pos.x = Screen.width;
                    rectTransform.anchoredPosition = pos;
                }
                break;
                
            case AnimationProfile.SlideFromTop:
                if (isShow)
                {
                    var pos = rectTransform.anchoredPosition;
                    pos.y = Screen.height;
                    rectTransform.anchoredPosition = pos;
                }
                break;
                
            case AnimationProfile.SlideFromBottom:
                if (isShow)
                {
                    var pos = rectTransform.anchoredPosition;
                    pos.y = -Screen.height;
                    rectTransform.anchoredPosition = pos;
                }
                break;
                
            case AnimationProfile.Scale:
                if (isShow)
                {
                    rectTransform.localScale = Vector3.zero;
                    canvasGroup.alpha = 0f;
                }
                else
                {
                    canvasGroup.alpha = 1f;
                }
                break;
                
            case AnimationProfile.ScaleAndFade:
                if (isShow)
                {
                    rectTransform.localScale = Vector3.zero;
                    canvasGroup.alpha = 0f;
                }
                break;
        }
    }

    private Sequence CreateAnimationSequence(LayerBase layer, AnimationProfile profile, float duration, bool isShow)
    {
        var sequence = DOTween.Sequence();
        var canvasGroup = layer.GetComponent<CanvasGroup>();
        var rectTransform = layer.transform as RectTransform;
        
        switch (profile)
        {
            case AnimationProfile.Fade:
                sequence.Append(canvasGroup.DOFade(isShow ? 1f : 0f, duration).SetEase(defaultEase));
                break;
                
            case AnimationProfile.SlideFromLeft:
            case AnimationProfile.SlideFromRight:
                var targetX = isShow ? 0f : (profile == AnimationProfile.SlideFromLeft ? -Screen.width : Screen.width);
                sequence.Append(rectTransform.DOAnchorPosX(targetX, duration).SetEase(defaultEase));
                break;
                
            case AnimationProfile.SlideFromTop:
            case AnimationProfile.SlideFromBottom:
                var targetY = isShow ? 0f : (profile == AnimationProfile.SlideFromTop ? Screen.height : -Screen.height);
                sequence.Append(rectTransform.DOAnchorPosY(targetY, duration).SetEase(defaultEase));
                break;
                
            case AnimationProfile.Scale:
                var targetScale = isShow ? Vector3.one : Vector3.zero;
                sequence.Append(rectTransform.DOScale(targetScale, duration).SetEase(defaultEase));
                if (isShow)
                {
                    sequence.Join(canvasGroup.DOFade(1f, duration * 0.5f).SetDelay(duration * 0.3f));
                }
                break;
                
            case AnimationProfile.ScaleAndFade:
                var scaleTarget = isShow ? Vector3.one : Vector3.zero;
                var alphaTarget = isShow ? 1f : 0f;
                sequence.Append(rectTransform.DOScale(scaleTarget, duration).SetEase(defaultEase));
                sequence.Join(canvasGroup.DOFade(alphaTarget, duration).SetEase(defaultEase));
                break;
        }
        
        return sequence;
    }

    private void FinalizeAnimationState(LayerBase layer, AnimationProfile profile, bool isShow)
    {
        var canvasGroup = layer.GetComponent<CanvasGroup>();
        var rectTransform = layer.transform as RectTransform;
        
        // Ensure final state is correct
        if (isShow)
        {
            canvasGroup.alpha = 1f;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            canvasGroup.alpha = 0f;
        }
    }

    private async UniTask DelayedAnimations(List<UniTask> animations, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        await UniTask.WhenAll(animations);
    }

    private (AnimationProfile profile, float duration, float delay) GetHideProfileFromTransition(TransitionProfile transition)
    {
        return transition switch
        {
            TransitionProfile.Instant => (AnimationProfile.Fade, 0f, 0f),
            TransitionProfile.Crossfade => (AnimationProfile.Fade, defaultAnimationDuration, 0f),
            TransitionProfile.SlideLeft => (AnimationProfile.SlideFromRight, defaultAnimationDuration, 0f),
            TransitionProfile.SlideRight => (AnimationProfile.SlideFromLeft, defaultAnimationDuration, 0f),
            TransitionProfile.SlideUp => (AnimationProfile.SlideFromBottom, defaultAnimationDuration, 0f),
            TransitionProfile.SlideDown => (AnimationProfile.SlideFromTop, defaultAnimationDuration, 0f),
            TransitionProfile.ScaleTransition => (AnimationProfile.Scale, defaultAnimationDuration, 0f),
            _ => (defaultProfile, defaultAnimationDuration, 0f)
        };
    }

    private (AnimationProfile profile, float duration, float delay) GetShowProfileFromTransition(TransitionProfile transition)
    {
        return transition switch
        {
            TransitionProfile.Instant => (AnimationProfile.Fade, 0f, 0f),
            TransitionProfile.Crossfade => (AnimationProfile.Fade, defaultAnimationDuration, 0f),
            TransitionProfile.SlideLeft => (AnimationProfile.SlideFromLeft, defaultAnimationDuration, defaultAnimationDuration * 0.5f),
            TransitionProfile.SlideRight => (AnimationProfile.SlideFromRight, defaultAnimationDuration, defaultAnimationDuration * 0.5f),
            TransitionProfile.SlideUp => (AnimationProfile.SlideFromTop, defaultAnimationDuration, defaultAnimationDuration * 0.5f),
            TransitionProfile.SlideDown => (AnimationProfile.SlideFromBottom, defaultAnimationDuration, defaultAnimationDuration * 0.5f),
            TransitionProfile.ScaleTransition => (AnimationProfile.ScaleAndFade, defaultAnimationDuration, defaultAnimationDuration * 0.2f),
            _ => (defaultProfile, defaultAnimationDuration, 0f)
        };
    }

    private void CleanupAnimationInfo(LayerAnimationInfo animInfo)
    {
        animInfo.AnimationSequence?.Kill();
        animInfo.AnimationSequence = null;
    }

    private void OnDestroy()
    {
        CancelAllAnimations();
        DOTween.KillAll();
    }
}

/// <summary>
/// Animation profiles cho different transition effects
/// </summary>
public enum AnimationProfile
{
    Default = 0,
    Fade = 1,
    SlideFromLeft = 2,
    SlideFromRight = 3,
    SlideFromTop = 4,
    SlideFromBottom = 5,
    Scale = 6,
    ScaleAndFade = 7
}

/// <summary>
/// Transition profiles cho group animations
/// </summary>
public enum TransitionProfile
{
    Default,
    Instant,
    Crossfade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    ScaleTransition
}

/// <summary>
/// Animation info tracking
/// </summary>
public class LayerAnimationInfo
{
    public LayerBase Layer;
    public AnimationProfile Profile;
    public float Duration;
    public bool IsShow;
    public float StartTime;
    public Sequence AnimationSequence;
}

/// <summary>
/// Animation statistics
/// </summary>
public struct AnimationStats
{
    public int ActiveAnimations;
    public int PooledTweens;
    public int LayersAnimating;
}
