# Tài Liệu Hệ Thống Quản Lý Layer

## Tổng Quan
Hệ thống Quản lý Layer là một kiến trúc quản lý layer UI toàn diện được xây dựng trên Unity, xử lý các tình huống layer UI phức tạp với các hành vi khác nhau (cửa sổ popup, toàn màn hình, thông báo, v.v.) trong khi cung cấp một API sạch và dễ sử dụng.

## Thành Phần Chính

### 1. LayerManager (Singleton)
- **Mục đích**: Trung tâm điều phối quản lý tất cả các layer UI trong ứng dụng
- **Tính năng**: Hoạt động bất đồng bộ, tải trước layer, điều hướng theo ngăn xếp, quản lý thứ tự z

### 2. LayerBase
- **Mục đích**: Lớp cơ sở mà tất cả các layer UI phải kế thừa
- **Yêu cầu**: Tự động đảm bảo các thành phần Canvas, CanvasGroup và RectTransform
- **Tính năng**: Quản lý hiển thị, theo dõi thứ tự sắp xếp, phương thức vòng đời

### 3. LayerGroup
- **Mục đích**: Nhóm nhiều layer lại với nhau và quản lý chúng như một đơn vị
- **Tính năng**: Các hoạt động nhóm, gán thứ tự z liên tiếp

### 4. LayerReferenceSO (ScriptableObject)
- **Mục đích**: Container cấu hình ánh xạ LayerType với prefab LayerBase
- **Tính năng**: Cấu hình thông qua Inspector, tìm kiếm thời gian chạy

## Các Loại Nhóm Layer

| Loại | Hành vi |
|------|---------|
| Root | Thay thế các group điều hướng khác; giữ Fixed |
| FullScreen | Ẩn nền, đóng popup; được thêm vào stack để hỗ trợ Back |
| Popup | Cửa sổ popup bình thường với hành vi tiêu chuẩn |
| Notify | Không vào stack; được đóng khi mở group tiếp theo |
| Fixed | Không vào stack; luôn nằm trên các group điều hướng, giữ qua Back và đổi Root |

### Quy tắc điều hướng

- Open, Back và CloseAll dùng chung hàng đợi, theo thứ tự gọi. Request mở được sao chép khi gửi để callback và danh sách layer không bị ghi đè bởi lần gọi sau.
- Mở lại bất kỳ group nào đã có trong stack sẽ đóng tất cả group phía sau nó, rồi hiển thị lại group đó. Ví dụ `Root → A → B → C → A` còn `Root → A`; Back tiếp theo về Root. Quy tắc này áp dụng cả Popup và Custom, không phụ thuộc `CloseOtherLayerOver`.
- Sorting và visibility được tính từ các group còn tồn tại. Layer dùng chung giữ quyền sở hữu của group phía dưới khi group phía trên đóng.
- `CloseAllLayerGroups()` giữ group cuối nếu `hasLayerRoot = true`, và đóng hết group trong stack nếu bằng `false`. Fixed không thuộc stack.
- Lỗi khởi tạo/callback được ghi log và không chặn các request kế tiếp. Hủy manager sẽ hủy phần chờ frame và bỏ các request còn lại.
- `OnShowComplete` chạy một frame sau khi gọi hiển thị. Luồng này chưa tích hợp `TransitionProcessor`/`LayerAnimationBase`, nên callback không đại diện cho kết thúc animation.

### Hiệu năng

- Stack dùng `List` theo thứ tự đáy → đỉnh, duyệt bằng chỉ số và xóa tại chỗ; đường chuyển UI không dùng LINQ hay tạo collection tạm để tính sorting/visibility.
- Mỗi layer tái sử dụng buffer sorting. Reconcile duyệt các group còn tồn tại và layer có quyền sở hữu trước/sau lần chuyển; không quét toàn bộ cache prefab mỗi lần. Preload được kiểm tra một lần khi bắt đầu điều hướng.
- Chỉ ghi `Canvas.enabled`, `sortingOrder` và các thuộc tính CanvasGroup khi giá trị thay đổi. Hide/Close vẫn tắt Canvas và raycast; Show/Back khôi phục Canvas theo trạng thái bật/tắt toàn bộ UI.
- Request mở vẫn tạo snapshot độc lập. `LayerGroup` chỉ được tạo khi có callback cần nhận nó; các object có thể được callback giữ lại không bị pool/reuse. Lần đầu tạo layer, tăng độ sâu stack hoặc kích thước group vẫn có thể cấp phát để mở rộng buffer.
- Với gameplay polling, dùng `IsLayerShowing`, `CopyShowingLayerTypesTo` và `LayerGroup.CopyLayerTypesTo` với list tái sử dụng. Các API trả snapshot collection cũ vẫn tạo bản sao độc lập.
- `CloseAll` giữ callback và trạng thái trung gian của từng lần đóng. Callback, instantiate prefab và công cụ debugger có chi phí riêng ngoài phần reconcile.
- Dùng marker `LayerManager.Reconcile` trong Unity Profiler để đo CPU/GC thực tế. Kết quả .NET harness không đại diện cho thời gian layout/rebuild/rendering của Unity.

## Tạo Layer Mới

### Bước 1: Định Nghĩa Loại Layer
Thêm một giá trị mới vào enum `LayerType` trong file `LayerSourcePath.cs`:

```csharp
public enum LayerType
{
    Layer01 = 1,
    Layer02 = 2,
    Layer03 = 3,
    Layer04 = 4,
    Layer05 = 5,
    Layer06 = 6,
    // Thêm loại layer mới của bạn vào đây
    MyNewLayer = 7
}
```

### Bước 2: Tạo Prefab Layer
1. Tạo một GameObject mới trong scene
2. Thêm các thành phần sau:
   - Canvas (với overrideSorting = true)
   - CanvasGroup
   - RectTransform
3. Tạo các phần tử UI của bạn làm con của GameObject này
4. Chuyển GameObject này thành prefab trong project

### Bước 3: Cấu Hình Layer
1. Trong Unity Editor, tạo một asset `LayerReferenceSO` mới:
   - Chuột phải trong cửa sổ Project → Create → UIManager → LayerReferenceSO
2. Trong Inspector, thêm một mục mới vào `layerReferenceList`
3. Đặt `layerType` thành giá trị LayerType mới của bạn
4. Gán prefab layer của bạn vào trường `layerBase`

### Bước 4: Tạo Triển Khai Layer
Tạo một lớp mới kế thừa từ `LayerBase`:

```csharp
using UnityEngine;

public class MyNewLayer : LayerBase
{
    // Thêm các trường cụ thể cho layer của bạn

    public override void InitData()
    {
        base.InitData();
        // Khởi tạo dữ liệu cụ thể cho layer tại đây
    }

    public override void ShowLayerAsync()
    {
        base.ShowLayerAsync();
        // Thêm logic hiển thị tùy chỉnh tại đây
    }

    public override void CloseLayerAsync(bool force = false)
    {
        base.CloseLayerAsync(force);
        // Thêm logic đóng tùy chỉnh tại đây
    }
}
```

### Bước 5: Cập Nhật LayerSourcePath (Tùy chọn)
Nếu bạn sử dụng addressable assets, thêm đường dẫn layer của bạn vào lớp `LayerSourcePath`:

```csharp
public static class LayerSourcePath
{
    public const string Layer01 = "Layers/LayerTest01";
    public const string MyNewLayer = "Layers/MyNewLayer"; // Thêm dòng này
    // ... các đường dẫn khác
}
```

## Sử Dụng Layer Trong Mã

### Hiển Thị Một Layer Đơn
Sử dụng các phương thức trợ giúp trong `ShowLayerHelper.cs` hoặc tạo của riêng bạn:

```csharp
// Sử dụng các phương thức trợ giúp được tạo tự động
LayerManager.Instance.ShowLayer01(LayerGroupType.Popup, (layerGroup) =>
{
    // Được gọi khi layer được hiển thị hoàn toàn
    Debug.Log("Layer01 hiện đã hiển thị");
});
```

### Hiển Thị Cấu Hình Layer Tùy Chỉnh
Để có nhiều quyền kiểm soát hơn đối với hành vi của layer:

```csharp
// Tạo cấu hình nhóm layer tùy chỉnh
var showData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.MyNewLayer);
showData.OnInitData = (layerGroup) =>
{
    if(layerGroup.GetLayerBase(LayerType.MyNewLayer, out var layerBase))
    {
        // Khởi tạo dữ liệu layer của bạn tại đây
        var myLayer = layerBase as MyNewLayer;
        if(myLayer != null)
        {
            myLayer.SomeProperty = someValue;
        }
    }
};
showData.OnShowComplete = (layerGroup) =>
{
    Debug.Log("Layer tùy chỉnh đã sẵn sàng!");
};

// Hiển thị layer
LayerManager.Instance.ShowGroupLayerAsync(showData);
```

### Quản Lý Nhiều Layer
Để hiển thị nhiều layer cùng nhau như một nhóm:

```csharp
var showData = new ShowLayerGroupData
{
    LayerGroupType = LayerGroupType.FullScreen
};
showData.LayerTypes.Add(LayerType.Layer01);
showData.LayerTypes.Add(LayerType.Layer02);

LayerManager.Instance.ShowGroupLayerAsync(showData);
```

### Đóng Layer
```csharp
// Đóng một loại layer cụ thể trực tiếp
var layerBase = LayerManager.Instance.GetLayerBase(LayerType.MyNewLayer);
if(layerBase != null)
{
    layerBase.CloseLayerAsync();
}

// Đóng nhóm layer cuối cùng (cho điều hướng quay lại)
LayerManager.Instance.CloseLastLayerGroup();
```

## Tùy Chỉnh Nâng Cao

### Thuộc Tính ShowLayerGroupData
- `CloseAllOtherLayer`: Nếu đúng, đóng tất cả các layer khác khi layer này xuất hiện
- `HideAllOtherLayer`: Nếu đúng, ẩn (nhưng không đóng) các layer khác
- `CloseAllPopup`: Nếu đúng, đóng tất cả các layer popup
- `CloseOtherLayerOver`: Nếu đúng, đóng các layer phía trên layer này trong ngăn xếp
- `AddToStack`: Nếu sai, layer sẽ không được thêm vào ngăn xếp điều hướng quay lại
- `FixedLayer`: Nếu đúng, đánh dấu layer là cố định
- `DisplayImmediately`: Nếu sai, hiển thị layer sau khi khởi tạo hoàn tất

### Tải Trước Layer
Để cải thiện hiệu suất, bạn có thể tải trước các layer trong thành phần LayerManager:
1. Chọn GameObject LayerManager trong scene của bạn
2. Tìm danh sách `layerPreload` trong Inspector
3. Thêm các mục để tải trước các loại layer cụ thể khi khởi động

## Thực Hành Tốt Nhất

1. **Sử dụng LayerGroupType thích hợp**: Chọn loại nhóm đúng cho mục đích UI của bạn
2. **Triển khai dọn dẹp đúng cách**: Ghi đè CloseLayerAsync để dọn tài nguyên
3. **Tải trước các layer thường xuyên sử dụng**: Thêm các layer phổ biến vào danh sách tải trước
4. **Sử dụng nhóm layer cho các UI liên quan**: Nhóm các phần tử UI liên quan lại với nhau
5. **Tận dụng hệ thống callback**: Sử dụng OnInitData và OnShowComplete để khởi tạo
6. **Cân nhắc khoảng cách thứ tự z**: Điều chỉnh spaceBetweenLayer nếu cần cho các phân cấp UI phức tạp
