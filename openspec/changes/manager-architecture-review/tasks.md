## 1. Manager 重命名（Step 1）

- [x] 1.1 将 `CombatChessManager.cs` 重命名为 `CombatEntityTracker.cs`，类名同步修改
- [x] 1.2 将 `BattlePreparationManager.cs` 重命名为 `BattleLoadoutProvider.cs`，类名同步修改
- [x] 1.3 将 `CombatChessInventoryManager.cs` 重命名为 `ChessDeploymentTracker.cs`，类名同步修改
- [x] 1.4 将 `CombatManagerUpdater.cs` 重命名为 `CombatTickDriver.cs`，类名同步修改
- [x] 1.5 全局搜索并替换所有对旧类名的引用（`CombatChessManager`、`BattlePreparationManager`、`CombatChessInventoryManager`、`CombatManagerUpdater`）
- [ ] 1.6 验证编译通过，无报错

## 2. CombatTrigger 文件夹迁移（Step 2）

- [x] 2.1 在 `Assets/AAAGame/Scripts/Game/Combat/` 下创建 `Trigger/` 文件夹
- [x] 2.2 将 `Explore/Combat/CombatTriggerManager.cs` 移动到 `Combat/Trigger/`
- [x] 2.3 将 `Explore/Combat/CombatTriggerType.cs` 移动到 `Combat/Trigger/`
- [x] 2.4 将 `Explore/Combat/CombatTriggerContext.cs` 移动到 `Combat/Trigger/`
- [x] 2.5 将 `Explore/Combat/CombatOpportunityDetector.cs` 移动到 `Combat/Trigger/`
- [x] 2.6 更新移动后文件的 namespace（如有）及所有引用
- [ ] 2.7 验证编译通过，无报错

## 3. 新增 CombatTriggerEvents，解耦 UI 引用（Step 3）

- [x] 3.1 在 `Combat/Trigger/` 下新建 `CombatTriggerEvents.cs`，定义三个静态事件：`OnEnemyInitiativeTriggered(int)`、`OnSneakAttackTriggered(List<int>)`、`OnCombatContextCleared()`
- [x] 3.2 修改 `CombatTriggerManager`：删除对 `CombatPreparationUI` 的直接引用，改为调用 `CombatTriggerEvents` 事件
- [x] 3.3 修改 `CombatPreparationUI`（或其对应 State）：订阅 `CombatTriggerEvents.OnEnemyInitiativeTriggered` 并实现显示逻辑
- [x] 3.4 修改 `CombatPreparationUI`（或其对应 State）：订阅 `CombatTriggerEvents.OnSneakAttackTriggered` 并实现显示逻辑
- [ ] 3.5 验证战斗触发流程功能等价（偷袭/遭遇/先手三种触发类型正常）

## 4. 移除 BattleChessManager Buff 委托（Step 3 附带）

- [x] 4.1 删除 `BattleChessManager.AddBuffToChess()` 方法
- [x] 4.2 删除 `BattleChessManager.RemoveBuffFromChess()` 方法
- [x] 4.3 找到所有调用这两个方法的位置，改为直接调用 `chessEntity.BuffManager.AddBuff()` / `RemoveBuff()`
- [ ] 4.4 验证编译通过，Buff 功能正常

## 5. 提取 ChessLifecycleHandler（Step 4）

- [x] 5.1 在 `SummonChess/` 下（或 `Combat/Chess/`）新建 `ChessLifecycleHandler.cs`（MonoBehaviour）
- [x] 5.2 实现 `ChessLifecycleHandler`：订阅 `SummonChessManager.OnChessSpawned`，对每个生成的棋子注册 HP 变化监听
- [x] 5.3 实现 HP 归零处理：调用 `CombatEntityTracker.UnregisterChess()`、驱动棋子进入死亡状态、延迟销毁
- [x] 5.4 从 `SummonChessManager` 中移除 HP 监听代码和死亡状态转换代码（含私有 `DestroyChessDelayed` 方法）
- [x] 5.5 确保 `ChessLifecycleHandler` 在 `SummonChessManager` 所在 GameObject 上被挂载（`AddComponent` in `Awake`）
- [ ] 5.6 验证棋子死亡流程功能等价（HP 归零 → 死亡动画 → 从追踪列表移除）

## 6. 棋子状态分层验证

- [x] 6.1 确认 `CombatEntityTracker` 中无任何 HP 读写调用（代码审查）
- [x] 6.2 确认 `GlobalChessManager` 是唯一持有跨战斗 HP 数据的类（代码审查）
- [x] 6.3 确认 `BattleChessManager.OnBattleEnd()` 正确将 HP 写回 `GlobalChessManager`

## 7. 收尾与验证

- [x] 7.1 全局搜索所有旧类名，确认无遗留引用（除注释外）
- [x] 7.2 确认 `Explore/Combat/` 文件夹下无遗留 CombatTrigger 相关文件
- [ ] 7.3 在编辑器中运行游戏，验证探索→战斗触发→战斗准备→战斗结束完整流程正常
