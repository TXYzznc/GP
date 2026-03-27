# 修复棋子状态持久化问题 - 设计方案

## 架构设计

### 数据分层

```
全局棋子数据层 (Global Chess Data)
    ↓ 同步（仅血量）
战斗场景棋子数据层 (Battle Chess Data)
    ↓ 显示
UI 层 (UI Display)
```

### 核心概念

1. **全局棋子数据** (`GlobalChessData`)
   - 存储棋子的持久化状态：血量、等级等
   - 在战斗间隙保持不变
   - 只能通过特定方式修改（如使用道具、回到基地自动恢复）
   - **不包含**：战斗中的临时状态效果（如中毒、冻结等）

2. **战斗棋子数据** (`BattleChessData`)
   - 战斗场景中的临时数据副本
   - 包含血量和临时状态效果（中毒、冻结等）
   - 用于战斗逻辑计算
   - 战斗结束时仅将血量同步回全局数据，状态效果清除

3. **状态同步机制**
   - **战斗开始**：从全局数据复制血量到战斗数据，状态效果初始化为空
   - **战斗进行**：修改战斗数据（血量、临时状态效果）
   - **战斗结束**：将战斗数据的血量回写到全局数据，清除所有临时状态效果

4. **血量恢复机制**
   - **基地恢复**：离开战斗回到基地时，自动恢复所有棋子血量到满值
   - **道具恢复**：在任何时候可以使用特殊道具恢复受伤棋子的血量
   - **技能恢复**：某些技能可能恢复血量
   - **限制**：已死亡的棋子（血量 <= 0）不能通过道具/技能恢复，只能回到基地后自动恢复

## 实现方案

### 1. 数据结构设计

```csharp
// 全局棋子状态数据 - 仅包含持久化数据
public class GlobalChessState
{
    public int ChessId { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    public int Level { get; set; }
    // ... 其他持久化属性
}

// 战斗中的棋子数据 - 包含临时状态
public class BattleChessData
{
    public int ChessId { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    public List<BuffData> ActiveBuffs { get; set; }  // 临时状态效果
    
    // 判断棋子是否已死亡
    public bool IsDead => CurrentHP <= 0;
    
    // 判断棋子是否可以恢复血量
    public bool CanRecover => !IsDead && CurrentHP < MaxHP;
}
```

### 2. 棋子管理器职责

**全局棋子管理器** (`GlobalChessManager`)
- 维护全局棋子状态数据（仅血量和基础属性）
- 提供状态查询接口
- 处理状态持久化（保存/加载）
- 提供血量更新接口（仅限战斗结束或特殊操作）
- 处理基地恢复逻辑

**战斗棋子管理器** (`BattleChessManager`)
- 管理战斗场景中的棋子数据
- 初始化时从全局管理器同步血量数据
- 管理临时状态效果（Buff）
- 战斗结束时回写血量到全局管理器

### 3. 同步流程

#### 战斗开始时
```
1. BattleChessManager.Initialize()
2. 遍历出战棋子列表
3. 从 GlobalChessManager 获取每个棋子的全局状态（血量）
4. 创建 BattleChessData 副本，初始化状态效果为空
5. 初始化战斗 UI（显示当前血量）
```

#### 战斗进行中
```
1. 所有状态修改都作用于 BattleChessData
2. 棋子受伤修改战斗数据的血量
3. 棋子获得 Buff 添加到战斗数据的状态效果列表
4. UI 订阅战斗数据变化事件，实时更新显示
5. 战斗结束时，状态效果自动清除
```

#### 战斗结束时
```
1. BattleChessManager.OnBattleEnd()
2. 遍历所有参战棋子
3. 将 BattleChessData 的血量回写到 GlobalChessManager
4. 清除所有临时状态效果
5. 触发全局数据更新事件
6. 清理战斗数据
```

#### 回到基地时
```
1. 调用 GlobalChessManager.RestoreAllChessHP()
2. 遍历所有棋子
3. 将所有棋子血量恢复到最大值
4. 触发全局数据更新事件
5. 更新 UI 显示
```

### 4. 关键接口设计

```csharp
// 全局棋子管理器接口
public interface IGlobalChessManager
{
    // 获取棋子全局状态
    GlobalChessState GetChessState(int chessId);
    
    // 更新棋子血量（仅限战斗结束或特殊操作）
    void UpdateChessHP(int chessId, int newHP);
    
    // 恢复所有棋子血量到满值（回到基地时调用）
    void RestoreAllChessHP();
    
    // 使用道具恢复棋子血量（仅限受伤的棋子）
    bool TryRecoverChessHP(int chessId, int recoverAmount);
}

// 战斗棋子管理器接口
public interface IBattleChessManager
{
    // 初始化战斗棋子数据
    void Initialize(List<int> chessIds);
    
    // 获取战斗中的棋子数据
    BattleChessData GetBattleChessData(int chessId);
    
    // 对棋子造成伤害
    void DamageChess(int chessId, int damageAmount);
    
    // 给棋子添加 Buff
    void AddBuffToChess(int chessId, BuffData buff);
    
    // 移除棋子的 Buff
    void RemoveBuffFromChess(int chessId, int buffId);
    
    // 战斗结束，同步数据
    void OnBattleEnd();
}
```

## 修改范围

### 需要修改的文件

1. **棋子数据管理**
   - 创建或修改全局棋子管理器
   - 创建战斗棋子管理器
   - 定义数据结构

2. **战斗系统**
   - 战斗初始化时调用数据同步
   - 战斗结束时调用数据回写
   - 确保所有伤害和 Buff 修改作用于战斗数据

3. **基地系统**
   - 离开战斗回到基地时调用恢复逻辑
   - 更新 UI 显示恢复后的血量

4. **道具系统**
   - 使用恢复道具时调用 `TryRecoverChessHP()`
   - 检查棋子是否可以恢复（未死亡）

5. **UI 系统**
   - 订阅战斗数据变化事件
   - 显示当前血量和临时状态效果

## 风险评估

- **数据一致性**：需要确保全局数据和战斗数据的同步正确
- **死亡棋子处理**：需要正确处理已死亡棋子的恢复限制
- **性能影响**：大量棋子时的数据复制性能
- **向后兼容**：需要确保现有存档数据的兼容性

## 验证方案

1. 单元测试：测试数据同步逻辑和血量恢复逻辑
2. 集成测试：测试战斗流程中的数据一致性
3. 手动测试：验证棋子血量在多次战斗中的持久化，以及基地恢复逻辑
