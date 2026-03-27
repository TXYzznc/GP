## Context

游戏当前处于原型/早期迭代阶段，战斗系统和探索系统在同一个 Game 文件夹下快速扩张，导致：
- 多个 Manager 在没有统一命名约定的情况下陆续创建，出现 Battle/Combat 前缀混用
- 棋子（Chess）状态管理职责随功能需求被拆散到多个 Manager，读写路径不清晰
- 探索阶段的战斗触发器直接依赖战斗准备 UI，形成跨层调用

本次整理不引入新功能，目标是**最小化破坏性**地完成重构，保证逻辑等价、可验证。

---

## Goals / Non-Goals

**Goals:**
- 统一 Manager 命名前缀规范，消除 Battle/Combat/Chess 混淆
- 明确棋子状态管理的读写分层：持久化层（跨战斗）/ 战斗层（战中同步）/ 追踪层（实时查询）
- 解耦 CombatTriggerManager 对 UI 的直接引用，改为事件驱动
- 将 CombatTriggerManager 从 Explore/ 迁移到正确的子系统目录
- 拆分 SummonChessManager 中的死亡/HP 监听职责

**Non-Goals:**
- 不改变任何游戏逻辑或数值
- 不重构 Buff 系统、Item 系统、AI 状态机
- 不改变数据表结构（.txt / .bytes 文件）
- 不合并 ChessSelectionManager / ChessPlacementManager（职责清晰，保持原样）

---

## Decisions

### D1：命名前缀规范

**规则：**
- `Combat` 前缀 → 战斗流程中的核心系统（生命周期、实体追踪、部署管理）
- `Battle` 前缀 → 战斗场景级别的资源/场地（Arena、准备资源）
- `Chess` 前缀 → 棋子数据/解锁/生成（与战斗无关的通用棋子操作）
- `Global` 前缀 → 跨场景/跨战斗的持久化状态

**重命名列表：**

| 当前名称 | 新名称 | 原因 |
|----------|--------|------|
| `CombatChessManager` | `CombatEntityTracker` | 实际职责是追踪战场上的实体，不是"管理棋子" |
| `BattlePreparationManager` | `BattleLoadoutProvider` | 提供出战资源（棋子ID列表），非流程管理 |
| `CombatChessInventoryManager` | `ChessDeploymentTracker` | 追踪部署状态，非库存管理 |
| `CombatManagerUpdater` | `CombatTickDriver` | 更准确描述其作为帧循环驱动的角色 |

**不重命名（职责已清晰）：**
- `CombatManager`、`BattleArenaManager`、`SummonChessManager`、`GlobalChessManager`、`BattleChessManager`、`EnemySpawnManager`、`ChessSelectionManager`、`ChessPlacementManager`

---

### D2：棋子状态分层架构

当前三层实际上对应不同生命周期，应明确定义：

```
┌─────────────────────────────────────────────────────┐
│ GlobalChessManager          [持久化层]               │
│  职责：跨战斗的 HP 存储，死亡恢复，升星后重注册       │
│  读写：战斗结束时写入，战斗开始时读出                 │
└────────────────────────┬────────────────────────────┘
                         │ 战斗开始时读取初始HP
┌────────────────────────▼────────────────────────────┐
│ BattleChessManager          [战斗同步层]              │
│  职责：战斗期间 HP 同步，战斗结束时写回 Global        │
│  读写：实体注册时读 Global，战斗结束时写回 Global     │
│  Buff 委托：直接交给 ChessEntity.BuffManager         │
└────────────────────────┬────────────────────────────┘
                         │ 实时查询（阵营/存活）
┌────────────────────────▼────────────────────────────┐
│ CombatEntityTracker         [实时追踪层]              │
│  职责：战场实体注册/注销，阵营分组，死亡清理          │
│  读写：只读（注册/注销由 SummonChessManager 驱动）    │
└─────────────────────────────────────────────────────┘
```

**决策：不合并 GlobalChessManager 与 BattleChessManager**

- 理由：两者生命周期不同，Global 跨战斗存活，BattleChessManager 随战斗初始化销毁
- 替代方案（被否决）：合并为一个类 → 导致跨场景持久化与战斗临时状态在一个类中，违反单一职责

---

### D3：CombatTriggerManager 解耦与迁移

**问题：** `CombatTriggerManager` 直接调用 `CombatPreparationUI`，违反业务层不应直接依赖 UI 层的原则。

**方案：事件解耦**

```
// 当前（耦合）
var ui = GF.UI.GetUIForm<CombatPreparationUI>();
ui.ShowEnemyInitiativeBuffNotification(effectId);

// 目标（解耦）
CombatTriggerEvents.OnEnemyInitiativeTriggered?.Invoke(effectId);
// CombatPreparationUI 订阅此事件
```

新增 `CombatTriggerEvents.cs`（静态事件类），定义：
- `OnEnemyInitiativeTriggered(int effectId)`
- `OnSneakAttackTriggered(List<int> debuffPool)`
- `OnCombatContextCleared()`

**文件夹迁移：**
- 从 `Explore/Combat/CombatTriggerManager.cs` → `Combat/Trigger/CombatTriggerManager.cs`
- 同迁：`CombatTriggerType.cs`、`CombatTriggerContext.cs`、`CombatOpportunityDetector.cs`

---

### D4：SummonChessManager 职责拆分

**当前过多职责：**
1. 异步生成棋子（主职责）
2. 监听 HP 变化事件
3. 触发死亡状态转换

**方案：**
- 职责 1 保留在 `SummonChessManager`
- 职责 2+3 提取到 `ChessLifecycleHandler.cs`（MonoBehaviour，挂载在同一 GameObject）

`ChessLifecycleHandler` 职责：
- 订阅 `ChessEntity.OnHPChanged`
- 当 HP ≤ 0 时通知 `BattleChessManager` 和 `CombatEntityTracker`
- 驱动棋子进入死亡状态

---

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|----------|
| 重命名导致 Unity 场景/预制体引用丢失（MonoBehaviour 类名变化） | 优先重命名纯 C# 类（非 MonoBehaviour），MonoBehaviour 类保留原名或使用 `[FormerlySerializedAs]` |
| CombatTriggerManager 迁移目录后 `.asmdef` 引用需要更新 | 迁移前确认 asmdef 配置，统一在 `Hotfix.asmdef` 下 |
| 事件解耦后 UI 订阅时序问题（UI 未初始化时事件已触发） | 事件发送前检查 UI 是否已打开，或使用延迟分发 |
| ChessLifecycleHandler 拆分后与 SummonChessManager 生命周期绑定需要协调 | 在 SummonChessManager.SpawnChessAsync 中同步 AddComponent |

---

## Migration Plan

按以下顺序执行，每步可独立验证：

1. **Step 1：纯重命名（无逻辑变化）**
   - 重命名 4 个 Manager 类和文件（见 D1 重命名列表）
   - 更新所有引用方（grep 全局替换）
   - 验证：编译通过，无运行时报错

2. **Step 2：迁移 CombatTrigger 文件夹**
   - 移动 4 个文件到 `Combat/Trigger/`
   - 更新 namespace（如有）
   - 验证：编译通过

3. **Step 3：添加 CombatTriggerEvents，解耦 UI 引用**
   - 新增 `CombatTriggerEvents.cs`
   - 修改 `CombatTriggerManager` 改为发送事件
   - 修改 `CombatPreparationUI` 订阅事件
   - 验证：战斗触发流程功能等价

4. **Step 4：提取 ChessLifecycleHandler**
   - 新建 `ChessLifecycleHandler.cs`
   - 从 `SummonChessManager` 中提取 HP 监听/死亡逻辑
   - 验证：棋子死亡流程功能等价

---

## Open Questions

- `CombatManagerUpdater`（建议改名 `CombatTickDriver`）：是否需要扩展以支持更多 Manager 的 Tick？如果是，考虑改为通用 `TickableManagerRegistry`
- `BattleChessManager.AddBuff()/RemoveBuff()` 是否应该完全删除，直接让调用方访问 `ChessEntity.BuffManager`？（目前是单行委托，价值有限）
- `EnemyGroupManager` 放在 `Explore/Enemy/System/` 是否合理？其触发 GroupCombat 的行为是否应迁移到 `Combat/Trigger/`？
