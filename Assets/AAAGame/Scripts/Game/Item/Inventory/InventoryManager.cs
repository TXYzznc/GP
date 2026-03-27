using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包管理器
/// </summary>
public class InventoryManager : SingletonBase<InventoryManager>
{
    #region 单例已由基类提供

    // 使用 SingletonBase<InventoryManager> 提供的 Instance 属性

    #endregion

    #region 字段

    [SerializeField]
    private int m_MaxSlotCount = 100; // 最大格子数量

    private List<InventorySlot> m_Slots; // 背包格子列表
    private bool m_IsInitialized = false; // 是否已初始化

    /// <summary>
    /// 背包内容变化事件
    /// </summary>
    public event System.Action OnInventoryChanged;

    #endregion

    #region 属性

    /// <summary>
    /// 最大格子数量
    /// </summary>
    public int MaxSlotCount => m_MaxSlotCount;

    /// <summary>
    /// 当前使用的格子数量
    /// </summary>
    public int UsedSlotCount
    {
        get
        {
            int count = 0;
            foreach (var slot in m_Slots)
            {
                if (!slot.IsEmpty)
                {
                    count++;
                }
            }
            return count;
        }
    }

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();

        // 初始化
        if (!m_IsInitialized)
        {
            DebugEx.Log("InventoryManager", "背包管理器初始化开始");
            InitializeSlots();
            m_IsInitialized = true;
            DebugEx.Success("InventoryManager", $"背包管理器初始化完成，格子数量:{m_MaxSlotCount}");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化背包格子
    /// </summary>
    private void InitializeSlots()
    {
        m_Slots = new List<InventorySlot>(m_MaxSlotCount);
        for (int i = 0; i < m_MaxSlotCount; i++)
        {
            m_Slots.Add(new InventorySlot(i));
        }
    }

    #endregion

    #region 公共方法 - 物品操作

    /// <summary>
    /// 添加物品
    /// </summary>
    public bool AddItem(int itemId, int count = 1)
    {
        DebugEx.Log("InventoryManager", $"尝试添加物品 ID:{itemId}, 数量:{count}");

        var itemData = ItemManager.Instance?.GetItemData(itemId);
        if (itemData == null)
        {
            DebugEx.Error("InventoryManager", $"物品数据不存在 ID:{itemId}");
            return false;
        }

        // 如果可堆叠，先尝试合并到现有堆叠
        if (itemData.CanStack)
        {
            int remaining = TryStackItem(itemId, count);
            if (remaining <= 0)
            {
                DebugEx.Success("InventoryManager", $"物品添加成功（堆叠）: {itemData.Name}");
                return true;
            }
            count = remaining;
        }

        // 创建新的物品实例并放入空格子
        while (count > 0)
        {
            var emptySlot = FindEmptySlot();
            if (emptySlot == null)
            {
                DebugEx.Warning("InventoryManager", "背包已满");
                return false;
            }

            var item = ItemManager.Instance.CreateItem(itemId);
            if (item == null)
            {
                DebugEx.Error("InventoryManager", $"创建物品失败 ID:{itemId}");
                return false;
            }

            int addCount = Math.Min(count, item.MaxStackCount);
            emptySlot.SetItem(item, addCount);
            count -= addCount;
        }

        DebugEx.Success("InventoryManager", $"物品添加成功: {itemData.Name}");

        // 触发背包变化事件
        OnInventoryChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 移除物品
    /// </summary>
    public bool RemoveItem(int itemId, int count = 1)
    {
        DebugEx.Log("InventoryManager", $"尝试移除物品 ID:{itemId}, 数量:{count}");

        int totalCount = GetItemCount(itemId);
        if (totalCount < count)
        {
            DebugEx.Warning(
                "InventoryManager",
                $"物品数量不足 ID:{itemId}, 需要:{count}, 拥有:{totalCount}"
            );
            return false;
        }

        int remaining = count;
        foreach (var slot in m_Slots)
        {
            if (slot.ItemId == itemId)
            {
                int removeCount = Math.Min(remaining, slot.Count);
                slot.RemoveItem(removeCount);
                remaining -= removeCount;

                if (remaining <= 0)
                {
                    break;
                }
            }
        }

        DebugEx.Success("InventoryManager", $"物品移除成功 ID:{itemId}, 数量:{count}");

        // 触发背包变化事件
        OnInventoryChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 使用物品
    /// </summary>
    public bool UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_Slots.Count)
        {
            DebugEx.Error("InventoryManager", $"格子索引越界: {slotIndex}");
            return false;
        }

        var slot = m_Slots[slotIndex];
        if (slot.IsEmpty)
        {
            DebugEx.Warning("InventoryManager", $"格子为空: {slotIndex}");
            return false;
        }

        var item = slot.ItemStack.Item;
        if (!item.CanUse)
        {
            DebugEx.Warning("InventoryManager", $"物品不可使用: {item.Name}");
            return false;
        }

        DebugEx.Log("InventoryManager", $"使用物品: {item.Name}");

        bool success = item.Use();
        if (success)
        {
            // 使用成功后减少数量
            slot.RemoveItem(1);
            DebugEx.Success("InventoryManager", $"物品使用成功: {item.Name}");
        }

        return success;
    }

    /// <summary>
    /// 获取物品数量
    /// </summary>
    public int GetItemCount(int itemId)
    {
        int count = 0;
        foreach (var slot in m_Slots)
        {
            if (slot.ItemId == itemId)
            {
                count += slot.Count;
            }
        }
        return count;
    }

    /// <summary>
    /// 检查是否拥有物品
    /// </summary>
    public bool HasItem(int itemId, int count = 1)
    {
        return GetItemCount(itemId) >= count;
    }

    /// <summary>
    /// 获取格子
    /// </summary>
    public InventorySlot GetSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_Slots.Count)
        {
            return null;
        }
        return m_Slots[slotIndex];
    }

    /// <summary>
    /// 获取所有格子
    /// </summary>
    public List<InventorySlot> GetAllSlots()
    {
        return m_Slots;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 尝试堆叠物品到现有格子
    /// </summary>
    private int TryStackItem(int itemId, int count)
    {
        foreach (var slot in m_Slots)
        {
            if (slot.ItemId == itemId && !slot.ItemStack.IsFull)
            {
                int added = slot.AddItem(count);
                count -= added;

                if (count <= 0)
                {
                    return 0;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 查找空格子
    /// </summary>
    private InventorySlot FindEmptySlot()
    {
        foreach (var slot in m_Slots)
        {
            if (slot.IsEmpty)
            {
                return slot;
            }
        }
        return null;
    }

    #endregion

    #region 存档与读档

    /// <summary>
    /// 保存背包数据到存档
    /// </summary>
    public List<InventoryItemSaveData> SaveInventory()
    {
        DebugEx.Log("InventoryManager", "开始保存背包数据");

        var saveDataList = new List<InventoryItemSaveData>();

        foreach (var slot in m_Slots)
        {
            if (slot.IsEmpty)
            {
                continue;
            }

            var itemStack = slot.ItemStack;
            var saveData = new InventoryItemSaveData
            {
                ItemId = itemStack.ItemId,
                Count = itemStack.Count,
                UniqueId = itemStack.Item.UniqueId,
                ObtainTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ExtraData = "", // 暂时不保存额外数据，后续扩展
            };

            saveDataList.Add(saveData);
        }

        DebugEx.Success("InventoryManager", $"背包数据保存完成，共 {saveDataList.Count} 个物品");
        return saveDataList;
    }

    /// <summary>
    /// 从存档加载背包数据
    /// </summary>
    public void LoadInventory(List<InventoryItemSaveData> saveDataList)
    {
        if (saveDataList == null || saveDataList.Count == 0)
        {
            DebugEx.Log("InventoryManager", "存档中没有背包数据，跳过加载");
            return;
        }

        DebugEx.Log("InventoryManager", $"开始加载背包数据，共 {saveDataList.Count} 个物品");

        // 清空当前背包
        ClearInventory();

        // 加载每个物品
        int successCount = 0;
        foreach (var saveData in saveDataList)
        {
            // 创建物品实例
            var item = ItemManager.Instance?.CreateItem(saveData.ItemId);
            if (item == null)
            {
                DebugEx.Warning("InventoryManager", $"创建物品失败，跳过 ItemId:{saveData.ItemId}");
                continue;
            }

            // 查找空格子
            var emptySlot = FindEmptySlot();
            if (emptySlot == null)
            {
                DebugEx.Warning("InventoryManager", "背包已满，无法加载更多物品");
                break;
            }

            // 设置物品到格子
            emptySlot.SetItem(item, saveData.Count);
            successCount++;
        }

        DebugEx.Success(
            "InventoryManager",
            $"背包数据加载完成，成功加载 {successCount}/{saveDataList.Count} 个物品"
        );
    }

    /// <summary>
    /// 清空背包（用于加载存档前清理）
    /// </summary>
    private void ClearInventory()
    {
        foreach (var slot in m_Slots)
        {
            slot.Clear();
        }
        DebugEx.Log("InventoryManager", "背包已清空");
    }

    #endregion
}
