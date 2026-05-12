## MODIFIED Requirements

### Requirement: 羁绊效果通过 SpecialEffectManager 应用
SynergyManager 在羁绊激活/解除时通过 SpecialEffectManager 管理效果，替代直接调用 GameEffectService。

#### Scenario: 羁绊激活
- **WHEN** SynergyManager.ActivateSynergy() 检测到羁绊条件满足
- **AND** SynergyTable 中对应羁绊的 EffectId != 0
- **THEN** 对所有参与棋子调用 SpecialEffectManager.ApplyEffect(effectId, chess, Synergy, synergyId)
- **AND** 效果类型通常为 Buff（EffectType=2），通过 BuffManager 添加 Buff

#### Scenario: 羁绊解除
- **WHEN** SynergyManager 检测到羁绊条件不再满足（如卸下宝物）
- **THEN** 对所有参与棋子调用 SpecialEffectManager.RemoveEffectBySource(chess, Synergy, synergyId)
- **AND** 之前添加的 Buff 被精确移除

#### Scenario: 多重羁绊
- **WHEN** 一个宝物同时参与多个羁绊
- **THEN** 每个羁绊独立管理自己的效果（不同 synergyId 作为 sourceId）
- **AND** 卸下该宝物时，所有不再满足条件的羁绊各自移除自己的效果

### Requirement: 羁绊效果与宝物穿戴效果独立
宝物穿戴的 EffectId（Passive）和羁绊的 EffectId（Buff）完全独立运作。

#### Scenario: 单件宝物穿戴
- **WHEN** 穿戴单件宝物
- **THEN** 仅该宝物的 TreasureTable.EffectId 生效（Passive 效果）
- **AND** 不触发任何羁绊

#### Scenario: 多件宝物激活羁绊
- **WHEN** 穿戴足够数量的宝物满足羁绊条件
- **THEN** 每件宝物各自的 Passive 效果 + 羁绊的 Buff 效果都生效
- **AND** 卸下一件导致羁绊解除时，仅移除羁绊 Buff，不影响其他宝物的 Passive

### Requirement: 与现有 SynergyManager 的最小改动集成
改造应尽量保持 SynergyManager 的检测逻辑不变，只修改效果应用方式。

#### Scenario: 最小侵入改动
- **WHEN** SynergyManager.ActivateSynergy() 中需要应用效果
- **THEN** 将原有的 GameEffectService.Execute(context) 调用替换为 SpecialEffectManager.ApplyEffect()
- **AND** 保持羁绊检测（RequireCount、RequireIds 匹配）逻辑完全不变

#### Scenario: GameEffectService 作为兼容层
- **WHEN** GameEffectService.Execute() 被调用（其他代码尚未迁移时）
- **THEN** 内部委托给 SpecialEffectManager.ApplyEffect()
- **AND** 保持向后兼容，逐步迁移
