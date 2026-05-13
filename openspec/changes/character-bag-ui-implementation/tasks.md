## 1. 配置表修改与生成

- [ ] 1.1 修改 SummonChessSkillTable.xlsx：将 Desc 列改名为 EffectText
- [ ] 1.2 修改 SummonChessSkillTable.xlsx：新增 DescText 列（字符串类型，技能文本描述，和技能相关，但和技能效果关系不大，更像是一小句话（30字以内），用来补充世界观和故事的）
- [ ] 1.3 验证 SummonChessSkillTable.xlsx 的所有数据行已正确填写 EffectText 和 DescText
- [ ] 1.4 修改 SummonChessTable.xlsx：新增 StoryText 列（字符串数组类型，元素数量不做限制。每段故事在100字到200字之间）
- [ ] 1.5 验证 SummonChessTable.xlsx 的所有棋子已填写对应阶段的故事文本
- [ ] 1.6 在 Unity 编辑器中运行 DataTableGenerator：处理 SummonChessSkillTable 和 SummonChessTable
- [ ] 1.7 验证生成的 DataTable 代码正确（DRSummonChessSkillTable、DRSummonChessTable）
- [ ] 1.8 验证 .bytes 文件已更新

## 2. UI 预制体层级搭建 - CharacterBagUI（这部分已经做好了。预制体和变量脚本已经生成完了）

- [ ] 2.1 在 CharacterBagUI.prefab 中：添加左侧棋子列表容器（GridLayoutGroup，命名 ChessContent）和 ChessItemUI_Small 模板
- [ ] 2.2 在 CharacterBagUI.prefab 中：添加左侧宝物列表容器（GridLayoutGroup，命名 TreasureContent，默认隐藏）和 TreasureItemUI 模板
- [ ] 2.3 在 CharacterBagUI.prefab 中：添加 TreasureSwitchBtn（用于在 ChessContent 和 TreasureContent 间切换）
- [ ] 2.4 在 CharacterBagUI.prefab 中：添加中间展示区（包含 NormalImage、occupationImage、SwitchBtn）
- [ ] 2.5 在 CharacterBagUI.prefab 中：添加右侧四个标签页按钮（StateBtn、TreasureBtn、LevelUpBtn、StoryBtn）
- [ ] 2.6 在 CharacterBagUI.prefab 中：搭建 StateUI 面板（包含 NameText、StateText、五个技能按钮、SkillEffectText、SkillDescText）
- [ ] 2.7 在 CharacterBagUI.prefab 中：搭建 TreasureUI 面板（包含 3 个 TreasureSlot、BaseEffect、SpecialEffect）
- [ ] 2.8 在 CharacterBagUI.prefab 中：搭建 LevelUpUI 面板（包含 4 个阶段按钮、LevelUp_Base、LevelUp_Skill）
- [ ] 2.9 在 CharacterBagUI.prefab 中：搭建 StoryUI 面板（包含 StoryText）
- [ ] 2.10 在 CharacterBagUI.prefab 中：添加关闭按钮（CloseBtn）
- [ ] 2.11 为 CharacterBagUI.prefab 生成 UIVariables 代码
- [ ] 2.12 验证 CharacterBagUI.Variables.cs 包含所有 UI 元素引用（包括 ChessContent、TreasureContent、TreasureSwitchBtn）

## 3. UI 预制体创建 - ChessItemUI_Small

- [ ] 3.1 创建 ChessItemUI_Small.prefab：添加棋子头像、名称、品质徽章（不显示等级，等级是局内属性）
- [ ] 3.2 在 ChessItemUI_Small.prefab 中：添加选中高亮效果容器（如 HighlightImage）
- [ ] 3.3 为 ChessItemUI_Small.prefab 生成 UIItemVariables 代码
- [ ] 3.4 验证 ChessItemUI_Small.Variables.cs 包含所有 UI 元素引用

## 4. UI 预制体创建 - TreasureItemUI

- [ ] 4.1 创建 TreasureItemUI.prefab：添加宝物图标、名称、品质徽章、数量显示
- [ ] 4.2 在 TreasureItemUI.prefab 中：添加拖拽源组件支持（参考 InventoryDragHandler）
- [ ] 4.3 在 TreasureItemUI.prefab 中：添加拖拽视觉反馈容器（如 DragFeedbackImage）
- [ ] 4.4 为 TreasureItemUI.prefab 生成 UIItemVariables 代码
- [ ] 4.5 验证 TreasureItemUI.Variables.cs 包含所有 UI 元素引用

## 5. 核心脚本实现 - CharacterBagUI 逻辑

- [ ] 5.1 编写 CharacterBagUI.cs：OnOpen 方法初始化棋子列表和第一个棋子的选择
- [ ] 5.2 编写 CharacterBagUI.cs：实现棋子列表加载逻辑（从玩家数据中读取拥有的棋子）
- [ ] 5.3 编写 CharacterBagUI.cs：实现左侧内容切换逻辑（TreasureSwitchBtn：ChessContent ↔ TreasureContent）
- [ ] 5.4 编写 CharacterBagUI.cs：实现宝物仓库数据加载（当切换到 TreasureContent 时加载玩家宝物）
- [ ] 5.5 编写 CharacterBagUI.cs：实现标签页切换逻辑（StateBtn/TreasureBtn/LevelUpBtn/StoryBtn）
- [ ] 5.6 编写 CharacterBagUI.cs：实现立绘/模型切换逻辑（SwitchBtn）
- [ ] 5.7 编写 CharacterBagUI.cs：实现关闭逻辑（CloseBtn）
- [ ] 5.8 编写 CharacterBagUI.cs：实现棋子选择处理（更新所有右侧面板）
- [ ] 5.9 编写 CharacterBagUI.cs：OnClose 方法清理资源和动画

## 6. 功能模块 - 状态标签页（StateUI）

- [ ] 6.1 编写 StateUIController 或在 CharacterBagUI 中实现：显示棋子名称（NameText）
- [ ] 6.2 实现棋子基础属性显示（StateText）：HP、Attack、Defense、Magic Resist 等
- [ ] 6.3 实现五个技能按钮的点击处理：PassiveSkill、NormalAtk、Skill_1、UltimateSkill
- [ ] 6.4 实现 Skill_2 的条件显示逻辑：当 Skill2Id != 0 时显示，否则隐藏
- [ ] 6.5 实现技能信息更新：点击技能按钮时更新 SkillEffectText（来自 EffectText）和 SkillDescText（来自 DescText）
- [ ] 6.6 实现技能按钮的悬停和点击视觉反馈

## 7. 功能模块 - 宝物标签页（TreasureUI）

- [ ] 7.0 宝箱槽位和 DetailInfoUI 中的装备槽位一样。
- [ ] 7.1 编写 TreasureUIController 或在 CharacterBagUI 中实现：显示三个宝物槽位
- [ ] 7.2 实现宝物槽位的数据绑定：读取棋子已装备的宝物
- [ ] 7.3 实现宝物槽位作为拖拽放置目标：接收来自 TreasureContent 的拖拽宝物
- [ ] 7.4 实现 BaseEffect 显示：解析 TreasureTable.BaseAttributes（JSON），汇总所有装备宝物的基础属性
- [ ] 7.5 实现 SpecialEffect 显示：查询 SpecialEffectTable 和 SynergyTable，显示特殊效果和羁绊描述
- [ ] 7.6 处理没有装备宝物时的空状态（显示 "None" 或占位符）
- [ ] 7.7 实现宝物卸载功能（可选）：允许从槽位拖出宝物

## 8. 功能模块 - 升级标签页（LevelUpUI）

- [ ] 8.1 编写 LevelUpUIController 或在 CharacterBagUI 中实现：显示四个阶段按钮（Stage 1/2/3A/3B）
- [ ] 8.2 实现阶段按钮的点击处理：点击时更新对应阶段的数据展示
- [ ] 8.3 实现当前阶段按钮的高亮显示
- [ ] 8.4 实现 LevelUp_Base 显示：从 SummonChessTable 读取所选阶段的所有属性
- [ ] 8.5 实现 LevelUp_Skill 显示：从 SummonChessTable 和 SummonChessSkillTable 读取所选阶段的所有技能

## 9. 功能模块 - 故事标签页（StoryUI）

- [ ] 9.1 编写 StoryUIController 或在 CharacterBagUI 中实现：显示棋子背景故事
- [ ] 9.2 实现故事文本读取：从 SummonChessTable.StoryText 数组中读取对应阶段的故事
- [ ] 9.3 处理故事为空或未填写时的状态

## 10. 棋子卡片组件 - ChessItemUI_Small

- [ ] 10.1 编写 ChessItemUI_Small.cs：实现卡片初始化（绑定数据）
- [ ] 10.2 实现卡片的点击事件：选中卡片并通知 CharacterBagUI
- [ ] 10.3 实现卡片的悬停效果：鼠标进入/离开时的视觉反馈
- [ ] 10.4 实现卡片的品质指示符显示：根据品质等级（1-4）显示不同颜色
- [ ] 10.5 实现卡片的高亮状态：当卡片被选中时显示特殊效果

## 11. 宝物卡片组件 - TreasureItemUI

- [ ] 11.1 编写 TreasureItemUI.cs：实现卡片初始化（绑定数据）
- [ ] 11.2 实现卡片的品质指示符显示：根据品质等级显示不同颜色
- [ ] 11.3 实现卡片数量显示：显示拥有该宝物的数量
- [ ] 11.4 实现卡片的悬停效果：鼠标进入/离开时的视觉反馈
- [ ] 11.5 实现卡片作为拖拽源：支持拖拽操作的初始化和事件触发

## 12. 宝物拖拽系统

- [ ] 12.1 编写 TreasureDragDropHandler.cs：实现宝物从 TreasureContent 到 TreasureUI 槽位的拖拽逻辑
- [ ] 12.2 实现拖拽开始时的视觉反馈：显示拖拽预览和阴影
- [ ] 12.3 实现拖拽悬停时的槽位高亮：表示可放置的目标
- [ ] 12.4 实现拖拽放置时的装备逻辑：更新棋子装备数据
- [ ] 12.5 实现拖拽取消或失败时的回退：宝物返回原位置
- [ ] 12.6 实现装备宝物后的数据同步：TreasureContent 数量更新，TreasureUI 效果刷新
- [ ] 12.7 实现宝物卸载功能（如果需要）：允许从槽位拖出宝物回到仓库

## 13. 宝物仓库数据管理

- [ ] 13.1 创建 TreasureRepository.cs：实现宝物仓库数据加载逻辑
- [ ] 13.2 实现从玩家数据中读取仓库宝物：仓库 + 背包数据联通
- [ ] 13.3 实现宝物排序和分类（可选）：按品质、类型排序
- [ ] 13.4 实现宝物数据的动态更新：装备/卸载后刷新列表

## 14. 辅助工具与优化

- [ ] 14.1 创建数据查询辅助类：TreasureDisplayHelper（用于查询宝物、特殊效果、羁绊数据）
- [ ] 14.2 实现对象池复用：为 ChessItemUI_Small 和 TreasureItemUI 实现对象池
- [ ] 14.3 实现 DataTable 缓存：避免频繁查表，提升性能
- [ ] 14.4 添加日志支持：使用 DebugEx 记录关键操作（棋子选择、标签页切换、宝物装备等）
- [ ] 14.5 实现错误处理：JSON 解析失败、数据缺失、拖拽异常时的容错机制

## 15. 集成与测试

- [ ] 15.1 连接所有 UI 事件：确保标签页切换、棋子选择、立绘切换、左侧列表切换等流畅无延迟
- [ ] 15.2 测试数据加载完整性：验证所有配置表数据正确加载和显示
- [ ] 15.3 测试棋子列表功能：滚动、选择、高亮等
- [ ] 15.4 测试 StateUI：技能展示、效果更新、Skill_2 条件显示
- [ ] 15.5 测试 TreasureUI：宝物显示、基础效果汇总、特殊效果和羁绊显示
- [ ] 15.6 测试 LevelUpUI：四个阶段的数据切换和显示
- [ ] 15.7 测试 StoryUI：故事文本的正确读取和显示
- [ ] 15.8 测试左侧列表切换：ChessContent ↔ TreasureContent 的切换效果
- [ ] 15.9 测试宝物仓库加载：宝物列表的完整性和数据正确性
- [ ] 15.10 测试宝物拖拽系统：拖拽到槽位、放置、数据同步等
- [ ] 15.11 测试立绘/模型切换：SwitchBtn 的切换效果和性能
- [ ] 15.12 性能测试：列表滚动的帧率，数据加载的耗时，拖拽时的性能
- [ ] 15.13 边界情况测试：0 个棋子、0 个宝物、超大列表、缺失数据的处理

## 16. 优化与打磨

- [ ] 16.1 优化 UI 动画：标签页切换的过度效果（如淡入淡出）、左侧列表切换的过度效果
- [ ] 16.2 优化立绘/模型切换的视觉效果
- [ ] 16.3 优化宝物拖拽的视觉反馈：更平滑的预览效果、阴影和发光
- [ ] 16.4 优化列表滚动性能：虚拟列表（如果需要）
- [ ] 16.5 添加加载状态提示：数据加载中的显示反馈
- [ ] 16.6 国际化支持：所有文本使用配置表或语言系统
- [ ] 16.7 代码审查与重构：确保代码质量和可读性
