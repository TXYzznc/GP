## 1. 核心类实现

- [x] 1.1 创建 TreasureChestInteractable.cs 文件，继承 InteractableBase
- [x] 1.2 定义 ChestState 枚举（Locked, Opened）
- [x] 1.3 在 Awake 中初始化 Animator 引用（GetComponent）
- [x] 1.4 实现 CanInteract() 方法（检查 m_IsAnimating）
- [x] 1.5 实现 InteractionTip 属性（根据状态返回不同文本）
- [x] 1.6 实现 InteractAnimIndex 属性（始终返回 -1）

## 2. 交互逻辑

- [x] 2.1 实现 OnInteract() 方法，启动 OpenChestAsync() 异步流程
- [x] 2.2 实现 OpenChestAsync() 异步方法，设置 m_IsAnimating 标志
- [x] 2.3 在 Locked 状态下：触发 Animator.SetTrigger("Open")
- [x] 2.4 等待动画完成（WaitUntil normalizedTime >= 1.0f）
- [x] 2.5 设置 m_State 为 Opened，更新 interactionTip 字段
- [x] 2.6 在 Opened 状态下：跳过动画直接调用 OpenChestUI()
- [x] 2.7 异步流程完成后重置 m_IsAnimating 标志
- [x] 2.8 使用 GetCancellationTokenOnDestroy() 防止销毁后执行

## 3. UI 占位实现

- [x] 3.1 实现 OpenChestUI() 方法（占位，输出 Debug Log）
- [x] 3.2 添加 TODO 注释说明后续 UI 实现位置

## 4. 可交互系统集成

- [x] 4.1 配置序列化字段：interactionTip、priority、interactionRadius、openAnimTrigger
- [x] 4.2 验证 TriggerCollider 自动创建（继承自 InteractableBase）
- [x] 4.3 确保类名符合项目命名规范（InteractableBase 后缀）

## 5. OutlineController 集成

- [x] 5.1 添加 [RequireComponent(typeof(OutlineController))] 属性
- [x] 5.2 在 Awake 中初始化 m_OutlineController 引用
- [x] 5.3 添加序列化字段：outlineColor、outlineSize
- [x] 5.4 在 Update 中动态控制描边显示/隐藏（基于 CanInteract 状态）
- [x] 5.5 CanInteract 为 true 时显示黄色描边，为 false 时隐藏

## 6. 测试验证

- [ ] 6.1 创建测试场景，放置带 Animator 的宝箱 GameObject
- [ ] 6.2 创建玩家角色，确保有 InteractionDetector 和 PlayerInputManager
- [ ] 6.3 验证：玩家靠近宝箱，提示显示"打开宝箱"，宝箱显示黄色描边
- [ ] 6.4 验证：按交互键，宝箱播放动画，描边隐藏（m_IsAnimating = true）
- [ ] 6.5 验证：动画播放过程中再按交互键，无反应（CanInteract 返回 false）
- [ ] 6.6 验证：动画完成后，提示更新为"查看宝箱"，描边重新显示
- [ ] 6.7 验证：再次交互，跳过动画直接打开界面（Log 输出）
- [ ] 6.8 验证：宝箱销毁时，异步任务正确取消，描边清理

## 7. 代码规范检查

- [x] 7.1 确保使用 UniTask，不使用协程
- [x] 7.2 异步方法以 Async 后缀命名，返回 UniTask
- [x] 7.3 使用 DebugEx.LogModule("TreasureChest", ...) 输出日志
- [x] 7.4 代码注释清晰，说明关键设计决策
- [x] 7.5 序列化字段使用 [Header] 和 [Tooltip] 说明用途
- [x] 7.6 使用 [RequireComponent] 标记必需的组件依赖
