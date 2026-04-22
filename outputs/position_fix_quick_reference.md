# 玩家位置修复 - 快速参考

## 核心问题
战场生成和玩家位置移动的时序不对，导致玩家位置计算错误。

## 修复的关键点

### 1️⃣ 进入战斗的新流程

```
CombatPreparationState.OnEnter()
  ↓
  SetupCombatCamera()
  ↓
  SceneTransitionManager.PrepareBeforeArenaSpawn()  ← 新：记录位置前的准备
    - 记录玩家位置和旋转（此时战场还不存在）
    - 隐藏敌人和交互物体
    - 标记玩家进入战斗
  ↓
  [UI初始化和其他准备工作...]
  ↓
  SpawnBattleArenaAsync()  ← 在这里异步生成战场
    ↓ 战场生成完成后
    ↓
    SceneTransitionManager.FinalizeAfterArenaSpawn()  ← 新：战场生成后的处理
      - 将玩家移动到 PlayerAnchor（战场现在存在）
      - 玩家朝向与 PlayerAnchor 一致
      - 播放溶解过渡效果
    ↓
    StartCameraSmoothMove()  ← 相机移动
    ↓
    LoadEnemyWaveConfig()    ← 加载敌人
```

### 2️⃣ 离开战斗的流程（已有，保持不变）

```
CombatState.OnLeave()
  ↓
  [同步清理工作...]
  ↓
  OnLeaveRestoreAsync()  ← 异步恢复
    ↓
    PlayerCharacterManager.RestorePositionAfterCombat()
      - 恢复玩家到记录的位置和旋转
    ↓
    SceneTransitionManager.ExitCombatAsync()
      - 显示敌人
      - 显示交互物体
      - 播放溶解过渡显示原场景
    ↓
    [恢复相机视角...]
```

## 关键代码变化

### SceneTransitionManager 新增方法

**RecordPlayerStateBeforeCombat()**
```csharp
// 记录玩家当前位置和旋转（战场生成前）
m_PlayerPositionBeforeCombat = playerGo.transform.position;
m_PlayerRotationBeforeCombat = playerGo.transform.rotation;
```

**PrepareBeforeArenaSpawn()**
```csharp
// 战场生成前调用
public void PrepareBeforeArenaSpawn()
{
    RecordPlayerStateBeforeCombat();  // 记录当前状态
    // ... 隐藏敌人、交互物体等
}
```

**FinalizeAfterArenaSpawn()**
```csharp
// 战场生成后调用
public async UniTask FinalizeAfterArenaSpawn()
{
    MovePlayerToArena();  // 战场现在存在，可以正确获取 PlayerAnchor
    await DissolveTransitionManager.Instance.TransitionToBattle(battleArena);
}
```

### MovePlayerToArena() 改进

**旧版本的问题：**
- 在战场不存在时被调用
- 没有同步玩家朝向
- 没有正确处理 CharacterController

**新版本的改进：**
```csharp
private void MovePlayerToArena()
{
    // ... 获取 PlayerAnchor ...
    
    // 使用 TeleportTo 正确处理 CharacterController
    PlayerController controller = playerGo.GetComponent<PlayerController>();
    if (controller != null)
    {
        // 同时设置位置和朝向（与 PlayerAnchor 一致）
        Vector3 targetForward = playerAnchor.forward;
        controller.TeleportTo(playerAnchor.position, targetForward);
    }
}
```

### RestorePlayerPosition() 改进

**新版本同时恢复位置和旋转：**
```csharp
private void RestorePlayerPosition()
{
    PlayerController controller = playerGo.GetComponent<PlayerController>();
    if (controller != null)
    {
        Vector3 forward = m_PlayerRotationBeforeCombat * Vector3.forward;
        controller.TeleportTo(m_PositionBeforeCombat, forward);
    }
}
```

## 调用点总结

| 方法 | 调用位置 | 作用 |
|------|--------|------|
| `PrepareBeforeArenaSpawn()` | `CombatPreparationState.ContinueCombatPreparationInitAsync()` | 战场生成前准备 |
| `FinalizeAfterArenaSpawn()` | `CombatPreparationState.SpawnBattleArenaAsync()` | 战场生成后最终化 |
| `ExitCombatAsync()` | `CombatState.OnLeaveRestoreAsync()` | 离开战斗恢复 |

## 验证清单

- [ ] 进入战斗时，玩家位置正确移动到 PlayerAnchor
- [ ] 玩家离开战斗后，位置准确恢复到原位置
- [ ] 玩家朝向与战场 PlayerAnchor 方向一致
- [ ] 多次进入/离开战斗，位置不出现累积偏差
- [ ] CharacterController 移动正常（没有卡顿）
- [ ] 溶解过渡效果正确播放

## 技术债务清理

- 移除了 `CombatPreparationState` 中重复的 `RecordPositionBeforeCombat()` 调用
- 将 `EnterCombatAsync()` 标记为 Obsolete（保持向后兼容）
- 分离了战场生成前后的逻辑，降低耦合
