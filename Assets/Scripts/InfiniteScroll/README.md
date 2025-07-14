# Hệ Thống Infinite Scroll

## Tổng quan
Hệ thống Infinite Scroll được thiết kế để tối ưu hiệu suất khi hiển thị danh sách dài bằng cách chỉ render các phần tử đang nằm trong tầm nhìn (viewport).

## Cấu trúc thư mục
```
InfiniteScroll/
├── Cursor/           # Xử lý logic con trỏ và vị trí
├── Invisible/        # Các thành phần ẩn
├── Root/             # Các lớp cốt lõi
├── Snap/             # Chức năng snap
└── Test/             # Các test case
```

## Các thành phần chính

### 1. InfiniteScrollData
Lớp chính quản lý toàn bộ logic của infinite scroll.

#### Thuộc tính
- `ScrollRect scrollRect`: Thành phần ScrollRect của Unity
- `GridLayoutGroup.Axis scrollType`: Loại cuộn (ngang/dọc)
- `Vector4D padding`: Khoảng cách đệm
- `Vector2 spacing`: Khoảng cách giữa các phần tử
- `Vector2 ContentSize`: Kích thước nội dung
- `RectTransform ContentRect`: RectTransform của nội dung
- `RectTransform ViewportRect`: RectTransform của viewport

#### Phương thức chính
- `void InitData(List<InfiniteScrollPlaceHolder> placeHolders)`
  - Khởi tạo dữ liệu với danh sách placeholders
  - Xóa dữ liệu cũ và thêm dữ liệu mới

- `void ClearData()`
  - Xóa toàn bộ dữ liệu và giải phóng tài nguyên

- `void AddDataRange(List<InfiniteScrollPlaceHolder> placeHolders)`
  - Thêm nhiều placeholders vào danh sách
  - Tính toán lại vị trí các phần tử

- `void ReloadData()`
  - Tải lại toàn bộ dữ liệu
  - Tính toán lại vị trí và cập nhật hiển thị

- `private void CalculateVisible()`
  - Tính toán các phần tử đang hiển thị trong viewport
  - Cập nhật trạng thái hiển thị của từng phần tử

### 2. InfiniteScrollElement
Đại diện cho một phần tử trong danh sách.

#### Thuộc tính
- `IDataLoader DataLoaderPF`: Component xử lý dữ liệu
- `IFS_ElementType elementType`: Loại phần tử
- `int numberFixed`: Số phần tử tối đa trên một hàng/cột
- `RectTransform RectTransform`: Thành phần RectTransform

#### Phương thức chính
- `void SetupData(Vector2 anchoredPosition, Vector4D margin, object data)`
  - Thiết lập vị trí và dữ liệu cho phần tử
  - Tự động điều chỉnh kích thước nếu cần

- `void ValidateNumberFixed()`
  - Kiểm tra và xác thực số lượng phần tử cố định

### 3. InfiniteScrollPlaceHolder
Quản lý trạng thái hiển thị của từng phần tử.

#### Phương thức chính
- `void SetVisible(bool isVisible)`
  - Cập nhật trạng thái hiển thị của phần tử
  - Gọi các sự kiện tương ứng khi thay đổi trạng thái

- `void UpdateData(Transform parent)`
  - Cập nhật dữ liệu và hiển thị của phần tử

## Các giao diện

### IDataLoader
```csharp
public interface IDataLoader
{
    void SetupData(object data);
}
```

### IInfiniteScrollVisible
```csharp
public interface IInfiniteScrollVisible
{
    bool IsVisible(InfiniteScrollPlaceHolder holder, InfiniteScrollData scrollData);
}
```

### IInfiniteScrollCursor
```csharp
public interface IInfiniteScrollCursor
{
    Vector2 CalculateAnchoredPosition(List<InfiniteScrollPlaceHolder> holders, InfiniteScrollData scrollData);
}
```

## Hướng dẫn sử dụng

1. Tạo một đối tượng chứa ScrollRect
2. Thêm component `InfiniteScrollData` vào đối tượng
3. Tạo prefab cho các phần tử với component `InfiniteScrollElement`
4. Triển khai `IDataLoader` để xử lý dữ liệu
5. Gọi `InitData()` để khởi tạo danh sách

## Ví dụ

```csharp
// Khởi tạo dữ liệu
var placeHolders = new List<InfiniteScrollPlaceHolder>();
// Thêm các placeholders vào danh sách
scrollData.InitData(placeHolders);

// Thêm dữ liệu mới
scrollData.AddDataRange(newPlaceHolders);

// Làm mới dữ liệu
scrollData.ReloadData();
```

## Tối ưu hóa
- Chỉ render các phần tử đang hiển thị
- Sử dụng object pooling để tái sử dụng đối tượng
- Hỗ trợ xử lý đa luồng với Parallel.ForEach
- Tự động điều chỉnh kích thước phần tử

## Giới hạn
- Chưa hỗ trợ kéo thả (drag and drop)
- Cần tối ưu thêm cho danh sách cực lớn

## Tương lai
- Thêm hiệu ứng chuyển động mượt mà
- Hỗ trợ kéo thả sắp xếp
- Tự động tải thêm khi cuộn đến gần cuối
