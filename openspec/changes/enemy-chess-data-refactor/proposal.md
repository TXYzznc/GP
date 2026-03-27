## Why

敌人棋子当前通过 `GlobalChessManager` 管理 HP 持久化，与玩家棋子共用同一套机制，导致不同场景中相同 ChessId 的敌人棋子互相覆盖状态，且玩家失败后敌人实体被销毁而无法恢复原有棋子数据。需要将敌人棋子数据与玩家棋子数据彻底分离，由每个 EnemyEntity 持有独立的棋子实例，并实现"玩家胜才消灭敌人，玩家败则敌人保留"的战斗结果逻辑。

## What Changes

- **新增** `EnemyChessState`：表示单个敌人棋子实例的 HP 数据（ChessId、当前HP、最大HP）
- **新增** `EnemyChessDataManager`：全局单例，以「敌人实体唯一ID + 棋子槽位索引」为 key 管理所有敌人棋子数据，与 GlobalChessManager 完全独立
- **修改** `EnemyEntity`：初始化时从 EnemyTable 读取 ChessIds，在 EnemyChessDataManager 中注册独立棋子实例；销毁时清理对应数据
- **修改** `BattleChessManager`：回写 HP 时区分玩家棋子（写 GlobalChessManager）和敌人棋子（写 EnemyChessDataManager），通过棋子的 `Camp` 字段判断
- **修改** `EnemyEntityManager.OnCombatEnd()`：玩家胜利时销毁敌人实体并清理数据；玩家失败时保留敌人实体（当前为无论胜负都销毁）
- **修改** `CombatPreparationState`：战斗准备阶段不再销毁敌人实体，改为隐藏/暂停
- **新增** 敌人棋子全灭检测：战斗中当某敌人的所有棋子 HP=0 时，触发该敌人失败事件

## Capabilities

### New Capabilities

- `enemy-chess-data`: 独立的敌人棋子数据层——每个 EnemyEntity 注册独立棋子实例，不走 GlobalChessManager；战斗 HP 回写到 EnemyChessDataManager；棋子全灭时触发敌人失败
- `enemy-survival-on-defeat`: 玩家失败时敌人实体保留，保留棋子当前 HP；玩家胜利时才销毁敌人实体并清理数据

### Modified Capabilities

（无已有 spec，不适用）

## Impact

- `EnemyEntity.cs`：新增棋子数据初始化与注册逻辑
- `EnemyEntityManager.cs`：`OnCombatEnd()` 增加胜负分支，失败时不销毁敌人
- `CombatPreparationState.cs`：敌人实体从"销毁"改为"暂停/隐藏"
- `BattleChessManager.cs`：`OnBattleEnd()` 回写逻辑分 camp 处理
- `EnemySpawnManager.cs` / `CombatState.cs`：生成敌方棋子时从 EnemyChessDataManager 读取初始 HP
- 新增文件：`EnemyChessState.cs`、`EnemyChessDataManager.cs`
