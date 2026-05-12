## ADDED Requirements

### Requirement: 装备穿戴时应用 SpecialEffect
ChessEquipmentManager 在 ApplyEquipmentStats 时，若装备配置了 SpecialEffectId > 0，SHALL 调用 SpecialEffectManager.ApplyEffect 为棋子绑定特效。

#### Scenario: 有 SpecialEffectId 的装备穿戴
- **WHEN** 为棋子穿戴 SpecialEffectId=10 的装备
- **THEN** SpecialEffectManager.ApplyEffect(10, entity, Equipment, itemId) 被调用，特效生效

#### Scenario: 无 SpecialEffectId 的装备穿戴
- **WHEN** 穿戴 SpecialEffectId=0 的装备
- **THEN** SpecialEffectManager 不被调用，基础属性正常应用

#### Scenario: 装备卸下时移除 SpecialEffect
- **WHEN** 卸下已穿戴的装备
- **THEN** SpecialEffectManager.RemoveEffectBySource(entity, Equipment, itemId) 被调用，特效移除

### Requirement: AttributeType.SpellPower 替代 MagicPower
系统 SHALL 统一使用 SpellPower，对旧配置中 "MagicPower" 字段自动兼容转换。

#### Scenario: 新配置 SpellPower 正常读取
- **WHEN** 装备配置 BaseAttributes 中含 "SpellPower": 50
- **THEN** ModifySpellPower(50) 被调用

#### Scenario: 旧配置 MagicPower 兼容读取
- **WHEN** 装备配置 BaseAttributes 中含 "MagicPower": 50（旧格式）
- **THEN** 自动转换为 SpellPower，ModifySpellPower(50) 被调用，不报错
