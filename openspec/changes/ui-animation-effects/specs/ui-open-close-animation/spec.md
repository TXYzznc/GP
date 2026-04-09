## ADDED Requirements

### Requirement: UIAnimationHelper 工具类
系统 SHALL 提供 `UIAnimationHelper` 静态工具类，封装所有通用 UI 动画模板。所有 UI 动画 SHALL 使用此工具类的方法实现，确保风格统一。

#### Scenario: 淡入动画
- **WHEN** 调用 `UIAnimationHelper.FadeIn(canvasGroup, duration)`
- **THEN** CanvasGroup.alpha 从 0 过渡到 1，使用 Ease.OutQuart 缓动

#### Scenario: 淡出动画
- **WHEN** 调用 `UIAnimationHelper.FadeOut(canvasGroup, duration)`
- **THEN** CanvasGroup.alpha 从 1 过渡到 0，使用 Ease.InQuart 缓动

#### Scenario: 方向滑入动画
- **WHEN** 调用 `UIAnimationHelper.SlideIn(rt, direction, offset, duration)`
- **THEN** RectTransform 从指定方向偏移位置滑动到原始位置，使用 Ease.OutQuart

#### Scenario: 缩放弹出动画
- **WHEN** 调用 `UIAnimationHelper.PopIn(rt, canvasGroup, duration)`
- **THEN** scale 从 0.85 到 1 同时 alpha 从 0 到 1，使用 Ease.OutQuart

#### Scenario: 缩放收回动画
- **WHEN** 调用 `UIAnimationHelper.PopOut(rt, canvasGroup, duration)`
- **THEN** scale 从 1 到 0.85 同时 alpha 从 1 到 0，使用 Ease.InQuart

#### Scenario: 子元素依次入场
- **WHEN** 调用 `UIAnimationHelper.StaggerChildren(parent, staggerDelay, duration)`
- **THEN** parent 下的每个活跃子元素按顺序以 staggerDelay 间隔执行淡入+上滑入场动画

### Requirement: 全屏页面打开/关闭动画
StartMenuUI、MenuUIForm、LoadGameUI、NewGameUI、GameUIForm SHALL 在打开时播放入场动画，关闭时播放退场动画。

#### Scenario: StartMenuUI 打开
- **WHEN** StartMenuUI 被打开
- **THEN** 背景先淡入(0.4s)，然后标题 Logo 从 scale 0 缩放到 1(0.5s)，最后按钮组从底部依次滑入(stagger 0.08s)

#### Scenario: StartMenuUI 关闭
- **WHEN** StartMenuUI 被关闭
- **THEN** 整体淡出(0.3s)，动画完成后才执行真正关闭

#### Scenario: LoadGameUI 打开
- **WHEN** LoadGameUI 被打开
- **THEN** 面板缩放弹出 PopIn(0.3s)，存档列表项依次入场(stagger 0.06s)

#### Scenario: MenuUIForm/GameUIForm 打开
- **WHEN** MenuUIForm 或 GameUIForm 被打开
- **THEN** 整体淡入(0.3s) + 内容区域从底部轻微滑入

#### Scenario: NewGameUI 步骤切换
- **WHEN** NewGameUI 从一个步骤切换到下一步
- **THEN** 当前步骤淡出+左滑(0.25s)，新步骤淡入+右滑入(0.3s)

### Requirement: 战斗系统 UI 动画
CombatPreparationUI、CombatUI、GameOverUIForm、EscapeResultUI SHALL 具有强调感的入场/退场动画。

#### Scenario: CombatPreparationUI 打开
- **WHEN** CombatPreparationUI 被打开
- **THEN** 整体淡入(0.3s)，棋子面板从底部滑入(0.35s)，装备面板从右侧滑入(0.35s)

#### Scenario: CombatUI 显示
- **WHEN** CombatUI 进入战斗状态显示
- **THEN** 顶部敌人信息从上方滑入(0.3s)，底部卡牌区从下方滑入(0.35s)，左侧 HP/MP 条从左滑入(0.3s)，三者同时进行

#### Scenario: GameOverUIForm 打开
- **WHEN** GameOverUIForm 被打开
- **THEN** 背景遮罩淡入(0.3s)，标题文本缩放弹入(0.4s, scale 1.2→1)，按钮淡入+上滑(0.3s)，按序列播放

#### Scenario: EscapeResultUI 打开和自动关闭
- **WHEN** EscapeResultUI 被打开
- **THEN** 从屏幕上方滑入+淡入(0.3s)
- **WHEN** EscapeResultUI 自动关闭时
- **THEN** 向上滑出+淡出(0.25s)，而非直接消失

### Requirement: 面板/弹窗滑入动画
InventoryUI、WarehouseUI、CloudArchiveUI SHALL 使用滑入或缩放弹出作为打开动画。

#### Scenario: InventoryUI 打开
- **WHEN** InventoryUI 被打开
- **THEN** 面板从右侧滑入(0.35s) + 淡入

#### Scenario: WarehouseUI 打开
- **WHEN** WarehouseUI 被打开
- **THEN** 面板从左侧滑入(0.35s) + 淡入

#### Scenario: CloudArchiveUI 打开
- **WHEN** CloudArchiveUI 被打开
- **THEN** 缩放弹出 PopIn(0.3s)

### Requirement: 对话框缩放弹出动画
SettingDialog、RatingDialog、LanguagesDialog、CommonDialog SHALL 使用统一的缩放弹出动画。

#### Scenario: 对话框打开
- **WHEN** 任一对话框被打开
- **THEN** 执行 PopIn 动画（scale 0.85→1 + alpha 0→1, 0.3s, Ease.OutQuart）

#### Scenario: 对话框关闭
- **WHEN** 任一对话框被关闭
- **THEN** 执行 PopOut 动画（scale 1→0.85 + alpha 1→0, 0.2s, Ease.InQuart），完成后才真正关闭

### Requirement: HUD 信息条淡入滑入动画
GamePlayInfoUI、CurrencyUI、PlayerSkillUI、StarPhoneUI、OutsiderFunctionUI SHALL 在 ShowUI/HideUI 时播放轻量淡入/淡出动画。

#### Scenario: GamePlayInfoUI 显示
- **WHEN** GamePlayInfoUI ShowUI 被调用
- **THEN** 从左侧轻微滑入(offset=80, 0.3s) + 淡入

#### Scenario: CurrencyUI 显示
- **WHEN** CurrencyUI ShowUI 被调用
- **THEN** 从上方轻微滑入(offset=30, 0.25s) + 淡入

#### Scenario: PlayerSkillUI 显示
- **WHEN** PlayerSkillUI ShowUI 被调用
- **THEN** 从底部滑入(offset=60, 0.3s) + 淡入

#### Scenario: OutsiderFunctionUI 显示
- **WHEN** OutsiderFunctionUI ShowUI 被调用
- **THEN** 从底部滑入(offset=50, 0.3s) + 淡入 + 功能项依次入场(stagger 0.05s)

#### Scenario: HUD 隐藏
- **WHEN** 任一 HUD 信息条 HideUI 被调用
- **THEN** 反向滑出+淡出(0.2s)

### Requirement: 通知/提示动画
UITopbar、ToastTips、FloatingBoxTip SHALL 使用边缘滑入动画。

#### Scenario: UITopbar 打开
- **WHEN** UITopbar 被打开
- **THEN** 从顶部滑入(offset=80, 0.3s) + 淡入

#### Scenario: ToastTips 打开
- **WHEN** ToastTips 被打开
- **THEN** 从顶部滑入(offset=50, 0.3s) + 淡入 + 轻微缩放(0.95→1)

#### Scenario: ToastTips 自动关闭
- **WHEN** ToastTips 倒计时结束
- **THEN** 向上滑出+淡出(0.25s)，动画完成后关闭

#### Scenario: FloatingBoxTip 打开
- **WHEN** FloatingBoxTip 被打开
- **THEN** 缩放弹出(0.9→1, 0.2s) + 淡入

### Requirement: 动画生命周期安全
所有 UI 动画 SHALL 正确处理生命周期，防止动画泄漏。

#### Scenario: UI 打开时清理残留动画
- **WHEN** 任一 UI 的 OnOpen 被调用
- **THEN** MUST 先调用 `DOTween.Kill(gameObject)` 清理残留动画，再播放新动画

#### Scenario: UI 关闭时强制完成动画
- **WHEN** 任一 UI 的 OnClose 被调用
- **THEN** MUST 调用 `DOTween.Kill(gameObject, true)` 强制完成所有残留动画

#### Scenario: StateAwareUIForm 快速切换
- **WHEN** StateAwareUIForm 快速连续调用 ShowUI/HideUI
- **THEN** MUST 先 Kill 当前动画再播放新动画，不得出现动画叠加
