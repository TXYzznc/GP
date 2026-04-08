## Why

当前描边系统存在两套独立管理逻辑（`ChessOutlineController` 按阵营常驻描边、`OutlineDisplayManager` 按 Layer 自动扫描），职责重叠且存在严重问题（Editor-only 配置加载、FindObjectsOfType 性能瓶颈）。需求已变更：取消常驻阵营描边和 Layer 自动扫描，改为仅在两个交互场景下按需显示描边。

## What Changes

- **BREAKING** 删除 `ChessOutlineController`，替换为通用的 `OutlineController` 组件（不再绑定阵营逻辑，纯粹管理描边的显示/隐藏/颜色）
- **BREAKING** 删除 `OutlineDisplayManager` 及其 Editor 脚本 `OutlineDisplayManagerEditor`，移除 Layer 自动扫描描边机制
- 清理 `ChessEntity` 中对旧 `ChessOutlineController` 的引用，改为挂载新的 `OutlineController`
- 清理 `CampRelationService` 中仅服务描边的注释和无用分支（`GetRelationToLocalPlayer` 保留，策略卡场景仍需要）
- 保留底层渲染管线不变：`OutlineRenderFeature`、`OutlineConfig`、`OutlineTest`、Shader
- 修改 `ChessSelectionManager`：选中棋子时通过 `OutlineController` 显示黄色描边
- 修改 `CardSlotItem`：拖拽策略卡预览时，对 `GetAffectedTargets()` 返回的目标棋子显示描边，己方绿色、敌方红色；释放/取消时移除描边

## Capabilities

### New Capabilities
- `outline-controller`: 通用描边控制组件，挂载在需要描边的物体上，提供 `ShowOutline(color, size)` / `HideOutline()` API，替代原有的 `ChessOutlineController` 和 `OutlineDisplayManager`
- `card-target-outline`: 策略卡拖拽时对作用目标棋子显示阵营描边（己方绿/敌方红），集成到 `CardSlotItem` 的拖拽预览流程

### Modified Capabilities
- （无需修改现有 spec，仅清理实现代码）

## Impact

**需要修改的文件：**
- `Assets/AAAGame/Scripts/TA/OuterGlow/OutlineTest.cs` — 保留，可能微调为配合新 OutlineController
- `Assets/AAAGame/Scripts/Game/Combat/Camp/ChessOutlineController.cs` — **删除**
- `Assets/AAAGame/Scripts/TA/OuterGlow/OutlineDisplayManager.cs` — **删除**
- `Assets/Editor/OutlineDisplayManagerEditor.cs` — **删除**
- `Assets/AAAGame/Scripts/Game/SummonChess/Entity/ChessEntity.cs` — 修改：OutlineController 属性类型变更
- `Assets/AAAGame/Scripts/Game/Combat/Chess/ChessSelectionManager.cs` — 修改：选中描边改为黄色
- `Assets/AAAGame/Scripts/UI/Item/CombatItems/CardSlotItem.cs` — 修改：添加拖拽目标描边逻辑

**保留不动的文件：**
- `OutlineRenderFeature.cs`、`OutlineConfig.cs`、Shader 文件、`.asset` 配置资产
- `CampRelationService.cs`（策略卡目标判断仍需要阵营关系服务）
