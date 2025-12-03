using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(IFS_Data))]
public class ScrollSnap : MonoBehaviour
{
    [Header("Snap Settings")]
    [SerializeField] private bool enableSnap = true;
    [SerializeField] private float snapSpeed = 10f;
    [SerializeField] private float snapThreshold = 0.1f;
    [Tooltip("Độ giảm tốc khi snap (0-1). Càng cao càng giảm nhanh")]
    [SerializeField] [Range(0.5f, 0.99f)] private float snapDeceleration = 0.92f;
    [Tooltip("Hệ số dự đoán vị trí dựa trên velocity (0-1). Càng cao càng dự đoán xa")]
    [SerializeField] [Range(0f, 1f)] private float velocityPredictionFactor = 0.5f;

    [Header("Snap Position")]
    [Tooltip("Vị trí snap trong viewport (0-1). 0.5 = center")]
    [SerializeField] [Range(0f, 1f)] private float snapPositionNormalized = 0.5f;

    [Header("Scroll Detection")]
    [SerializeField] private float scrollStopDelay = 0.15f;
    [Tooltip("Velocity threshold để detect scroll đã dừng")]
    [SerializeField] private float velocityThreshold = 10f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private IFS_Data _scrollData;
    private ScrollRect _scrollRect;
    private bool _isSnapping = false;
    private bool _isDragging = false;
    private Coroutine _snapCoroutine;
    private Coroutine _checkScrollStopCoroutine;
    private Vector2 _lastContentPosition;
    private float _lastScrollTime;

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            // Auto get IFS_Data component
            if (_scrollData == null)
            {
                _scrollData = GetComponent<IFS_Data>();
            }

            // Validate parameters
            snapSpeed = Mathf.Max(0.1f, snapSpeed);
            snapThreshold = Mathf.Max(0.01f, snapThreshold);
            snapDeceleration = Mathf.Clamp(snapDeceleration, 0.5f, 0.99f);
            velocityPredictionFactor = Mathf.Clamp01(velocityPredictionFactor);
            snapPositionNormalized = Mathf.Clamp01(snapPositionNormalized);
            scrollStopDelay = Mathf.Max(0.01f, scrollStopDelay);
            velocityThreshold = Mathf.Max(0f, velocityThreshold);
        }
    }

    private void Awake()
    {
        _scrollData = GetComponent<IFS_Data>();
    }

    private void Start()
    {
        _scrollRect = GetScrollRect();
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            // Add drag event listeners
            var scrollRectEventTrigger = _scrollRect.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (scrollRectEventTrigger == null)
            {
                scrollRectEventTrigger = _scrollRect.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }

            // Begin drag
            var beginDragEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.BeginDrag
            };
            beginDragEntry.callback.AddListener((data) => { OnBeginDrag(); });
            scrollRectEventTrigger.triggers.Add(beginDragEntry);

            // End drag
            var endDragEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.EndDrag
            };
            endDragEntry.callback.AddListener((data) => { OnEndDrag(); });
            scrollRectEventTrigger.triggers.Add(endDragEntry);
        }

        _lastContentPosition = _scrollRect.content.anchoredPosition;
    }

    private void OnDestroy()
    {
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }
    }

    private ScrollRect GetScrollRect()
    {
        // Access scrollRect through reflection since it's private
        var field = typeof(IFS_Data).GetField("scrollRect",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        return field?.GetValue(_scrollData) as ScrollRect;
    }

    private List<IFS_PlaceHolder> GetPlaceHolders()
    {
        // Access _placeHolders through reflection
        var field = typeof(IFS_Data).GetField("_placeHolders",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        return field?.GetValue(_scrollData) as List<IFS_PlaceHolder>;
    }

    private void OnBeginDrag()
    {
        _isDragging = true;
        _isSnapping = false;

        if (_snapCoroutine != null)
        {
            StopCoroutine(_snapCoroutine);
            _snapCoroutine = null;
        }

        if (_checkScrollStopCoroutine != null)
        {
            StopCoroutine(_checkScrollStopCoroutine);
            _checkScrollStopCoroutine = null;
        }
    }

    private void OnEndDrag()
    {
        _isDragging = false;
        _lastScrollTime = Time.time;

        if (enableSnap && _checkScrollStopCoroutine == null)
        {
            _checkScrollStopCoroutine = StartCoroutine(CheckScrollStop());
        }
    }

    private void OnScrollValueChanged(Vector2 position)
    {
        if (!_isDragging && !_isSnapping)
        {
            _lastScrollTime = Time.time;

            if (_checkScrollStopCoroutine == null && enableSnap)
            {
                _checkScrollStopCoroutine = StartCoroutine(CheckScrollStop());
            }
        }

        _lastContentPosition = _scrollRect.content.anchoredPosition;
    }

    private IEnumerator CheckScrollStop()
    {
        yield return new WaitForSeconds(scrollStopDelay);

        while (true)
        {
            // Check if scroll has stopped
            float timeSinceLastScroll = Time.time - _lastScrollTime;
            Vector2 velocity = _scrollRect.velocity;
            float currentVelocity = _scrollData.ScrollType == GridLayoutGroup.Axis.Vertical
                ? Mathf.Abs(velocity.y)
                : Mathf.Abs(velocity.x);

            if (timeSinceLastScroll >= scrollStopDelay && currentVelocity < velocityThreshold)
            {
                if (enableSnap && !_isSnapping)
                {
                    StartSnapping();
                }
                break;
            }

            yield return null;
        }

        _checkScrollStopCoroutine = null;
    }

    private void StartSnapping()
    {
        if (_snapCoroutine != null)
        {
            StopCoroutine(_snapCoroutine);
        }

        _snapCoroutine = StartCoroutine(SnapToNearestItem());
    }

    private IEnumerator SnapToNearestItem()
    {
        _isSnapping = true;

        // Find nearest item to snap position
        IFS_PlaceHolder nearestItem = FindNearestItemToSnapPosition();

        if (nearestItem == null)
        {
            _isSnapping = false;
            yield break;
        }

        // Calculate target position
        Vector2 targetPosition = CalculateSnapTargetPosition(nearestItem);

        // Get current velocity and position
        Vector2 currentPosition = _scrollRect.content.anchoredPosition;
        Vector2 direction = targetPosition - currentPosition;
        float distance = direction.magnitude;

        if (distance < snapThreshold)
        {
            _scrollRect.content.anchoredPosition = targetPosition;
            _scrollRect.velocity = Vector2.zero;
            _isSnapping = false;
            yield break;
        }

        // Use current scroll velocity as initial snap velocity
        Vector2 currentVelocity = _scrollRect.velocity;
        float isVertical = _scrollData.ScrollType == GridLayoutGroup.Axis.Vertical ? 1f : 0f;
        float isHorizontal = 1f - isVertical;

        // Get velocity magnitude in scroll direction
        float initialSpeed = Mathf.Abs(currentVelocity.y * isVertical + currentVelocity.x * isHorizontal);

        // Use minimum speed if velocity is too low
        initialSpeed = Mathf.Max(initialSpeed, snapSpeed * 50f);

        // Normalize direction
        Vector2 directionNormalized = direction.normalized;

        // Snap with deceleration
        float remainingDistance = distance;
        float currentSpeed = initialSpeed;

        while (remainingDistance > snapThreshold)
        {
            // Calculate movement this frame
            float movement = currentSpeed * Time.deltaTime;

            if (movement >= remainingDistance)
            {
                // Final snap
                _scrollRect.content.anchoredPosition = targetPosition;
                break;
            }

            // Move towards target
            currentPosition += directionNormalized * movement;
            _scrollRect.content.anchoredPosition = currentPosition;

            // Update remaining distance
            remainingDistance -= movement;

            // Apply deceleration
            currentSpeed *= snapDeceleration;

            yield return null;
        }

        _scrollRect.content.anchoredPosition = targetPosition;
        _scrollRect.velocity = Vector2.zero;
        _isSnapping = false;
        _snapCoroutine = null;
    }

    private IFS_PlaceHolder FindNearestItemToSnapPosition()
    {
        List<IFS_PlaceHolder> placeHolders = GetPlaceHolders();
        if (placeHolders == null || placeHolders.Count == 0)
        {
            return null;
        }

        float snapPosition = GetSnapPositionInViewport();

        // Calculate predicted position based on current velocity
        Vector2 predictedContentPosition = CalculatePredictedPosition();

        IFS_PlaceHolder nearestItem = null;
        float minDistance = float.MaxValue;

        if (debugMode)
        {
            Debug.Log($"=== Finding Nearest Item ===");
            Debug.Log($"Snap Position: {snapPosition}, Viewport: {_scrollData.ViewportWidth}x{_scrollData.ViewportHeight}");
            Debug.Log($"Current Position: {_scrollRect.content.anchoredPosition}");
            Debug.Log($"Predicted Position: {predictedContentPosition}");
            Debug.Log($"Velocity: {_scrollRect.velocity}");
        }

        foreach (var placeHolder in placeHolders)
        {
            // Skip space placeholders
            if (placeHolder is IFS_PlaceSpace) continue;

            // Calculate item position in viewport using predicted position
            float itemPosition = GetItemPositionInViewport(placeHolder, predictedContentPosition);
            float distance = Mathf.Abs(itemPosition - snapPosition);

            if (debugMode && distance < 500) // Only log nearby items
            {
                Debug.Log($"Item {placeHolder.Data}: Position={itemPosition:F2}, Distance={distance:F2}, AnchorY={placeHolder.AnchoredPosition.y:F2}");
            }

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestItem = placeHolder;
            }
        }

        if (debugMode && nearestItem != null)
        {
            Debug.Log($"<color=green>Selected Item: {nearestItem.Data}, MinDistance: {minDistance:F2}</color>");
        }

        return nearestItem;
    }

    private Vector2 CalculatePredictedPosition()
    {
        Vector2 currentPosition = _scrollRect.content.anchoredPosition;
        Vector2 currentVelocity = _scrollRect.velocity;

        // Calculate how far the content will move based on velocity and deceleration
        // Using physics: final_distance = initial_velocity / (1 - deceleration_factor)
        // This is an approximation of exponential decay

        float isVertical = _scrollData.ScrollType == GridLayoutGroup.Axis.Vertical ? 1f : 0f;
        float isHorizontal = 1f - isVertical;

        float velocityMagnitude = Mathf.Abs(currentVelocity.y * isVertical + currentVelocity.x * isHorizontal);

        // Calculate predicted distance using velocity prediction factor
        // The factor controls how much we trust the velocity prediction
        float decelerationRate = 0.96f; // ScrollRect default deceleration
        float predictedDistance = velocityMagnitude * Time.fixedDeltaTime / (1f - decelerationRate);
        predictedDistance *= velocityPredictionFactor;

        // Apply predicted movement
        Vector2 predictedPosition = currentPosition;

        if (_scrollData.ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            // Positive velocity means scrolling down (content moving up)
            float direction = currentVelocity.y > 0 ? 1f : -1f;
            predictedPosition.y += predictedDistance * direction;
        }
        else
        {
            // Horizontal scroll
            float direction = currentVelocity.x > 0 ? 1f : -1f;
            predictedPosition.x += predictedDistance * direction;
        }

        return predictedPosition;
    }

    private float GetSnapPositionInViewport()
    {
        if (_scrollData.ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            return _scrollData.ViewportHeight * snapPositionNormalized;
        }
        else
        {
            return _scrollData.ViewportWidth * snapPositionNormalized;
        }
    }

    private float GetItemPositionInViewport(IFS_PlaceHolder placeHolder, Vector2? contentAnchorOverride = null)
    {
        Vector2 contentAnchor = contentAnchorOverride ?? _scrollRect.content.anchoredPosition;
        Vector2 itemAnchor = placeHolder.AnchoredPosition;

        if (_scrollData.ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            // Vertical scroll with pivot (0.5f, 1) - anchor at top
            // itemAnchor.y is negative from content top
            // contentAnchor.y is POSITIVE when scrolling down

            // Absolute position from content top (positive)
            float itemTopFromContent = -itemAnchor.y;
            // Current scroll offset (positive when scrolled down)
            float scrollOffset = contentAnchor.y;
            // Position in viewport from top (item center)
            float itemPositionInViewport = itemTopFromContent - scrollOffset + placeHolder.ItemHeight / 2f;

            return itemPositionInViewport;
        }
        else
        {
            // Horizontal scroll
            float itemCenterX = itemAnchor.x + contentAnchor.x + placeHolder.ItemWidth / 2f;
            return itemCenterX;
        }
    }

    private Vector2 CalculateSnapTargetPosition(IFS_PlaceHolder placeHolder)
    {
        Vector2 currentPosition = _scrollRect.content.anchoredPosition;
        Vector2 itemAnchor = placeHolder.AnchoredPosition;
        float snapPosition = GetSnapPositionInViewport();

        if (_scrollData.ScrollType == GridLayoutGroup.Axis.Vertical)
        {
            // For vertical scroll with pivot (0.5f, 1):
            // snapPosition is distance from viewport top (0 = top, ViewportHeight = bottom)
            // itemAnchor.y is negative from content top
            // contentAnchor.y is POSITIVE when scrolled down

            // Item's center position from content top (positive value)
            float itemCenterFromContentTop = -itemAnchor.y + placeHolder.ItemHeight / 2f;

            // We want the item center to appear at snapPosition from viewport top
            // targetScrollOffset = itemCenterFromContentTop - snapPosition
            // Since content.y is positive when scrolling down:
            float targetY = itemCenterFromContentTop - snapPosition;

            // Clamp to valid scroll bounds
            float contentHeight = _scrollData.ContentSize.y;
            float viewportHeight = _scrollData.ViewportHeight;

            // Only clamp if content is larger than viewport
            if (contentHeight > viewportHeight)
            {
                float minY = 0f; // At top
                float maxY = contentHeight - viewportHeight; // Max scroll down
                targetY = Mathf.Clamp(targetY, minY, maxY);
            }
            else
            {
                targetY = 0f; // Content fits in viewport, stay at top
            }

            if (debugMode)
            {
                Debug.Log($"ItemAnchor.y: {itemAnchor.y:F2}, ItemCenterFromTop: {itemCenterFromContentTop:F2}");
                Debug.Log($"SnapPosition: {snapPosition:F2}");
                Debug.Log($"Target Y: {targetY:F2}, Current: {currentPosition.y:F2}");
                Debug.Log($"Content: {contentHeight:F2}, Viewport: {viewportHeight:F2}, MaxScroll: {contentHeight - viewportHeight:F2}");
            }

            return new Vector2(currentPosition.x, targetY);
        }
        else
        {
            // Calculate X offset needed to position item at snap position
            float itemCenterX = itemAnchor.x + placeHolder.ItemWidth / 2f;
            float targetX = snapPosition - itemCenterX;

            // Clamp to valid scroll bounds
            float contentWidth = _scrollData.ContentSize.x;
            float viewportWidth = _scrollData.ViewportWidth;

            if (contentWidth > viewportWidth)
            {
                float minX = 0f;
                float maxX = contentWidth - viewportWidth;
                targetX = Mathf.Clamp(targetX, -maxX, minX);
            }
            else
            {
                targetX = 0f;
            }

            if (debugMode)
            {
                Debug.Log($"Target X: {targetX:F2}, Current: {currentPosition.x:F2}, ItemCenterX: {itemCenterX:F2}, SnapPos: {snapPosition:F2}");
            }

            return new Vector2(targetX, currentPosition.y);
        }
    }

    // Public methods to control snap behavior
    public void SetEnableSnap(bool enable)
    {
        enableSnap = enable;
    }

    public void SetSnapPosition(float normalizedPosition)
    {
        snapPositionNormalized = Mathf.Clamp01(normalizedPosition);
    }

    public void SetSnapSpeed(float speed)
    {
        snapSpeed = Mathf.Max(0.1f, speed);
    }

    public void SnapToIndex(int index)
    {
        List<IFS_PlaceHolder> placeHolders = GetPlaceHolders();
        if (placeHolders == null || index < 0 || index >= placeHolders.Count)
        {
            return;
        }

        if (_snapCoroutine != null)
        {
            StopCoroutine(_snapCoroutine);
        }

        _snapCoroutine = StartCoroutine(SnapToSpecificItem(placeHolders[index]));
    }

    private IEnumerator SnapToSpecificItem(IFS_PlaceHolder placeHolder)
    {
        _isSnapping = true;

        Vector2 targetPosition = CalculateSnapTargetPosition(placeHolder);
        Vector2 startPosition = _scrollRect.content.anchoredPosition;

        float elapsedTime = 0f;
        float duration = 0.3f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            _scrollRect.content.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        _scrollRect.content.anchoredPosition = targetPosition;
        _scrollRect.velocity = Vector2.zero;
        _isSnapping = false;
        _snapCoroutine = null;
    }
}
