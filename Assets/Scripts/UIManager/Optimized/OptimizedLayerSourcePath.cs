using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Performance-optimized version of LayerSourcePath with caching and validation
/// Giảm reflection calls từ mỗi lần gọi xuống chỉ initialization time
/// </summary>
public static class OptimizedLayerSourcePath
{
    // Cache để tránh reflection calls
    private static readonly Dictionary<string, string> _pathCache = new Dictionary<string, string>();
    private static readonly Dictionary<LayerType, string> _layerTypePathCache = new Dictionary<LayerType, string>();
    private static bool _initialized = false;

    // Paths definition - thêm mới paths ở đây
    public const string Layer01 = "Layers/LayerTest01";
    public const string MainMenuLayer = "Layers/MainMenu";
    public const string GameplayLayer = "Layers/Gameplay";
    public const string PauseLayer = "Layers/Pause";
    public const string SettingsLayer = "Layers/Settings";
    public const string InventoryLayer = "Layers/Inventory";
    public const string ShopLayer = "Layers/Shop";
    public const string LoadingLayer = "Layers/Loading";
    public const string NotificationLayer = "Layers/Notification";

    /// <summary>
    /// Initialize cache - gọi một lần duy nhất khi game start
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_initialized) return;
        
        InitializePathCache();
        InitializeLayerTypeCache();
        ValidatePaths();
        
        _initialized = true;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[OptimizedLayerSourcePath] Initialized with {_pathCache.Count} paths cached");
        #endif
    }

    /// <summary>
    /// Cache all field paths sử dụng reflection - chỉ chạy 1 lần
    /// </summary>
    private static void InitializePathCache()
    {
        var type = typeof(OptimizedLayerSourcePath);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(string) && field.IsLiteral)
            {
                var value = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    _pathCache[field.Name] = value;
                }
            }
        }
    }

    /// <summary>
    /// Map LayerType enum với paths
    /// </summary>
    private static void InitializeLayerTypeCache()
    {
        var layerTypes = Enum.GetValues(typeof(LayerType));
        
        foreach (LayerType layerType in layerTypes)
        {
            var layerTypeName = layerType.ToString();
            if (_pathCache.TryGetValue(layerTypeName, out string path))
            {
                _layerTypePathCache[layerType] = path;
            }
        }
    }

    /// <summary>
    /// Validate tất cả paths có valid format
    /// </summary>
    private static void ValidatePaths()
    {
        foreach (var kvp in _pathCache)
        {
            if (string.IsNullOrWhiteSpace(kvp.Value))
            {
                Debug.LogError($"[OptimizedLayerSourcePath] Invalid path for {kvp.Key}: '{kvp.Value}'");
            }
            else if (!kvp.Value.StartsWith("Layers/"))
            {
                Debug.LogWarning($"[OptimizedLayerSourcePath] Path {kvp.Key} doesn't follow convention 'Layers/': '{kvp.Value}'");
            }
        }
    }

    /// <summary>
    /// Get path by field name - Zero reflection calls sau khi initialized
    /// </summary>
    public static string GetPath(string variableName)
    {
        if (!_initialized)
        {
            Initialize();
        }

        if (_pathCache.TryGetValue(variableName, out string path))
        {
            return path;
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogError($"[OptimizedLayerSourcePath] No path found for: {variableName}. Available paths: {string.Join(", ", _pathCache.Keys)}");
        #endif
        
        return null;
    }

    /// <summary>
    /// Get path by LayerType - Fastest method, zero allocations
    /// </summary>
    public static string GetPath(LayerType layerType)
    {
        if (!_initialized)
        {
            Initialize();
        }

        return _layerTypePathCache.GetValueOrDefault(layerType);
    }

    /// <summary>
    /// Check if layer type has valid path - cho validation
    /// </summary>
    public static bool IsValidLayerType(LayerType layerType)
    {
        if (!_initialized)
        {
            Initialize();
        }

        return _layerTypePathCache.ContainsKey(layerType);
    }

    /// <summary>
    /// Get all available layer types - cho debug
    /// </summary>
    public static IReadOnlyCollection<LayerType> GetAvailableLayerTypes()
    {
        if (!_initialized)
        {
            Initialize();
        }

        return _layerTypePathCache.Keys;
    }

    /// <summary>
    /// Get cache statistics - cho performance monitoring
    /// </summary>
    public static (int pathCount, int layerTypeCount) GetCacheStats()
    {
        if (!_initialized)
        {
            Initialize();
        }

        return (_pathCache.Count, _layerTypePathCache.Count);
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Editor-only validation tool
    /// </summary>
    [UnityEditor.MenuItem("UIManager/Validate Layer Paths")]
    private static void ValidateAllPaths()
    {
        Initialize();
        
        Debug.Log("=== Layer Path Validation ===");
        Debug.Log($"Total paths cached: {_pathCache.Count}");
        Debug.Log($"Total layer types mapped: {_layerTypePathCache.Count}");
        
        // Check for unmapped layer types
        var allLayerTypes = Enum.GetValues(typeof(LayerType));
        var unmappedTypes = new List<LayerType>();
        
        foreach (LayerType layerType in allLayerTypes)
        {
            if (!_layerTypePathCache.ContainsKey(layerType))
            {
                unmappedTypes.Add(layerType);
            }
        }
        
        if (unmappedTypes.Count > 0)
        {
            Debug.LogWarning($"Unmapped LayerTypes: {string.Join(", ", unmappedTypes)}");
        }
        else
        {
            Debug.Log("✅ All LayerTypes have valid paths!");
        }
    }
    #endif
}
