# 游戏状态 UI

> **最后更新**: 2026-04-17
> **状态**: 有效

---

## 一、UI 分类

根据游戏状态，UI 分为四类：

| UI 类型 | 显示时机 | 监听事件 | 示例 UI |
|---------|---------|---------|---------|
| **局外 UI** | 主菜单、角色选择等 | `OutOfGameEnterEvent` / `OutOfGameLeaveEvent` | 主菜单、角色选择、设置 |
| **局内通用 UI** | 局内状态（探索+战斗） | `InGameEnterEvent` / `InGameLeaveEvent` | HP/MP、Buff图标、货币、小地图 |
| **探索 UI** | 探索状态（第三人称控制） | `ExplorationEnterEvent` / `ExplorationLeaveEvent` | 技能栏、星盘、任务追踪 |
| **战斗 UI** | 战斗状态（回合制战斗） | `CombatEnterEvent` / `CombatLeaveEvent` | 手牌、召唤师状态、回合指示 |

---

## 二、StateAwareUIForm 基类

所有状态感知 UI 继承 `StateAwareUIForm`，实现自动订阅/取消订阅事件，防止内存泄漏。

```csharp
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

    private void OnExplorationEnter(object sender, GameEventArgs e) => ShowUI();
    private void OnExplorationLeave(object sender, GameEventArgs e) => HideUI();
}
```

其他类型 UI 只需将对应的事件替换为：
- 战斗 UI：`CombatEnterEventArgs` / `CombatLeaveEventArgs`
- 局内通用：`InGameEnterEventArgs` / `InGameLeaveEventArgs`
- 局外 UI：`OutOfGameEnterEventArgs` / `OutOfGameLeaveEventArgs`

---

## 三、事件 ID 速查表

| 事件名称 | EventArgs 类名 | 触发时机 |
|---------|---------------|---------|
| OutOfGameEnterEvent | `OutOfGameEnterEventArgs` | 进入局外状态 |
| OutOfGameLeaveEvent | `OutOfGameLeaveEventArgs` | 离开局外状态 |
| InGameEnterEvent | `InGameEnterEventArgs` | 进入局内状态 |
| InGameLeaveEvent | `InGameLeaveEventArgs` | 离开局内状态 |
| ExplorationEnterEvent | `ExplorationEnterEventArgs` | 进入探索状态 |
| ExplorationLeaveEvent | `ExplorationLeaveEventArgs` | 离开探索状态 |
| CombatEnterEvent | `CombatEnterEventArgs` | 进入战斗状态 |
| CombatLeaveEvent | `CombatLeaveEventArgs` | 离开战斗状态 |
| CombatEndEvent | `CombatEndEventArgs` | 战斗结束 |

使用 `EventArgs.EventId` 而非手动填数字：
```csharp
// 正确
GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
// 错误
GF.Event.Subscribe(1001, OnExplorationEnter);
```

---

## 四、Unity 配置步骤

### 步骤 1：创建 UI 预制体

推荐目录结构：
```
Assets/AAAGame/Prefabs/UI/
├── OutOfGame/          # 局外 UI
├── InGame/             # 局内通用 UI
├── Exploration/        # 探索 UI
└── Combat/             # 战斗 UI
```

### 步骤 2：创建 UI 脚本

继承 `StateAwareUIForm`，实现 `SubscribeEvents()` 和 `UnsubscribeEvents()`。

### 步骤 3：配置预制体

**重要**：将 GameObject 的 `Active` 设置为 `false`（初始隐藏），UI 会在对应状态进入时自动显示。

### 步骤 4：注册 UI ID

在 `UIFormId.cs` 中添加 UI 的 ID 常量。

### 步骤 5：在 Procedure 中打开 UI

在 `GameProcedure.OnEnter()` 中打开需要的 UI（预制体 Active=false，打开后不会立即显示，等待事件触发）：

```csharp
// 打开局内通用 UI
GF.UI.OpenUIForm(UIFormId.PlayerStatusUI, "UI/InGame/PlayerStatusUI");

// 打开探索 UI（初始隐藏，等待事件触发）
GF.UI.OpenUIForm(UIFormId.SkillBarUI, "UI/Exploration/SkillBarUI");

// 打开战斗 UI（初始隐藏，等待事件触发）
GF.UI.OpenUIForm(UIFormId.CombatHandUI, "UI/Combat/CombatHandUI");
```

---

## 五、调试

### 添加日志

```csharp
private void OnExplorationEnter(object sender, GameEventArgs e)
{
    Log.Info("收到探索进入事件");
    ShowUI();
}
```

### 测试快捷键（在 GameProcedure 中添加）

```csharp
// F1: 切换到探索状态
if (Input.GetKeyDown(KeyCode.F1))
    GetInGameState()?.SwitchToExploration();

// F2: 切换到战斗状态
if (Input.GetKeyDown(KeyCode.F2))
    GetInGameState()?.SwitchToCombat();
```

---

## 六、常见问题

| 问题 | 原因 | 解决 |
|------|------|------|
| UI 没有自动显示 | 预制体 Active 没有设为 false | 在 Inspector 取消勾选 Active |
| 事件订阅后没有触发 | 事件 ID 不正确 | 使用 `EventArgs.EventId` |
| UI 关闭后无法再次打开 | 没有在 UnsubscribeEvents 中取消订阅 | 订阅/取消订阅必须一一对应 |
| 内存泄漏 | 事件订阅后未取消 | 始终在 UnsubscribeEvents 中取消订阅 |
| UI 没有打开 | 没有在 Procedure 中调用 OpenUIForm | 在 GameProcedure.OnEnter() 中添加 |

---

## 七、最佳实践

- 所有状态感知 UI 的预制体 `Active` 初始设为 `false`
- 订阅和取消订阅必须一一对应，防止内存泄漏
- 不需要频繁显示/隐藏的 UI 可以不使用 `StateAwareUIForm`
- 对于复杂 UI，可以在隐藏时禁用 Update 逻辑
- 关键操作（显示/隐藏）时输出日志，便于调试

---

**参考**：`游戏状态管理系统_设计方案_系统设计_2026-04-17.md`
