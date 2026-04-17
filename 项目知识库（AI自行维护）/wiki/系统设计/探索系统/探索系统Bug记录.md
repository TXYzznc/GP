> **最后更新**: 2026-04-17
> **状态**: 有效
> **分类**: Bug修复

---

# 探索系统 Bug 记录

合并自：NavMesh重新激活后立即调用问题 + 战败返回后立即被触发战斗 + 探索系统广播机制问题

---

## Bug 1：NavMesh 重新激活后立即调用报错

**严重程度**：中等 | **修复状态**：已修复

### 症状

- Console 报错：`"Resume" can only be called on an active agent that has been placed on a NavMesh.`
- 敌人实体恢复后 AI 停止运作，卡在战斗状态无法回到 Idle

### 复现步骤

1. 触发与敌人战斗
2. `HideEntityForCombat()` 调用 `SetActive(false)`
3. 玩家失败，`RestoreEntityAfterCombat()` 调用 `SetActive(true)` 后紧接着调用 NavAgent 方法

### 根本原因

`SetActive(false)` 将 NavMeshAgent 从 NavMesh 移除；`SetActive(true)` 后需要等至少一帧 NavMesh 才重新就绪，**同帧调用任何 NavAgent 方法均会报错**。

重构前使用 `Destroy()` 而非 `SetActive`，此问题从未出现。

### 修复

在所有 NavAgent 操作前加 `isOnNavMesh` 守卫：

```csharp
// ✅ 统一写法
if (navAgent != null && navAgent.isOnNavMesh)
{
    navAgent.isStopped = value;
}
```

**修改的文件**：
- `EnemyEntity.cs` — `ExitCombat()` 中 NavAgent 操作
- `EnemyEntityManager.cs` — `HideEntityForCombat()` 和 `RestoreEntityAfterCombat()`
- `EnemyIdleState.cs` — `OnEnter()` 中 NavAgent 操作
- `EnemyCombatState.cs` — `OnExit()` 中 NavAgent 操作

### 预防措施

**凡涉及 `SetActive(false/true)` 的 NavMeshAgent 使用场景，必须先检查 `isOnNavMesh`，不要假设 `SetActive(true)` 后 NavMeshAgent 立即可用。**

---

## Bug 2：战败返回后立即被再次触发战斗

**严重程度**：中等 | **修复状态**：已修复

### 症状

玩家战斗失败后返回探索场景，当帧或次帧立刻进入新一轮战斗准备，体验极差。

### 根本原因

`ResetAlert()` 将警觉度清零后，如果玩家仍处于敌人检测范围内（玩家位置未改变），`VisionConeDetector.UpdateDetection()` 下一帧继续执行，快速重新积累警觉度，几帧内即达到触发阈值。

```
RestoreEntityAfterCombat() → SetActive(true) → AI 恢复
→ VisionConeDetector 检测到玩家（还在原地）
→ AlertLevel 快速升高
→ 切换到 Alert 状态 → 触发 TriggerCombat()
```

重构前敌人在战斗失败时未被恢复，此问题从未出现。

### 修复

给 `EnemyEntity` 增加 5 秒战斗冷却期，Idle 和 Patrol 状态在冷却期内跳过玩家检测：

```csharp
// EnemyEntity.cs
private float m_CombatCooldownEndTime;
private const float k_CombatCooldownDuration = 5f;

public bool IsCombatCooldownOver => Time.time >= m_CombatCooldownEndTime;

public void ExitCombat()
{
    m_IsInCombat = false;
    m_CombatCooldownEndTime = Time.time + k_CombatCooldownDuration;
    // ...
}
```

```csharp
// EnemyIdleState.OnUpdate() 和 EnemyPatrolState.OnUpdate()
if (m_AI.IsPlayerDetected && m_AI.Entity.IsCombatCooldownOver)
{
    m_AI.ChangeState(EnemyAIState.Alert);
}
```

> **后续改进计划**：改成当玩家离开战斗时给玩家一个持续 20 秒的无法被发现效果（替代当前固定 5 秒冷却）

---

## Bug 3：广播机制死循环（探索系统广播）

**严重程度**：中等 | **修复状态**：已修复

### 症状

敌人在 Chase 和 Broadcast 状态之间无限循环切换：`Chase → Broadcast → Chase → Broadcast → ...`，无法正常进入战斗。

### 根本原因

**设计错误**：广播被设计成一个独立的 `EnemyBroadcastState`，但广播本质上是瞬间行为，不应占用一个独立状态。

Chase → Broadcast（广播一次）→ 检测到仍在 Chase 距离内 → 切回 Chase → 距离又达到广播触发 → 无限循环。

**附加问题**：`BroadcastPlayerPosition()` 调用时缺少 `broadcastRange` 参数。

### 修复

删除 `EnemyBroadcastState`，将广播逻辑整合进 `EnemyChaseState`，用 `m_HasBroadcasted` 标志确保只广播一次：

```csharp
// EnemyChaseState
private bool m_HasBroadcasted = false;

public override void OnUpdate()
{
    float distance = Vector3.Distance(...);

    // 广播（只执行一次）
    if (!m_HasBroadcasted && m_AI.Entity.Config.IsElite && distance <= m_AI.Entity.Config.BroadcastDistance)
    {
        EnemyGroupManager.Instance?.BroadcastPlayerPosition(
            m_AI.Entity, m_AI.PlayerTransform.position, m_AI.Entity.Config.BroadcastDistance);
        m_HasBroadcasted = true;
    }

    // 进入战斗距离
    if (distance <= m_AI.Entity.Config.CombatDistance)
    {
        m_AI.ChangeState(EnemyStateType.Combat);
    }
}

public override void OnExit()
{
    m_HasBroadcasted = false;
}
```

**修复后状态流**：`Idle → Alert → Chase（含广播行为）→ Combat`

### 经验总结

- **状态应该是持续的**：瞬间完成的行为不应设计为独立状态
- **状态切换条件要互斥**：避免条件重叠导致来回切换

---

## 相关文件

- `Assets/AAAGame/Scripts/Game/Explore/Enemy/Core/EnemyEntity.cs`
- `Assets/AAAGame/Scripts/Game/Explore/Enemy/Core/EnemyEntityManager.cs`
- `Assets/AAAGame/Scripts/Game/Explore/Enemy/State/EnemyIdleState.cs`
- `Assets/AAAGame/Scripts/Game/Explore/Enemy/State/EnemyPatrolState.cs`
- `Assets/AAAGame/Scripts/Game/Explore/Enemy/State/EnemyCombatState.cs`
- `Assets/AAAGame/Scripts/Game/Explore/Enemy/EnemyChaseState.cs`
- `Assets/AAAGame/Scripts/Game/Explore/Enemy/EnemyGroupManager.cs`
