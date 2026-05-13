## Why

玩家需要一个专业的角色管理界面来查看和管理拥有的棋子，同时需要一个便捷的宝物管理入口来装备宝物到棋子。这是 RPG 游戏的核心功能之一，支持玩家的角色收集、养成和装备优化体验。

## What Changes

- **新增 CharacterBagUI 主界面**：三区域布局（左侧棋子/宝物列表、中间立绘/模型展示、右侧四标签页）
- **新增左侧宝物仓库切换**：左侧新增 TreasureContent（显示仓库和背包中的所有宝物）和 TreasureSwitchBtn（在 ChessContent ↔ TreasureContent 间切换）
- **新增 ChessItemUI_Small 小卡组件**：显示棋子头像、名称、品质等信息（不显示等级，等级是局内属性）
- **新增 TreasureItemUI 宝物卡组件**：显示宝物图标、名称、品质、数量等信息，支持拖拽装备
- **配置表扩展**：
  - SummonChessSkillTable 新增 `DescText` 字段（技能文本描述，补充世界观和故事，≤30字）
  - SummonChessTable 新增 `StoryText` 字段（背景故事数组，每段 100-200 字）
  - SummonChessSkillTable 现有 `Desc` 字段改名为 `EffectText`（技能效果描述）
- **新增宝物装备系统**：支持从 TreasureContent 拖拽宝物到 TreasureUI 的宝物槽位进行装备，交互逻辑参考战斗装备界面
- **新增宝物槽位效果展示**：显示装备宝物的总基础属性、特殊效果和激活羁绊
- **新增棋子属性升级预览**：支持查看 1/2/3A/3B 四个阶级的属性和技能信息

## Capabilities

### New Capabilities
- `character-bag-ui`: 角色管理界面主表单，管理四个标签页的切换、棋子选择和左侧列表切换
- `chess-item-card`: 棋子小卡 UI 组件，显示棋子基本信息和品质标识
- `treasure-item-card`: 宝物卡 UI 组件，显示宝物信息和数量，支持拖拽操作
- `treasure-repository-system`: 宝物仓库系统，显示玩家拥有的所有宝物（仓库+背包联通），支持左侧切换显示
- `chess-skill-display`: 棋子技能展示系统，支持显示被动/普攻/技能1/技能2/大招及其详情
- `treasure-equipment-display`: 宝物装备展示系统，显示装备槽位、基础效果和特殊效果+羁绊
- `treasure-drag-drop-system`: 宝物拖拽装备系统，支持从 TreasureContent 拖拽宝物到棋子装备槽位
- `chess-level-preview`: 棋子阶级升级预览系统，支持查看不同阶级的属性和技能

### Modified Capabilities
- `data-table-system`: 扩展 SummonChessSkillTable（新增 DescText，改名 Desc→EffectText）和 SummonChessTable（新增 StoryText）

## Impact

- **配置表**：SummonChessSkillTable.xlsx、SummonChessTable.xlsx 需要更新并重新生成 DataTable 代码
- **UI 脚本**：新增 CharacterBagUI.cs、ChessItemUI_Small.cs、TreasureItemUI.cs、TreasureDisplayHelper.cs 等
- **UI 预制体**：CharacterBagUI.prefab、ChessItemUI_Small.prefab、TreasureItemUI.prefab 需要完成层级搭建和 Variables 生成
- **系统集成**：涉及 TreasureTable、SpecialEffectTable、SynergyTable 的查询；涉及玩家装备数据的持久化
- **拖拽系统**：新增宝物拖拽装备的交互逻辑（参考 InventoryDragHandler）
- **依赖系统**：涉及 SummonChessTable、SummonChessSkillTable、TreasureTable、SpecialEffectTable、SynergyTable；涉及玩家背包/仓库数据
