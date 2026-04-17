# FloatingBoxTip 悬浮提示框使用说明

> **最后更新**: 2026-04-17
> **状态**: 已验证有效
> **核心类**: FloatingBoxTip, FloatingBoxTipManager

## 📋 目录

- [功能概述](#功能概述)
- [主要特性](#主要特性)
- [使用方法](#使用方法)
- [NewGameUI 中的实际应用](#newgameui-中的实际应用)
- [FloatingBoxTip 组件方法](#floatingboxtip-组件方法)
- [动画效果](#动画效果)
- [与 Toast 的对比](#与-toast-的对比)
- [配置要求](#配置要求)
- [注意事项](#注意事项)
- [常见问题](#常见问题)

---


FloatingBoxTip 是一个悬浮提示框组件，用于在 UI 上显示临时的提示信息，比 Toast 更灵活，可以精确控制位置。

## 主要特性

- ✅ 支持跟随鼠标位置显示
- ✅ 支持相对于指定 UI 元素显示
- ✅ 淡入淡出动画效果
- ✅ 可自定义偏移量
- ✅ 支持富文本格式

[↑ 返回目录](#目录)

---

## 使用方法

### 1. 跟随鼠标显示

```csharp
// 在鼠标位置显示提示框（默认偏移：右上方 20,20）
int tipId = GF.UI.ShowFloatingTip("这是提示文本");

// 自定义偏移量
int tipId = GF.UI.ShowFloatingTip("这是提示文本", new Vector2(30f, 30f));
```

### 2. 相对于 UI 元素显示

```csharp
// 在指定 UI 元素旁边显示（默认偏移：右上方 10,10）
RectTransform targetRect = skillIcon.GetComponent<RectTransform>();
int tipId = GF.UI.ShowFloatingTipAt("技能描述", targetRect);

// 自定义偏移量（例如：显示在左侧）
int tipId = GF.UI.ShowFloatingTipAt("技能描述", targetRect, new Vector2(-150f, 0f));
```

### 3. 关闭提示框

```csharp
// 关闭指定的提示框
GF.UI.Close(tipId);

// 关闭所有悬浮提示框
GF.UI.CloseAllFloatingTips();
```

### 4. 富文本支持

```csharp
// 使用富文本格式
string richText = "<b>技能名称</b>\n<color=yellow>伤害: 100</color>\n<i>冷却时间: 5秒</i>";
GF.UI.ShowFloatingTip(richText);
```

[↑ 返回目录](#目录)

---

## NewGameUI 中的实际应用

### 技能悬浮提示实现

```csharp
private int m_CurrentFloatingTipId = -1;

// 显示技能提示
private void ShowSkillTooltip(int skillIndex)
{
    var skill = GetSkillData(skillIndex);
    if (skill != null)
    {
        // 构建提示文本
        string tooltipText = $"<b>{skill.Name}</b>\n{skill.Description}";
        
        // 获取技能图标
        var skillIcon = varSkillArr[skillIndex].GetComponent<RectTransform>();
        
        // 显示悬浮提示框
        m_CurrentFloatingTipId = GF.UI.ShowFloatingTipAt(tooltipText, skillIcon, new Vector2(10f, 0f));
    }
}

// 隐藏技能提示
private void HideSkillTooltip()
{
    if (m_CurrentFloatingTipId != -1)
    {
        GF.UI.Close(m_CurrentFloatingTipId);
        m_CurrentFloatingTipId = -1;
    }
}

// 在 OnClose 中清理
protected override void OnClose(bool isShutdown, object userData)
{
    HideSkillTooltip();
    base.OnClose(isShutdown, userData);
}
```

[↑ 返回目录](#目录)

---

## FloatingBoxTip 组件方法

### SetData(string text)
设置提示框显示的文本内容。

```csharp
floatingTip.SetData("新的提示文本");
```

### SetPosition(Vector2 screenPosition)
设置提示框的屏幕坐标位置。

```csharp
floatingTip.SetPosition(Input.mousePosition);
```

### SetPositionRelativeTo(RectTransform targetRect, Vector2 offset)
设置提示框相对于目标 UI 元素的位置。

```csharp
floatingTip.SetPositionRelativeTo(targetRect, new Vector2(10f, 10f));
```

[↑ 返回目录](#目录)

---

## 动画效果

FloatingBoxTip 使用 UI 框架统一的打开和关闭动效，无需手动管理动画。框架会自动处理：
- 打开时的淡入/缩放动画
- 关闭时的淡出/缩放动画

只需调用标准的打开和关闭接口即可：
```csharp
// 打开（框架自动播放打开动画）
int tipId = GF.UI.ShowFloatingTip("提示文本");

// 关闭（框架自动播放关闭动画）
GF.UI.Close(tipId);
```

[↑ 返回目录](#目录)

---

## 与 Toast 的对比

| 特性 | FloatingBoxTip | Toast |
|------|----------------|-------|
| 位置控制 | ✅ 精确控制 | ❌ 固定位置 |
| 跟随元素 | ✅ 支持 | ❌ 不支持 |
| 手动关闭 | ✅ 支持 | ❌ 自动消失 |
| 动画效果 | ✅ 框架统一管理 | ✅ 框架统一管理 |
| 适用场景 | 悬浮提示、技能描述 | 通知消息、操作反馈 |

[↑ 返回目录](#目录)

---

## 配置要求

### Unity 配置

1. **FloatingBoxTip 预制体**
   - 需要包含一个 Text 组件（varText）
   - 建议添加背景图片和边框
   - 建议设置合适的 Padding

2. **UIViews 枚举**
   - ✅ 已定义：`UIViews.FloatingBoxTip = 14`

3. **UITable 配置表**
   - **必须配置**：在 UITable.txt 中添加 FloatingBoxTip 的配置
   - **ID**: 14
   - **UIGroupId**: 指向 Tips 组的 ID（与 ToastTips 相同）
   - **UIPrefab**: FloatingBoxTip 预制体路径
   - **示例配置**：
   ```
   14,FloatingBoxTip,Tips组ID,FloatingBoxTip,false,100
   ```

4. **UI 分组**
   - FloatingBoxTip 必须在 `Tips` 组中（通过 UITable 配置）
   - 确保 SortOrder 高于其他 UI
   - Tips 组应该是最上层的 UI 组

### UITable 配置示例

假设 Tips 组的 ID 是 3，配置如下：

```
# UITable.txt
# ID, Name, UIGroupId, UIPrefab, PauseCoveredUI, SortOrder, EscapeClose
14, FloatingBoxTip, 3, FloatingBoxTip, false, 100, false
```

### 层级结构

运行时的层级结构应该是：
```
UICanvasRoot
├── UI Group - UIForm
├── UI Group - Dialog  
└── UI Group - Tips
    ├── ToastTips (实例)
    └── FloatingBoxTip (实例) ← 应该在这里
```

### 推荐样式

```
FloatingBoxTip 预制体结构:
├── Background (Image)
│   ├── Padding: 10px
│   ├── Color: 半透明黑色 (0, 0, 0, 200)
│   └── Border: 1px 白色
└── Text (Text)
    ├── Font Size: 14
    ├── Color: 白色
    ├── Alignment: Left
    └── Rich Text: Enabled
```

[↑ 返回目录](#目录)

---

## 注意事项

1. **性能考虑**: 不要同时显示过多悬浮提示框
2. **层级管理**: 确保提示框在最上层显示
3. **及时清理**: 在 UI 关闭时记得清理悬浮提示框
4. **文本长度**: 避免文本过长导致提示框超出屏幕

[↑ 返回目录](#目录)

---

## 常见问题

### Q: 提示框位置不正确？
A: 检查 UICamera 是否正确设置，确保使用 `GF.UICamera` 进行坐标转换。

### Q: 提示框不显示？
A: 检查 UIViews.FloatingBoxTip 是否在 UITable 中配置，确保 UI 分组正确。

### Q: 如何实现鼠标跟随效果？
A: 在 Update 中持续更新位置：
```csharp
if (m_CurrentFloatingTipId != -1)
{
    var uiForm = GF.UI.GetUIForm(m_CurrentFloatingTipId);
    if (uiForm != null)
    {
        var tip = (uiForm as UIForm).Logic as FloatingBoxTip;
        tip?.SetPosition(Input.mousePosition + new Vector3(20f, 20f, 0f));
    }
}
```
e

[↑ 返回目录](#目录)
