## Context

项目采用 Unity GameFramework 框架，UI 系统基于 GF UI 模块（UITable 注册，UIViews 枚举），数据由 DataTable（Excel → .bytes）驱动，热修复代码在 `Hotfix.asmdef` 内。物品系统目前仅有 `ItemTable` 基础定义，尚无背包/仓库 UI 或格子管理逻辑。玩家移动由 `ThirdPersonController` 或类似组件驱动，背包开启时需对外暴露锁定接口。

## Goals / Non-Goals

**Goals:**
- 实现完整的背包 UI（网格、分页、拖拽、吸附、开关）
- 实现仓库 UI（网格、一键存入、拖拽）
- 实现道具快捷栏 UI
- 实现召唤物装备栏 UI（水平滑动）
- 实现场景物品拾取与背包内物品交互（详情、移动、使用、拆分、丢弃）
- 实现一键整理（堆叠 → 分类 → 稀有度排序）
- 扩展配置表（负重、仓库容量、资源规则）
- 背包开启时锁定玩家移动

**Non-Goals:**
- 装备强化/合成/交易系统
- 网络同步/多人背包
- 物品掉落/生成逻辑（由关卡/战斗系统负责）
- 商城/商店 UI

## Decisions

### 1. UI 架构：独立 UIForm vs 子面板
**决策**：背包、仓库、快捷栏、召唤物装备栏均作为独立 `UIForm` 注册到 UITable，通过 GF.UI 系统打开/关闭。

**原因**：GF UIForm 生命周期管理完善，各面板可独立显示层级，符合项目现有规范。快捷栏常驻显示，背包/仓库为弹出式；独立 UIForm 便于层级控制。

**备选**：将背包/仓库/快捷栏合并为一个 UIForm 内的多个 CanvasGroup。**拒绝**：耦合度过高，快捷栏需要常驻而背包需要弹出，生命周期不一致。

---

### 2. 背包格子管理：运行时生成 vs 预制体池
**决策**：背包格子通过对象池（GF Entity 或自定义 SimplePool）运行时生成，初始容量从 `ResourceRuleTable` 读取，扩容时动态增加格子数。

**原因**：格子数量可变（可扩展），预制体内固定格子数无法适应配置化。对象池复用避免频繁 Instantiate。

**备选**：ScrollView + ContentSizeFitter 自动布局。**采用**：配合 GridLayoutGroup，布局自动适配，仅需控制激活格子数量。

---

### 3. 物品拖拽：UGUI EventSystem vs 自定义 Raycast
**决策**：使用 UGUI `IDragHandler` / `IBeginDragHandler` / `IEndDragHandler` 接口，拖拽时在顶层 Canvas 创建临时拖拽图标，落点通过 `OverlapPoint` 检测目标格子。

**原因**：与 GF UI 系统兼容性好，无需额外依赖，网格吸附逻辑简单（找最近格子中心）。

---

### 4. 物品数据模型：运行时 InventoryItem vs DataTable 直接引用
**决策**：定义运行时 `InventoryItem` 类，包含：`ItemId`（对应 ItemTable）、`Count`（数量）、`Durability`（当前耐久）、`SlotIndex`（当前格子位置）。背包持久化由 `PlayerDataManager` 负责序列化。

**原因**：运行时物品状态（数量、耐久）是动态数据，不适合放 DataTable。DataTable 仅存静态配置（基础重量、最大堆叠数、稀有度等）。

---

### 5. 配置表扩展策略
**决策**：
- `PlayerDataTable`：新增 `WeightLimit`（int，负重上限）、`WeightMoveSpeedEffect`（float，负重超限时移速系数）
- `PlayerInitTable`：新增 `InitWarehouseCapacity`（int，初始仓库格子数）
- 新增 `ResourceRuleTable`：`InitInventorySlots`、`MaxExtendSlots`、`ExtendBaseCost`、`ExtendSlotsPerUpgrade`、`InitWarehouseSlots`

**原因**：负重是玩家属性（随成长变化），归入 PlayerDataTable 合理；初始仓库容量是初始化配置，归入 PlayerInitTable；背包/仓库规则是全局资源配置，独立成表便于设计师调整。

---

### 6. 玩家移动锁定接口
**决策**：在 `PlayerController`（或 `ThirdPersonController`）暴露 `SetMovementLocked(bool)` 方法；背包 `InventoryUIForm.OnOpen()` 调用锁定，`OnClose()` 解锁。

**原因**：单向依赖（UI → Player），符合现有架构，无需事件总线。

---

### 7. 一键整理算法
**决策**：
1. 同类物品堆叠（Count 合并，不超过 MaxStack）
2. 按 `ItemType` 分组排序（武器→防具→消耗品→材料→其他）
3. 组内按 `Rarity` 降序排列

**原因**：规则简单直观，无需外部依赖，O(n log n) 复杂度对背包规模（≤100格）完全够用。

## Risks / Trade-offs

- **拖拽性能**：大量格子同时更新可能卡顿 → 仅更新涉及格子的 UI，不全量刷新
- **DataTable 生成**：新增字段需重新运行 DataTableGenerator，生成文件不可手改 → 任务中明确列出需要用户手动运行生成步骤
- **背包存档**：物品持久化方案（PlayerPrefs / 文件 / 云存档）本期仅实现内存状态，存档集成留给后续 Phase
- **重量系统**：负重超限对移速的实时影响需要每次物品增减后重新计算 → 在 `InventoryManager` 的 `OnItemAdded/Removed` 事件中触发计算
- **UI 层级**：背包打开时鼠标事件可能被场景 Raycast 穿透 → 背包 Canvas 使用足够高的 SortingOrder，并在开启时屏蔽场景输入

## Migration Plan

1. 扩展 `ItemTable.xlsx`（确认重量、耐久度、稀有度、可堆叠字段已存在或新增）
2. 扩展 `PlayerDataTable.xlsx` 和 `PlayerInitTable.xlsx`
3. 新建 `ResourceRuleTable.xlsx`
4. 运行 DataTableGenerator 生成 .cs 和 .bytes
5. 实现 InventoryManager（运行时数据管理）
6. 实现各 UIForm 脚本
7. 在 UITable 注册新 UI
8. 创建 Prefab 并绑定变量
9. 联调：拾取 → 背包显示 → 仓库存取 → 快捷使用

**回滚**：各 UIForm 独立，删除对应预制体和 UITable 条目即可回滚，不影响其他系统。

## Open Questions

- `ItemTable` 中是否已有 `Weight`（重量）、`MaxStack`（最大堆叠）、`Rarity`（稀有度）、`Durability`（最大耐久）字段？需确认后再决定是新增还是复用。
- 仓库是否与背包共用同一个 `InventoryUIForm`（切换面板），还是独立打开？（建议独立，但需与策划确认）
- 快捷栏槽位数量是固定的（如 5 格）还是也从配置表读取？
