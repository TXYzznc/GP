## Why

玩家（探索状态）和召唤师（战斗状态）在代码中高度耦合：`PlayerRuntimeDataManager` 直接从 `SummonerTable.MoveSpeed` 读取玩家移速；召唤师没有阵营和战斗实体身份，无法作为攻击目标参与战斗。随着 Buff 系统和战斗系统的扩展，这种耦合会导致越来越多的歧义逻辑，需要在系统规模扩大前明确分离。

## What Changes

- **新增** `SummonerTable` 中独立的 `PlayerMoveSpeed` 字段（玩家探索移速），与现有 `MoveSpeed`（召唤师战斗移速）分离
- **新增** `SummonerCombatEntity`：召唤师作为战斗单位参与战斗，持有阵营（Camp=0，玩家方），可被敌人选为攻击目标
- **修改** `SummonerRuntimeDataManager`：每次战斗开始时重置 HP/MP 至满值（不继承上一场状态），战斗结束后不回写
- **修改** `PlayerRuntimeDataManager`：移速字段改为从 `SummonerTable.PlayerMoveSpeed` 读取，与召唤师战斗移速解耦
- **修改** Buff 应用层：区分 `BuffTarget.Player`（作用于探索状态移速/污染值）和 `BuffTarget.Summoner`（作用于战斗HP/MP/移速）
- **BREAKING** `SummonerTable` 新增 `PlayerMoveSpeed` 列，需重新生成配置表代码并填写数据（已手动完成）

## Capabilities

### New Capabilities

- `summoner-combat-entity`: 召唤师作为战斗参与者——持有阵营、可被敌人攻击、每场战斗 HP 初始化为满值
- `player-summoner-stat-split`: 玩家移速与召唤师移速的数据分离，支持各自独立被 Buff 修改

### Modified Capabilities

（暂无已有 spec 文件，openspec/specs/ 目录为空）

## Impact

- `SummonerTable.cs`（自动生成）：新增 `PlayerMoveSpeed` 字段
- `PlayerRuntimeDataManager.cs`：`Initialize()` 改读 `PlayerMoveSpeed`
- `SummonerRuntimeDataManager.cs`：新增 `InitializeForBattle()` / `ResetToFull()` 方法
- `CombatEntityTracker`：需能注册召唤师实体并将其纳入敌人 AI 的目标选择
- `EnemyAI` 目标选择逻辑：目标池中包含召唤师实体
- Buff 系统 `BuffApplyHelper`：增加 `BuffTarget` 枚举区分作用对象（应该不需要？作用于召唤师的Buff和作用于棋子的Buff原理完全一致，作用于玩家的Buff较少，可以直接单独在Buff脚本中特殊处理效果，无需扩展系统增加负担）
- 配置表：`SummonerTable.xlsx` 新增 `PlayerMoveSpeed` 列
