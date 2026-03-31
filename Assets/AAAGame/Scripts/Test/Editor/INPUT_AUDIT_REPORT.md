# 键盘输入审计报告

生成时间：2026-03-31

## 📊 输入总览

| 类别 | 数量 | 状态 |
|------|------|------|
| **正常玩家输入** | 15+ | ✅ 通过 PlayerInputManager |
| **测试输入** | 20+ | ⚠️ 部分未禁用 |
| **直接 Input.Get 调用** | 15+ | ⚠️ 可能冲突 |
| **UI 输入** | 5+ | ✅ UI 内部处理 |

---

## ✅ 正确的输入方式

### PlayerInputManager（推荐）
位置：`Assets/AAAGame/Scripts/Game/Input/PlayerInputManager.cs`

**输入列表：**
- `A/D` - 移动
- `Shift` - 冲刺
- `鼠标` - 视角转动
- `I` - 背包
- `G` - 仓库
- `Q/E/R` - 召唤师技能
- `1-5` - 热键栏

---

## ⚠️ 直接 Input.Get 调用（潜在竞争）

### 🔴 高优先级修复

| 文件 | 按键 | 功能 | 建议 |
|------|------|------|------|
| **ChessSelectionManager.cs** | 左键/右键 | 选择棋子 | 应通过 PlayerInputManager |
| **ChessPlacementManager.cs** | 左键/右键 | 放置棋子 | 应通过 PlayerInputManager |
| **CombatOpportunityDetector.cs** | Space | 机会攻击 | 应通过 PlayerInputManager |
| **WorldItemPickup.cs** | 右键 | 捡起物品 | 应通过 PlayerInputManager |
| **UIFormBase.cs** | Esc | 关闭UI | ✅ UI内部可接受 |
| **NewGameUI.cs** | Space | 开始游戏 | ✅ UI内部可接受 |

### 🟡 测试输入（已禁用）

| 文件 | 按键 | 状态 |
|------|------|------|
| **EnemyTestController.cs** | F1-F8 | ✅ 已禁用 (Update 注释) |
| **ProjectileTestController.cs** | Space/A/R/H/箭头/T | ⚠️ **还在 HandleInput() 中！** |
| **GameTestManager.cs** | U/O/P/R | ⚠️ **还在 HandleStateTestKeys() 中！** |
| **ChessTestInput.cs** | Space/1/2/3 | ✅ 已注释 (Update 注释) |

---

## 🎯 输入竞争风险分析

### 可能冲突的按键

| 按键 | 玩家输入 | 测试输入 | 冲突? |
|------|---------|---------|-------|
| **Space** | ❌ 无 | ProjectileTestController | ✅ 安全 |
| **A** | 移动左 | ProjectileTestController | 🔴 **冲突！** |
| **Q/E/R** | 召唤技能 | 无 | ✅ 安全 |
| **1-5** | 热键栏 | 无 | ✅ 安全 |
| **左键** | ChessSelection | 无 | ✅ 安全 |
| **右键** | ChessSelection | ChessPlacement | 🔴 **冲突！** |

---

## 🛠️ 立即行动项

### 必须修复
1. ❌ **ProjectileTestController.HandleInput()** - 被注释掉但仍在代码中
   - 需要删除或完全移到 GameTestWindow

2. ❌ **GameTestManager.HandleStateTestKeys()** - 被注释掉但仍在代码中
   - 快捷键 U/O/P/R 可能在某些情况下被触发

3. ❌ **直接 Input.Get 调用的棋子系统**
   - ChessSelectionManager - 用 PlayerInputManager 的鼠标位置信息
   - ChessPlacementManager - 同上

4. ❌ **CombatOpportunityDetector**
   - Space 键应该从 PlayerInputManager 获取

### 建议的输入架构

```
所有输入 → PlayerInputManager
    ↓
分发给具体系统：
  - PlayerController（移动/冲刺）
  - PlayerSkillManager（技能）
  - ChessSelectionManager（选择）
  - CombatOpportunityDetector（机会攻击）
  - UI 系统（Esc 关闭等）
```

---

## 📋 检查清单

- [ ] 删除或移到 GameTestWindow：ProjectileTestController.HandleInput()
- [ ] 删除或移到 GameTestWindow：GameTestManager.HandleStateTestKeys()
- [ ] 修改 ChessSelectionManager 使用 PlayerInputManager
- [ ] 修改 ChessPlacementManager 使用 PlayerInputManager
- [ ] 修改 CombatOpportunityDetector 使用 PlayerInputManager
- [ ] 修改 WorldItemPickup 使用 PlayerInputManager
- [ ] 在 PlayerInputManager 中添加缺失的输入事件
- [ ] 测试输入是否有冲突

---

## 关键发现

⚠️ **问题：** 项目中存在 3 个地方的测试输入虽然被注释掉，但代码仍然存在，可能导致意外触发

✅ **优点：** 大多数玩家输入已经通过 PlayerInputManager 集中管理

🔴 **关键风险：** 棋子选择/放置系统直接使用 Input.Get，容易与其他输入冲突
