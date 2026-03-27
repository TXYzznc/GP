## ADDED Requirements

### Requirement: ResourceRuleTable 配置背包仓库规则
新增 `ResourceRuleTable` DataTable，SHALL 包含以下字段：`InitInventorySlots`（int，初始背包格子数）、`MaxExtendSlots`（int，最大可扩展格子数）、`ExtendBaseCost`（int，扩展基础金币费用）、`ExtendSlotsPerUpgrade`（int，每次扩展增加格子数）、`InitWarehouseSlots`（int，初始仓库格子数）。

#### Scenario: 读取初始背包格子数
- **WHEN** `InventoryManager` 初始化
- **THEN** 背包容量等于 `ResourceRuleTable.InitInventorySlots`

#### Scenario: 读取仓库初始容量
- **WHEN** `WarehouseManager` 初始化
- **THEN** 仓库容量等于 `ResourceRuleTable.InitWarehouseSlots`

### Requirement: InventoryManager 运行时物品数据管理
新增 `InventoryManager` SHALL 维护运行时背包与仓库的物品列表（`List<InventoryItem>`），并在物品增减时触发负重计算与 UI 刷新事件。

#### Scenario: 添加物品触发事件
- **WHEN** `InventoryManager.AddItem()` 被调用
- **THEN** `OnInventoryChanged` 事件触发，UI 刷新显示新物品，当前负重重新计算

#### Scenario: 移除物品触发事件
- **WHEN** `InventoryManager.RemoveItem()` 被调用
- **THEN** `OnInventoryChanged` 事件触发，UI 刷新，当前负重重新计算
