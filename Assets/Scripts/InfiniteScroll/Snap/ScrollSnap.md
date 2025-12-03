# ScrollSnap Component

Component tự động snap các item về vị trí cố định khi scroll dừng, tạo trải nghiệm mượt mà và chuyên nghiệp cho infinite scroll list.

## Tính năng chính

- **Auto Snap**: Tự động snap item gần nhất về vị trí target khi scroll dừng
- **Velocity-Based**: Snap speed đồng bộ với tốc độ scroll hiện tại
- **Smart Prediction**: Dự đoán vị trí cuối để chọn target item chính xác
- **Smooth Deceleration**: Giảm tốc tự nhiên thay vì dừng đột ngột
- **Configurable**: Nhiều parameters để tùy chỉnh behavior

## Setup

### Bước 1: Thêm Component
1. Chọn GameObject có component `IFS_Data` (infinite scroll list)
2. Add Component → `ScrollSnap`
3. Component sẽ tự động setup và hoạt động

### Bước 2: Cấu hình (Optional)
Điều chỉnh các parameters trong Inspector theo nhu cầu.

## Parameters

### Snap Settings

#### Enable Snap
- **Type**: `bool`
- **Default**: `true`
- **Mô tả**: Bật/tắt chức năng snap

#### Snap Speed
- **Type**: `float`
- **Default**: `10`
- **Range**: > 0.1
- **Mô tả**: Tốc độ snap tối thiểu (khi scroll rất chậm)
- **Gợi ý**: 10-20 cho smooth, 30-50 cho nhanh

#### Snap Threshold
- **Type**: `float`
- **Default**: `0.1`
- **Range**: > 0.01
- **Mô tả**: Khoảng cách tối thiểu để trigger snap (pixels)
- **Gợi ý**: Giữ mặc định 0.1

#### Snap Deceleration
- **Type**: `float`
- **Default**: `0.92`
- **Range**: `0.5 - 0.99`
- **Mô tả**: Độ giảm tốc mỗi frame
  - `0.5`: Giảm tốc rất nhanh
  - `0.92`: Giảm tốc vừa phải (recommended)
  - `0.99`: Giảm tốc rất chậm
- **Gợi ý**: 0.90-0.95

#### Velocity Prediction Factor
- **Type**: `float`
- **Default**: `0.5`
- **Range**: `0 - 1`
- **Mô tả**: Hệ số dự đoán vị trí dựa trên velocity
  - `0`: Không dự đoán (chọn item tại vị trí hiện tại)
  - `0.5`: Dự đoán vừa phải (recommended)
  - `1`: Dự đoán xa nhất
- **Gợi ý**: 0.3-0.6 tùy cảm giác

### Snap Position

#### Snap Position Normalized
- **Type**: `float`
- **Default**: `0.5`
- **Range**: `0 - 1`
- **Mô tả**: Vị trí snap trong viewport
  - `0`: Top/Left edge
  - `0.5`: Center (recommended)
  - `1`: Bottom/Right edge

### Scroll Detection

#### Scroll Stop Delay
- **Type**: `float`
- **Default**: `0.15`
- **Range**: > 0.01
- **Mô tả**: Thời gian chờ để detect scroll đã dừng (seconds)
- **Gợi ý**: 0.1-0.2

#### Velocity Threshold
- **Type**: `float`
- **Default**: `10`
- **Range**: ≥ 0
- **Mô tả**: Ngưỡng velocity để coi như scroll đã dừng
- **Gợi ý**: 5-20

### Debug

#### Debug Mode
- **Type**: `bool`
- **Default**: `false`
- **Mô tả**: Bật log debug để kiểm tra snap behavior

## Cách hoạt động

### 1. Detection Phase
```
User scrolls → Release → Scroll gradually stops
                          ↓
                    Velocity < threshold
                          ↓
                    Delay > scrollStopDelay
                          ↓
                    Trigger Snap
```

### 2. Target Selection
```
Get current velocity
        ↓
Calculate predicted position (velocity × prediction factor)
        ↓
Find nearest item to snap position (at predicted position)
        ↓
Calculate target scroll position
```

### 3. Snap Animation
```
Get current scroll velocity as initial snap speed
        ↓
Each frame:
  - Move toward target
  - Speed = Speed × snapDeceleration
  - Until: distance < threshold
        ↓
Snap complete
```

## Public Methods

### SetEnableSnap(bool enable)
Bật/tắt snap programmatically.

```csharp
scrollSnap.SetEnableSnap(false); // Disable snap
scrollSnap.SetEnableSnap(true);  // Enable snap
```

### SetSnapPosition(float normalizedPosition)
Thay đổi vị trí snap (0-1).

```csharp
scrollSnap.SetSnapPosition(0.5f);  // Snap to center
scrollSnap.SetSnapPosition(0.3f);  // Snap to 30% from top
```

### SetSnapSpeed(float speed)
Thay đổi snap speed.

```csharp
scrollSnap.SetSnapSpeed(15f);  // Medium speed
scrollSnap.SetSnapSpeed(30f);  // Fast speed
```

### SnapToIndex(int index)
Snap đến item cụ thể bằng index.

```csharp
scrollSnap.SnapToIndex(0);   // Snap to first item
scrollSnap.SnapToIndex(5);   // Snap to 6th item
```

## Ví dụ sử dụng

### Example 1: Basic Setup
```csharp
// Simply add component, it works automatically
// No code needed!
```

### Example 2: Toggle Snap On/Off
```csharp
public class ScrollController : MonoBehaviour
{
    private ScrollSnap scrollSnap;

    void Start()
    {
        scrollSnap = GetComponent<ScrollSnap>();
    }

    public void OnToggleSnap(bool isOn)
    {
        scrollSnap.SetEnableSnap(isOn);
    }
}
```

### Example 3: Navigate to Specific Item
```csharp
public class MenuController : MonoBehaviour
{
    private ScrollSnap scrollSnap;

    void Start()
    {
        scrollSnap = GetComponent<ScrollSnap>();
    }

    public void NavigateToPage(int pageIndex)
    {
        // Disable snap during navigation
        scrollSnap.SetEnableSnap(false);

        // Snap to target page
        scrollSnap.SnapToIndex(pageIndex);

        // Re-enable snap after animation
        StartCoroutine(EnableSnapAfterDelay(0.5f));
    }

    IEnumerator EnableSnapAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        scrollSnap.SetEnableSnap(true);
    }
}
```

### Example 4: Dynamic Snap Position
```csharp
public class CarouselController : MonoBehaviour
{
    private ScrollSnap scrollSnap;

    void Start()
    {
        scrollSnap = GetComponent<ScrollSnap>();

        // Snap to top third for header layout
        scrollSnap.SetSnapPosition(0.33f);
    }

    public void SwitchToFullscreen()
    {
        // Snap to center for fullscreen
        scrollSnap.SetSnapPosition(0.5f);
    }
}
```

## Tips & Best Practices

### Performance
- Snap chỉ hoạt động khi scroll dừng, không ảnh hưởng performance khi scroll
- Debug mode nên tắt trong production build

### UX Design
1. **Snap Position**: Thường dùng `0.5` (center) cho UI cân đối
2. **Prediction Factor**: `0.3-0.6` cho cảm giác tự nhiên
3. **Deceleration**: `0.90-0.95` cho smooth animation

### Common Use Cases

#### Card Carousel
```
Snap Position: 0.5 (center)
Prediction Factor: 0.4
Deceleration: 0.93
```

#### Header Menu
```
Snap Position: 0.2 (near top)
Prediction Factor: 0.5
Deceleration: 0.95
```

#### Gallery View
```
Snap Position: 0.5 (center)
Prediction Factor: 0.3
Deceleration: 0.90
```

### Troubleshooting

#### Snap quá nhanh/chậm
- Điều chỉnh `Snap Speed` (tốc độ tối thiểu)
- Điều chỉnh `Snap Deceleration` (càng cao càng chậm)

#### Snap sai item
- Tăng `Velocity Prediction Factor` nếu scroll nhanh
- Giảm `Velocity Prediction Factor` nếu scroll chậm
- Bật `Debug Mode` để kiểm tra

#### Snap không trigger
- Kiểm tra `Velocity Threshold` (có thể quá thấp)
- Kiểm tra `Scroll Stop Delay` (có thể quá cao)

#### Giật khi snap
- Tăng `Velocity Prediction Factor` (0.5-0.7)
- Điều chỉnh `Snap Deceleration` (0.92-0.95)

## Technical Notes

### Requirements
- Cần component `IFS_Data` trên cùng GameObject
- Tự động access `ScrollRect` và placeholders qua reflection
- Hỗ trợ cả vertical và horizontal scroll

### Compatibility
- Unity 2019.4+
- Infinite Scroll Framework
- Horizontal & Vertical layouts

### Event Flow
```
OnEndDrag → CheckScrollStop → FindNearestItem → SnapToItem
                ↑
OnScrollValueChanged (nếu có momentum)
```

## Version History

### Current Version
- Velocity-based snap speed
- Smart prediction
- Smooth deceleration
- Configurable parameters

---

**Created by**: QuangTD
**Component**: ScrollSnap.cs
**Location**: Assets/QuangTD/InfiniteScroll/Snap/
