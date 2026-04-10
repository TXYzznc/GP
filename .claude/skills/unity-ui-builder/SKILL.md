---
name: unity-ui-builder
description: "Unity UI 全流程设计与构建技能。从需求分析 → 界面设计 → HTML 原型 → 参考图生成 → C# 脚本创建 → Unity 场景构建 → 预制体保存，完成完整的 UI 开发闭环。当用户要求创建新的 UI 界面、设计游戏 UI 面板、构建 Unity UI 预制体时触发。关键词：创建UI、新建界面、设计面板、UI预制体、构建UI、搭建界面。即使用户只提到其中部分步骤（如只要求设计界面，或只要求在 Unity 中创建），也应使用此技能来确保整个流程的规范性和一致性。"
---

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
