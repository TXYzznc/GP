# 背包快照与结算资源计算 - 实现完成文档

**实现日期**: 2026-04-22  
**版本**: 1.0  
**状态**: ✅ P0-P1 功能实现完成

---

## 一、实现概述

### 需求总结

1. **背包快照**：进入局内时保存背包状态，结算时对比价值差
2. **虚拟物品处理**：清理背包中的虚拟物品，转换为账号资源
3. **资源计算**：使用背包价值差计算结算收益

### 完成情况

| 需求 | 状态 | 文件 | 说明 |
|------|------|------|------|
| 背包快照 | ✅ 完成 | InventoryManager.cs | CreateSnapshot/GetSnapshot/ClearSnapshot |
| 价值计算 | ✅ 完成 | InventoryManager.cs | CalculateInventoryValue (实例/静态) |
| 虚拟物品转换 | ✅ 完成 | InventoryManager.cs | ConvertVirtualItems() |
| 虚拟物品常量 | ✅ 完成 | InventoryManager.cs | VIRTUAL_ITEM_* 常量 |
| 结算数据扩展 | ✅ 完成 | SettlementData.cs | ResourceGain, VirtualGold 等字段 |
| 结算逻辑更新 | ✅ 完成 | SettlementManager.cs | 数据收集和奖励应用 |
| 宝箱虚拟物品处理 | ✅ 完成 | TreasureBoxSlotContainerImpl.cs | TakeAll() 优化 |
| 快照创建 | ✅ 完成 | InGameState.cs | OnEnter 中调用 CreateSnapshot |

---

## 二、关键文件修改

### 1. InventoryManager.cs

**新增常量**：
```csharp
public const int VIRTUAL_ITEM_GOLD = 999;           // 金币
public const int VIRTUAL_ITEM_ORIGIN_STONE = 99999; // 起源石
public const int VIRTUAL_ITEM_SPIRIT_STONE = 9999;  // 灵石（局内临时货币）
```

**新增方法**：
- `CreateSnapshot()` - 保存快照
- `GetSnapshot()` - 获取快照
- `ClearSnapshot()` - 清除快照
- `CalculateInventoryValue()` - 计算当前背包价值
- `CalculateInventoryValue(List<InventoryItemSaveData>)` - 计算指定数据的价值
- `ConvertVirtualItems()` - 清理虚拟物品并返回统计

**功能说明**：

```csharp
// 进入局内时调用
inventory.CreateSnapshot();

// 结算时获取数据
var snapshot = inventory.GetSnapshot();
int snapshotValue = InventoryManager.CalculateInventoryValue(snapshot);
int currentValue = inventory.CalculateInventoryValue();
int resourceGain = currentValue - snapshotValue;

// 结算时清理虚拟物品
var (gold, originStone, spiritStone) = inventory.ConvertVirtualItems();

// 结算完成后清除快照
inventory.ClearSnapshot();
```

### 2. SettlementData.cs

**新增字段**：
```csharp
public int ResourceGain { get; set; }              // 背包价值差
public int VirtualGold { get; set; }               // 虚拟金币数量
public int VirtualOriginStone { get; set; }        // 虚拟起源石数量
public int VirtualSpiritStone { get; set; }        // 虚拟灵石数量
```

### 3. SettlementManager.cs

**修改 CollectSettlementDataAsync()**：
- 计算背包价值差
- 清理并收集虚拟物品
- 记录详细日志

**修改 ApplyRewardsAsync()**：
- 应用资源收益（价值差）
- 应用虚拟金币
- 应用虚拟起源石
- 清理灵石（不保存）
- 清除背包快照

### 4. TreasureBoxSlotContainerImpl.cs

**优化 TakeAll() 方法**：
- 金币直接转换为 PlayerSaveData.Gold
- 起源石直接转换为 PlayerSaveData.OriginStone
- 灵石直接删除（不保存到任何地方）
- 普通物品保留进入背包的行为

### 5. InGameState.cs

**在 OnEnter() 中添加**：
```csharp
InventoryManager.Instance?.CreateSnapshot();
DebugEx.LogModule("InGameState", "已创建背包快照");
```

---

## 三、工作流程

### 完整的生命周期

```
1. 进入局内 (InGameState.OnEnter)
   └─ InventoryManager.CreateSnapshot()
      └─ 保存当前背包到 m_SnapshotBeforeSession

2. 玩家探索和获得物品
   ├─ 拾取物品 → InventoryManager.AddItem()
   ├─ 打开宝箱 → TreasureChestInteractable.OpenChestAsync()
   └─ 宝箱全部拿走 → TreasureBoxSlotContainerImpl.TakeAll()
      └─ 虚拟物品直接转换为账号资源

3. 触发结算 (SettlementManager.TriggerSettlementAsync)
   └─ 1. CollectSettlementDataAsync()
        ├─ GetSnapshot() 获取快照
        ├─ CalculateInventoryValue() 计算价值差
        └─ ConvertVirtualItems() 清理残留虚拟物品
      
      2. ApplyRewardsAsync()
        ├─ AddGold(ResourceGain) 应用价值差
        ├─ AddGold(VirtualGold) 应用虚拟金币
        ├─ AddOriginStone(VirtualOriginStone) 应用虚拟起源石
        └─ ClearSnapshot() 清除快照

4. 返回基地
   └─ 背包快照已清除，准备下一次进入
```

---

## 四、关键数据流

### 虚拟物品的三个来源

| 来源 | 时机 | 处理方式 | 最终结果 |
|------|------|--------|--------|
| **宝箱获得** | TakeAll() 时 | 直接转换为资源 | 不进入背包 |
| **背包中残留** | 结算时 ConvertVirtualItems() | 扫描并转换 | 确保没有遗漏 |
| **灵石** | TakeAll() 或 ConvertVirtualItems() | 删除（不保存） | 局内货币，不持久化 |

### 资源收益计算

```
资源收益 = 当前背包总价值 - 快照背包总价值
         = Σ(当前物品价值 × 数量) - Σ(快照物品价值 × 数量)

最终金币 = 资源收益 + 虚拟金币 + 其他来源金币

最终起源石 = 虚拟起源石
```

---

## 五、测试指南

### 场景1：基础快照创建

**步骤**：
1. 启动游戏进入局内
2. 检查日志是否显示 "背包快照已保存"
3. 查看 `InventoryManager.m_SnapshotBeforeSession` 是否有数据

**验证**：
- ✓ 快照成功创建
- ✓ 快照中的物品数量正确
- ✓ 物品ID和数量匹配

### 场景2：背包价值计算

**准备**：
- 确保 ItemTable 中所有物品都配置了 Value 字段
- 清空背包开始测试

**步骤**：
1. 进入局内（背包为空，价值=0）
2. 拾取 苹果（Value=10）x 3
3. 触发结算
4. 检查日志中的背包价值对比

**验证**：
- ✓ 进入局内价值 = 0
- ✓ 当前背包价值 = 30
- ✓ 资源收益 = 30

### 场景3：宝箱虚拟物品处理

**步骤**：
1. 打开宝箱，观察生成的物品
2. 检查是否包含虚拟物品（金币、起源石、灵石）
3. 点击 "全部拿走"
4. 检查账号资源是否增加
5. 检查背包中是否仍有虚拟物品残留

**验证**：
- ✓ 宝箱成功生成虚拟物品
- ✓ TakeAll() 后账号资源正确增加
- ✓ 背包中无虚拟物品残留
- ✓ 日志显示虚拟物品转换信息

### 场景4：结算流程完整验证

**步骤**：
1. 空背包进入局内
2. 拾取 苹果（Value=10）x 5 → 总价值50
3. 打开宝箱获得：金币×100、起源石×10、灵石×50
4. 点击 TakeAll()
   - 预期：金币+100，起源石+10，灵石删除，背包中无虚拟物品
5. 再次拾取 苹果（Value=10）x 3 → 当前价值80（5+3）
6. 触发结算

**预期结果**：
```
背包价值对比：
  进入局内 = 0
  当前 = 80
  收益 = 80

虚拟物品：
  金币 = 0（已通过 TakeAll 转换）
  起源石 = 0（已通过 TakeAll 转换）
  灵石 = 0（已删除）

最终金币 = 原有 + 80（收益） + 100（宝箱虚拟）
最终起源石 = 原有 + 10（宝箱虚拟）
```

### 场景5：边界条件测试

**测试项**：

| 条件 | 期望结果 |
|------|--------|
| 快照为 null（未调用 CreateSnapshot） | ResourceGain = 0，不崩溃 |
| 背包价值减少（玩家丢弃物品） | ResourceGain = 0（Mathf.Max） |
| ItemTable 未加载 | 日志警告，计算结果为0 |
| Value 字段未配置（=0） | 该物品不计价值 |
| 虚拟物品数量超大（1000000+） | 正常转换，无溢出 |
| 多次调用 ConvertVirtualItems() | 第一次清理，后续返回(0,0,0) |

---

## 六、日志输出示例

### 进入局内
```
[InGameState] 进入局内状态
[InGameState] 已创建背包快照
```

### 结算时
```
[SettlementManager] 开始收集结算数据...
[SettlementManager] 背包价值对比: 进入局内=0, 当前=80, 收益=80
[InventoryManager] 虚拟物品清理: 金币 x0 → 账号资源
[InventoryManager] 虚拟物品清理: 起源石 x0 → 账号资源
[InventoryManager] 虚拟物品清理: 灵石 x0 → 删除（局内货币）
[SettlementManager] 数据收集完成: 资源收益=80, 金币=0, 起源石=0, 灵石=0, 经验=100

[SettlementManager] 开始应用结算奖励
[SettlementManager] 获得经验: 100
[SettlementManager] 获得资源（价值）: 80
[SettlementManager] 获得资源（虚拟物品）: 100
[SettlementManager] 获得起源石（虚拟物品）: 10
[SettlementManager] 灵石（局内货币）已清理: 50
[InventoryManager] 背包快照已清除
[SettlementManager] 奖励应用完成，存档已保存
```

### 宝箱获得虚拟物品
```
[TreasureChest] [GenerateTreasureItems] 生成金币物品: 金币 x100（虚拟物品，待用户获取）
[TreasureChest] [GenerateTreasureItems] 生成灵石物品: 灵石 x50（虚拟物品，待用户获取）

[TreasureBoxContainer] 金币 x100 → 账号资源
[TreasureBoxContainer] 起源石 x10 → 账号资源
[TreasureBoxContainer] 灵石 x50 → 删除（局内货币）
[TreasureBoxContainer] 全部拿走: 成功 X 件
```

---

## 七、常见问题

### Q1: ItemTable.Value 字段如何配置？

**A**: 在 ItemTable.txt 中添加 Value 列（如果未添加）：
- 可设置为物品的售价 (SellPrice)
- 或设置为独立的估值

虚拟物品的 Value 可保持为 0，因为它们不会参与背包价值计算。

### Q2: 为什么灵石被直接删除？

**A**: 灵石是局内货币，仅在战斗中使用，不需要持久化到账号。删除灵石强制了"局内货币"的语义，避免玩家误会可以保留。

### Q3: 如果玩家在结算前没有调用 ConvertVirtualItems() 会怎样？

**A**: SettlementManager.CollectSettlementDataAsync() 中会调用 ConvertVirtualItems()，所以结算时一定会清理虚拟物品。这是双重保险机制。

### Q4: 快照存储在哪里？

**A**: 快照存储在内存中的 `InventoryManager.m_SnapshotBeforeSession`，仅在会话期间有效。结算完成后清除。

### Q5: 如果中途重新进入局内会怎样？

**A**: InGameState.OnEnter() 会重新调用 CreateSnapshot()，覆盖旧快照。这符合预期行为。

---

## 八、后续优化方向

### 已识别但未实现的功能

1. **配置表 Value 字段填充**
   - 需要对所有物品配置 Value 字段
   - 建议值 = SellPrice 或单独估值系统

2. **虚拟物品来源追踪**
   - 当前只统计数量，未区分来源（宝箱/其他）
   - 可扩展为详细的收获报告

3. **结算 UI 展示**
   - 当前只在日志中输出
   - 需要在结算界面展示：资源收益、虚拟物品等详细数据

4. **跨局数据保存**
   - 当前结算数据不持久化
   - 可考虑保存结算统计到账号数据

---

## 九、验证检查清单

- [ ] InventoryManager 能否正确创建快照
- [ ] InventoryManager 能否正确计算背包价值
- [ ] InventoryManager 能否正确清理虚拟物品
- [ ] SettlementManager 能否正确收集结算数据
- [ ] SettlementManager 能否正确应用奖励
- [ ] TreasureBoxSlotContainerImpl 能否正确处理虚拟物品
- [ ] InGameState 能否正确调用 CreateSnapshot
- [ ] 日志输出正确且无报错
- [ ] 所有场景下虚拟物品无残留
- [ ] 账号资源在结算后正确增加

---

## 十、文件清单

| 文件 | 修改类型 | 行数 | 描述 |
|------|--------|------|------|
| SettlementData.cs | 新增字段 | 4 | 资源收益相关字段 |
| InventoryManager.cs | 新增功能 | 140+ | 快照、价值计算、虚拟物品转换 |
| SettlementManager.cs | 修改逻辑 | 60+ | 数据收集和奖励应用 |
| TreasureBoxSlotContainerImpl.cs | 优化方法 | 60+ | TakeAll() 虚拟物品处理 |
| InGameState.cs | 新增调用 | 2 | CreateSnapshot 调用 |

---

**实现完成日期**: 2026-04-22  
**代码审查状态**: 待审查  
**功能测试状态**: 待测试
