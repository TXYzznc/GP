## ADDED Requirements

### Requirement: Tab 快捷键开关背包
系统 SHALL 在按下 Tab 键时打开背包 UIForm，再次按下时关闭；背包打开时玩家移动 SHALL 被锁定，关闭时恢复。

#### Scenario: Tab 打开背包
- **WHEN** 玩家按下 Tab 键且背包当前关闭
- **THEN** `InventoryUIForm` 被 GF.UI 打开，玩家移动被锁定

#### Scenario: Tab 关闭背包
- **WHEN** 玩家按下 Tab 键且背包当前已打开
- **THEN** `InventoryUIForm` 被 GF.UI 关闭，玩家移动恢复

### Requirement: 背包格子网格布局
`InventoryUIForm` SHALL 使用 GridLayoutGroup 显示背包格子，格子数量从 `ResourceRuleTable.InitInventorySlots` 读取，支持多页（每页固定格子数），通过 A/D 键切换页面。

#### Scenario: 初始化格子数量
- **WHEN** `InventoryUIForm` 打开
- **THEN** 显示的格子数等于 `ResourceRuleTable.InitInventorySlots`，超出单页容量时分页显示

#### Scenario: A/D 键换页
- **WHEN** 背包打开且存在多页，玩家按 A 或 D 键
- **THEN** 背包显示上一页或下一页内容，页码指示器更新

### Requirement: 物品拖拽与网格吸附
背包内物品 SHALL 支持鼠标左键拖拽，拖拽时在顶层 Canvas 显示拖拽图标，松开时吸附到最近的目标格子；若目标格子已有物品则交换位置。

#### Scenario: 拖拽到空格子
- **WHEN** 玩家拖拽物品到空格子并松开
- **THEN** 物品移动到目标格子，原格子清空

#### Scenario: 拖拽到有物品的格子
- **WHEN** 玩家拖拽物品到已有物品的格子并松开
- **THEN** 两格子内的物品交换位置

#### Scenario: 拖拽到背包外无效区域
- **WHEN** 玩家拖拽物品到非格子区域并松开
- **THEN** 物品返回原格子，不发生移动

### Requirement: 一键整理背包
背包 SHALL 提供一键整理按钮，触发后按以下顺序整理：① 相同物品堆叠（不超过 MaxStack）；② 按 ItemType 分组；③ 组内按 Rarity 降序排列。

#### Scenario: 触发一键整理
- **WHEN** 玩家点击整理按钮
- **THEN** 背包格子内物品按堆叠→分类→稀有度规则重新排列，UI 刷新显示

### Requirement: 物品详情显示
左键单击背包内物品 SHALL 在详情面板显示：物品名称、图标、稀有度、重量、耐久度、描述及可用操作。

#### Scenario: 左键点击物品
- **WHEN** 玩家左键单击背包中的物品格子
- **THEN** 详情面板显示该物品的完整信息
