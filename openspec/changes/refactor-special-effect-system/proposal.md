## Why

当前 SpecialEffectTable 承担了过多职责（消耗品即时效果、装备/宝物被动效果、战斗Buff效果、羁绊效果），且效果执行层缺乏类型抽象——所有效果都被当作"添加Buff"处理。实际上特殊效果（SpecialEffect）比 Buff 层级更高：有些效果是添加 Buff，有些是即时执行（加金币），有些是被动常驻（装备在身上就生效，无图标、不可驱散），有些是条件触发。

本次重构的核心目标：
1. **保留 SpecialEffectTable 作为统一效果定义表**，通过 EffectType 字段区分不同效果类型
2. **引入 SpecialEffectInstance 类层次结构**，按 EffectType 分派执行逻辑
3. **各专属表（Consumable/Equipment/Treasure/Synergy）的 EffectId 统一指向 SpecialEffectTable**
4. **战斗先手/偷袭效果独立配置**，通过 CombatRuleTable 的 EffectId 字段引用

## What Changes

- **SpecialEffectTable 新增 EffectType 字段**：区分 Instant(1)、Buff(2)、Passive(3)、Trigger(4) 四种类型
- **新增 SpecialEffectInstance 抽象类 + 4 个子类**：InstantEffect、BuffSpecialEffect、PassiveEffect、TriggerEffect
- **新增 SpecialEffectManager**：统一管理效果的创建、应用、移除生命周期
- **ConsumableTable / EquipmentTable / TreasureTable** 的 EffectId 统一指向 SpecialEffectTable
- **SynergyTable.EffectId** 指向 SpecialEffectTable
- **CombatRuleTable 新增 InitiativeEffectId / SneakAttackEffectId** 指向 SpecialEffectTable

## Capabilities

### New Capabilities
- `effect-type-system`：SpecialEffectTable 通过 EffectType 区分四种效果类型，支持灵活配置
- `effect-instance-hierarchy`：SpecialEffectInstance 类层次结构，统一效果生命周期管理
- `passive-effect`：被动效果（装备/宝物在身上时永久生效，无图标、不可驱散、自动移除）
- `trigger-effect`：条件触发效果（预留，基于 ChessAttribute 事件系统）

### Modified Capabilities
- `special-effect-execution`：GameEffectService 改为通过 SpecialEffectManager 分派执行
- `item-effect-binding`：各物品专属表统一通过 EffectId → SpecialEffectTable 获取效果定义

## Impact

- **配置层**：SpecialEffectTable 新增 EffectType 列；CombatRuleTable 新增 2 个字段
- **代码变更**：
  - 新增 SpecialEffectInstance 类层次（4 个子类）
  - 新增 SpecialEffectManager（效果生命周期管理）
  - 修改 GameEffectService（调用 SpecialEffectManager 而非直接操作 Buff）
  - ChessEquipmentManager：穿戴/卸下时通过 SpecialEffectManager 应用/移除被动效果
  - SynergyManager：羁绊激活/解除时通过 SpecialEffectManager 应用/移除效果
- **向后兼容**：现有 BuffTable 和 BuffManager 完全不变；ItemEffectFactory 保持兼容
