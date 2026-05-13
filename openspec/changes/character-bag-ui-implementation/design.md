## Context

**Current State:**
- CharacterBagUI 预制体已创建但功能未实现
- ChessItemUI_Small 预制体不存在，需要新建
- SummonChessSkillTable 有 Desc 字段，需改名为 EffectText 并新增 DescText
- SummonChessTable 缺少 StoryText 字段
- 相关配置表（TreasureTable、SpecialEffectTable、SynergyTable）已存在

**Constraints:**
- 必须使用 GameFramework 的 UI/DataTable 系统
- 异步操作必须使用 UniTask，不允许协程
- 不能手改自动生成的 DataTable 和 UIVariables 文件
- 所有日志必须使用 DebugEx
- 所有输入必须走 PlayerInputManager

**Stakeholders:**
- UI 系统：需要提供四个标签页的切换机制
- DataTable 系统：需要更新和重新生成表格数据

## Goals / Non-Goals

**Goals:**
- 实现完整的角色管理界面，支持棋子查看、属性展示、宝物装备、升级预览和故事浏览
- 创建可复用的棋子小卡 UI 组件
- 支持立绘/3D 模型的切换展示
- 整合多个配置表的数据展示（技能、宝物、特殊效果、羁绊）
- 确保界面切换流畅，数据加载高效

**Non-Goals:**
- 不实现实际的棋子升级操作（仅查看预览）
- 不实现宝物的装备/卸载操作（仅展示）
- 不实现新的特殊效果或羁绊机制（仅读取和展示）

## Decisions

### 1. 三区域布局设计
**决策**：采用左列表 + 中展示 + 右详情的三区域布局
**理由**：
- 左侧列表便于快速浏览和切换棋子
- 中间展示区突出棋子形象
- 右侧详情标签页支持多维度查看（属性、装备、升级、故事）
**替代方案考虑**：单页签 + 详情抽屉（不如三区域紧凑），全屏沉浸式（操作不便）

### 2. 左侧列表双模式设计（棋子 ↔ 宝物）
**决策**：左侧通过 TreasureSwitchBtn 在 ChessContent（棋子列表）和 TreasureContent（宝物仓库）间切换
**理由**：
- 充分利用左侧空间，宝物和棋子都是主要交互对象
- 参考战斗装备界面的布局和交互习惯
- 两个内容区默认状态明确（棋子/宝物），减少用户困惑
- 支持拖拽交互，用户可在两个列表中进行操作
**替代方案考虑**：两列并排（界面过宽，空间浪费），浮窗弹出（打破沉浸感），标签页（和右侧标签页重复）

### 3. 中间区域立绘/模型切换
**决策**：通过 SwitchBtn 在 NormalImage（立绘）和 occupationImage（3D 模型）间切换
**理由**：
- 充分展示棋子的视觉特征
- 参考 NewGameUI 的已验证设计
- 切换逻辑简单，用户体验清晰
**替代方案考虑**：并排展示（浪费空间），动画过渡（增加复杂度）

### 4. 技能显示策略
**决策**：
- PassiveSkill/NormalAtk/Skill_1/UltimateSkill 始终显示
- Skill_2 默认隐藏，当 SummonChessTable.Skill2Id != 0 时显示
- SkillEffectText 读取 SummonChessSkillTable.EffectText（现有 Desc 改名）
- SkillDescText 读取新增的 SummonChessSkillTable.DescText
**理由**：
- 大多数棋子只有一个技能，隐藏 Skill_2 减少界面混乱
- 两层文本展示（效果 + 描述）满足玩家的学习需求
**替代方案考虑**：所有技能按钮固定显示（空间浪费），单一文本显示（信息不足）

### 5. 宝物效果展示
**决策**：
- BaseEffect：JSON 解析 TreasureTable.BaseAttributes，汇总显示所有装备宝物的基础属性
- SpecialEffect：查询 TreasureTable.SpecialEffectId → SpecialEffectTable，以及 TreasureTable.SynergyIds → SynergyTable
**理由**：
- BaseEffect 提供快速的属性总览
- SpecialEffect 包含特殊效果和羁绊，满足深度探索需求
- JSON 解析灵活，支持未来的属性扩展
**替代方案考虑**：直接显示所有宝物详情（过于繁琐），简化为仅显示属性（信息不完整）

### 6. 升级预览的四阶段设计
**决策**：LevelUp_Base 和 LevelUp_Skill 显示所选阶段的数据，但点击阶段按钮不实际升级
**理由**：
- 玩家需要在升级前了解各阶段的权衡
- 分离查看和操作，避免误触发升级
- 阶段数据直接从 SummonChessTable 对应行读取
**替代方案考虑**：集成升级操作（超出当前范围），仅显示当前和下一阶段（限制探索）

### 7. ChessItemUI_Small 的可复用设计
**决策**：
- 创建独立的 ChessItemUI_Small.prefab 和对应脚本
- 通过网格动态生成列表
- 支持点击选择和悬停效果
**理由**：
- 可复用于其他 UI（如战斗界面、交易界面）
- 网格动态生成支持大量棋子列表
- 悬停效果提升交互反馈
**替代方案考虑**：硬编码固定数量（扩展性差），重度定制（失去可复用性）

### 8. 宝物卡片和拖拽交互设计
**决策**：创建独立的 TreasureItemUI 组件，支持从 TreasureContent 拖拽到 TreasureUI 槽位
**理由**：
- 参考 InventoryDragHandler 的已验证拖拽设计
- 宝物卡片可复用于其他 UI（如商城、交易）
- 拖拽交互清晰直观，降低学习成本
**替代方案考虑**：拖拽后确认弹窗（增加交互复杂度），右键菜单（操作不直观）

### 9. 数据流向设计
**决策**：
- CharacterBagUI 作为中心枢纽，管理棋子选择和数据流向
- 各功能模块（SkillDisplayer、TreasureDisplayer 等）通过事件或回调接收选中的棋子数据
- 数据查询集中在 CharacterBagUI，减少重复查表
**理由**：
- 单一数据流向，易于调试和维护
- 事件模式支持解耦和扩展
**替代方案考虑**：每个模块独立查表（重复代码多），全局管理器（增加耦合）

### 10. DataTable 字段修改
**决策**：
- SummonChessSkillTable：Desc 改名为 EffectText，新增 DescText（string）
- SummonChessTable：新增 StoryText（string[]，3 个元素）
**理由**：
- 改名使字段含义更清晰
- 新增字段直接支持 UI 显示需求
- string[] 支持多阶段故事
**替代方案考虑**：新增单独的表（增加复杂度），使用 CustomData 存储（查询困难）

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| DataTable 修改导致现有系统破坏 | 确保只增不删，改名用 partial 类适配过渡期 |
| 大量棋子列表导致性能下降 | 使用对象池复用 ChessItemUI_Small，根据需要分页加载 |
| 配置表查询频繁导致卡顿 | 缓存已加载的表数据，避免重复查询 |
| 宝物效果 JSON 解析失败 | 添加异常捕获和日志，提供默认值 |
| 多个 UI 同时修改选中棋子状态 | 使用事件系统单向通知，避免循环更新 |

## Migration Plan

**Phase 1：数据表更新**
1. 修改 SummonChessSkillTable.xlsx（改名 Desc → EffectText，新增 DescText）
2. 修改 SummonChessTable.xlsx（新增 StoryText）
3. 运行 DataTableGenerator 生成新的 DataTable 代码

**Phase 2：UI 预制体准备**
1. 在 Unity 编辑器中搭建 CharacterBagUI.prefab 层级（包括 ChessContent 和 TreasureContent 及 TreasureSwitchBtn）
2. 创建并搭建 ChessItemUI_Small.prefab
3. 创建并搭建 TreasureItemUI.prefab（支持拖拽）
4. 为三个预制体生成 Variables 代码

**Phase 3：脚本实现**
1. 编写 CharacterBagUI.cs（管理逻辑，包括左侧内容切换）
2. 编写 ChessItemUI_Small.cs（卡牌逻辑）
3. 编写 TreasureItemUI.cs（宝物卡逻辑，支持拖拽源）
4. 编写各个功能模块（StateUI、TreasureUI、LevelUpUI、StoryUI 的逻辑）
5. 编写宝物拖拽系统（TreasureDragDropHandler，参考 InventoryDragHandler）
6. 编写宝物仓库数据加载（TreasureRepository）
7. 编写数据查询辅助类（TreasureDisplayHelper 等）

**Phase 4：集成和测试**
1. 连接 UI 事件和逻辑
2. 测试数据加载、切换、显示的完整流程
3. 性能优化和边界情况处理

**Rollback 策略**：
- 如需回滚，保留旧的 DataTable 版本，恢复为之前的 .bytes 文件
- UI 预制体版本控制，必要时恢复上一个版本

## Open Questions

1. **ChessItemUI_Small / TreasureItemUI 的网格列数**：目前假设 3 列，是否需要根据屏幕自适应？
2. **Skill_2 显示条件**：当前只检查 Skill2Id != 0，是否有其他条件？
3. **宝物装备时的确认流程**：拖拽后是否需要确认弹窗，还是直接生效？
4. **宝物卸载**：用户是否可以从槽位拖出宝物进行卸载？
5. **宝物拖拽的可视化反馈**：拖拽时是否显示幽灵图像、阴影或其他效果？
6. **性能优化**：如果棋子超过 50 个或宝物超过 100 个，是否需要虚拟列表？
7. **国际化**：所有文本是否需要多语言支持？
