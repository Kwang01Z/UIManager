using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Test script to verify all TransitionProfile values work correctly
/// Tests both regular and persistent layer transitions
/// </summary>
public class TransitionProfileTest : MonoBehaviour
{
    [Header("Layer Types for Testing")]
    [SerializeField] private LayerType testLayerType1 = LayerType.Layer01;
    [SerializeField] private LayerType testLayerType2 = LayerType.MainMenuLayer;
    [SerializeField] private LayerType persistentTestLayer = LayerType.NotificationLayer;
    
    [Header("Test Controls")]
    [SerializeField] private bool runAllTestsOnStart = false;
    
    private async void Start()
    {
        if (runAllTestsOnStart)
        {
            await RunAllTransitionTests();
        }
    }
    
    /// <summary>
    /// Test all available TransitionProfile values
    /// </summary>
    public async UniTask RunAllTransitionTests()
    {
        Debug.Log("=== TransitionProfile Test Suite ===");
        
        // Test each transition profile
        await TestTransitionProfile(TransitionProfile.Default);
        await TestTransitionProfile(TransitionProfile.Instant);
        await TestTransitionProfile(TransitionProfile.Crossfade);
        await TestTransitionProfile(TransitionProfile.SlideLeft);
        await TestTransitionProfile(TransitionProfile.SlideRight);
        await TestTransitionProfile(TransitionProfile.SlideUp);
        await TestTransitionProfile(TransitionProfile.SlideDown);
        await TestTransitionProfile(TransitionProfile.ScaleTransition);
        
        Debug.Log("=== All Transition Tests Complete ===");
    }
    
    /// <summary>
    /// Test a specific TransitionProfile
    /// </summary>
    private async UniTask TestTransitionProfile(TransitionProfile profile)
    {
        Debug.Log($"--- Testing TransitionProfile.{profile} ---");
        
        try
        {
            // Show layer with the transition
            var data = LayerGroupBuilder.Build(LayerGroupType.Popup, testLayerType1);
            var layerGroup = await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
                data, 
                transition: profile
            );
            
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f));
            
            // Close the layer
            await OptimizedLayerManager.Instance.CloseLastLayerGroupAsync(profile);
            
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.3f));
            
            Debug.Log($"✅ TransitionProfile.{profile} - SUCCESS");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ TransitionProfile.{profile} - FAILED: {e.Message}");
        }
    }
    
    /// <summary>
    /// Test persistent layer with transitions
    /// </summary>
    public async UniTask TestPersistentLayerTransitions()
    {
        Debug.Log("=== Testing Persistent Layer Transitions ===");
        
        // Show persistent layer
        var persistentData = LayerGroupBuilder.Build(LayerGroupType.Persistent, persistentTestLayer);
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
            persistentData, 
            transition: TransitionProfile.SlideUp
        );
        
        Debug.Log("📌 Persistent layer shown");
        
        // Test various transitions that should NOT affect persistent layer
        await TestTransitionWithPersistent(TransitionProfile.Crossfade, LayerGroupType.Popup);
        await TestTransitionWithPersistent(TransitionProfile.SlideLeft, LayerGroupType.FullScreen);
        await TestTransitionWithPersistent(TransitionProfile.ScaleTransition, LayerGroupType.Root);
        
        Debug.Log("=== Persistent Layer Tests Complete ===");
    }
    
    private async UniTask TestTransitionWithPersistent(TransitionProfile profile, LayerGroupType groupType)
    {
        Debug.Log($"Testing {profile} with {groupType} - persistent layer should remain visible");
        
        var data = LayerGroupBuilder.Build(groupType, testLayerType2);
        await OptimizedLayerManager.Instance.ShowGroupLayerAsync(data, transition: profile);
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(1));
        
        // The persistent layer should still be visible here
        await OptimizedLayerManager.Instance.CloseLastLayerGroupAsync();
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f));
    }
    
    /// <summary>
    /// Test animation interruption and cancellation
    /// </summary>
    public async UniTask TestAnimationInterruption()
    {
        Debug.Log("=== Testing Animation Interruption ===");
        
        try
        {
            // Start a slow transition
            var data1 = LayerGroupBuilder.Build(LayerGroupType.Popup, testLayerType1);
            var task1 = OptimizedLayerManager.Instance.ShowGroupLayerAsync(
                data1, 
                transition: TransitionProfile.SlideLeft
            );
            
            // Don't wait for completion, immediately start another
            await UniTask.Delay(System.TimeSpan.FromMilliseconds(100));
            
            var data2 = LayerGroupBuilder.Build(LayerGroupType.FullScreen, testLayerType2);
            var task2 = OptimizedLayerManager.Instance.ShowGroupLayerAsync(
                data2, 
                transition: TransitionProfile.SlideRight
            );
            
            // Wait for both to complete
            await UniTask.WhenAll(task1, task2);
            
            Debug.Log("✅ Animation interruption test - SUCCESS");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Animation interruption test - FAILED: {e.Message}");
        }
    }
    
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("UIManager/Tests/Run All Transition Tests")]
    public static void RunAllTransitionTestsEditor()
    {
        if (Application.isPlaying)
        {
            var tester = FindObjectOfType<TransitionProfileTest>();
            if (tester != null)
            {
                tester.RunAllTransitionTests().Forget();
            }
            else
            {
                Debug.LogWarning("No TransitionProfileTest component found in scene");
            }
        }
        else
        {
            Debug.LogWarning("Tests can only be run in Play Mode");
        }
    }
    
    [UnityEditor.MenuItem("UIManager/Tests/Test Persistent Layer Transitions")]
    public static void TestPersistentLayerTransitionsEditor()
    {
        if (Application.isPlaying)
        {
            var tester = FindObjectOfType<TransitionProfileTest>();
            if (tester != null)
            {
                tester.TestPersistentLayerTransitions().Forget();
            }
        }
    }
    
    [UnityEditor.MenuItem("UIManager/Tests/Test Animation Interruption")]
    public static void TestAnimationInterruptionEditor()
    {
        if (Application.isPlaying)
        {
            var tester = FindObjectOfType<TransitionProfileTest>();
            if (tester != null)
            {
                tester.TestAnimationInterruption().Forget();
            }
        }
    }
    #endif
}

/// <summary>
/// Available TransitionProfile values reference
/// </summary>
public static class TransitionProfileReference
{
    /// <summary>
    /// All available TransitionProfile values
    /// </summary>
    public static readonly TransitionProfile[] AllProfiles = new TransitionProfile[]
    {
        TransitionProfile.Default,     // Uses default system transition
        TransitionProfile.Instant,    // No animation, immediate
        TransitionProfile.Crossfade,  // Fade in/out
        TransitionProfile.SlideLeft,  // Slide from/to left
        TransitionProfile.SlideRight, // Slide from/to right
        TransitionProfile.SlideUp,    // Slide from/to top
        TransitionProfile.SlideDown,  // Slide from/to bottom
        TransitionProfile.ScaleTransition // Scale with fade
    };
    
    /// <summary>
    /// Get description for a TransitionProfile
    /// </summary>
    public static string GetDescription(TransitionProfile profile)
    {
        return profile switch
        {
            TransitionProfile.Default => "Uses system default transition",
            TransitionProfile.Instant => "Immediate show/hide, no animation",
            TransitionProfile.Crossfade => "Fade in/out transition",
            TransitionProfile.SlideLeft => "Slide from/to left edge",
            TransitionProfile.SlideRight => "Slide from/to right edge", 
            TransitionProfile.SlideUp => "Slide from/to top edge",
            TransitionProfile.SlideDown => "Slide from/to bottom edge",
            TransitionProfile.ScaleTransition => "Scale with fade effect",
            _ => "Unknown transition profile"
        };
    }
    
    /// <summary>
    /// Log all available transitions with descriptions
    /// </summary>
    public static void LogAllTransitions()
    {
        Debug.Log("=== Available TransitionProfile Values ===");
        foreach (var profile in AllProfiles)
        {
            Debug.Log($"• TransitionProfile.{profile}: {GetDescription(profile)}");
        }
    }
}
