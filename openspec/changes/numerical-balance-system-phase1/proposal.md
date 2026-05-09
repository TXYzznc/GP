## Why

当前数值系统存在严重的不平衡：棋子属性分布混乱（嫦娥一阶30生命、三阶580生命，倍数达19.3倍），升阶经验过低（击败一个三阶棋子就能升级），导致游戏缺乏成就感和进度感。需要建立统一的数值标准模型，确保不同品质、不同阶级的棋子战斗力呈线性合理增长。

## What Changes

- **SummonChessTable** - 规范化所有棋子的三维属性值，按照线性倍数（1.0x → 1.6x → 2.4x）和品质点数（2.4/2.7/3.0/3.3）标准化
- **ChessAdvanceTable** - 升阶经验从 [10, 20] 调整为 [85, 150]，确保需要多场战斗才能完成进阶
- **创建宝物系统** - 新建 TreasureBoxTable 扩展，实现御魂风格的永久装备系统，包含品质分级、强化升级、副属性机制、套装效果
- **创建战斗装备系统** - 扩展 EquipmentTable，实现金铲铲风格的局内一次性装备掉落、合成、卸下机制
- **创建宝物副属性系统** - 新建 AffixTable 扩展或独立表，定义副属性类型、掉率、强化权重

## Capabilities

### New Capabilities
- `chess-quality-grading`: 棋子品质分级系统，4档品质（蓝/紫/金/炫彩）对应属性点数 2.4/2.7/3.0/3.3，覆盖所有棋子的基础属性规范化
- `chess-advancement-system`: 棋子进阶系统升级，一阶→二阶→三阶 3 阶制度，线性属性倍数 1.0x/1.6x/2.4x，升阶经验规范化
- `treasure-system`: 宝物（御魂）系统，在 ItemTable 中新增 Type=3 宝物条目，棋子可装备 2 件宝物，包含 5 档品质、副属性、套装效果，提供永久属性提升
- `battle-equipment-system`: 战斗装备系统，在 ItemTable 中新增 Type=4 装备条目，局内掉落一次性装备，棋子可装备最多 3 件，白/蓝/金品质，战斗结束消失

### Modified Capabilities
- `chess-attributes`: 棋子属性规范化，修改现有棋子属性值使其符合线性倍数增长，确保品质间差异清晰（约 2.74 倍）

## Impact

**DataTable 修改**：
- SummonChessTable - 修改所有棋子的 MaxHp、AtkDamage、Armor、MagicResist、SpellPower 等属性值
- ChessAdvanceTable - 修改升阶经验值
- ItemTable - 在 Type=3（宝物）和 Type=4（装备）中新增具体物品条目，配置 AffixPoolIds、BaseAttributes 等
- AffixTable - 新增宝物和装备的副属性词条
- BuffTable - 新增 5 个宝物套装效果 Buff

**代码影响**：
- 棋子属性初始化逻辑可能需调整（若当前存在缓存或预计算）
- 宝物和装备系统逻辑实现延后到 Phase 2/3（Phase 1 仅做配置表设计）
- UI 支持延后到系统逻辑实现时

**测试影响**：
- 需要通过战斗力公式 FC = HP × DPS 验证各品质、各阶级的平衡性
- 宝物和装备系统的平衡测试延后到 Phase 2/3
