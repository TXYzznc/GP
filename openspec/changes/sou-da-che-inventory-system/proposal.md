## Why

游戏目前缺乏完整的背包/仓库交互系统，玩家无法在战斗外管理物品、装备和消耗品。背包系统是召唤师养成与战斗准备的核心入口，需要在进入 Phase 17（物品与装备系统）之前建立完整的 UI 框架与数据基础。

## What Changes

- 新增背包 UI（Tab 快捷键开关，A/D 换页，支持网格拖拽与吸附）
- 新增仓库 UI（支持一键存入与逐个拖拽）
- 新增快捷栏 UI（道具栏，可从背包拖入）
- 新增召唤物装备栏 UI（支持水平滑动，独立面板）
- 物品系统：重量属性、耐久度显示、稀有度背景色、堆叠
- 物品交互：场景中左键拾取、右键使用；背包内左键详情、拖拽移格、右键菜单（使用/拆分/丢弃）
- 一键整理：堆叠→分类→稀有度排序
- 配置表扩展：PlayerDataTable 增加负重相关字段，PlayerInitTable 增加初始仓库容量，新增资源管理规则（背包格数、仓库格数、扩展费用等）
- 玩家技能 UI 迁移到左上角（独立布局）
- 背包打开时锁定玩家移动

## Capabilities

### New Capabilities
- `inventory-ui`: 背包 UI 核心——网格布局、分页、Tab 开关、拖拽吸附、格子数配置
- `warehouse-ui`: 仓库 UI——网格布局、一键存入、拖拽存入
- `item-hotbar-ui`: 快捷栏 UI——道具栏，支持从背包拖入，快捷使用
- `summon-equipment-ui`: 召唤物装备栏 UI——水平滑动、装备格子
- `item-interaction`: 物品交互逻辑——拾取、使用、拆分、丢弃、拖拽、堆叠、一键整理
- `inventory-data-config`: 配置表扩展——PlayerDataTable 负重字段、PlayerInitTable 仓库容量、资源管理规则表

### Modified Capabilities
- `player-summoner-stat-split`: PlayerDataTable 新增 `WeightLimit`（负重上限）、`WeightMoveSpeedEffect`（负重对移速的影响系数）字段；PlayerInitTable 新增 `InitWarehouseCapacity`（初始仓库容量）字段

## Impact

- **UI**：新增 `InventoryUIForm`、`WarehouseUIForm`、`ItemHotbarUIForm`、`SummonEquipmentUIForm`（均需录入 UITable）
- **DataTable**：`PlayerDataTable.xlsx`、`PlayerInitTable.xlsx` 新增字段；新增 `ResourceRuleTable.xlsx`（背包/仓库规则）
- **Entity/Item**：`ItemTable` 需包含重量、耐久度、稀有度、可堆叠字段
- **Player**：移动锁定接口需在背包开启时调用
- **PlayerSkillUI**：布局迁移到左上角，不影响技能逻辑
