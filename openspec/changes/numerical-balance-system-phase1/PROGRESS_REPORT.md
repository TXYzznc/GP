# 数值平衡系统设计 — 进度报告

**日期**：2026-05-09  
**状态**：✅ Task 1 分析完成，等待确认

---

## 整体进度

```
Task 1：数据准备与分析            ✅ 100% 完成 → 🔄 等待确认
Task 2：宝物系统数据设计          ⏳ 设计草稿完成（v2）
Task 3：装备系统数据设计          ⏳ 设计草稿完成（v2）
Task 4-8：实际修改配置表          ⏸️ 等待 Task 1-3 确认
Task 9-11：验证、文档、就绪       ⏸️ 后续步骤
```

---

## Task 1 完整成果清单

### 📊 设计文档（6 份）

1. **role_attribute_model.md** ✅
   - 定义 5 种职业（战士、刺客、法师、坦克、射手）
   - 定义 4 种功能（输出、辅助、控制、其他）
   - 设计 19 种职业-功能组合的六维属性分配
   - 6 维属性：MaxHp, MaxMp, Armor, MagicResist, SpellPower, AtkDamage

2. **task1_attribute_analysis.md** ✅
   - 现有 5 个棋子的属性提取和分析
   - 品质倍数验证（蓝:紫:金 = 1:1.27:1.57）
   - FC 公式验证（战力守恒律）
   - 升阶经验验证（85/150 EXP = 4 场战斗）

3. **chess_complete_attribute_calculation.md** ✅
   - 为所有 5 个现有棋子计算三品质、三阶级的完整属性
   - 包含 FC 值验证和对比表
   - 当前配置 vs 新设计值的详细差异

4. **task1_modification_checklist.md** ✅
   - **后羿**：规范线性倍数，三阶攻击 26→24
   - **嫦娥**：重大修改，HP BUG 修复（30→330）
   - **邪灵**：规范化，去掉物理攻击
   - **恶魂**：规范化，修复 Armor 二阶异常，法强下调
   - **黑暗杨戬**：保持超模，规范线性倍数
   - 包含每个改动的详细理由和风险评估

5. **task1_advance_table_modification.md** ✅
   - 升阶经验调整：10,20 → 85,150
   - 新增缺失棋子（邪灵、恶魂、黑暗杨戬）
   - Boss 特殊处理方案

6. **task1_final_summary.md** ✅
   - 关键发现与决策记录
   - 对其他系统的影响分析
   - Phase 2-3 的准备工作清单

### 📈 关键数据

#### 品质倍数体系
| 品质 | 点数 | FC倍数 | 说明 |
|------|------|--------|------|
| 蓝色 | 2.4 | 1.0x | 基础 |
| 紫色 | 2.7 | 1.27x | 当前后羿、嫦娥 |
| 金色 | 3.0 | 1.57x | 黑暗杨戬 |
| 超模 | 3.5 | 自定 | Boss 专用 |

#### 阶级倍数体系
| 阶级 | 倍数 |
|------|------|
| 一阶 | 1.0x |
| 二阶 | 1.6x |
| 三阶 | 2.4x |

#### 升阶经验体系
| 升阶 | EXP | 累计 | 所需战斗 |
|------|-----|----|---------|
| 一→二 | 85 | 85 | 1.4 |
| 二→三 | 150 | 235 | 3.9≈4 |

### 🎯 关键问题修复

1. **嫦娥 HP 严重 BUG**
   - 当前倍数：1→14→19.3（完全错误！）
   - 新倍数：1→1.6→2.4（规范）
   - 修复幅度：一阶 30→330（+1000%）

2. **恶魂 Armor 二阶异常**
   - 当前：20→15→20（下降！）
   - 新值：20→32→48（规范）

3. **后羿三阶攻击过高**
   - 当前：10→16→26（倍数 2.6x）
   - 新值：10→16→24（倍数 2.4x）

4. **邪灵、恶魂混合属性**
   - 问题：既有物攻又有法强，不符合职业标准
   - 解决：完全去掉物理攻击，专注法强

### 📋 所有修改汇总

**SummonChessTable 修改**：
- 5 个棋子
- 9 个属性字段（MaxHp, AtkDamage, AtkSpeed, Armor, MagicResist, SpellPower, CritRate 等）
- 共 5×3 阶级 = 15 组数据需要修改

**ChessAdvanceTable 修改**：
- 2 个棋子升阶经验调整（1, 4）
- 3 个新棋子升阶配置（10, 11, 12）
- 总计 5 个棋子的升阶配置

---

## Task 2 & 3 状态

### Task 2：宝物系统数据设计
**状态**：⏳ 设计完成（v2），等待 Task 1 确认后同步调整

**已设计内容**：
- ItemTable 新增 25 个宝物（5 品质 × 5 种类型）
- TreasureTable 配置 25 条宝物数据
- AffixTable 10 条副属性词条
- BuffTable 5 个套装效果 Buff

**文件**：
- `task2_treasure_design_v2.md`（已完成）

### Task 3：装备系统数据设计
**状态**：⏳ 设计完成（v2），等待 Task 1 确认后同步调整

**已设计内容**：
- ItemTable 新增 20 个装备（基础 + 特殊）
- EquipmentTable 20 条装备配置
- 14 个合成配方
- 3 个掉落池规则

**文件**：
- `task3_equipment_design_v2.md`（已完成）

---

## 待用户确认的关键决策

### 🔴 最高优先级

1. **嫦娥是否接受大幅修改？**
   - HP 一阶：30 → 330
   - 这是修复严重 BUG
   - 需要确认这样的修改是否可接受

### 🟡 高优先级

2. **物理与法系职业的明确分工**
   - 后羿（物理）：AtkDamage ≠ 0，SpellPower = 0
   - 邪灵/恶魂（法系）：AtkDamage = 0，SpellPower ≠ 0
   - 嫦娥（法系辅助）：AtkDamage = 0，SpellPower ≠ 0
   - 确认这个设计是否符合意图

3. **升阶经验调整确认**
   - 从 10,20 改为 85,150
   - 确认是否符合游戏节奏

4. **Boss 不可升阶**
   - 黑暗杨戬升阶经验设为 0,0
   - 确认是否同意 Boss 不可升阶

5. **品质间的 FC 倍数**
   - 蓝→紫：1.27x（验证通过）
   - 紫→金：1.57x（验证通过）
   - 确认这个倍数是否符合游戏平衡

### 🟢 中优先级

6. Task 2、Task 3 是否需要基于新的职业-功能模型调整？
7. 其他品质、其他职业的棋子如何设计？

---

## 下一步行动（待用户确认）

### 确认后立即执行（预计 1-2 小时）
1. 修改 SummonChessTable（5 棋子，15 组数据）
2. 修改 ChessAdvanceTable（5 棋子）
3. 运行 DataTableGenerator 生成新的 .cs 文件
4. 基本加载测试

### 随后执行（预计 2-3 小时）
5. Task 2、3 的最终审核和实施
6. ItemTable、TreasureTable、EquipmentTable 等新增项

### 验证阶段（预计 1-2 小时）
7. 在 Unity 编辑器中加载新配置
8. 运行战斗测试，验证品质倍数和 FC 值
9. 检查与其他系统的交互（AI、Buff 等）

---

## 项目知识库结构

```
openspec/changes/numerical-balance-system-phase1/
├── proposal.md                          # 设计提案（背景、影响）
├── design.md                            # 技术设计（决策、方案）
├── specs/                               # 5 个领域规范
│   ├── chess-quality-grading/spec.md
│   ├── chess-advancement-system/spec.md
│   ├── treasure-system/spec.md
│   ├── battle-equipment-system/spec.md
│   └── chess-attributes/spec.md
├── tasks.md                             # 11 个 Task 组，36+ 具体任务
│
├── Task 1 文档：
├── role_attribute_model.md              # 职业-功能六维模型 ✅
├── task1_attribute_analysis.md          # 属性分析（初始） ✅
├── chess_complete_attribute_calculation.md  # 完整计算表 ✅
├── task1_modification_checklist.md      # 修改清单 ✅
├── task1_advance_table_modification.md  # 升阶表修改 ✅
├── task1_final_summary.md               # Task 1 总结 ✅
│
├── Task 2 & 3 文档：
├── task2_treasure_design_v2.md          # 宝物系统设计 ⏳
├── task3_equipment_design_v2.md         # 装备系统设计 ⏳
│
└── PROGRESS_REPORT.md                   # 本文件
```

---

## 预计工作量

| 阶段 | 任务 | 预计时间 | 状态 |
|------|------|---------|------|
| Phase 1 | Task 1 分析 | 4 小时 | ✅ 完成 |
| Phase 2 | 用户确认 | 1-2 小时 | 🔄 进行中 |
| Phase 3 | 配置表修改 | 3-4 小时 | ⏸️ 待开始 |
| Phase 4 | 验证测试 | 2-3 小时 | ⏸️ 待开始 |
| Phase 5 | Task 2-3 实施 | 2-3 小时 | ⏸️ 待开始 |
| Phase 6 | 最终验证 | 1-2 小时 | ⏸️ 待开始 |
| **总计** | | **13-18 小时** | |

---

## 快速检查清单

### 分析阶段验证 ✅
- [x] 提取现有棋子属性
- [x] 定义职业-功能矩阵
- [x] 设计六维属性标准
- [x] 计算新属性值
- [x] 验证品质倍数
- [x] 验证阶级倍数
- [x] 生成修改清单

### 待确认阶段 🔄
- [ ] 用户确认所有修改提案
- [ ] 用户确认品质倍数
- [ ] 用户确认升阶经验
- [ ] 用户确认 Boss 特殊处理

### 实施阶段 ⏸️
- [ ] 修改 SummonChessTable
- [ ] 修改 ChessAdvanceTable
- [ ] 修改 ItemTable（Task 2-3）
- [ ] 修改 TreasureTable（Task 2）
- [ ] 修改 EquipmentTable（Task 3）
- [ ] 修改 AffixTable（Task 2）
- [ ] 修改 BuffTable（Task 2）

### 验证阶段 ⏸️
- [ ] 数据加载测试
- [ ] 品质倍数验证
- [ ] 战斗测试
- [ ] AI 行为验证
- [ ] Buff 系统交互测试

---

## 联系方式和反馈

如需讨论任何设计决策或反馈修改方案，请查看对应的文档：
- 职业-功能设计 → `role_attribute_model.md`
- 具体数值修改 → `task1_modification_checklist.md`
- 升阶经验 → `task1_advance_table_modification.md`
- 总体方案 → `task1_final_summary.md`

---

**准备就绪，等待用户确认！** 🚀

