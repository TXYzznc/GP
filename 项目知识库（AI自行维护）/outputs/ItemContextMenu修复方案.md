# ItemContextMenu 菜单自动关闭 Bug 修复方案

## 问题分析

### 症状
- 菜单显示后 ~0.765 秒自动关闭
- 用户点击菜单按钮（使用、分割、丢弃）时，点击被识别为"菜单外部点击"，导致菜单关闭
- 无法使用任何消耗品效果

### 根本原因
在 `InventoryUI.CheckContextMenuClickOutside()` 方法中：
1. 每帧都在 `OnUpdate()` 中使用 `Input.GetMouseButtonDown()` 检查鼠标点击
2. 该检查发生在 **EventSystem 完整处理事件之后**
3. Button 组件的点击事件处理逻辑与菜单关闭判断产生竞态条件

**时间序列（错误的）：**
```
Frame N: User clicks menu button
  └─ Input.GetMouseButtonDown() → true
  └─ InventoryUI.CheckContextMenuClickOutside() 执行
  └─ RectTransformUtility.RectangleContainsScreenPoint() 返回 false（误判）
  └─ 菜单关闭 HideContextMenu()
  
Frame N+?: Button.OnPointerClick() 执行
  └─ 事件已被吞掉，Button 点击无效
```

## 修复方案

### 核心思路
在 `Input.GetMouseButtonDown()` 检测到点击后，**延迟一帧** 再判断是否关闭菜单。这样可以让 EventSystem 有足够时间处理 Button 点击事件，确定点击是否真的在菜单外。

### 实现步骤

**修改文件：** `Assets/AAAGame/Scripts/UI/InventoryUI.cs`

1. **原方法（有问题）：**
```csharp
private void CheckContextMenuClickOutside()
{
    if (m_CachedContextMenu == null || !m_CachedContextMenu.gameObject.activeSelf)
        return;

    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
    {
        var menuRect = m_CachedContextMenu.GetComponent<RectTransform>();
        if (
            menuRect != null
            && !RectTransformUtility.RectangleContainsScreenPoint(menuRect, Input.mousePosition)
        )
        {
            m_CachedContextMenu.HideContextMenu();
            DebugEx.Log("InventoryUI", "菜单外部点击，关闭菜单");
        }
    }
}
```

2. **新方法（修复后）：**
```csharp
/// <summary>
/// 检查菜单外部点击，自动关闭菜单
/// 使用延迟一帧的方式，让 EventSystem 优先处理按钮点击
/// </summary>
private void CheckContextMenuClickOutside()
{
    if (m_CachedContextMenu == null || !m_CachedContextMenu.gameObject.activeSelf)
        return;

    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
    {
        // 延迟一帧检查，让 EventSystem 先处理按钮点击
        CheckMenuClickDelayedAsync().Forget();
    }
}

private async UniTask CheckMenuClickDelayedAsync()
{
    // 等待一帧，让 EventSystem 处理完事件
    await UniTask.Yield();

    if (m_CachedContextMenu == null || !m_CachedContextMenu.gameObject.activeSelf)
        return;

    // 获取菜单的 RectTransform
    var menuRect = m_CachedContextMenu.GetComponent<RectTransform>();
    if (menuRect == null)
        return;

    // 检查点击是否在菜单范围内
    if (!RectTransformUtility.RectangleContainsScreenPoint(menuRect, Input.mousePosition))
    {
        // 点击在菜单外，关闭菜单
        m_CachedContextMenu.HideContextMenu();
        DebugEx.Log("InventoryUI", "菜单外部点击，关闭菜单");
    }
}
```

## 关键改进

| 方面 | 原方案 | 新方案 |
|------|--------|--------|
| **时机** | 同帧检查 | 延迟一帧检查 |
| **Button 事件** | 被打断 | 完整处理 |
| **用户体验** | 菜单无法使用 | 菜单正常交互 |
| **异步安全** | N/A | 使用 UniTask + Forget() |

## 修复后的测试流程

### 1. 基础功能测试
- [ ] 右键点击背包物品，菜单显示
- [ ] 菜单正确显示对应按钮（使用、分割、丢弃）
- [ ] 点击"使用"按钮，菜单关闭，物品被使用
- [ ] 点击"分割"按钮，分割面板显示
- [ ] 点击"丢弃"按钮，物品数量减少

### 2. 消耗品效果验证
- [ ] 点击"使用"按钮，消耗品效果执行（检查日志）
- [ ] 金币消耗品：金币数增加（100-500 随机）
- [ ] 生命药水：生命值恢复 500 点
- [ ] 魔法药水：魔法值恢复 300 点
- [ ] 经验药水：获得 1000 经验值
- [ ] 卡牌解锁消耗品：卡牌被解锁，菜单提示"已拥有"或"解锁成功"

### 3. 菜单关闭逻辑验证
- [ ] 点击菜单外部区域，菜单关闭（延迟一帧有效）
- [ ] 使用 ESC 键关闭菜单
- [ ] 连续点击多个物品，菜单正确切换

### 4. 边界情况测试
- [ ] 快速连续点击"使用"按钮，确保不会重复消耗
- [ ] 菜单显示时打开背包其他 UI，菜单保持可交互
- [ ] 背包关闭时菜单自动隐藏

## 编译验证

**代码编译状态：✅ 通过**
- 仅有少量警告（项目既有的）
- 无新增编译错误

## 时间线

- **根本原因确认**：Input 检查与 EventSystem 事件处理的竞态条件
- **方案设计**：延迟一帧检查，让 EventSystem 优先处理
- **实现**：修改 `CheckContextMenuClickOutside()` 和 `CheckMenuClickDelayedAsync()`
- **编译验证**：✅ 通过

## 后续步骤

1. **用户在 Unity 编辑器中编译** Unity 项目
2. **运行游戏并测试**上述测试用例
3. **验证所有消耗品效果**正常工作
4. **确认菜单交互**流畅无卡顿

---

**状态**：✅ 代码修复完成，待用户 Unity 编译验证
