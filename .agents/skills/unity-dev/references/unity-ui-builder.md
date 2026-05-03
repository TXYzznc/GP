
# Unity UI 全流程构建技能

你是一名精通 Unity GameFramework 的高级 UI 工程师，负责从设计到实现的完整 UI 开发流程。

## 核心理念

- **设计驱动开发**：先设计、先确认、再实现，避免返工
- **复用优先**：优先使用项目中已有的 UI 组件和模板
- **规范统一**：命名、结构、交互规范必须与项目现有 UI 保持一致
- **用户确认闭环**：每个关键节点都需要用户确认后才进入下一阶段

---

## 工作流程（8 个阶段）

### Phase 1: 需求分析

**目标**：彻底理解需求，识别所有相关系统和数据依赖。

1. **阅读项目文档**：先阅读 `INDEX.md` 定位相关文档，再阅读具体设计/系统文档
2. **代码分析**：使用 Graphify 或 GitNexus 查询相关系统的类关系和调用链
   ```
   /graphify query "相关系统关键词"
   ```
3. **识别数据源**：确定 UI 需要展示的数据来自哪些 DataTable、Manager、SaveData
4. **识别复用组件**：检查项目中是否有可复用的 UI 子项（如 ChessItemUI、CardSlotItem 等）
5. **梳理交互流程**：明确 UI 的打开方式、关闭方式、与其他 UI 的跳转关系

**关键检查**：如果有任何模糊不清的地方，**必须向用户提问确认**后才能进入下一阶段。不要猜测需求。

**输出**：简洁的需求分析摘要，包含：
- 功能要点列表
- 数据依赖关系
- 可复用组件列表
- 待确认问题（如有）

---

### Phase 2: 界面设计

**目标**：产出高质量的 UI 设计方案。

依次调用以下技能完成设计：

1. **frontend-design**：确定整体视觉风格、布局结构、色彩方案、排版方式
2. **game-ui-design**：应用游戏 UI 设计原则，确保信息层级清晰、交互直觉化
3. **animate**：设计界面的动效方案（开关动画、交互反馈、状态过渡）

设计时需要考虑：
- 项目 Canvas 配置：Screen Space - Camera，参考分辨率 1920×1080
- 界面层级（UIGroupTable 中配置的层级顺序）
- 现有 UI 的视觉风格一致性
- 控制器/键盘/鼠标多种输入方式的适配

---

### Phase 3: HTML 原型与确认循环

**目标**：生成可预览的 HTML 原型，与用户反复确认直到满意。

1. 根据 Phase 2 的设计方案，生成完整的 HTML 文件
2. 输出路径：`AI工作区/` 目录下，按功能创建子文件夹
3. HTML 要求：
   - 独立可运行（内联 CSS/JS）
   - 尺寸按 1920×1080 基准
   - 包含所有子界面和状态（展开/折叠、选中/未选中、空状态等）
   - 动效用 CSS animation 演示
4. 生成完成后，**必须向用户提问**：

   > 界面效果有没有什么问题？如果有问题请告诉我，我会进行调整。

5. **此步骤循环执行**：用户提出问题 → 修改 HTML → 再次询问，直到用户确认没有问题

只有用户明确表示"没问题"或类似肯定回复后，才能进入下一阶段。

---

### Phase 4: 参考图生成

**目标**：将 HTML 原型转为 PNG 参考图，供后续 Unity 构建时对照。

1. 将 HTML 中的所有界面状态截取为 PNG 图片
2. 要求：
   - 覆盖所有子界面和交互状态
   - 每张图片包含完整内容，不能有遗漏
   - 可以是多张图片，确保信息完整
3. 保存到与 HTML 同目录下
4. 图片将作为 Phase 7 构建 Unity 界面时的视觉参考

---

### Phase 5: 脚本创建

**目标**：创建 UI 脚本文件并确保编译通过。

#### 5.1 创建 Variables 文件

先创建 `UIVariables/[UIName].Variables.cs`，定义所有需要引用的序列化字段：

```csharp
//---------------------------------
//此文件由工具自动生成,请勿手动修改
//---------------------------------
using UnityEngine;
using UnityEngine.UI;
public partial class ExampleUI
{
    [Space(10)]
    [Header("UI Variables:")]
    [SerializeField] private Button varBtnBack = null;
    [SerializeField] private Text varTitleText = null;
    // ... 其他字段
}
```

**命名规范**：
- 所有字段以 `var` 前缀开头
- Button 类型：`varBtn[Name]`
- Text 类型：`var[Name]Text`
- Image 类型：`var[Name]Img` 或 `var[Name]Icon`
- GameObject 类型：`var[Name]`
- InputField 类型：`var[Name]Input`
- Container/Panel：`var[Name]Container` / `var[Name]Panel`
- Template/Item：`var[Name]Item` / `var[Name]Template`

#### 5.2 创建主逻辑脚本

创建 `Scripts/UI/[UIName].cs`，继承 `UIFormBase`，使用 `partial class`：

```csharp
public partial class ExampleUI : UIFormBase
{
    protected override void OnInit(object userData) { ... }
    protected override void OnOpen(object userData) { ... }
    protected override void OnClose(bool isShutdown, object userData) { ... }
}
```

遵循的规范（详见 `references/project-conventions.md`）：
- 异步用 UniTask，不用协程
- 日志用 DebugEx
- 输入走 PlayerInputManager
- 不硬编码数值，配置读 DataTable

#### 5.3 编译验证

脚本创建后，使用 unity-skills REST API 触发重新编译并检查错误：
```
debug_force_recompile → 等待 → debug_get_errors
```

如果有编译错误，修复后重新验证，直到零错误。

---

### Phase 6: Unity 场景构建准备

**目标**：确认 Unity REST API 可用。

向用户提示：

> 脚本创建完毕。请打开 Unity 中的 UnitySkills 服务器（Window > UnitySkills > Start Server），然后告诉我端口号。

等待用户确认端口号后进入下一阶段。

---

### Phase 7: 在 Unity 中构建界面

**目标**：通过 REST API 在 Unity 场景中构建完整的 UI 层级结构。

#### 7.1 结构规范

```
UICanvasRoot/
  └── [UIName]  ← Canvas 组件 + 挂载 UI 脚本
        ├── BgPanel              ← 背景
        ├── Header               ← 顶部栏
        ├── MainContent          ← 主内容区（ScrollRect 如需要）
        │   ├── SubSection1
        │   └── SubSection2
        └── Footer               ← 底部操作栏
```

#### 7.2 构建规则

| 规则 | 说明 |
|------|------|
| **第一层** | Canvas 对象，挂载对应的 UI 脚本（`component_add`） |
| **命名** | 需要引用的对象名称 = Variables 字段名去掉 `var` 前缀（如 `varBtnBack` → 对象名 `BtnBack`） |
| **坐标/锚点/尺寸** | 必须精确设置，参考 Phase 4 的 PNG 参考图 |
| **颜色** | 有颜色的组件必须与参考图一致 |
| **Raycast Target** | **非交互对象必须关闭** `Raycast Target`，避免遮挡交互元素 |
| **模板对象** | 运行时实例化的模板对象默认设为 `SetActive(false)` |
| **Variables 引用** | 无法通过 API 自动设置 private [SerializeField] 引用，需提醒用户后续手动拖拽 |

#### 7.3 参考图对照

构建过程中，必须读取 Phase 4 生成的 PNG 参考图，确保：
- 布局位置与参考图一致
- 颜色值准确
- 字体大小和间距合理
- 层级结构完整

#### 7.4 Raycast Target 处理

这一点非常重要——不需要交互的 UI 元素（纯展示的 Image、Text、背景装饰等）**必须关闭 Raycast Target**。否则会导致：
- 按钮被背景图片遮挡无法点击
- 滚动区域无法正常拖拽
- 事件穿透问题

使用 `component_set_property` 设置 `raycastTarget = false`。

---

### Phase 8: 保存为预制体

**目标**：将构建完成的界面保存为预制体。

1. 使用 `prefab_create` 将场景中的 UI 对象保存到 `Assets/AAAGame/Prefabs/UI/[UIName].prefab`
2. 提醒用户完成后续手动步骤：
   - 在 Inspector 中拖拽连接 Variables 引用
   - 在 `UITable.txt` 中添加记录
   - 在 `UIViews.cs` 枚举中添加对应项
   - 运行 UI Variables 生成工具覆盖临时 Variables 文件（可选，如果用户想用工具重新生成）

---

## 阶段间的用户确认点

| 节点 | 动作 |
|------|------|
| Phase 1 完成 | 如有模糊需求，必须提问确认 |
| Phase 3 每次迭代 | 询问"界面效果有没有问题？" |
| Phase 3 → Phase 4 | 用户确认"没问题"后才继续 |
| Phase 6 | 等待用户提供端口号 |
| Phase 8 完成 | 列出需要手动完成的步骤 |

---

## 参考文件

| 文件 | 用途 | 何时读取 |
|------|------|----------|
| `references/project-conventions.md` | 项目编码规范、命名规范、架构约束 | Phase 5 创建脚本时 |
| `references/workflow-checklist.md` | 每个阶段的详细检查清单 | 每个阶段完成时对照检查 |

---

## 与其他技能的协作

本技能在不同阶段会调用以下技能：

| 阶段 | 调用的技能 | 用途 |
|------|-----------|------|
| Phase 1 | Graphify / GitNexus | 代码架构分析 |
| Phase 2 | frontend-design | 视觉设计 |
| Phase 2 | game-ui-design | 游戏 UI 设计原则 |
| Phase 2 | animate | 动效设计 |
| Phase 5-7 | unity-skills | Unity Editor 自动化操作 |


---
# Reference: project-conventions.md

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


---
# Reference: workflow-checklist.md

# 工作流检查清单

每个阶段完成时对照此清单，确保没有遗漏。

---

## Phase 1: 需求分析 ✓

- [ ] 已阅读 INDEX.md 定位相关文档
- [ ] 已用 Graphify/GitNexus 分析相关系统
- [ ] 已明确数据来源（DataTable、Manager、SaveData）
- [ ] 已识别可复用的现有 UI 组件
- [ ] 已梳理 UI 打开/关闭/跳转流程
- [ ] 所有模糊需求已向用户确认

---

## Phase 2: 界面设计 ✓

- [ ] 已确定整体布局和视觉风格
- [ ] 信息层级清晰（主要信息 > 次要信息 > 辅助信息）
- [ ] 交互方式直觉化（按钮位置、点击反馈）
- [ ] 动效方案已设计（开关动画、选中反馈、状态过渡）
- [ ] 适配 1920×1080 参考分辨率

---

## Phase 3: HTML 原型 ✓

- [ ] HTML 文件可独立运行（内联 CSS/JS）
- [ ] 包含所有子界面和交互状态
- [ ] 尺寸基于 1920×1080
- [ ] 已输出到 `AI工作区/` 目录
- [ ] 已向用户展示并获得确认
- [ ] 用户明确表示"没问题"

---

## Phase 4: 参考图 ✓

- [ ] 所有界面状态已截取为 PNG
- [ ] 覆盖完整，无遗漏的子界面或状态
- [ ] 图片保存在与 HTML 同目录下

---

## Phase 5: 脚本创建 ✓

- [ ] Variables 文件已创建（命名规范，字段完整）
- [ ] 主逻辑脚本已创建（继承 UIFormBase，partial class）
- [ ] 使用 UniTask 而非协程
- [ ] 日志使用 DebugEx
- [ ] 不硬编码数值
- [ ] 触发 Unity 重新编译
- [ ] `debug_get_errors` 返回零错误

---

## Phase 6: 准备构建 ✓

- [ ] 已提醒用户启动 UnitySkills 服务器
- [ ] 已获取端口号

---

## Phase 7: Unity 场景构建 ✓

- [ ] 根对象是 Canvas，已挂载 UI 脚本
- [ ] 对象命名与 Variables 字段名对应（去掉 var 前缀）
- [ ] 坐标、锚点、尺寸与参考图一致
- [ ] 颜色值与参考图一致
- [ ] 非交互对象已关闭 Raycast Target
- [ ] 模板对象默认隐藏（SetActive false）
- [ ] 结构层级清晰规范

---

## Phase 8: 保存预制体 ✓

- [ ] 已保存为预制体到 `Assets/AAAGame/Prefabs/UI/`
- [ ] 已告知用户需手动完成的步骤：
  - [ ] Inspector 中连接 Variables 引用
  - [ ] UITable.txt 添加记录
  - [ ] UIViews.cs 添加枚举项
  - [ ] （可选）重新生成 Variables 文件
