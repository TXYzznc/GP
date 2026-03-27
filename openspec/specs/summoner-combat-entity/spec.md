# Spec: Summoner Combat Entity

## Purpose

将召唤师（Summoner）作为独立战斗实体纳入战斗系统，使其能够被注册为合法目标、承受伤害、并在每场战斗开始时重置状态。

## Requirements

### Requirement: 召唤师作为战斗实体注册
战斗开始时，系统 SHALL 将 `SummonerCombatProxy`（Camp=0）注册到 `CombatEntityTracker`，使其成为合法的战斗目标。

#### Scenario: 战斗开始时注册召唤师
- **WHEN** `CombatManager.StartCombat()` 被调用
- **THEN** `CombatEntityTracker.RegisterSummoner(proxy)` 被调用，召唤师进入目标池

#### Scenario: 战斗结束时注销召唤师
- **WHEN** `CombatManager.EndCombat()` 被调用
- **THEN** `CombatEntityTracker.UnregisterSummoner()` 被调用，召唤师移出目标池

### Requirement: 敌方 AI 可选召唤师为攻击目标
当敌方（Camp=1）查询目标列表时，系统 SHALL 将存活的召唤师包含在返回结果中。

#### Scenario: 召唤师存活时出现在敌方目标列表
- **WHEN** `CombatEntityTracker.GetEnemyCache(myCamp: 1)` 被调用
- **THEN** 返回列表包含 `SummonerCombatProxy` 对应的目标条目（HP > 0）

#### Scenario: 召唤师死亡后不出现在目标列表
- **WHEN** 召唤师 `IsDead == true`，且 `GetEnemyCache(myCamp: 1)` 被调用
- **THEN** 返回列表不包含召唤师

### Requirement: 召唤师每场战斗 HP 初始化为满值
战斗开始时，系统 SHALL 将召唤师 HP/MP 重置为配置最大值，不继承上一场战斗结束时的状态。

#### Scenario: 战斗开始时 HP 回满
- **WHEN** `CombatManager.StartCombat()` 被调用
- **THEN** `SummonerRuntimeDataManager.InitializeForBattle()` 被调用，`CurrentHP == MaxHP`，`CurrentMP == MaxMP`

#### Scenario: 战斗失败不回写召唤师状态
- **WHEN** 战斗以失败结束（`EndCombat(isVictory: false)`）
- **THEN** `SummonerRuntimeDataManager` 不向任何持久化存储写入当前 HP/MP 值

### Requirement: 召唤师可承受伤害
`SummonerCombatProxy` SHALL 提供 `TakeDamage(float damage)` 接口，使敌人攻击逻辑能够对召唤师造成伤害。

#### Scenario: 受到伤害后 HP 减少
- **WHEN** `SummonerCombatProxy.TakeDamage(damage)` 被调用且 damage > 0
- **THEN** `SummonerRuntimeDataManager.CurrentHP` 减少对应数值（不低于 0）

#### Scenario: HP 降至 0 后标记为死亡
- **WHEN** 伤害导致 HP 降至 0
- **THEN** `SummonerCombatProxy.IsDead == true`
