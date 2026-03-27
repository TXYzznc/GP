## ADDED Requirements

### Requirement: 召唤物装备栏水平滑动显示
`SummonEquipmentUIForm` SHALL 在背包 UI 顶部区域显示召唤物装备栏，支持水平滑动，每个召唤物对应一个装备栏条目。

#### Scenario: 打开背包时召唤物装备栏可见
- **WHEN** 背包 UI 打开
- **THEN** 召唤物装备栏显示在顶部，列出当前玩家拥有的召唤物

#### Scenario: 水平滑动查看更多召唤物
- **WHEN** 召唤物数量超过一屏显示上限，玩家左右滑动
- **THEN** 装备栏水平滚动，显示更多召唤物条目

### Requirement: 召唤物装备槽显示
每个召唤物条目 SHALL 显示其当前装备的道具/装备图标（空槽显示占位图标）。

#### Scenario: 显示已装备物品
- **WHEN** 召唤物有装备的道具
- **THEN** 对应装备槽显示物品图标

#### Scenario: 显示空槽
- **WHEN** 召唤物装备槽无装备
- **THEN** 对应槽位显示空槽占位图标
