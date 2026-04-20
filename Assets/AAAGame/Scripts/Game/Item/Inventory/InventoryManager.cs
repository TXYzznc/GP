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

    // 虚拟物品缓存
    private int m_CachedGold = 0;      // 金币缓存
    private int m_CachedSpiritStone = 0; // 灵石缓存

    /// <summary>
    /// 背包内容变化事件
    /// </summary>
    public event System.Action OnInventoryChanged;

    /// <summary>
    /// 特定物品数量变化事件（物品ID, 新数量）
    /// </summary>
    public event System.Action<int, int> OnItemQuantityChanged;

    /// <summary>
    /// 金币数量变化事件
    /// </summary>
    public event System.Action<int> OnGoldChanged;

    /// <summary>
    /// 灵石数量变化事件
    /// </summary>
    public event System.Action<int> OnSpiritStoneChanged;

    #endregion

    #region 属性

    /// <summary>
    /// 最大格子数量
    /// </summary>
    public int MaxSlotCount => m_MaxSlotCount;

    /// <summary>是否已初始化</summary>
    public bool IsInitialized => m_IsInitialized;

    /// <summary>
    /// 背包中金币的数量（缓存值）
    /// </summary>
    public int Gold => m_CachedGold;

    /// <summary>
    /// 背包中灵石的数量（缓存值）
    /// </summary>
    public int SpiritStone => m_CachedSpiritStone;

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
                UpdateVirtualItemCache(itemId, count);
                int stackedCount = GetItemCount(itemId);
                OnItemQuantityChanged?.Invoke(itemId, stackedCount);
                DebugEx.Success("InventoryManager", $"物品添加成功（堆叠）: {itemData.Name}");
                OnInventoryChanged?.Invoke();
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

        UpdateVirtualItemCache(itemId, count);
        DebugEx.Success("InventoryManager", $"物品添加成功: {itemData.Name}");

        int newCount = GetItemCount(itemId);
        OnItemQuantityChanged?.Invoke(itemId, newCount);

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

        UpdateVirtualItemCache(itemId, -count);
        DebugEx.Success("InventoryManager", $"物品移除成功 ID:{itemId}, 数量:{count}");

        int newCount = GetItemCount(itemId);
        OnItemQuantityChanged?.Invoke(itemId, newCount);

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
    /// 交换两个格子的物品
    /// </summary>
    public bool MoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= m_Slots.Count || toSlot < 0 || toSlot >= m_Slots.Count)
        {
            DebugEx.Error("InventoryManager", $"MoveItem 格子索引越界: {fromSlot} -> {toSlot}");
            return false;
        }

        var from = m_Slots[fromSlot];
        var to = m_Slots[toSlot];

        // 交换格子内容
        var tempStack = from.ItemStack;
        int tempCount = from.Count;

        if (to.IsEmpty)
        {
            to.SetItem(from.ItemStack?.Item, from.Count);
            from.Clear();
        }
        else
        {
            // 同种物品且可堆叠，尝试合并
            if (from.ItemId == to.ItemId && from.ItemStack?.Item?.MaxStackCount > 1)
            {
                int canAdd = to.ItemStack.Item.MaxStackCount - to.Count;
                int add = Math.Min(canAdd, from.Count);
                to.AddItem(add);
                from.RemoveItem(add);
            }
            else
            {
                // 互换
                var fromItem = from.ItemStack?.Item;
                int fromCount = from.Count;
                var toItem = to.ItemStack?.Item;
                int toCount = to.Count;

                from.Clear();
                to.Clear();
                from.SetItem(toItem, toCount);
                to.SetItem(fromItem, fromCount);
            }
        }

        DebugEx.Log("InventoryManager", $"物品移动: {fromSlot} -> {toSlot}");
        OnInventoryChanged?.Invoke();
        return true;
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

    /// <summary>
    /// 一键整理：先堆叠同种物品，再按 ItemType 分组、Rarity 降序排列
    /// </summary>
    public void SortInventory()
    {
        // 1. 收集所有非空堆叠
        var stacks = new List<ItemStack>();
        foreach (var slot in m_Slots)
        {
            if (!slot.IsEmpty)
                stacks.Add(slot.ItemStack);
        }

        // 2. 堆叠合并：同 ItemId 且可堆叠的合并到同一堆
        var merged = new List<ItemStack>();
        foreach (var stack in stacks)
        {
            var existing = merged.Find(s => s.ItemId == stack.ItemId && !s.IsFull);
            if (existing != null)
            {
                int canAdd = existing.Item.MaxStackCount - existing.Count;
                int add = Math.Min(canAdd, stack.Count);
                existing.Add(add);
                stack.Remove(add);
                if (stack.Count > 0)
                    merged.Add(stack);
            }
            else
            {
                merged.Add(stack);
            }
        }

        // 3. 按 ItemType 升序，Quality 降序，ItemId 升序 排序
        merged.Sort(
            (a, b) =>
            {
                int typeCompare = a.Item.ItemData.Type.CompareTo(b.Item.ItemData.Type);
                if (typeCompare != 0)
                    return typeCompare;
                int qualityCompare = b.Item.ItemData.Quality.CompareTo(a.Item.ItemData.Quality);
                if (qualityCompare != 0)
                    return qualityCompare;
                return a.ItemId.CompareTo(b.ItemId);
            }
        );

        // 4. 重新填回格子
        for (int i = 0; i < m_Slots.Count; i++)
        {
            if (i < merged.Count)
                m_Slots[i].SetItemStack(merged[i]);
            else
                m_Slots[i].Clear();
        }

        DebugEx.Success("InventoryManager", "背包整理完成");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 计算当前总负重（Weight 字段待 DataTable 配置化后接入，当前每件物品计 1 重量）
    /// </summary>
    public float CalculateCurrentWeight()
    {
        float total = 0f;
        foreach (var slot in m_Slots)
        {
            if (!slot.IsEmpty)
                total += slot.Count; // TODO: 改为 slot.ItemStack.Item.ItemData.Weight * slot.Count
        }
        return total;
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
        // 先清空当前背包（无论saveDataList是否为空）
        ClearInventory();

        if (saveDataList == null || saveDataList.Count == 0)
        {
            DebugEx.Log("InventoryManager", "存档中没有背包数据，背包已清空");
            OnInventoryChanged?.Invoke();
            return;
        }

        DebugEx.Log("InventoryManager", $"开始加载背包数据，共 {saveDataList.Count} 个物品");

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

        // 触发背包变化事件，刷新UI
        OnInventoryChanged?.Invoke();
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
        m_CachedGold = 0;
        m_CachedSpiritStone = 0;
        DebugEx.Log("InventoryManager", "背包已清空");
    }

    /// <summary>
    /// 更新虚拟物品缓存
    /// </summary>
    private void UpdateVirtualItemCache(int itemId, int changeCount)
    {
        switch (itemId)
        {
            case 999: // 金币
                int oldGold = m_CachedGold;
                m_CachedGold = GetItemCount(999);
                if (m_CachedGold != oldGold)
                {
                    OnGoldChanged?.Invoke(m_CachedGold);
                    DebugEx.Log("InventoryManager", $"金币更新: {oldGold} → {m_CachedGold}");
                }
                break;

            case 99999: // 灵石
                int oldStone = m_CachedSpiritStone;
                m_CachedSpiritStone = GetItemCount(99999);
                if (m_CachedSpiritStone != oldStone)
                {
                    OnSpiritStoneChanged?.Invoke(m_CachedSpiritStone);
                    DebugEx.Log("InventoryManager", $"灵石更新: {oldStone} → {m_CachedSpiritStone}");
                }
                break;
        }
    }

    /// <summary>
    /// 刷新所有虚拟物品缓存
    /// </summary>
    public void RefreshVirtualItemCache()
    {
        UpdateVirtualItemCache(999, 0);
        UpdateVirtualItemCache(99999, 0);
    }

    #endregion
}
