using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 仓库管理器
/// 管理玩家仓库中的物品存取逻辑
/// </summary>
public class WarehouseManager
{
    #region 单例

    private static WarehouseManager s_Instance;
    public static WarehouseManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new WarehouseManager();
            }
            return s_Instance;
        }
    }

    #endregion

    #region 字段

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized = false;

    /// <summary>仓库物品列表</summary>
    private List<InventoryItem> m_WarehouseItems = new List<InventoryItem>();

    /// <summary>仓库容量（格子数）</summary>
    private int m_WarehouseCapacity = 50;

    /// <summary>物品存入事件</summary>
    public event Action<InventoryItem> OnItemStored;

    /// <summary>物品取出事件</summary>
    public event Action<InventoryItem> OnItemRetrieved;

    /// <summary>仓库容量变化事件</summary>
    public event Action<int> OnCapacityChanged;

    #endregion

    #region 属性

    /// <summary>是否已初始化</summary>
    public bool IsInitialized => m_IsInitialized;

    /// <summary>仓库容量</summary>
    public int WarehouseCapacity => m_WarehouseCapacity;

    /// <summary>已使用的格子数</summary>
    public int UsedSlots => m_WarehouseItems.Count;

    /// <summary>剩余格子数</summary>
    public int AvailableSlots => m_WarehouseCapacity - UsedSlots;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化仓库管理器
    /// </summary>
    public void Initialize(int warehouseCapacity = 50)
    {
        if (m_IsInitialized)
        {
            DebugEx.Log("WarehouseManager", "仓库管理器已初始化，跳过重复初始化");
            return;
        }

        m_WarehouseCapacity = warehouseCapacity;
        m_WarehouseItems.Clear();

        m_IsInitialized = true;

        DebugEx.Success("WarehouseManager", $"仓库管理器初始化完成 - 容量:{m_WarehouseCapacity}");
    }

    /// <summary>
    /// 清理仓库数据
    /// </summary>
    public void Cleanup()
    {
        if (!m_IsInitialized)
        {
            return;
        }

        m_WarehouseItems.Clear();
        m_IsInitialized = false;

        DebugEx.Log("WarehouseManager", "仓库数据已清理");
    }

    #endregion

    #region 物品查询

    /// <summary>
    /// 获取所有仓库物品
    /// </summary>
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(m_WarehouseItems);
    }

    /// <summary>
    /// 根据物品ID获取物品
    /// </summary>
    public InventoryItem GetItemById(int itemId)
    {
        return m_WarehouseItems.FirstOrDefault(item => item.ItemId == itemId);
    }

    /// <summary>
    /// 根据格子索引获取物品
    /// </summary>
    public InventoryItem GetItemBySlot(int slotIndex)
    {
        return m_WarehouseItems.FirstOrDefault(item => item.SlotIndex == slotIndex);
    }

    /// <summary>
    /// 获取指定物品ID的总数量
    /// </summary>
    public int GetItemCount(int itemId)
    {
        var item = GetItemById(itemId);
        return item != null ? item.Count : 0;
    }

    #endregion

    #region 物品存入

    /// <summary>
    /// 存入单个物品到仓库
    /// </summary>
    public bool StoreItem(int itemId, int count = 1, int durability = 0)
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "仓库管理器未初始化");
            return false;
        }

        if (count <= 0)
        {
            DebugEx.Warning("WarehouseManager", $"存入物品数量无效: {count}");
            return false;
        }

        // 获取物品配置
        var itemData = ItemManager.Instance?.GetItemData(itemId);
        if (itemData == null)
        {
            DebugEx.Error("WarehouseManager", $"物品ID不存在: {itemId}");
            return false;
        }

        // 检查是否可堆叠
        if (itemData.MaxStackCount > 1)
        {
            var existingItem = GetItemById(itemId);
            if (existingItem != null && existingItem.Count < itemData.MaxStackCount)
            {
                // 堆叠到现有物品
                int addCount = Mathf.Min(count, itemData.MaxStackCount - existingItem.Count);
                int oldCount = existingItem.Count;
                existingItem.Count += addCount;

                DebugEx.Log(
                    "WarehouseManager",
                    $"物品堆叠: ID={itemId}, 数量 {oldCount} -> {existingItem.Count}"
                );

                OnItemStored?.Invoke(existingItem);

                // 如果还有剩余物品，继续存入
                int remainCount = count - addCount;
                if (remainCount > 0)
                {
                    return StoreItem(itemId, remainCount, durability);
                }

                return true;
            }
        }

        // 检查仓库是否有空间
        if (UsedSlots >= m_WarehouseCapacity)
        {
            DebugEx.Warning("WarehouseManager", $"仓库已满，无法存入物品 ID={itemId}");
            return false;
        }

        // 创建新物品
        int newSlotIndex = FindAvailableSlot();
        var newItem = new InventoryItem(itemId, count, durability, newSlotIndex);
        m_WarehouseItems.Add(newItem);

        DebugEx.Log(
            "WarehouseManager",
            $"物品存入: ID={itemId}, 数量={count}, 格子={newSlotIndex}"
        );

        OnItemStored?.Invoke(newItem);

        return true;
    }

    /// <summary>
    /// 一键存入背包中的所有物品到仓库
    /// </summary>
    public bool StoreAll()
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "仓库管理器未初始化");
            return false;
        }

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null || !inventoryManager.IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "背包管理器未初始化");
            return false;
        }

        var allSlots = inventoryManager.GetAllSlots();
        int successCount = 0;
        int failCount = 0;

        // 先收集需要存入的物品（避免遍历时修改集合）
        var toStore = new System.Collections.Generic.List<(int itemId, int count)>();
        foreach (var slot in allSlots)
        {
            if (!slot.IsEmpty)
                toStore.Add((slot.ItemId, slot.Count));
        }

        foreach (var (itemId, count) in toStore)
        {
            if (StoreItem(itemId, count, 0))
                successCount++;
            else
                failCount++;
        }

        // 从背包中移除已处理的物品
        foreach (var (itemId, count) in toStore)
        {
            inventoryManager.RemoveItem(itemId, count);
        }

        DebugEx.Log("WarehouseManager", $"一键存入完成 - 成功:{successCount}, 失败:{failCount}");

        return failCount == 0;
    }

    #endregion

    #region 物品取出

    /// <summary>
    /// 从仓库取出物品到背包
    /// </summary>
    public bool RetrieveItem(int itemId, int count = 1)
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "仓库管理器未初始化");
            return false;
        }

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null || !inventoryManager.IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "背包管理器未初始化");
            return false;
        }

        var item = GetItemById(itemId);
        if (item == null)
        {
            DebugEx.Warning("WarehouseManager", $"仓库中不存在物品 ID={itemId}");
            return false;
        }

        if (count > item.Count)
        {
            DebugEx.Warning(
                "WarehouseManager",
                $"取出数量超过仓库存量: 请求={count}, 实际={item.Count}"
            );
            count = item.Count;
        }

        // 尝试添加到背包
        if (!inventoryManager.AddItem(itemId, count))
        {
            DebugEx.Warning("WarehouseManager", $"背包已满，无法取出物品 ID={itemId}");
            return false;
        }

        // 从仓库移除
        item.Count -= count;
        if (item.Count <= 0)
        {
            m_WarehouseItems.Remove(item);
        }

        DebugEx.Log("WarehouseManager", $"物品取出: ID={itemId}, 数量={count}");

        OnItemRetrieved?.Invoke(item);

        return true;
    }

    #endregion

    #region 容量管理

    /// <summary>
    /// 扩展仓库容量
    /// </summary>
    public void ExpandCapacity(int additionalSlots)
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "仓库管理器未初始化");
            return;
        }

        int oldCapacity = m_WarehouseCapacity;
        m_WarehouseCapacity += additionalSlots;

        DebugEx.Log(
            "WarehouseManager",
            $"仓库容量扩展: {oldCapacity} -> {m_WarehouseCapacity} (+{additionalSlots})"
        );

        OnCapacityChanged?.Invoke(m_WarehouseCapacity);
    }

    #endregion

    #region 拖拽操作

    /// <summary>
    /// 存入物品到指定格子（拖拽操作）
    /// </summary>
    public bool StoreItemToSlot(int itemId, int count, int targetSlotIndex, int durability = 0)
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "仓库管理器未初始化");
            return false;
        }

        if (targetSlotIndex < 0 || targetSlotIndex >= m_WarehouseCapacity)
        {
            DebugEx.Warning("WarehouseManager", $"[StoreItemToSlot] 无效的格子索引: {targetSlotIndex}");
            return false;
        }

        var targetItem = GetItemBySlot(targetSlotIndex);

        // 目标格子为空，直接存入
        if (targetItem == null)
        {
            var newItem = new InventoryItem(itemId, count, durability, targetSlotIndex);
            m_WarehouseItems.Add(newItem);
            DebugEx.Log("WarehouseManager", $"[StoreItemToSlot] 存入物品到格子 {targetSlotIndex}: ID={itemId}, 数量={count}");
            OnItemStored?.Invoke(newItem);
            return true;
        }

        // 目标格子已有物品，尝试堆叠
        var itemData = ItemManager.Instance?.GetItemData(itemId);
        if (itemData == null || itemData.MaxStackCount <= 1)
        {
            DebugEx.Warning("WarehouseManager", $"[StoreItemToSlot] 目标格子已有物品，且物品不可堆叠");
            return false;
        }

        if (targetItem.ItemId == itemId && targetItem.Count < itemData.MaxStackCount)
        {
            int addCount = Mathf.Min(count, itemData.MaxStackCount - targetItem.Count);
            targetItem.Count += addCount;
            DebugEx.Log("WarehouseManager", $"[StoreItemToSlot] 堆叠物品到格子 {targetSlotIndex}: 数量 {targetItem.Count - addCount} -> {targetItem.Count}");
            OnItemStored?.Invoke(targetItem);

            // 如果还有剩余物品，存入下一个空格子
            int remainCount = count - addCount;
            if (remainCount > 0)
            {
                return StoreItem(itemId, remainCount, durability);
            }
            return true;
        }

        DebugEx.Warning("WarehouseManager", $"[StoreItemToSlot] 无法存入到格子 {targetSlotIndex}");
        return false;
    }

    /// <summary>
    /// 交换或移动两个格子的物品（拖拽操作）
    /// </summary>
    public bool SwapSlots(int fromSlotIndex, int toSlotIndex)
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "仓库管理器未初始化");
            return false;
        }

        if (fromSlotIndex < 0 || fromSlotIndex >= m_WarehouseCapacity ||
            toSlotIndex < 0 || toSlotIndex >= m_WarehouseCapacity)
        {
            DebugEx.Warning("WarehouseManager", $"[SwapSlots] 无效的格子索引: from={fromSlotIndex}, to={toSlotIndex}");
            return false;
        }

        if (fromSlotIndex == toSlotIndex)
        {
            return true; // 相同格子，无需操作
        }

        var fromItem = GetItemBySlot(fromSlotIndex);
        var toItem = GetItemBySlot(toSlotIndex);

        if (fromItem == null)
        {
            DebugEx.Warning("WarehouseManager", $"[SwapSlots] 源格子 {fromSlotIndex} 为空");
            return false;
        }

        // 交换 SlotIndex
        fromItem.SlotIndex = toSlotIndex;
        if (toItem != null)
        {
            toItem.SlotIndex = fromSlotIndex;
            OnItemStored?.Invoke(toItem); // 通知 UI 更新
        }
        else
        {
            // 目标格子为空，fromItem 移到目标格子
            OnItemStored?.Invoke(fromItem); // 通知 UI 更新
        }

        DebugEx.Log("WarehouseManager", $"[SwapSlots] 交换格子 {fromSlotIndex} <-> {toSlotIndex}");
        return true;
    }

    /// <summary>
    /// 从指定格子移除物品（拖拽操作使用）
    /// </summary>
    public bool RemoveItem(int slotIndex, int count)
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("WarehouseManager", "仓库管理器未初始化");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= m_WarehouseCapacity)
        {
            DebugEx.Warning("WarehouseManager", $"[RemoveItem] 无效的格子索引: {slotIndex}");
            return false;
        }

        var item = GetItemBySlot(slotIndex);
        if (item == null)
        {
            DebugEx.Warning("WarehouseManager", $"[RemoveItem] 格子 {slotIndex} 为空");
            return false;
        }

        if (count <= 0 || count > item.Count)
        {
            count = item.Count;
        }

        item.Count -= count;
        if (item.Count <= 0)
        {
            m_WarehouseItems.Remove(item);
            DebugEx.Log("WarehouseManager", $"[RemoveItem] 物品已完全移除: 格子={slotIndex}");
        }
        else
        {
            DebugEx.Log("WarehouseManager", $"[RemoveItem] 物品部分移除: 格子={slotIndex}, 剩余数量={item.Count}");
        }

        OnItemRetrieved?.Invoke(item);
        return true;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 查找可用的格子索引
    /// </summary>
    private int FindAvailableSlot()
    {
        for (int i = 0; i < m_WarehouseCapacity; i++)
        {
            if (GetItemBySlot(i) == null)
            {
                return i;
            }
        }
        return -1; // 无可用格子
    }

    #endregion
}
