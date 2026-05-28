## Context

项目使用 GameFramework UI 系统，所有 UI 继承 `UIFormBase`，通过 `GF.UI.OpenUI(UIViews.xxx)` / `CloseUI()` 管理。UI Prefab 在 Screen Space - Camera Canvas 下，需要手动搭建 Prefab 并运行 DataTableGenerator 生成 UITable。输入统一走 `PlayerInputManager` 单例。

GM 面板属于开发辅助工具，不需要走 GF.UI 的完整生命周期（不需要在 UITable 注册，不需要 UIViews 枚举），因为：
1. 不依赖 GameFramework 资源加载流程（简单 Prefab 直接 Instantiate）
2. 不受 UI 层级遮挡规则影响（需要永远在最顶层）
3. 减少手动配置工作量，降低展示时环境配置成本

## Goals / Non-Goals

**Goals:**
- 运行时快捷键（C 键）开关 GM 面板
- 三个功能 Tab：物品添加、策略卡&棋子解锁、局内棋子管理
- 直接复用现有 Manager 接口，不新建业务接口
- 纯代码 UI（IMGUI 或 uGUI 简单搭建），不依赖美术资源

**Non-Goals:**
- 不做美化，不需要与游戏风格一致
- 不做权限控制（不是线上功能）
- 不做撤销操作
- 不支持移动端

## Decisions

### 决策 1：使用 Unity IMGUI 而非 uGUI Prefab

**选择 IMGUI（OnGUI）**

理由：
- GM 面板是纯开发工具，不需要美术资源
- IMGUI 零配置：不需要创建 Prefab、不需要搭建 UITable、不需要用户手动操作任何编辑器步骤
- AI 可以一次性完整实现，不存在"需要用户先手动完成 Prefab"的阻塞环节
- 可随时通过 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 在发布版本中剔除

替代方案（uGUI Prefab）的问题：需要用户手动创建 Prefab → 生成 Variables → 才能写逻辑脚本，存在交付阻塞。

### 决策 2：GMPanelManager 作为独立 MonoBehaviour 单例

不通过 GF.UI 注册，直接挂载在 DontDestroyOnLoad 的 GameObject 上（由 GameProcedure 或场景启动时自动创建）。

理由：
- GM 面板需要跨场景持久存在
- 不走 GF.UI 资源加载流程，启动更快
- 避免依赖 UITable 配置

### 决策 3：物品列表在 Tab 激活时懒加载，不每帧刷新

物品列表数据从 ItemTable 读取一次后缓存，搜索过滤在缓存数据上做，不重复查询配置表。

### 决策 4：复活逻辑步骤顺序

复活必须按照 ChessLifecycleHandler.HandleChessDeath 的逆序执行：
1. 重新启用 Collider（在 SetHp 之前，防止 SetHp 触发的事件找不到碰撞体）
2. SetHp(MaxHp)
3. ChangeState(ChessState.Idle)
4. CombatEntityTracker.ReviveChess()
5. ChessDeploymentTracker.MarkChessAlive()（通过 GetInstanceIdByEntity 获取 instanceId）

### 决策 5：C 键在 GM 面板打开时不屏蔽其他输入

GM 面板打开时调用 `PlayerInputManager.Instance.RequestMouseUnlock()` 解锁鼠标，但不调用 `SetEnable(false)`，因为：
- IMGUI 本身不参与 EventSystem，不会与 uGUI 产生冲突
- 战斗输入（技能、移动）在 GM 面板打开时仍然生效，是可接受的展示行为

## Risks / Trade-offs

- **[风险] IMGUI 在高分辨率下字体偏小** → 使用 `GUI.matrix` 做全局缩放，或调整 `GUI.skin.font` 字号
- **[风险] 局内棋子 Tab 刷新时机** → 每次打开该 Tab 时重新获取棋子列表（GetAlliesIncludingDead），而非每帧轮询
- **[风险] 物品列表过长导致 IMGUI ScrollView 卡顿** → 仅渲染搜索过滤后的结果，通常在搜索后列表较短

## Open Questions

无，所有接口已确认存在。
