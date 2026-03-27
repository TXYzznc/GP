## MODIFIED Requirements

### Requirement: SummonChessManager 仅负责生成与销毁
`SummonChessManager` SHALL 仅负责棋子实例的异步生成（`SpawnChessAsync`）和销毁（`DestroyChess` / `DestroyAllChess`）及缓存查询。
HP 变化监听和死亡状态转换 SHALL NOT 存在于 `SummonChessManager` 中。

#### Scenario: SpawnChessAsync 不注册 HP 监听
- **WHEN** `SummonChessManager.SpawnChessAsync()` 完成生成
- **THEN** SHALL NOT 在方法内直接订阅 `ChessEntity.OnHPChanged`
- **THEN** SHALL 触发 `OnChessSpawned` 事件，由 `ChessLifecycleHandler` 响应

#### Scenario: SummonChessManager 无死亡逻辑
- **WHEN** 检查 `SummonChessManager` 代码
- **THEN** SHALL NOT 包含任何死亡状态（Dead State）的转换调用

### Requirement: ChessLifecycleHandler 负责 HP 监听与死亡驱动
系统 SHALL 包含 `ChessLifecycleHandler` 类（MonoBehaviour），职责为：
- 订阅 `SummonChessManager.OnChessSpawned`，对新生成棋子注册 HP 变化监听
- 当 HP ≤ 0 时通知 `BattleChessManager` 和 `CombatEntityTracker`（注销）
- 驱动棋子进入死亡状态

#### Scenario: 棋子生成后自动注册监听
- **WHEN** `SummonChessManager.OnChessSpawned` 触发
- **THEN** `ChessLifecycleHandler` SHALL 订阅该棋子的 HP 变化事件

#### Scenario: HP 归零时驱动死亡流程
- **WHEN** 某棋子 HP 变化事件触发且新值 ≤ 0
- **THEN** `ChessLifecycleHandler` SHALL 调用 `CombatEntityTracker.UnregisterChess(entity)`
- **THEN** `ChessLifecycleHandler` SHALL 通知 `BattleChessManager` 记录死亡
- **THEN** `ChessLifecycleHandler` SHALL 驱动棋子实体进入死亡状态

#### Scenario: ChessLifecycleHandler 与 SummonChessManager 同生命周期
- **WHEN** `SummonChessManager` 的 GameObject 被初始化
- **THEN** `ChessLifecycleHandler` SHALL 作为同一 GameObject 上的组件存在（`AddComponent` 或挂载）
