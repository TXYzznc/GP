# 交互系统和玩家位置问题修复

## 问题 1：可交互对象隐藏时提示未被清除

### 问题描述
- 玩家在宝箱旁边
- 进入战斗状态 → 宝箱被隐藏（`SetActive(false)`）
- ❌ 但交互提示（F 键提示）还在显示
- ❌ 按下 F 可以打开宝箱 UI

### 原因分析

在 `InteractionDetector.cs` 的 `Update()` 中：

```csharp
private void Update()
{
    CleanupCandidates();  // 清理已销毁的对象，但没有清理隐藏的对象
    
    var bestTarget = EvaluateBestTarget();
    // 即使对象被隐藏，它仍然在候选列表中
    // 评分系统仍然认为它有效
    // 提示仍然显示
}
```

问题：`CleanupCandidates()` 只检查对象是否销毁，不检查是否隐藏。

### 修复方案

修改 `CleanupCandidates()` 方法，添加隐藏对象检查：

```csharp
private void CleanupCandidates()
{
    for (int i = m_Candidates.Count - 1; i >= 0; i--)
    {
        var candidate = m_Candidates[i];

        // ⭐ 检查对象是否已销毁
        if (candidate is MonoBehaviour mb && mb == null)
        {
            m_Candidates.RemoveAt(i);
            continue;
        }

        // ⭐ 检查对象是否隐藏（SetActive(false)）
        if (candidate is MonoBehaviour mb2 && !mb2.gameObject.activeSelf)
        {
            // 如果这个对象是当前目标，需要清除目标
            if (CurrentTarget == candidate)
            {
                CurrentTarget = null;
                OnTargetChanged?.Invoke(null);
                UpdateTipVisibility();  // 隐藏提示
            }

            // 重置交互标记和描边
            var base_ = candidate as InteractableBase;
            if (base_ != null)
            {
                base_.SetInteractionStarted(false);
                base_.OnSetAsTarget(false);
            }

            m_Candidates.RemoveAt(i);
        }
    }
}
```

### 修复后的行为 ✅

```
进入战斗 → 宝箱隐藏（SetActive(false)）
    ↓
CleanupCandidates() 检测到隐藏
    ↓
从候选列表移除 ✓
取消当前目标 ✓
隐藏交互提示 ✓
重置描边 ✓
    ↓
结果：提示消失，无法交互 ✓
```

### 关键流程

```
每帧 Update()
├─ CleanupCandidates()
│  ├─ 检查对象是否销毁 → 移除
│  ├─ ⭐ 检查对象是否隐藏 → 移除 + 清空当前目标
│  └─ 重置交互标记
├─ EvaluateBestTarget()
│  └─ 只从有效的候选列表中选择
├─ UpdateTipVisibility()
│  └─ 根据当前目标决定是否显示提示
└─ HandleInteractInput()
   └─ 只有有效的目标才能交互
```

---

## 问题 2：进入游戏时是否恢复玩家位置？

### 现在的行为（已实现）

**是的，进入游戏时会恢复玩家位置。** 流程如下：

#### 初始化流程

```
启动游戏 → MainMenuProcedure
    ↓
选择存档 → GameFlowManager.EnterGame()
    ↓
加载游戏场景 → GameProcedure.OnEnter()
    ↓
PlayerCharacterManager.SpawnPlayerCharacterFromSave(callback)
    ↓
从存档数据读取玩家位置：saveData.PlayerPos
    ↓
生成玩家角色 → 恢复到记录位置 ✓
```

#### 具体代码位置

**1. GameProcedure.cs (第 68 行)**
```csharp
PlayerCharacterManager.Instance.SpawnPlayerCharacterFromSave(OnCharacterSpawned);
```

**2. PlayerCharacterManager.cs (第 87 行)**
```csharp
public void SpawnPlayerCharacterFromSave(Action<GameObject> onComplete = null)
{
    var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
    Vector3 spawnPosition = saveData.PlayerPos;  // ⭐ 读取存档位置
    SpawnCharacter(prefabConfigId, spawnPosition, onComplete);
}
```

**3. PlayerCharacterManager.cs (第 134 行)**
```csharp
CurrentPlayerCharacter = Instantiate(prefabAsset, position, Quaternion.identity);
// ⭐ 在保存的位置生成玩家
```

### 位置保存和恢复的完整生命周期

```
【游戏启动】
  ↓
【进入游戏】
  読込: saveData.PlayerPos
  ↓
  SpawnPlayerCharacter(saveData.PlayerPos)
  ↓
  玩家在记录位置生成 ✓
  
【游戏中探索】
  玩家移动...
  
【进入战斗】
  记录当前位置（由 SceneTransitionManager 完成）
  ↓
  生成战场（高度 +20）
  ↓
  移动玩家到 PlayerAnchor
  
【离开战斗】
  恢复玩家到记录位置 ✓（由 PlayerCharacterManager.RestorePositionAfterCombat() 完成）
  
【退出游戏】
  调用 PlayerCharacterManager.SaveCurrentPosition()
  ↓
  saveData.PlayerPos = 当前位置
  ↓
  PlayerAccountDataManager.SaveCurrentSave()
  ↓
  写入存档文件 ✓
```

### 位置保存时机

玩家位置会在以下时刻保存：

| 事件 | 代码位置 | 说明 |
|------|--------|------|
| **宝箱打开** | BattlePresetManager.cs | 保存位置（防止作弊读档） |
| **战场结束** | PlayerCharacterManager.SaveCurrentPosition() | 保存战后位置 |
| **手动保存** | 菜单操作 | 用户主动保存档位 |
| **场景切换** | 可选 | 需要在过渡时手动调用 |

### 确认问题

你的问题：**"是不是有一个恢复玩家位置的操作？"** 

**答案：✅ 有的。**

- **进入游戏时**：玩家从存档位置恢复（在 `SpawnPlayerCharacterFromSave()` 中）
- **离开战斗时**：玩家从记录位置恢复（在 `RestorePositionAfterCombat()` 中）
- **场景切换时**：可选（看是否在 `ChangeSceneProcedure` 中调用恢复）

### 完整的位置追踪链

```
时间线：
T0: 游戏启动，读取存档位置 = (0, 1, 5)
T1: 玩家移动到 (10, 1, 20)
T2: 触发战斗，记录位置 = (10, 1, 20)
T3: 生成战场，玩家移至 PlayerAnchor = (10, 21, 20)
T4: 战斗结束，恢复位置 = (10, 1, 20) ✓
T5: 玩家继续探索，移动到 (15, 1, 25)
T6: 退出游戏，保存位置 = (15, 1, 25)
    ↓
T0（下次启动）: 玩家在 (15, 1, 25) 生成 ✓
```

---

## 修复总结

### 修改文件

**1. InteractionDetector.cs**
- 修改 `CleanupCandidates()` 方法
- 添加隐藏对象检查
- 当对象隐藏时自动清除目标和提示

### 文件位置
- `Assets/AAAGame/Scripts/Game/Interact/InteractionDetector.cs`

### 修复前后对比

#### 修复前
```
宝箱隐藏 → 提示仍显示 → 可以按 F 打开 ❌
```

#### 修复后
```
宝箱隐藏 → 自动从候选列表移除 → 提示消失 → 无法交互 ✅
```

---

## 验证清单

### 问题 1 验证

- [ ] 玩家在宝箱旁
- [ ] 进入战斗（宝箱隐藏）
- [ ] 检查交互提示是否消失 ✓
- [ ] 按 F 无法打开宝箱 ✓
- [ ] 离开战斗，宝箱重新显示，提示恢复 ✓

### 问题 2 验证

- [ ] 启动游戏，进入场景
- [ ] 检查玩家是否在存档位置生成 ✓
- [ ] 进入战斗，离开战斗后位置恢复 ✓
- [ ] 退出游戏重启，玩家在新位置生成 ✓

---

## 补充说明

### 为什么需要隐藏对象检查？

在 `SceneTransitionManager.HideInteractives()` 中：

```csharp
private void HideInteractives()
{
    int interactiveLayer = LayerMask.NameToLayer("Interactive");
    var allObjects = Object.FindObjectsOfType<GameObject>(true);
    foreach (var obj in allObjects)
    {
        if (obj.layer == interactiveLayer)
        {
            obj.SetActive(false);  // ⭐ 进入战斗时隐藏
        }
    }
}
```

宝箱被设置为 `SetActive(false)`，但交互系统没有检查这个状态，所以需要添加检查。

### 时序图

```
播放器按 F 键
    ↓
HandleInteractInput() 检查 CurrentTarget
    ├─ 如果 CurrentTarget 被隐藏
    │  └─ ❌ 被隐藏的对象仍在 CurrentTarget （修复前）
    │  └─ ✓ 被隐藏的对象已从目标清除（修复后）
    │
    └─ 检查 CanInteract()
       ├─ ❌ 隐藏的对象仍返回 true（修复前）
       └─ ✓ 隐藏的对象无法交互（修复后）
```

---

**修复完成。** ✅
