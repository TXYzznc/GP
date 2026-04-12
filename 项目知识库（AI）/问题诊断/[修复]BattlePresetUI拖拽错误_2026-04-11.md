# BattlePresetUI 拖拽错误修复 & Mask 显示优化

## 问题描述

### 问题 1：拖拽错误
在 `BattlePresetUI` 中，棋子项（ChessItemUI）的拖拽功能产生错误：

```
[Error] [ChessItemUI] OnBeginDrag: 实例不存在 instanceId=preset_1
[Error] [ChessItemUI] OnBeginDrag: 实例不存在 instanceId=pool_1
```

### 问题 2：Mask 显示不正确
预设界面的棋子项 Mask 显示逻辑不符合需求：
- 已选棋子区域：Mask 应该默认隐藏
- 可选棋子池：已选中的棋子应显示 Mask，并显示"已选中"文本（而非"已出战"）

## 根本原因

### 拖拽错误
1. `BattlePresetUI` 中使用虚拟ID（`preset_1`、`pool_1`）
2. `ChessItemUI.OnBeginDrag()` 尝试从 `ChessDeploymentTracker` 查询这些虚拟ID
3. 虚拟ID不存在，导致错误

### Mask 显示问题
1. `ChessItemUI.RefreshDeployStatus()` 中的 Mask 显示逻辑是为战斗准备界面设计的
2. 预设界面需要不同的 Mask 显示规则

## 修复方案

### 修改内容

**文件：** `Assets/AAAGame/Scripts/UI/BattlePresetUI.cs`

#### 1. 添加 using 语句
```csharp
using System;
using System.Reflection;
using UnityEngine.EventSystems;
```

#### 2. 禁用拖拽功能
在 `CreateSelectedChessItem()` 和 `CreatePoolChessItem()` 中禁用 `EventTrigger`

#### 3. 管理 Mask 显示
- **已选棋子区域**：调用 `HideChessItemMask()` 隐藏 Mask
- **可选棋子池**：
  - 未选中：隐藏 Mask
  - 已选中：调用 `ShowChessItemSelectedMask()` 显示 Mask 和"已选中"文本

#### 4. 新增辅助方法
```csharp
private void HideChessItemMask(GameObject chessItemGo)
{
    // 通过反射访问 varMask 和 varText，设置为隐藏
}

private void ShowChessItemSelectedMask(GameObject chessItemGo)
{
    // 通过反射访问 varMask 和 varText，显示 Mask 并设置文本为"已选中"
}
```

## 设计说明

### 为什么使用反射？
- `varMask` 和 `varText` 是 `ChessItemUI` 的 private 字段
- 预设界面需要覆盖 `RefreshDeployStatus()` 中的默认 Mask 显示逻辑
- 反射提供了最小侵入的解决方案，无需修改 `ChessItemUI` 本身

### Mask 显示规则
| 区域 | 状态 | Mask | Text |
|------|------|------|------|
| 已选棋子 | - | 隐藏 | 隐藏 |
| 可选池 | 未选中 | 隐藏 | 隐藏 |
| 可选池 | 已选中 | 显示 | "已选中" |

## 验证方法

1. 打开 BattlePresetUI
2. 检查已选棋子区域：Mask 应隐藏
3. 检查可选棋子池：
   - 未选中的棋子：Mask 隐藏
   - 已选中的棋子：Mask 显示，Text 显示"已选中"
4. 尝试拖拽棋子：不应出现错误日志

## 相关文件

- `Assets/AAAGame/Scripts/UI/BattlePresetUI.cs` - 修复文件
- `Assets/AAAGame/Scripts/UI/Item/CombatItems/ChessItemUI.cs` - 棋子 UI 组件
- `Assets/AAAGame/Scripts/UI/UIItemVariables/ChessItemUI.Variables.cs` - 棋子 UI 变量

---

**修复时间：** 2026-04-11  
**修复者：** AI Assistant  
**状态：** ✅ 完成
