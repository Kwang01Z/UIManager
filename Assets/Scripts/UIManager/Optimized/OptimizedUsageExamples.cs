using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Usage examples cho optimized UIManager system
/// Minh họa cách sử dụng với best practices
/// </summary>
public class OptimizedUsageExamples : MonoBehaviour
{
    [Header("Example Settings")]
    [SerializeField] private bool runExamplesOnStart = false;
    
    private async void Start()
    {
        if (runExamplesOnStart)
        {
            await RunAllExamples();
        }
    }
    
    public async UniTask RunAllExamples()
    {
        Debug.Log("=== Running Optimized UIManager Examples ===");
        
        // Example 1: Basic layer showing với animation
        await Example1_BasicLayerShow();
        await UniTask.Delay(System.TimeSpan.FromSeconds(2));
        
        // Example 2: Batch operations và preloading
        await Example2_BatchOperationsAndPreloading();
        await UniTask.Delay(System.TimeSpan.FromSeconds(2));
        
        // Example 3: Complex transitions
        await Example3_ComplexTransitions();
        await UniTask.Delay(System.TimeSpan.FromSeconds(2));
        
        // Example 4: Performance monitoring
        await Example4_PerformanceMonitoring();
        
        Debug.Log("=== All examples completed ===");
    }
    
    /// <summary>
    /// Example 1: Basic layer showing với animation
    /// </summary>
    private async UniTask Example1_BasicLayerShow()
    {
        Debug.Log("--- Example 1: Basic Layer Show ---");
        
        // Show single layer với default crossfade animation
        var showData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.Layer01);
        var layerGroup = await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            showData, 
            onInitData: async (group) =>
            {
                // Setup data for layer
                if (group.GetLayerBase(LayerType.Layer01, out var layer))
                {
                    // Initialize layer với custom data
                    Debug.Log("Setting up Layer01 data...");
                }
            },
            transition: TransitionProfile.ScaleTransition
        );
        
        Debug.Log($"Layer group shown with {layerGroup.LayerTypes.Count} layers");
    }
    
    /// <summary>
    /// Example 2: Batch operations và preloading
    /// </summary>
    private async UniTask Example2_BatchOperationsAndPreloading()
    {
        Debug.Log("--- Example 2: Batch Operations & Preloading ---");
        
        // Preload layers trước khi cần sử dụng
        await OptimizedLayerManager.Instance.PreloadLayersAsync(
            LayerType.MainMenuLayer, 
            LayerType.SettingsLayer,
            LayerType.InventoryLayer
        );
        
        // Show multiple layers cùng lúc (batch operation)
        var showData = LayerGroupBuilder.Build(
            LayerGroupType.FullScreen, 
            LayerType.MainMenuLayer, 
            LayerType.NotificationLayer
        );
        
        var layerGroup = await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            showData,
            transition: TransitionProfile.SlideUp
        );
        
        // Get performance stats
        var stats = OptimizedLayerManager.Instance.GetStats();
        Debug.Log($"Current stats - Showing: {stats.ShowingLayerCount}, " +
                  $"Processing: {stats.IsProcessing}, " +
                  $"Cached: {stats.ResourceStats.TotalCachedLayers}");
    }
    
    /// <summary>
    /// Example 3: Complex transitions với layer switching
    /// </summary>
    private async UniTask Example3_ComplexTransitions()
    {
        Debug.Log("--- Example 3: Complex Transitions ---");
        
        // Show main menu
        var mainMenuData = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.MainMenuLayer);
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            mainMenuData,
            transition: TransitionProfile.Crossfade
        );
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(1));
        
        // Transition to gameplay
        var gameplayData = LayerGroupBuilder.Build(LayerGroupType.FullScreen, LayerType.GameplayLayer);
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            gameplayData,
            transition: TransitionProfile.SlideRight
        );
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(1));
        
        // Show pause overlay
        var pauseData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PauseLayer);
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            pauseData,
            transition: TransitionProfile.ScaleTransition
        );
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(1));
        
        // Close pause (animated)
        await OptimizedLayerManager.Instance.CloseLastLayerGroupAsync(TransitionProfile.ScaleTransition);
    }
    
    /// <summary>
    /// Example 4: Performance monitoring và debugging
    /// </summary>
    private async UniTask Example4_PerformanceMonitoring()
    {
        Debug.Log("--- Example 4: Performance Monitoring ---");
        
        // Generate performance report
        if (PerformanceMonitor.Instance != null)
        {
            var report = PerformanceMonitor.Instance.GeneratePerformanceReport();
            Debug.Log("Performance Report:\n" + report);
            
            // Get warnings
            var warnings = PerformanceMonitor.Instance.GetWarnings();
            if (warnings.Count > 0)
            {
                Debug.Log($"Performance warnings: {warnings.Count}");
                foreach (var warning in warnings)
                {
                    Debug.LogWarning(warning);
                }
            }
        }
        
        // Get resource manager stats
        if (ResourceManager.Instance != null)
        {
            var resourceStats = ResourceManager.Instance.GetStats();
            Debug.Log($"Resource Stats - Cached: {resourceStats.TotalCachedLayers}, " +
                      $"References: {resourceStats.ActiveReferences}, " +
                      $"Memory: {resourceStats.EstimatedMemoryUsage / 1024f:F1}KB");
        }
        
        // Get animation stats
        if (LayerAnimationController.Instance != null)
        {
            var animStats = LayerAnimationController.Instance.GetStats();
            Debug.Log($"Animation Stats - Active: {animStats.ActiveAnimations}, " +
                      $"Layers Animating: {animStats.LayersAnimating}");
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor buttons để test các examples
    /// </summary>
    [UnityEditor.MenuItem("UIManager/Examples/Run All Examples")]
    public static void RunAllExamplesEditor()
    {
        if (Application.isPlaying)
        {
            var example = FindObjectOfType<OptimizedUsageExamples>();
            if (example != null)
            {
                example.RunAllExamples().Forget();
            }
            else
            {
                Debug.LogWarning("No OptimizedUsageExamples component found in scene");
            }
        }
        else
        {
            Debug.LogWarning("Examples can only be run in Play Mode");
        }
    }
    
    [UnityEditor.MenuItem("UIManager/Examples/Toggle Debug GUI")]
    public static void ToggleDebugGUI()
    {
        if (Application.isPlaying && PerformanceMonitor.Instance != null)
        {
            PerformanceMonitor.Instance.ToggleDebugGUI();
        }
    }
    #endif
}

/// <summary>
/// Migration helper để chuyển từ LayerManager cũ sang OptimizedLayerManager
/// </summary>
public static class MigrationHelper
{
    /// <summary>
    /// Migrate existing LayerManager usage sang OptimizedLayerManager
    /// </summary>
    public static class LayerManagerMigration
    {
        // OLD WAY (LayerManager):
        // var group = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.Layer01);
        // await LayerManager.Instance.ShowGroupLayerAsync(group, SetupData);
        
        // NEW WAY (OptimizedLayerManager):
        public static async UniTask ShowLayer01Optimized()
        {
            var group = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.Layer01);
            await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
                group, 
                SetupDataOptimized,
                TransitionProfile.Crossfade
            );
        }
        
        private static async UniTask SetupDataOptimized(LayerGroup layerGroup)
        {
            if (layerGroup.GetLayerBase(LayerType.Layer01, out var layerBase))
            {
                // Setup data với performance tracking
                using var tracker = PerformanceMonitor.Instance?.TrackOperation("SetupLayer01Data");
                
                // Your setup code here
                await UniTask.Yield();
            }
        }
    }
    
    /// <summary>
    /// Performance optimization checklist
    /// </summary>
    public static class OptimizationChecklist
    {
        /// <summary>
        /// Validate optimization setup
        /// </summary>
        public static void ValidateOptimizationSetup()
        {
            Debug.Log("=== Optimization Setup Validation ===");
            
            // Check path cache
            var pathStats = OptimizedLayerSourcePath.GetCacheStats();
            Debug.Log($"✅ Path Cache: {pathStats.pathCount} paths, {pathStats.layerTypeCount} types");
            
            // Check managers
            Debug.Log($"ResourceManager: {(ResourceManager.Instance != null ? "✅" : "❌")}");
            Debug.Log($"AnimationController: {(LayerAnimationController.Instance != null ? "✅" : "❌")}");
            Debug.Log($"PerformanceMonitor: {(PerformanceMonitor.Instance != null ? "✅" : "❌")}");
            Debug.Log($"OptimizedLayerManager: {(OptimizedLayerManager.Instance != null ? "✅" : "❌")}");
            
            // Performance recommendations
            Debug.Log("\n=== Performance Recommendations ===");
            Debug.Log("1. ✅ Use OptimizedLayerManager.PreloadLayersAsync() for frequently used layers");
            Debug.Log("2. ✅ Enable batch operations for multiple layer loading");
            Debug.Log("3. ✅ Use TransitionProfile for smooth animations");
            Debug.Log("4. ✅ Monitor performance with F12 debug GUI");
            Debug.Log("5. ✅ Use using statements with PerformanceMonitor.TrackOperation()");
        }
    }
}

/// <summary>
/// Best practices guide
/// </summary>
public static class BestPracticesGuide
{
    /// <summary>
    /// Memory management best practices
    /// </summary>
    public static class Memory
    {
        // ✅ GOOD: Preload frequently used layers
        public static async UniTask PreloadFrequentLayers()
        {
            await OptimizedLayerManager.Instance.PreloadLayersAsync(
                LayerType.MainMenuLayer,
                LayerType.PauseLayer,
                LayerType.NotificationLayer
            );
        }
        
        // ✅ GOOD: Release layers when done
        public static void ReleaseUnusedLayer()
        {
            ResourceManager.Instance.ReleaseLayer(LayerType.SettingsLayer);
        }
        
        // ✅ GOOD: Force cleanup when memory pressure
        public static void HandleMemoryPressure()
        {
            ResourceManager.Instance.ForceCleanup();
        }
    }
    
    /// <summary>
    /// Performance monitoring best practices
    /// </summary>
    public static class Performance
    {
        // ✅ GOOD: Track expensive operations
        public static async UniTask ExpensiveOperation()
        {
            using var tracker = PerformanceMonitor.Instance?.TrackOperation("ExpensiveOperation");
            
            // Your expensive code here
            await UniTask.Delay(System.TimeSpan.FromMilliseconds(100));
        }
        
        // ✅ GOOD: Monitor stats regularly
        public static void CheckPerformanceStats()
        {
            var stats = OptimizedLayerManager.Instance.GetStats();
            if (stats.ResourceStats.TotalCachedLayers > 10)
            {
                Debug.LogWarning("Too many cached layers - consider cleanup");
            }
        }
    }
    
    /// <summary>
    /// Animation best practices
    /// </summary>
    public static class Animation
    {
        // ✅ GOOD: Use appropriate transition types
        public static async UniTask ShowMainMenu()
        {
            var data = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.MainMenuLayer);
            await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
                data, 
                transition: TransitionProfile.Crossfade // Smooth for main transitions
            );
        }
        
        // ✅ GOOD: Use instant for loading screens
        public static async UniTask ShowLoadingScreen()
        {
            var data = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.LoadingLayer);
            await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
                data,
                transition: TransitionProfile.Instant // No delay for loading
            );
        }
    }
}
