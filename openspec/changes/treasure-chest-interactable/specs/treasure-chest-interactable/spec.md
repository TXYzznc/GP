## ADDED Requirements

### Requirement: Treasure chest state management

宝箱需维护两种状态（Locked 和 Opened），初始为 Locked，首次交互后转为 Opened。

#### Scenario: Chest starts in locked state
- **WHEN** 宝箱 GameObject 被创建并挂上 TreasureChestInteractable 脚本
- **THEN** 宝箱初始状态为 Locked，交互提示显示"打开宝箱"

#### Scenario: Chest transitions to opened after first interaction
- **WHEN** 玩家与 Locked 状态的宝箱交互，并且动画播放完成
- **THEN** 宝箱状态变为 Opened，交互提示变为"查看宝箱"

#### Scenario: Chest remains opened after transition
- **WHEN** 宝箱已是 Opened 状态
- **THEN** 再次交互时保持 Opened 状态，不会回到 Locked

### Requirement: Animation playback for initial interaction

首次交互时，宝箱自身播放开箱动画。

#### Scenario: Play animation when chest is locked
- **WHEN** 玩家与 Locked 状态的宝箱交互
- **THEN** 系统调用 `Animator.SetTrigger("Open")` 触发宝箱动画

#### Scenario: Animation is optional
- **WHEN** 宝箱上无 Animator 组件
- **THEN** 跳过动画播放，继续后续逻辑

#### Scenario: Wait for animation to complete
- **WHEN** 动画开始播放
- **THEN** 系统等待 Animator 的 normalizedTime >= 1.0f 后再执行后续逻辑

#### Scenario: Skip animation on subsequent interactions
- **WHEN** 玩家与 Opened 状态的宝箱交互
- **THEN** 系统不播放任何动画，直接执行打开界面逻辑

### Requirement: Treasure chest interface opening

交互完成后弹出宝箱界面供玩家查看。

#### Scenario: Open interface after locked state interaction
- **WHEN** 玩家与 Locked 状态的宝箱交互，动画播放完成
- **THEN** 系统打开宝箱界面

#### Scenario: Open interface on subsequent interactions
- **WHEN** 玩家与 Opened 状态的宝箱交互
- **THEN** 系统打开宝箱界面

### Requirement: Prevent repeated interaction during animation

动画播放过程中禁止重复交互，保护时序。

#### Scenario: Block interaction during animation playback
- **WHEN** 宝箱正在播放开箱动画（`m_IsAnimating == true`）
- **THEN** `CanInteract()` 返回 false，玩家无法再次触发交互

#### Scenario: Allow interaction after animation completes
- **WHEN** 动画播放完成（`m_IsAnimating == false`）
- **THEN** `CanInteract()` 返回 true，玩家可以再次交互

### Requirement: Player animation handling

玩家侧不播放交互动画。

#### Scenario: No player animation triggered
- **WHEN** 玩家与宝箱交互
- **THEN** `InteractAnimIndex` 返回 -1，玩家不播放任何交互动画

### Requirement: Cancellation token handling

异步操作使用 cancellation token 防止对象销毁后继续执行。

#### Scenario: Cancel pending animation wait on destruction
- **WHEN** 宝箱正在等待动画完成，且宝箱 GameObject 被销毁
- **THEN** 异步任务自动取消，不会继续执行

### Requirement: Interactable interface integration

遵循 InteractableBase 接口规范，正确集成到交互系统。

#### Scenario: Detected by interaction detector
- **WHEN** 玩家进入宝箱的检测范围
- **THEN** InteractionDetector 检测到宝箱并添加到候选列表

#### Scenario: Evaluated for best target
- **WHEN** 多个交互对象在范围内
- **THEN** 宝箱参与评分计算，根据优先级和距离被选中
