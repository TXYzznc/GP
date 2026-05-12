## MODIFIED Requirements

### Requirement: SpecialEffectTable 新增 EffectType 字段
SpecialEffectTable 保持为统一效果定义表，新增 EffectType 字段以区分效果执行方式。

**更新后字段**：
- Id (int): 效果唯一标识
- Name (string): 效果名称
- EffectType (int): 效果类型 1=Instant, 2=Buff, 3=Passive, 4=Trigger
- BuffIds (int[]): EffectType=Buff 时使用，指向 BuffTable 的 Buff ID 数组
- AttributeModifiers (string): EffectType=Passive 时使用，JSON 格式（如 {"Atk":10,"Def":5}）
- EffectParams (string): 通用 JSON 参数（Instant 效果的具体类型和参数、Trigger 的触发条件等）

#### Scenario: Instant 类型效果（消耗品）
- **WHEN** EffectType=1 且被触发时
- **THEN** 系统立即执行 EffectParams 中定义的操作（如加金币、回血），执行完毕无后续状态

#### Scenario: Buff 类型效果（战斗/羁绊）
- **WHEN** EffectType=2 且被触发时
- **THEN** 系统通过 BuffManager.AddBuff() 将 BuffIds 中的所有 Buff 添加到目标棋子

#### Scenario: Passive 类型效果（装备/宝物常驻）
- **WHEN** EffectType=3 且装备/宝物被穿戴时
- **THEN** 系统直接修改棋子属性（AttributeModifiers），效果无图标、不可驱散、持续到卸下

#### Scenario: Trigger 类型效果（条件触发，预留）
- **WHEN** EffectType=4 且配置了触发条件
- **THEN** 系统订阅对应事件，满足条件时执行效果

### Requirement: EffectParams JSON 格式约定

#### Scenario: Instant 效果参数
- **WHEN** EffectType=1
- **THEN** EffectParams 格式为 `{"type":"AddGold","amount":100}` 或 `{"type":"RestoreHP","value":50}`
- **AND** ItemEffectFactory 根据 type 字段创建具体效果实例

#### Scenario: Passive 效果参数（AttributeModifiers）
- **WHEN** EffectType=3
- **THEN** AttributeModifiers 格式为 `{"Atk":10,"Def":5,"MaxHP":100}`
- **AND** 支持整数值（绝对加成）

#### Scenario: Trigger 效果参数（预留）
- **WHEN** EffectType=4
- **THEN** EffectParams 格式为 `{"triggerEvent":"OnDamageTaken","condition":"chance:0.3","effectType":"Buff","buffIds":[1001]}`

### Requirement: 各物品表 EffectId 统一指向 SpecialEffectTable
ConsumableTable.UseEffectId、EquipmentTable.EffectId、TreasureTable.EffectId、SynergyTable.EffectId 全部指向同一张 SpecialEffectTable。

#### Scenario: 装备效果
- **WHEN** EquipmentTable 某装备配置 EffectId=2001
- **THEN** 查询 SpecialEffectTable[2001]，其 EffectType=3（Passive），应用 AttributeModifiers

#### Scenario: 消耗品效果
- **WHEN** ConsumableTable 某消耗品配置 UseEffectId=1001
- **THEN** 查询 SpecialEffectTable[1001]，其 EffectType=1（Instant），执行 EffectParams

#### Scenario: 宝物效果
- **WHEN** TreasureTable 某宝物配置 EffectId=3001
- **THEN** 查询 SpecialEffectTable[3001]，其 EffectType=3（Passive），应用 AttributeModifiers

#### Scenario: 羁绊效果
- **WHEN** SynergyTable 某羁绊配置 EffectId=4001
- **THEN** 查询 SpecialEffectTable[4001]，其 EffectType=2（Buff），对参与棋子应用 BuffIds

### Requirement: CombatRuleTable 新增效果 ID 字段
CombatRuleTable 新增 InitiativeEffectId 和 SneakAttackEffectId，指向 SpecialEffectTable。

#### Scenario: 先手效果
- **WHEN** 战斗中一方获得先手
- **THEN** 系统读取 CombatRuleTable.InitiativeEffectId，查询 SpecialEffectTable 并应用效果

#### Scenario: 偷袭效果
- **WHEN** 战斗中一方发动偷袭
- **THEN** 系统读取 CombatRuleTable.SneakAttackEffectId，查询 SpecialEffectTable 并应用效果
