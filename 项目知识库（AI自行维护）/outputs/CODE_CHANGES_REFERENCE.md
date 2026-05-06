# 代码修改参考 - 精确位置和内容

此文档提供所有代码修改的精确位置和内容摘要。

---

## File 1: InventoryManager.cs

### 位置：Assets/AAAGame/Scripts/Game/Item/Inventory/InventoryManager.cs

#### 修改 1: 虚拟物品常量定义（第 29-31 行）

```csharp
// 虚拟物品 ID 常量
public const int VIRTUAL_ITEM_GOLD = 999;           // 金币
public const int VIRTUAL_ITEM_ORIGIN_STONE = 99999; // 起源石
public const int VIRTUAL_ITEM_SPIRIT_STONE = 9999;  // 灵石（局内临时货币）
```

**用途**: 在整个项目中使用这些常量识别虚拟物品

---

#### 修改 2: 快照字段定义（第 34 行）

```csharp
// 背包快照数据（进入局内时保存）
private List<InventoryItemSaveData> m_SnapshotBeforeSession = null;
```

**用途**: 存储进入局内时的背包状态快照

---

#### 修改 3: CreateSnapshot() 方法（第 645-649 行）

```csharp
/// <summary>
/// 保存当前背包快照（进入局内时调用）
/// </summary>
public void CreateSnapshot()
{
    m_SnapshotBeforeSession = SaveInventory();
    DebugEx.Log("InventoryManager", $"背包快照已保存，物品数={m_SnapshotBeforeSession.Count}");
}
```

**调用时机**: InGameState.OnEnter() 中调用

**功能**: 复制当前背包到快照中

---

#### 修改 4: GetSnapshot() 方法（第 654-657 行）

```csharp
/// <summary>
/// 获取背包快照（结算时使用）
/// </summary>
public List<InventoryItemSaveData> GetSnapshot()
{
    return m_SnapshotBeforeSession;
}
```

**调用时机**: SettlementManager.CollectSettlementDataAsync() 中调用

**功能**: 返回进入局内时的背包快照

---

#### 修改 5: ClearSnapshot() 方法（第 662-666 行）

```csharp
/// <summary>
/// 清除背包快照（结算完成后调用）
/// </summary>
public void ClearSnapshot()
{
    m_SnapshotBeforeSession = null;
    DebugEx.Log("InventoryManager", "背包快照已清除");
}
```

**调用时机**: SettlementManager.ApplyRewardsAsync() 中调用

**功能**: 清理快照占用的内存

---

#### 修改 6: CalculateInventoryValue() 实例方法（第 671-693 行）

```csharp
/// <summary>
/// 计算背包总价值（基于 ItemTable.Value）
/// </summary>
public int CalculateInventoryValue()
{
    int totalValue = 0;
    var itemTable = GF.DataTable.GetDataTable<ItemTable>();
    if (itemTable == null)
    {
        DebugEx.Warning("InventoryManager", "ItemTable 未加载，无法计算背包价值");
        return 0;
    }

    foreach (var slot in m_Slots)
    {
        if (!slot.IsEmpty)
        {
            var itemRow = itemTable.GetDataRow(slot.ItemId);
            if (itemRow != null)
            {
                totalValue += itemRow.Value * slot.Count;
            }
        }
    }
    return totalValue;
}
```

**调用时机**: SettlementManager.CollectSettlementDataAsync() 中调用，用于获取当前背包价值

**功能**: 遍历所有格子，使用 ItemTable.Value 计算总价值

---

#### 修改 7: CalculateInventoryValue() 静态方法（第 698-719 行）

```csharp
/// <summary>
/// 计算指定背包数据的总价值
/// </summary>
public static int CalculateInventoryValue(List<InventoryItemSaveData> inventoryData)
{
    if (inventoryData == null) return 0;

    int totalValue = 0;
    var itemTable = GF.DataTable.GetDataTable<ItemTable>();
    if (itemTable == null)
    {
        DebugEx.Warning("InventoryManager", "ItemTable 未加载，无法计算背包价值");
        return 0;
    }

    foreach (var item in inventoryData)
    {
        var itemRow = itemTable.GetDataRow(item.ItemId);
        if (itemRow != null)
        {
            totalValue += itemRow.Value * item.Count;
        }
    }
    return totalValue;
}
```

**调用时机**: SettlementManager.CollectSettlementDataAsync() 中调用，用于计算快照价值

**功能**: 对指定的背包数据（而非当前背包）计算价值

---

#### 修改 8: ConvertVirtualItems() 方法（第 728-771 行）

```csharp
/// <summary>
/// 清理背包中的虚拟物品并转换到账号资源
/// 金币 → PlayerSaveData.Gold
/// 起源石 → PlayerSaveData.OriginStone
/// 灵石 → 直接删除（仅局内使用）
/// </summary>
/// <returns>清理的虚拟物品统计（金币、起源石、灵石数量）</returns>
public (int gold, int originStone, int spiritStone) ConvertVirtualItems()
{
    int goldCount = 0;
    int originStoneCount = 0;
    int spiritStoneCount = 0;

    // 遍历所有格子，收集虚拟物品数量
    foreach (var slot in m_Slots)
    {
        if (slot.IsEmpty) continue;

        switch (slot.ItemId)
        {
            case VIRTUAL_ITEM_GOLD:
                goldCount += slot.Count;
                break;
            case VIRTUAL_ITEM_ORIGIN_STONE:
                originStoneCount += slot.Count;
                break;
            case VIRTUAL_ITEM_SPIRIT_STONE:
                spiritStoneCount += slot.Count;
                break;
        }
    }

    // 移除背包中的虚拟物品
    if (goldCount > 0)
    {
        RemoveItem(VIRTUAL_ITEM_GOLD, goldCount);
        DebugEx.Log("InventoryManager", $"虚拟物品清理: 金币 x{goldCount} → 账号资源");
    }
    if (originStoneCount > 0)
    {
        RemoveItem(VIRTUAL_ITEM_ORIGIN_STONE, originStoneCount);
        DebugEx.Log("InventoryManager", $"虚拟物品清理: 起源石 x{originStoneCount} → 账号资源");
    }
    if (spiritStoneCount > 0)
    {
        RemoveItem(VIRTUAL_ITEM_SPIRIT_STONE, spiritStoneCount);
        DebugEx.Log("InventoryManager", $"虚拟物品清理: 灵石 x{spiritStoneCount} → 删除（局内货币）");
    }

    return (goldCount, originStoneCount, spiritStoneCount);
}
```

**调用时机**: SettlementManager.CollectSettlementDataAsync() 中调用

**返回值**: 元组 (金币数, 起源石数, 灵石数)

**功能**: 扫描并清理背包中的所有虚拟物品

---

## File 2: SettlementData.cs

### 位置：Assets/AAAGame/Scripts/Game/Settlement/SettlementData.cs

#### 修改：新增 4 个属性（第 35-45 行）

```csharp
/// <summary>通过背包价值差计算的资源收益</summary>
public int ResourceGain { get; set; }

/// <summary>虚拟物品：金币数量</summary>
public int VirtualGold { get; set; }

/// <summary>虚拟物品：起源石数量</summary>
public int VirtualOriginStone { get; set; }

/// <summary>虚拟物品：灵石数量（局内货币，仅统计不保存）</summary>
public int VirtualSpiritStone { get; set; }
```

**用途**: 在结算流程中存储资源计算结果

**默认值**: 均为 0（自动初始化）

---

## File 3: SettlementManager.cs

### 位置：Assets/AAAGame/Scripts/Game/Settlement/SettlementManager.cs

#### 修改 1: CollectSettlementDataAsync() 方法（第 130-166 行）

关键更改部分（第 140-156 行）：

```csharp
// 1. 计算背包价值差（资源收益）
var snapshot = InventoryManager.Instance?.GetSnapshot();
if (snapshot != null)
{
    int snapshotValue = InventoryManager.CalculateInventoryValue(snapshot);
    int currentValue = InventoryManager.Instance?.CalculateInventoryValue() ?? 0;
    m_CurrentSettlementData.ResourceGain = Mathf.Max(0, currentValue - snapshotValue);

    DebugEx.LogModule("SettlementManager",
        $"背包价值对比: 进入局内={snapshotValue}, 当前={currentValue}, 收益={m_CurrentSettlementData.ResourceGain}");
}

// 2. 清理并收集虚拟物品
var (gold, originStone, spiritStone) = InventoryManager.Instance?.ConvertVirtualItems() ?? (0, 0, 0);
m_CurrentSettlementData.VirtualGold = gold;
m_CurrentSettlementData.VirtualOriginStone = originStone;
m_CurrentSettlementData.VirtualSpiritStone = spiritStone;
```

**目的**: 计算背包价值差和虚拟物品统计

**关键算法**:
- 获取进入局内前的背包快照
- 计算快照的总价值
- 计算当前背包的总价值
- 使用 Mathf.Max(0, ...) 防止负收益
- 通过元组解构获取虚拟物品计数

---

#### 修改 2: ApplyRewardsAsync() 方法（第 240-302 行）

关键更改部分（第 257-284 行）：

```csharp
// ⭐ 应用资源收益（通过背包价值差计算）
if (m_CurrentSettlementData.ResourceGain > 0)
{
    accountManager.AddGold(m_CurrentSettlementData.ResourceGain);
    DebugEx.LogModule("SettlementManager", $"获得资源（价值）: {m_CurrentSettlementData.ResourceGain}");
}

// ⭐ 应用虚拟物品：金币
if (m_CurrentSettlementData.VirtualGold > 0)
{
    accountManager.AddGold(m_CurrentSettlementData.VirtualGold);
    DebugEx.LogModule("SettlementManager", $"获得金币（虚拟物品）: {m_CurrentSettlementData.VirtualGold}");
}

// ⭐ 应用虚拟物品：起源石
if (m_CurrentSettlementData.VirtualOriginStone > 0)
{
    accountManager.AddOriginStone(m_CurrentSettlementData.VirtualOriginStone);
    DebugEx.LogModule("SettlementManager", $"获得起源石（虚拟物品）: {m_CurrentSettlementData.VirtualOriginStone}");
}

// ⭐ 灵石不保存，仅记录日志
if (m_CurrentSettlementData.VirtualSpiritStone > 0)
{
    DebugEx.LogModule("SettlementManager",
        $"灵石（局内货币）已清理: {m_CurrentSettlementData.VirtualSpiritStone}");
}
```

最后添加（第 294 行）：

```csharp
// ⭐ 清理背包快照
InventoryManager.Instance?.ClearSnapshot();
```

**目的**: 应用所有结算奖励并清理快照

**应用顺序**:
1. 背包价值差 → 金币
2. 虚拟金币 → 金币
3. 虚拟起源石 → 起源石
4. 虚拟灵石 → 日志记录（不保存）
5. 清除快照

---

## File 4: TreasureBoxSlotContainerImpl.cs

### 位置：Assets/AAAGame/Scripts/UI/Components/TreasureBoxSlotContainerImpl.cs

#### 修改：TakeAll() 方法中的虚拟物品处理（第 505-569 行）

关键修改部分（第 516-561 行）：

```csharp
public int TakeAll()
{
    int successCount = 0;
    var accountManager = PlayerAccountDataManager.Instance;

    for (int i = 0; i < TREASURE_BOX_CAPACITY; i++)
    {
        if (m_Slots[i] != null)
        {
            var item = m_Slots[i];

            // ⭐ 虚拟物品特殊处理：直接转换为账号资源
            switch (item.ItemId)
            {
                case InventoryManager.VIRTUAL_ITEM_GOLD:
                    if (accountManager != null)
                    {
                        accountManager.AddGold(item.Count);
                        DebugEx.Log("TreasureBoxContainer", $"金币 x{item.Count} → 账号资源");
                    }
                    m_Slots[i] = null;
                    successCount++;
                    break;

                case InventoryManager.VIRTUAL_ITEM_ORIGIN_STONE:
                    if (accountManager != null)
                    {
                        accountManager.AddOriginStone(item.Count);
                        DebugEx.Log("TreasureBoxContainer", $"起源石 x{item.Count} → 账号资源");
                    }
                    m_Slots[i] = null;
                    successCount++;
                    break;

                case InventoryManager.VIRTUAL_ITEM_SPIRIT_STONE:
                    // 灵石直接删除（局内货币）
                    DebugEx.Log("TreasureBoxContainer", $"灵石 x{item.Count} → 删除（局内货币）");
                    m_Slots[i] = null;
                    successCount++;
                    break;

                default:
                    // 普通物品才加到背包
                    var inv = InventoryManager.Instance;
                    bool ok = inv != null && inv.AddItem(item.ItemId, item.Count);
                    if (ok)
                    {
                        m_Slots[i] = null;
                        successCount++;
                    }
                    else
                    {
                        DebugEx.Warning("TreasureBoxContainer", "背包已满，剩余物品无法全部放入");
                        goto EXIT_LOOP;
                    }
                    break;
            }
        }
    }

EXIT_LOOP:
    DebugEx.Log("TreasureBoxContainer", $"全部拿走: 成功 {successCount} 件");
    OnSlotChanged?.Invoke();
    return successCount;
}
```

**目的**: 在宝箱全部拿走时立即处理虚拟物品

**处理逻辑**:
- 虚拟金币 (999) → 直接调用 AddGold()
- 虚拟起源石 (99999) → 直接调用 AddOriginStone()
- 虚拟灵石 (9999) → 删除（不保存）
- 普通物品 → 保留原有行为（进入背包）

---

## File 5: InGameState.cs

### 位置：Assets/AAAGame/Scripts/GameState/States/InGameState.cs

#### 修改：OnEnter() 方法（第 73-74 行）

添加代码位置（在 InitInventoryAndWarehouse() 之后）：

```csharp
// ⭐ 保存进入局内时的背包快照
InventoryManager.Instance?.CreateSnapshot();
DebugEx.LogModule("InGameState", "已创建背包快照");
```

**完整上下文**（第 69-76 行）：

```csharp
// 初始化背包与仓库（容量从 PlayerInitTable 读取）
InitInventoryAndWarehouse();

// ⭐ 保存进入局内时的背包快照
InventoryManager.Instance?.CreateSnapshot();
DebugEx.LogModule("InGameState", "已创建背包快照");

// 订阅战斗结束事件
GF.Event.Subscribe(CombatEndEventArgs.EventId, OnCombatEnd);
```

**调用时机**: 进入局内时，在背包初始化完成后，所有子系统启动前

**功能**: 创建背包快照用于后续结算对比

---

## 总结表

| 文件 | 修改类型 | 行数 | 新增方法/字段 |
|------|--------|------|-------------|
| InventoryManager.cs | 新增功能 | 29-31, 34, 645-771 | 3 常量, 1 字段, 6 方法 |
| SettlementData.cs | 新增字段 | 35-45 | 4 属性 |
| SettlementManager.cs | 修改逻辑 | 140-156, 257-294 | 修改 2 个方法 |
| TreasureBoxSlotContainerImpl.cs | 优化方法 | 516-561 | 修改 switch 语句 |
| InGameState.cs | 新增调用 | 73-74 | 1 行调用 + 1 行日志 |

**总计**:
- 新增代码：~145 行
- 修改代码：~90 行
- 总计修改：~235 行

---

## 编译检查清单

在编译项目时，确保以下内容：

- [x] 所有文件都包含必要的 using 语句
- [x] InventoryManager.VIRTUAL_ITEM_* 常量被正确引用
- [x] ItemTable API 调用正确（GF.DataTable.GetDataTable<ItemTable>()）
- [x] 元组返回类型 (int, int, int) 支持（C# 7.0+）
- [x] 空合并运算符 ?. 和 ?? 支持（C# 6.0+）
- [x] 所有方法签名与调用点匹配

---

## 调试技巧

如果编译出错，按以下顺序检查：

1. **InventoryManager 编译错误**
   → 检查 ItemTable 是否正确导入
   → 检查 GF.DataTable API 是否可用

2. **SettlementData 编译错误**
   → 通常不会出错，只是新增属性

3. **SettlementManager 编译错误**
   → 检查 InventoryManager.Instance 是否可访问
   → 检查元组语法是否支持

4. **TreasureBoxSlotContainerImpl 编译错误**
   → 检查 InventoryManager.VIRTUAL_ITEM_* 常量是否可访问
   → 检查 accountManager 的方法是否存在

5. **InGameState 编译错误**
   → 通常不会出错，只是方法调用

---

**参考完整文档**: SETTLEMENT_RESOURCE_CALCULATION.md
