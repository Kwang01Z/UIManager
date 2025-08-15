using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Performance-optimized LayerManager với integration của tất cả optimization components
/// Tích hợp ResourceManager, AnimationController và PerformanceMonitor
/// </summary>
public class OptimizedLayerManager : MonoSingleton<OptimizedLayerManager>
{
    [Header("Layer Settings")]
    [SerializeField] private bool hasLayerRoot = true;
    [SerializeField] private RectTransform layerParent;
    [SerializeField] private int spaceBetweenLayer = 100;
    
    [Header("Performance")]
    [SerializeField] private bool enableBatchOperations = true;
    [SerializeField] private bool enableSmartPreloading = true;
    [SerializeField] private bool enableAnimations = true;
    
    [Header("Animation")]
    [SerializeField] private TransitionProfile defaultTransition = TransitionProfile.Crossfade;
    [SerializeField] private float defaultAnimationDuration = 0.3f;

    // Core state
    private CancellationToken _destroyCt;
    private readonly HashSet<LayerType> _showingLayerTypes = new();
    private readonly Stack<ShowLayerGroupData> _showingLayerGroups = new();
    
    // Performance tracking
    private bool _isProcessing;
    private readonly Queue<LayerOperation> _operationQueue = new();

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        _destroyCt = this.GetCancellationTokenOnDestroy();
        
        if (!layerParent) 
            layerParent = transform as RectTransform;
            
        // Initialize subsystems
        EnsureSubsystemsReady();
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[OptimizedLayerManager] Initialized with optimizations enabled");
        #endif
    }

    private void EnsureSubsystemsReady()
    {
        // Ensure ResourceManager is initialized
        if (ResourceManager.Instance == null)
        {
            var resourceManagerGO = new GameObject("ResourceManager");
            resourceManagerGO.AddComponent<ResourceManager>();
        }
        
        // Ensure AnimationController is initialized if animations enabled
        if (enableAnimations && LayerAnimationController.Instance == null)
        {
            var animationControllerGO = new GameObject("LayerAnimationController");
            animationControllerGO.AddComponent<LayerAnimationController>();
        }
        
        // Ensure PerformanceMonitor is initialized
        if (PerformanceMonitor.Instance == null)
        {
            var performanceMonitorGO = new GameObject("PerformanceMonitor");
            performanceMonitorGO.AddComponent<PerformanceMonitor>();
        }
    }

    public bool IsShowing => _isProcessing;

    /// <summary>
    /// Show layer group với full optimization và animation support
    /// </summary>
    public async UniTask<LayerGroup> ShowGroupLayerAsync(
        ShowLayerGroupData showData, 
        Func<LayerGroup, UniTask> onInitData = null,
        TransitionProfile transition = TransitionProfile.Default)
    {
        using var tracker = PerformanceMonitor.Instance?.TrackOperation($"ShowGroupLayer_{string.Join(",", showData.LayerTypes)}");

        // Validation
        if (!ValidateShowData(showData))
        {
            return new LayerGroup();
        }

        // Wait for availability với timeout
        await UniTask.WhenAny(
            UniTask.WaitUntil(() => !_isProcessing, cancellationToken: _destroyCt),
            UniTask.WaitForSeconds(5, cancellationToken: _destroyCt)
        );

        if (_destroyCt.IsCancellationRequested || _isProcessing)
        {
            Debug.LogWarning($"[OptimizedLayerManager] Cannot show group - system busy or cancelled");
            return new LayerGroup();
        }

        _isProcessing = true;
        LayerGroup result = null;

        try
        {
            // Preload nếu enabled
            if (enableSmartPreloading)
            {
                await PreloadRequiredLayers(showData);
            }

            result = await InitLayerGroupOptimized(showData);
            if (result == null) return new LayerGroup();

            // Initialize data callback
            if (onInitData != null)
            {
                using var dataTracker = PerformanceMonitor.Instance?.TrackOperation("InitLayerData");
                await onInitData.Invoke(result);
            }

            // Handle conflicting layers
            await HandleLayerConflicts(showData);

            // Update state
            _showingLayerGroups.Push(showData);
            _showingLayerTypes.UnionWith(showData.LayerTypes);

            // Display với animation
            await DisplayLayerGroupOptimized(result, transition);

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[OptimizedLayerManager] Successfully showed group: {string.Join(",", showData.LayerTypes)}");
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[OptimizedLayerManager] Error showing layer group: {e}");
        }
        finally
        {
            _isProcessing = false;
        }

        return result ?? new LayerGroup();
    }

    /// <summary>
    /// Close last layer group với animation
    /// </summary>
    public async UniTask CloseLastLayerGroupAsync(TransitionProfile transition = TransitionProfile.Default)
    {
        using var tracker = PerformanceMonitor.Instance?.TrackOperation("CloseLastLayerGroup");

        if (_showingLayerGroups.Count == 0) return;
        if (_showingLayerGroups.Count <= 1 && hasLayerRoot) return;

        var lastGroup = _showingLayerGroups.Pop();
        
        // Get current group to animate
        var layersToClose = new List<LayerBase>();
        foreach (var layerType in lastGroup.LayerTypes)
        {
            var layer = await ResourceManager.Instance.GetLayerAsync(layerType);
            if (layer != null)
            {
                layersToClose.Add(layer);
            }
        }

        // Animate close nếu enabled
        if (enableAnimations && layersToClose.Count > 0)
        {
            var hideProfile = GetHideProfileFromTransition(transition);
            var animationTasks = layersToClose.Select(layer => 
                LayerAnimationController.Instance.AnimateHideAsync(layer, hideProfile.profile, hideProfile.duration)
            );
            
            await UniTask.WhenAll(animationTasks);
        }

        // Release resources
        foreach (var layerType in lastGroup.LayerTypes)
        {
            ResourceManager.Instance.ReleaseLayer(layerType);
        }

        // Update state
        _showingLayerTypes.ExceptWith(lastGroup.LayerTypes);
    }

    /// <summary>
    /// Preload layers for better performance
    /// </summary>
    public async UniTask PreloadLayersAsync(params LayerType[] layerTypes)
    {
        using var tracker = PerformanceMonitor.Instance?.TrackOperation($"PreloadLayers_{layerTypes.Length}");
        
        await ResourceManager.Instance.PreloadLayersAsync(layerTypes);
    }

    /// <summary>
    /// Get layer with caching
    /// </summary>
    public async UniTask<LayerBase> GetLayerAsync(LayerType layerType)
    {
        return await ResourceManager.Instance.GetLayerAsync(layerType);
    }

    /// <summary>
    /// Get performance statistics
    /// </summary>
    public OptimizedLayerManagerStats GetStats()
    {
        return new OptimizedLayerManagerStats
        {
            ShowingLayerCount = _showingLayerTypes.Count,
            LayerGroupStackDepth = _showingLayerGroups.Count,
            IsProcessing = _isProcessing,
            QueuedOperations = _operationQueue.Count,
            ResourceStats = ResourceManager.Instance?.GetStats() ?? default,
            AnimationStats = LayerAnimationController.Instance?.GetStats() ?? default
        };
    }

    private bool ValidateShowData(ShowLayerGroupData showData)
    {
        if (showData?.LayerTypes == null || showData.LayerTypes.Count == 0)
        {
            Debug.LogError("[OptimizedLayerManager] Invalid show data - no layer types specified");
            return false;
        }

        // Validate all layer types have paths
        foreach (var layerType in showData.LayerTypes)
        {
            if (!OptimizedLayerSourcePath.IsValidLayerType(layerType))
            {
                Debug.LogError($"[OptimizedLayerManager] Invalid layer type: {layerType}");
                return false;
            }
        }

        return true;
    }

    private async UniTask PreloadRequiredLayers(ShowLayerGroupData showData)
    {
        // Smart preloading - only preload if not already cached
        var layersToPreload = showData.LayerTypes
            .Where(layerType => ResourceManager.Instance.GetStats().TotalCachedLayers == 0 || 
                              !_showingLayerTypes.Contains(layerType))
            .ToArray();

        if (layersToPreload.Length > 0)
        {
            await ResourceManager.Instance.PreloadLayersAsync(layersToPreload);
        }
    }

    private async UniTask<LayerGroup> InitLayerGroupOptimized(ShowLayerGroupData showData)
    {
        var layerGroup = new LayerGroup();

        if (enableBatchOperations)
        {
            // Batch load all layers
            var loadTasks = showData.LayerTypes.Select(async layerType =>
            {
                var layer = await ResourceManager.Instance.GetLayerAsync(layerType);
                return new { LayerType = layerType, Layer = layer };
            });

            var results = await UniTask.WhenAll(loadTasks);

            foreach (var result in results)
            {
                if (result.Layer != null)
                {
                    layerGroup.AddLayer(result.LayerType, result.Layer);
                }
            }
        }
        else
        {
            // Sequential loading
            foreach (var layerType in showData.LayerTypes)
            {
                var layer = await ResourceManager.Instance.GetLayerAsync(layerType);
                if (layer != null)
                {
                    layerGroup.AddLayer(layerType, layer);
                }
            }
        }

        return layerGroup;
    }

    private async UniTask HandleLayerConflicts(ShowLayerGroupData showData)
    {
        var conflictTasks = new List<UniTask>();

        if (showData.CloseAllOtherLayer)
        {
            conflictTasks.Add(CloseAllLayersExcept(showData));
        }
        else
        {
            if (showData.CloseOtherLayerOver)
                conflictTasks.Add(CloseLayersOver(showData));
            
            if (showData.HideAllOtherLayer)
                conflictTasks.Add(HideAllLayersExcept(showData));
            
            if (showData.CloseAllPopup)
                conflictTasks.Add(CloseAllPopups(showData));
        }

        if (conflictTasks.Count > 0)
        {
            await UniTask.WhenAll(conflictTasks);
        }
    }

    private async UniTask DisplayLayerGroupOptimized(LayerGroup layerGroup, TransitionProfile transition)
    {
        // Set sorting order
        SetSortingLayer(layerGroup);

        // Show with animation if enabled
        if (enableAnimations && transition != TransitionProfile.Instant)
        {
            await LayerAnimationController.Instance.AnimateGroupTransitionAsync(null, layerGroup, transition);
        }
        else
        {
            // Direct show without animation
            await layerGroup.ShowGroupAsync();
        }
    }

    private void SetSortingLayer(LayerGroup layerGroup)
    {
        int bestOrder = GetBestLayerSorting(layerGroup) + spaceBetweenLayer;
        layerGroup.SetSortOrder(bestOrder);
    }

    private int GetBestLayerSorting(LayerGroup layerGroup)
    {
        int bestLayerSorting = 0;
        var otherLayers = _showingLayerTypes.Except(layerGroup.LayerTypes);
        
        foreach (var layerType in otherLayers)
        {
            var layer = ResourceManager.Instance.GetLayerAsync(layerType).GetAwaiter().GetResult();
            if (layer != null)
            {
                bestLayerSorting = Mathf.Max(bestLayerSorting, layer.GetSortingOrder());
            }
        }

        return bestLayerSorting / spaceBetweenLayer * spaceBetweenLayer;
    }

    // Helper methods for conflict resolution
    private async UniTask CloseAllLayersExcept(ShowLayerGroupData showData)
    {
        var layersToClose = showData.IgnoreHideThisLayer 
            ? _showingLayerTypes.Except(showData.LayerTypes)
            : _showingLayerTypes;

        var closeTasks = layersToClose.Select(async layerType =>
        {
            var layer = await ResourceManager.Instance.GetLayerAsync(layerType);
            if (layer != null)
            {
                await layer.CloseLayerAsync(true);
                ResourceManager.Instance.ReleaseLayer(layerType);
            }
        });

        await UniTask.WhenAll(closeTasks);
        
        _showingLayerGroups.Clear();
        _showingLayerTypes.Clear();
    }

    private async UniTask CloseLayersOver(ShowLayerGroupData showData)
    {
        // Implementation for closing layers over current group
        // Similar to original but with optimization
        await UniTask.CompletedTask; // Placeholder
    }

    private async UniTask HideAllLayersExcept(ShowLayerGroupData showData)
    {
        var layersToHide = _showingLayerTypes.Except(showData.LayerTypes);
        var hideTasks = layersToHide.Select(async layerType =>
        {
            var layer = await ResourceManager.Instance.GetLayerAsync(layerType);
            if (layer != null)
            {
                await layer.HideLayerAsync();
            }
        });

        await UniTask.WhenAll(hideTasks);
    }

    private async UniTask CloseAllPopups(ShowLayerGroupData showData)
    {
        var popupLayers = _showingLayerGroups
            .Where(x => x.LayerGroupType == LayerGroupType.Popup)
            .SelectMany(x => x.LayerTypes)
            .Except(showData.LayerTypes)
            .Distinct();

        var closeTasks = popupLayers.Select(async layerType =>
        {
            var layer = await ResourceManager.Instance.GetLayerAsync(layerType);
            if (layer != null)
            {
                await layer.CloseLayerAsync(true);
                ResourceManager.Instance.ReleaseLayer(layerType);
            }
        });

        await UniTask.WhenAll(closeTasks);
        
        // Update state
        _showingLayerTypes.ExceptWith(popupLayers);
        var groupsToRemove = _showingLayerGroups
            .Where(x => x.LayerGroupType == LayerGroupType.Popup)
            .ToList();
        
        foreach (var group in groupsToRemove)
        {
            // Remove from stack (this is simplified - actual implementation would be more complex)
        }
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
            _ => (AnimationProfile.Fade, defaultAnimationDuration, 0f)
        };
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Editor helper to validate system setup
    /// </summary>
    [UnityEditor.MenuItem("UIManager/Validate Optimized Setup")]
    private static void ValidateOptimizedSetup()
    {
        Debug.Log("=== Optimized UIManager Validation ===");
        
        // Check if optimized path system is working
        var stats = OptimizedLayerSourcePath.GetCacheStats();
        Debug.Log($"✅ Path cache: {stats.pathCount} paths, {stats.layerTypeCount} layer types");
        
        // Check subsystems
        Debug.Log($"ResourceManager: {(ResourceManager.Instance != null ? "✅ Ready" : "❌ Missing")}");
        Debug.Log($"AnimationController: {(LayerAnimationController.Instance != null ? "✅ Ready" : "❌ Missing")}");
        Debug.Log($"PerformanceMonitor: {(PerformanceMonitor.Instance != null ? "✅ Ready" : "❌ Missing")}");
        
        Debug.Log("Validation complete!");
    }
    #endif
}

/// <summary>
/// Layer operation for queuing
/// </summary>
public struct LayerOperation
{
    public LayerOperationType Type;
    public ShowLayerGroupData Data;
    public TransitionProfile Transition;
}

public enum LayerOperationType
{
    Show,
    Close,
    Hide
}

/// <summary>
/// Optimized LayerManager statistics
/// </summary>
public struct OptimizedLayerManagerStats
{
    public int ShowingLayerCount;
    public int LayerGroupStackDepth;
    public bool IsProcessing;
    public int QueuedOperations;
    public ResourceStats ResourceStats;
    public AnimationStats AnimationStats;
}
