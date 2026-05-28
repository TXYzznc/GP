## Why

游戏展示时需要快速调整游戏数据（物品、策略卡、棋子解锁、局内棋子状态），目前只能通过修改存档文件或代码来实现，效率低且不灵活。GM 面板提供一个运行时的开发者控制台，通过快捷键（C 键）随时开关，方便展示演示时实时控制游戏状态。

## What Changes

- 新增 `GMPanelUI` 界面（Tab 分页式 UI），通过 C 键切换显示/隐藏
- 在 `PlayerInputManager` 新增 `GMPanelToggleTriggered` 属性，监听 C 键输入
- 新增 `GMPanelManager` 脚本，处理三个 Tab 的业务逻辑：
  - **物品 Tab**：扫描 ItemTable 展示所有物品，支持搜索过滤，可设定数量后添加到背包
  - **策略卡&棋子 Tab**：展示解锁进度，提供一键解锁所有策略卡/棋子的操作
  - **局内棋子 Tab**：战斗中显示己方所有棋子状态（含死亡），支持单体满血/复活、全体满血
- 打开 GM 面板时解锁鼠标，关闭时恢复

## Capabilities

### New Capabilities

- `gm-panel-ui`: GM 面板 UI 系统，Tab 分页布局，快捷键 C 键切换，覆盖物品管理、解锁管理、局内棋子管理三个功能模块

### Modified Capabilities

- `player-input`: PlayerInputManager 新增 GMPanelToggleTriggered 属性（C 键）

## Impact

- **新增文件**：`GMPanelUI.cs`、`GMPanelManager.cs`、对应 UIVariables 脚本（工具生成）、Prefab（手动搭建）
- **修改文件**：`PlayerInputManager.cs`（新增 C 键输入）、`UIViews.cs`（工具自动生成，新增枚举项）、`UITable`（配置表，手动更新）
- **依赖系统**：InventoryManager、ItemManager、CardTable、ChessDataManager、ChessUnlockManager、PlayerAccountDataManager、CombatEntityTracker、CombatManager
