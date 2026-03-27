---
paths: ["Assets/AAAGame/Scripts/UI/**/*.cs", "Assets/AAAGame/Prefabs/UI/**/*.prefab"]
---

# UI 系统规则

## UIVariables 自动生成文件

`Scripts/UI/UIVariables/` 和 `Scripts/UI/UIItemVariables/` 下的 `*.Variables.cs` 文件**全部自动生成**，禁止手动修改。

如需添加字段，右键 Hierarchy 中的 UI 节点 → **DataTable → Generate UI Variables**。

## 新建 UI 的标准流程

1. 在 `Assets/AAAGame/Prefabs/UI/` 下创建预制体
2. 在 `Assets/AAAGame/DataTable/Core/UITable.txt` 中添加记录（递增 ID）
3. 在 `Scripts/UI/Core/UIViews.cs` 的枚举中添加对应项
4. 创建 `Scripts/UI/[Name]UIForm.cs`（继承 `UIFormBase`）
5. 生成 Variables 文件

## UIFormBase 生命周期

```csharp
protected override void OnOpen(object userData)    // UI 打开，初始化数据
protected override void OnClose(bool isShutdown, object userData)  // UI 关闭，清理
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)  // 每帧更新
protected override void OnResume()  // 从暂停恢复（被覆盖的UI重新显示）
protected override void OnPause()   // 被更高层UI覆盖
```

## 打开/关闭 UI 的标准写法

```csharp
// 打开（通过 UIViews 枚举）
GF.UI.OpenUI(UIViews.SomeUIForm, userData);

// 关闭
GF.UI.CloseUI(uiFormInstance);

// 传参给 UI
var uiParams = UIParams.Create();
uiParams.SetParam("key", value);
GF.UI.OpenUI(UIViews.SomeUIForm, uiParams);
```

## DOTween 动画规范

- UI 开关动画用 `CanvasGroup.DOFade()`
- 动画结束后必须能被中途打断：`DOTween.Kill(this, true)` 再重新播放
- 异步等待动画结束：`await tween.AsyncWaitForCompletion()`
- 不要在 `OnClose` 之后继续播放动画（对象可能已被池回收）

## 注意事项

- UI 层级顺序在 `UIGroupTable` 中配置，数值越大越靠前
- Dialog 类 UI 不要直接关闭，通过回调或事件关闭，防止打断其他流程
- 同一时间只允许一个同类型弹窗（在 Open 前检查是否已经打开）
