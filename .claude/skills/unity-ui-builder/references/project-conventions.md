# 项目编码与 UI 规范

本文件汇总 Clash of Gods 项目中与 UI 开发相关的核心约束和规范。创建脚本前必读。

---

## 1. 脚本规范

### 命名
| 类型 | 格式 | 示例 |
|------|------|------|
| UI 表单 | `[Name]UI` 或 `[Name]UIForm` | `BattlePresetUI` |
| 管理器 | `[Name]Manager` | `BattlePresetManager` |
| UI Variables | `[Name].Variables.cs` | `BattlePresetUI.Variables.cs` |

### 代码结构
- UI 脚本使用 `partial class`，继承 `UIFormBase`
- Variables 文件放在 `Scripts/UI/UIVariables/`，是 partial class 的另一半
- 不要手改 Variables 文件（由工具生成），但临时创建一个让编译通过是可以的

### 异步
- **统一使用 UniTask**，禁止使用协程
- 异步方法名以 `Async` 结尾
- FSM 状态的 OnEnter 不支持 async，用 `.Forget()` 桥接

### 日志
- 使用 `DebugEx.LogModule("ModuleName", "message")` 输出日志
- Warning 用 `DebugEx.WarningModule()`

### 输入
- 所有按键输入必须走 `PlayerInputManager`

### DataTable
- 不硬编码数值，所有配置读 DataTable
- 不手改 `DataTable/` 和 `UIVariables/` 目录下的自动生成文件

---

## 2. UI 打开/关闭

```csharp
// 打开 UI
GF.UI.OpenUI(UIViews.SomeUI, userData);

// 关闭 UI
GF.UI.CloseUIForm(this.UIForm);

// 传参
var uiParams = UIParams.Create();
uiParams.SetParam("key", value);
GF.UI.OpenUI(UIViews.SomeUI, uiParams);
```

---

## 3. UI 生命周期

```csharp
protected override void OnInit(object userData)    // 初始化（只调用一次）
protected override void OnOpen(object userData)    // 每次打开
protected override void OnClose(bool isShutdown, object userData)  // 每次关闭
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)  // 每帧
```

- `OnClose` 中必须清理所有事件订阅和动画
- DOTween 动画在关闭前 `DOTween.Kill(target, true)`

---

## 4. Canvas 配置

- **Screen Space - Camera** 模式，参考分辨率 1920×1080
- 坐标计算：`anchoredPos = screenPos - new Vector2(canvasSize.x / 2f, canvasSize.y / 2f)`
- **不要用屏幕空间覆盖模式的计算方式**

---

## 5. Variables 文件格式

```csharp
//---------------------------------
//此文件由工具自动生成,请勿手动修改
//更新自:DESKTOP-XXXXXXX
//---------------------------------
using UnityEngine;
using UnityEngine.UI;
public partial class ExampleUI
{
    [Space(10)]
    [Header("UI Variables:")]
    [SerializeField] private Button varBtnBack = null;
    [SerializeField] private Text varTitleText = null;
    [SerializeField] private GameObject varContentPanel = null;
}
```

### 字段命名规范
| 组件类型 | 前缀格式 | 示例 |
|----------|----------|------|
| Button | `varBtn[Name]` | `varBtnSave` |
| Text | `var[Name]Text` | `varTitleText` |
| Image | `var[Name]Img` / `var[Name]Icon` | `varAvatarImg` |
| InputField | `var[Name]Input` | `varNameInput` |
| GameObject（容器） | `var[Name]Container` / `var[Name]Panel` | `varChessPoolContainer` |
| GameObject（模板） | `var[Name]Item` / `var[Name]Template` | `varPresetSlotItem` |
| ScrollRect | `var[Name]Scroll` | `varContentScroll` |
| Slider | `var[Name]Slider` | `varVolumeSlider` |
| Toggle | `var[Name]Toggle` | `varSoundToggle` |
| CanvasGroup | `var[Name]Group` | `varMainGroup` |

---

## 6. 必须手动完成的步骤（不可跳过）

以下操作必须由用户手动完成：
1. 在 `UITable.txt` 中添加记录
2. 在 `UIViews.cs` 枚举中添加对应项
3. 在 Inspector 中拖拽连接 Variables 引用
4. 创建/调整 Prefab 布局
5. 配置表更新后运行 DataTableGenerator
