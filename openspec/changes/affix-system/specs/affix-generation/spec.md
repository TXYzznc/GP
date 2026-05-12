## ADDED Requirements

### Requirement: 根据品质生成词条数量
系统 SHALL 根据宝物品质(ItemRarity)查询 AffixRuleTable，在 [AffixCountMin, AffixCountMax] 范围内随机确定词条数量。

#### Scenario: 各品质词条数量范围
- **WHEN** 创建一个品质为 Common(1) 的宝物
- **THEN** 词条数量为 0~1 条

#### Scenario: 传说品质最少3条
- **WHEN** 创建一个品质为 Legendary(5) 的宝物
- **THEN** 词条数量为 3~5 条

### Requirement: 加权随机选择词条
系统 SHALL 从 AffixTable 所有词条中按 Weight 字段加权随机选择，允许同一词条被多次选中。

#### Scenario: 高权重词条更容易被选中
- **WHEN** 词条A的Weight=9，词条B的Weight=7
- **THEN** 词条A被选中的概率高于词条B

#### Scenario: 允许重复选择
- **WHEN** 生成3条词条
- **THEN** 可能出现2条或3条相同的词条ID

### Requirement: 基于品质缩放词条数值
系统 SHALL 使用公式 `最终值 = ValueMin + (ValueMax - ValueMin) × Random(ScaleMin, ScaleMax)` 计算每条词条的实际数值，其中 ScaleMin/ScaleMax 来自 AffixRuleTable。

#### Scenario: 低品质数值偏低
- **WHEN** Common品质宝物获得攻击力词条(ValueMin=20, ValueMax=80)
- **THEN** 实际值在 20~35 范围内（ScaleMin=0, ScaleMax=0.25）

#### Scenario: 高品质数值偏高
- **WHEN** Legendary品质宝物获得攻击力词条(ValueMin=20, ValueMax=80)
- **THEN** 实际值在 65~80 范围内（ScaleMin=0.75, ScaleMax=1.0）

### Requirement: 宝物创建时自动生成词条
系统 SHALL 在 ItemManager.CreateItem() 创建 TreasureItem 时，自动调用词条生成逻辑，将生成的 AffixEffect 列表赋给宝物。

#### Scenario: 创建宝物即带词条
- **WHEN** 调用 ItemManager.CreateItem(itemId) 且该物品类型为 Treasure
- **THEN** 返回的 TreasureItem.Affixes 包含根据其品质生成的词条列表
