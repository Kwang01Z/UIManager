using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Example demonstrating how to use Persistent layer groups
/// Persistent layers are never hidden by other layer operations
/// </summary>
public class PersistentLayerExample : MonoBehaviour
{
    [Header("Layer Types for Demo")]
    [SerializeField] private LayerType persistentLayerType = LayerType.NotificationLayer;
    [SerializeField] private LayerType regularLayerType = LayerType.MainMenuLayer;
    [SerializeField] private LayerType fullScreenLayerType = LayerType.GameplayLayer;
    
    [Header("Demo Controls")]
    [SerializeField] private bool runDemoOnStart = false;
    
    private async void Start()
    {
        if (runDemoOnStart)
        {
            await RunPersistentLayerDemo();
        }
    }
    
    /// <summary>
    /// Demonstrate how Persistent layers work
    /// </summary>
    public async UniTask RunPersistentLayerDemo()
    {
        Debug.Log("=== Persistent Layer Demo ===");
        
        // Step 1: Show a persistent layer (e.g., notification system, debug overlay, etc.)
        await ShowPersistentLayer();
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(2));
        
        // Step 2: Show regular layers - persistent should remain visible
        await ShowRegularLayer();
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(2));
        
        // Step 3: Show fullscreen layer - this usually hides other layers, but not persistent ones
        await ShowFullScreenLayer();
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(3));
        
        Debug.Log("=== Demo Complete ===");
        Debug.Log("Notice how the Persistent layer remained visible throughout all operations!");
    }
    
    /// <summary>
    /// Show a persistent layer (like notification system, debug panel, etc.)
    /// </summary>
    private async UniTask ShowPersistentLayer()
    {
        Debug.Log("--- Showing Persistent Layer ---");
        
        // Create persistent layer group
        var persistentData = LayerGroupBuilder.Build(LayerGroupType.Persistent, persistentLayerType);
        
        var layerGroup = await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            persistentData,
            onInitData: async (group) =>
            {
                // Setup persistent layer - this could be notification system, debug panel, etc.
                if (group.GetLayerBase(persistentLayerType, out var layer))
                {
                    Debug.Log($"✅ Initialized persistent layer: {persistentLayerType}");
                    // Configure the persistent layer here
                }
                await UniTask.Yield();
            },
            transition: TransitionProfile.SlideDown
        );
        
        Debug.Log($"📌 Persistent layer shown: {persistentLayerType} - This will stay visible!");
    }
    
    /// <summary>
    /// Show a regular layer
    /// </summary>
    private async UniTask ShowRegularLayer()
    {
        Debug.Log("--- Showing Regular Layer ---");
        
        var regularData = LayerGroupBuilder.Build(LayerGroupType.Popup, regularLayerType);
        
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            regularData,
            transition: TransitionProfile.Crossfade
        );
        
        Debug.Log($"🔵 Regular layer shown: {regularLayerType}");
        Debug.Log("Notice: Persistent layer is still visible!");
    }
    
    /// <summary>
    /// Show a fullscreen layer - this normally hides all other layers
    /// </summary>
    private async UniTask ShowFullScreenLayer()
    {
        Debug.Log("--- Showing FullScreen Layer (should hide regular layers but NOT persistent) ---");
        
        var fullScreenData = LayerGroupBuilder.Build(LayerGroupType.FullScreen, fullScreenLayerType);
        
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            fullScreenData,
            transition: TransitionProfile.SlideLeft
        );
        
        Debug.Log($"🟣 FullScreen layer shown: {fullScreenLayerType}");
        Debug.Log("FullScreen layers usually hide all others, but Persistent layers remain visible!");
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor buttons for manual testing
    /// </summary>
    [UnityEditor.MenuItem("UIManager/Examples/Demo Persistent Layers")]
    public static void RunPersistentDemo()
    {
        if (Application.isPlaying)
        {
            var example = FindObjectOfType<PersistentLayerExample>();
            if (example != null)
            {
                example.RunPersistentLayerDemo().Forget();
            }
            else
            {
                Debug.LogWarning("No PersistentLayerExample found in scene");
            }
        }
        else
        {
            Debug.LogWarning("Demo can only be run in Play Mode");
        }
    }
    
    [UnityEditor.MenuItem("UIManager/Examples/Show Persistent Layer")]
    public static void ShowPersistentLayerOnly()
    {
        if (Application.isPlaying)
        {
            var example = FindObjectOfType<PersistentLayerExample>();
            if (example != null)
            {
                example.ShowPersistentLayer().Forget();
            }
        }
    }
    
    [UnityEditor.MenuItem("UIManager/Examples/Show FullScreen Layer")]
    public static void ShowFullScreenLayerOnly()
    {
        if (Application.isPlaying)
        {
            var example = FindObjectOfType<PersistentLayerExample>();
            if (example != null)
            {
                example.ShowFullScreenLayer().Forget();
            }
        }
    }
    #endif
}

/// <summary>
/// Static helper methods for common persistent layer operations
/// </summary>
public static class PersistentLayerHelpers
{
    /// <summary>
    /// Show a notification that persists across all UI changes
    /// </summary>
    public static async UniTask ShowPersistentNotification(LayerType notificationType)
    {
        var data = LayerGroupBuilder.Build(LayerGroupType.Persistent, notificationType);
        
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            data,
            onInitData: async (group) =>
            {
                if (group.GetLayerBase(notificationType, out var layer))
                {
                    // Setup notification layer
                    Debug.Log($"📢 Persistent notification shown: {notificationType}");
                }
                await UniTask.Yield();
            },
            transition: TransitionProfile.SlideUp
        );
    }
    
    /// <summary>
    /// Show a debug overlay that stays on top
    /// </summary>
    public static async UniTask ShowPersistentDebugOverlay(LayerType debugLayerType)
    {
        var data = LayerGroupBuilder.Build(LayerGroupType.Persistent, debugLayerType);
        
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            data,
            onInitData: async (group) =>
            {
                if (group.GetLayerBase(debugLayerType, out var layer))
                {
                    // Setup debug overlay
                    Debug.Log($"🐛 Persistent debug overlay shown: {debugLayerType}");
                }
                await UniTask.Yield();
            },
            transition: TransitionProfile.Crossfade
        );
    }
    
    /// <summary>
    /// Show persistent UI elements (like loading indicators, connection status, etc.)
    /// </summary>
    public static async UniTask ShowPersistentSystemUI(params LayerType[] systemLayers)
    {
        var data = LayerGroupBuilder.Build(LayerGroupType.Persistent, systemLayers);
        
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            data,
            onInitData: async (group) =>
            {
                foreach (var layerType in systemLayers)
                {
                    if (group.GetLayerBase(layerType, out var layer))
                    {
                        Debug.Log($"⚙️ Persistent system UI shown: {layerType}");
                    }
                }
                await UniTask.Yield();
            },
            transition: TransitionProfile.Instant
        );
    }
}

/// <summary>
/// Common use cases for Persistent layers
/// </summary>
public static class PersistentLayerUseCases
{
    /// <summary>
    /// Use Case 1: Always-visible notification system
    /// Shows notifications that persist across scene changes
    /// </summary>
    public static async UniTask SetupPersistentNotificationSystem()
    {
        await PersistentLayerHelpers.ShowPersistentNotification(LayerType.NotificationLayer);
    }
    
    /// <summary>
    /// Use Case 2: Debug console that stays on top
    /// Useful during development
    /// </summary>
    public static async UniTask SetupDebugConsole()
    {
        // Assuming you have a debug layer type
        if (System.Enum.IsDefined(typeof(LayerType), "DebugConsoleLayer"))
        {
            var debugLayer = (LayerType)System.Enum.Parse(typeof(LayerType), "DebugConsoleLayer");
            await PersistentLayerHelpers.ShowPersistentDebugOverlay(debugLayer);
        }
    }
    
    /// <summary>
    /// Use Case 3: Loading indicators and connection status
    /// UI elements that should always be visible
    /// </summary>
    public static async UniTask SetupSystemStatusUI()
    {
        // Show multiple persistent system UI elements
        await PersistentLayerHelpers.ShowPersistentSystemUI(
            LayerType.LoadingLayer
            // Add more system UI layers as needed
        );
    }
    
    /// <summary>
    /// Use Case 4: Tutorial overlay
    /// Tutorial that persists across different screens
    /// </summary>
    public static async UniTask ShowPersistentTutorial(LayerType tutorialLayer)
    {
        var data = LayerGroupBuilder.Build(LayerGroupType.Persistent, tutorialLayer);
        
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            data,
            onInitData: async (group) =>
            {
                if (group.GetLayerBase(tutorialLayer, out var layer))
                {
                    Debug.Log($"📚 Persistent tutorial shown: {tutorialLayer}");
                    // Setup tutorial logic here
                }
                await UniTask.Yield();
            },
            transition: TransitionProfile.ScaleTransition
        );
    }
}
