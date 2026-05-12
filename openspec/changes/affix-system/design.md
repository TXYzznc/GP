## Context

当前宝物系统已有完整的数据结构：`TreasureItem` 包含 `List<AffixEffect> m_Affixes` 字段，`AffixData`/`AffixEffect` 数据类已存在，`AffixTable` 有12条词条模板。缺失的是：词条生成逻辑、品质与词条的关联规则、装备时属性应用。

品质来源：固定在 ItemTable 的 Rarity 字段（后续可扩展为动态roll）。

## Goals / Non-Goals

**Goals:**
- 实现基于品质的词条生成算法
- 新增 AffixRuleTable 配置表驱动生成规则
- 宝物创建时自动生成词条
- 装备/卸下时正确应用/移除词条属性

**Non-Goals:**
- 词条重铸/锁定机制
- 动态品质roll（品质固定，预留接口）
- 词条UI展示优化（使用现有 GetDetailInfo 展示）

## Decisions

### 1. 词条生成时机：创建时一次性生成

**选择**：在 `ItemManager.CreateItem()` 中调用 `AffixGenerator.GenerateAffixes(rarity)`

**理由**：词条随宝物生命周期固定，不会变化。创建时生成最简单，无需额外存储/恢复逻辑。

**备选**：延迟生成（首次装备时）—— 增加复杂度无收益。

### 2. 数值计算公式：线性缩放

**选择**：`最终值 = ValueMin + (ValueMax - ValueMin) × Random(ScaleMin, ScaleMax)`

**理由**：直观、可配置、相邻品质有合理重叠区间。

**备选**：分段区间（每品质独立Min/Max）—— 需要更多配置列，灵活性反而低。

### 3. 词条选择：加权随机，允许重复

**选择**：按 `AffixTable.Weight` 加权随机，同一词条可被选中多次。

**理由**：用户确认允许重复；加权随机已有Weight字段支持。

### 4. AffixGenerator 作为静态工具类

**选择**：`AffixGenerator` 为静态类，无状态，纯函数式。

**理由**：生成逻辑无需实例状态，调用简洁 `AffixGenerator.Generate(rarity)`。

### 5. 属性应用方式：装备时遍历词条叠加

**选择**：`TreasureItem` 装备时遍历 `m_Affixes`，通过 `ChessAttribute` 的现有修改接口叠加属性。

**理由**：复用现有属性系统，无需新增机制。

## Risks / Trade-offs

- **[词条数值平衡]** → 所有数值通过配置表控制，后续可随时调整无需改代码
- **[允许重复可能出现双倍同属性]** → 设计确认允许，体验上增加极品装备的惊喜感
- **[品质固定后续想改动态roll]** → AffixGenerator 入参是 ItemRarity 枚举，改为动态传入即可，无需重构
