using UnityEngine;

/// <summary>
/// Adjusts UI elements to avoid screen cutouts (notches, hole-punches)
/// while ignoring under-display cameras
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaUIAdjuster : MonoBehaviour
{
    [SerializeField] private RectTransform[] rectPanelSafes = null;
    [SerializeField] private bool debugLog = false;
    
    private void Awake()
    {
        AdjustForCutouts();
    }

    private void AdjustForCutouts()
    {
        if (rectPanelSafes == null || rectPanelSafes.Length == 0)
        {
            if (debugLog) Debug.LogWarning("No RectTransforms assigned to adjust", this);
            return;
        }

        // Get the main screen safe area first
        Rect safeArea = Screen.safeArea;
        
        if (debugLog)
        {
            Debug.Log($"Screen: {Screen.width}x{Screen.height}");
            Debug.Log($"Safe Area: {safeArea}");
            Debug.Log($"Cutouts count: {Screen.cutouts.Length}");
        }

        // If no cutouts, just apply the standard safe area
        if (Screen.cutouts.Length == 0)
        {
            ApplySafeArea(safeArea);
            return;
        }

        // Process cutouts (ignoring under-display cameras)
        foreach (Rect cutout in Screen.cutouts)
        {
            if (debugLog) Debug.Log($"Processing cutout: {cutout}");

            // Skip if this appears to be an under-display camera (cutout is too small)
            if (cutout.width < 10 || cutout.height < 10) 
                continue;

            // Adjust safe area based on cutout position
            if (cutout.yMax >= Screen.height - 1) // Top cutout (notch)
            {
                safeArea.yMax = Mathf.Min(safeArea.yMax, cutout.yMin);
            }
            else if (cutout.yMin <= 1) // Bottom cutout
            {
                safeArea.yMin = Mathf.Max(safeArea.yMin, cutout.yMax);
            }
            else if (cutout.xMin <= 1) // Left cutout
            {
                safeArea.xMin = Mathf.Max(safeArea.xMin, cutout.xMax);
            }
            else if (cutout.xMax >= Screen.width - 1) // Right cutout
            {
                safeArea.xMax = Mathf.Min(safeArea.xMax, cutout.xMin);
            }
        }

        ApplySafeArea(safeArea);
    }

    private void ApplySafeArea(Rect safeArea)
    {
        Vector2 anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
        Vector2 anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

        if (debugLog) Debug.Log($"Applying safe area - Min: {anchorMin}, Max: {anchorMax}");

        foreach (var rectTransform in rectPanelSafes)
        {
            if (rectTransform == null)
            {
                if (debugLog) Debug.LogWarning("Null RectTransform in array", this);
                continue;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    // For testing in editor
    #if UNITY_EDITOR
    [ContextMenu("Test Safe Area Adjustment")]
    private void TestAdjustment()
    {
        AdjustForCutouts();
    }
    #endif
}