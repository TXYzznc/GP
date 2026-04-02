## Why

在战斗系统中引入策略卡机制，为玩家提供更多战术选择和策略深度。策略卡作为消耗性战术资源，可以在关键时刻改变战局，增强游戏的策略性和可玩性。

## What Changes

- **新增策略卡系统**：在战斗阶段的 CombatUI 中新增 CardSlots 区域，动态刷新 8 张策略卡供玩家使用（这部分的预制体和变量已经准备好了）
- **拖拽交互机制**：实现策略卡的拖拽释放功能，包括范围预览、吸附效果、取消释放等交互
- **效果执行系统**：策略卡效果与现有技能系统（召唤师技能、棋子技能）执行机制相似，可以进行参考。但是策略卡的效果会更加多样化，可能相比技能系统，策略卡系统中通用的效果比较少。基本每个策略卡都需要单独的脚本控制效果。（后续可以合并一部分相似的，但是目前还都比较特殊，适合单独控制）
- **配置表扩展**：扩展 CardTable 配置表，支持策略卡的完整配置（效果类型、参数、资源等）（已完成并更新了xlsx配置表和.cs文件）
- **UI 预制体集成**：基于现有 CardSlotItem 预制体和脚本，实现策略卡的 UI 显示和交互
- **卡牌信息详情显示**：当选中卡牌时，会显示DetailInfoUI（CombatUI.varDetailInfoUI）,然后调用DetailInfoUI的SetData和RefrushUI方法来显示选中的卡牌的信息
- **卡牌选中效果**：左键点击未选中的卡牌，可以选中该卡片，选中后除了对这张卡牌进行一下卡牌标记以外，没有啥效果。选中的卡牌会向上移动20单位位置（作为基础选中效果），同时显示DetailInfoUI并显示这张卡牌的信息。同时最多选中一张卡牌，当左键点击已选中的卡牌时，会将这张卡牌恢复未选中状态（同时需要更新DetailInfoUI状态：清除数据，刷新显示，然后隐藏DetailInfoUI）
- **卡牌显示效果**：卡牌（CardSlotItem）的Btn中的Image显示为这张卡牌对应的图片资源。

## Capabilities

### New Capabilities

- `card-drag-drop`: 策略卡拖拽释放系统，包括拖拽检测、范围预览、吸附效果、释放判定
- `card-effect-execution`: 策略卡效果执行系统，参考现有技能效果机制
- `card-slot-management`: 策略卡槽管理系统，负责卡牌刷新、显示、销毁等生命周期管理
- `card-range-preview`: 策略卡范围预览系统，显示作用范围和卡槽吸附范围

### Modified Capabilities

- `combat-ui`: CombatUI 需要集成 CardSlots 区域，管理策略卡的显示和交互（已经有了，变量为CombatUI.varCardSlots）

## Impact

**受影响的系统**：
- **CombatUI**：需要添加 CardSlots 区域和相关管理逻辑
- **技能效果系统**：策略卡效果可以参考现有的技能效果执行机制（SummonerSkillTable、SummonChessSkillTable 的效果系统）
- **输入系统**：需要处理策略卡的拖拽输入（鼠标左键拖拽）
- **资源系统**：需要加载策略卡图标、预制体、特效等资源

**新增文件**：
- `CardManager.cs` - 策略卡管理器
- `CardDragHandler.cs` - 策略卡拖拽处理
- `CardEffectExecutor.cs` - 策略卡效果执行器
- `CardRangePreview.cs` - 策略卡范围预览
- `CardSlotItem.cs` - 策略卡槽 UI 组件（可能已存在，需要扩展）

**配置表变更**：
- `CardTable.xlsx` - 扩展字段以支持策略卡完整配置（已设计完成）

**依赖关系**：
- 依赖 CombatUI 的现有架构
- 依赖 ResourceExtension 进行资源加载
