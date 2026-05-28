## 1. PlayerInputManager 修改

- [x] 1.1 在 PlayerInputManager.cs 新增 `GMPanelToggleTriggered` 公共属性
- [x] 1.2 在 Update() 中添加 C 键检测：`GMPanelToggleTriggered = Input.GetKeyDown(KeyCode.C)`

## 2. GMPanelManager 核心脚本

- [x] 2.1 创建 `Assets/AAAGame/Scripts/UI/GMPanelManager.cs`，继承 MonoBehaviour，实现单例
- [x] 2.2 实现 IMGUI OnGUI 窗口框架：固定尺寸窗口、标题栏、三个 Tab 按钮切换
- [x] 2.3 在 Start/OnEnable 中订阅输入，在 Update 中检测 `PlayerInputManager.Instance.GMPanelToggleTriggered`，切换面板开关
- [x] 2.4 面板开关时调用 `PlayerInputManager.Instance.RequestMouseUnlock()` / `RequestMouseLock()`

## 3. 物品管理 Tab

- [x] 3.1 实现物品列表数据加载：从 `GF.DataTable.GetDataTable<ItemTable>().GetAllDataRows()` 读取并缓存
- [x] 3.2 实现搜索过滤逻辑（按 Name 字段过滤缓存列表）
- [x] 3.3 实现数量输入框（int 字段，默认值 1）
- [x] 3.4 实现 ScrollView 滚动列表，每行显示物品名称、类型（ItemType 枚举）、品质（ItemRarity 枚举）、添加按钮
- [x] 3.5 添加按钮点击调用 `InventoryManager.Instance.AddItem(itemId, count)`

## 4. 解锁管理 Tab

- [x] 4.1 实现解锁进度显示：从 `PlayerAccountDataManager.Instance.CurrentSaveData` 读取 `OwnedStrategyCardIds` / `OwnedUnitCardIds` 计数
- [x] 4.2 实现一键解锁全部策略卡：遍历 `CardTable.GetAllDataRows()`，将未解锁 ID 添加到 `saveData.OwnedStrategyCardIds`，调用 `SaveCurrentSave()`
- [x] 4.3 实现一键解锁全部棋子：遍历 `ChessDataManager.Instance.GetAllConfigIds()`，调用 `ChessUnlockManager.Instance.UnlockChess(id)`
- [x] 4.4 解锁操作后刷新显示进度

## 5. 局内棋子管理 Tab

- [x] 5.1 实现状态判断：`CombatManager.Instance == null || !CombatManager.Instance.IsInCombat` 时显示提示文本
- [x] 5.2 战斗中调用 `CombatEntityTracker.Instance.GetAlliesIncludingDead((int)CampType.Player)` 获取棋子列表并渲染
- [x] 5.3 每行显示棋子名称（`chess.Config.Name`）、HP 进度条（CurrentHp/MaxHp）、死亡标记
- [x] 5.4 实现"满血"按钮：调用 `chess.Attribute.SetHp(chess.Attribute.MaxHp)`
- [x] 5.5 实现"复活"按钮（仅死亡棋子显示）：按顺序执行①重新启用 Collider ②SetHp(MaxHp) ③ChangeState(Idle) ④CombatEntityTracker.ReviveChess() ⑤ChessDeploymentTracker.MarkChessAlive()
- [x] 5.6 实现"全体满血"按钮：对 GetAllies 返回的存活棋子全部调用 SetHp(MaxHp)

## 6. 初始化挂载

- [x] 6.1 找到合适的启动入口（GameProcedure 或场景中已存在的管理器），在游戏启动后自动创建 GMPanelManager GameObject（DontDestroyOnLoad）
