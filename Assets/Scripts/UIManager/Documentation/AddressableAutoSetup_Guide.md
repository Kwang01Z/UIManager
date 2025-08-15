# Addressable Auto-Setup Guide

## 🎯 Tổng quan

Tính năng **Addressable Auto-Setup** tự động thiết lập Addressable entries cho tất cả UI Layer prefabs với key pattern chuẩn `Layers/{PrefabName}`, giúp tự động hóa workflow setup và đảm bảo consistency.

---

## 🔧 Cách hoạt động

### Automatic Setup (OnValidate)
Mỗi khi LayerBase prefab được sửa đổi trong Editor, system sẽ tự động:
1. **Kiểm tra** prefab đã có Addressable entry chưa
2. **Tạo mới** entry nếu chưa có với key `Layers/{PrefabName}`
3. **Cập nhật** key nếu không đúng pattern
4. **Log kết quả** setup để tracking

### Pattern Convention
```
Prefab Name: MainMenuLayer.prefab
Addressable Key: Layers/MainMenuLayer

Prefab Name: GameplayLayer.prefab  
Addressable Key: Layers/GameplayLayer
```

---

## 📋 Editor Menu Commands

### `UIManager/Addressables/Auto-Setup All Layer Prefabs`
**Mục đích**: Batch setup tất cả LayerBase prefabs trong project
**Kết quả**:
```
=== Addressable Auto-Setup Results ===
✅ Created: 3
🔄 Updated: 2  
✅ Already Configured: 5
❌ Failed: 0
```

### `UIManager/Addressables/Validate All Layer Prefabs`
**Mục đích**: Kiểm tra tất cả prefabs có đúng setup không
**Kết quả**: Báo cáo các issues nếu có

### `UIManager/Addressables/Create UI Layers Group`
**Mục đích**: Tạo dedicated Addressable group "UI Layers"
**Lợi ích**: Tổ chức tốt hơn và có thể optimize riêng cho UI

### `UIManager/Addressables/Move Layers to UI Group`
**Mục đích**: Di chuyển tất cả layer entries vào UI Layers group

### `UIManager/Addressables/Generate Setup Report`
**Mục đích**: Tạo báo cáo chi tiết về trạng thái setup hiện tại

---

## 🚀 Workflow sử dụng

### Setup ban đầu

1. **Khởi tạo Addressables** (nếu chưa có):
   ```
   Window > Asset Management > Addressables > Groups
   → Create Addressables Settings
   ```

2. **Auto-setup tất cả layers**:
   ```
   UIManager/Addressables/Auto-Setup All Layer Prefabs
   ```

3. **Tạo dedicated group** (Optional):
   ```
   UIManager/Addressables/Create UI Layers Group
   UIManager/Addressables/Move Layers to UI Group
   ```

### Workflow hàng ngày

1. **Tạo prefab mới** với LayerBase component
2. **Save prefab** → Auto-setup sẽ chạy tự động
3. **Validate** định kỳ để đảm bảo consistency:
   ```
   UIManager/Addressables/Validate All Layer Prefabs
   ```

---

## ⚙️ Technical Details

### Auto-Setup Logic
```csharp
protected virtual void OnValidate()
{
    gameObject.SetActive(false);
    
    #if UNITY_EDITOR
    // Auto-setup Addressable if not already configured
    AutoSetupAddressable();
    #endif
}
```

### Key Generation Logic
```csharp
string prefabName = Path.GetFileNameWithoutExtension(assetPath);
string addressableKey = $"Layers/{prefabName}";
```

### Safety Checks
- ✅ Chỉ chạy trong Editor mode
- ✅ Chỉ process prefab assets  
- ✅ Kiểm tra Addressable settings tồn tại
- ✅ Exception handling đầy đủ
- ✅ Không overwrite manual settings

---

## 🔍 Troubleshooting

### ❌ "Addressable settings not found"
**Nguyên nhân**: Addressables chưa được khởi tạo
**Giải pháp**:
```
Window > Asset Management > Addressables > Groups
→ Create Addressables Settings
```

### ❌ "No default Addressable group found"
**Nguyên nhân**: Default group bị xóa
**Giải pháp**: Tạo lại default group trong Addressables window

### ⚠️ "Key mismatch" warnings
**Nguyên nhân**: Manual changes to addressable keys
**Giải pháp**: 
- Để system tự động fix: `Auto-Setup All Layer Prefabs`
- Hoặc manually update key theo pattern

### 🔄 Keys không update tự động
**Nguyên nhân**: OnValidate không được gọi
**Giải pháp**:
- Force refresh: Select prefab và nhấn Ctrl+R
- Hoặc chạy batch command: `Auto-Setup All Layer Prefabs`

---

## 📊 Best Practices

### ✅ Recommended
- **Đặt tên prefab** theo LayerType enum để consistency
- **Sử dụng UI Layers group** để tổ chức tốt hơn  
- **Validate định kỳ** để đảm bảo setup đúng
- **Generate report** trước khi build release

### ❌ Tránh
- **Rename prefabs thủ công** sau khi đã setup Addressables
- **Xóa Addressable entries** của layers một cách thủ công
- **Override auto-generated keys** trừ khi có lý do đặc biệt

---

## 🎛️ Advanced Configuration

### Custom Group Settings
Có thể customize group settings trong `ConfigureUILayerGroupSettings()`:
```csharp
private static void ConfigureUILayerGroupSettings(AddressableAssetGroup group)
{
    // Configure compression
    // Configure bundle settings  
    // Configure loading priorities
}
```

### Custom Key Patterns
Có thể thay đổi pattern trong constants:
```csharp
private const string LAYER_PREFIX = "Layers/";
private const string UI_LAYER_GROUP_NAME = "UI Layers";
```

---

## 📈 Performance Benefits

### Before Auto-Setup
- ❌ Manual setup cho mỗi prefab
- ❌ Inconsistent naming conventions
- ❌ Easy to miss new prefabs
- ❌ Hard to validate setup

### After Auto-Setup  
- ✅ **Zero manual work** - automatic setup
- ✅ **Consistent naming** - enforced pattern
- ✅ **Never miss prefabs** - auto-detect new ones
- ✅ **Easy validation** - built-in checking tools

---

## 🛠️ Integration với Optimized System

Auto-setup hoạt động seamlessly với optimized UIManager:
```csharp
// OptimizedLayerSourcePath sẽ automatically match keys
public const string MainMenuLayer = "Layers/MainMenuLayer";

// ResourceManager sẽ load correctly  
await ResourceManager.Instance.GetLayerAsync(LayerType.MainMenuLayer);
```

---

## 🎉 Summary

Addressable Auto-Setup feature cung cấp:

**🤖 Automation**: Zero manual setup required
**📏 Consistency**: Enforced naming conventions  
**🔍 Validation**: Built-in checking tools
**🎯 Integration**: Seamless với optimized system
**⚡ Performance**: No runtime overhead

**→ Tăng productivity và giảm errors trong development workflow!**

---

**Lưu ý**: Feature này chỉ hoạt động trong Unity Editor và không ảnh hưởng đến runtime performance.
