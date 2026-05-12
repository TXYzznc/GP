## ADDED Requirements

### Requirement: 战斗结束销毁所有飞行投射物
CombatManager 在 EndBattle 流程中，SHALL 在销毁棋子实体之前，销毁场上所有 ChessProjectile 对象。

#### Scenario: 有投射物时正常销毁
- **WHEN** 战斗结束时场上存在 N 个飞行中的 ChessProjectile
- **THEN** 所有 ChessProjectile GameObject 被 Destroy，日志输出"已销毁 N 个飞行中的投射物"

#### Scenario: 无投射物时不报错
- **WHEN** 战斗结束时场上没有 ChessProjectile
- **THEN** EndBattle 正常执行，无异常

#### Scenario: 投射物不在战斗结束后命中目标
- **WHEN** 投射物正在飞行途中触发战斗结束
- **THEN** 投射物被销毁，目标不受伤害，不触发技能命中逻辑
