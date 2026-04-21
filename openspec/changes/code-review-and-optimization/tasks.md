## 1. 前置分析 (0.5 天)

- [ ] 1.1 确认项目正常运行（从头开始战斗到结束）
- [ ] 1.2 记录改之前的行为：帧率、内存占用、日志信息
- [ ] 1.3 准备测试场景：新建一个快速测试场景，含战斗/UI 交互

## 2. 修复 #1：ChessPlacementManager.StartPlacement (async void → UniTask)

- [ ] 2.1 找到 ChessPlacementManager.cs 中的 StartPlacement 方法
- [ ] 2.2 改签名：`async void StartPlacement` → `async UniTask StartPlacementAsync`
- [ ] 2.3 添加 CancellationToken 参数：`cancellationToken: this.GetCancellationTokenOnDestroy()`
- [ ] 2.4 找所有调用处，改为 `await ChessPlacementManager.Instance.StartPlacementAsync()`
- [ ] 2.5 运行战斗准备阶段，确保棋子放置正常
- [ ] 2.6 提交这个修复

## 3. 修复 #2：CombatState.SpawnEnemies (async void → UniTask)

- [ ] 3.1 找到 CombatState.cs 中的 SpawnEnemies 和 InitializeMousePreview 方法
- [ ] 3.2 改 SpawnEnemies：`async void` → `async UniTask SpawnEnemiesAsync`
- [ ] 3.3 改 InitializeMousePreview：`async void` → `async UniTask InitializeMousePreviewAsync`
- [ ] 3.4 两个方法都加 CancellationToken 保护
- [ ] 3.5 找调用处，改为 `await`
- [ ] 3.6 运行战斗流程，从准备到敌人生成，确保正常
- [ ] 3.7 提交这个修复

## 4. 修复 #3：BuffManager.AddBuff 去重 (两个重载 → 单一实现)

- [ ] 4.1 打开 BuffManager.cs，找两个 AddBuff 重载
- [ ] 4.2 创建新的单一 AddBuff：参数为 `(int buffId, GameObject caster = null, ChessAttribute casterAttr = null)`
- [ ] 4.3 把 AddBuff 逻辑合并到新方法（处理 casterAttr 为 null 时自动从 caster 提取）
- [ ] 4.4 两个旧重载改为 `[Obsolete]`，内部调用新方法
- [ ] 4.5 运行战斗，测试 Buff 添加/堆叠（包括敌人和玩家 Buff）
- [ ] 4.6 提交这个修复

## 5. 修复 #4：BuffManager.Update 热路径 (for 循环优化)

- [ ] 5.1 打开 BuffManager.cs 的 Update 方法
- [ ] 5.2 改 `foreach (var buff in m_Buffs)` → `for (int i = 0; i < m_Buffs.Count; i++)`
- [ ] 5.3 同时优化：缓存 `m_Buffs.Count` 避免每帧重复读取
- [ ] 5.4 运行战斗 10 分钟，监控 Buff 是否正常生效/移除
- [ ] 5.5 运行 Unity Profiler，对比 GC allocation 前后差异
- [ ] 5.6 提交这个修复

## 6. 修复 #5：CombatState 事件泄漏 (添加 Unsubscribe)

- [ ] 6.1 打开 CombatState.cs，找 OnEnter 中的 `GF.Event.Subscribe(CombatEndEventArgs.EventId, OnCombatEnd)`
- [ ] 6.2 在 OnLeave 中添加对应的 `GF.Event.Unsubscribe(CombatEndEventArgs.EventId, OnCombatEnd)`
- [ ] 6.3 检查 OnEnter 中是否还有其他事件订阅，都加上 Unsubscribe
- [ ] 6.4 运行多次进入/退出战斗（5 次以上），检查是否有重复处理
- [ ] 6.5 提交这个修复

## 7. 修复 #6：CardSlotContainer 参数检测 (移出 Update)

- [ ] 7.1 打开 CardSlotContainer.cs 的 Update 方法
- [ ] 7.2 找 `HasParametersChanged()` 检测，改为：每 0.5 秒检测一次（不是每帧）
- [ ] 7.3 或者移到 OnValidate（仅编辑器时检测），运行时不检测
- [ ] 7.4 运行战斗，拖拽卡牌确保位置计算正常
- [ ] 7.5 运行 Profiler，确保 Update 时间减少
- [ ] 7.6 提交这个修复

## 8. 修复 #7：CombatManager 单例规范化 (SingletonBase<T>)

- [ ] 8.1 打开 CombatManager.cs
- [ ] 8.2 改继承：`: SingletonBase<CombatManager>`
- [ ] 8.3 删除手工写的 Instance 属性和 OnDestroy
- [ ] 8.4 运行战斗，确保 CombatManager.Instance 正常访问
- [ ] 8.5 提交这个修复

## 9. 修复 #8：销毁令牌保护 (CancellationToken 全覆盖)

- [ ] 9.1 扫描所有 async UniTask 方法（Manager、State、UI Form）
- [ ] 9.2 逐个添加 `cancellationToken: this.GetCancellationTokenOnDestroy()`
- [ ] 9.3 优先处理：StateBase 和 UIFormBase 的直接子类
- [ ] 9.4 运行战斗，销毁对象中途（如快速切换 UI），确保无空引用
- [ ] 9.5 提交这个修复

## 总耗时：~16 个工作日（每个修复 1-2 天）

**注意**：按顺序逐个修复，每个修复独立提交。发现 bug 立即回滚该修复，不影响其他。
