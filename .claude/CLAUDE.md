# Clash of Gods — 项目指南

- 本项目为 Unity 回合制 RPG，GameFramework 框架，支持热修复。始终用**中文**回答。
- 你是一名精通unity的全栈工程师，不需要过度解释基础概念
- 回复尽量简洁，不要加无关的客套话
- 优先用简单方案，不要过度工程
- 在进行回答前，必须先阅读**INDEX.md**和**GITNEXUS.md**

## 任务工作流

**处理任务时的标准流程（提高精确度、减少 Token 消耗）：**

1. **初步分析** - 理解任务需求，识别关键关键词和系统名称
2. **使用 Graphify 查询** - 搜索与任务相关的脚本
   - 运行 `/graphify query "关键词"` 查询知识图谱
   - 识别相关的核心类、管理器、系统
   - 查看类之间的依赖关系和调用链
3. **精准定位** - 基于图谱结果，确定需要分析的具体脚本文件
4. **深入分析** - 阅读相关脚本代码，理解实现细节
5. **制定方案** - 在充分理解现有架构的基础上设计解决方案

**Graphify 查询示例：**
```bash
/graphify query "UI 系统如何与战斗系统交互"          # 跨系统关系
/graphify query "Buff 应用流程"                    # 系统流程
/graphify query "卡牌数据结构"                     # 数据模型
/graphify path "CardManager" "CombatUI"            # 最短路径
```

**好处：**
- ✅ 避免无谓的文件搜索，直接定位相关代码
- ✅ 理解代码之间的关系，减少遗漏
- ✅ 减少重复搜索，降低 Token 消耗
- ✅ 提高方案的准确性和完整性

## 项目文档

- **INDEX.md** - 知识库导航（93 篇文档，按系统分类）
  - 战斗系统、UI系统、物品系统、探索系统、棋子系统、框架系统、配置表系统等
  - 查找具体系统或问题，以及开发业务功能时，先查 INDEX.md 看看能否找到对应文档进行阅读

- **GITNEXUS.md** - 代码分析工具指南
  - 包含架构分析、影响范围、Bug追踪、重构等技能
  - 进行复杂代码分析或大范围重构时使用

- **Graphify 知识图谱** - 项目代码架构可视化
  - `graphify-out/graph.html` - Assets/AAAGame/Scripts 核心逻辑图（3979 节点）
  - `graphify-out-game/graph.html` - Assets/AAAGame/Scripts/Game 业务系统图（1588 节点）
  - 使用 `/graphify query "关键词"` 查询相关脚本和系统关系
  - 比逐个文件搜索更快、更准确，能完整显示类依赖关系

## 技术栈

- **Unity GameFramework**：Procedure / FSM / Event / Entity / UI / Resource
- **UniTask**：所有异步操作用 `await UniTask`，不用协程
- **DOTween**：UI 动画
- **DataTable**：Excel → 自动生成 .cs + .bytes（不要手改生成文件）
- **热修复**：`Assets/AAAGame/Scripts/` 下的代码在 `Hotfix.asmdef` 程序集内
- **输入方式**：所有按键输入必须走PlayerInputManager。

## 命名规范

| 类型 | 规范 | 示例 |
|------|------|------|
| 流程 | `[Name]Procedure` | `GameProcedure` |
| 状态 | `[Name]State` | `CombatPreparationState` |
| UI 表单 | `[Name]UIForm` 或 `[Name]UI` | `GameUIForm` |
| 数据表类 | `[Name]Table` | `BuffTable` |
| Buff | `[Name]Buff` | `StatModBuff` |
| 管理器 | `[Name]Manager` | `CombatTriggerManager` |

## 关键约束

- **不要手改** `DataTable/` 下的文件，它们由 DataTableGenerator 工具自动生成
- **不要手改** `UIVariables/` 和 `UIItemVariables/` 下的文件，同样自动生成
- **不要硬编码数值**，所有配置读 DataTable（BuffTable、EnemyEntityTable 等）
- 异步方法名必须以 `Async` 结尾，返回 `UniTask` 或 `UniTask<T>`
- 每次改动 DataTable 相关代码前，先确认 .xlsx 是否需要同步更新
- **遇到必须手动完成的任务时，必须先通知用户完成后再继续开发**。以下操作必须由用户手动完成，不可跳过或提前编写依赖代码：
  - 配置表更新（新增字段、新建表 → 运行 DataTableGenerator）
  - Prefab 创建与 UI 层级搭建
  - Variables 脚本生成（`UIVariables/`、`UIItemVariables/` 由工具生成）
  - 正确顺序：**用户先定义 Prefab/配置表 → 工具生成 Variables/DataTable → 再编写对应逻辑脚本**
- 输出日志使用DebugEX类中的方法

### 文档存放路径
- **统一目录** - 所有 AI 生成的 .md 文档必须保存到 `项目知识库（AI）/` 文件夹
- **子文件夹** - 可根据任务内容在 `项目知识库（AI）/` 内创建子文件夹分类管理

## Canvas 配置

**项目只有两个 Canvas：**
1. **屏幕空间-摄像机模式**（Screen Space - Camera）
   - 需要指定 UI 摄像机（worldCamera）
   - 坐标计算：屏幕坐标（0,0 在左下角）→ anchoredPosition（原点在中心）
   - 公式：`anchoredPos = screenPos - new Vector2(canvasSize.x / 2f, canvasSize.y / 2f)`

2. **世界空间模式**（World Space）
   - 直接在 3D 场景中渲染

**注意**：进行坐标相关计算时，**不要用屏幕空间-覆盖模式的方式**。两种模式的坐标系转换完全不同。

## 常见陷阱

- `async void` 方法无法被 `await`，导致时序问题——一律改为返回 `UniTask`
- DataTable 的 `.bytes` 文件需要重新运行 DataTableGenerator 才能更新
- UI 关闭时 DOTween 动画可能还在播放，需要 `DOTween.Kill(target)` 或 `DOComplete`
- GF.Entity / GF.UI 的获取需要在对应系统初始化后才能调用  
- 目前项目中使用的UI在屏幕空间-摄像机下面，当需要计算UI偏移时，需要注意计算方式，不要用错了（有时容易误用成屏幕空间-覆盖这种模式下的计算方式），同时，鼠标坐标通常是屏幕空间-覆盖模式。

## 压缩时保留

压缩上下文时，始终保留：
- 已修改的文件列表
- 当前 Phase 编号和完成状态
- 关键架构决策（如为什么选某个方案）