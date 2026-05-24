using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包UI管理器 - 负责物品与UI格子的映射和同步
/// 核心功能：
/// 1. 维护物品实例ID到格子UI的映射（一对多）
/// 2. 维护空闲格子队列（按容器类型分类）
/// 3. 提供O(1)复杂度的添加/移除物品操作
/// </summary>
public class InventoryUIManager
{
    #region 字段

    // 物品实例ID → 该物品所在的所有格子UI
    private Dictionary<int, List<InventorySlotUI>> m_ItemToSlotsMap;

    // 容器类型 → 空闲格子队列（按索引排序）
    private Dictionary<SlotContainerType, Queue<InventorySlotUI>> m_FreeSlotQueues;

    // 所有容器的格子列表
    private Dictionary<SlotContainerType, List<InventorySlotUI>> m_AllSlots;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化管理器
    /// </summary>
    public void Initialize(
        List<InventorySlotUI> inventorySlots,
        List<InventorySlotUI> equipSlots,
        List<InventorySlotUI> fastSlots
    )
    {
        m_ItemToSlotsMap = new Dictionary<int, List<InventorySlotUI>>();
        m_FreeSlotQueues = new Dictionary<SlotContainerType, Queue<InventorySlotUI>>();
        m_AllSlots = new Dictionary<SlotContainerType, List<InventorySlotUI>>();

        // 注册所有格子
        RegisterSlots(SlotContainerType.Inventory, inventorySlots);
        RegisterSlots(SlotContainerType.Equip, equipSlots);
        RegisterSlots(SlotContainerType.FastBar, fastSlots);

        DebugEx.Success("InventoryUIManager", "初始化完成");
    }

    /// <summary>
    /// 注册容器的所有格子
    /// </summary>
    private void RegisterSlots(SlotContainerType containerType, List<InventorySlotUI> slots)
    {
        if (slots == null || slots.Count == 0)
            return;

        m_AllSlots[containerType] = slots;

        var freeQueue = new Queue<InventorySlotUI>();
        foreach (var slot in slots)
        {
            var itemUI = slot.GetItemUI();
            if (itemUI == null || !itemUI.gameObject.activeSelf)
            {
                freeQueue.Enqueue(slot);
            }
        }

        m_FreeSlotQueues[containerType] = freeQueue;

        DebugEx.Log(
            "InventoryUIManager",
            $"注册容器 {containerType}: 总格子={slots.Count}, 空闲={freeQueue.Count}"
        );
    }

    /// <summary>
    /// 清理所有映射
    /// </summary>
    public void Clear()
    {
        m_ItemToSlotsMap?.Clear();
        m_FreeSlotQueues?.Clear();
        m_AllSlots?.Clear();
    }

    #endregion

    #region 添加物品

    /// <summary>
    /// 添加物品到UI（自动分配到合适的容器）
    /// </summary>
    public void AddItemToUI(ItemStack itemStack)
    {
        if (itemStack == null)
            return;

        var item = itemStack.Item;
        var instanceId = itemStack.InstanceId;

        // 1. 添加到 InventoryContent（所有物品都在这里）
        var inventorySlot = GetOrCreateFreeSlot(SlotContainerType.Inventory);
        if (inventorySlot != null)
        {
            SetSlotItem(inventorySlot, itemStack);
            AddToMapping(instanceId, inventorySlot);
        }

        // 2. 如果是装备，同时添加到 EquipContent
        if (item is EquipmentItem)
        {
            var equipSlot = GetOrCreateFreeSlot(SlotContainerType.Equip);
            if (equipSlot != null)
            {
                SetSlotItem(equipSlot, itemStack);
                AddToMapping(instanceId, equipSlot);
            }
        }

        DebugEx.Success(
            "InventoryUIManager",
            $"添加物品: {item.Name} (ID:{instanceId}) 到 {GetSlotCount(instanceId)} 个格子"
        );
    }

    /// <summary>
    /// 获取或创建空闲格子（O(1) 复杂度）
    /// </summary>
    private InventorySlotUI GetOrCreateFreeSlot(SlotContainerType containerType)
    {
        // 从空闲队列中获取
        if (m_FreeSlotQueues.TryGetValue(containerType, out var queue) && queue.Count > 0)
        {
            return queue.Dequeue();
        }

        // 如果没有空闲格子，从所有格子中查找
        if (m_AllSlots.TryGetValue(containerType, out var slots))
        {
            foreach (var slot in slots)
            {
                var itemUI = slot.GetItemUI();
                if (itemUI == null || !itemUI.gameObject.activeSelf)
                {
                    return slot;
                }
            }
        }

        DebugEx.Warning("InventoryUIManager", $"容器 {containerType} 没有空闲格子");
        return null;
    }

    /// <summary>
    /// 设置格子物品（复用或创建 InventoryItemUI）
    /// </summary>
    private void SetSlotItem(InventorySlotUI slot, ItemStack itemStack)
    {
        var itemUI = slot.GetItemUI();

        // 如果已有 ItemUI，直接复用
        if (itemUI != null)
        {
            itemUI.SetData(itemStack);
            itemUI.gameObject.SetActive(true);
            DebugEx.Log("InventoryUIManager", $"复用 ItemUI: 格子={slot.SlotIndex}");
        }
        else
        {
            // 否则通过 SetData 触发异步加载
            slot.SetData(itemStack);
            DebugEx.Log("InventoryUIManager", $"创建 ItemUI: 格子={slot.SlotIndex}");
        }
    }

    #endregion

    #region 移除物品

    /// <summary>
    /// 从UI中移除物品（自动从所有容器中移除）
    /// </summary>
    public void RemoveItemFromUI(int instanceId)
    {
        // 1. 从映射表中查找所有格子（O(1) 复杂度）
        if (!m_ItemToSlotsMap.TryGetValue(instanceId, out var slots))
        {
            DebugEx.Warning("InventoryUIManager", $"物品 {instanceId} 不在任何格子中");
            return;
        }

        // 2. 遍历所有格子并隐藏 ItemUI
        foreach (var slot in slots)
        {
            var itemUI = slot.GetItemUI();
            if (itemUI != null)
            {
                itemUI.Clear(); // 隐藏，不销毁
                itemUI.gameObject.SetActive(false);
            }

            // 将格子加入空闲队列
            AddToFreeQueue(slot);

            DebugEx.Log(
                "InventoryUIManager",
                $"移除物品: 格子={slot.SlotIndex}, 容器={slot.ContainerType}"
            );
        }

        // 3. 清理映射
        m_ItemToSlotsMap.Remove(instanceId);

        DebugEx.Success("InventoryUIManager", $"移除物品 {instanceId} 从 {slots.Count} 个格子");
    }

    /// <summary>
    /// 添加格子到空闲队列
    /// </summary>
    private void AddToFreeQueue(InventorySlotUI slot)
    {
        if (!m_FreeSlotQueues.TryGetValue(slot.ContainerType, out var queue))
        {
            queue = new Queue<InventorySlotUI>();
            m_FreeSlotQueues[slot.ContainerType] = queue;
        }

        queue.Enqueue(slot);
    }

    #endregion

    #region 快捷栏操作

    /// <summary>
    /// 将物品添加到快捷栏（已在背包中的物品）
    /// </summary>
    public void AddItemToFastSlot(int instanceId, int fastSlotIndex)
    {
        // 1. 检查物品是否在背包中
        if (!m_ItemToSlotsMap.ContainsKey(instanceId))
        {
            DebugEx.Warning("InventoryUIManager", $"物品 {instanceId} 不在背包中");
            return;
        }

        // 2. 获取指定的快捷栏格子
        if (!m_AllSlots.TryGetValue(SlotContainerType.FastBar, out var fastSlots))
        {
            DebugEx.Error("InventoryUIManager", "快捷栏容器未注册");
            return;
        }

        if (fastSlotIndex < 0 || fastSlotIndex >= fastSlots.Count)
        {
            DebugEx.Error("InventoryUIManager", $"快捷栏索引越界: {fastSlotIndex}");
            return;
        }

        var fastSlot = fastSlots[fastSlotIndex];

        // 3. 获取物品数据（从已有格子中获取）
        var existingSlots = m_ItemToSlotsMap[instanceId];
        var itemStack = existingSlots[0].GetItemUI()?.GetItemStack();

        if (itemStack == null)
        {
            DebugEx.Error("InventoryUIManager", $"无法获取物品数据: {instanceId}");
            return;
        }

        // 4. 设置快捷栏格子
        SetSlotItem(fastSlot, itemStack);
        AddToMapping(instanceId, fastSlot);

        // 5. 从空闲队列中移除
        RemoveFromFreeQueue(SlotContainerType.FastBar, fastSlot);

        DebugEx.Success(
            "InventoryUIManager",
            $"添加物品 {instanceId} 到快捷栏格子 {fastSlotIndex}"
        );
    }

    /// <summary>
    /// 从快捷栏移除物品（不影响背包）
    /// </summary>
    public void RemoveItemFromFastSlot(int instanceId, int fastSlotIndex)
    {
        if (!m_ItemToSlotsMap.TryGetValue(instanceId, out var slots))
        {
            return;
        }

        // 找到快捷栏格子
        var fastSlot = slots.Find(s =>
            s.ContainerType == SlotContainerType.FastBar && s.SlotIndex == fastSlotIndex
        );

        if (fastSlot != null)
        {
            var itemUI = fastSlot.GetItemUI();
            if (itemUI != null)
            {
                itemUI.Clear();
                itemUI.gameObject.SetActive(false);
            }

            slots.Remove(fastSlot);
            AddToFreeQueue(fastSlot);

            DebugEx.Success("InventoryUIManager", $"从快捷栏移除物品 {instanceId}");
        }
    }

    /// <summary>
    /// 从空闲队列中移除指定格子
    /// </summary>
    private void RemoveFromFreeQueue(SlotContainerType containerType, InventorySlotUI slot)
    {
        if (m_FreeSlotQueues.TryGetValue(containerType, out var queue))
        {
            var tempList = new List<InventorySlotUI>(queue);
            tempList.Remove(slot);
            m_FreeSlotQueues[containerType] = new Queue<InventorySlotUI>(tempList);
        }
    }

    #endregion

    #region 映射表管理

    /// <summary>
    /// 添加物品到映射表
    /// </summary>
    private void AddToMapping(int instanceId, InventorySlotUI slot)
    {
        if (!m_ItemToSlotsMap.TryGetValue(instanceId, out var slots))
        {
            slots = new List<InventorySlotUI>();
            m_ItemToSlotsMap[instanceId] = slots;
        }

        if (!slots.Contains(slot))
        {
            slots.Add(slot);
        }
    }

    /// <summary>
    /// 获取物品所在的所有格子
    /// </summary>
    public List<InventorySlotUI> GetItemSlots(int instanceId)
    {
        return m_ItemToSlotsMap.TryGetValue(instanceId, out var slots) ? slots : null;
    }

    /// <summary>
    /// 获取物品所在格子数量
    /// </summary>
    private int GetSlotCount(int instanceId)
    {
        return m_ItemToSlotsMap.TryGetValue(instanceId, out var slots) ? slots.Count : 0;
    }

    #endregion
}
