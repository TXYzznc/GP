## ADDED Requirements

### Requirement: PlayerDataTable 包含负重相关字段
`PlayerDataTable` SHALL 包含 `WeightLimit`（int，负重上限，单位与物品重量单位一致）和 `WeightMoveSpeedEffect`（float，超出负重上限时移速系数，如 0.7 表示降速 30%）字段。

#### Scenario: 读取负重上限
- **WHEN** `PlayerRuntimeDataManager.Initialize()` 被调用
- **THEN** `CurrentWeightLimit` 等于对应 `PlayerDataTable.WeightLimit`

#### Scenario: 超重时移速降低
- **WHEN** 玩家背包当前总重量超过 `WeightLimit`
- **THEN** 玩家实际移速 = 基础移速 × `WeightMoveSpeedEffect`

### Requirement: PlayerInitTable 包含初始仓库容量字段
`PlayerInitTable` SHALL 包含 `InitWarehouseCapacity`（int，玩家初始仓库格子数）字段，用于新玩家初始化仓库容量。

#### Scenario: 新玩家仓库容量初始化
- **WHEN** 新玩家数据被初始化
- **THEN** 玩家仓库格子数等于 `PlayerInitTable.InitWarehouseCapacity`
