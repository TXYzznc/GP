## ADDED Requirements

### Requirement: EnemyChessState 数据对象
系统 SHALL 提供 `EnemyChessState` 类，表示一个敌人棋子实例的持久化数据，包含字段：`ChessId`（int）、`CurrentHp`（double）、`MaxHp`（double）。`IsDead` 属性 SHALL 返回 `CurrentHp <= 0`。

#### Scenario: 创建 EnemyChessState
- **WHEN** 传入 chessId=1, maxHp=100
- **THEN** `CurrentHp == 100`，`MaxHp == 100`，`IsDead == false`

#### Scenario: HP 归零后 IsDead
- **WHEN** `CurrentHp` 被设置为 0
- **THEN** `IsDead == true`

---

### Requirement: EnemyChessDataManager 全局管理独立棋子实例
系统 SHALL 提供 `EnemyChessDataManager` 纯 C# 单例，以字符串 key（格式 `"{entityGuid}_{slotIndex}"`）为索引管理所有敌人棋子状态，与 `GlobalChessManager` 完全独立。

#### Scenario: 注册敌人棋子
- **WHEN** 以 guid="abc", slotIndex=0, chessId=1, maxHp=100 注册
- **THEN** `GetState("abc_0")` 返回 CurrentHp=100 的 EnemyChessState

#### Scenario: 同 ChessId 不同 entityGuid 互相独立
- **WHEN** 两个 entityGuid 不同的敌人均注册 chessId=1
- **THEN** 修改其中一个的 HP 不影响另一个

#### Scenario: 清理指定敌人的所有棋子
- **WHEN** 调用 `RemoveAllForEntity("abc")`
- **THEN** 所有 key 前缀为 `"abc_"` 的数据被移除

---

### Requirement: EnemyEntity 初始化时注册棋子数据
`EnemyEntity` SHALL 在 `Initialize()` 时从 EnemyTable 读取该敌人的 `ChessIds`，并为每个 ChessId 按槽位顺序向 `EnemyChessDataManager` 注册一条 `EnemyChessState`（使用自身唯一 GUID + slotIndex 为 key）。GUID SHALL 在 `Awake()` 中生成。

#### Scenario: 初始化注册棋子
- **WHEN** EnemyEntity（EntityConfigId=1001）的 BattleConfigId 对应 EnemyTable 中 ChessIds=[1,4]
- **THEN** EnemyChessDataManager 中注册了两条记录：`"{guid}_0"` (chessId=1) 和 `"{guid}_1"` (chessId=4)，HP 均为满血

#### Scenario: 两个相同类型敌人棋子互不干扰
- **WHEN** 场景中有两个 EntityConfigId=1001 的 EnemyEntity，均拥有 chessId=1
- **THEN** 各自在 EnemyChessDataManager 中有独立记录，修改一方 HP 不影响另一方

---

### Requirement: 敌方棋子生成时从 EnemyChessDataManager 读取初始 HP
`EnemySpawnManager` 在生成敌方 `ChessEntity` 时 SHALL 从 `EnemyChessDataManager` 读取对应槽位的 `CurrentHp`，并在 `BattleChessManager.RegisterChessEntity()` 后将该 HP 应用到 `entity.Attribute`，而非使用满血值。

#### Scenario: 已受伤棋子在下次战斗继承 HP
- **WHEN** 敌人棋子在上次战斗中 HP 降至 60，战斗结束（玩家失败）保留数据
- **THEN** 下次战斗生成该棋子时 HP=60

#### Scenario: 满血棋子正常生成
- **WHEN** 敌人棋子未经历过战斗，HP=MaxHp
- **THEN** 生成时 HP=MaxHp，行为与原逻辑相同

---

### Requirement: BattleChessManager 按 Camp 分发 HP 回写
`BattleChessManager.OnBattleEnd()` SHALL 根据 `ChessEntity.Camp` 区分回写目标：Camp=0（玩家方）回写 `GlobalChessManager`；Camp=1（敌方）回写 `EnemyChessDataManager`。

#### Scenario: 玩家棋子回写 GlobalChessManager
- **WHEN** Camp=0 的棋子 HP=80，战斗结束
- **THEN** `GlobalChessManager` 中该 ChessId 的 HP 被更新为 80，`EnemyChessDataManager` 不受影响

#### Scenario: 敌方棋子回写 EnemyChessDataManager
- **WHEN** Camp=1 的棋子 HP=30，战斗结束
- **THEN** `EnemyChessDataManager` 中对应 key 的 HP 被更新为 30，`GlobalChessManager` 不受影响

#### Scenario: 死亡棋子（实体已销毁）回写 HP=0
- **WHEN** Camp=1 的棋子在战斗中死亡（GameObject 被销毁）
- **THEN** `EnemyChessDataManager` 中对应 key 的 HP 被回写为 0（从 BattleChessData 缓存读取）
