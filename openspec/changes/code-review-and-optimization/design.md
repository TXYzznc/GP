## Context

项目拥有 500+ C# 脚本，核心系统包括 Buff、战斗、UI、事件等模块。当前代码存在的问题：
1. **async void 滥用**：10+ 处违反项目异步规则（应返回 UniTask）
2. **代码重复**：管理器中的重载方法逻辑重复（BuffManager.AddBuff 重载 80% 一致）
3. **事件泄漏**：未能完整清理事件订阅，长时间运行导致内存增长
4. **性能热路径**：Update 中频繁的 GetBuff/GetComponent、参数检测等
5. **单例不一致**：多种单例实现方式混用

## Goals / Non-Goals

**Goals：**
- 消除所有 async void，替换为 UniTask，统一异步编程风格
- 重构代码重复，通过方法提取和参数化降低维护负担
- 审计并修复事件订阅泄漏，建立生命周期规范
- 优化 Update 热路径，减少频繁查询和检测
- 统一单例实现，使用 SingletonBase<T> 为标准
- 为销毁后执行的异步代码加入 CancellationToken 保护

**Non-Goals：**
- 大规模 API 重设计（保持向后兼容）
- 添加新功能或修改游戏逻辑
- 重构数据结构或改变类关系
- 性能基准测试和详细 profiling（但需监控内存和帧率）

## Decisions

### 1. Async Void 替换策略
**决策**：全局扫描并替换 async void → UniTask，统一到项目标准

**具体方案**：
- 文件清单：10+ 个文件的 async void 方法（ChessPlacementManager、CombatState、CombatVFXManager 等）
- 替换模式：`async void Foo()` → `async UniTask FooAsync()`
- 调用端：`.Forget()` 改为 `await` 或明确 `.Forget()` + 注释说明
- 异常处理：通过 try-catch 确保异常被妥善处理（async void 会吞没异常）

**为什么选择**：async void 无法被 await，异常会被吞没，导致难以调试的竞态。UniTask 可被等待、取消和异常处理。

**考虑的替代方案**：
- 保持 async void：容易导致隐性 bug，不符合项目规则 ✗
- 使用 Coroutine：项目已完全迁移到 UniTask ✗

---

### 2. BuffManager 去重构
**决策**：合并 AddBuff 的两个重载，通过默认参数和辅助方法消除重复

**具体方案**：
```csharp
// 之前：两个 80% 相同的重载
public void AddBuff(int buffId, GameObject caster = null)
public void AddBuff(int buffId, GameObject caster, ChessAttribute casterAttr)

// 之后：单一实现 + 内部初始化方法
public void AddBuff(int buffId, GameObject caster = null, ChessAttribute casterAttr = null)
{
    var config = GetAndValidateBuffConfig(buffId);
    if (config == null) return;
    
    var existingBuff = GetBuff(buffId);
    if (existingBuff != null) {
        existingBuff.OnStack();
        OnBuffStackChanged?.Invoke(buffId, existingBuff.StackCount);
        return;
    }
    
    var newBuff = BuffFactory.Create(buffId);
    if (newBuff == null) return;
    
    m_Context.Caster = caster;
    m_Context.CasterAttribute = casterAttr ?? (caster?.GetComponent<ChessAttribute>() ?? null);
    m_Context.OwnerAttribute = gameObject.GetComponent<ChessAttribute>();
    m_Context.OwnerBuffManager = this;
    
    InitializeBuff(newBuff, config, buffId);
}

private void InitializeBuff(IBuff buff, BuffRow config, int buffId)
{
    buff.Init(m_Context, config);
    m_Buffs.Add(buff);
    buff.OnEnter();
    DebugEx.LogModule("BuffManager", $"添加 Buff: {config.Name} (ID:{buffId})");
    OnBuffAdded?.Invoke(buffId);
}
```

**为什么选择**：减少维护负担，降低 bug 风险（逻辑不一致）

**考虑的替代方案**：
- 保持两个重载：维护成本高，bug 风险 ✗
- 彻底重设计 AddBuff API：破坏向后兼容性 ✗

---

### 3. 事件订阅生命周期规范
**决策**：建立 "订阅时+取消订阅时" 的检查清单，修复现有泄漏

**具体方案**：
- **检查清单**：扫描所有 OnEnable/OnDisable 或 OnEnter/OnLeave 中的 += 和 -=
- **修复模式**：
  ```csharp
  // OnEnable 或初始化时
  GF.Event.Subscribe(SomeEvent.EventId, OnSomeEvent);
  
  // OnDisable 或清理时
  GF.Event.Unsubscribe(SomeEvent.EventId, OnSomeEvent);
  ```
- **自检工具**：编写脚本检查 += 数量 > -= 数量的情况
- **优先级**：检查战斗系统、UI 系统、棋子系统（高频事件订阅）

**为什么选择**：事件泄漏导致长期运行内存增长，影响稳定性

**考虑的替代方案**：
- 弱引用事件系统：复杂度高，需要重新设计事件系统 ✗
- 依赖 GF.Event 自动清理：当前 GF 版本不支持 ✗

---

### 4. Update 热路径优化
**决策**：缓存 GetBuff 结果、移出频繁检测、批量遍历优化

**具体方案**：
- **BuffManager.Update 优化**：
  - 预计算待删除列表，避免重复 GetBuff 调用
  - 使用 for 循环替代 foreach（减少枚举器分配）
  
- **CardSlotContainer.Update 优化**：
  - 参数检测从 Update 改为 OnValidate（编辑器时检测，运行时不检测）
  - 或将检测频率从每帧改为每 0.5 秒一次
  
- **GetComponent 缓存**：
  - 在 Awake/Start 时缓存 ChessAttribute、RectTransform 等常用组件
  - 避免在 Update/热路径上调用 GetComponent

**为什么选择**：Update 是帧级热路径，每毫秒都影响帧率

**考虑的替代方案**：
- Job System：过度设计，复杂度不值得 ✗
- 禁用参数检测：丧失开发便利性 ✗

---

### 5. 单例标准化
**决策**：统一所有管理器使用 SingletonBase<T> 基类或一致的单例模式

**具体方案**：
- **标准模式**（推荐用于需要 MonoBehaviour 的单例）：
  ```csharp
  public class SomeManager : SingletonBase<SomeManager>
  {
      // SingletonBase 提供 Instance、OnDestroy 等
  }
  ```
  
- **非 MonoBehaviour 单例**（如 CardManager）：
  ```csharp
  private static CardManager s_Instance;
  public static CardManager Instance
  {
      get
      {
          if (s_Instance == null)
              s_Instance = new CardManager();
          return s_Instance;
      }
  }
  private CardManager() { }
  
  private void OnDestroy() // 供外部调用，如 GameFramework OnShutdown
  {
      s_Instance = null;
  }
  ```

- **清单**：CardManager、ChessPlacementManager、CombatVFXManager 等需统一

**为什么选择**：一致的单例模式便于维护和故障排查

**考虑的替代方案**：
- 完全消除单例（使用 DI）：架构变更过大 ✗
- 各自为政：当前状态，导致不一致 ✗

---

### 6. 销毁令牌保护
**决策**：为所有 MonoBehaviour 异步方法加入 CancellationToken，防止销毁后继续执行

**具体方案**：
```csharp
private async UniTask DoAsyncWorkAsync()
{
    await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
    // 对象销毁时自动取消，不会继续执行
}
```

- 修复的文件：包含 async UniTask 方法的所有 MonoBehaviour
- 检查清单：StateBase、UIFormBase 继承类等

**为什么选择**：防止对象销毁后继续执行异步代码导致空引用异常

**考虑的替代方案**：
- try-catch 包装：不够精确，难以确保所有场景都处理 ✗
- Forget()：未监控异常 ✗

## Risks / Trade-offs

| 风险 | 防御方案 |
|------|---------|
| **API 破坏兼容性** | 在 BuffManager 中保留旧重载（标记 Obsolete），指向新方法 |
| **遗漏 async void** | 使用 Roslyn analyzer 自动检测，CI 流程中拦截 |
| **事件泄漏修复不完整** | 编写单元测试验证 += 和 -= 次数相等 |
| **性能优化无效** | 采集帧率和内存数据前后对比，不满足目标则回滚 |
| **单例转换过程冲突** | 逐个系统进行，充分测试后合并，避免全局转换引入 bug |
| **销毁令牌调试复杂** | 在 log 中标记被取消的异步操作，便于问题排查 |

## Migration Plan

**增量式修复策略** — 每次修复一个问题，确保稳定性

**修复顺序**（按影响范围和难度）：
1. **ChessPlacementManager.StartPlacement** (async void → UniTask)
2. **CombatState.SpawnEnemies** (async void → UniTask)
3. **BuffManager.AddBuff 去重** (两个重载 → 单一实现)
4. **BuffManager.Update 热路径** (优化 GetBuff 调用)
5. **CombatState 事件泄漏** (添加 GF.Event.Unsubscribe)
6. **CardSlotContainer 参数检测** (移出 Update)
7. **CombatManager 单例规范化** (SingletonBase<T>)
8. **销毁令牌保护** (CancellationToken 添加)

**每次修复的流程**：
1. 修改代码（一个文件/一个方法）
2. 运行测试（确保该功能模块正常）
3. 提交变更（含修复说明）
4. 进入下一个问题

**总耗时**：~16 个工作日（每个修复 1-2 天）

**回滚策略**：
- 每个修复单独提交，bug 立即回滚该修复
- 不堆积多个修复，降低排查难度

## Open Questions

1. **Roslyn analyzer 的具体实现**：是否使用现有工具（FxCop、StyleCop）还是自定义？
2. **事件泄漏检测的自动化程度**：手工审计还是编写自动化工具？
3. **单例销毁时机**：GameFramework Shutdown 时统一调用，还是各单例各自清理？
4. **性能目标**：帧率和内存提升的具体量化目标是什么？（如帧率 +2%、内存 -10MB）
5. **兼容性保证期限**：Obsolete 标记的旧 API 保留多少个版本后删除？
