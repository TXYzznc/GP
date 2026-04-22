using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宝箱格子容器实现 - 固定大小的格子数组
/// 规则：宝箱 ↔ 背包（双向）
/// 格子索引 = 存储数组索引（直接对应，格子N存储在m_Slots[N]）
/// </summary>
public class TreasureBoxSlotContainerImpl : SlotContainerBase
{
    /// <summary>宝箱格子容量</summary>
    private const int TREASURE_BOX_CAPACITY = 50;

    /// <summary>格子更新事件（宝箱内容变化时触发，用于通知UI刷新）</summary>
    public event Action OnSlotChanged;

    /// <summary>格子数组（固定大小50，格子索引 = 数组索引）</summary>
    private readonly InventoryItem[] m_Slots = new InventoryItem[TREASURE_BOX_CAPACITY];

    public override SlotContainerType ContainerType => SlotContainerType.TreasureBox;

    /// <summary>
    /// 用初始物品列表初始化宝箱（只在宝箱首次打开时调用）
    /// </summary>
    public void Initialize(List<ItemStack> initialItems)
    {
        // 检查是否已初始化（如果任何格子有物品，就认为已初始化）
        bool hasItems = false;
        for (int i = 0; i < TREASURE_BOX_CAPACITY; i++)
        {
            if (m_Slots[i] != null)
            {
                hasItems = true;
                break;
            }
        }

        if (hasItems)
            return; // 已初始化，不重复初始化

        if (initialItems == null || initialItems.Count == 0)
        {
            DebugEx.Log("TreasureBoxContainer", "宝箱初始化为空");
            OnSlotChanged?.Invoke();
            return;
        }

        // 将物品放入对应格子
        int itemCount = 0;
        for (int i = 0; i < initialItems.Count && i < TREASURE_BOX_CAPACITY; i++)
        {
            var itemStack = initialItems[i];
            if (itemStack != null && !itemStack.IsEmpty && itemStack.Item != null)
            {
                m_Slots[i] = new InventoryItem(itemStack.Item.ItemId, itemStack.Count, 0, i);
                itemCount++;
            }
        }

        DebugEx.Log("TreasureBoxContainer", $"宝箱初始化完成，物品数={itemCount}");
        OnSlotChanged?.Invoke();
    }

    public override InventorySlot GetSlot(int slotIndex)
    {
        var slot = new InventorySlot(slotIndex);

        // 直接从数组获取该格子的物品
        if (slotIndex >= 0 && slotIndex < TREASURE_BOX_CAPACITY && m_Slots[slotIndex] != null)
        {
            var item = m_Slots[slotIndex];
            if (item.Count > 0)
            {
                var itemObj = ItemManager.Instance?.CreateItem(item.ItemId);
                if (itemObj != null)
                {
                    slot.SetItem(itemObj, item.Count);
                }
            }
        }

        return slot;
    }

    /// <summary>
    /// 宝箱允许与背包交互、以及内部拖拽
    /// </summary>
    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        return otherContainerType == SlotContainerType.Inventory
            || otherContainerType == SlotContainerType.TreasureBox;
    }

    protected override bool ExecuteMove(
        int fromSlotIndex,
        ISlotContainer targetContainer,
        int targetSlotIndex
    )
    {
        var fromItem = m_Slots[fromSlotIndex];
        if (fromItem == null || fromItem.Count <= 0)
            return false;

        var itemId = fromItem.ItemId;
        var count = fromItem.Count;

        // 检查目标格子是否为空
        var targetSlot = targetContainer.GetSlot(targetSlotIndex);
        bool targetIsEmpty = targetSlot == null || targetSlot.IsEmpty;

        string targetStatus = targetIsEmpty ? "为空" : "非空";
        DebugEx.Log(
            "TreasureBoxContainer",
            $"[宝箱→{targetContainer.ContainerType}] {fromSlotIndex} → {targetSlotIndex} (目标{targetStatus})"
        );

        bool success = targetContainer switch
        {
            InventorySlotContainerImpl inv => MoveToInventory(inv, itemId, count, targetSlotIndex),
            TreasureBoxSlotContainerImpl tb => MoveToTreasureBox(
                fromSlotIndex,
                tb,
                targetSlotIndex
            ),
            _ => false,
        };

        // 清空源格子：目标为空 OR 目标是同种物品（堆叠）
        // 不清空：宝箱→宝箱（MoveToTreasureBox 已处理）
        if (success && targetContainer is not TreasureBoxSlotContainerImpl)
        {
            if (targetIsEmpty || targetSlot.ItemId == itemId)
            {
                RemoveItem(fromSlotIndex, count);
            }
        }

        return success;
    }

    private bool MoveToInventory(
        InventorySlotContainerImpl _,
        int itemId,
        int count,
        int targetSlotIndex
    )
    {
        var inv = InventoryManager.Instance;
        var targetSlot = inv?.GetSlot(targetSlotIndex);

        if (targetSlot == null)
            return false;

        if (targetSlot.IsEmpty)
        {
            var item = ItemManager.Instance?.CreateItem(itemId);
            if (item == null)
                return false;

            targetSlot.SetItem(item, count);

            // 刷新背包UI（如果打开的话）
            RefreshInventoryUIIfOpen();
            return true;
        }
        else if (targetSlot.ItemId == itemId && targetSlot.ItemStack?.Item?.MaxStackCount > 1)
        {
            targetSlot.AddItem(count);

            // 刷新背包UI（如果打开的话）
            RefreshInventoryUIIfOpen();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 宝箱内部拖拽（宝箱→宝箱）
    /// 支持两种操作：1) 移动到空格子或堆叠，2) 交换两个非空格子
    /// </summary>
    private bool MoveToTreasureBox(
        int fromSlotIndex,
        TreasureBoxSlotContainerImpl targetTb,
        int targetSlotIndex
    )
    {
        if (targetTb == null || fromSlotIndex < 0 || fromSlotIndex >= TREASURE_BOX_CAPACITY)
            return false;

        var fromItem = m_Slots[fromSlotIndex];
        if (fromItem == null || fromItem.Count <= 0)
            return false;

        int itemId = fromItem.ItemId;
        int count = fromItem.Count;

        // 首先尝试接收（移动或堆叠）
        bool success = targetTb.ReceiveItemFromTreasureBox(itemId, count, targetSlotIndex);
        bool isSwap = false;

        // 接收失败时，尝试交换
        if (!success && targetSlotIndex >= 0 && targetSlotIndex < TREASURE_BOX_CAPACITY)
        {
            var targetItem = targetTb.m_Slots[targetSlotIndex];
            if (targetItem != null) // 目标格子有物品，尝试交换
            {
                success = targetTb.SwapItemWithTreasureBox(targetSlotIndex, this, fromSlotIndex);
                isSwap = success;
            }
        }

        // 只在移动成功时清空源格子（交换时源格子现在有对方的物品，不应清空）
        if (success && !isSwap)
        {
            m_Slots[fromSlotIndex] = null;
            OnSlotChanged?.Invoke();
        }

        return success;
    }

    /// <summary>
    /// 如果背包UI打开，刷新它的显示
    /// </summary>
    private void RefreshInventoryUIIfOpen()
    {
        // 通过查找场景中的InventoryUI来刷新（如果打开的话）
        var inventoryUI = UnityEngine.Object.FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.RefreshAll();
            DebugEx.Log("TreasureBoxContainer", "[MoveToInventory] 已刷新背包UI");
        }
    }

    /// <summary>
    /// 宝箱间物品交换（仅限同一宝箱内或两个宝箱之间）
    /// 源格子和目标格子的物品互换位置
    /// </summary>
    public bool SwapItemWithTreasureBox(
        int fromSlotIndex,
        TreasureBoxSlotContainerImpl targetTb,
        int targetSlotIndex
    )
    {
        if (
            targetTb == null
            || fromSlotIndex < 0
            || fromSlotIndex >= TREASURE_BOX_CAPACITY
            || targetSlotIndex < 0
            || targetSlotIndex >= TREASURE_BOX_CAPACITY
        )
            return false;

        var fromItem = m_Slots[fromSlotIndex];
        var targetItem = targetTb.m_Slots[targetSlotIndex];

        // 两个格子都为空或都为非空才能交换
        // 如果一个为空一个非空，应该用 Move 而不是 Swap
        if ((fromItem == null && targetItem == null) || (fromItem == null) != (targetItem == null))
            return false;

        // 交换物品
        m_Slots[fromSlotIndex] = targetItem;
        targetTb.m_Slots[targetSlotIndex] = fromItem;

        DebugEx.Log("TreasureBoxContainer", $"[宝箱交换] 格子 {fromSlotIndex} ↔ {targetSlotIndex}");

        OnSlotChanged?.Invoke();
        targetTb.OnSlotChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 宝箱间物品移动（内部拖拽）
    /// 移动指定数量的物品到目标宝箱的目标格子
    /// </summary>
    public bool ReceiveItemFromTreasureBox(int itemId, int count, int targetSlotIndex = -1)
    {
        if (itemId <= 0 || count <= 0)
            return false;

        // 如果指定了目标格子
        if (targetSlotIndex >= 0 && targetSlotIndex < TREASURE_BOX_CAPACITY)
        {
            var targetItem = m_Slots[targetSlotIndex];
            if (targetItem == null)
            {
                // 目标格子为空，直接存入
                m_Slots[targetSlotIndex] = new InventoryItem(itemId, count, 0, targetSlotIndex);
                DebugEx.Log(
                    "TreasureBoxContainer",
                    $"[宝箱→宝箱] 物品直接存入格子: ID={itemId}, 数量={count}, 格子={targetSlotIndex}"
                );
                OnSlotChanged?.Invoke();
                return true;
            }
            else if (targetItem.ItemId == itemId)
            {
                // 目标格子有相同物品，尝试堆叠
                var itemData = ItemManager.Instance?.GetItemData(itemId);
                if (itemData != null && itemData.MaxStackCount > 1)
                {
                    int addCount = Mathf.Min(count, itemData.MaxStackCount - targetItem.Count);
                    int oldCount = targetItem.Count;
                    targetItem.Count += addCount;
                    DebugEx.Log(
                        "TreasureBoxContainer",
                        $"[宝箱→宝箱] 物品堆叠: ID={itemId}, 数量 {oldCount} -> {targetItem.Count}"
                    );
                    OnSlotChanged?.Invoke();

                    // 如果还有剩余物品，存入新格子
                    int remainCount = count - addCount;
                    if (remainCount > 0)
                    {
                        return StoreItem(itemId, remainCount);
                    }
                    return true;
                }
            }
        }
        else
        {
            // 自动查找可用格子
            var itemData2 = ItemManager.Instance?.GetItemData(itemId);

            // 先查找是否已有相同物品可以堆叠
            for (int i = 0; i < TREASURE_BOX_CAPACITY; i++)
            {
                if (
                    m_Slots[i] != null
                    && m_Slots[i].ItemId == itemId
                    && itemData2 != null
                    && itemData2.MaxStackCount > 1
                )
                {
                    int addCount = Mathf.Min(count, itemData2.MaxStackCount - m_Slots[i].Count);
                    int oldCount = m_Slots[i].Count;
                    m_Slots[i].Count += addCount;
                    DebugEx.Log(
                        "TreasureBoxContainer",
                        $"[宝箱→宝箱] 物品堆叠: ID={itemId}, 数量 {oldCount} -> {m_Slots[i].Count}"
                    );
                    OnSlotChanged?.Invoke();

                    int remainCount = count - addCount;
                    if (remainCount > 0)
                    {
                        return StoreItem(itemId, remainCount);
                    }
                    return true;
                }
            }

            // 存入新物品
            return StoreItem(itemId, count);
        }

        return false;
    }

    /// <summary>
    /// 从背包拖拽物品到宝箱
    /// targetSlotIndex：用户想放入的目标格子，如果为-1则自动查找
    /// </summary>
    public bool ReceiveItemFromInventory(int itemId, int count, int targetSlotIndex = -1)
    {
        // 如果指定了目标格子，直接放入
        if (targetSlotIndex >= 0 && targetSlotIndex < TREASURE_BOX_CAPACITY)
        {
            var targetItem = m_Slots[targetSlotIndex];
            if (targetItem == null)
            {
                // 目标格子为空，直接存入
                m_Slots[targetSlotIndex] = new InventoryItem(itemId, count, 0, targetSlotIndex);
                DebugEx.Log(
                    "TreasureBoxContainer",
                    $"物品直接存入格子: ID={itemId}, 数量={count}, 格子={targetSlotIndex}"
                );
                OnSlotChanged?.Invoke();
                return true;
            }
            else if (targetItem.ItemId == itemId)
            {
                // 目标格子有相同物品，尝试堆叠
                var itemData = ItemManager.Instance?.GetItemData(itemId);
                if (itemData != null && itemData.MaxStackCount > 1)
                {
                    int addCount = Mathf.Min(count, itemData.MaxStackCount - targetItem.Count);
                    int oldCount = targetItem.Count;
                    targetItem.Count += addCount;
                    DebugEx.Log(
                        "TreasureBoxContainer",
                        $"物品堆叠: ID={itemId}, 数量 {oldCount} -> {targetItem.Count}"
                    );
                    OnSlotChanged?.Invoke();

                    // 如果还有剩余物品，存入新格子
                    int remainCount = count - addCount;
                    if (remainCount > 0)
                    {
                        return StoreItem(itemId, remainCount);
                    }
                    return true;
                }
            }
        }

        // 自动查找可用格子
        var itemData2 = ItemManager.Instance?.GetItemData(itemId);

        // 先查找是否已有相同物品可以堆叠
        for (int i = 0; i < TREASURE_BOX_CAPACITY; i++)
        {
            if (
                m_Slots[i] != null
                && m_Slots[i].ItemId == itemId
                && itemData2 != null
                && itemData2.MaxStackCount > 1
            )
            {
                int addCount = Mathf.Min(count, itemData2.MaxStackCount - m_Slots[i].Count);
                int oldCount = m_Slots[i].Count;
                m_Slots[i].Count += addCount;
                DebugEx.Log(
                    "TreasureBoxContainer",
                    $"物品堆叠: ID={itemId}, 数量 {oldCount} -> {m_Slots[i].Count}"
                );
                OnSlotChanged?.Invoke();

                int remainCount = count - addCount;
                if (remainCount > 0)
                {
                    return StoreItem(itemId, remainCount);
                }
                return true;
            }
        }

        // 存入新物品
        return StoreItem(itemId, count);
    }

    /// <summary>
    /// 存入物品到宝箱（自动查找可用格子）
    /// </summary>
    private bool StoreItem(int itemId, int count)
    {
        // 查找第一个空格子
        for (int i = 0; i < TREASURE_BOX_CAPACITY; i++)
        {
            if (m_Slots[i] == null)
            {
                m_Slots[i] = new InventoryItem(itemId, count, 0, i);
                DebugEx.Log(
                    "TreasureBoxContainer",
                    $"物品存入: ID={itemId}, 数量={count}, 格子={i}"
                );
                OnSlotChanged?.Invoke();
                return true;
            }
        }

        DebugEx.Warning("TreasureBoxContainer", "宝箱已满");
        return false;
    }

    /// <summary>
    /// 移除物品（拖拽操作使用）
    /// </summary>
    public bool RemoveItem(int slotIndex, int count)
    {
        if (slotIndex < 0 || slotIndex >= TREASURE_BOX_CAPACITY)
            return false;

        var item = m_Slots[slotIndex];
        if (item == null || item.Count <= 0)
            return false;

        item.Count -= count;
        if (item.Count <= 0)
        {
            m_Slots[slotIndex] = null;
            DebugEx.Log("TreasureBoxContainer", $"[RemoveItem] 物品已完全移除: 格子={slotIndex}");
        }
        else
        {
            DebugEx.Log(
                "TreasureBoxContainer",
                $"[RemoveItem] 物品部分移除: 格子={slotIndex}, 剩余数量={item.Count}"
            );
        }

        OnSlotChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 将所有物品放入背包（全部拿走按钮用）
    /// ⭐ 虚拟物品特殊处理：直接转换为账号资源，不进入背包
    /// </summary>
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

    /// <summary>
    /// 宝箱是否已清空
    /// </summary>
    public bool IsEmpty()
    {
        for (int i = 0; i < TREASURE_BOX_CAPACITY; i++)
        {
            if (m_Slots[i] != null)
                return false;
        }
        return true;
    }
}
