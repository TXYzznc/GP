## MODIFIED Requirements

### Requirement: 棋子状态读写分层
棋子状态管理 SHALL 按生命周期分为三层，各层职责不得越界：

**持久化层 — `GlobalChessManager`**
- 唯一负责跨战斗 HP 的读写（存储 + 恢复）
- 战斗开始时被 `BattleChessManager` 读取
- 战斗结束时被 `BattleChessManager` 写回

**战斗同步层 — `BattleChessManager`**
- 战斗期间从 `GlobalChessManager` 读取初始 HP
- 战斗结束时将最终 HP 写回 `GlobalChessManager`
- SHALL NOT 直接持有跨战斗状态

**实时追踪层 — `CombatEntityTracker`（原 CombatChessManager）**
- 仅负责战场实体的注册/注销/阵营查询
- SHALL NOT 直接读写 HP 数据
- 死亡清理由 `ChessLifecycleHandler` 驱动（通知注销）

#### Scenario: 战斗开始时 HP 读取
- **WHEN** `BattleChessManager.RegisterChessEntity()` 被调用
- **THEN** SHALL 从 `GlobalChessManager.GetChessState()` 读取初始 HP 并应用到实体

#### Scenario: 战斗结束时 HP 写回
- **WHEN** `BattleChessManager.OnBattleEnd()` 被调用
- **THEN** SHALL 将所有注册实体的当前 HP 写回 `GlobalChessManager`

#### Scenario: 实时追踪层不直接操作 HP
- **WHEN** 外部系统调用 `CombatEntityTracker` 的任意方法
- **THEN** `CombatEntityTracker` SHALL NOT 调用任何 HP 读写接口

#### Scenario: 阵营查询
- **WHEN** 调用 `CombatEntityTracker.GetChessOfCamp(int camp)`
- **THEN** SHALL 返回当前存活（已注册未注销）的对应阵营棋子列表

### Requirement: BattleChessManager Buff 委托精简
`BattleChessManager.AddBuff()` 和 `RemoveBuff()` 方法 SHALL 被移除；调用方 SHALL 直接访问 `ChessEntity.BuffManager`。

#### Scenario: 直接通过实体操作 Buff
- **WHEN** 外部系统需要为棋子添加 Buff
- **THEN** SHALL 通过 `chessEntity.BuffManager.AddBuff()` 直接调用，不经过 `BattleChessManager`
