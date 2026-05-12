## MODIFIED Requirements

### Requirement: 装备穿戴/卸下时应用被动效果
ChessEquipmentManager 在穿戴/卸下装备时，通过 SpecialEffectManager 管理装备的被动效果。

#### Scenario: 穿戴装备
- **WHEN** ChessEquipmentManager.EquipItem() 被调用
- **AND** 该装备在 EquipmentTable 中配置了 EffectId != 0
- **THEN** 调用 SpecialEffectManager.ApplyEffect(effectId, chess, Equipment, equipId)
- **AND** PassiveEffect 直接修改 ChessAttribute

#### Scenario: 卸下装备
- **WHEN** ChessEquipmentManager.UnequipItem() 被调用
- **THEN** 调用 SpecialEffectManager.RemoveEffectBySource(chess, Equipment, equipId)
- **AND** PassiveEffect 反向修改 ChessAttribute，精确回滚

#### Scenario: 装备 BaseAttributes 与 EffectId 并存
- **WHEN** 装备同时有 BaseAttributes（EquipmentTable 中的固定属性）和 EffectId（特殊效果）
- **THEN** 两者都生效：BaseAttributes 由现有逻辑处理，EffectId 由 SpecialEffectManager 处理
- **AND** 两者互不干扰，卸下时各自清除

### Requirement: 宝物穿戴/卸下时应用效果
宝物穿戴时通过 SpecialEffectManager 应用效果，卸下时移除。

#### Scenario: 穿戴宝物
- **WHEN** 宝物被穿戴到棋子
- **AND** TreasureTable 中配置了 EffectId != 0
- **THEN** 调用 SpecialEffectManager.ApplyEffect(effectId, chess, Treasure, treasureId)

#### Scenario: 卸下宝物
- **WHEN** 宝物从棋子卸下
- **THEN** 调用 SpecialEffectManager.RemoveEffectBySource(chess, Treasure, treasureId)

#### Scenario: 宝物 BaseAttributes + EffectId
- **WHEN** 宝物同时有固定属性和特殊效果
- **THEN** 两者都独立生效和移除

### Requirement: 消耗品使用时执行即时效果
消耗品使用时通过 SpecialEffectManager 执行即时效果。

#### Scenario: 使用消耗品
- **WHEN** ConsumableItem.OnUse() 被调用
- **AND** ConsumableTable.UseEffectId != 0
- **THEN** 调用 SpecialEffectManager.ApplyEffect(useEffectId, chess, Consumable, consumableId)
- **AND** InstantEffect 通过 ItemEffectFactory 执行具体效果

#### Scenario: 与现有 ItemEffectFactory 的兼容
- **WHEN** InstantEffect 被执行
- **THEN** 内部调用 ItemEffectFactory.Create(effectType) 创建具体效果实例
- **AND** 调用 IItemEffect.Execute(context) 执行
- **AND** 保持与现有消耗品效果代码的完全兼容

### Requirement: 效果应用不影响现有 BaseAttributes 逻辑
装备和宝物的 BaseAttributes（固定属性加成）由现有逻辑处理，SpecialEffectManager 只负责 EffectId 对应的特殊效果。

#### Scenario: 分离关注点
- **WHEN** 穿戴装备时
- **THEN** BaseAttributes 由 ChessEquipmentManager 现有逻辑处理（直接加属性）
- **AND** EffectId 由 SpecialEffectManager 处理（可能是 Passive、Buff 或 Trigger）
- **AND** 两个系统独立运作，互不依赖
