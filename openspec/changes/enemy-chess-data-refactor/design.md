## Context

当前架构中所有棋子（玩家方和敌人方）的跨战斗 HP 持久化均由 `GlobalChessManager` 统一管理，以 `ChessId` 为 key。这意味着：
- 同一场景中两个使用相同 ChessId 的敌人会互相覆盖彼此的 HP 记录
- 敌人实体在战斗准备阶段被销毁，导致玩家失败后场景中无法恢复敌人
- 战斗结束时 `BattleChessManager.OnBattleEnd()` 对所有棋子统一回写 `GlobalChessManager`，敌我不分

玩家棋子（Camp=0）和敌方棋子（Camp=1）需要完全分离的数据层。

## Goals / Non-Goals

**Goals:**
- 每个 `EnemyEntity` 在初始化时从 EnemyTable 读取 ChessIds，向 `EnemyChessDataManager` 注册独立棋子实例（key = `entityGuid + slotIndex`）
- 同一场景中多个使用相同 ChessId 的敌人，各自的棋子数据完全独立
- 战斗中敌方棋子 HP 回写到 `EnemyChessDataManager`，玩家棋子回写到 `GlobalChessManager`（通过 `Camp` 字段区分）
- 玩家胜利时：销毁敌人实体，清理 `EnemyChessDataManager` 对应数据
- 玩家失败时：保留敌人实体（不销毁），保留其棋子当前 HP 数据
- 战斗准备阶段不销毁敌人实体，改为隐藏/停止 AI

**Non-Goals:**
- 玩家棋子持久化逻辑不变（仍走 GlobalChessManager）
- 不涉及存档系统（EnemyChessDataManager 为内存管理，不持久化到磁盘）
- 不改变棋子战斗内的伤害/技能/AI逻辑

## Decisions

### 决策 1：EnemyChessDataManager 的 Key 设计

使用 `string entityGuid + int slotIndex` 的组合 key（格式：`"{guid}_{slot}"`）。

- **为何不用 `ChessId`**：同一场景中两个敌人可能持有同一 ChessId，不能作为唯一 key
- **为何不用 `instanceId`（ChessEntity.InstanceId）**：棋子实体在战斗准备时才生成，初始化时不存在
- **为何用 entityGuid + slotIndex**：EnemyEntity 唯一标识（GUID 在 Awake 中生成），slotIndex 是该敌人棋子列表中的位置，两者组合全场景唯一

### 决策 2：Camp 字段区分玩家/敌人棋子

`ChessEntity.Camp == 0` → 玩家棋子 → 回写 `GlobalChessManager`
`ChessEntity.Camp == 1` → 敌方棋子 → 回写 `EnemyChessDataManager`

`BattleChessManager` 注册时需同时记录每个 ChessEntity 的 `Camp`，`OnBattleEnd()` 中按 Camp 分发回写。

此外，敌方棋子注册时，还需记录对应的 `enemyGuid + slotIndex`（通过 `EnemyChessDataManager` 查找）以便正确定位写回 key。

### 决策 3：战斗准备阶段敌人实体的处理方式

**改为"隐藏+暂停"而非"销毁"**：
- 设置 `EnemyEntity.gameObject.SetActive(false)` + `NavMeshAgent.isStopped = true`
- 保留 GameObject，战斗结束后按胜负决定是否 `Destroy`

备选方案（销毁后重建）被否决，因为重建需要重新读取配置、重新分配 GUID，会破坏 EnemyChessDataManager 的 key 映射。

### 决策 4：EnemyChessDataManager 的生命周期

纯 C# 单例（同 `GlobalChessManager`），随进程存活，不随场景销毁。在 `EnemyEntity.OnDestroy()` 时自动清理对应数据。

## Risks / Trade-offs

- **风险：GUID 在 Awake 中生成，热重载或反序列化可能产生重复**
  → 使用 `System.Guid.NewGuid().ToString()` 生成，足够唯一；不涉及序列化

- **风险：战斗准备阶段 SetActive(false) 后 NavMesh 状态可能残留**
  → 在 `SetActive(false)` 前先 `NavMeshAgent.isStopped = true`，恢复时重新启用

- **风险：BattleChessManager 回写时需要查找敌方棋子对应的 entityGuid+slot**
  → 注册时在 `BattleChessManager` 内维护一个 `Dictionary<int chessEntityInstanceId, string enemyKey>` 映射

- **Trade-off：EnemyChessDataManager 不持久化，游戏重启后敌人棋子恢复满血**
  → 符合设计预期（敌人棋子不跨进程保存）

## Migration Plan

1. 新增 `EnemyChessState` 和 `EnemyChessDataManager`（纯增量，不影响现有代码）
2. 修改 `EnemyEntity`：Awake 中生成 GUID，Initialize 时注册棋子数据
3. 修改 `BattleChessManager`：注册时记录 Camp 和 enemy key 映射，OnBattleEnd 按 Camp 分支回写
4. 修改 `EnemySpawnManager`：生成敌方棋子时从 EnemyChessDataManager 读取初始 HP
5. 修改 `CombatPreparationState`：销毁敌人改为 SetActive(false)
6. 修改 `EnemyEntityManager.OnCombatEnd()`：玩家胜利时 Destroy + 清理数据，失败时 SetActive(true) 恢复敌人
7. 验证：两次战斗间敌人 HP 保留，胜利后敌人消失

回滚：以上均为新增或局部修改，各步骤独立，可逐步回退。

## Open Questions

- 玩家失败恢复敌人时，是否需要恢复敌人的位置和 AI 状态（当前停在战斗触发点）？建议恢复出生点位置并重置 AI 到 Idle。
- 同一场景中多个敌人同时参与群体战斗时，棋子死亡如何判断"哪个敌人失败"？需要在 BattleChessManager 中建立棋子→敌人的反向映射。（本期暂不实现群体战斗的敌人失败判定，留空）
