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
    private WarehouseSlotContainerImpl m_WarehouseSlotContainer;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 初始化仓库容器组件
        m_WarehouseSlotContainer = GetComponent<WarehouseSlotContainerImpl>();
        if (m_WarehouseSlotContainer == null)
            m_WarehouseSlotContainer = gameObject.AddComponent<WarehouseSlotContainerImpl>();

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

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // 检查菜单外部点击关闭
        CheckContextMenuClickOutside();
    }

    /// <summary>
    /// 检查菜单外部点击，自动关闭菜单
    /// </summary>
    private void CheckContextMenuClickOutside()
    {
        // 如果菜单未显示，不需要检查
        if (m_CachedContextMenu == null || !m_CachedContextMenu.gameObject.activeSelf)
            return;

        // 检查是否有鼠标点击
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            // 检查点击是否在菜单范围内
            var menuRect = m_CachedContextMenu.GetComponent<RectTransform>();
            if (menuRect != null && !RectTransformUtility.RectangleContainsScreenPoint(menuRect, Input.mousePosition))
            {
                // 在菜单外，关闭菜单
                m_CachedContextMenu.HideContextMenu();
                DebugEx.Log("WarehouseUI", "菜单外部点击，关闭菜单");
            }
        }
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
                slot.SetSlotContainer(m_WarehouseSlotContainer);
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

    #region 物品上下文菜单

    private ItemContextMenu m_CachedContextMenu;

    /// <summary>
    /// 显示物品上下文菜单（动态加载预制体版本）
    /// </summary>
    public void ShowItemContextMenu(ItemStack itemStack, int slotIndex, RectTransform slotRect)
    {
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning("WarehouseUI", "ShowItemContextMenu: 物品为空");
            return;
        }

        // 获取或加载菜单预制体
        if (m_CachedContextMenu == null)
        {
            if (varItemContextMenu == null)
            {
                DebugEx.Error("WarehouseUI", "ShowItemContextMenu: varItemContextMenu 预制体未设置");
                return;
            }

            // 动态加载菜单预制体到 Canvas（不是 Content）
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                DebugEx.Error("WarehouseUI", "ShowItemContextMenu: 未找到 Canvas");
                return;
            }

            var menuGO = Instantiate(varItemContextMenu, canvas.transform);
            m_CachedContextMenu = menuGO.GetComponent<ItemContextMenu>();

            if (m_CachedContextMenu == null)
            {
                DebugEx.Error("WarehouseUI", "ShowItemContextMenu: 菜单预制体中没有 ItemContextMenu 组件");
                Destroy(menuGO);
                return;
            }

            DebugEx.Log("WarehouseUI", "ShowItemContextMenu: 菜单预制体已加载");
        }

        // 显示菜单
        m_CachedContextMenu.ShowContextMenu(itemStack, slotIndex, Vector2.zero, slotRect);
    }

    #endregion
}
