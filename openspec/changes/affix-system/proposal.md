## Why

宝物系统缺少词条机制，所有宝物只有固定的基础属性，没有随机性和品质差异体验。需要实现词条系统让宝物具备随机属性词条，词条数量和数值强度由品质决定，增加游戏深度和掉落的随机乐趣。

## What Changes

- 新增 `AffixRuleTable` 配置表，定义每个品质等级的词条生成规则（数量上下限、数值缩放范围）
- 新增 `AffixGenerator` 类，实现基于品质的词条生成算法（加权随机选择 + 数值缩放）
- 修改 `ItemManager.CreateItem()` 流程，创建宝物时自动生成词条
- 更新 `AffixTable` 中冷却缩减词条为百分比类型（5%~40%）
- 词条属性在宝物装备时叠加到棋子 `ChessAttribute`

## Capabilities

### New Capabilities
- `affix-generation`: 词条生成系统，根据宝物品质随机生成词条列表（数量、选择、数值计算）
- `affix-attribute-apply`: 词条属性应用，宝物装备/卸下时词条属性的叠加与移除

### Modified Capabilities
（无现有 spec 需要修改）

## Impact

- 配置表：新增 `AffixRuleTable.xlsx`，修改 `AffixTable.xlsx`（冷却缩减改百分比）
- 代码：`ItemManager`、`TreasureItem`、`ChessAttribute` 需要修改
- 新增：`AffixGenerator.cs`
- 数据流：创建宝物 → 生成词条 → 装备时应用属性 → 卸下时移除属性
