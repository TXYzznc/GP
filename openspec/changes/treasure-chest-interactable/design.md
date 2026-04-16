## Context

当前交互系统架构：
- `IInteractable` 接口定义交互对象契约（交互提示、优先级、动画索引、可交互判断、交互逻辑）
- `InteractableBase` 继承 MonoBehaviour 并实现 IInteractable，提供默认 Trigger Collider 检测
- `InteractionDetector` 挂在玩家角色上，负责检测范围内的交互对象、评分选择最佳目标、处理交互输入和触发
- 玩家交互由 `PlayerInteraction` 负责播放玩家侧交互动画和触发回调

宝箱作为可交互对象需要在此基础上扩展，支持两种不同的交互流程（首次开启 vs 再次查看）。

## Goals / Non-Goals

**Goals:**
- 实现 `TreasureChestInteractable` 类，继承 `InteractableBase`
- 支持 Locked/Opened 两种状态，状态间转换由首次交互触发
- 首次交互（Locked 状态）：播放动画 → 打开宝箱界面 → 设为 Opened
- 再次交互（Opened 状态）：跳过动画直接打开宝箱界面
- 动画播放过程中阻止重复交互

**Non-Goals:**
- 宝箱界面 UI 实现（占位，后续由用户完成）
- 解锁条件系统（当前无需，可扩展架构预留）
- 宝箱内容管理、掉落物品处理（业务逻辑层面）
- 动画资源、Animator Controller 配置（美术资源）

## Decisions

### Decision 1: 状态管理方案

**选择：** 在脚本内维护 `ChestState` 枚举状态（Locked/Opened）

**理由：**
- 状态单一明确，不需要外部持久化
- Locked → Opened 单向转换，无需复杂状态机
- 扩展解锁条件时只需加前置检查，无需改结构

**替代方案：**
- 用 `FSM<TreasureChestInteractable>` —— 过度工程，宝箱交互只有两种流程
- 用配置表驱动 —— 当前不需要可配置状态

### Decision 2: 动画播放机制

**选择：** 宝箱自身 `Animator.SetTrigger()` 触发，使用 `WaitUntil(normalizedTime >= 1f)` 等待完成

**理由：**
- 宝箱动画由宝箱自己控制，不依赖玩家动画
- Trigger 触发比 Play 更灵活（可配置参数）
- normalizedTime >= 1f 是标准的动画完成判定（支持多层动画混合）
- 无需侵入式的回调或事件

**替代方案：**
- 用事件系统 —— 增加耦合，而且动画播放频繁
- 用协程 —— 项目规范禁用，统一用 UniTask
- 等固定帧数 —— 不通用，动画长度可变

### Decision 3: 重复交互的处理

**选择：** `CanInteract` 返回 `!m_IsAnimating`；Opened 状态下 `CanInteract` 仍返回 true

**理由：**
- 阻止动画播放中的重复触发（保护时序）
- Opened 状态后仍可交互，打开界面（用户需求）
- 更新 `interactionTip` 从"打开宝箱" → "查看宝箱"（UX 清晰）

**替代方案：**
- Opened 后 `CanInteract` 返回 false —— 不符合用户需求（Opened 后仍可打开界面）

### Decision 4: 异步流程设计

**选择：** `OnInteract` 同步调用 `OpenChestAsync().Forget()`

**理由：**
- `OnInteract` 按接口定义不返回 UniTask（无法异步等待）
- 用 `Forget()` 表示"启动后不等待"，符合项目规范
- 使用 `GetCancellationTokenOnDestroy()` 防止宝箱销毁后继续执行
- 异步流程清晰：动画等待 → 界面打开都在 `OpenChestAsync` 中

## Risks / Trade-offs

| 风险 | 缓解方案 |
|------|---------|
| **Animator 可能不存在** | 动画部分用 `if (m_Animator != null)` 包裹，无 Animator 时跳过 |
| **动画 normalizedTime 判定不准确** | 多数游戏引擎标准用法；特殊长动画可改用事件通知 |
| **多次快速点击时序混乱** | `m_IsAnimating` 标志保护，播放动画期间禁止重新触发 |
| **UI 占位无具体实现** | 明确标注 TODO，后续补全时只需填充 `OpenChestUI()` 方法 |
| **Opened 状态后仍可交互** | 符合用户需求（打开界面看内容），非 Bug；如需变更只需改 `CanInteract` 逻辑 |

## Open Questions

- 宝箱界面具体内容（Prefab 结构、字段数据）由用户后续完成，当前占位
- 是否需要在宝箱界面打开前做额外业务逻辑（如记录首次开启日志）—— 后续扩展时在 `OpenChestAsync` 中补充
