# CastRange 系统架构设计实现

## 设计目标

统一所有棋子技能的施法范围管理，区分以下概念：
- **AtkRange**：普攻范围（仅用于普攻）
- **CastRange**：技能施法范围（所有技能都使用，除了自我技能）
- **AreaRadius**：技能效果范围（生效范围，独立于施法范围）

## 核心修改

### 1. ChessTargetFinder 工具类增强
**文件**：`Assets/AAAGame/Scripts/Utils/Game/ChessTargetFinder.cs`

新增方法：
```csharp
/// <summary>
/// 检查目标是否在施法范围内（用于技能）
/// </summary>
public static bool IsInCastRange(ChessEntity self, ChessEntity target, double castRange)
```

**用途**：所有需要检查施法范围的地方使用该方法

### 2. ChessSkillBase 技能基类增强
**文件**：`Assets/AAAGame/Scripts/Game/SummonChess/Core/ChessSkillBase.cs`

新增方法：
```csharp
/// <summary>
/// 检查目标是否在施法范围内
/// </summary>
protected bool IsInCastRange(ChessEntity caster, ChessEntity target)
```

**用途**：技能类可以调用该方法判断目标是否在施法范围内

### 3. ChessAIBase AI基类核心改动
**文件**：`Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs`

#### 3.1 使用技能状态（TickUsingSkill）增强
- **新增逻辑**：在释放技能前检查目标是否在技能的 CastRange 范围内
- **如果不在范围**：自动切换到移动状态靠近敌人
- **特殊处理**：自我技能（ID=13）始终返回true，无需范围检查

```csharp
protected override void TickUsingSkill(float dt)
{
    // ... 前置检查
    
    if (!m_IsUsingSkill)
    {
        // ⭐ 新增：检查施法范围
        if (!IsSkillInRange(m_PendingSkillIndex, m_CurrentTarget))
        {
            ChangeState(ChessAIState.Moving);
            return;
        }
        
        // ... 释放技能
    }
}
```

#### 3.2 移动时的范围判断（MoveToTarget）增强
- **逻辑**：如果有待命技能，优先移动到技能施法范围内
- **计算**：基于技能的 CastRange 而不是 AtkRange

```csharp
protected virtual void MoveToTarget(ChessEntity target)
{
    double rangeToUse = (float)m_Context.Entity.Attribute.AtkRange;
    
    // 如果有待命技能，用技能的 CastRange
    if (m_ShouldUseSkillAfterAttack || m_CurrentState == ChessAIState.Moving)
    {
        IChessSkill skill = GetPendingSkill();
        if (skill != null && skill.Config != null && skill.Config.Id != 13)
        {
            rangeToUse = skill.Config.CastRange;
        }
    }
    
    // 移动到范围边缘
    Vector3 moveTarget = targetPos - direction * ((float)rangeToUse * 0.8f);
    m_Context.Entity.Movement?.MoveTo(moveTarget);
}
```

#### 3.3 新增辅助方法
```csharp
/// <summary>
/// 检查目标是否在技能施法范围内
/// 特殊处理：自我技能始终返回true
/// </summary>
protected bool IsSkillInRange(int skillIndex, ChessEntity target)
```

### 4. 子类AI实现（FSMMeleeAI、FSMRangedAI）
**文件**：
- `Assets/AAAGame/Scripts/Game/SummonChess/AI/FSMMeleeAI.cs`
- `Assets/AAAGame/Scripts/Game/SummonChess/AI/FSMRangedAI.cs`

增强移动状态判断逻辑：
```csharp
protected override void TickMoving(float dt)
{
    // 如果有待命技能，检查技能范围
    if (m_ShouldUseSkillAfterAttack && m_PendingSkillIndex > 0)
    {
        if (IsSkillInRange(m_PendingSkillIndex, m_CurrentTarget))
        {
            ChangeState(ChessAIState.Idle);
            return;
        }
    }
    // 否则检查攻击范围
    else if (IsInAttackRange(m_CurrentTarget))
    {
        ChangeState(ChessAIState.Idle);
        return;
    }
    
    MoveToTarget(m_CurrentTarget);
}
```

## 行为流程

### 普攻流程
```
待机 → 找到目标 → 检查AtkRange
  ├─ 在范围内 → 进入攻击状态 → 普攻
  └─ 不在范围内 → 进入移动状态 → 移动到AtkRange内 → 回到待机
```

### 技能释放流程（有待命技能时）
```
待机 → 决策要用技能 → 进入移动状态（如果不在CastRange内）
  ├─ 在技能CastRange内 → 到达时返回待机 → 进入使用技能状态
  │   ├─ 检查CastRange ✓
  │   └─ 释放技能
  └─ 移动中 → 继续移动直到到达 → 返回待机
```

## 特殊情况处理

### 自我技能（ID=13 后羿技能一）
- 特殊标记：`skill.Config.Id == 13`
- **不需要范围检查**：`IsSkillInRange` 直接返回 `true`
- **效果**：可以在任意距离释放

## 关键参数对照

| 参数 | 使用场景 | 含义 |
|------|---------|------|
| `AtkRange` | 普攻/普攻效果 | 普攻能打到的距离 |
| `CastRange` | 所有技能（除自我） | 技能能释放的距离 |
| `AreaRadius` | 技能生效 | 技能效果的范围（如法阵范围） |

## 测试清单

- [ ] 邪灵大招：检查是否需要移动到 CastRange=10 范围内才能释放
- [ ] 邪灵技能一：检查是否在 CastRange 范围内才能使用
- [ ] 后羿技能一：检查是否不需要范围检查（自我技能）
- [ ] 后羿大招：检查是否需要移动到 CastRange 范围内
- [ ] 嫦娥技能一：检查范围管理

## 注意事项

1. **移动优先级**：有待命技能时，优先移动到技能范围而不是攻击范围
2. **缓冲距离**：移动目标为 `range * 0.8f`，留出20%缓冲防止抖动
3. **范围检查顺序**：技能范围 > 攻击范围
4. **日志跟踪**：所有范围判断都有对应的日志输出便于调试
