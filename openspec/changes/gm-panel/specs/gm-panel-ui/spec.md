## ADDED Requirements

### Requirement: GM 面板开关
GM 面板 SHALL 通过 C 键切换显示/隐藏状态，打开时解锁鼠标，关闭时恢复鼠标状态（通过 PlayerInputManager.RequestMouseUnlock/RequestMouseLock）。

#### Scenario: C 键打开 GM 面板
- **WHEN** 用户按下 C 键且 GM 面板当前未打开
- **THEN** GM 面板 UI 显示，鼠标解锁，游戏输入暂停响应

#### Scenario: C 键关闭 GM 面板
- **WHEN** 用户按下 C 键且 GM 面板当前已打开
- **THEN** GM 面板 UI 隐藏，鼠标状态恢复

### Requirement: Tab 分页布局
GM 面板 SHALL 包含三个 Tab 页签：物品管理、解锁管理、局内棋子管理，点击页签切换对应内容区域。

#### Scenario: 切换 Tab
- **WHEN** 用户点击某个 Tab 按钮
- **THEN** 对应内容面板显示，其他面板隐藏

### Requirement: 物品管理 Tab
物品管理 Tab SHALL 在打开时自动扫描 ItemTable 所有行并渲染列表，每行显示物品名称、类型、品质，并提供添加按钮。顶部提供搜索框（按名称过滤）和数量输入框（影响所有添加操作）。

#### Scenario: 显示物品列表
- **WHEN** 用户切换到物品管理 Tab
- **THEN** 从 ItemTable.GetAllDataRows() 读取所有物品并渲染到 ScrollView 列表中

#### Scenario: 搜索过滤
- **WHEN** 用户在搜索框输入文字
- **THEN** 列表只显示名称包含该文字的物品

#### Scenario: 添加物品
- **WHEN** 用户点击某物品的添加按钮
- **THEN** 调用 InventoryManager.Instance.AddItem(itemId, count)，count 取自数量输入框

### Requirement: 解锁管理 Tab
解锁管理 Tab SHALL 显示策略卡已解锁数/总数、棋子已解锁数/总数，并分别提供一键解锁全部按钮。

#### Scenario: 显示解锁进度
- **WHEN** 用户切换到解锁管理 Tab
- **THEN** 从 PlayerAccountDataManager.CurrentSaveData 和 CardTable/ChessDataManager 计算并显示当前解锁进度

#### Scenario: 一键解锁全部策略卡
- **WHEN** 用户点击"一键解锁全部策略卡"按钮
- **THEN** 遍历 CardTable 所有行，将未解锁的 ID 添加到 saveData.OwnedStrategyCardIds，调用 SaveCurrentSave()，刷新显示进度

#### Scenario: 一键解锁全部棋子
- **WHEN** 用户点击"一键解锁全部棋子"按钮
- **THEN** 遍历 ChessDataManager.GetAllConfigIds()，对每个 ID 调用 ChessUnlockManager.Instance.UnlockChess()，刷新显示进度

### Requirement: 局内棋子管理 Tab
局内棋子管理 Tab SHALL 在非战斗状态下显示提示（"仅战斗中有效"），战斗中实时显示己方所有棋子（含死亡），每个棋子显示名称、HP 条、死亡状态，并提供"满血"和"复活"按钮；底部提供"全体满血"按钮。

#### Scenario: 非战斗状态提示
- **WHEN** 用户切换到局内棋子 Tab 且 CombatManager.Instance.IsInCombat 为 false
- **THEN** 显示"仅战斗中有效"提示文本，隐藏棋子列表

#### Scenario: 战斗中显示棋子列表
- **WHEN** 用户切换到局内棋子 Tab 且处于战斗中
- **THEN** 调用 CombatEntityTracker.Instance.GetAlliesIncludingDead(CampType.Player) 获取己方棋子并渲染列表

#### Scenario: 单体满血
- **WHEN** 用户点击某棋子的"满血"按钮
- **THEN** 调用 chess.Attribute.SetHp(chess.Attribute.MaxHp)

#### Scenario: 复活棋子
- **WHEN** 用户点击某死亡棋子的"复活"按钮
- **THEN** 依次执行：SetHp(MaxHp)、ChangeState(ChessState.Idle)、CombatEntityTracker.ReviveChess()、ChessDeploymentTracker.MarkChessAlive()、重新启用 Collider

#### Scenario: 全体满血
- **WHEN** 用户点击"全体满血"按钮
- **THEN** 对所有己方存活棋子调用 SetHp(MaxHp)
