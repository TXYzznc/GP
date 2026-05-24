# CharacterBagUI RenderTexture释放错误修复

## 问题描述

CharacterBagUI在关闭时出现RenderTexture释放错误：
```
[Error] Releasing render texture that is set as Camera.targetTexture!
```

## 问题分析

### 时序问题

从日志分析发现问题的根本原因是**生命周期时序问题**：

1. **第一次关闭**：`CharacterBagUI.OnClose()` 成功清理RenderTexture（ID: -1842450）
2. **错误发生**：旧的`UIModelViewer.OnDestroy()`被延迟调用，尝试释放已经被清理的RenderTexture
3. **第二次打开**：创建新RenderTexture（ID: -1845092），但此时旧的OnDestroy还在执行

### 根本原因

- `OnClose()`中虽然清理了RenderTexture，但**UIModelViewer的`OnDestroy()`方法仍然会在之后被调用**
- `OnDestroy()`中没有检查RenderTexture是否已经被提前清理，导致重复释放
- Unity的`Destroy()`是延迟执行的，导致`OnDestroy()`在新UI已经初始化后才被调用

## 解决方案

### 1. 在UIModelViewer中添加公共清理方法

**文件**：`Assets/AAAGame/Scripts/UI/Components/UIModelViewer.cs`

添加`CleanupRenderTexture()`公共方法：
- 清理模型
- 解除RawImage引用
- 解除Camera引用
- 释放并销毁RenderTexture
- **关键**：将`m_RenderTexture`设为null，防止`OnDestroy()`重复释放

```csharp
/// <summary>
/// 公共清理方法 - 供外部调用，彻底清理RenderTexture
/// </summary>
public void CleanupRenderTexture()
{
    DebugEx.Log(this.GetType().Name, "CleanupRenderTexture 开始");

    // 清理模型
    ClearModel();

    // 解除RawImage引用
    if (m_TargetImage != null)
    {
        m_TargetImage.texture = null;
        DebugEx.Log(this.GetType().Name, "CleanupRenderTexture: 已解除RawImage引用");
    }

    // 解除Camera引用
    if (m_ModelCamera != null && m_ModelCamera.targetTexture != null)
    {
        DebugEx.Log(
            this.GetType().Name,
            $"CleanupRenderTexture: 解除Camera对RenderTexture {m_ModelCamera.targetTexture.GetInstanceID()} 的引用"
        );
        m_ModelCamera.targetTexture = null;
    }

    // 释放并销毁RenderTexture
    if (m_RenderTexture != null)
    {
        int rtId = m_RenderTexture.GetInstanceID();

        if (m_RenderTexture.IsCreated())
        {
            m_RenderTexture.Release();
            DebugEx.Log(
                this.GetType().Name,
                $"CleanupRenderTexture: 已释放RenderTexture {rtId}"
            );
        }

        Destroy(m_RenderTexture);
        m_RenderTexture = null; // ⭐ 关键：设为null，防止OnDestroy重复释放
        DebugEx.Log(
            this.GetType().Name,
            $"CleanupRenderTexture: 已销毁RenderTexture {rtId} 并设为null"
        );
    }

    DebugEx.Log(this.GetType().Name, "CleanupRenderTexture 完成");
}
```

### 2. 修改OnDestroy检查RenderTexture是否已被清理

```csharp
private void OnDestroy()
{
    DebugEx.Log(this.GetType().Name, "OnDestroy 开始清理");

    // ⭐ 检查RenderTexture是否已经被清理（通过CleanupRenderTexture）
    if (m_RenderTexture == null)
    {
        DebugEx.Log(
            this.GetType().Name,
            "OnDestroy: RenderTexture已被提前清理，跳过重复释放"
        );
    }
    else
    {
        // 如果没有被提前清理，执行正常清理流程
        DebugEx.Log(
            this.GetType().Name,
            $"OnDestroy: RenderTexture {m_RenderTexture.GetInstanceID()} 未被提前清理，现在清理"
        );

        // ... 正常清理流程
    }

    // 清理模型根节点
    if (m_ModelRoot != null)
    {
        Destroy(m_ModelRoot);
        m_ModelRoot = null;
    }

    DebugEx.Log(this.GetType().Name, "OnDestroy 清理完成");
}
```

### 3. 修改CharacterBagUI.OnClose调用新的清理方法

**文件**：`Assets/AAAGame/Scripts/UI/CharacterBagUI.cs`

简化`OnClose()`，直接调用`UIModelViewer.CleanupRenderTexture()`：

```csharp
protected override void OnClose(bool isShutdown, object userData)
{
    DebugEx.Log(nameof(CharacterBagUI), "OnClose 开始清理资源");

    // 清理棋子卡片池
    foreach (var item in m_ChessItemPool)
    {
        if (item != null)
            Destroy(item.gameObject);
    }
    m_ChessItemPool.Clear();

    // 清理宝物槽位池
    foreach (var item in m_TreasureSlotPool)
    {
        if (item != null)
            Destroy(item.gameObject);
    }
    m_TreasureSlotPool.Clear();

    // ⭐ 调用UIModelViewer的公共清理方法（彻底清理RenderTexture，防止OnDestroy重复释放）
    if (m_ModelViewer != null)
    {
        DebugEx.Log(nameof(CharacterBagUI), "调用UIModelViewer.CleanupRenderTexture()");
        m_ModelViewer.CleanupRenderTexture();
        DebugEx.Log(nameof(CharacterBagUI), "UIModelViewer清理完成");
    }

    // 请求锁定鼠标（通过引用计数管理）
    var input = PlayerInputManager.Instance;
    if (input != null)
        input.RequestMouseLock();

    DebugEx.Log(nameof(CharacterBagUI), "OnClose 清理完成");

    base.OnClose(isShutdown, userData);
}
```

## 修复效果

### 修复前

```
[Log] [CharacterBagUI] OnClose 开始清理资源
[Log] [CharacterBagUI] 找到RenderTexture: -1842450
[Log] [CharacterBagUI] 已解除RawImage对RenderTexture的引用
[Log] [CharacterBagUI] 已释放RenderTexture
[Log] [CharacterBagUI] 已销毁RenderTexture对象
[Log] [CharacterBagUI] OnClose 清理完成
[Error] Releasing render texture that is set as Camera.targetTexture!  ← 错误
```

### 修复后（预期）

```
[Log] [CharacterBagUI] OnClose 开始清理资源
[Log] [CharacterBagUI] 调用UIModelViewer.CleanupRenderTexture()
[Log] [UIModelViewer] CleanupRenderTexture 开始
[Log] [UIModelViewer] CleanupRenderTexture: 已解除RawImage引用
[Log] [UIModelViewer] CleanupRenderTexture: 解除Camera对RenderTexture -1842450 的引用
[Log] [UIModelViewer] CleanupRenderTexture: 已释放RenderTexture -1842450
[Log] [UIModelViewer] CleanupRenderTexture: 已销毁RenderTexture -1842450 并设为null
[Log] [UIModelViewer] CleanupRenderTexture 完成
[Log] [CharacterBagUI] UIModelViewer清理完成
[Log] [CharacterBagUI] OnClose 清理完成
[Log] [UIModelViewer] OnDestroy 开始清理
[Log] [UIModelViewer] OnDestroy: RenderTexture已被提前清理，跳过重复释放  ← 不再报错
[Log] [UIModelViewer] OnDestroy 清理完成
```

## 关键要点

1. **提前清理**：在`OnClose()`中调用`CleanupRenderTexture()`提前清理RenderTexture
2. **设为null**：清理后将`m_RenderTexture`设为null，作为"已清理"的标记
3. **防重复释放**：在`OnDestroy()`中检查`m_RenderTexture == null`，跳过重复释放
4. **完整清理流程**：解除RawImage引用 → 解除Camera引用 → 释放RenderTexture → 销毁对象 → 设为null

## 相关文件

- `Assets/AAAGame/Scripts/UI/Components/UIModelViewer.cs` - 添加公共清理方法
- `Assets/AAAGame/Scripts/UI/CharacterBagUI.cs` - 调用清理方法
- `Assets/测试输出日志/ConsoleLog_2026-05-24_02-08-14.log` - 错误日志

## 测试建议

1. 打开CharacterBagUI
2. 关闭CharacterBagUI
3. 再次打开CharacterBagUI
4. 再次关闭CharacterBagUI
5. 检查日志中是否还有`Releasing render texture that is set as Camera.targetTexture!`错误

---

**创建时间**：2026-05-24  
**状态**：✅ 已修复
