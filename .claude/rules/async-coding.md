---
paths: ["Assets/AAAGame/Scripts/**/*.cs"]
---

# 异步编程规则

## 核心原则

**项目统一使用 UniTask，禁止使用协程（IEnumerator / StartCoroutine）。**

## 命名规范

所有异步方法必须以 `Async` 结尾：

```csharp
// ✅ 正确
private async UniTask InitializeCombatAsync() { }
public async UniTask<bool> TryEscapeAsync() { }

// ❌ 错误
private async void InitializeCombat() { }  // async void 无法被 await！
private IEnumerator InitializeCombat() { } // 不用协程
```

## UniTask 常用写法

```csharp
// 等待一帧
await UniTask.Yield();

// 延迟（毫秒）
await UniTask.Delay(500);

// 等待条件满足
await UniTask.WaitUntil(() => someCondition);

// 等待 DOTween 动画
await tween.AsyncWaitForCompletion();

// 等待多个任务并行完成
await UniTask.WhenAll(task1, task2, task3);

// 取消令牌（防止对象销毁后继续执行）
var cts = new CancellationTokenSource();
await UniTask.Delay(1000, cancellationToken: cts.Token);
```

## 防止销毁后执行

MonoBehaviour 的异步方法必须处理对象销毁情况：

```csharp
private async UniTask DoSomethingAsync()
{
    await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
    // 对象已销毁时自动取消，不会继续执行
}
```

## 常见错误

```csharp
// ❌ async void：异常会被吞掉，且无法 await
private async void ShowNotificationAsync()
{
    await UniTask.Delay(1000);
    // 如果这里抛异常，调用方完全不知道
}

// ✅ 正确：返回 UniTask
private async UniTask ShowNotificationAsync()
{
    await UniTask.Delay(1000);
}

// 调用时使用 await
await ShowNotificationAsync();
// 或者明确忽略（需要注释说明原因）
ShowNotificationAsync().Forget();
```

## 状态机中的异步

GameFramework 的 FSM 状态 `OnEnter` 不支持 async，需要这样处理：

```csharp
protected override void OnEnter(IFsm<InGameState> fsm)
{
    base.OnEnter(fsm);
    InitializeAsync(fsm).Forget(); // 启动异步，Forget 表示不等待
}

private async UniTask InitializeAsync(IFsm<InGameState> fsm)
{
    await DoSomeAsyncWork();
    // 继续后续逻辑...
}
```
