using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class WarehouseUI : UIFormBase
{
    private readonly List<InventorySlotUI> m_Slots = new();
    private WarehouseManager m_WarehouseManager;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        BindButtonEvents();
        DebugEx.Success("WarehouseUI", "仓库UI初始化完成");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        m_WarehouseManager = WarehouseManager.Instance;
        if (m_WarehouseManager != null)
        {
            m_WarehouseManager.OnItemStored += OnWarehouseChanged;
            m_WarehouseManager.OnItemRetrieved += OnWarehouseChanged;
            m_WarehouseManager.OnCapacityChanged += OnCapacityChanged;
        }

        BuildSlots();
        RefreshWarehouse();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (m_WarehouseManager != null)
        {
            m_WarehouseManager.OnItemStored -= OnWarehouseChanged;
            m_WarehouseManager.OnItemRetrieved -= OnWarehouseChanged;
            m_WarehouseManager.OnCapacityChanged -= OnCapacityChanged;
        }

        base.OnClose(isShutdown, userData);
    }

    #region 初始化

    /// <summary>
    /// 根据仓库容量动态生成格子（每次打开时重建，支持容量扩展）
    /// </summary>
    private void BuildSlots()
    {
        if (varContent == null)
        {
            DebugEx.Error("WarehouseUI", "varContent 未设置");
            return;
        }

        if (varInventorySlotUI == null)
        {
            DebugEx.Error("WarehouseUI", "varInventorySlotUI 预制体未设置，无法生成格子");
            return;
        }

        int capacity = m_WarehouseManager?.WarehouseCapacity ?? 50;

        DebugEx.Log("WarehouseUI", $"开始生成仓库格子: 当前{m_Slots.Count}, 目标{capacity}");

        // 复用已有格子，不足时追加，多余时隐藏
        for (int i = m_Slots.Count; i < capacity; i++)
        {
            var go = Object.Instantiate(varInventorySlotUI, varContent.transform);
            go.name = $"Slot_{i}"; // 方便调试

            var slot = go.GetComponent<InventorySlotUI>();
            if (slot != null)
            {
                slot.SetSlotIndex(i);
                slot.SetContainerType(SlotContainerType.Warehouse);
                // 初始化格子背景颜色为默认
                slot.SetRarity(0);
                m_Slots.Add(slot);
            }
            else
            {
                DebugEx.Error("WarehouseUI", $"格子预制体 {i} 找不到 InventorySlotUI 组件");
                Object.Destroy(go);
            }
        }

        // 显示/隐藏格子
        for (int i = 0; i < m_Slots.Count; i++)
        {
            m_Slots[i].gameObject.SetActive(i < capacity);
        }

        DebugEx.Success("WarehouseUI", $"仓库格子生成完成: 共{m_Slots.Count}个");
    }

    private void BindButtonEvents()
    {
        if (varCloseBtn != null)
            varCloseBtn.onClick.AddListener(OnClickClose);

        if (varStoreAllBtn != null)
            varStoreAllBtn.onClick.AddListener(OnStoreAllClick);
    }

    #endregion

    #region 刷新

    private void RefreshWarehouse()
    {
        if (m_WarehouseManager == null)
            return;

        var items = m_WarehouseManager.GetAllItems();

        for (int i = 0; i < m_Slots.Count; i++)
        {
            if (!m_Slots[i].gameObject.activeSelf)
                continue;

            var warehouseItem = items.Find(x => x.SlotIndex == i);

            // 获取物品或创建空堆叠
            ItemStack itemStack = null;
            if (warehouseItem != null)
            {
                var item = ItemManager.Instance?.CreateItem(warehouseItem.ItemId);
                if (item != null)
                {
                    itemStack = new ItemStack(item, warehouseItem.Count);
                }
            }

            // 统一调用 SetData，格子自动处理UI刷新和背景颜色
            m_Slots[i].SetData(itemStack);
        }
    }

    #endregion

    #region 事件回调

    private void OnWarehouseChanged(InventoryItem _) => RefreshWarehouse();

    private void OnCapacityChanged(int newCapacity)
    {
        BuildSlots();
        RefreshWarehouse();
    }

    #endregion

    #region 按钮事件

    private void OnStoreAllClick()
    {
        bool result = m_WarehouseManager?.StoreAll() ?? false;
        if (!result)
            DebugEx.Warning("WarehouseUI", "仓库空间不足，部分物品未能存入");
        RefreshWarehouse();
    }

    #endregion
}
