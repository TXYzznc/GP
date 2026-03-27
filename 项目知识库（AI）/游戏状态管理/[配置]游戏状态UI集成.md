> **最后更新**: 2026-03-23
> **状态**: 有效
---

# 游戏状态管理系统 - UI 集成配置说明

[↑ 返回目录](#目录)

---

## 📋 目录

- [一、概述](#一概述)
- [二、UI分类](#二ui分类)
- [三、StateAwareUIForm基类](#三stateawareuiform基类)
- [四、Unity配置步骤](#四unity配置步骤)
- [五、不同类型UI的配置示例](#五不同类型ui的配置示例)
- [六、测试流程](#六测试流程)
- [七、常见问题](#七常见问题)
- [八、最佳实践](#八最佳实践)
- [九、下一步](#九下一步)
- [十、参考资料](#十参考资料)

---

[↑ 返回目录](#目录)

---

## 一、概述

本文档说明如何将现有 UI 集成到游戏状态管理系统中，使 UI 能够根据游戏状态自动显示/隐藏。

---

[↑ 返回目录](#目录)

---

## 二、UI 分类

根据设计方案，UI 分为以下几类：

### 1. 局外 UI
**显示时机**：在局外状态（主菜单、角色选择等）
- 主菜单 UI
- 角色选择 UI
- 设置 UI
- 存档管理 UI

**监听事件**：
- `OutOfGameEnterEvent` → 显示
- `OutOfGameLeaveEvent` → 隐藏

---

### 2. 局内通用 UI
**显示时机**：在局内状态（无论探索还是战斗）
- HP/MP 显示
- Buff 图标
- 货币显示
- 小地图

**监听事件**：
- `InGameEnterEvent` → 显示
- `InGameLeaveEvent` → 隐藏

---

### 3. 探索 UI
**显示时机**：在探索状态（第三人称控制）
- 快捷技能栏
- 星盘 UI
- 任务追踪
- 交互提示

**监听事件**：
- `ExplorationEnterEvent` → 显示
- `ExplorationLeaveEvent` → 隐藏

---

### 4. 战斗 UI
**显示时机**：在战斗状态（回合制战斗）
- 召唤师状态面板
- 手牌显示
- 战斗技能栏
- 回合指示器

**监听事件**：
- `CombatEnterEvent` → 显示
- `CombatLeaveEvent` → 隐藏

---

[↑ 返回目录](#目录)

---

## 三、StateAwareUIForm 基类

### 1. 基类说明

`StateAwareUIForm` 是所有状态感知 UI 的基类，提供了：
- 自动订阅/取消订阅事件
- 防止内存泄漏
- 统一的显示/隐藏接口

### 2. 使用方法

继承 `StateAwareUIForm` 并实现两个抽象方法：

```csharp
public class YourUI : StateAwareUIForm
{
    protected override void SubscribeEvents()
    {
        // 订阅需要监听的事件
        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    protected override void UnsubscribeEvents()
    {
        // 取消订阅（必须与订阅对应）
        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    private void OnExplorationEnter(object sender, GameEventArgs e)
    {
        ShowUI();  // 显示 UI
    }

    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        HideUI();  // 隐藏 UI
    }
}
```

---

[↑ 返回目录](#目录)

---

## 四、Unity 配置步骤

### 步骤 1：创建 UI 预制体

1. 在 Unity 中创建 UI Canvas（如果还没有）
2. 在 Canvas 下创建你的 UI 面板
3. 将 UI 面板制作成预制体
4. 将预制体放到 `Assets/AAAGame/Prefabs/UI/` 目录下

**推荐目录结构**：
```
Assets/AAAGame/Prefabs/UI/
├── OutOfGame/          # 局外 UI
│   ├── MainMenuUI.prefab
│   └── CharacterSelectUI.prefab
├── InGame/             # 局内通用 UI
│   ├── PlayerStatusUI.prefab
│   └── MiniMapUI.prefab
├── Exploration/        # 探索 UI
│   ├── SkillBarUI.prefab
│   └── StarMapUI.prefab
└── Combat/             # 战斗 UI
    ├── CombatHandUI.prefab
    └── TurnIndicatorUI.prefab
```

---

### 步骤 2：创建 UI 脚本

1. 创建 UI 脚本，继承 `StateAwareUIForm`
2. 实现 `SubscribeEvents()` 和 `UnsubscribeEvents()`
3. 根据 UI 类型，监听对应的事件

**示例：探索 UI 脚本**

```csharp
using UnityGameFramework.Runtime;
using GameFramework.Event;

/// <summary>
/// 技能栏 UI - 在探索状态下显示
/// </summary>
public class SkillBarUI : StateAwareUIForm
{
    protected override void SubscribeEvents()
    {
        // 监听探索状态进入/离开事件
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
        Log.Info("SkillBarUI: 显示技能栏");
    }

    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        HideUI();
        Log.Info("SkillBarUI: 隐藏技能栏");
    }

    // 其他 UI 逻辑...
}
```

---

### 步骤 3：配置 UI 预制体

1. 选中 UI 预制体
2. 添加你创建的 UI 脚本组件
3. 配置 UI 的初始状态：
   - **重要**：将 GameObject 的 `Active` 设置为 `false`（初始隐藏）
   - UI 会在对应状态进入时自动显示

**Inspector 配置示例**：
```
SkillBarUI (GameObject)
├── Active: ✗ (取消勾选，初始隐藏)
├── SkillBarUI (Script)
│   └── (无需配置，事件自动处理)
└── 其他组件...
```

---

### 步骤 4：注册 UI 到 GF 框架

在 `UIFormId.cs` 中添加 UI 的 ID：

```csharp
public static class UIFormId
{
    // 探索 UI
    public const int SkillBarUI = 1001;
    public const int StarMapUI = 1002;
    
    // 战斗 UI
    public const int CombatHandUI = 2001;
    public const int TurnIndicatorUI = 2002;
    
    // 局内通用 UI
    public const int PlayerStatusUI = 3001;
    public const int MiniMapUI = 3002;
    
    // 局外 UI
    public const int MainMenuUI = 4001;
    public const int CharacterSelectUI = 4002;
}
```

---

### 步骤 5：在 Procedure 中打开 UI

在 `GameProcedure.cs` 的 `OnEnter()` 中打开需要的 UI：

```csharp
protected override void OnEnter(ProcedureOwner procedureOwner)
{
    base.OnEnter(procedureOwner);
    
    // 打开局内通用 UI（始终显示）
    GF.UI.OpenUIForm(UIFormId.PlayerStatusUI, "UI/InGame/PlayerStatusUI");
    GF.UI.OpenUIForm(UIFormId.MiniMapUI, "UI/InGame/MiniMapUI");
    
    // 打开探索 UI（初始隐藏，等待事件触发）
    GF.UI.OpenUIForm(UIFormId.SkillBarUI, "UI/Exploration/SkillBarUI");
    GF.UI.OpenUIForm(UIFormId.StarMapUI, "UI/Exploration/StarMapUI");
    
    // 打开战斗 UI（初始隐藏，等待事件触发）
    GF.UI.OpenUIForm(UIFormId.CombatHandUI, "UI/Combat/CombatHandUI");
    GF.UI.OpenUIForm(UIFormId.TurnIndicatorUI, "UI/Combat/TurnIndicatorUI");
}
```

**注意**：
- UI 预制体的 `Active` 已设置为 `false`，所以打开后不会立即显示
- UI 会在对应的状态事件触发时自动显示/隐藏

---

[↑ 返回目录](#目录)

---

## 五、不同类型 UI 的配置示例

### 1. 探索 UI 示例

```csharp
/// <summary>
/// 星盘 UI - 在探索状态下显示
/// </summary>
public class StarMapUI : StateAwareUIForm
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

---

### 2. 战斗 UI 示例

```csharp
/// <summary>
/// 战斗手牌 UI - 在战斗状态下显示
/// </summary>
public class CombatHandUI : StateAwareUIForm
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
        // 初始化手牌数据
        InitializeHand();
    }

    private void OnCombatLeave(object sender, GameEventArgs e)
    {
        HideUI();
        // 清理手牌数据
        ClearHand();
    }

    private void InitializeHand()
    {
        // TODO: 加载手牌数据
    }

    private void ClearHand()
    {
        // TODO: 清理手牌数据
    }
}
```

---

### 3. 局内通用 UI 示例

```csharp
/// <summary>
/// 玩家状态 UI - 在局内状态下始终显示
/// </summary>
public class PlayerStatusUI : StateAwareUIForm
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
        // 刷新玩家数据
        RefreshPlayerData();
    }

    private void OnInGameLeave(object sender, GameEventArgs e)
    {
        HideUI();
    }

    private void RefreshPlayerData()
    {
        // TODO: 从 PlayerCharacterManager 获取数据并刷新 UI
    }
}
```

---

### 4. 局外 UI 示例

```csharp
/// <summary>
/// 主菜单 UI - 在局外状态下显示
/// </summary>
public class MainMenuUI : StateAwareUIForm
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

    // 按钮回调
    public void OnStartGameClicked()
    {
        // 切换到局内状态
        GameStateManager.Instance.SwitchToInGame();
    }
}
```

---

[↑ 返回目录](#目录)

---

## 六、测试流程

### 1. 测试探索 UI

1. 启动游戏，进入局内状态
2. 观察探索 UI 是否自动显示
3. 按 F2 键切换到战斗状态
4. 观察探索 UI 是否自动隐藏
5. 战斗结束后，观察探索 UI 是否自动显示

### 2. 测试战斗 UI

1. 在探索状态下，按 F2 键切换到战斗状态
2. 观察战斗 UI 是否自动显示
3. 等待 3 秒（战斗自动结束）
4. 观察战斗 UI 是否自动隐藏

### 3. 测试局内通用 UI

1. 启动游戏，进入局内状态
2. 观察局内通用 UI 是否显示
3. 切换探索/战斗状态
4. 观察局内通用 UI 是否始终显示
5. 返回主菜单
6. 观察局内通用 UI 是否隐藏

---

[↑ 返回目录](#目录)

---

## 七、常见问题

### Q1: UI 没有自动显示/隐藏？

**检查清单**：
1. UI 脚本是否继承了 `StateAwareUIForm`
2. 是否正确实现了 `SubscribeEvents()` 和 `UnsubscribeEvents()`
3. 事件 ID 是否正确（使用 `EventArgs.EventId`）
4. UI 预制体的初始 `Active` 是否设置为 `false`
5. 是否在 Procedure 中打开了 UI

### Q2: 如何调试事件触发？

在事件回调中添加日志：

```csharp
private void OnExplorationEnter(object sender, GameEventArgs e)
{
    Log.Info("收到探索进入事件");
    ShowUI();
}
```

### Q3: UI 关闭后无法再次打开？

确保在 `UnsubscribeEvents()` 中取消了所有订阅：

```csharp
protected override void UnsubscribeEvents()
{
    // 必须取消所有在 SubscribeEvents() 中订阅的事件
    GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
    GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
}
```

### Q4: 如何让 UI 在特定条件下显示？

可以在事件回调中添加条件判断：

```csharp
private void OnExplorationEnter(object sender, GameEventArgs e)
{
    // 只有在玩家等级 >= 10 时才显示
    if (PlayerData.Level >= 10)
    {
        ShowUI();
    }
}
```

---

[↑ 返回目录](#目录)

---

## 八、最佳实践

### 1. UI 初始状态

- 所有状态感知 UI 的预制体 `Active` 应设置为 `false`
- UI 会在对应状态进入时自动显示

### 2. 事件订阅

- 始终在 `SubscribeEvents()` 中订阅事件
- 始终在 `UnsubscribeEvents()` 中取消订阅
- 订阅和取消订阅必须一一对应

### 3. 性能优化

- 不需要频繁显示/隐藏的 UI，可以不使用 `StateAwareUIForm`
- 对于复杂 UI，可以在隐藏时禁用 Update 逻辑

### 4. 日志输出

- 在关键操作（显示/隐藏）时输出日志
- 便于调试和追踪问题

---

[↑ 返回目录](#目录)

---

## 九、下一步

完成 UI 集成后，可以继续：

1. **阶段四**：集成到现有流程（修改 GameProcedure）
2. **阶段五**：测试与优化

---

[↑ 返回目录](#目录)

---

## 十、参考资料

- 设计方案：`游戏状态管理系统_设计方案.md`
- 任务列表：`游戏状态管理系统_任务列表.md`
- GF UI 框架文档：[GameFramework UI 系统](https://gameframework.cn/)

[↑ 返回目录](#目录)
