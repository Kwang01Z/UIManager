using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Smart resource manager với reference counting và automatic cleanup
/// Quản lý memory efficiently và tránh memory leaks
/// </summary>
public class ResourceManager : MonoSingleton<ResourceManager>
{
    [Header("Memory Settings")]
    [SerializeField] private int maxCachedLayers = 10;
    [SerializeField] private float memoryCleanupInterval = 30f;
    [SerializeField] private float unusedLayerLifetime = 60f;
    [SerializeField] private bool enableSmartPreloading = true;

    // Reference counting system
    private readonly Dictionary<LayerType, LayerResourceInfo> _resourceInfos = new();
    private readonly Queue<LayerType> _lruQueue = new();
    private readonly HashSet<LayerType> _preloadedLayers = new();
    
    // Memory monitoring
    private float _lastCleanupTime;
    private long _totalMemoryUsage;
    
    protected override void Awake()
    {
        base.Awake();
        _lastCleanupTime = Time.time;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        StartPerformanceMonitoring().Forget();
        #endif
    }

    /// <summary>
    /// Get or load layer với reference counting
    /// </summary>
    public async UniTask<LayerBase> GetLayerAsync(LayerType layerType)
    {
        var resourceInfo = GetOrCreateResourceInfo(layerType);
        
        // Increment reference count
        resourceInfo.ReferenceCount++;
        resourceInfo.LastAccessTime = Time.time;
        
        // Move to front of LRU queue
        UpdateLRUQueue(layerType);
        
        if (resourceInfo.LayerInstance != null)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ResourceManager] Cache hit for {layerType} (RefCount: {resourceInfo.ReferenceCount})");
            #endif
            return resourceInfo.LayerInstance;
        }

        return await LoadLayerAsync(layerType, resourceInfo);
    }

    /// <summary>
    /// Release reference to layer
    /// </summary>
    public void ReleaseLayer(LayerType layerType)
    {
        if (_resourceInfos.TryGetValue(layerType, out var resourceInfo))
        {
            resourceInfo.ReferenceCount = Mathf.Max(0, resourceInfo.ReferenceCount - 1);
            resourceInfo.LastReleaseTime = Time.time;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ResourceManager] Released {layerType} (RefCount: {resourceInfo.ReferenceCount})");
            #endif
            
            // Schedule cleanup if no references
            if (resourceInfo.ReferenceCount == 0)
            {
                ScheduleCleanup();
            }
        }
    }

    /// <summary>
    /// Preload layers for better performance
    /// </summary>
    public async UniTask PreloadLayersAsync(params LayerType[] layerTypes)
    {
        if (!enableSmartPreloading) return;
        
        var preloadTasks = layerTypes
            .Where(layerType => !_resourceInfos.ContainsKey(layerType) && !_preloadedLayers.Contains(layerType))
            .Select(layerType => PreloadLayerAsync(layerType))
            .ToArray();
            
        if (preloadTasks.Length > 0)
        {
            await UniTask.WhenAll(preloadTasks);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ResourceManager] Preloaded {preloadTasks.Length} layers");
            #endif
        }
    }

    /// <summary>
    /// Force cleanup unused resources
    /// </summary>
    public void ForceCleanup()
    {
        CleanupUnusedResources();
    }

    /// <summary>
    /// Get memory usage statistics
    /// </summary>
    public ResourceStats GetStats()
    {
        return new ResourceStats
        {
            TotalCachedLayers = _resourceInfos.Count,
            ActiveReferences = _resourceInfos.Values.Sum(r => r.ReferenceCount),
            EstimatedMemoryUsage = _totalMemoryUsage,
            PreloadedLayers = _preloadedLayers.Count
        };
    }

    private LayerResourceInfo GetOrCreateResourceInfo(LayerType layerType)
    {
        if (!_resourceInfos.TryGetValue(layerType, out var resourceInfo))
        {
            resourceInfo = new LayerResourceInfo
            {
                LayerType = layerType,
                CreationTime = Time.time
            };
            _resourceInfos[layerType] = resourceInfo;
        }
        return resourceInfo;
    }

    private async UniTask<LayerBase> LoadLayerAsync(LayerType layerType, LayerResourceInfo resourceInfo)
    {
        string loadPath = OptimizedLayerSourcePath.GetPath(layerType);
        if (string.IsNullOrEmpty(loadPath))
        {
            Debug.LogError($"[ResourceManager] Invalid path for {layerType}");
            return null;
        }

        try
        {
            var loadStartTime = Time.realtimeSinceStartup;
            
            var handle = Addressables.InstantiateAsync(loadPath, OptimizedLayerManager.Instance.transform);
            await handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var layerInstance = handle.Result.GetComponent<LayerBase>();
                if (layerInstance != null)
                {
                    resourceInfo.LayerInstance = layerInstance;
                    resourceInfo.AssetHandle = handle;
                    resourceInfo.LoadTime = Time.realtimeSinceStartup - loadStartTime;
                    
                    // Estimate memory usage
                    resourceInfo.EstimatedSize = EstimateLayerSize(layerInstance);
                    _totalMemoryUsage += resourceInfo.EstimatedSize;
                    
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[ResourceManager] Loaded {layerType} in {resourceInfo.LoadTime:F3}s (Size: {resourceInfo.EstimatedSize / 1024f:F1}KB)");
                    #endif
                    
                    return layerInstance;
                }
            }
            
            Debug.LogError($"[ResourceManager] Failed to load {layerType} from {loadPath}");
            Addressables.Release(handle);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceManager] Exception loading {layerType}: {e}");
        }
        
        return null;
    }

    private async UniTask PreloadLayerAsync(LayerType layerType)
    {
        var resourceInfo = GetOrCreateResourceInfo(layerType);
        resourceInfo.IsPreloaded = true;
        _preloadedLayers.Add(layerType);
        
        await LoadLayerAsync(layerType, resourceInfo);
    }

    private void UpdateLRUQueue(LayerType layerType)
    {
        // Remove from queue if exists
        var tempQueue = new Queue<LayerType>();
        while (_lruQueue.Count > 0)
        {
            var item = _lruQueue.Dequeue();
            if (item != layerType)
            {
                tempQueue.Enqueue(item);
            }
        }
        
        // Restore queue and add to front
        while (tempQueue.Count > 0)
        {
            _lruQueue.Enqueue(tempQueue.Dequeue());
        }
        _lruQueue.Enqueue(layerType);
    }

    private void Update()
    {
        if (Time.time - _lastCleanupTime >= memoryCleanupInterval)
        {
            CleanupUnusedResources();
            _lastCleanupTime = Time.time;
        }
    }

    private void ScheduleCleanup()
    {
        // Cleanup will happen on next Update cycle
    }

    private void CleanupUnusedResources()
    {
        var currentTime = Time.time;
        var layersToCleanup = new List<LayerType>();
        
        // Find layers to cleanup
        foreach (var kvp in _resourceInfos)
        {
            var info = kvp.Value;
            if (info.ReferenceCount == 0 && 
                info.LayerInstance != null &&
                currentTime - info.LastReleaseTime >= unusedLayerLifetime)
            {
                layersToCleanup.Add(kvp.Key);
            }
        }
        
        // Enforce max cache limit
        while (_resourceInfos.Count - layersToCleanup.Count > maxCachedLayers && _lruQueue.Count > 0)
        {
            var oldestLayer = _lruQueue.Dequeue();
            if (_resourceInfos.TryGetValue(oldestLayer, out var info) && info.ReferenceCount == 0)
            {
                layersToCleanup.Add(oldestLayer);
            }
        }
        
        // Cleanup layers
        foreach (var layerType in layersToCleanup)
        {
            CleanupLayer(layerType);
        }
        
        if (layersToCleanup.Count > 0)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ResourceManager] Cleaned up {layersToCleanup.Count} unused layers");
            #endif
        }
    }

    private void CleanupLayer(LayerType layerType)
    {
        if (_resourceInfos.TryGetValue(layerType, out var info))
        {
            if (info.AssetHandle.IsValid())
            {
                Addressables.ReleaseInstance(info.AssetHandle);
            }
            
            _totalMemoryUsage -= info.EstimatedSize;
            _resourceInfos.Remove(layerType);
            _preloadedLayers.Remove(layerType);
        }
    }

    private long EstimateLayerSize(LayerBase layer)
    {
        // Simple estimation - can be improved with actual profiling
        long estimatedSize = 0;
        
        // Base GameObject overhead
        estimatedSize += 1024; // ~1KB base
        
        // Canvas components
        var canvases = layer.GetComponentsInChildren<Canvas>(true);
        estimatedSize += canvases.Length * 512;
        
        // UI Images/Text
        var images = layer.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        var texts = layer.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        estimatedSize += (images.Length + texts.Length) * 256;
        
        return estimatedSize;
    }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    private async UniTaskVoid StartPerformanceMonitoring()
    {
        while (this != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5));
            
            var stats = GetStats();
            if (stats.TotalCachedLayers > 0)
            {
                Debug.Log($"[ResourceManager Stats] Layers: {stats.TotalCachedLayers}, " +
                         $"Active Refs: {stats.ActiveReferences}, " +
                         $"Memory: {stats.EstimatedMemoryUsage / 1024f:F1}KB, " +
                         $"Preloaded: {stats.PreloadedLayers}");
            }
        }
    }
    #endif
}

/// <summary>
/// Resource info for each layer
/// </summary>
public class LayerResourceInfo
{
    public LayerType LayerType;
    public LayerBase LayerInstance;
    public AsyncOperationHandle<GameObject> AssetHandle;
    public int ReferenceCount;
    public float CreationTime;
    public float LastAccessTime;
    public float LastReleaseTime;
    public float LoadTime;
    public long EstimatedSize;
    public bool IsPreloaded;
}

/// <summary>
/// Resource usage statistics
/// </summary>
public struct ResourceStats
{
    public int TotalCachedLayers;
    public int ActiveReferences;
    public long EstimatedMemoryUsage;
    public int PreloadedLayers;
}
