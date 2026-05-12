## Context

本变更涉及 Clash of Gods 战斗核心的多个子系统同步演进，主要方向为：
1. 伤害归因链路完善（TakeDamage 增加 caster 参数）
2. 阵营系统从静态枚举改为动态战场分配
3. 战斗结束清理链路补全（投射物销毁）
4. 棋子属性新增 HpFloor 锁血机制
5. 装备穿脱联动 SpecialEffectManager
6. 羁绊精确刷新（先移除后重应用）
7. 物品系统宝物词条自动生成

变更集中在 `Assets/AAAGame/Scripts/Game/` 下的战斗、Buff、棋子、物品模块，以及 `Assets/AAAGame/Scripts/Manager/` 下的管理器层。

## Goals / Non-Goals

**Goals:**
- 验证每个子系统的核心功能正确性
- 验证系统间交互无回归（尤其是 TakeDamage 来源追踪链路）
- 验证边界与异常条件（null caster、HpFloor=0、空阵容等）
- 输出结构化测试需求文档，可指导 QA 手动测试

**Non-Goals:**
- 不编写自动化测试代码
- 不测试美术资源、动画效果
- 不覆盖未改动的稳定系统（如探索系统、UI布局）

## Decisions

**决策 1：以变更模块为测试分组单位**
- 每个被改动的模块对应一组测试用例，避免遗漏
- 理由：变更边界清晰，便于 QA 按模块分配测试任务

**决策 2：测试文档结构采用"前置条件 → 操作步骤 → 预期结果"三段式**
- 参考行业通用测试用例格式
- 每条用例独立可执行，避免用例间依赖

**决策 3：覆盖"直接改动"和"间接影响"两个层次**
- 直接改动：被修改的类和方法
- 间接影响：调用被修改接口的上游（如 ChangeMagicCircle、EvilSpiritMagicCircle 调用 TakeDamage）

## Risks / Trade-offs

- [TakeDamage caster 参数为 null] → 需验证所有调用点均正确传入，否则伤害统计丢失来源
- [动态战场 Camp ID 冲突] → AllocateBattlefield 每次递增，需验证 ReleaseAllBattlefields 后能正确重置
- [HpFloor 与死亡判定冲突] → HpFloor > 0 时 IsDead 永远为 false，需确认死亡逻辑是否有特殊处理
- [SynergyManager 先移除后重应用] → 若激活帧与羁绊检查帧间隔短，可能产生闪烁或重复应用，需验证频繁上下棋子的场景

## Migration Plan

所有变更已在代码层完成，配置表已更新。测试阶段：
1. 在编辑器 Play Mode 下手动验证各测试用例
2. 使用 CombatSimulatorPanel 辅助验证战斗相关用例
3. 配置表相关用例需确认 DataTableGenerator 已重新生成 .bytes 文件

## Open Questions

- ChessAttribute.HpFloor 与哪些技能/Buff 配合使用？（文档未见明确配置）
- SpecialEffectManager.RemoveEffectBySource 的具体实现是否已验证幂等性？
