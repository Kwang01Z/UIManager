# Layer Manager Debugger Tool

## Tổng quan

Layer Manager Debugger là một công cụ Editor Window dành cho Unity để debug và monitor hệ thống Layer Manager trong thời gian thực. Công cụ này giúp developer theo dõi trạng thái các layer, sorting order và lịch sử hoạt động mà không ảnh hưởng đến hiệu năng runtime.

## Tính năng

### 🔍 **Theo dõi trạng thái hiện tại**
- Hiển thị các layer đang hoạt động với sorting order
- Thông tin về LayerManager (IsShowing, SpaceBetweenLayer, LimitLayer)
- Stack các group đang hiển thị
- Trạng thái active/inactive của từng layer

### 📊 **Lịch sử Groups**
- Theo dõi các action Show/Close của các group
- Thông tin về loại group (Root, Popup, FullScreen, etc.)
- Frame count khi action xảy ra
- Danh sách layers trong group

### 🎯 **Lịch sử Layers**
- Lịch sử sorting order của từng layer
- Action cuối cùng thực hiện (Show, Hide, Sorting Updated)
- Frame count của action cuối cùng
- Chuỗi sorting order history

## Cách sử dụng

### 1. Mở Debugger Window

```
Menu → Window → UI Manager → Layer Manager Debugger
```

### 2. Chạy game để xem thông tin

Debugger chỉ hoạt động khi game đang chạy (Play mode).

### 3. Các nút điều khiển

- **Auto Refresh**: Tự động refresh mỗi 0.5 giây
- **Refresh**: Refresh thủ công
- **Clear History**: Xóa lịch sử đã lưu

## Kiến trúc

### LayerManagerDebugger.cs
- Editor Window chính để hiển thị giao diện
- Chỉ hoạt động trong Unity Editor
- Không ảnh hưởng đến hiệu năng runtime

### LayerManagerDebugTracker.cs
- MonoBehaviour để thu thập dữ liệu trong runtime
- Tự động tạo GameObject ẩn khi cần thiết
- Giới hạn lịch sử để tránh memory leak (100 groups, 10 sorting orders per layer)

## Nguyên tắc hoạt động

### Không ảnh hưởng hiệu năng
- Chỉ thu thập dữ liệu cần thiết
- Sử dụng cấu trúc dữ liệu tối ưu
- Giới hạn số lượng lịch sử lưu trữ

### Thread-safe
- Chỉ hoạt động trên main thread
- Không sử dụng multi-threading

### Editor-only
- Chỉ compile trong Unity Editor
- Không bao gồm trong build cuối cùng

## Ví dụ sử dụng

```csharp
// Trong game code - hoạt động bình thường
var showData = ShowLayerGroupData.Build(LayerGroupType.Popup, LayerType.MainMenu)
    .AddLayer(LayerType.Background);

LayerManager.Instance.ShowGroupLayerAsync(showData);

// Trong Editor - mở Debugger để xem:
// - Group mới được tạo với type Popup
// - Layer MainMenu và Background được hiển thị
// - Sorting order được tính toán tự động
// - Lịch sử được ghi lại
```

## Debug thông tin hiển thị

### Trạng thái hiện tại
```
=== TRẠNG THÁI HIỆN TẠI ===
Is Showing: False
Space Between Layer: 100
Limit Layer: 64

Layers đang hiển thị:
• MainMenu    Sorting: 204    Active: true
• Background  Sorting: 203    Active: true

Stack Groups:
• Group 1: Popup    ID: 1    Layers: MainMenu, Background
```

### Lịch sử Groups
```
=== LỊCH SỬ GROUPS ===
Frame 1250    Action: Show    Type: Popup
Layers: MainMenu, Background
ID: 1
```

### Lịch sử Layers
```
=== LỊCH SỬ LAYERS ===
Layer: MainMenu
Current Sorting: 204    Last Action: Show    Frame: 1245
Sorting History: 0 → 101 → 204

Layer: Background
Current Sorting: 203    Last Action: Show    Frame: 1245
Sorting History: 0 → 203
```

## Lưu ý quan trọng

1. **Chỉ dành cho debug**: Không sử dụng trong production code
2. **Không ảnh hưởng performance**: Được thiết kế để không gây overhead
3. **Tự động cleanup**: Lịch sử được giới hạn để tránh memory leak
4. **Editor only**: Không được include trong build

## Troubleshooting

### Không thấy thông tin
- Đảm bảo game đang ở Play mode
- Kiểm tra có LayerManager trong scene không
- DebugTracker sẽ tự động được tạo khi cần thiết

### Lịch sử trống
- Một số action có thể không được track đầy đủ
- Kiểm tra lại logic track trong LayerManagerDebugTracker

### Performance issues
- Nếu gặp vấn đề hiệu năng, tắt Auto Refresh
- Sử dụng Refresh thủ công khi cần thiết
