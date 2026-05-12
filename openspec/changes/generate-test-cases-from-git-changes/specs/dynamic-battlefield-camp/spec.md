## ADDED Requirements

### Requirement: 动态战场阵营分配
系统 SHALL 支持通过 AllocateBattlefield() 动态分配互为敌对的两个 Camp ID，从 DynamicBattleStart(100) 起每次递增 2。

#### Scenario: 第一次分配
- **WHEN** 调用 AllocateBattlefield()
- **THEN** 返回 (100, 101)，且 IsEnemy(100, 101) == true

#### Scenario: 第二次分配独立
- **WHEN** 连续调用 AllocateBattlefield() 两次
- **THEN** 第一次返回 (100,101)，第二次返回 (102,103)，且 IsEnemy(100,103) == false（不同战场不互为敌人）

#### Scenario: 释放所有战场后重置
- **WHEN** 调用 ReleaseAllBattlefields()
- **THEN** 所有 Camp ID >= 100 的敌对关系被清除，下次 AllocateBattlefield() 重新从 (100,101) 开始

### Requirement: 动态阵营不影响默认 PVE 关系
移除 PVP Team1-4 后，Player vs Enemy 的默认 PVE 关系 SHALL 保持不变。

#### Scenario: PVE 默认敌对
- **WHEN** 检查 IsEnemy(CampType.Player, CampType.Enemy)
- **THEN** 返回 true

#### Scenario: 中立不敌对
- **WHEN** 检查 IsEnemy(CampType.Neutral, CampType.Player)
- **THEN** 返回 false

#### Scenario: ClearCustomRelations 恢复默认
- **WHEN** 注册了自定义敌对关系后调用 ClearCustomRelations()
- **THEN** 自定义关系消失，默认 PVE 关系恢复，s_NextDynamicBattleIndex 归零

### Requirement: 邪灵大招阵营过滤
EvilSpiritMagicCircle 搜索目标时 SHALL 只对与施法者敌对的单位生效。

#### Scenario: 友方不被邪灵大招命中
- **WHEN** 邪灵使用大招，场上存在同阵营友方单位
- **THEN** 友方单位不受到伤害
