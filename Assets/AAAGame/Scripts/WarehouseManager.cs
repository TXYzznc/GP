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
        if (itemData.MaxStack > 1)
        {
            var existingItem = GetItemById(itemId);
            if (existingItem != null && existingItem.Count < itemData.MaxStack)
            {
                // 堆叠到现有物品
                int addCount = Mathf.Min(count, itemData.MaxStack - existingItem.Count);
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

        var allItems = inventoryManager.GetAllItems();
        int successCount = 0;
        int failCount = 0;

        foreach (var item in allItems)
        {
            if (StoreItem(item.ItemId, item.Count, item.Durability))
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        // 从背包中移除已存入的物品
        foreach (var item in allItems)
        {
            inventoryManager.RemoveItem(item.ItemId, item.Count);
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
        if (!inventoryManager.AddItem(itemId, count, item.Durability))
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
