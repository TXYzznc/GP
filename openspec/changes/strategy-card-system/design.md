## Context

**现有架构**：
- CombatUI 已有 CardSlots 区域（varCardSlots）和 CardSlotItem 预制体（varCardSlotItem）
- CardSlotItem.cs 已存在基础框架，包含 SetData() 和 OnCardClicked() 方法
- 现有技能系统（SummonerSkillManager）使用 ISummonerSkill 接口 + Factory 模式创建技能实例
- 技能通过 PlayerInputManager 触发，使用 TryCast() 方法执行
- 资源加载统一使用 ResourceExtension.LoadSpriteAsync()

**配置表现状**：
- CardTable 已完成设计，包含 19 个字段（Id, Name, Desc, IconId, PrefabConfig, SpiritCost, TargetType, CastRange, AreaRadius, DamageType, DamageCoeff, BaseDamage, InstantBuffs, HitBuffs, ParamsConfig, EffectId, HitEffectId, EffectSpawnHeight, Rarity, UnlockCondition）
- PrefabConfig 和 ParamsConfig 使用 JSON 格式存储配置
- 已配置 12 张策略卡数据

**技术约束**：
- 使用 UniTask 进行异步操作，不用协程
- 所有配置读 DataTable，不硬编码
- 输入必须走 PlayerInputManager
- 日志使用 DebugEx

## Goals / Non-Goals

**Goals:**
- 在战斗阶段显示 8 张策略卡供玩家使用
- 实现拖拽释放交互，包括范围预览和吸附效果
- 执行策略卡效果（伤害、治疗、Buff、特殊效果等）
- 策略卡使用后销毁，从卡槽中移除

**Non-Goals:**
- 策略卡抽卡/刷新机制（后续实现）
- 策略卡获取/解锁系统（后续实现）
- 策略卡动画效果优化（初版使用基础动画）
- 策略卡 AI 使用逻辑（仅玩家可用）

## Decisions

### 1. 拖拽实现方式

**决策**：使用 Unity EventSystem 的 IBeginDragHandler、IDragHandler、IEndDragHandler 接口

**理由**：
- 与 Unity UI 系统原生集成，无需额外输入处理
- 自动处理拖拽事件的生命周期
- 支持多点触控（移动端扩展）

**替代方案**：
- ❌ 手动监听 Input.GetMouseButton：需要自己管理状态，代码复杂
- ❌ 使用 PlayerInputManager：PlayerInputManager 专注于游戏输入，UI 拖拽不适合

**实现细节**：
- CardSlotItem 实现拖拽接口
- OnBeginDrag：创建拖拽预览对象，显示范围指示器
- OnDrag：更新预览位置，检测吸附区域
- OnEndDrag：判断释放位置，执行效果或回归卡槽

### 2. 范围预览机制

**决策**：使用 Projector 或 DecalProjector 显示地面范围圈

**理由**：
- 直观显示作用范围
- 支持不规则地形投影
- 性能开销可控

**替代方案**：
- ❌ LineRenderer 绘制圆圈：不支持地形投影，视觉效果差
- ❌ UI Canvas 覆盖层：无法准确对应 3D 世界坐标

**实现细节**：
- 预制体包含 Projector 组件和范围材质
- 根据 CardTable.AreaRadius 动态调整投影大小
- 拖拽时实时更新投影位置
- 释放后销毁投影对象

### 3. 效果执行复用

**决策**：每张策略卡使用独立的效果脚本，不强制复用

**理由**：
- 策略卡效果多样化，强行抽象会增加复杂度
- 独立脚本便于快速迭代和调试
- 后续可以重构合并相似效果

**替代方案**：
- ❌ 统一效果执行器 + 配置驱动：初期效果类型不明确，过早抽象会限制设计
- ❌ 完全复用技能系统：策略卡的目标选择和执行时机与技能不同

**实现细节**：
- 创建 ICardEffect 接口定义执行方法
- 每张卡实现独立的 CardEffect 类（如 HolyShieldCardEffect）
- CardEffectExecutor 根据 CardId 创建对应效果实例
- 效果脚本可以参考现有的 Buff、伤害、治疗等底层系统

### 4. 卡槽管理策略

**决策**：CombatUI 负责卡槽生命周期，CardManager 负责卡牌数据管理

**理由**：
- 职责分离：UI 管理显示，Manager 管理数据
- CombatUI 已有 RefreshCardSlots() 方法，扩展即可
- CardManager 作为单例，便于其他系统访问卡牌数据

**替代方案**：
- ❌ 全部放在 CombatUI：职责过重，不利于测试
- ❌ 全部放在 CardManager：UI 逻辑与数据逻辑耦合

**实现细节**：
- CardManager 维护当前可用卡牌列表（List<CardData>）
- CombatUI.RefreshCardSlots() 从 CardManager 获取数据并创建 UI
- CardSlotItem 使用后通知 CardManager 移除卡牌
- CardManager 触发事件，CombatUI 监听并刷新 UI

### 6. 卡牌选中交互

**决策**：使用单选模式，左键点击切换选中状态，选中时显示详情 UI

**理由**：
- 简化交互逻辑，同时最多选中一张卡
- 选中状态提供视觉反馈（向上移动 20 单位）
- 与 DetailInfoUI 联动，显示卡牌详细信息

**替代方案**：
- ❌ 多选模式：增加复杂度，不符合当前需求
- ❌ 悬停显示详情：移动端不支持悬停

**实现细节**：
- CardSlotItem 维护 isSelected 状态
- 点击未选中卡牌：设为选中，向上移动 20 单位，显示 DetailInfoUI
- 点击已选中卡牌：取消选中，恢复位置，隐藏 DetailInfoUI
- 选中新卡牌时自动取消之前选中的卡牌
- DetailInfoUI 调用 SetData(cardData) 和 RefreshUI() 显示信息

### 7. 卡牌图标显示

**决策**：CardSlotItem 的 Btn 中的 Image 组件显示卡牌图标

**理由**：
- 直观显示卡牌内容
- 使用 ResourceExtension.LoadSpriteAsync() 异步加载
- 与现有资源加载方式一致

**实现细节**：
- 在 SetData() 中加载 CardTable.IconId 对应的图标
- 使用 ResourceExtension.LoadSpriteAsync(iconId, btnImage) 直接加载到 Image
- 加载失败时使用默认占位图

### 5. DetailInfoUI 集成

**决策**：使用 CombatUI.varDetailInfoUI 显示选中卡牌的详细信息

**理由**：
- 复用现有 DetailInfoUI 组件
- 统一的信息展示方式
- 减少重复开发

**实现细节**：
- CardSlotItem 选中时调用 DetailInfoUI.SetData(cardData)
- 调用 DetailInfoUI.RefreshUI() 刷新显示
- 显示 DetailInfoUI（SetActive(true)）
- 取消选中时清除数据并隐藏 DetailInfoUI

### 8. 目标选择机制

**决策**：根据 TargetType 自动选择目标，无需手动点选

**理由**：
- 简化交互流程，拖拽释放即可
- 配置表已定义目标类型（自身/友方/敌方/全体）
- 初版优先快速实现，后续可扩展手动选择

**替代方案**：
- ❌ 手动点选目标：增加交互步骤，影响战斗节奏
- ❌ 智能推荐目标：需要 AI 逻辑，复杂度高

**实现细节**：
- TargetType=1（自身）：直接作用于玩家召唤师
- TargetType=2（友方单体）：选择最近的友方棋子
- TargetType=3（友方全体）：获取所有友方棋子
- TargetType=4（敌方单体）：选择释放位置最近的敌人
- TargetType=5（敌方全体）：获取所有敌方单位
- TargetType=6（全场）：获取所有单位

## Risks / Trade-offs

### 风险 1：拖拽性能问题
**风险**：OnDrag 每帧调用，频繁的射线检测和范围更新可能影响性能

**缓解措施**：
- 限制射线检测频率（每 0.1 秒检测一次）
- 使用对象池管理范围预览对象
- 拖拽时降低其他 UI 更新频率

### 风险 2：效果脚本数量膨胀
**风险**：12 张卡需要 12 个效果脚本，后续扩展会导致文件数量激增

**缓解措施**：
- 第一版先实现独立脚本，验证效果设计
- 第二版重构时提取通用效果类（如 DamageCardEffect、BuffCardEffect）
- 使用配置驱动通用效果，特殊效果保留独立脚本

### 风险 3：卡槽吸附判定不准确
**风险**：拖拽回卡槽时吸附范围判定可能不符合预期

**缓解措施**：
- 使用 RectTransformUtility.RectangleContainsScreenPoint 精确判定
- 提供可调节的吸附阈值参数
- 添加视觉反馈（高亮卡槽边框）提示吸附状态

### 风险 4：配置表 JSON 解析错误
**风险**：PrefabConfig 和 ParamsConfig 使用 JSON 格式，解析失败会导致卡牌无法使用

**缓解措施**：
- 使用 JsonUtility 或 Newtonsoft.Json 进行解析
- 添加异常捕获和日志输出
- 提供默认配置作为 fallback
- 配置表填写时提供 JSON 格式校验工具

### 风险 5：战斗中卡牌数据同步
**风险**：卡牌使用后需要同步到存档，避免重复使用

**缓解措施**：
- CardManager 使用后立即从列表中移除
- 战斗结束时保存卡牌使用记录到 CombatSessionData
- 战斗失败时恢复卡牌（可选设计）

## Migration Plan

**部署步骤**：
1. 创建 CardManager 和基础数据结构
2. 扩展 CardSlotItem 实现拖拽接口
3. 实现范围预览系统
4. 创建 CardEffectExecutor 和第一个效果脚本（神圣庇护）
5. 集成到 CombatUI，测试完整流程
6. 逐步添加其他 11 张卡的效果脚本

**回滚策略**：
- 保留原有 CardSlotItem.cs 的备份
- 新增代码使用 #if ENABLE_STRATEGY_CARD 宏控制
- 出现问题时可以快速禁用功能

**测试计划**：
- 单元测试：CardManager 的卡牌管理逻辑
- 集成测试：拖拽交互和范围预览
- 战斗测试：每张卡的效果执行和目标选择

## Open Questions

1. **卡牌消耗资源**：CardTable 有 SpiritCost 字段，但当前战斗系统没有"灵力"资源，是使用金币还是新增资源？
   - 建议：初版忽略消耗，直接使用；后续根据平衡性需求决定
   - 灵力是召唤师的属性之一：就是玩家战斗状态下的MP值（目前应该已经有这个属性了？）

2. **卡牌获取来源**：玩家如何获得策略卡？战前配置还是战斗中获取？
   - 初版在战斗开始时随机分配 8 张卡；后续再根据玩法和需求进行扩展

3. **范围预览颜色**：友方/敌方/全场的范围圈使用什么颜色区分？
   - 范围圈统一使用黄色

4. **拖拽取消**：拖拽到无效区域（如 UI 外）时如何处理？
   - 建议：自动回归卡槽，播放取消音效
   - 注意：无效区域是指既不在返回卡槽区域，也不在战场区域
   - 关于范围和拖拽：
   CombatUI中有个varCardSlotAdsorptionArea，当鼠标在这个区域时，显示这个区域对象的Image组件；当松开拖拽时，如果鼠标落点在这个区域，则将当前选中的卡牌返回卡槽。
   拖拽时，从鼠标处发射射线（和放置棋子时的预览效果类似），如果射线在战场区域，则显示卡牌的作用范围预览（这部分可以参考放置棋子时的预览效果实现方式）
   如果鼠标发射的射线不在战场区域，并且鼠标也不在varCardSlotAdsorptionArea区域，则表示此时处于无效区域，如果松开鼠标，则把卡牌返回卡槽
