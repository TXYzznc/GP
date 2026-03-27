## ADDED Requirements

### Requirement: 玩家移速与召唤师移速使用独立配置字段
`SummonerTable` SHALL 包含独立的 `PlayerMoveSpeed`（float）字段用于玩家探索移速，现有 `MoveSpeed` 字段保留作为召唤师战斗移速。

#### Scenario: 配置表包含 PlayerMoveSpeed 字段
- **WHEN** DataTable Generator 运行后
- **THEN** `SummonerTable` 类包含 `public float PlayerMoveSpeed` 属性，与 `MoveSpeed` 独立存在

### Requirement: 玩家运行时移速从 PlayerMoveSpeed 初始化
`PlayerRuntimeDataManager.Initialize()` SHALL 从 `SummonerTable.PlayerMoveSpeed` 读取初始移速，而非 `MoveSpeed`。

#### Scenario: 初始化时读取正确字段
- **WHEN** `PlayerRuntimeDataManager.Initialize(summonerConfig)` 被调用
- **THEN** `CurrentMoveSpeed` 等于 `summonerConfig.PlayerMoveSpeed`

#### Scenario: 两个移速字段值不同时各自独立
- **WHEN** `SummonerTable` 中 `PlayerMoveSpeed = 5.0` 而 `MoveSpeed = 4.0`
- **THEN** 探索状态下玩家移速为 5.0，召唤师战斗移速为 4.0，互不影响
