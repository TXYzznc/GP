## 1. 配置表扩展（DataTable）

- [x] 1.1 确认 `ItemTable.xlsx` 是否已有 Weight、MaxStack、Rarity、MaxDurability 字段；如无则新增
- [x] 1.2 在 `PlayerDataTable.xlsx` 新增 `WeightLimit`（int）、`WeightMoveSpeedEffect`（float）字段
- [x] 1.3 在 `PlayerInitTable.xlsx` 新增 `InitWarehouseCapacity`（int）字段
- [x] 1.4 新建 `ResourceRuleTable.xlsx`，包含字段：`InitInventorySlots`、`MaxExtendSlots`、`ExtendBaseCost`、`ExtendSlotsPerUpgrade`、`InitWarehouseSlots`，填入初始数据
- [x] 1.5 运行 DataTableGenerator，生成 `ResourceRuleTable.cs` / `.bytes` 及更新 `PlayerDataTable.cs`、`PlayerInitTable.cs`、`ItemTable.cs` 

## 2. 运行时数据层（InventoryManager）

- [x] 2.1 定义 `InventoryItem` 运行时类（ItemId, Count, Durability, SlotIndex）
- [x] 2.2 创建 `InventoryManager.cs`，实现 `AddItem()`、`RemoveItem()`、`MoveItem(slotA, slotB)`、`OnInventoryChanged` 事件
- [x] 2.3 在 `InventoryManager` 中实现负重计算：`CalculateCurrentWeight()` 并在物品变动时触发移速更新
- [x] 2.4 实现一键整理算法：`SortInventory()`（堆叠 → ItemType 分组 → Rarity 降序）
- [x] 2.5 创建 `WarehouseManager.cs`，实现仓库存取逻辑（`StoreItem()`、`RetrieveItem()`、`StoreAll()`）

## 3. 背包 UI（InventoryUIForm）

- [x] 3.1 在 `UITable.xlsx` 注册 `InventoryUIForm`（新行），运行 Generator 更新 `UIViews` 枚举
- [x] 3.2 用 ui-scaffold 工具生成 `InventoryUIForm.cs` 脚本模板
- [x] 3.3 创建 `InventoryUIForm.prefab`，添加 GridLayoutGroup 背包区域、分页指示器、整理按钮、详情面板
- [] 3.4 实现格子对象池：`InventorySlotPool`，根据 `ResourceRuleTable.InitInventorySlots` 初始化格子数量（背包格子数量为最大容量（PlayerDataTable.InventorySize）+n个背景为黑色的格子，表示待解锁格子。n为（6-背包最大容量%6）） **【待实现】**
- [x] 3.5 实现分页逻辑：A/D 键切换页，更新格子显示内容与页码指示器（页签UI暂未创建，等待用户手动创建后再写代码） **【页签已创建完毕：PageLabelItem，InventoryUI.varPageLabelItem】**
- [x] 3.6 实现 `InventorySlotUI` 组件：显示物品图标、稀有度背景色（读 `ColorTable`）、物品堆叠数量（不可堆叠显示x1）
- [x] 3.7 实现左键点击格子显示详情面板（物品名、图标、稀有度、重量、描述）
- [x] 3.8 实现右键点击弹出上下文菜单（使用/拆分/丢弃），根据物品类型动态显示选项（道具可使用/拆分/丢弃，宝物可丢弃，装备可使用/丢弃，任务道具不可丢弃）
- [x] 3.9 实现整理按钮：调用 `InventoryManager.SortInventory()` 并刷新 UI
- [x] 3.10 在 `InventoryUIForm.OnOpen()` 锁定玩家移动；`OnClose()` 解锁

## 4. 物品拖拽系统

- [x] 4.1 创建 `InventoryDragHandler.cs`，实现 `IBeginDragHandler`、`IDragHandler`、`IEndDragHandler` 接口
- [x] 4.2 拖拽开始时在顶层 Canvas 生成临时拖拽图标（克隆物品图标）
- [x] 4.3 拖拽结束时通过 `OverlapPoint` 检测目标格子（背包/仓库/快捷栏），触发对应 Manager 的移动/存取逻辑
- [x] 4.4 无效拖拽（非格子区域）时物品图标返回原格子

## 5. 仓库 UI（WarehouseUIForm）

- [x] 5.1 在 `UITable.xlsx` 注册 `WarehouseUIForm`，运行 Generator 更新枚举
- [x] 5.2 生成 `WarehouseUIForm.cs` 脚本模板与 `WarehouseUIForm.prefab`
- [] 5.3 实现仓库格子网格显示，容量读自玩家数据中的仓库容量字段 **【待实现】**
- [x] 5.4 实现"一键存入"按钮：调用 `WarehouseManager.StoreAll()`，空间不足时显示提示
- [!] 5.5 复用 `InventoryDragHandler` 支持背包↔仓库拖拽 **【待实现】**

## 6. 快捷栏 UI（ItemHotbarUIForm）

- [x] 6.1 在 `UITable.xlsx` 注册 `ItemHotbarUIForm`，运行 Generator
- [x] 6.2 生成 `ItemHotbarUIForm.cs` 脚本模板与 `ItemHotbarUIForm.prefab`（底部 HUD 常驻）
- [x] 6.3 实现快捷栏槽位（默认 5 格），支持从背包拖入（引用绑定，非移动物品）
- [x] 6.4 实现数字键 1-5 快捷使用：触发物品使用效果，减少数量，耗尽时清除图标

## 7. 召唤物装备栏 UI（SummonEquipmentUIForm）

- [x] 7.1 在 `UITable.xlsx` 注册 `SummonEquipmentUIForm`，运行 Generator

召唤物装备栏移动到了InventoryUI里面，作为InventoryUI的一部分：InventoryUI.varEquipScrollView

- [ ] 7.2 生成 `SummonEquipmentUIForm.cs` 脚本模板与 `SummonEquipmentUIForm.prefab`
- [ ] 7.3 使用 `ScrollRect`（水平）显示召唤物装备栏条目，内容从 `SummonChessTable` 读取玩家召唤物列表
- [ ] 7.4 每个召唤物条目显示装备槽图标（已装备显示物品图标，空槽显示占位图）

## 8. 场景物品交互

- [x] 8.1 创建 `WorldItemPickup.cs` 组件（挂在场景掉落物品预制体上），实现鼠标悬浮 Tooltip 显示
- [x] 8.2 实现左键点击：调用 `InventoryManager.AddItem()`，背包满时显示"背包已满"提示
- [x] 8.3 实现右键点击（可使用物品）：直接触发物品使用效果，物品从场景移除

## 9. 玩家技能 UI 迁移

- [x] 9.1 修改玩家技能 UI 布局，将技能 UI 移动至屏幕左上角
- [x] 9.2 确认迁移后不影响技能触发与 CD 显示逻辑

## 10. Tab 快捷键入口

- [x] 10.1 在主游戏输入处理器中注册 Tab 键回调（如 `GamePlayInputHandler`）
- [x] 10.2 Tab 按下时检查背包状态：未打开则 `GF.UI.OpenUI(UIViews.InventoryUIForm)`，已打开则 `GF.UI.CloseUI()`

## 11. 联调与验证

- [ ] 11.1 场景拾取物品 → 背包显示正确（图标、稀有度色、耐久）
- [ ] 11.2 背包拖拽移格 → 仓库存取 → 快捷栏引用绑定，全流程验证
- [ ] 11.3 超重后玩家移速降低验证（对比 `WeightMoveSpeedEffect` 系数）
- [ ] 11.4 一键整理验证（堆叠、分类、稀有度排序结果正确）
- [ ] 11.5 背包打开/关闭时玩家移动锁定/解锁验证
