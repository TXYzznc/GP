## ADDED Requirements

### Requirement: 战斗准备阶段隐藏敌人实体而非销毁
`CombatPreparationState` 在进入战斗准备时 SHALL 将参战敌人实体设置为 `SetActive(false)` 并停止 NavMeshAgent，而不是销毁其 GameObject。

#### Scenario: 敌人在准备阶段被隐藏
- **WHEN** 玩家触发与敌人的战斗，进入 CombatPreparationState
- **THEN** 对应 EnemyEntity 的 GameObject 被设置为 inactive，NavMeshAgent 停止，但对象未被 Destroy

#### Scenario: 隐藏后 EnemyChessDataManager 数据保留
- **WHEN** EnemyEntity 被设置为 SetActive(false)
- **THEN** 其在 EnemyChessDataManager 中注册的棋子数据不被清除

---

### Requirement: 玩家胜利时销毁敌人实体并清理数据
`EnemyEntityManager.OnCombatEnd(playerWin=true)` SHALL 销毁参战敌人的 GameObject，并调用 `EnemyChessDataManager.RemoveAllForEntity()` 清理其棋子数据。

#### Scenario: 玩家胜利清理敌人
- **WHEN** 战斗结束且 playerWin=true
- **THEN** 参战 EnemyEntity 的 GameObject 被 Destroy，EnemyChessDataManager 中该敌人的所有棋子数据被移除

---

### Requirement: 玩家失败时保留敌人实体
`EnemyEntityManager.OnCombatEnd(playerWin=false)` SHALL 将参战敌人的 GameObject 重新设置为 `SetActive(true)`，并恢复其 AI 到 Idle 状态，回到出生点位置（或最近安全点）。敌人的 `EnemyChessDataManager` 数据 SHALL 保留（含已更新的 HP）。

#### Scenario: 玩家失败后敌人恢复
- **WHEN** 战斗结束且 playerWin=false
- **THEN** 参战 EnemyEntity 的 GameObject 被设置为 active，AI 重置为 Idle，NavMeshAgent 重新启用

#### Scenario: 玩家失败后敌人棋子 HP 数据保留
- **WHEN** 战斗结束且 playerWin=false，敌方某棋子当前 HP=50
- **THEN** 该棋子在 EnemyChessDataManager 中的 CurrentHp=50，下次战斗继承此值

---

### Requirement: 敌方所有棋子死亡时触发敌人失败
当战斗中某个敌人实体对应的所有棋子 HP 均为 0 时，系统 SHALL 触发该敌人失败事件，标记玩家胜利。

#### Scenario: 敌方全部棋子死亡
- **WHEN** 参战敌人的所有 ChessEntity（Camp=1）HP 均降至 0
- **THEN** 触发战斗胜利结算，玩家判定为胜利

#### Scenario: 部分棋子死亡不触发结算
- **WHEN** 参战敌人有 2 个棋子，1 个死亡，另 1 个 HP>0
- **THEN** 战斗继续，不触发结算
