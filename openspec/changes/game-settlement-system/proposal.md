## Why

当前项目缺少完整的游戏结算系统。玩家在两个关键场景下需要结算本局游戏：
1. 通过传送门主动传送回基地
2. 在战斗中完全死亡

这两种情况都需要显示结算界面、执行数据统计、加载新场景、并从"局内"状态切换到"局外"状态。现有系统中缺少这个统一的流程框架。

## What Changes

- **新增结算系统框架**：创建 `SettlementManager`，统一管理结算流程，支持不同触发源（传送门、死亡等）
- **新增结算UI**：创建 `SettlementUIForm`，显示本局统计数据（经验、金币、掉落物品等）
- **新增结算流程**：创建 `SettlementProcedure`，协调结算 → 场景加载 → 状态转换的完整流程
- **集成传送门触发**：修改 `TeleportGateInteractable`，触发结算而非直接切换场景
- **集成死亡检测**：修改 `CombatState`，在玩家死亡时触发结算而非直接结束战斗
- **状态转换**：确保从 `CombatState` / `CombatPreparationState` 切换到 `BaseState`（或适当的局外状态）
- **场景加载协调**：结算界面在最上层，加载场景异步进行，玩家可手动关闭结算界面后进入新场景

## Capabilities

### New Capabilities
- `settlement-data-collection`: 收集本局游戏数据（经验、金币、掉落物品、击杀数等）
- `settlement-ui-display`: 结算界面显示和交互（显示统计数据、手动关闭按钮）
- `settlement-flow-management`: 结算流程管理（结算 → 场景加载 → 状态转换的完整编排）
- `death-settlement-trigger`: 死亡触发结算（检测玩家全灭，触发结算流程）
- `teleport-settlement-trigger`: 传送门触发结算（通过传送门交互触发结算而非直接切换场景）
- `in-out-state-transition`: 局内外状态转换（从战斗/探索状态切换到基地状态）

### Modified Capabilities
- `teleport-system`: 传送门系统的行为变更——不再直接加载场景，而是触发结算流程
- `combat-system`: 战斗系统的行为变更——死亡后触发结算流程而非直接结束战斗
- `game-state-management`: 游戏状态管理的扩展——增加结算状态或在既有状态机中加入结算流程

## Impact

**受影响的代码和系统**：
- `CombatState.cs` - 死亡检测和结算触发
- `TeleportGateInteractable.cs` - 传送门交互逻辑修改
- `CombatManager.cs` / 战斗系统 - 数据收集（经验、金币等）
- `GameProcedure.cs` - 流程编排，可能需要添加 `SettlementProcedure`
- `SceneStateManager.cs` - 状态转换管理
- UI 系统 - 新增 `SettlementUIForm` 和相关 UITable 配置
- `PlayerAccountDataManager.cs` - 可能需要保存结算结果

**新增依赖**：
- `SettlementManager` (核心管理器)
- `SettlementUIForm` (UI 表单)
- `SettlementProcedure` (可选，如果用流程架构)
- 相关的 DataTable（如果需要配置化结算奖励）
