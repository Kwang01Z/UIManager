# Phân Tích Hệ Thống Localization

Hệ thống Localization trong dự án được thiết kế để quản lý đa ngôn ngữ một cách linh hoạt, hiệu quả và tối ưu hiệu suất (Zero GC). Hệ thống hỗ trợ cả nội dung văn bản (Text) từ CSV và các tài sản đồ họa/âm thanh (Asset) thông qua ScriptableObject.

## 1. Thành Phần Chính

### Core (Lõi)
- **`LocalizationManager`**: Lớp tĩnh điều phối toàn bộ hệ thống.
    - Quản lý ngôn ngữ hiện tại (`CurrentLanguage`).
    - Cache dữ liệu văn bản vào `Dictionary<string, string>` để truy xuất nhanh $O(1)$ và không tạo rác.
    - Tự động khởi tạo trước khi Scene đầu tiên load (`BeforeSceneLoad`).
- **`LocalizationConfig` (ScriptableObject)**: Lưu trữ cấu hình toàn cục.
    - Danh sách ngôn ngữ hỗ trợ.
    - Cấu hình URL Google Sheets để đồng bộ dữ liệu.
    - Mapping Asset (Sprite, Audio) theo từ khóa và ngôn ngữ.
- **`LanguageCode`**: Enum định nghĩa các mã ngôn ngữ (vi, en, ja, ko...).

### Components (Thành phần UI)
- **`LocalizeBase`**: Lớp trừu tượng cơ sở cho các script tự động hóa localization.
- **`LocalizeTMP`**: Tự động cập nhật văn bản cho TextMeshPro.
- **`LocalizeSprite`**: Tự động thay đổi hình ảnh theo ngôn ngữ.
- **`LocalizeAudio`**: Tự động thay đổi Clip âm thanh.

### Dữ liệu & Đồng bộ
- **`GoogleSheetsDownloader`**: Sử dụng **UniTask** để tải dữ liệu CSV (Publish URL) từ Google Sheets về máy, lưu cache tại `persistentDataPath`.

---

## 2. Luồng Hoạt Động (Workflow)

### Khởi tạo
1. Khi ứng dụng chạy, `LocalizationManager.Init()` được gọi.
2. Hệ thống kiểm tra ngôn ngữ đã lưu trong `PlayerPrefs`, nếu không có sẽ lấy ngôn ngữ hệ thống.
3. Dữ liệu CSV được nạp từ `persistentDataPath` (ưu tiên bản tải về mới nhất) hoặc từ `Resources` (bản fallback).

### Cập nhật ngôn ngữ
1. Khi `LocalizationManager.CurrentLanguage` thay đổi:
    - Lưu lại vào `PlayerPrefs`.
    - Xóa và nạp lại Cache từ điển mới.
    - Kích hoạt sự kiện `OnLanguageChanged`.
2. Các component trên UI (Text, Sprite...) nhận sự kiện và tự gọi hàm `OnLocalize()` để hiển thị nội dung mới.

---

## 3. Đặc Điểm Kỹ Thuật & Tối Ưu

- **UniTask Integration**: Mọi thao tác bất đồng bộ (tải web) đều sử dụng UniTask thay vì Coroutine truyền thống, giúp code sạch và dễ quản lý lỗi.
- **Hiệu Năng**:
    - Truy xuất văn bản qua Dictionary tránh việc duyệt tìm chuỗi thủ công.
    - Việc nạp dữ liệu vào Cache RAM giúp tránh truy cập IO file liên tục.
- **Mở rộng**:
    - Hỗ trợ tích hợp Gemini AI (có trường API Key trong Config) cho mục đích dịch tự động hoặc xử lý ngôn ngữ trong Editor.
    - Hỗ trợ đồng bộ trực tiếp từ Google Sheets, giúp Game Designer có thể chỉnh sửa nội dung mà không cần can thiệp vào Unity Editor.

---

## 4. Cách Sử Dụng

### Gán Text cho UI
1. Thêm component `LocalizeTMP` vào GameObject có TextMeshPro.
2. Nhập `Key` tương ứng với cột đầu tiên trong file CSV.

### Gán Asset (Ảnh/Âm thanh)
1. Mở file `LocalizationConfig` (đặt trong Resources).
2. Thêm một mục mới trong `Asset Entries`.
3. Nhập `Key` và kéo các Asset tương ứng cho từng ngôn ngữ.
4. Thêm component `LocalizeSprite` hoặc `LocalizeAudio` vào GameObject và nhập `Key`.

### Chuyển đổi ngôn ngữ bằng code
```csharp
LocalizationManager.CurrentLanguage = LanguageCode.en;
```

---
*Bản phân tích được tổng hợp bởi Antigravity.*
