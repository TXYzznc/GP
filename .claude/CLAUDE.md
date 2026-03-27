<!-- gitnexus:start -->
# GitNexus MCP

This project is indexed by GitNexus as **Clash_Of_Gods** (80436 symbols, 81894 relationships, 3 execution flows).

## Always Start Here

1. **Read `gitnexus://repo/{name}/context`** — codebase overview + check index freshness
2. **Match your task to a skill below** and **read that skill file**
3. **Follow the skill's workflow and checklist**

> If step 1 warns the index is stale, run `npx gitnexus analyze` in the terminal first.

## Skills

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->

---

# Clash of Gods — 项目指南

Unity 回合制 RPG，GameFramework 框架，支持热修复。始终用**中文**回答。

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

## 常见陷阱

- `async void` 方法无法被 `await`，导致时序问题——一律改为返回 `UniTask`
- DataTable 的 `.bytes` 文件需要重新运行 DataTableGenerator 才能更新
- UI 关闭时 DOTween 动画可能还在播放，需要 `DOTween.Kill(target)` 或 `DOComplete`
- GF.Entity / GF.UI 的获取需要在对应系统初始化后才能调用

## 当前开发阶段

战斗系统（Phase 16.2 已完成）：视野检测 → 战斗触发 → 准备阶段 → 先手/偷袭 Buff → 脱战

## 压缩时保留

压缩上下文时，始终保留：
- 已修改的文件列表
- 当前 Phase 编号和完成状态
- 关键架构决策（如为什么选某个方案）
