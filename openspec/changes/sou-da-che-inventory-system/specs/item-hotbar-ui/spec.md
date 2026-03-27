## ADDED Requirements

### Requirement: 快捷栏常驻显示
`ItemHotbarUIForm` SHALL 在游戏主场景中常驻显示于屏幕底部，显示固定数量的道具槽位（槽位数从 `ResourceRuleTable` 读取或固定为 5）。

#### Scenario: 进入场景时快捷栏可见
- **WHEN** 玩家进入主游戏场景
- **THEN** 快捷栏 UI 显示在屏幕底部，道具槽位可见

### Requirement: 从背包拖入快捷栏
玩家 SHALL 能将背包中的物品拖拽到快捷栏槽位，物品同时保留在背包格子中（快捷栏为引用，非移动）。

#### Scenario: 拖拽物品到快捷栏
- **WHEN** 玩家将背包物品拖拽到快捷栏空槽位
- **THEN** 快捷栏槽位显示该物品图标，背包中物品仍存在

### Requirement: 快捷使用快捷栏物品
玩家 SHALL 能通过数字键（1-5）快速使用对应快捷栏槽位的道具（若物品可使用）。

#### Scenario: 数字键快捷使用
- **WHEN** 玩家按数字键 N（1≤N≤5）且对应槽位有可使用物品
- **THEN** 物品使用效果触发，物品数量减少 1（耗尽时清除图标）
