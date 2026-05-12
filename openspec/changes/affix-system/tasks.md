## 1. 配置表准备

- [x] 1.1 创建 AffixRuleTable.txt（字段：Id, Name, AffixCountMin, AffixCountMax, ValueScaleMin, ValueScaleMax），填入5个品质等级的数据
- [x] 1.2 更新 AffixTable.txt 第11行冷却缩减：ValueType改为2(百分比)，ValueMin=5，ValueMax=40，Description改为"冷却缩减+{0}%"
- [x] 1.3 创建 AffixRuleTable.cs DataTable 类（用户后续可用 DataTableGenerator 重新生成）

## 2. 核心逻辑实现

- [x] 2.1 创建 AffixGenerator.cs 静态类（路径：Scripts/Game/Item/Effect/AffixGenerator.cs），实现 Generate(ItemRarity) 方法
- [x] 2.2 实现加权随机选择算法（按Weight从AffixTable所有行中选择，允许重复）
- [x] 2.3 实现数值缩放计算（查AffixRuleTable获取ScaleMin/ScaleMax，应用公式）

## 3. 集成到物品创建流程

- [x] 3.1 修改 ItemManager.CreateItem()，在创建 TreasureItem 后调用 AffixGenerator.Generate(rarity) 并赋值给宝物
- [x] 3.2 TreasureItem 添加 SetAffixes() 方法接收生成的词条列表

## 4. 属性应用与移除

- [x] 4.1 TreasureItem 添加 ApplyAffixesToChess() 方法，遍历 Affixes 将属性叠加到 ChessAttribute（区分 Fixed/Percent 类型）
- [x] 4.2 TreasureItem 添加 RemoveAffixesFromChess() 方法，遍历 Affixes 移除对应属性加成

## 5. 附带修复

- [x] 5.1 扩展 AttributeType 枚举以匹配 AffixTable 实际数据（新增 MaxMP, CritDamage, MagicResist, SpellPower, CooldownReduce）
- [x] 5.2 修复 PassiveEffect.cs 和 ChessEquipmentManager.cs 中的 MagicPower → SpellPower 引用
- [x] 5.3 ItemManager.ParseAttributes() 添加 "MagicPower" → "SpellPower" 兼容映射
- [x] 5.4 ItemManager 添加 GetAllAffixData() 方法供 AffixGenerator 使用
