## 1. 配置表分离（PlayerMoveSpeed）

- [x] 1.1 在 `SummonerTable.xlsx` 中新增 `PlayerMoveSpeed`（float）列，初始值与现有 `MoveSpeed` 相同
- [x] 1.2 运行 DataTable Generator，重新生成 `SummonerTable.cs`，确认 `PlayerMoveSpeed` 属性存在
- [x] 1.3 修改 `PlayerRuntimeDataManager.Initialize()`：将 `summonerConfig.MoveSpeed` 改为 `summonerConfig.PlayerMoveSpeed`

## 2. SummonerRuntimeDataManager 战斗初始化

- [x] 2.1 在 `SummonerRuntimeDataManager` 中新增 `InitializeForBattle()` 方法，将 HP/MP 重置为最大值并触发对应事件
- [x] 2.2 确认现有 `Initialize()` 仍从配置读取 MaxHP/MaxMP（不改动），`InitializeForBattle()` 只负责回满

## 3. SummonerCombatProxy

- [x] 3.1 新建 `SummonerCombatProxy.cs`（MonoBehaviour），包含字段：`Camp = 0`、`IsDead`，以及方法 `TakeDamage(float damage)`
- [x] 3.2 `TakeDamage()` 内部调用 `SummonerRuntimeDataManager.Instance` 扣除 HP，HP ≤ 0 时设置 `IsDead = true`
- [x] 3.3 在玩家角色预制体（或 `PlayerCharacterManager.SpawnCharacter()` 中）确保挂载 `SummonerCombatProxy`

## 4. CombatEntityTracker 接入召唤师

- [x] 4.1 在 `CombatEntityTracker` 中新增 `RegisterSummoner(SummonerCombatProxy proxy)` 和 `UnregisterSummoner()` 方法，缓存代理引用
- [x] 4.2 修改 `GetEnemyCache(int myCamp)`：当 myCamp == 1 且召唤师已注册且 `!IsDead` 时，将召唤师作为目标条目追加到返回列表

## 5. CombatManager 接入

- [x] 5.1 在 `CombatManager.StartCombat()` 中：获取玩家角色上的 `SummonerCombatProxy`，调用 `RegisterSummoner()` 和 `SummonerRuntimeDataManager.InitializeForBattle()`
- [x] 5.2 在 `CombatManager.EndCombat()` 中：调用 `CombatEntityTracker.UnregisterSummoner()`
