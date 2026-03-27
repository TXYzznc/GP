# NavMesh 重新激活后立即调用问题 问题诊断和修复

> **最后更新**: 2026-03-25
> **状态**: 有效
> **严重程度**: 中等
> **修复状态**: 已修复

---

## 问题概述

将 `EnemyEntity` 所在 GameObject `SetActive(false)` 后再 `SetActive(true)` 时，立即调用 `NavMeshAgent` 的方法（如 `isStopped`、`Warp`、`ResetPath`）会报错，导致 AI 状态机无法正常恢复。

---

## 症状描述

### 表现形式

- Console 报错：`"Resume" can only be called on an active agent that has been placed on a NavMesh.`
- Console 报错：`Failed to create agent because there is no valid NavMesh`
- 敌人实体恢复后 AI 停止运作，卡在战斗状态无法回到 Idle

### 复现步骤

1. 触发与敌人战斗
2. `EnemyEntityManager.HideEntityForCombat()` 调用 `SetActive(false)`
3. 玩家失败，`RestoreEntityAfterCombat()` 调用 `SetActive(true)` 后紧接着调用 NavAgent 方法

**预期行为**：NavAgent 方法正常执行，AI 恢复到 Idle 状态

**实际行为**：抛出 NavMesh 相关异常，AI 无法恢复

### 影响范围

- `EnemyEntityManager.HideEntityForCombat()`
- `EnemyEntityManager.RestoreEntityAfterCombat()`
- `EnemyEntity.ExitCombat()`
- `EnemyIdleState.OnEnter()`
- `EnemyCombatState.OnExit()`

---

## 根本原因分析

### 初步诊断

`SetActive(false)` 时 Unity 会将 `NavMeshAgent` 从 NavMesh 上移除。

### 深入调查

Unity NavMeshAgent 的行为：
- `SetActive(false)` → Agent 从 NavMesh 移除，`isOnNavMesh = false`
- `SetActive(true)` → Agent **不会立即**重新放置到 NavMesh，需要等待下一帧的物理更新
- 在同一帧中调用 `isStopped = true/false`、`Warp()`、`ResetPath()` 等方法时，由于 Agent 尚未就绪，触发异常

```
// 问题代码
entity.gameObject.SetActive(true);
entity.NavAgent.Warp(pos);       // ❌ 同帧调用，isOnNavMesh=false
entity.NavAgent.isStopped = false; // ❌ 同帧调用
```

### 根本原因

**根本原因**：`SetActive(true)` 后 NavMeshAgent 需要至少一帧才能重新放置到 NavMesh，同帧调用任何 NavAgent 方法均会报错。

**为什么之前没有发现**：重构前使用 `Destroy()` 而非 `SetActive(false/true)`，此问题从未出现。

---

## 修复方案

### 修复思路

在所有 NavAgent 操作前加 `isOnNavMesh` 守卫。若 Agent 尚未就绪则跳过该帧的操作，等下一帧 Agent 就绪后由正常 AI 状态机驱动。

### 代码修改

**修改的文件**:
```
Assets/AAAGame/Scripts/Game/Explore/Enemy/Core/EnemyEntity.cs
Assets/AAAGame/Scripts/Game/Explore/Enemy/Core/EnemyEntityManager.cs
Assets/AAAGame/Scripts/Game/Explore/Enemy/State/EnemyIdleState.cs
Assets/AAAGame/Scripts/Game/Explore/Enemy/State/EnemyCombatState.cs
```

**代码对比**:

```csharp
// ❌ 修改前 — EnemyEntity.ExitCombat()
m_IsInCombat = false;
m_NavAgent.isStopped = false;
m_AI?.ResetToIdle();

// ✅ 修改后
m_IsInCombat = false;
if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
    m_NavAgent.isStopped = false;
m_AI?.ResetToIdle();
```

```csharp
// ❌ 修改前 — EnemyEntityManager.HideEntityForCombat()
entity.NavAgent.isStopped = true;
entity.gameObject.SetActive(false);

// ✅ 修改后
if (entity.NavAgent != null && entity.NavAgent.isOnNavMesh)
    entity.NavAgent.isStopped = true;
entity.gameObject.SetActive(false);
```

```csharp
// ❌ 修改前 — EnemyEntityManager.RestoreEntityAfterCombat()
entity.gameObject.SetActive(true);
entity.NavAgent.Warp(entity.SpawnPosition); // ❌

// ✅ 修改后
entity.gameObject.SetActive(true);
if (entity.NavAgent != null && entity.NavAgent.isOnNavMesh)
    entity.NavAgent.Warp(entity.SpawnPosition);
entity.ExitCombat();
```

```csharp
// ❌ 修改前 — EnemyIdleState.OnEnter()
var agent = m_AI.Entity.NavAgent;
agent.isStopped = true; // ❌ 可能在 SetActive 同帧执行

// ✅ 修改后
var agent = m_AI.Entity.NavAgent;
if (agent != null && agent.isOnNavMesh)
    agent.isStopped = true;
```

---

## 验证方法

### 测试步骤

1. 启动游戏，进入探索场景
2. 靠近敌人触发战斗
3. 故意失败战斗
4. 观察控制台是否有 NavMesh 相关报错
5. 观察敌人实体是否正常恢复到 Idle/巡逻状态

### 预期结果

- Console 无 NavMesh 报错
- 敌人恢复后正常在场景中巡逻

### 验证结果

- [x] 无 NavMesh 相关报错
- [x] 敌人实体恢复后 AI 正常运作
- [x] 没有引入新的问题

---

## 预防措施

### 代码审查重点

- **凡是涉及 `SetActive(false/true)` 的 NavMeshAgent 使用场景，必须先检查 `isOnNavMesh`**
- 不要假设 `SetActive(true)` 后 NavMeshAgent 立即可用

### 通用模式

```csharp
// 统一守卫写法
if (navAgent != null && navAgent.isOnNavMesh)
{
    navAgent.isStopped = value;
    // 其他 NavAgent 操作
}
```

---

## 相关文档

- [[设计]敌人棋子数据重构](./[设计]敌人棋子数据重构.md)
- [[总结]敌人棋子数据重构](./[总结]敌人棋子数据重构.md)
