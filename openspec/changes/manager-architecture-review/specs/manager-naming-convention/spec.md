## ADDED Requirements

### Requirement: Manager 前缀命名规范
项目中所有 Manager 类的命名 SHALL 遵循以下前缀语义：
- `Combat` 前缀：战斗流程中的核心系统（追踪、部署、生命周期）
- `Battle` 前缀：战斗场景级别的资源/场地（Arena、出战配置）
- `Chess` 前缀：棋子数据/解锁/生成（通用棋子操作，与战斗无关）
- `Global` 前缀：跨场景/跨战斗的持久化状态

#### Scenario: 实体追踪类命名
- **WHEN** 某 Manager 职责是在战斗中按阵营注册/注销/查询实体
- **THEN** 类名 SHALL 使用 `CombatEntityTracker`，不使用 `CombatChessManager`

#### Scenario: 出战资源提供类命名
- **WHEN** 某 Manager 职责是向战斗系统提供可用棋子 ID 列表（只读，不管理流程）
- **THEN** 类名 SHALL 使用 `BattleLoadoutProvider`，不使用 `BattlePreparationManager`

#### Scenario: 部署状态追踪类命名
- **WHEN** 某 Manager 职责是追踪战斗中棋子的已部署/未部署/死亡状态
- **THEN** 类名 SHALL 使用 `ChessDeploymentTracker`，不使用 `CombatChessInventoryManager`

#### Scenario: 帧驱动类命名
- **WHEN** 某 MonoBehaviour 职责是为多个 Singleton Manager 提供 Update Tick
- **THEN** 类名 SHALL 使用 `CombatTickDriver`，不使用 `CombatManagerUpdater`

### Requirement: 重命名后引用完整更新
当 Manager 类名发生变更时，项目内所有引用该类的位置 SHALL 同步更新。

#### Scenario: 编译无错误
- **WHEN** 完成所有重命名操作后
- **THEN** 项目 SHALL 能无编译错误地构建

#### Scenario: 无残留旧类名引用
- **WHEN** 在项目中全局搜索旧类名时
- **THEN** 除注释和文档外，MUST NOT 存在任何旧类名的代码引用
