> **最后更新**: 2026-03-23
> **状态**: 有效
---

# 游戏状态管理系统 - UI 快速配置参考

## 📋 目录

- [一、UI 类型与事件对应表](#一、ui-类型与事件对应表)
- [二、快速配置步骤](#二、快速配置步骤)
- [三、事件 ID 速查表](#三、事件-id-速查表)
- [四、常用代码片段](#四、常用代码片段)
- [五、调试技巧](#五、调试技巧)
- [六、测试快捷键](#六、测试快捷键)
- [七、常见错误](#七、常见错误)
- [八、完整示例](#八、完整示例)
- [九、下一步](#九、下一步)
- [十、参考文档](#十、参考文档)

---

| **局外 UI** | 主菜单、角色选择 | `OutOfGameEnterEvent`<br>`OutOfGameLeaveEvent` | 主菜单、角色选择、设置 |
| **局内通用 UI** | 局内状态（探索+战斗） | `InGameEnterEvent`<br>`InGameLeaveEvent` | HP/MP、Buff、货币、小地图 |
| **探索 UI** | 探索状态 | `ExplorationEnterEvent`<br>`ExplorationLeaveEvent` | 技能栏、星盘、任务追踪 |
| **战斗 UI** | 战斗状态 | `CombatEnterEvent`<br>`CombatLeaveEvent` | 手牌、召唤师状态、回合指示 |

---

## 二、快速配置步骤

### 步骤 1：创建 UI 脚本模板

根据 UI 类型，复制对应的模板代码：

#### 探索 UI 模板

```csharp
using UnityGameFramework.Runtime;
using GameFramework.Event;

public class YourExplorationUI : StateAwareUIForm
{
    protected override void SubscribeEvents()
    {
        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    protected override void UnsubscribeEvents()
    {
        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    private void OnExplorationEnter(object sender, GameEventArgs e)
    {
        ShowUI();
    }

    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        HideUI();
    }
}
```

#### 战斗 UI 模板

```csharp
using UnityGameFramework.Runtime;
using GameFramework.Event;

public class YourCombatUI : StateAwareUIForm
{
    protected override void SubscribeEvents()
    {
        GF.Event.Subscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Subscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);
    }

    protected override void UnsubscribeEvents()
    {
        GF.Event.Unsubscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Unsubscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);
    }

    private void OnCombatEnter(object sender, GameEventArgs e)
    {
        ShowUI();
    }

    private void OnCombatLeave(object sender, GameEventArgs e)
    {
        HideUI();
    }
}
```

#### 局内通用 UI 模板

```csharp
using UnityGameFramework.Runtime;
using GameFramework.Event;

public class YourInGameUI : StateAwareUIForm
{
    protected override void SubscribeEvents()
    {
        GF.Event.Subscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Subscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);
    }

    protected override void UnsubscribeEvents()
    {
        GF.Event.Unsubscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Unsubscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);
    }

    private void OnInGameEnter(object sender, GameEventArgs e)
    {
        ShowUI();
    }

    private void OnInGameLeave(object sender, GameEventArgs e)
    {
        HideUI();
    }
}
```

#### 局外 UI 模板

```csharp
using UnityGameFramework.Runtime;
using GameFramework.Event;

public class YourOutOfGameUI : StateAwareUIForm
{
    protected override void SubscribeEvents()
    {
        GF.Event.Subscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Subscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    protected override void UnsubscribeEvents()
    {
        GF.Event.Unsubscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Unsubscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    private void OnOutOfGameEnter(object sender, GameEventArgs e)
    {
        ShowUI();
    }

    private void OnOutOfGameLeave(object sender, GameEventArgs e)
    {
        HideUI();
    }
}
```

---

### 步骤 2：Unity Inspector 配置

1. 选中 UI 预制体
2. 添加你创建的 UI 脚本组件
3. **重要**：取消勾选 GameObject 的 `Active`（初始隐藏）

```
Inspector 配置：
┌─────────────────────────────┐
│ YourUI (GameObject)         │
│ ☐ Active (取消勾选)         │
├─────────────────────────────┤
│ YourUI (Script)             │
│ (无需配置)                  │
└─────────────────────────────┘
```

---

### 步骤 3：在 Procedure 中打开 UI

在 `GameProcedure.OnEnter()` 中添加：

```csharp
// 打开 UI（初始隐藏，等待事件触发）
GF.UI.OpenUIForm(UIFormId.YourUI, "UI/YourUI");
```

---

[↑ 返回目录](#目录)

---

## 三、事件 ID 速查表

| 事件名称 | 事件 ID | 触发时机 |
|---------|---------|---------|
| `OutOfGameEnterEvent` | `OutOfGameEnterEventArgs.EventId` | 进入局外状态 |
| `OutOfGameLeaveEvent` | `OutOfGameLeaveEventArgs.EventId` | 离开局外状态 |
| `InGameEnterEvent` | `InGameEnterEventArgs.EventId` | 进入局内状态 |
| `InGameLeaveEvent` | `InGameLeaveEventArgs.EventId` | 离开局内状态 |
| `ExplorationEnterEvent` | `ExplorationEnterEventArgs.EventId` | 进入探索状态 |
| `ExplorationLeaveEvent` | `ExplorationLeaveEventArgs.EventId` | 离开探索状态 |
| `CombatEnterEvent` | `CombatEnterEventArgs.EventId` | 进入战斗状态 |
| `CombatLeaveEvent` | `CombatLeaveEventArgs.EventId` | 离开战斗状态 |
| `CombatEndEvent` | `CombatEndEventArgs.EventId` | 战斗结束 |

---

[↑ 返回目录](#目录)

---

## 四、常用代码片段

### 1. 订阅事件

```csharp
GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
```

### 2. 取消订阅事件

```csharp
GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
```

### 3. 显示 UI

```csharp
ShowUI();
```

### 4. 隐藏 UI

```csharp
HideUI();
```

### 5. 添加日志

```csharp
Log.Info("YourUI: 显示");
```

### 6. 条件显示

```csharp
private void OnExplorationEnter(object sender, GameEventArgs e)
{
    if (SomeCondition)
    {
        ShowUI();
    }
}
```

---

[↑ 返回目录](#目录)

---

## 五、调试技巧

### 1. 添加日志输出

```csharp
private void OnExplorationEnter(object sender, GameEventArgs e)
{
    Log.Info("收到探索进入事件");
    ShowUI();
    Log.Info("UI 已显示");
}
```

### 2. 检查事件是否触发

在 `SubscribeEvents()` 中添加日志：

```csharp
protected override void SubscribeEvents()
{
    Log.Info("订阅探索状态事件");
    GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
    GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
}
```

### 3. 检查 UI 是否正确隐藏

在 `OnExplorationLeave` 中添加日志：

```csharp
private void OnExplorationLeave(object sender, GameEventArgs e)
{
    Log.Info("收到探索离开事件");
    HideUI();
    Log.Info("UI 已隐藏");
}
```

---

[↑ 返回目录](#目录)

---

## 六、测试快捷键

在 `GameProcedure` 中添加测试代码（任务 4.3）：

```csharp
private void Update()
{
    // F1: 切换到探索状态
    if (Input.GetKeyDown(KeyCode.F1))
    {
        InGameState inGameState = GetInGameState();
        inGameState?.SwitchToExploration();
    }

    // F2: 切换到战斗状态
    if (Input.GetKeyDown(KeyCode.F2))
    {
        InGameState inGameState = GetInGameState();
        inGameState?.SwitchToCombat();
    }
}
```

---

[↑ 返回目录](#目录)

---

## 七、常见错误

### ❌ 错误 1：UI 没有自动显示

**原因**：预制体的 `Active` 没有设置为 `false`

**解决**：在 Inspector 中取消勾选 GameObject 的 `Active`

---

### ❌ 错误 2：事件订阅后没有触发

**原因**：事件 ID 不正确

**解决**：使用 `EventArgs.EventId` 而不是手动输入 ID

```csharp
// ❌ 错误
GF.Event.Subscribe(1001, OnExplorationEnter);

// ✅ 正确
GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
```

---

### ❌ 错误 3：UI 关闭后无法再次打开

**原因**：没有在 `UnsubscribeEvents()` 中取消订阅

**解决**：确保订阅和取消订阅一一对应

```csharp
protected override void UnsubscribeEvents()
{
    // 必须取消所有订阅
    GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
    GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
}
```

---

### ❌ 错误 4：内存泄漏

**原因**：事件订阅后没有取消订阅

**解决**：始终在 `UnsubscribeEvents()` 中取消订阅

---

[↑ 返回目录](#目录)

---

## 八、完整示例

### 示例：技能栏 UI

**脚本**：`SkillBarUI.cs`

```csharp
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.Event;

/// <summary>
/// 技能栏 UI - 在探索状态下显示
/// </summary>
public class SkillBarUI : StateAwareUIForm
{
    [Header("UI 组件")]
    [SerializeField] private GameObject[] skillSlots;

    protected override void SubscribeEvents()
    {
        Log.Info("SkillBarUI: 订阅探索状态事件");
        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    protected override void UnsubscribeEvents()
    {
        Log.Info("SkillBarUI: 取消订阅探索状态事件");
        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    private void OnExplorationEnter(object sender, GameEventArgs e)
    {
        Log.Info("SkillBarUI: 收到探索进入事件");
        ShowUI();
        RefreshSkills();
    }

    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        Log.Info("SkillBarUI: 收到探索离开事件");
        HideUI();
    }

    private void RefreshSkills()
    {
        // TODO: 刷新技能栏数据
        Log.Info("SkillBarUI: 刷新技能栏");
    }
}
```

**Unity 配置**：
1. 创建 UI 预制体：`Assets/AAAGame/Prefabs/UI/Exploration/SkillBarUI.prefab`
2. 添加 `SkillBarUI` 脚本
3. 取消勾选 GameObject 的 `Active`
4. 在 `GameProcedure.OnEnter()` 中打开：
   ```csharp
   GF.UI.OpenUIForm(UIFormId.SkillBarUI, "UI/Exploration/SkillBarUI");
   ```

---

[↑ 返回目录](#目录)

---

## 九、下一步

完成 UI 配置后：

1. 测试 UI 自动显示/隐藏
2. 继续阶段四：集成到现有流程
3. 继续阶段五：测试与优化

---

[↑ 返回目录](#目录)

---

## 十、参考文档

- 详细配置说明：`游戏状态管理系统_UI集成配置说明.md`
- 设计方案：`游戏状态管理系统_设计方案.md`
- 任务列表：`游戏状态管理系统_任务列表.md`

[↑ 返回目录](#目录)
