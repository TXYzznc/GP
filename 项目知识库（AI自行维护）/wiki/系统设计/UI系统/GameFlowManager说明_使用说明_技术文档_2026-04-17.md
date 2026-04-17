# GameFlowManager 实现说明

> **最后更新**: 2026-04-17
> **状态**: 已验证有效
> **核心类**: GameFlowManager, StartGameProcedure

## 📋 目录

- [概述](#概述)
- [实现内容](#实现内容)
- [核心功能](#核心功能)
- [技术实现](#技术实现)
- [优势](#优势)
- [使用示例](#使用示例)
- [注意事项](#注意事项)

---


创建了一个优雅的 `GameFlowManager` 静态类来集中管理游戏流程，消除了多个 UI 脚本中重复的 `EnterGame()` 方法。

## 实现内容

### 1. 新增文件

- **Assets/AAAGame/Scripts/Manager/GameFlowManager.cs**
  - 静态类设计的游戏流程管理器
  - 提供统一的游戏流程控制接口
  - 通过 `StartGameProcedure.RequestChangeScene()` 触发场景切换

- **Assets/AAAGame/Scripts/EventArgs/ChangeSceneEventArgs.cs**
  - 场景切换事件参数（预留，暂未使用）

### 2. 修改文件

- **Assets/AAAGame/Scripts/Procedures/StartGameProcedure.cs**
  - 添加了静态方法 `RequestChangeScene()` 用于从外部触发场景切换
  - 保存了 FSM 引用，以便在 Procedure 内部调用 `ChangeState`

- **Assets/AAAGame/Scripts/Procedures/GameProcedure.cs**
  - 实现了完整的游戏流程逻辑
  - 添加了事件订阅和清理
  - 提供返回主菜单和重新开始功能

- **Assets/AAAGame/Scripts/Procedures/ChangeSceneProcedure.cs**
  - 添加了对 "Test" 场景的支持
  - 加载 Test 场景后自动切换到 GameProcedure

- **Assets/AAAGame/Scripts/UI/StartMenuUI.cs**
  - 重构 `EnterGame()` 方法，调用 `GameFlowManager.EnterGame()`
  - 重构 `QuitGame()` 方法，调用 `GameFlowManager.QuitGame()`

- **Assets/AAAGame/Scripts/UI/LoadGameUI.cs**
  - 重构 `EnterGame()` 方法，调用 `GameFlowManager.EnterGame()`

- **Assets/AAAGame/Scripts/UI/NewGameUI.cs**
  - 重构 `EnterGame()` 方法，调用 `GameFlowManager.EnterGame()`

[↑ 返回目录](#目录)

---

## 核心功能

### GameFlowManager 提供的接口

```csharp
// 进入游戏（加载 Test 场景并切换到 GameProcedure）
GameFlowManager.EnterGame();

// 切换场景
GameFlowManager.ChangeScene("SceneName");

// 返回主菜单
GameFlowManager.BackToMenu();

// 退出游戏
GameFlowManager.QuitGame();
```

[↑ 返回目录](#目录)

---

## 技术实现

### 流程切换方式

由于 `ChangeState` 是 `ProcedureBase` 的 protected 方法，只能在 Procedure 内部调用，因此采用以下方案：

1. **GameFlowManager** 调用 `StartGameProcedure.RequestChangeScene(sceneName)`
2. **StartGameProcedure** 在内部调用 `ChangeState<ChangeSceneProcedure>(fsm)`
3. **ChangeSceneProcedure** 加载场景完成后，根据场景名称自动切换到对应的 Procedure

```csharp
// GameFlowManager.cs
public static void ChangeScene(string sceneName)
{
    GFBuiltin.BuiltinView.ShowLoadingProgress();
    StartGameProcedure.RequestChangeScene(sceneName);
}

// StartGameProcedure.cs
public static void RequestChangeScene(string sceneName)
{
    s_ProcedureOwner.SetData<VarString>(ChangeSceneProcedure.P_SceneName, sceneName);
    var currentProcedure = s_ProcedureOwner.CurrentState as ProcedureBase;
    currentProcedure.ChangeState<ChangeSceneProcedure>(s_ProcedureOwner);
}
```

### 流程图

```
UI 脚本
  ↓
GameFlowManager.EnterGame()
  ↓
GameFlowManager.ChangeScene("Test")
  ↓
StartGameProcedure.RequestChangeScene("Test")
  ↓
ChangeSceneProcedure (加载 Test 场景)
  ↓
GameProcedure (自动切换)
```

[↑ 返回目录](#目录)

---

## 优势

1. **代码复用**：消除了三个 UI 脚本中的重复代码
2. **集中管理**：所有游戏流程逻辑集中在一个地方，易于维护
3. **静态类设计**：无需实例化，任何地方都可以直接调用
4. **符合框架**：正确使用了 GameFramework 的 Procedure 系统
5. **职责分离**：
   - GameFlowManager 负责提供简洁的 API
   - StartGameProcedure 负责触发流程切换
   - ChangeSceneProcedure 负责加载场景并自动切换到目标 Procedure

[↑ 返回目录](#目录)

---

## 使用示例

在任何需要进入游戏的地方，只需一行代码：

```csharp
GameFlowManager.EnterGame();
```

这会自动：
1. 打印当前存档信息（用于调试）
2. 显示加载进度界面
3. 通过 StartGameProcedure 触发场景切换
4. ChangeSceneProcedure 加载 Test 场景
5. 场景加载完成后自动切换到 GameProcedure
6. 开始游戏

[↑ 返回目录](#目录)

---

## 注意事项

- GameFlowManager 是静态类，无需实例化
- 场景切换必须在 StartGameProcedure 激活时才能使用
- ChangeSceneProcedure 会根据场景名称自动切换到对应的 Procedure
- 确保场景名称正确（当前配置为 "Test"）
- GameProcedure 中的 TODO 部分需要根据实际游戏需求补充

[↑ 返回目录](#目录)
