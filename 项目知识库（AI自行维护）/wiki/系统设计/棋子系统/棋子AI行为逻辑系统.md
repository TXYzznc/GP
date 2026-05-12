# 棋子 AI 行为逻辑系统详解

> 最后更新：2026-05-07
> 状态：已完成
> 适用范围：所有棋子 AI（包括敌方AI、玩家放置的棋子、召唑师棋子）

## 核心概念

棋子 AI 基于**有限状态机（FSM）**设计，通过六个状态和清晰的状态转换规则实现完整的战斗行为。

```
Summoning (召唤) 
    ↓ 召唤动画完成（0.5秒）
Idle (待机) ← ★ 核心决策状态（每0.5秒重新搜索目标）
    ↓
    ├─ 应该释放技能 → UsingSkill (技能释放)
    ├─ 目标在攻击范围 → Attacking (普通攻击)
    └─ 目标在移动距离 → Moving (移动)
        ↓（到达范围 或 目标改变）
        └→ Idle (返回决策)
    
Dead (死亡) ← 由外部调用 ForceDead()
```

---

## 状态详解

### 1. Summoning（召唤状态）
**触发条件**：AI初始化时自动进入  
**持续时间**：0.5秒（m_SummoningDuration）  
**转换规则**：计时器结束 → Idle

**用途**：
- 播放召唤特效/动画
- 初始化 AI 的各项数据
- 为棋子进入战场预留时间

**代码位置**：[ChessAIBase.cs:266-275](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L266-L275)

---

### 2. Idle（待机状态）
**核心责任**：做所有关键决策的地方  
**执行频率**：每帧检查（但目标搜索间隔为 0.5 秒）

**每帧逻辑** [ChessAIBase.cs:285-337](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L285-L337)：

```
1. 定期搜索目标（间隔 0.5 秒）
   └─ m_TargetSearchTimer <= 0 时触发
   └─ 调用 FindTarget() 使用加权评分选择敌人
   └─ 重置计时器到 0.5 秒

2. 如果有目标，按优先级做出决策：
   ├─ 优先级1：应该释放技能？ → 转换到 UsingSkill
   ├─ 优先级2：在攻击范围内？ → 转换到 Attacking  
   └─ 优先级3：需要移动？ → 转换到 Moving

3. 如果没有目标，保持待机状态
```

**关键点**：
- ✅ **决策一致性**：在待机状态做一次决策，然后执行对应状态的行为
- ✅ **目标锁定**：一旦选择目标，就持续使用，直到返回待机重新评估
- ✅ **定期搜索**：每 0.5 秒重新搜索一次（防止敌人摧毁后 AI 仍在攻击空气）

---

### 3. Moving（移动状态）
**触发条件**：
- 目标存在但不在攻击/技能范围内
- 攻击范围内的移动目标超出范围

**转换规则** [FSMMeleeAI.cs:49-107](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/FSMMeleeAI.cs#L49-L107)：

```
if (目标无效) 
    → Idle

if (有待命技能 && 在技能范围内) 
    → Idle（返回释放技能）

if (无待命技能 && 在攻击范围内) 
    → Idle（返回决策攻击）

else
    → 继续移动
```

**移动目标计算** [ChessAIBase.cs:838-873](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L838-L873)：

- 优先移动到**技能范围边缘**（如果有待命技能）
- 否则移动到**攻击范围边缘**
- 距离计算：`targetPos - direction * (range * 0.8)`
  - `* 0.8` 是缓冲系数，防止卡在目标上

**debug 输出**：每 10 帧输出一次当前距离、范围、是否在范围内等信息

---

### 4. Attacking（攻击状态）
**触发条件**：目标在攻击范围内，且不应该释放技能

**每帧逻辑** [ChessAIBase.cs:355-431](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L355-L431)：

```
1. 检查目标是否有效
   └─ 无效 → Idle

2. 检查是否应该释放技能（带防抖，冷却 0.2 秒）
   ├─ 正在攻击中 → 标记 m_ShouldUseSkillAfterAttack = true
   │                (攻击结束后释放技能)
   └─ 未在攻击中 → 立即转换到 UsingSkill

3. 检查目标是否超出攻击范围（含缓冲区）
   └─ 超出 → Moving

4. 执行攻击（冷却完成且不在攻击中）
   └─ 触发动画和伤害计算
   └─ 重置冷却计时器
```

**攻击冷却**：
- 初始值由棋子的 `Attribute.AtkSpeed` 决定
- 每次攻击后重置
- 在 [ChessAIBase.cs:240-256](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L240-L256) 中每帧递减

**范围缓冲**：
- 计算：`distance > attackRange * 1.2`
- 防止目标走出一个像素就导致 AI 重新移动的 bug

**技能插队机制**：
- 如果攻击中检测到应该释放技能，不会立即中断当前攻击
- 而是标记 `m_ShouldUseSkillAfterAttack = true`
- 攻击动画完成后，通过回调函数 `OnAttackComplete()` 转换到技能状态

---

### 5. UsingSkill（技能释放状态）
**触发条件**：
- 在 Idle 状态检测到应该释放技能
- 在 Attacking 状态检测到应该释放技能且此时不在攻击中
- 从 Moving 返回到 Idle，优先级判定为释放技能

**核心逻辑** [ChessAIBase.cs:441-503](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L441-L503)：

```
1. 检查目标是否有效
   └─ 无效 → Idle

2. 如果还未开始释放技能（m_IsUsingSkill == false）：
   ├─ 检查是否在技能施法范围内
   │  └─ 不在 → Moving（移动到范围内）
   └─ 面向目标
   └─ 通过 CombatController 触发技能
      ├─ Skill2（大招）→ TriggerSkill2FromAI()
      └─ Skill1（技能1）→ TriggerSkill1FromAI()
   └─ 标记 m_IsUsingSkill = true

3. 等待技能动画完成
   └─ 由 OnSkillComplete 回调处理
   └─ 返回 Idle 重新决策
```

**关键细节**：
- ✅ **技能目标一致性**：使用 `m_CurrentTarget`（待机状态锁定的目标），不会重新搜索
- ✅ **范围检查**：特殊处理自我技能（如后羿技能1），这类技能总是在范围内
- ✅ **统一入口**：所有技能释放都通过 CombatController，避免绕过管理器

**技能选择优先级** [DefaultSkillReleaseStrategy.cs:42-60](Assets/AAAGame/Scripts/Game/SummonChess/AI/SkillStrategy/DefaultSkillReleaseStrategy.cs#L42-L60)：
```
GetPrioritySkill() {
    if (大招 可释放) return 2;
    if (技能1 可释放) return 1;
    return 0;
}
```

---

### 6. Dead（死亡状态）
**触发条件**：外部调用 `AI.ForceDead()`

**行为**：
- 停止所有 AI 逻辑
- 不允许从死亡状态切换出去（防止死亡后复活但 AI 已停止的问题）
- 清空当前目标

**代码位置**：[ChessAIBase.cs:519-523, 614-617](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L519-L523)

---

## 目标搜索（索敌）系统

### 搜索流程

```
1. FindTarget() 被调用（在 Idle 状态每 0.5 秒调用一次）
    ↓
2. 获取敌人信息缓存（CombatEntityTracker.GetEnemyCache）
    ↓
3. 检查是否有嘲讽效果
    ├─ 有嘲讽 → 强制攻击嘲讽来源
    └─ 无嘲讽 → 继续
    ↓
4. 使用索敌策略选择最优目标（ITargetSearchStrategy）
    └─ DefaultTargetSearchStrategy（加权评分）
    ↓
5. 应用索敌修改器（混乱、嘲讽扩展等）
    └─ 由 Buff 注册和注销
    ↓
6. 返回最终目标
```

**代码位置**：[ChessAIBase.cs:636-688](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L636-L688)

### 索敌配置（TargetSearchConfig）

[TargetSearchConfig.cs](Assets/AAAGame/Scripts/Game/Combat/AI/TargetSearchConfig.cs)

**三种预设配置**：

| 配置名 | 距离权重 | 血量权重 | 威胁度权重 | 适用场景 |
|--------|---------|---------|----------|--------|
| **默认** | 0.3 | 0.5 | 0.2 | 平衡策略（优先攻击残血） |
| **近战** | 0.5 | 0.3 | 0.2 | 近距离敌人（优先最近） |
| **远程** | 0.2 | 0.6 | 0.2 | 远距离敌人（优先残血） |

**评分计算** [ChessAIBase.cs:727-745](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L727-L745)：

```csharp
// 标准化后的加权评分
score = (max_distance - actual_distance) * distance_weight    // 距离因素
      + (1.0 - hp_percent) * hp_weight                         // 血量因素（残血加分）
      + atk_damage * threat_weight;                            // 威胁度因素
```

**特殊情况**：
- 如果目标被嘲讽（IsTaunted == true），强制攻击嘲讽来源
- 如果敌人缓存为空，返回 null（保持当前目标或待机）

---

## 技能释放系统

### 技能决策（ISkillReleaseStrategy）

**职责**：
- 判断大招和技能1是否可以释放
- 选择优先级最高的技能

**检查条件** [DefaultSkillReleaseStrategy.cs:21-40](Assets/AAAGame/Scripts/Game/SummonChess/AI/SkillStrategy/DefaultSkillReleaseStrategy.cs#L21-L40)：

```csharp
ShouldUseSkill1() {
    if (技能1不存在) return false;
    return 技能1.CanCast();  // 检查：激活 + 冷却完成 + 法力足够
}

ShouldUseSkill2() {
    if (大招不存在) return false;
    return 大招.CanCast();   // 检查：激活 + 冷却完成 + 法力足够
}

GetPrioritySkill() {
    if (ShouldUseSkill2()) return 2;
    if (ShouldUseSkill1()) return 1;
    return 0;
}
```

### 技能释放防抖机制

**目的**：防止 AI 在一帧内反复切换技能决策

**实现** [ChessAIBase.cs:373-404, 750-765](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L373-L404)：

```
防抖冷却时间: SKILL_DECISION_COOLDOWN = 0.2秒

每帧检查:
if (防抖计时器 > 0) 
    return false;  // 不允许新的技能决策

if (已标记为攻击后使用技能)
    return false;  // 避免重复决策
```

---

## 关键机制详解

### 1. 状态转换钩子

**进入状态（OnEnterState）** [ChessAIBase.cs:544-587](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L544-L587)

| 状态 | 操作 |
|------|------|
| Summoning | 启动计时器，停止移动 |
| Idle | 停止移动，立即搜索目标（设置计时器=0） |
| Moving | 无特殊初始化 |
| Attacking | 停止移动，重置攻击状态，冷却=0（可立即攻击） |
| UsingSkill | 停止移动，重置技能释放标记 |
| Dead | 清空目标，停止移动 |

**退出状态（OnExitState）** [ChessAIBase.cs:592-609](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L592-L609)

| 状态 | 操作 |
|------|------|
| Attacking | 重置 m_IsAttacking，清除 m_ShouldUseSkillAfterAttack |
| UsingSkill | 重置 m_IsUsingSkill，清除 m_ShouldUseSkillAfterAttack，清除 m_PendingSkillIndex |

**🔴 关键 Bug 修复**：
退出 UsingSkill 状态时必须清除 `m_ShouldUseSkillAfterAttack` 和 `m_PendingSkillIndex`，否则 AI 会卡住（即使已返回 Idle，仍然持有攻击后释放技能的标记）

### 2. 可执行性检查（CanExecuteAI）

**条件** [ChessAIBase.cs:204-235](Assets/AAA Game/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L204-L235)：

```
✅ CombatController 存在且启用
✅ 没有玩家移动指令（玩家手动移动棋子时，AI 暂停）
```

**触发时机**：每帧 Tick() 开始前

**为什么需要**：
- 战斗准备阶段 CombatController 未启用，AI 不能执行
- 玩家拖拽棋子移动时，AI 需要让步

### 3. 目标一致性保证

**问题背景**：
- AI 的索敌策略使用加权评分（距离 0.3 + 血量 0.5 + 威胁度 0.2）
- ChessTargetFinder.FindNearestEnemy() 只按直线距离排序
- 导致技能和移动选择不同的目标

**解决方案**：
```
1. 在 Idle 状态决策时使用 m_CurrentTarget
2. 技能释放时使用 AI.CurrentTarget（新增属性，line 91）
3. 永远不在技能代码中重新调用 FindNearestEnemy()
```

**代码示例**（EvilSpiritUltimate.cs）：
```csharp
// ✅ 正确做法
var aiBase = caster.AI as ChessAIBase;
ChessEntity targetEnemy = aiBase?.CurrentTarget;
if (targetEnemy == null)
{
    targetEnemy = FindNearestEnemy(caster);  // 备选方案
}
```

### 4. 技能后攻击机制

**场景**：攻击到一半时，突然满足技能释放条件

**处理逻辑** [ChessAIBase.cs:381-402](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L381-L402)：

```
1. 如果正在播放攻击动画
   └─ 标记 m_ShouldUseSkillAfterAttack = true
   └─ 继续播放攻击动画

2. 攻击动画完成时，OnAttackComplete() 回调触发
   └─ 检查 m_ShouldUseSkillAfterAttack == true
   └─ 切换到 UsingSkill 状态释放技能

3. 如果未在攻击中
   └─ 立即切换到 UsingSkill 状态
```

**关键状态标记**：
- `m_IsAttacking`：是否正在播放攻击动画
- `m_ShouldUseSkillAfterAttack`：攻击完成后是否应该释放技能
- `m_PendingSkillIndex`：待释放的技能索引（1 or 2）

---

## 计时器系统

| 计时器 | 含义 | 初始值 | 用途 |
|--------|------|--------|------|
| `m_AttackCooldownTimer` | 攻击冷却 | 由 AtkSpeed 决定 | 控制攻击频率 |
| `m_TargetSearchTimer` | 目标搜索间隔 | 0 | 每 0.5 秒重新搜索 |
| `m_SummoningTimer` | 召唤动画持续时间 | 0.5 秒 | 从 Summoning 到 Idle |
| `m_SkillDecisionCooldown` | 技能决策防抖 | 0.2 秒 | 防止技能频繁切换 |

**更新逻辑** [ChessAIBase.cs:240-256](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs#L240-L256)：
```csharp
每帧 dt 时间内：
- m_AttackCooldownTimer -= dt (如果 > 0)
- m_TargetSearchTimer -= dt
- m_SkillDecisionCooldown -= dt (如果 > 0)
```

---

## 两种 AI 实现

### FSMMeleeAI（近战 AI）[FSMMeleeAI.cs](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSMMeleeAI.cs)

**特点**：
- 直接追击目标
- 优先最近的敌人（距离权重 0.5）
- 无复杂的移动预判

**核心方法**：
- `TickMoving()`：持续向目标移动，检查是否到达范围
- 每 10 帧输出一次 debug 信息，包括距离、范围、状态标记

**使用场景**：
- 敌方棋子（邪灵、嫦娥等）
- 玩家放置的肉盾棋子

### FSMRangedAI（远程 AI）

**预设但功能类似 FSMMeleeAI**

---

## 完整交互流程示例

### 场景：敌方邪灵棋子攻击玩家的召唑师棋子

```
时间轴：

T0: 棋子进入战斗阶段
    ├─ AI.Init() 初始化
    └─ ChangeState(Summoning)

T0 + 0.5s: 召唑动画完成
    ├─ ChangeState(Idle)
    └─ m_TargetSearchTimer = 0（立即搜索）

Tick1: Idle 状态
    ├─ 搜索目标（缓存返回所有敌人）
    ├─ 加权评分选择最近的活着的敌人
    ├─ 找到：召唑师棋子
    ├─ m_CurrentTarget = 召唑师棋子
    └─ 优先级判定：不应该释放技能 → ChangeState(Moving)

Tick2-100: Moving 状态
    ├─ 每帧检查距离
    ├─ 距离 > 攻击范围 * 1.2 → 继续移动
    └─ 每 10 帧输出：距离=5.2m, 范围=1.5m, InRange=false

Tick101: Moving 状态
    ├─ 距离 ≈ 1.6m（1.5m 范围 * 1.2 缓冲）
    ├─ IsInAttackRange(target) == true
    └─ ChangeState(Idle)（返回决策）

Tick102: Idle 状态
    ├─ 检查技能决策 → 应该释放大招（法力充足、冷却完成）
    ├─ 标记 m_PendingSkillIndex = 2
    └─ ChangeState(UsingSkill)

Tick103: UsingSkill 状态
    ├─ 面向目标
    ├─ 调用 CombatController.TriggerSkill2FromAI()
    ├─ 大招执行：
    │  ├─ 使用 AI.CurrentTarget（依然是召唑师棋子）✅
    │  └─ 在召唑师位置生成法阵
    ├─ 标记 m_IsUsingSkill = true
    └─ 等待技能完成回调

OnSkillComplete 回调：
    ├─ m_IsUsingSkill = false
    ├─ m_ShouldUseSkillAfterAttack = false
    ├─ m_PendingSkillIndex = 0
    └─ ChangeState(Idle)（重新决策）

Tick104+: Idle 状态
    ├─ 大招冷却中...
    └─ 继续循环
```

---

## 常见问题排查

### Q1: AI 卡在 Moving 状态，一直在移动

**可能原因**：
1. 目标无效但检查失败 → 检查 `IsTargetValid()` 的实现
2. 到达范围判定有问题 → 检查 `IsInAttackRange()` 的距离计算
3. 缓冲区系数过大 → 调整 `ATTACK_RANGE_BUFFER = 1.2`

**调试**：
- 启用 FSMMeleeAI 中的每 10 帧 debug 输出
- 查看：当前距离、设置范围、InRange 标记、是否有待命技能

### Q2: 技能选择了错误的目标

**原因**：技能中调用了 `FindNearestEnemy()` 而不是使用 `AI.CurrentTarget`

**修复**：
```csharp
// ❌ 错误
var target = FindNearestEnemy(caster);

// ✅ 正确
var aiBase = caster.AI as ChessAIBase;
var target = aiBase?.CurrentTarget ?? FindNearestEnemy(caster);
```

### Q3: AI 在攻击中途突然释放技能，攻击动画中断

**原因**：没有采用"攻击后释放"机制，而是直接中断状态

**检查**：
- 技能决策是否在 Attacking 状态检查？
- 如果正在攻击，是否标记了 `m_ShouldUseSkillAfterAttack`？
- OnAttackComplete 回调是否正确触发？

### Q4: AI 冻结不动，没有任何日志

**原因**：
1. CanExecuteAI() 返回 false → 检查 CombatController 是否启用
2. 某个状态的转换卡住 → 检查状态转换条件的逻辑
3. 待命技能标记未清除 → 检查 OnExitState() 的清理代码

**调试**：
```csharp
// 在 Tick() 开头添加临时日志
if (!CanExecuteAI())
    DebugEx.Warning("AI", $"{m_Context.Entity.Config.Name} - CanExecuteAI=false");
```

---

## 性能优化建议

### 1. 敌人信息缓存

✅ **已实现**：`CombatEntityTracker.GetEnemyCache(myCamp)` 返回一个缓存列表，避免每次索敌都遍历全场

**更新时机**：
- CombatManager 在战斗开始时调用 `BuildEnemyCache()`
- 敌人死亡时调用 `ClearEnemyCache()` 强制重建

### 2. 目标搜索间隔

✅ **已实现**：每 0.5 秒搜索一次，不是每帧

### 3. 防抖机制

✅ **已实现**：技能决策冷却 0.2 秒，防止高频切换

---

## 总结

棋子 AI 系统通过以下设计实现高效、一致的战斗行为：

| 设计点 | 好处 |
|--------|------|
| **清晰的 FSM 状态机** | 易于理解、调试、扩展 |
| **待机状态集中决策** | 避免在多个状态重复搜索，确保决策一致 |
| **目标锁定** | 技能和移动选择同一个目标 |
| **敌人信息缓存** | 减少遍历，提高性能 |
| **防抖机制** | 防止频繁切换，减少不稳定行为 |
| **统一技能入口** | 所有技能通过 CombatController 释放，便于管理 |
| **状态转换钩子** | 进入/退出状态时的清理和初始化 |

---

## 相关文件

| 文件 | 用途 |
|------|------|
| [ChessAIBase.cs](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs) | AI 基类，包含 FSM 状态机、状态逻辑、目标搜索 |
| [FSMMeleeAI.cs](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/FSMMeleeAI.cs) | 近战 AI 实现 |
| [FSMRangedAI.cs](Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/FSMRangedAI.cs) | 远程 AI 实现 |
| [TargetSearchConfig.cs](Assets/AAAGame/Scripts/Game/Combat/AI/TargetSearchConfig.cs) | 索敌配置（距离、血量、威胁度权重） |
| [DefaultSkillReleaseStrategy.cs](Assets/AAAGame/Scripts/Game/SummonChess/AI/SkillStrategy/DefaultSkillReleaseStrategy.cs) | 技能释放决策 |
| [CombatEntityTracker.cs](Assets/AAAGame/Scripts/Game/Combat/Core/CombatEntityTracker.cs) | 棋子生命周期管理、敌人缓存 |
