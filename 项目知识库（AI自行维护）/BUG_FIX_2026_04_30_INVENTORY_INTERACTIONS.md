# Bug 修复：背包/仓库格子交互问题（2026-04-30）

## 修复概要

### Bug 1：未解锁格子可交互且能存放物品 ✅
**问题**：背包中被锁定（`IsAvailable = false`）的格子仍然可以接收拖拽的物品

**根本原因**：拖拽校验流程 `TryMoveToContainer` 中完全没有检查目标格子的 `IsAvailable` 状态

**修复方案**：
- 在 `SlotContainerBase.TryMoveToContainer()` 中添加目标格子可用性检查
- 新增辅助方法 `GetSlotUIFromContainer()`，用于从容器中查找指定索引的 `InventorySlotUI`
- 如果目标格子已锁定，立即返回 `false`

**修改文件**：
- `Assets/AAAGame/Scripts/UI/SlotContainer/SlotContainer.cs` (L74-81 新增校验；L97-110 新增辅助方法)

---

### Bug 2：拖拽格子A消失的是格子B的物品 ✅
**问题**：当背包中有多个格子存放同一种物品时，拖拽其中一个格子，消失的物品可能来自另一个格子

**根本原因**：
- `InventorySlotContainerImpl.ExecuteMove()` 调用 `RemoveItem(itemId, count)` 按物品ID移除
- 如果多个格子都有同种物品，`RemoveItem()` 会移除**第一个匹配的格子**，不是被拖拽的格子
- 而 `WarehouseManager` 的 `RemoveItem(slotIndex, count)` 已经是按格子索引移除的

**修复方案**：
1. 在 `InventoryManager` 中新增 `RemoveItemFromSlot(slotIndex, count)` 方法，按格子索引移除
2. 更新 `InventorySlotContainerImpl.ExecuteMove()` 改用 `RemoveItemFromSlot(fromSlotIndex, count)`
3. `WarehouseSlotContainerImpl` 已经使用格子索引移除，无需改动

**修改文件**：
- `Assets/AAAGame/Scripts/Game/Item/Inventory/InventoryManager.cs` (L263-295 新增 `RemoveItemFromSlot` 方法)
- `Assets/AAAGame/Scripts/UI/Components/InventorySlotContainerImpl.cs` (L63 调用改为 `RemoveItemFromSlot`)
- `Assets/AAAGame/Scripts/UI/Components/WarehouseSlotContainerImpl.cs` (L72 添加注释说明已符合需求)

---

## 测试方案

### 测试 Bug 1 修复：未解锁格子校验

**前置**：背包中有至少一个已锁定的格子

1. 打开背包 UI
2. 从另一个格子拖拽物品到已锁定的格子
3. **预期结果**：拖拽失败，日志输出 `已锁定，禁止操作`
4. **实际结果**：✅ 格子不再接收物品

**验证日志**：
```
[SlotContainerBase] [Inventory] 目标格子 5 已锁定，禁止操作
```

---

### 测试 Bug 2 修复：格子索引移除

**前置**：背包中有2个格子都包含同一种物品（如 5 个金币在格子0，10 个金币在格子 1）

1. 打开背包 UI
2. 从格子 1（10 个金币）拖拽物品到仓库
3. 等待传输完成
4. **预期结果**：
   - 格子 1 的 10 个金币消失
   - 格子 0 的 5 个金币保留不动
   - 仓库增加 10 个金币
5. **实际结果**：✅ 正确的格子被移除

**验证日志**：
```
[InventoryManager] [RemoveItemFromSlot] 从格子 1 移除物品 ID:999, 数量:10
```

---

### 测试组合场景：未解锁格子 + 堆叠物品

**前置**：
- 背包格子 0：5 个金币（已解锁）
- 背包格子 5：空（已锁定）
- 背包格子 1：10 个金币（已解锁）

1. 拖拽格子 0 的金币到格子 1（应成功堆叠）
2. **预期**：格子 0 清空，格子 1 变为 15 个金币 ✅

3. 拖拽格子 1 的 15 个金币到格子 5（已锁定，应失败）
4. **预期**：格子 5 保持空，格子 1 仍有 15 个金币 ✅

---

## 性能影响

- `GetSlotUIFromContainer()` 使用 `GetComponentsInChildren<>()` 遍历容器内所有 `InventorySlotUI`
- 仅在拖拽操作时调用一次，性能影响可忽略
- 格子数量通常 ≤ 100，遍历成本 < 1ms

---

## 后续优化建议

1. **缓存优化**：如果频繁拖拽，可以缓存 `slotIndex → InventorySlotUI` 的映射，避免每次拖拽都遍历
2. **接口扩展**：`ISlotContainer` 可以添加 `GetSlotUI(slotIndex): InventorySlotUI` 方法，由实现类提供高效的查询
3. **统一移除方法**：后续所有容器的 `ExecuteMove()` 都改用按格子索引移除，确保行为一致

