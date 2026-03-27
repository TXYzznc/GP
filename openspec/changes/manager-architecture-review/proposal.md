## Why

Game 文件夹中随着功能迭代新增了多个 Manager，命名混乱（Battle/Combat/Chess 前缀交叉使用）、职责边界模糊（如棋子状态被 GlobalChessManager、BattleChessManager、CombatChessManager 三个类分散管理），以及探索系统中的 Manager 直接操作战斗系统，导致模块间耦合度过高，难以维护和扩展。

## What Changes

- **重命名** Manager 脚本，统一命名前缀规范，消除 Battle/Combat 前缀的混淆
- **拆分** 职责混杂的 Manager（尤其是 SummonChessManager：生成 + 生命周期 + HP 监听三合一）
- **合并** 高度重叠的 Manager（GlobalChessManager + BattleChessManager 的 HP 管理职责）
- **解耦** CombatTriggerManager 对 UI 层的直接依赖（当前直接调用 CombatPreparationUI）
- **迁移** 探索系统中属于战斗核心逻辑的部分（CombatTriggerManager 位于 Explore/ 文件夹但管理战斗流程）
- **整理** 文件夹结构，确保 Combat/、Explore/、SummonChess/ 各子系统边界清晰

## Capabilities

### New Capabilities
- `manager-naming-convention`: 定义项目 Manager 统一命名规范（前缀含义、职责边界）

### Modified Capabilities
- `chess-state-management`: 当前由三个 Manager 分散管理棋子状态（Global/Battle/CombatChess），整理为职责清晰的分层结构
- `combat-trigger-flow`: 战斗触发流程从探索系统解耦，明确触发器与战斗系统的接口边界
- `chess-spawn-lifecycle`: SummonChessManager 职责拆分，生成与状态管理分离

## Impact

### 受影响的文件（核心 Manager）

| 当前文件 | 问题 | 建议操作 |
|----------|------|----------|
| `Combat/Core/CombatManager.cs` | 直接引用 6 个 Manager，是空壳 Placeholder | 明确职责边界，作为战斗生命周期入口 |
| `Combat/Core/CombatChessManager.cs` | 名称与 CombatManager 过于接近，实际做实体追踪 | 重命名为 `CombatEntityTracker` |
| `Combat/Core/BattleChessManager.cs` | Battle 前缀与 Combat 前缀混用，HP 同步职责与 GlobalChessManager 重叠 | 合并到 GlobalChessManager 或明确接口 |
| `SummonChess/Manager/GlobalChessManager.cs` | 跨战斗状态存储（HP、死亡），与 BattleChessManager 重叠 | 保留为唯一持久化 HP 管理者，明确读写接口 |
| `SummonChess/Manager/SummonChessManager.cs` | 生成 + HP 监听 + 死亡状态转换三合一 | 拆出 HP 监听/死亡处理到独立 Handler |
| `Explore/Combat/CombatTriggerManager.cs` | 位于 Explore/ 却管理战斗流程，直接调用 CombatPreparationUI | 迁移到 Combat/，UI 交互改为事件/接口 |
| `Combat/Core/BattlePreparationManager.cs` | Battle 前缀，实际做战斗前的棋子资源准备 | 重命名为 `CombatResourceProvider` 或 `BattleLoadoutProvider` |
| `Combat/Chess/CombatChessInventoryManager.cs` | 长名，职责是部署状态追踪 | 重命名为 `ChessDeploymentTracker` |

### 受影响的依赖关系
- `CombatManager` 的依赖链需要重新梳理
- `CombatTriggerManager` → `CombatPreparationUI` 的直接 UI 引用需要通过事件解耦
- `BattleChessManager.AddBuff()/RemoveBuff()` 是对 `ChessEntity.BuffManager` 的委托，可考虑直接访问

### 不受影响
- Buff 系统（BuffManager、BuffFactory、各 Buff 实现）职责清晰，无需调整
- Item/Inventory 系统独立，无需调整
- AI 状态机（FSMMeleeAI、FSMRangedAI）职责清晰，无需调整
- 数据表类（ChessDataManager、各 xxxTable.cs）职责清晰
