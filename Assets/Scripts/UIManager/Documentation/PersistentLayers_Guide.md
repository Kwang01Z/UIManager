# Persistent Layer Groups - User Guide

## 🎯 Tổng quan

**Persistent Layer Groups** là một tính năng mới trong UIManager cho phép tạo các UI layers **không bao giờ bị ẩn** bởi các operations của layer khác. Điều này rất hữu ích cho:

- **Notification systems** luôn hiển thị
- **Debug overlays** trong development  
- **System status indicators** (loading, connection, etc.)
- **Tutorial overlays** persist qua nhiều screens

---

## 🔧 Cách hoạt động

### LayerGroupType.Persistent

Khi một layer được đánh dấu là `LayerGroupType.Persistent`, nó sẽ:

✅ **Không bị ẩn** bởi `HideAllOtherLayer` operations  
✅ **Không bị đóng** bởi `CloseAllOtherLayer` operations  
✅ **Vẫn hiển thị** khi có FullScreen layers khác  
✅ **Duy trì state** qua các transitions  

### Filtering Logic

```csharp
// LayerManager tự động filter out persistent layers
private List<LayerType> FilterOutPersistentLayers(List<LayerType> layerTypes)
{
    // Persistent layers sẽ được bỏ qua trong hide/close operations
    return layerTypes.Where(layerType => !IsLayerPersistent(layerType)).ToList();
}
```

---

## 📋 Usage Examples

### Basic Usage

```csharp
// Tạo persistent layer group
var persistentData = LayerGroupBuilder.Build(
    LayerGroupType.Persistent, 
    LayerType.NotificationLayer
);

// Show với animation
await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
    persistentData,
    onInitData: async (group) =>
    {
        // Setup persistent layer
        if (group.GetLayerBase(LayerType.NotificationLayer, out var layer))
        {
            // Configure notification system
        }
        await UniTask.Yield();
    },
    transition: TransitionProfile.SlideUp
);
```

### Multiple Persistent Layers

```csharp
// Show multiple persistent system UI elements
var systemUIData = LayerGroupBuilder.Build(
    LayerGroupType.Persistent,
    LayerType.NotificationLayer,
    LayerType.LoadingLayer
);

await OptimizedLayerManager.Instance.ShowGroupLayerAsync(systemUIData);
```

---

## 🎬 Behavior Comparison

### Before Persistent Layers

```csharp
// Old behavior:
// 1. Show notification
await ShowNotification();

// 2. Show fullscreen layer
await ShowFullScreen();  // ❌ Notification gets hidden!

// 3. User misses important notifications
```

### With Persistent Layers

```csharp  
// New behavior:
// 1. Show persistent notification
await ShowPersistentNotification();

// 2. Show fullscreen layer  
await ShowFullScreen();  // ✅ Notification stays visible!

// 3. User always sees important notifications
```

---

## 🔄 Layer Operations Impact

| Operation | Regular Layers | Persistent Layers |
|-----------|----------------|-------------------|
| **HideAllOtherLayer** | ❌ Hidden | ✅ Visible |
| **CloseAllOtherLayer** | ❌ Closed | ✅ Remains |
| **CloseAllPopup** | ❌ Closed (if popup) | ✅ Remains |
| **FullScreen layers** | ❌ Hidden | ✅ Visible |
| **Root layers** | ❌ May be affected | ✅ Unaffected |

---

## 💡 Common Use Cases

### 1. Notification System

```csharp
public static async UniTask SetupNotificationSystem()
{
    var data = LayerGroupBuilder.Build(
        LayerGroupType.Persistent, 
        LayerType.NotificationLayer
    );
    
    await OptimizedLayerManager.Instance.ShowGroupLayerAsync(data);
    // Notifications will persist across all UI changes
}
```

### 2. Debug Console (Development)

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
public static async UniTask ShowDebugConsole()
{
    var data = LayerGroupBuilder.Build(
        LayerGroupType.Persistent, 
        LayerType.DebugConsoleLayer
    );
    
    await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
        data,
        transition: TransitionProfile.Crossfade
    );
    // Debug console stays on top of everything
}
#endif
```

### 3. Loading Indicators

```csharp
public static async UniTask ShowPersistentLoadingIndicator()
{
    var data = LayerGroupBuilder.Build(
        LayerGroupType.Persistent, 
        LayerType.LoadingLayer
    );
    
    await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
        data,
        transition: TransitionProfile.Instant
    );
    // Loading indicator visible during all transitions
}
```

### 4. Tutorial Overlay

```csharp
public static async UniTask ShowPersistentTutorial()
{
    var data = LayerGroupBuilder.Build(
        LayerGroupType.Persistent, 
        LayerType.TutorialLayer
    );
    
    await OptimizedLayerManager.Instance.ShowGroupLayerAsync(
        data,
        onInitData: async (group) =>
        {
            // Setup tutorial steps that persist across screens
            await InitializeTutorialSystem(group);
        },
        transition: TransitionProfile.ScaleTransition
    );
}
```

---

## ⚠️ Best Practices

### ✅ Good Use Cases

- **System notifications** that users must see
- **Debug tools** during development
- **Critical status indicators** (connection, loading)
- **Tutorial guides** that span multiple screens
- **Accessibility overlays** (screen readers, etc.)

### ❌ Avoid Using For

- **Regular popups** that should be dismissable
- **Modal dialogs** that block interaction
- **Temporary UI elements** that should hide
- **Context-specific menus** 

### 🎯 Design Considerations

1. **Visual Hierarchy**: Persistent layers should be visually subtle
2. **User Control**: Provide way to dismiss if needed
3. **Performance**: Don't overuse - limit to essential elements
4. **Accessibility**: Ensure persistent elements don't interfere

---

## 🔧 Advanced Features

### Custom Filtering Logic

```csharp
// Check if a specific layer is persistent
private bool IsLayerPersistent(LayerType layerType)
{
    return _showingLayerGroups.Any(group => 
        group.LayerGroupType == LayerGroupType.Persistent && 
        group.LayerTypes.Contains(layerType));
}
```

### Conditional Persistence

```csharp
// Show layer as persistent only in certain conditions
public static async UniTask ShowConditionalPersistent(LayerType layerType, bool shouldPersist)
{
    var groupType = shouldPersist ? LayerGroupType.Persistent : LayerGroupType.Popup;
    var data = LayerGroupBuilder.Build(groupType, layerType);
    
    await OptimizedLayerManager.Instance.ShowGroupLayerAsync(data);
}
```

### Sorting Order Management

```csharp
// Persistent layers automatically get proper sorting order
// They remain on top even when new layers are shown
```

---

## 🚀 Integration với Optimized System

Persistent layers hoạt động seamlessly với tất cả tính năng optimization:

✅ **ResourceManager**: Smart caching và memory management  
✅ **AnimationController**: Smooth transitions cho persistent layers  
✅ **PerformanceMonitor**: Tracking performance impact  
✅ **Addressable Auto-Setup**: Automatic path resolution  

---

## 📊 Performance Impact

### Memory Usage
- **Minimal overhead**: Chỉ tracking thêm group type
- **Smart filtering**: Efficient lookup algorithms
- **Memory cleanup**: Persistent layers vẫn được managed properly

### CPU Performance  
- **Optimized filtering**: O(n) complexity cho layer filtering
- **Cached lookups**: Avoid repeated searches
- **Batch operations**: Multiple persistent layers processed together

---

## 🐛 Debug và Troubleshooting

### Debug Logs
```
[LayerManager] Skipping LayerType.NotificationLayer - it belongs to a Persistent group and cannot be hidden
[OptimizedLayerManager] Skipping LayerType.LoadingLayer - it belongs to a Persistent group and cannot be hidden
```

### Validation
```csharp
// Check if system is working correctly
var stats = OptimizedLayerManager.Instance.GetStats();
Debug.Log($"Showing {stats.ShowingLayerCount} layers total");

// Verify persistent layers are actually persistent
bool isNotificationPersistent = IsLayerPersistent(LayerType.NotificationLayer);
Debug.Log($"Notification layer persistent: {isNotificationPersistent}");
```

### Common Issues

1. **Layer still gets hidden**
   - ✅ Check LayerGroupType is actually Persistent
   - ✅ Verify layer was shown with correct group type

2. **Performance impact**
   - ✅ Limit number of persistent layers
   - ✅ Use PerformanceMonitor to track impact

3. **Visual conflicts**  
   - ✅ Adjust sorting orders manually if needed
   - ✅ Design persistent layers to be non-intrusive

---

## 📈 Migration từ Regular Layers

### Step 1: Identify Candidates
```csharp
// Find layers that should never be hidden
// Examples: notifications, debug tools, status indicators
```

### Step 2: Update Layer Creation
```csharp
// Before:
var data = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.NotificationLayer);

// After:
var data = LayerGroupBuilder.Build(LayerGroupType.Persistent, LayerType.NotificationLayer);
```

### Step 3: Test Behavior
```csharp
// Verify persistent layers remain visible during:
// - FullScreen transitions
// - Root layer changes
// - CloseAllOtherLayer calls
```

---

## 🎉 Summary

**Persistent Layer Groups** provide:

**🔒 Reliability**: Critical UI elements never get hidden accidentally  
**🎯 Use Case Coverage**: Perfect for notifications, debug tools, status indicators  
**⚡ Performance**: Minimal overhead với smart filtering  
**🔧 Integration**: Works seamlessly với existing optimized system  
**📱 UX Improvement**: Better user experience với always-visible important elements

**Perfect cho production apps** requiring reliable system UI elements!

---

**Next Steps**: 
1. Identify layers that should be persistent
2. Update LayerGroupType to Persistent  
3. Test behavior với different UI transitions
4. Monitor performance impact với PerformanceMonitor
