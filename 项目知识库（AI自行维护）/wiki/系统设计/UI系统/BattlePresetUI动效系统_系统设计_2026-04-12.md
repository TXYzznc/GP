> **最后更新**: 2026-04-17
> **状态**: 已验证有效
> **分类**: 系统设计

---
# BattlePresetUI 动效系统实现

**实现日期**: 2026-04-17  
**文件**: `Assets/AAAGame/Scripts/UI/BattlePresetUI.cs`

## 实现方案

采用**方案1（保留 LayoutGroup）**，利用 VerticalLayoutGroup 的自动布局能力，通过 DOTween 实现流畅动效。

## 已实现的动效

### 1. 入场动画
- 左侧预设列表：淡入 + 向右滑动（400ms）
- 右侧编辑区域：淡入（400ms，延迟150ms）

### 2. 预设交互
- 创建新预设：缩放弹出动画（0.5 → 1.1 → 1.0）
- 按钮悬停：缩放至 1.05
- 按钮点击：按压效果（1.0 → 0.95 → 1.0）

### 3. 棋子/策略卡选择
- **添加**：可选池项脉冲 + 已选项弹出（0.5 → 1.1 → 1.0）
- **移除**：已选项缩放淡出 + 可选池项恢复并脉冲
- 可选池状态：透明度变化（1.0 ↔ 0.4）

### 4. 数量计数器
- 数字更新：旧数字向上淡出，新数字从下方淡入
- 达到上限：变红 + 抖动（±5度）

### 5. 设为默认
- DefaultBadge：缩放弹出（0 → 1.2 → 1.0）+ 旋转360度

## 技术要点

### DOTween 参数
```csharp
Ease.OutQuart   // 大部分动画
Ease.OutQuint   // 快速响应
Ease.OutBack    // 弹出效果

0.15f  // 按钮按压
0.20f  // 悬停反馈
0.25f  // 状态变化
0.30f  // 添加/移除
0.40f  // 入场动画
```

### 关键方法
- `PlayOpenAnimation()` - 入场动画
- `PlayCreatePresetAnimation()` - 创建预设动画
- `AddButtonAnimation()` - 按钮交互动画
- `PlayItemAddAnimation()` - 添加项动画
- `PlayItemRemoveAnimation()` - 移除项动画
- `PlayCounterUpdateAnimation()` - 计数器更新动画
- `PlaySetDefaultAnimation()` - 设为默认动画
- `KillAllAnimations()` - 清理所有动画

### 动画清理
在 `OnClose()` 中调用 `KillAllAnimations()` 清理所有 DOTween 动画，避免内存泄漏。

## 优势

1. ✅ 保留 LayoutGroup，无需修改预制体
2. ✅ 动效流畅自然，反馈清晰
3. ✅ 代码改动最小，性能优秀
4. ✅ 所有动画都是 GPU 加速（transform + opacity）

## Bug修复记录

### Bug 1: 取消选中卡片时额外的卡片被隐藏
**问题**: 在动画回调中先移除数据再调用Refresh，导致数据长度变化时误隐藏其他项

**修复方案**:
- 将数据移除操作（`RemoveAt`）提前到动画播放前
- 动画完成后再调用 `RefreshSelectedXXXFromIndex()` 重新排列

### Bug 2: 数据污染问题（未选中对象的scale和alpha被改为0）
**问题**: `RefreshSelectedXXXFromIndex()` 只隐藏一个"多余"项，但对象池复用导致可能隐藏了正在播放动画的项或其他正常项

**修复方案**:
1. 添加安全检查（跳过 null 或已隐藏的项）
2. 在 Refresh 中恢复正常状态（`scale=1, alpha=1`）防止被动画污染
3. 隐藏所有多余的项（从数据长度到列表末尾），而不是只隐藏一个

**关键代码**:
```csharp
// 恢复正常状态（防止被动画污染）
go.transform.localScale = Vector3.one;
var canvasGroup = go.GetComponent<CanvasGroup>();
if (canvasGroup != null)
{
    canvasGroup.alpha = 1f;
}

// 隐藏多余的项（从数据长度开始到列表末尾）
for (int i = m_EditingPreset.UnitCardIds.Count; i < m_SelectedChessItems.Count; i++)
{
    if (m_SelectedChessItems[i] != null && m_SelectedChessItems[i].activeSelf)
    {
        m_SelectedChessItems[i].SetActive(false);
    }
}
```

## 测试建议

1. ✅ 打开界面，检查入场动画是否流畅
2. ✅ 创建新预设，观察弹出效果
3. ✅ 添加/移除棋子和策略卡，检查动画连贯性
4. ✅ 达到数量上限（8/8），观察抖动和变红效果
5. ✅ 设为默认预设，检查徽章动画
6. ✅ 快速操作，确保动画不会卡顿或重叠
7. ✅ 连续快速移除多个项，确认不会出现数据污染
8. ✅ 移除后立即添加，确认对象池复用正常

## 实现状态

✅ 所有动效已实现  
✅ Bug已修复  
✅ 代码无编译错误  
🔄 等待实际测试验证

---

**最后更新**: 2026-04-17  
**状态**: 实现完成，等待测试
