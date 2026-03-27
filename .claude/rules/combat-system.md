---
paths: ["Assets/AAAGame/Scripts/Game/Combat/**/*.cs", "Assets/AAAGame/Scripts/Game/Buff/**/*.cs", "Assets/AAAGame/Scripts/GameState/**/*.cs", "Assets/AAAGame/Scripts/Manager/CombatTriggerManager.cs"]
---

# 战斗系统规则

## 系统架构

```
CombatOpportunityDetector（检测玩家附近敌人）
    ↓ 触发
CombatTriggerManager（判断触发类型：偷袭/遭遇战/敌方先手）
    ↓ 进入
CombatPreparationState（准备阶段 UI 和 Buff 选择）
    ↓ 确认
CombatState（实时战斗）
    ↓ 结束/脱战
CombatEscapeSystem / GameOverProcedure
```

## 战斗触发类型（CombatTriggerType）

| 类型 | 条件 | 效果 |
|------|------|------|
| `SneakAttack` | 玩家从背后接近敌人 | 玩家选择偷袭 Debuff 施加给敌方 |
| `Encounter` | 正面遭遇 | 无先手加成，双方平等 |
| `EnemyInitiative` | 敌方先手 | 敌方随机获得先手 Buff |
| `PlayerInitiative` | 玩家先手 | 玩家随机获得先手 Buff |

## SpecialEffectTable 使用规范

先手/偷袭效果**必须**通过 `SpecialEffectTable` 配置，不要在代码里硬编码效果：

- `EffectCategory = 1`：玩家先手效果
- `EffectCategory = 2`：敌方先手效果
- `EffectCategory = 3`：偷袭效果（供 SneakDebuffSelectionUI 选择）

效果包含两种 Buff 列表：
- `BuffIds`：施加给**对方**的 Buff
- `SelfBuffIds`：施加给**自身方**的 Buff

## Buff 应用规范

```csharp
// 统一通过 BuffApplyHelper 应用，不要直接操作角色属性
BuffApplyHelper.ApplyBuff(targetCharacter, buffId, isGroupTarget: false);
BuffApplyHelper.ApplyBuff(targetCharacter, buffId, isGroupTarget: true); // 全体
```

## 状态机规范

- 游戏状态在 `GameState/States/` 下，继承 `FsmState<InGameState>`
- 状态切换通过 `ChangeState<TState>()` 进行，不要直接 new 状态对象
- 准备阶段异步方法必须返回 `UniTask`，用 `await` 等待，不用 `async void`
- 状态退出时（`OnLeave`）必须清理所有订阅的事件

## 常见问题

- `CombatTriggerContext` 是一次性对象，战斗结束后通过 `ExitCombat()` 清除
- 先手 Buff 通知 UI（`InitiativeBuffNotificationUI`）是 `CombatPreparationUI` 的子对象，不是独立 UI
- 脱战系统（`CombatEscapeSystem`）的成功率从 `EscapeRuleTable` 读取，不要硬编码
