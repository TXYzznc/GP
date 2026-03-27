# 棋子手动移动后索敌目标修复设计

## 概述

修复棋子在玩家手动移动后丢失索敌能力的问题。问题的根本原因是AI在恢复到Idle状态后，仍然保留着移动前的旧目标，但由于位置变化导致目标不在攻击范围内，而目标搜索计时器没有立即重置，导致无法及时重新搜索新目标。

## 术语表

- **Bug_Condition (C)**: 棋子被玩家手动移动完成后，AI恢复但保留旧目标的条件
- **Property (P)**: 手动移动完成后应该立即清除旧目标并重新搜索的期望行为
- **Preservation**: 非手动移动情况下的现有AI行为必须保持不变
- **OnMovementArrived**: ChessCombatController中处理移动完成的方法
- **m_TargetSearchTimer**: ChessAIBase中控制目标搜索频率的计时器
- **m_CurrentTarget**: ChessAIBase中当前锁定的攻击目标

## Bug 详情

### 故障条件

Bug在玩家手动移动棋子完成后出现。ChessCombatController的OnMovementArrived方法会将AI状态恢复为Idle，但ChessAIBase中的m_CurrentTarget和m_TargetSearchTimer没有相应重置，导致AI尝试攻击基于旧位置选择的目标。

**正式规范:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type MovementCompletionEvent
  OUTPUT: boolean
  
  RETURN input.isPlayerControlled = true
         AND input.aiStateChangedTo = Idle
         AND m_CurrentTarget != null
         AND NOT IsInAttackRange(m_CurrentTarget)
END FUNCTION
```

### 示例

- 玩家将后羿从位置A移动到位置B，移动前后羿的目标是敌人X
- 移动完成后，后羿在位置B，但目标仍是敌人X
- 由于距离变化，敌人X不在后羿新位置的攻击范围内
- 系统持续输出"目标不在攻击范围内"警告
- 由于m_TargetSearchTimer > 0，后羿不会立即重新搜索目标

## 期望行为

### 保持要求

**不变行为:**
- AI自主移动时的目标搜索逻辑必须保持不变
- 正常攻击状态下的目标管理必须保持不变
- 目标搜索间隔机制在非手动移动情况下必须保持不变

**范围:**
所有不涉及玩家手动移动完成的输入都应该完全不受此修复影响。这包括:
- AI自主移动和攻击行为
- 技能释放过程中的目标管理
- 正常的目标搜索计时器逻辑

## 假设根本原因

基于Bug描述，最可能的问题是:

1. **目标状态未重置**: OnMovementArrived方法只重置了AI状态，但没有清除旧目标
   - 玩家移动完成后m_CurrentTarget仍指向旧目标
   - 旧目标可能不适合新位置

2. **搜索计时器未重置**: 目标搜索计时器没有在手动移动完成后立即重置
   - m_TargetSearchTimer > 0导致无法立即重新搜索
   - 需要等待完整的搜索间隔才能找到新目标

3. **状态同步问题**: ChessCombatController和ChessAIBase之间的状态同步不完整
   - CombatController恢复AI状态但没有通知AIBase重置目标状态

## 正确性属性

Property 1: 故障条件 - 手动移动后立即重新搜索目标

_对于任何_ 玩家手动移动棋子完成的输入（isBugCondition返回true），修复后的AI系统应该立即清除旧目标并重新搜索适合新位置的目标，确保棋子能够正常进行索敌和攻击。

**验证: 需求 2.1, 2.2, 2.3**

Property 2: 保持 - 非手动移动行为

_对于任何_ 不是玩家手动移动完成的输入（isBugCondition返回false），修复后的代码应该产生与原始代码完全相同的结果，保持所有现有的AI自主移动、攻击和目标搜索功能。

**验证: 需求 3.1, 3.2, 3.3**

## 修复实现

### 需要的更改

假设我们的根本原因分析是正确的:

**文件**: `Assets/AAAGame/Scripts/Game/SummonChess/Component/ChessCombatController.cs`

**方法**: `OnMovementArrived`

**具体更改**:
1. **添加AI目标重置通知**: 在恢复AI状态时，通知AI组件清除旧目标
   - 调用AI的目标重置方法
   - 确保目标搜索计时器立即重置

2. **增强状态同步**: 确保CombatController和AIBase之间的状态完全同步
   - 在AI状态恢复时同步重置相关状态

**文件**: `Assets/AAAGame/Scripts/Game/SummonChess/AI/FSM/ChessAIBase.cs`

**具体更改**:
3. **添加手动移动重置方法**: 提供公共方法供CombatController调用来重置目标状态
   - 清除m_CurrentTarget
   - 重置m_TargetSearchTimer为0以立即触发搜索

4. **增强OnEnterState逻辑**: 在进入Idle状态时检查是否需要立即搜索目标

## 测试策略

### 验证方法

测试策略遵循两阶段方法：首先在未修复的代码上展现反例来证明Bug存在，然后验证修复正确工作并保持现有行为。

### 探索性故障条件检查

**目标**: 在实施修复之前展现Bug的反例。确认或反驳根本原因分析。如果反驳，我们需要重新假设。

**测试计划**: 编写测试模拟玩家手动移动棋子的场景，并断言移动完成后AI应该能够重新搜索目标。在未修复代码上运行这些测试以观察失败并理解根本原因。

**测试用例**:
1. **手动移动后目标重置测试**: 模拟玩家移动棋子到新位置，旧目标不在攻击范围内（在未修复代码上会失败）
2. **目标搜索计时器重置测试**: 验证手动移动完成后立即触发目标搜索（在未修复代码上会失败）
3. **新位置目标搜索测试**: 验证基于新位置能找到合适的目标（在未修复代码上会失败）
4. **边界情况测试**: 移动到没有敌人的区域时的行为（可能在未修复代码上失败）

**期望的反例**:
- 手动移动完成后AI仍尝试攻击旧目标
- 可能原因: 目标未清除、搜索计时器未重置、状态同步不完整

### 修复检查

**目标**: 验证对于所有满足Bug条件的输入，修复后的函数产生期望的行为。

**伪代码:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := handlePlayerMovementComplete_fixed(input)
  ASSERT expectedBehavior(result)
END FOR
```

### 保持检查

**目标**: 验证对于所有不满足Bug条件的输入，修复后的函数产生与原始函数相同的结果。

**伪代码:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT handleAIBehavior_original(input) = handleAIBehavior_fixed(input)
END FOR
```

**测试方法**: 推荐使用基于属性的测试进行保持检查，因为:
- 它在输入域上自动生成许多测试用例
- 它捕获手动单元测试可能遗漏的边界情况
- 它为所有非Bug输入提供强有力的行为不变保证

**测试计划**: 首先在未修复代码上观察AI自主移动和攻击行为，然后编写基于属性的测试捕获这些行为。

**测试用例**:
1. **AI自主移动保持**: 验证AI自主移动到目标的行为在修复后保持不变
2. **正常攻击流程保持**: 验证正常攻击状态下的目标管理保持不变
3. **目标搜索间隔保持**: 验证非手动移动情况下的搜索计时器逻辑保持不变
4. **技能释放保持**: 验证技能释放过程中的目标管理保持不变

### 单元测试

- 测试手动移动完成后的目标重置逻辑
- 测试目标搜索计时器在不同场景下的行为
- 测试AI状态转换时的目标管理

### 基于属性的测试

- 生成随机游戏状态并验证手动移动后的目标搜索正确工作
- 生成随机AI行为配置并验证非手动移动行为的保持
- 测试各种场景下目标搜索和攻击逻辑的一致性

### 集成测试

- 测试完整的手动移动流程，包括目标重新搜索和攻击
- 测试在不同战斗场景下手动移动的影响
- 测试手动移动与AI自主行为的交互