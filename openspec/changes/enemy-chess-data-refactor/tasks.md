## 1. 新增数据层（无依赖，纯增量）

- [x] 1.1 新建 `EnemyChessState.cs`（`Assets/AAAGame/Scripts/Game/Explore/Enemy/Core/`）：字段 ChessId/CurrentHp/MaxHp，属性 IsDead
- [x] 1.2 新建 `EnemyChessDataManager.cs`（同目录）：纯 C# 单例，`Dictionary<string, EnemyChessState>` 存储，提供 `Register(guid, slot, chessId, maxHp)`、`GetState(key)`、`UpdateHp(key, hp)`、`RemoveAllForEntity(guid)` 方法

## 2. EnemyEntity 集成

- [x] 2.1 `EnemyEntity.Awake()`：生成 `EntityGuid = System.Guid.NewGuid().ToString()`，暴露为公共属性
- [x] 2.2 `EnemyEntity.Initialize()`：读取 EnemyTable 中 BattleConfigId 对应的 ChessIds，遍历按槽位向 `EnemyChessDataManager` 注册（首次注册时满血，已存在则跳过）
- [x] 2.3 `EnemyEntity.OnDestroy()`：调用 `EnemyChessDataManager.RemoveAllForEntity(EntityGuid)`（仅在玩家胜利时 Destroy，失败时不 Destroy 故不触发）

## 3. BattleChessManager 回写重构

- [x] 3.1 `BattleChessManager` 新增字段：`Dictionary<int instanceId, (int camp, string enemyKey)> m_ChessMetaDict`
- [x] 3.2 `RegisterChessEntity()` 时记录 camp 和 enemyKey（Camp=1 时通过 `EnemyChessDataManager` 反查 entityGuid+slot，或由调用方传入）
- [x] 3.3 `OnBattleEnd()` 按 camp 分支：Camp=0 → `GlobalChessManager.UpdateChessHP()`；Camp=1 → `EnemyChessDataManager.UpdateHp(enemyKey, hp)`
- [x] 3.4 死亡棋子（entity==null）同样按 camp 分支处理，从 `BattleChessData` 缓存取 HP

## 4. EnemySpawnManager 读取历史 HP

- [x] 4.1 `EnemySpawnManager` 在生成敌方棋子（`SummonChessManager.SpawnChessAsync`）后，从 `EnemyChessDataManager` 读取对应 key 的 CurrentHp，调用 `entity.Attribute.SetHp()` 覆盖满血值
- [x] 4.2 需要建立「棋子槽位索引 → enemyKey」的映射，供 BattleChessManager 回写时使用（可由 EnemySpawnManager 在生成后调用 `BattleChessManager.SetEnemyKeyForChess(instanceId, key)` 传入）

## 5. CombatPreparationState：隐藏而非销毁敌人

- [x] 5.1 找到销毁敌人实体的代码（`CombatPreparationState.cs` line 221-222 及 `EnemyEntityManager.DestroyEntity()`），改为 `entity.gameObject.SetActive(false)` + 停止 NavMeshAgent
- [x] 5.2 `EnemyEntityManager` 新增 `HideEntityForCombat(EnemyEntity)` 和 `RestoreEntityAfterCombat(EnemyEntity)` 方法封装此逻辑

## 6. EnemyEntityManager.OnCombatEnd() 胜负分支

- [x] 6.1 `playerWin=true`：调用 `Destroy(enemy.gameObject)` 销毁敌人（原有逻辑，注意现在 OnDestroy 会触发 EnemyChessDataManager 清理）
- [x] 6.2 `playerWin=false`：调用 `RestoreEntityAfterCombat()`，SetActive(true)，重置 AI 到 Idle，恢复 NavMeshAgent，EnemyChessDataManager 数据保留（HP 已由 OnBattleEnd 更新）

## 7. 验证任务

- [ ] 7.1 编译通过，无报错
- [ ] 7.2 第一次战斗：敌人棋子初始化为满血，战斗结束（玩家失败）后敌人保留
- [ ] 7.3 第二次战斗：敌人棋子 HP 继承上次战斗结果（非满血）
- [ ] 7.4 玩家胜利后：场景中敌人消失，EnemyChessDataManager 中对应数据清除
- [ ] 7.5 同一场景两个相同类型敌人：各自棋子数据独立，互不干扰
- [ ] 7.6 玩家棋子行为不变：仍通过 GlobalChessManager 持久化，与敌人棋子无交叉
