# Hệ thống UIManager - Phân tích Chi tiết

## 1. Mục tiêu Hệ thống
Hệ thống UIManager được thiết kế để quản lý các layer UI trong Unity với khả năng:
- Hiển thị/ẩn UI layers theo nhóm
- Quản lý thứ tự hiển thị (sorting order) 
- Tải UI bất đồng bộ thông qua Addressable Assets
- Điều khiển lifecycle của UI layers

## 2. Kiến trúc Hệ thống

### 2.1 Core Components

#### **LayerManager** (Singleton Pattern)
- **Vai trò**: Controller chính của hệ thống UI
- **Kế thừa**: MonoSingleton<LayerManager>
- **Chức năng chính**:
  - Quản lý việc tạo và hiển thị LayerGroup
  - Load UI prefabs từ Addressables
  - Quản lý sorting order cho các layers
  - Điều khiển lifecycle của UI layers

#### **LayerBase** (Abstract Base Class)
- **Vai trò**: Base class cho tất cả UI layers
- **Required Components**: Canvas, CanvasGroup, RectTransform  
- **Chức năng chính**:
  - Show/Hide/Close layer operations
  - Quản lý sorting order stack
  - Virtual methods để override trong subclasses

#### **LayerGroup** (Composite Pattern)
- **Vai trò**: Container quản lý một nhóm layers
- **Chức năng chính**:
  - Nhóm nhiều layers thành một unit
  - Thực hiện operations đồng bộ trên tất cả layers
  - Quản lý sorting order cho group

### 2.2 Supporting Components

#### **LayerSourcePath** (Static Resource Manager)
- **Vai trò**: Quản lý đường dẫn tới UI prefabs
- **Pattern**: Static class với Reflection
- **Chức năng**: Mapping LayerType enum với Addressable paths

#### **ShowLayerGroupData** (Configuration Object)
- **Vai trò**: Chứa thông tin cấu hình hiển thị layer group
- **Properties**:
  - `LayerTypes`: Danh sách các layers cần hiển thị
  - `LayerGroupType`: Loại group (Root, FullScreen, Popup, etc.)
  - Các flags điều khiển hành vi hiển thị

#### **LayerGroupBuilder** (Builder Pattern)
- **Vai trò**: Factory để tạo ShowLayerGroupData
- **Pattern**: Static factory methods

## 3. Data Flow & Workflow

### 3.1 Show Layer Group Process
```
1. LayerManager.ShowGroupLayerAsync()
   ↓
2. Wait for !IsShowing (Thread-safe)
   ↓  
3. InitLayerGroup() - Load/Create layers
   ↓
4. onInitData() callback - Setup data
   ↓
5. HideLayerRequired() - Hide conflicting layers
   ↓
6. DisplayLayerGroup() - Show new group
```

### 3.2 Layer Loading Process
```
1. InitLayerBase(LayerType)
   ↓
2. Check if layer exists in cache
   ↓
3. If not exists: AddressableLoadLayer()
   ↓
4. Instantiate prefab from Addressables
   ↓
5. Cache LayerBase in _createdLayerBases
```

### 3.3 Sorting Order Management
- **Spacing**: `spaceBetweenLayer = 100` giữa các groups
- **Algorithm**: Tìm max sorting order hiện tại, thêm spacing
- **Layer trong Group**: Tăng dần +1 từ base order của group

## 4. Design Patterns được sử dụng

### 4.1 **Singleton Pattern**
- **LayerManager**: Đảm bảo chỉ có một instance quản lý UI
- **MonoSingleton<T>**: Generic base class với thread-safe initialization

### 4.2 **Composite Pattern** 
- **LayerGroup**: Treat một group layers như single object
- Có thể thực hiện operations trên cả group hoặc individual layers

### 4.3 **Factory Pattern**
- **LayerGroupBuilder**: Tạo ShowLayerGroupData với validation
- **AddressableLoadLayer**: Factory method tạo layers từ prefabs

### 4.4 **Template Method Pattern**
- **LayerBase**: Định nghĩa skeleton của layer lifecycle
- Virtual methods để subclasses override

### 4.5 **Strategy Pattern**
- **LayerGroupType**: Khác nhau hành vi dựa trên type
- **ShowLayerGroupData.ValidateData()**: Thiết lập behavior theo type

## 5. Async/Await Architecture

### 5.1 **UniTask Integration**
- Sử dụng UniTask thay vì Unity Coroutines
- Tốt hơn về performance và memory
- Hỗ trợ cancellation tokens

### 5.2 **Concurrency Management**
- `IsShowing` flag ngăn concurrent show operations
- `CancellationToken` để handle object destruction
- `UniTask.WhenAll()` cho parallel operations

### 5.3 **Thread Safety**
- Timeout mechanism (5 seconds) cho show operations
- Proper cancellation handling

## 6. Memory Management

### 6.1 **Object Pooling**
- Layers được cache trong `_createdLayerBases`
- Không destroy khi close, chỉ hide để reuse
- Close với `force=true` để cleanup sorting order stack

### 6.2 **Resource Management**  
- Sử dụng Addressables cho lazy loading
- Proper disposal của AsyncOperationHandle
- CancellationToken cleanup on destroy

## 7. Extensibility Points

### 7.1 **LayerBase Inheritance**
```csharp
public class CustomLayer : LayerBase
{
    public override async UniTask ShowLayerAsync()
    {
        // Custom show animation
        await base.ShowLayerAsync();
    }
}
```

### 7.2 **Custom LayerTypes**
```csharp
// Thêm vào LayerType enum
public enum LayerType
{
    Layer01 = 1,
    NewCustomLayer = 2,  // Thêm mới
}

// Thêm path vào LayerSourcePath
public static class LayerSourcePath
{
    public const string NewCustomLayer = "Layers/CustomLayer";
}
```

### 7.3 **Helper Methods Pattern**
- File `ShowLayerHelper.cs` chứa extension methods
- Partial class pattern để tách logic

## 8. Ưu điểm của Hệ thống

### 8.1 **Architecture**
✅ **Separation of Concerns**: Tách biệt logic quản lý và UI logic
✅ **Async First**: Modern async/await pattern
✅ **Memory Efficient**: Object pooling và lazy loading
✅ **Thread Safe**: Proper concurrency handling

### 8.2 **Maintainability**  
✅ **Extensible**: Easy để thêm layer types mới
✅ **Testable**: Clear interfaces và dependency injection ready
✅ **Configurable**: ShowLayerGroupData cho flexible behavior

### 8.3 **Performance**
✅ **Lazy Loading**: Load UI khi cần thiết
✅ **Batching**: Show multiple layers cùng lúc
✅ **Caching**: Reuse loaded layers

## 9. Nhược điểm & Cải tiến

### 9.1 **Potential Issues**
⚠️ **Reflection Usage**: LayerSourcePath.GetPath() dùng reflection - có thể chậm
⚠️ **Memory Leaks**: Không có explicit cleanup cho cached layers
⚠️ **Error Handling**: Một số async operations thiếu proper error handling

### 9.2 **Đề xuất Cải tiến**

#### **9.2.1 Resource Management**
```csharp
// Thêm method cleanup unused layers
public void ClearUnusedLayers()
{
    var unusedLayers = _createdLayerBases
        .Where(x => !_showingLayerTypes.Contains(x.Key))
        .ToList();
    
    foreach (var layer in unusedLayers)
    {
        Addressables.ReleaseInstance(layer.Value.gameObject);
        _createdLayerBases.Remove(layer.Key);
    }
}
```

#### **9.2.2 Configuration Validation**
```csharp
// Thêm validation cho ShowLayerGroupData
public bool IsValid()
{
    if (LayerTypes == null || LayerTypes.Count == 0) 
        return false;
    
    // Validate layer paths exist
    foreach (var layerType in LayerTypes)
    {
        if (string.IsNullOrEmpty(LayerSourcePath.GetPath(layerType.ToString())))
            return false;
    }
    
    return true;
}
```

#### **9.2.3 Performance Optimization**
```csharp
// Cache reflection results
private static readonly Dictionary<string, string> _pathCache = new();

public static string GetPath(string variableName)
{
    if (_pathCache.TryGetValue(variableName, out string cachedPath))
        return cachedPath;
        
    // Existing reflection code...
    _pathCache[variableName] = result;
    return result;
}
```

## 10. Kết luận

Hệ thống UIManager là một kiến trúc UI management tiên tiến với:
- **Design patterns** phù hợp và hiện đại
- **Async architecture** tốt với UniTask
- **Extensibility** cao cho future development
- **Performance** tối ưu với pooling và lazy loading

Tuy nhiên cần cải thiện ở **error handling**, **resource cleanup** và **reflection optimization** để trở thành production-ready system.

## 11. Usage Examples

### 11.1 **Basic Usage**
```csharp
// Show single layer
var group = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.Layer01);
await LayerManager.Instance.ShowGroupLayerAsync(group);

// Show multiple layers
var group = LayerGroupBuilder.Build(LayerGroupType.FullScreen, 
    LayerType.Layer01, LayerType.Layer02);
await LayerManager.Instance.ShowGroupLayerAsync(group);
```

### 11.2 **Custom Data Setup**
```csharp
var group = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.Layer01);
await LayerManager.Instance.ShowGroupLayerAsync(group, SetupData);

async UniTask SetupData(LayerGroup layerGroup)
{
    if (layerGroup.GetLayerBase(LayerType.Layer01, out var layer))
    {
        // Setup layer với custom data
        var customLayer = layer as CustomLayer;
        await customLayer.InitializeWithData(someData);
    }
}
```

---
**Tài liệu được tạo bởi**: AI Coding Assistant
**Ngày tạo**: 15/08/2025
**Phiên bản**: 1.0
