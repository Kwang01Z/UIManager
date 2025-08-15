using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Runtime performance monitor và debug system cho UIManager
/// Tracking memory, performance và animation metrics
/// </summary>
public class PerformanceMonitor : MonoSingleton<PerformanceMonitor>
{
    [Header("Monitoring Settings")]
    [SerializeField] private bool enableRuntimeMonitoring = true;
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private int maxHistoryPoints = 60;
    [SerializeField] private bool showDebugGUI = false;
    
    [Header("Performance Thresholds")]
    [SerializeField] private float maxLoadTime = 0.5f;
    [SerializeField] private long maxMemoryUsage = 50 * 1024 * 1024; // 50MB
    [SerializeField] private int maxActiveAnimations = 5;
    
    // Performance tracking
    private readonly CircularBuffer<PerformanceSnapshot> _performanceHistory = new(60);
    private readonly Dictionary<string, float> _operationTimes = new();
    private readonly List<string> _performanceWarnings = new();
    
    // GUI Display
    private bool _showGUI = false;
    private Vector2 _scrollPosition;
    private GUIStyle _labelStyle;
    private GUIStyle _warningStyle;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (enableRuntimeMonitoring)
        {
            StartMonitoring().Forget();
        }
        
        InitializeGUIStyles();
    }

    /// <summary>
    /// Start monitoring performance metrics
    /// </summary>
    public async UniTaskVoid StartMonitoring()
    {
        while (this != null && enableRuntimeMonitoring)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(updateInterval));
            CapturePerformanceSnapshot();
        }
    }

    /// <summary>
    /// Track operation execution time
    /// </summary>
    public IDisposable TrackOperation(string operationName)
    {
        return new OperationTracker(this, operationName);
    }

    /// <summary>
    /// Record operation time manually
    /// </summary>
    public void RecordOperationTime(string operationName, float duration)
    {
        _operationTimes[operationName] = duration;
        
        // Check thresholds
        if (duration > maxLoadTime)
        {
            AddWarning($"Slow operation: {operationName} took {duration:F3}s");
        }
    }

    /// <summary>
    /// Get current performance snapshot
    /// </summary>
    public PerformanceSnapshot GetCurrentSnapshot()
    {
        return new PerformanceSnapshot
        {
            Timestamp = Time.time,
            FrameRate = 1f / Time.unscaledDeltaTime,
            MemoryUsage = GetMemoryUsage(),
            ResourceStats = ResourceManager.Instance?.GetStats() ?? default,
            AnimationStats = LayerAnimationController.Instance?.GetStats() ?? default,
            LayerCount = LayerManager.Instance != null ? GetActiveLayerCount() : 0
        };
    }

    /// <summary>
    /// Get performance history
    /// </summary>
    public IReadOnlyList<PerformanceSnapshot> GetPerformanceHistory()
    {
        return _performanceHistory.ToList();
    }

    /// <summary>
    /// Get recent performance warnings
    /// </summary>
    public IReadOnlyList<string> GetWarnings()
    {
        return _performanceWarnings.AsReadOnly();
    }

    /// <summary>
    /// Clear performance warnings
    /// </summary>
    public void ClearWarnings()
    {
        _performanceWarnings.Clear();
    }

    /// <summary>
    /// Toggle debug GUI visibility
    /// </summary>
    public void ToggleDebugGUI()
    {
        _showGUI = !_showGUI;
    }

    /// <summary>
    /// Generate performance report
    /// </summary>
    public string GeneratePerformanceReport()
    {
        var sb = new StringBuilder();
        var snapshot = GetCurrentSnapshot();
        
        sb.AppendLine("=== UIManager Performance Report ===");
        sb.AppendLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        
        // Current state
        sb.AppendLine("--- Current State ---");
        sb.AppendLine($"Frame Rate: {snapshot.FrameRate:F1} FPS");
        sb.AppendLine($"Memory Usage: {snapshot.MemoryUsage / 1024f / 1024f:F1} MB");
        sb.AppendLine($"Active Layers: {snapshot.LayerCount}");
        sb.AppendLine();
        
        // Resource stats
        sb.AppendLine("--- Resource Manager ---");
        sb.AppendLine($"Cached Layers: {snapshot.ResourceStats.TotalCachedLayers}");
        sb.AppendLine($"Active References: {snapshot.ResourceStats.ActiveReferences}");
        sb.AppendLine($"Estimated Memory: {snapshot.ResourceStats.EstimatedMemoryUsage / 1024f:F1} KB");
        sb.AppendLine($"Preloaded Layers: {snapshot.ResourceStats.PreloadedLayers}");
        sb.AppendLine();
        
        // Animation stats
        sb.AppendLine("--- Animation System ---");
        sb.AppendLine($"Active Animations: {snapshot.AnimationStats.ActiveAnimations}");
        sb.AppendLine($"Layers Animating: {snapshot.AnimationStats.LayersAnimating}");
        sb.AppendLine($"Pooled Tweens: {snapshot.AnimationStats.PooledTweens}");
        sb.AppendLine();
        
        // Operation times
        if (_operationTimes.Count > 0)
        {
            sb.AppendLine("--- Operation Times ---");
            foreach (var kvp in _operationTimes.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value:F3}s");
            }
            sb.AppendLine();
        }
        
        // Warnings
        if (_performanceWarnings.Count > 0)
        {
            sb.AppendLine("--- Performance Warnings ---");
            foreach (var warning in _performanceWarnings.TakeLast(10))
            {
                sb.AppendLine($"⚠️ {warning}");
            }
        }
        
        return sb.ToString();
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Save performance report to file (Editor only)
    /// </summary>
    [UnityEditor.MenuItem("UIManager/Generate Performance Report")]
    public static void SavePerformanceReport()
    {
        if (Instance != null)
        {
            var report = Instance.GeneratePerformanceReport();
            var path = $"UIManager_Performance_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            System.IO.File.WriteAllText(path, report);
            UnityEngine.Debug.Log($"Performance report saved to: {path}");
        }
    }
    #endif

    private void CapturePerformanceSnapshot()
    {
        var snapshot = GetCurrentSnapshot();
        _performanceHistory.Add(snapshot);
        
        CheckPerformanceThresholds(snapshot);
    }

    private void CheckPerformanceThresholds(PerformanceSnapshot snapshot)
    {
        // Check memory usage
        if (snapshot.MemoryUsage > maxMemoryUsage)
        {
            AddWarning($"High memory usage: {snapshot.MemoryUsage / 1024f / 1024f:F1}MB");
        }
        
        // Check animation count
        if (snapshot.AnimationStats.ActiveAnimations > maxActiveAnimations)
        {
            AddWarning($"Too many animations: {snapshot.AnimationStats.ActiveAnimations}");
        }
        
        // Check frame rate
        if (snapshot.FrameRate < 30f)
        {
            AddWarning($"Low frame rate: {snapshot.FrameRate:F1} FPS");
        }
    }

    private void AddWarning(string warning)
    {
        var timestampedWarning = $"[{DateTime.Now:HH:mm:ss}] {warning}";
        _performanceWarnings.Add(timestampedWarning);
        
        // Keep only recent warnings
        while (_performanceWarnings.Count > 20)
        {
            _performanceWarnings.RemoveAt(0);
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Debug.LogWarning($"[PerformanceMonitor] {warning}");
        #endif
    }

    private long GetMemoryUsage()
    {
        return Profiler.GetTotalAllocatedMemory();
    }

    private int GetActiveLayerCount()
    {
        // This would need to be implemented based on LayerManager structure
        return 0; // Placeholder
    }

    private void InitializeGUIStyles()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        _labelStyle = new GUIStyle();
        _warningStyle = new GUIStyle();
        #endif
    }

    private void Update()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ToggleDebugGUI();
        }
        #endif
    }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (!_showGUI && !showDebugGUI) return;
        
        InitializeGUIStylesIfNeeded();
        
        var rect = new Rect(10, 10, 400, Screen.height - 20);
        GUI.Box(rect, "UIManager Performance Monitor");
        
        var contentRect = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, rect.height - 35);
        _scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, new Rect(0, 0, contentRect.width - 20, GetContentHeight()));
        
        DrawPerformanceInfo();
        
        GUI.EndScrollView();
    }

    private void InitializeGUIStylesIfNeeded()
    {
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };
            
            _warningStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = Color.yellow }
            };
        }
    }

    private float GetContentHeight()
    {
        return 500f; // Estimate based on content
    }

    private void DrawPerformanceInfo()
    {
        var snapshot = GetCurrentSnapshot();
        
        GUILayout.Label("=== Current Performance ===", _labelStyle);
        GUILayout.Label($"FPS: {snapshot.FrameRate:F1}", _labelStyle);
        GUILayout.Label($"Memory: {snapshot.MemoryUsage / 1024f / 1024f:F1} MB", _labelStyle);
        GUILayout.Label($"Layers: {snapshot.LayerCount}", _labelStyle);
        
        GUILayout.Space(10);
        GUILayout.Label("=== Resource Manager ===", _labelStyle);
        GUILayout.Label($"Cached: {snapshot.ResourceStats.TotalCachedLayers}", _labelStyle);
        GUILayout.Label($"References: {snapshot.ResourceStats.ActiveReferences}", _labelStyle);
        GUILayout.Label($"Preloaded: {snapshot.ResourceStats.PreloadedLayers}", _labelStyle);
        
        GUILayout.Space(10);
        GUILayout.Label("=== Animations ===", _labelStyle);
        GUILayout.Label($"Active: {snapshot.AnimationStats.ActiveAnimations}", _labelStyle);
        GUILayout.Label($"Animating Layers: {snapshot.AnimationStats.LayersAnimating}", _labelStyle);
        
        if (_performanceWarnings.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("=== Warnings ===", _warningStyle);
            foreach (var warning in _performanceWarnings.TakeLast(5))
            {
                GUILayout.Label($"⚠️ {warning}", _warningStyle);
            }
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("Generate Report"))
        {
            var report = GeneratePerformanceReport();
            UnityEngine.Debug.Log(report);
        }
        
        if (GUILayout.Button("Clear Warnings"))
        {
            ClearWarnings();
        }
    }
    #endif
}

/// <summary>
/// Performance snapshot data structure
/// </summary>
public struct PerformanceSnapshot
{
    public float Timestamp;
    public float FrameRate;
    public long MemoryUsage;
    public ResourceStats ResourceStats;
    public AnimationStats AnimationStats;
    public int LayerCount;
}

/// <summary>
/// Operation time tracker - implements IDisposable for using statement
/// </summary>
public class OperationTracker : IDisposable
{
    private readonly PerformanceMonitor _monitor;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;

    public OperationTracker(PerformanceMonitor monitor, string operationName)
    {
        _monitor = monitor;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _monitor.RecordOperationTime(_operationName, (float)_stopwatch.Elapsed.TotalSeconds);
    }
}

/// <summary>
/// Circular buffer for performance history
/// </summary>
public class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;
    
    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }
    
    public void Add(T item)
    {
        _buffer[_head] = item;
        _head = (_head + 1) % _buffer.Length;
        
        if (_count < _buffer.Length)
        {
            _count++;
        }
    }
    
    public List<T> ToList()
    {
        var result = new List<T>(_count);
        for (int i = 0; i < _count; i++)
        {
            var index = (_head - _count + i + _buffer.Length) % _buffer.Length;
            result.Add(_buffer[index]);
        }
        return result;
    }
}
