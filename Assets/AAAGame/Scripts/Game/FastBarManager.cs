using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 快捷栏管理器
/// 管理玩家快捷栏中的物品存取逻辑（独立容器）
/// </summary>
public class FastBarManager
{
    #region 单例

    private static FastBarManager s_Instance;
    public static FastBarManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new FastBarManager();
            }
            return s_Instance;
        }
    }

    #endregion

    #region 字段

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized = false;

    /// <summary>快捷栏物品列表（5个槽位）</summary>
    private List<InventorySlot> m_FastBarSlots = new List<InventorySlot>();

    /// <summary>快捷栏容量（固定5格）</summary>
    private const int FASTBAR_CAPACITY = 5;

    /// <summary>格子变化事件（统一事件）</summary>
    public event Action<SlotChangeEventArgs> OnSlotChanged;

    /// <summary>[Obsolete] 物品存入事件（已废弃，保留向后兼容）</summary>
    [Obsolete("Use OnSlotChanged instead")]
    public event Action<int, InventorySlot> OnItemStored;

    /// <summary>[Obsolete] 物品取出事件（已废弃，保留向后兼容）</summary>
    [Obsolete("Use OnSlotChanged instead")]
    public event Action<int, InventorySlot> OnItemRetrieved;

    /// <summary>[Obsolete] 物品清空事件（已废弃，保留向后兼容）</summary>
    [Obsolete("Use OnSlotChanged instead")]
    public event Action<int> OnSlotCleared;

    #endregion

    #region 属性

    /// <summary>是否已初始化</summary>
    public bool IsInitialized => m_IsInitialized;

    /// <summary>快捷栏容量</summary>
    public int FastBarCapacity => FASTBAR_CAPACITY;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化快捷栏管理器
    /// </summary>
    public void Initialize()
    {
        if (m_IsInitialized)
        {
            DebugEx.Log("FastBarManager", "快捷栏管理器已初始化，跳过重复初始化");
            return;
        }

        m_FastBarSlots.Clear();
        for (int i = 0; i < FASTBAR_CAPACITY; i++)
        {
            m_FastBarSlots.Add(new InventorySlot(i));
        }

        m_IsInitialized = true;
        DebugEx.Success("FastBarManager", "快捷栏管理器初始化完成");
    }

    /// <summary>
    /// 清理快捷栏数据
    /// </summary>
    public void Cleanup()
    {
        if (!m_IsInitialized)
            return;

        m_FastBarSlots.Clear();
        m_IsInitialized = false;
        DebugEx.Log("FastBarManager", "快捷栏数据已清理");
    }

    #endregion

    #region 事件通知

    /// <summary>
    /// 通知格子变化事件
    /// </summary>
    private void NotifySlotChanged(int slotIndex, SlotChangeType changeType, int oldCount, int newCount)
    {
        var slot = slotIndex >= 0 && slotIndex < m_FastBarSlots.Count ? m_FastBarSlots[slotIndex] : null;
        var args = new SlotChangeEventArgs
        {
            ContainerType = SlotContainerType.FastBar,
            SlotIndex = slotIndex,
            ItemId = slot?.ItemId ?? -1,
            OldCount = oldCount,
            NewCount = newCount,
            ChangeType = changeType
        };

        OnSlotChanged?.Invoke(args);

        // 向后兼容：保留旧事件
#pragma warning disable CS0618
        if (changeType == SlotChangeType.Add || changeType == SlotChangeType.Update || changeType == SlotChangeType.Move)
        {
            OnItemStored?.Invoke(slotIndex, slot);
        }
        else if (changeType == SlotChangeType.Remove || changeType == SlotChangeType.Clear)
        {
            OnItemRetrieved?.Invoke(slotIndex, slot);
            if (changeType == SlotChangeType.Clear)
                OnSlotCleared?.Invoke(slotIndex);
        }
#pragma warning restore CS0618
    }

    #endregion

    #region 物品查询

    /// <summary>
    /// 获取指定索引的快捷栏格子
    /// </summary>
    public InventorySlot GetSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_FastBarSlots.Count)
            return null;
        return m_FastBarSlots[slotIndex];
    }

    /// <summary>
    /// 获取所有快捷栏格子
    /// </summary>
    public List<InventorySlot> GetAllSlots()
    {
        return new List<InventorySlot>(m_FastBarSlots);
    }

    #endregion

    #region 物品操作

    /// <summary>
    /// 存入物品到快捷栏指定格子
    /// </summary>
    public bool StoreItemToSlot(ItemBase item, int count, int targetSlotIndex)
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("FastBarManager", "快捷栏管理器未初始化");
            return false;
        }

        if (targetSlotIndex < 0 || targetSlotIndex >= m_FastBarSlots.Count)
        {
            DebugEx.Warning("FastBarManager", $"无效的快捷栏索引: {targetSlotIndex}");
            return false;
        }

        if (item == null || count <= 0)
        {
            DebugEx.Warning("FastBarManager", $"物品或数量无效");
            return false;
        }

        var targetSlot = m_FastBarSlots[targetSlotIndex];

        // 如果目标格子为空，直接放入
        if (targetSlot.IsEmpty)
        {
            targetSlot.SetItem(item, count);
            DebugEx.Log("FastBarManager", $"存入物品到快捷栏[{targetSlotIndex}]: {item.Name} x{count}");
            NotifySlotChanged(targetSlotIndex, SlotChangeType.Add, 0, count);
            return true;
        }

        // 如果是同种物品，尝试堆叠
        if (targetSlot.ItemId == item.ItemId && item.MaxStackCount > 1)
        {
            int addCount = Mathf.Min(count, item.MaxStackCount - targetSlot.Count);
            int oldCount = targetSlot.Count;
            targetSlot.AddItem(addCount);
            DebugEx.Log("FastBarManager", $"堆叠物品到快捷栏[{targetSlotIndex}]: 数量 {oldCount} -> {targetSlot.Count}");
            NotifySlotChanged(targetSlotIndex, SlotChangeType.Update, oldCount, targetSlot.Count);
            return true;
        }

        DebugEx.Warning("FastBarManager", $"快捷栏[{targetSlotIndex}] 无法存入物品");
        return false;
    }

    /// <summary>
    /// 清空指定快捷栏格子
    /// </summary>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_FastBarSlots.Count)
            return;

        var slot = m_FastBarSlots[slotIndex];
        int oldCount = slot.Count;

        slot.Clear();
        DebugEx.Log("FastBarManager", $"快捷栏[{slotIndex}] 已清空");

        NotifySlotChanged(slotIndex, SlotChangeType.Clear, oldCount, 0);
    }

    /// <summary>
    /// 交换两个快捷栏格子的物品
    /// </summary>
    public bool SwapSlots(int fromSlotIndex, int toSlotIndex)
    {
        if (!m_IsInitialized)
        {
            DebugEx.Error("FastBarManager", "快捷栏管理器未初始化");
            return false;
        }

        if (fromSlotIndex < 0 || fromSlotIndex >= m_FastBarSlots.Count ||
            toSlotIndex < 0 || toSlotIndex >= m_FastBarSlots.Count)
        {
            DebugEx.Warning("FastBarManager", $"无效的格子索引: from={fromSlotIndex}, to={toSlotIndex}");
            return false;
        }

        if (fromSlotIndex == toSlotIndex)
            return true;

        var fromSlot = m_FastBarSlots[fromSlotIndex];
        var toSlot = m_FastBarSlots[toSlotIndex];

        // 临时保存数据
        var fromData = fromSlot.ItemStack;
        var toData = toSlot.ItemStack;

        // 清空两个格子
        fromSlot.Clear();
        toSlot.Clear();

        // 交换数据
        if (toData != null && !toData.IsEmpty)
        {
            fromSlot.SetItemStack(toData);
        }

        if (fromData != null && !fromData.IsEmpty)
        {
            toSlot.SetItemStack(fromData);
        }

        DebugEx.Log("FastBarManager", $"交换快捷栏 {fromSlotIndex} <-> {toSlotIndex}");
        NotifySlotChanged(fromSlotIndex, SlotChangeType.Move, fromSlot.Count, fromSlot.Count);
        NotifySlotChanged(toSlotIndex, SlotChangeType.Move, toSlot.Count, toSlot.Count);
        return true;
    }

    #endregion
}
