## MODIFIED Requirements

### Requirement: 战斗先手/偷袭效果从配置表驱动
CombatRuleTable 新增 InitiativeEffectId 和 SneakAttackEffectId 字段，战斗初始化时通过 SpecialEffectManager 应用效果，替代硬编码逻辑。

#### Scenario: 先手效果应用
- **WHEN** 战斗开始且一方获得先手（由 CombatTriggerEvents 判定）
- **THEN** 读取 CombatRuleTable.InitiativeEffectId
- **AND** 调用 SpecialEffectManager.ApplyEffect(effectId, targetChess, Combat, ruleId)
- **AND** 效果通常为 Buff 类型（EffectType=2），通过 BuffManager 添加速度/攻击力增益

#### Scenario: 偷袭效果应用
- **WHEN** 战斗开始且一方发动偷袭
- **THEN** 读取 CombatRuleTable.SneakAttackEffectId
- **AND** 调用 SpecialEffectManager.ApplyEffect(effectId, targetChess, Combat, ruleId)

#### Scenario: 与现有 CombatTriggerEvents 的集成
- **WHEN** CombatTriggerEvents 确定先手/偷袭归属后
- **THEN** 不再直接使用硬编码的效果 ID 或 Buff 池
- **AND** 改为从 CombatRuleTable 读取 EffectId，通过 SpecialEffectManager 统一应用
- **AND** 现有的 LastSneakDebuffPool / LastPlayerInitiativeBuffPool 机制保持，用于战斗结束时清理

### Requirement: 战斗效果的生命周期
战斗中应用的 Buff 效果在战斗结束时由现有 BuffManager 的清理机制处理。

#### Scenario: 战斗结束清理
- **WHEN** 战斗结束
- **THEN** BuffManager 按现有逻辑清理战斗中的临时 Buff
- **AND** SpecialEffectManager 中对应的 Combat 来源效果标记为 inactive

#### Scenario: 效果持续整场战斗
- **WHEN** 先手 Buff 被添加
- **THEN** Buff 持续至战斗结束（由 BuffTable 的 Duration 配置决定）
- **AND** 不因回合数增加而额外 Tick
