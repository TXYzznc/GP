## ADDED Requirements

### Requirement: SpecialEffectInstance 抽象基类
新增 SpecialEffectInstance 抽象类作为所有效果实例的基类，定义统一的效果生命周期接口。

**接口**：
- Apply(target): 将效果应用到目标棋子
- Remove(): 从目标移除效果
- IsActive: 效果是否仍在生效

#### Scenario: 效果生命周期
- **WHEN** 效果被应用
- **THEN** Apply() 被调用，IsActive 变为 true
- **WHEN** 效果需要移除
- **THEN** Remove() 被调用，IsActive 变为 false

### Requirement: InstantEffect 子类
InstantEffect 继承 SpecialEffectInstance，实现立即执行效果（消耗品）。

**行为**：
- Apply() 时立即执行效果逻辑，执行后 IsActive 直接为 false
- Remove() 无操作（效果已执行完毕）
- 通过 ItemEffectFactory 创建具体效果（AddGoldEffect、RestoreHPEffect 等）

#### Scenario: 消耗品使用
- **WHEN** 玩家使用消耗品（如金币药水）
- **THEN** InstantEffect.Apply() → ItemEffectFactory.Create("AddGold") → Execute()
- **AND** 效果立即生效，无后续状态需要管理

#### Scenario: 执行后无需移除
- **WHEN** InstantEffect 执行完毕
- **THEN** IsActive = false，SpecialEffectManager 不需要追踪该实例

### Requirement: BuffSpecialEffect 子类
BuffSpecialEffect 继承 SpecialEffectInstance，实现通过 BuffManager 添加 Buff 的效果。

**行为**：
- Apply() 时调用 BuffManager.AddBuff() 添加 BuffIds 中的所有 Buff
- Remove() 时调用 BuffManager.RemoveBuff() 移除对应的 Buff 实例
- 记录添加的 Buff 实例 ID 列表，用于精确移除

#### Scenario: 战斗中应用 Buff
- **WHEN** 战斗先手效果触发（EffectType=Buff）
- **THEN** BuffSpecialEffect.Apply() → 为目标棋子添加 BuffIds 中的所有 Buff
- **AND** Buff 有图标显示、有持续时间、可被驱散

#### Scenario: 羁绊激活 Buff
- **WHEN** 羁绊条件满足
- **THEN** BuffSpecialEffect.Apply() → 为所有参与棋子添加 BuffIds

#### Scenario: 移除 Buff
- **WHEN** 羁绊条件不再满足
- **THEN** BuffSpecialEffect.Remove() → 移除之前添加的所有 Buff 实例

### Requirement: PassiveEffect 子类
PassiveEffect 继承 SpecialEffectInstance，实现被动常驻效果（装备/宝物穿戴期间）。

**行为**：
- Apply() 时直接修改 ChessAttribute（如 Atk += 10, Def += 5）
- Remove() 时反向修改 ChessAttribute（Atk -= 10, Def -= 5）
- 无图标、不可驱散、不参与 Buff 系统的 Tick 遍历
- 记录所有修改的属性和增量值

#### Scenario: 装备穿戴
- **WHEN** 棋子穿戴一把武器（EffectId 对应 Passive 效果，AttributeModifiers={"Atk":15}）
- **THEN** PassiveEffect.Apply() → chess.Attribute.Atk += 15
- **AND** 无 Buff 图标显示，效果立即生效

#### Scenario: 装备卸下
- **WHEN** 棋子卸下该武器
- **THEN** PassiveEffect.Remove() → chess.Attribute.Atk -= 15
- **AND** 属性精确回滚，不影响其他效果

#### Scenario: 多个 Passive 叠加
- **WHEN** 棋子同时穿戴 3 件装备 + 2 件宝物，各有 Passive 效果
- **THEN** 所有效果独立 Apply，各自记录增量值
- **AND** 卸下任一件时只移除该件的效果，其他不受影响

### Requirement: TriggerEffect 子类（预留）
TriggerEffect 继承 SpecialEffectInstance，实现条件触发效果（预留框架）。

**行为**：
- Apply() 时订阅 ChessAttribute 的事件（如 OnDamageTaken）
- Remove() 时取消事件订阅
- 触发时根据 EffectParams 中的条件判断是否执行

#### Scenario: 受击反击（预留示例）
- **WHEN** 装备配置了 Trigger 效果（triggerEvent=OnDamageTaken, chance=0.3）
- **THEN** TriggerEffect.Apply() 订阅 OnDamageTaken 事件
- **WHEN** 棋子受到攻击且概率满足
- **THEN** 执行反击效果

### Requirement: SpecialEffectManager 管理效果生命周期
新增 SpecialEffectManager，负责效果的创建、应用、追踪和移除。

**接口**：
- ApplyEffect(effectId, target, sourceType, sourceId): 创建并应用效果
- RemoveEffectBySource(target, sourceType, sourceId): 按来源移除效果
- GetActiveEffects(target): 获取目标身上的所有活跃效果

**所有权标识**：
- sourceType: Equipment / Treasure / Synergy / Combat / Consumable
- sourceId: 具体物品或羁绊的 ID

#### Scenario: 应用效果
- **WHEN** ApplyEffect(2001, chess, Equipment, equipId) 被调用
- **THEN** 查询 SpecialEffectTable[2001]，根据 EffectType 创建对应子类实例
- **AND** 调用 instance.Apply(chess)，将实例注册到活跃效果列表

#### Scenario: 按来源移除
- **WHEN** RemoveEffectBySource(chess, Equipment, equipId) 被调用
- **THEN** 查找该来源的所有活跃效果实例，逐一调用 Remove()
- **AND** 从活跃列表移除

#### Scenario: 消耗品效果不追踪
- **WHEN** ApplyEffect() 应用 Instant 类型效果
- **THEN** 执行后不注册到活跃列表（无需追踪，无需移除）
