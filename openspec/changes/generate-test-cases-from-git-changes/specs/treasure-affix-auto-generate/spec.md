## ADDED Requirements

### Requirement: 宝物创建时自动生成词条
ItemManager 在创建 ItemType.Treasure 时，SHALL 调用 AffixGenerator.Generate(itemData.Rarity) 自动生成词条并绑定到 TreasureItem。

#### Scenario: 普通品质宝物词条生成
- **WHEN** 创建一个 Rarity=1（普通）的宝物
- **THEN** TreasureItem.Affixes 不为 null，包含符合普通品质规则的词条数量

#### Scenario: 稀有品质宝物词条多于普通
- **WHEN** 创建 Rarity=3（稀有）的宝物
- **THEN** 词条数量 >= 普通品质生成数量

#### Scenario: 词条属性合法
- **WHEN** 宝物创建后
- **THEN** 每条词条的 AttributeType 在 AffixTable 中有效定义，数值在配置范围内

### Requirement: GetAllAffixData 返回所有词条配置
ItemManager.GetAllAffixData() SHALL 返回从 AffixTable 加载的所有词条数据列表（非空）。

#### Scenario: 正常返回全量词条
- **WHEN** ItemManager 已初始化，调用 GetAllAffixData()
- **THEN** 返回 List<AffixData>，Count > 0

#### Scenario: 未初始化时不抛出异常
- **WHEN** ItemManager 未完成初始化时调用 GetAllAffixData()
- **THEN** 返回空列表，不抛出 NullReferenceException
