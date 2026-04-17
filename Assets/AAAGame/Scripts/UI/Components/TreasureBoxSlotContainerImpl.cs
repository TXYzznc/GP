using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宝箱格子容器实现 - 每个宝箱实例有独立的物品存储
/// 规则：宝箱 ↔ 背包（双向）
/// 支持拖拽、右键快捷转移、全部拿走等操作
/// 数据持久化在容器内部，直到宝箱对象被销毁
/// </summary>
public class TreasureBoxSlotContainerImpl : SlotContainerBase
{
    /// <summary>宝箱格子容量</summary>
    private const int TREASURE_BOX_CAPACITY = 50;

    /// <summary>格子更新事件（宝箱内容变化时触发，用于通知UI刷新）</summary>
    public event Action OnSlotChanged;

    /// <summary>内部物品列表（格子索引 → InventoryItem）</summary>
    private readonly List<InventoryItem> m_TreasureItems = new();

    public override SlotContainerType ContainerType => SlotContainerType.TreasureBox;

    /// <summary>
    /// 用初始物品列表初始化宝箱（只在宝箱首次打开时调用）
    /// </summary>
    public void Initialize(List<ItemStack> initialItems)
    {
        if (m_TreasureItems.Count > 0)
            return;  // 已初始化，不重复初始化

        m_TreasureItems.Clear();

        if (initialItems == null || initialItems.Count == 0)
        {
            DebugEx.Log("TreasureBoxContainer", "宝箱初始化为空");
            OnSlotChanged?.Invoke();
            return;
        }

        for (int i = 0; i < initialItems.Count && i < TREASURE_BOX_CAPACITY; i++)
        {
            var itemStack = initialItems[i];
            if (itemStack != null && !itemStack.IsEmpty && itemStack.Item != null)
            {
                var item = new InventoryItem(itemStack.Item.ItemId, itemStack.Count, 0, i);
                m_TreasureItems.Add(item);
            }
        }

        DebugEx.Log("TreasureBoxContainer", $"宝箱初始化完成，物品数={m_TreasureItems.Count}");
        OnSlotChanged?.Invoke();
    }

    public override InventorySlot GetSlot(int slotIndex)
    {
        var slot = new InventorySlot(slotIndex);
        var item = GetItemBySlot(slotIndex);

        if (item != null && item.Count > 0)
        {
            var itemObj = ItemManager.Instance?.CreateItem(item.ItemId);
            if (itemObj != null)
            {
                slot.SetItem(itemObj, item.Count);
            }
        }

        return slot;
    }

    /// <summary>
    /// 宝箱允许与背包交互（双向拖拽）
    /// </summary>
    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        return otherContainerType == SlotContainerType.Inventory;
    }

    protected override bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        var fromItem = GetItemBySlot(fromSlotIndex);
        if (fromItem == null || fromItem.Count <= 0)
            return false;

        var itemId = fromItem.ItemId;
        var count = fromItem.Count;

        // 检查目标格子是否为空
        var targetSlot = targetContainer.GetSlot(targetSlotIndex);
        bool targetIsEmpty = targetSlot == null || targetSlot.IsEmpty;

        string targetStatus = targetIsEmpty ? "为空" : "非空";
        DebugEx.Log("TreasureBoxContainer",
            $"[宝箱→{targetContainer.ContainerType}] {fromSlotIndex} → {targetSlotIndex} (目标{targetStatus})");

        bool success = targetContainer switch
        {
            InventorySlotContainerImpl inv => MoveToInventory(inv, itemId, count, targetSlotIndex),
            _ => false
        };

        // 清空源格子：目标为空 OR 目标是同种物品（堆叠）
        if (success && (targetIsEmpty || targetSlot.ItemId == itemId))
        {
            RemoveItem(fromSlotIndex, count);
        }

        return success;
    }

    private bool MoveToInventory(InventorySlotContainerImpl _, int itemId, int count, int targetSlotIndex)
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
    /// 从背包拖拽物品到宝箱（背包格子调用）
    /// targetSlotIndex：用户想放入的目标格子，如果为-1则自动查找
    /// </summary>
    public bool ReceiveItemFromInventory(int itemId, int count, int targetSlotIndex = -1)
    {
        // 如果指定了目标格子且格子为空，直接存入该格子
        if (targetSlotIndex >= 0 && targetSlotIndex < TREASURE_BOX_CAPACITY)
        {
            var targetItem = GetItemBySlot(targetSlotIndex);
            if (targetItem == null)
            {
                // 目标格子为空，直接存入
                var item = new InventoryItem(itemId, count, 0, targetSlotIndex);
                m_TreasureItems.Add(item);
                DebugEx.Log("TreasureBoxContainer", $"物品直接存入格子: ID={itemId}, 数量={count}, 格子={targetSlotIndex}");
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
                    DebugEx.Log("TreasureBoxContainer",
                        $"物品堆叠: ID={itemId}, 数量 {oldCount} -> {targetItem.Count}");
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

        // 如果目标格子不可用，或为-1，则自动查找格子
        // 查找宝箱中是否已有相同物品
        var existingItem = GetItemById(itemId);
        var itemData2 = ItemManager.Instance?.GetItemData(itemId);

        if (existingItem != null && itemData2 != null && itemData2.MaxStackCount > 1)
        {
            // 堆叠到现有物品
            int addCount = Mathf.Min(count, itemData2.MaxStackCount - existingItem.Count);
            int oldCount = existingItem.Count;
            existingItem.Count += addCount;

            DebugEx.Log("TreasureBoxContainer",
                $"物品堆叠: ID={itemId}, 数量 {oldCount} -> {existingItem.Count}");

            OnSlotChanged?.Invoke();

            // 如果还有剩余物品，存入新格子
            int remainCount = count - addCount;
            if (remainCount > 0)
            {
                return StoreItem(itemId, remainCount);
            }

            return true;
        }

        // 存入新物品
        return StoreItem(itemId, count);
    }

    /// <summary>
    /// 存入物品到宝箱
    /// </summary>
    private bool StoreItem(int itemId, int count)
    {
        if (m_TreasureItems.Count >= TREASURE_BOX_CAPACITY)
        {
            DebugEx.Warning("TreasureBoxContainer", "宝箱已满");
            return false;
        }

        int slotIndex = FindAvailableSlot();
        if (slotIndex < 0)
            return false;

        var item = new InventoryItem(itemId, count, 0, slotIndex);
        m_TreasureItems.Add(item);

        DebugEx.Log("TreasureBoxContainer", $"物品存入: ID={itemId}, 数量={count}, 格子={slotIndex}");
        OnSlotChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 移除物品（拖拽操作使用）
    /// </summary>
    public bool RemoveItem(int slotIndex, int count)
    {
        var item = GetItemBySlot(slotIndex);
        if (item == null || item.Count <= 0)
            return false;

        item.Count -= count;
        if (item.Count <= 0)
        {
            m_TreasureItems.Remove(item);
            DebugEx.Log("TreasureBoxContainer", $"[RemoveItem] 物品已完全移除: 格子={slotIndex}");
        }
        else
        {
            DebugEx.Log("TreasureBoxContainer", $"[RemoveItem] 物品部分移除: 格子={slotIndex}, 剩余数量={item.Count}");
        }

        OnSlotChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 获取指定格子的物品
    /// </summary>
    private InventoryItem GetItemBySlot(int slotIndex)
    {
        foreach (var item in m_TreasureItems)
        {
            if (item.SlotIndex == slotIndex)
                return item;
        }
        return null;
    }

    /// <summary>
    /// 获取指定物品ID的物品
    /// </summary>
    private InventoryItem GetItemById(int itemId)
    {
        foreach (var item in m_TreasureItems)
        {
            if (item.ItemId == itemId)
                return item;
        }
        return null;
    }

    /// <summary>
    /// 查找可用格子索引
    /// </summary>
    private int FindAvailableSlot()
    {
        for (int i = 0; i < TREASURE_BOX_CAPACITY; i++)
        {
            if (GetItemBySlot(i) == null)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 将所有物品放入背包（全部拿走按钮用）
    /// </summary>
    public int TakeAll()
    {
        int successCount = 0;
        var itemsToTake = new List<InventoryItem>(m_TreasureItems);  // 复制列表，避免遍历时修改

        foreach (var item in itemsToTake)
        {
            bool ok = InventoryManager.Instance?.AddItem(item.ItemId, item.Count) ?? false;
            if (ok)
            {
                m_TreasureItems.Remove(item);
                successCount++;
            }
            else
            {
                DebugEx.Warning("TreasureBoxContainer", "背包已满，剩余物品无法全部放入");
                break;
            }
        }

        DebugEx.Log("TreasureBoxContainer", $"全部拿走: 成功 {successCount} 件");
        OnSlotChanged?.Invoke();
        return successCount;
    }

    /// <summary>
    /// 宝箱是否已清空
    /// </summary>
    public bool IsEmpty()
    {
        return m_TreasureItems.Count == 0;
    }
}
