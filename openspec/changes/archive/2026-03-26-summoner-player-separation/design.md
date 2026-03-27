## Context

当前系统状态：
- `PlayerRuntimeDataManager`：管理玩家探索状态的运行时数据（污染值、移速），移速目前从 `SummonerTable.MoveSpeed` 读取——与召唤师移速共享同一字段
- `SummonerRuntimeDataManager`：管理召唤师战斗中的 HP/MP，`Initialize()` 已会回满值，但缺少在 `CombatManager.StartCombat()` 中的显式调用入口
- 召唤师没有战斗实体身份：`CombatEntityTracker` 只注册 `ChessEntity`，敌人 AI 的目标池不包含召唤师
- `ChessEntity.Camp`：`int`，0=玩家方，1=敌方；阵营关系通过 `CampRelationService.GetRelation()` 判断

本次变更涉及两个独立子问题：
1. **数据分离**：玩家移速字段解耦（低风险配置改动）
2. **召唤师战斗实体化**：让召唤师能被敌人选为目标（中等风险，需接入 CombatEntityTracker）

## Goals / Non-Goals

**Goals:**
- `SummonerTable` 中玩家移速与召唤师移速使用独立字段
- 每次战斗开始时 `SummonerRuntimeDataManager` 自动回满 HP/MP
- 召唤师能作为合法攻击目标被敌人 AI 选中
- 最小化对现有 `ChessEntity` / `CombatEntityTracker` 的改动

**Non-Goals:**
- 召唤师的战斗行为（攻击、技能释放）——本次仅实现"可被攻击"占位
- Buff 系统新增 `BuffTarget` 枚举——作用于召唤师的 Buff 与棋子逻辑相同，单独在 Buff 脚本中特殊处理
- 召唤师死亡后的游戏结束逻辑
- 移速 Buff 的具体应用（数据分离后，Buff 层各自调用对应 Manager 即可）

## Decisions

### 1. 配置表：新增 `PlayerMoveSpeed` 字段

在 `SummonerTable.xlsx` 中新增 `PlayerMoveSpeed`（float）列，保留现有 `MoveSpeed` 作为召唤师战斗移速。

`PlayerRuntimeDataManager.Initialize()` 改读 `summonerConfig.PlayerMoveSpeed`。

> **为什么不复用 MoveSpeed：** 两者语义不同，Buff 需要分别作用，共用一个字段会导致修改一方影响另一方。

### 2. 召唤师战斗实体：`SummonerCombatProxy`（轻量代理）

不让召唤师继承 `ChessEntity`（语义错误，侵入性强），而是新建一个轻量 `SummonerCombatProxy : MonoBehaviour`：

```
SummonerCombatProxy
  - Camp: int = 0（玩家方）
  - CurrentHP / MaxHP（转发至 SummonerRuntimeDataManager）
  - TakeDamage(float damage)
  - IsDead: bool
```

挂载在玩家角色 GameObject 上（与角色共生命周期）。

`CombatEntityTracker` 新增：
- `RegisterSummoner(SummonerCombatProxy proxy)` / `UnregisterSummoner()`
- `GetEnemyCache(myCamp)` 中，当 myCamp=1（敌方）时，将存活的 `SummonerCombatProxy` 加入返回列表

> **为什么不扩展 ChessEntity：** ChessEntity 绑定了棋子部署、死亡标记、`ChessDeploymentTracker` 等逻辑，召唤师不适用这些系统。用代理对象可以最小化接口改动。

> **备选方案：** 让 `CombatEntityTracker` 持有一个独立的 `ISummonerTarget` 接口列表——过度设计，当前只有一个召唤师。

### 3. 战斗开始时召唤师 HP 初始化

`SummonerRuntimeDataManager` 新增 `InitializeForBattle()` 方法（将 HP/MP 回满），在 `CombatManager.StartCombat()` 中调用，替代现有无参 `Initialize()` 的散落调用。

战斗结束时不回写（与玩家棋子不同，召唤师没有跨场状态）。

### 4. Buff 作用于召唤师

作用于召唤师的 Buff 效果（移速、HP）直接在各 Buff 脚本中调用 `SummonerRuntimeDataManager` / `PlayerRuntimeDataManager` 的对应方法，不引入 `BuffTarget` 枚举。

## Risks / Trade-offs

- **[风险] `PlayerMoveSpeed` 配置表 BREAKING 变更** → 需要同时更新 `SummonerTable.xlsx` 并重新运行 DataTable Generator，否则编译报错。迁移步骤见下方。
- **[风险] `GetEnemyCache` 修改影响现有目标选择** → 召唤师 HP 为 0 时必须从列表中排除，否则死亡后仍会被选为目标。`SummonerCombatProxy.IsDead` 作为过滤条件。
- **[取舍] 召唤师无攻击行为** → 本次只实现"可被攻击"，召唤师不会主动反击。后续扩展召唤师战斗行为时需在 `SummonerCombatProxy` 上挂载技能系统。

## Migration Plan

1. `SummonerTable.xlsx` 新增 `PlayerMoveSpeed` 列，填写与 `MoveSpeed` 相同的初始值
2. 运行 DataTable Generator 重新生成 `SummonerTable.cs`
3. 修改 `PlayerRuntimeDataManager.Initialize()` 改读 `PlayerMoveSpeed`
4. 新增 `SummonerCombatProxy.cs` 并在玩家角色预制体上挂载
5. 修改 `SummonerRuntimeDataManager`：新增 `InitializeForBattle()`
6. 修改 `CombatEntityTracker`：新增注册/注销召唤师方法，更新 `GetEnemyCache`
7. 修改 `CombatManager.StartCombat()`：调用 `RegisterSummoner` 和 `InitializeForBattle`
8. 修改 `CombatManager.EndCombat()`：调用 `UnregisterSummoner`

## Open Questions

- 召唤师死亡是否触发战斗失败？（本次先不实现，TakeDamage 只扣 HP，到 0 后标记 IsDead）
- 玩家角色预制体上是否已有合适的挂载点，还是需要在 `PlayerCharacterManager.SpawnCharacter` 时动态 AddComponent？
