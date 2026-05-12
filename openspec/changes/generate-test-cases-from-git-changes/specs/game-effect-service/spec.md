## MODIFIED Requirements

### Requirement: GameEffectService 作为兼容层委托执行
GameEffectService.Execute(effectId, context) SHALL 将实际执行逻辑委托给 SpecialEffectManager，不再自行解析 BuffIds/SelfBuffIds。

#### Scenario: 单目标效果执行
- **WHEN** context.Targets.Count == 1，调用 Execute(effectId, context)
- **THEN** 内部调用 SpecialEffectManager.ApplyEffect(effectId, singleTarget, sourceType, effectId)，返回执行结果

#### Scenario: 多目标效果执行
- **WHEN** context.Targets.Count > 1，调用 Execute(effectId, context)
- **THEN** 内部调用 SpecialEffectManager.ApplyEffect(effectId, targets, sourceType, effectId, caster)

#### Scenario: context 为 null 时安全失败
- **WHEN** context 参数为 null
- **THEN** 方法返回 false，不抛出异常

#### Scenario: EffectSource 到 EffectSourceType 映射正确
- **WHEN** context.Source == EffectSource.Item
- **THEN** 转换为 EffectSourceType.Consumable
- **WHEN** context.Source == EffectSource.Synergy
- **THEN** 转换为 EffectSourceType.Synergy
- **WHEN** context.Source == EffectSource.CombatPrep
- **THEN** 转换为 EffectSourceType.Combat
