## ADDED Requirements

### Requirement: 仓库 UI 网格布局
`WarehouseUIForm` SHALL 使用 GridLayoutGroup 显示仓库格子，格子数量从 `PlayerInitTable.InitWarehouseCapacity` 读取玩家当前仓库容量。

#### Scenario: 仓库格子初始化
- **WHEN** `WarehouseUIForm` 打开
- **THEN** 显示格子数等于玩家当前仓库容量，已存放物品正确显示

### Requirement: 一键存入仓库
仓库 UI SHALL 提供"一键存入"按钮，点击后将背包中所有非快捷栏物品移入仓库（仓库空间足够时）。

#### Scenario: 一键存入成功
- **WHEN** 玩家点击"一键存入"且仓库有足够空间
- **THEN** 背包中所有物品转移到仓库，背包清空（快捷栏物品不受影响）

#### Scenario: 仓库空间不足
- **WHEN** 玩家点击"一键存入"但仓库空间不足
- **THEN** 能放入的物品放入，剩余物品留在背包，提示"仓库空间不足"

### Requirement: 拖拽存取仓库
玩家 SHALL 能将背包格子内的物品拖拽到仓库格子（存入），或将仓库格子物品拖拽到背包格子（取出）。

#### Scenario: 拖拽存入仓库
- **WHEN** 玩家将背包物品拖拽到仓库空格子
- **THEN** 物品从背包移除，出现在仓库对应格子

#### Scenario: 拖拽从仓库取出
- **WHEN** 玩家将仓库物品拖拽到背包空格子
- **THEN** 物品从仓库移除，出现在背包对应格子
