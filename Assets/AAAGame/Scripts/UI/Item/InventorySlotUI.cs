using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 格子所属的容器类型
/// </summary>
public enum SlotContainerType
{
    Inventory,
    Warehouse,
    Equip,
    FastBar,
}

/// <summary>
/// 背包/仓库格子UI
/// 只负责格子容器（背景槽位），InventoryItemUI 已作为子对象预置在 varInventoryItemUI 下
/// 关键：这个格子知道自己属于哪个容器（业务逻辑上），通过 m_Container 引用
/// </summary>
public partial class InventorySlotUI : UIItemBase, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>缓存的 InventoryItemUI 组件</summary>
    private InventoryItemUI m_ItemUI;

    /// <summary>格子索引</summary>
    public int SlotIndex { get; private set; }

    /// <summary>所属容器类型（由 InventoryUI / WarehouseUI 初始化时设置）</summary>
    public SlotContainerType ContainerType { get; private set; }

    /// <summary>业务逻辑上的容器引用（真正操作数据的容器）</summary>
    public ISlotContainer SlotContainer { get; private set; }

    /// <summary>该格子是否已解锁可用（背包锁定格子为false，库存栏/快捷栏无此状态）</summary>
    public bool IsAvailable { get; private set; } = true;

    protected override void OnInit()
    {
        base.OnInit();

        // 动态查找子对象中的 InventoryItemUI（支持动态生成的格子）
        m_ItemUI = GetComponentInChildren<InventoryItemUI>();

        if (m_ItemUI == null)
        {
            DebugEx.Warning("InventorySlotUI", $"格子 {gameObject.name} 找不到 InventoryItemUI 子组件！" +
                $"请检查预制体层级：InventorySlotUI > InventoryItemUI");
        }
    }

    public void SetSlotIndex(int index)
    {
        SlotIndex = index;
    }

    public void SetContainerType(SlotContainerType type)
    {
        ContainerType = type;
    }

    /// <summary>
    /// 设置业务逻辑上的容器引用
    /// 这个方法在创建格子时调用，绑定格子到实际的容器
    /// </summary>
    public void SetSlotContainer(ISlotContainer container)
    {
        SlotContainer = container;
        if (container != null && container.ContainerType != ContainerType)
        {
            DebugEx.Warning("InventorySlotUI",
                $"格子容器类型不匹配: ContainerType={ContainerType}, Container.Type={container.ContainerType}");
        }
    }

    /// <summary>
    /// 设置格子可用性（用于背包锁定格）
    /// </summary>
    public void SetAvailable(bool available)
    {
        IsAvailable = available;

        // 更新锁定状态显示
        if (varLock != null)
            varLock.SetActive(!available);
    }

    /// <summary>
    /// 设置格子背景颜色
    /// 锁定格显示黑色，可用格按稀有度着色或默认颜色
    /// </summary>
    public void SetRarity(int rarity)
    {
        if (varBg == null) return;

        if (!IsAvailable)
        {
            varBg.color = new Color(0.1f, 0.1f, 0.1f, 1f); // 锁定格：深灰/黑色
        }
        else
        {
            varBg.color = rarity > 0 ? RarityColorHelper.GetColor(rarity) : RarityColorHelper.DefaultBg;
        }
    }

    public InventoryItemUI GetItemUI() => m_ItemUI;

    /// <summary>
    /// 设置格子数据（一站式处理数据和UI刷新）
    /// </summary>
    public void SetData(ItemStack itemStack)
    {
        var itemUI = GetItemUI();
        if (itemUI == null)
            return;

        // 设置物品数据
        itemUI.SetData(itemStack);

        // 自动设置背景颜色（根据物品品质）
        int quality = 0;
        if (itemStack != null && !itemStack.IsEmpty && itemStack.Item != null)
        {
            quality = (int)itemStack.Item.Quality;
        }
        SetRarity(quality);
    }

    #region 鼠标交互

    /// <summary>
    /// 鼠标进入格子时显示高亮
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (varHighLightImg != null && varHighLightImg.gameObject.activeSelf == false)
        {
            varHighLightImg.gameObject.SetActive(true);
            DebugEx.Log("InventorySlotUI", $"格子 {SlotIndex} 高亮显示");
        }
    }

    /// <summary>
    /// 鼠标离开格子时隐藏高亮
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (varHighLightImg != null && varHighLightImg.gameObject.activeSelf == true)
        {
            varHighLightImg.gameObject.SetActive(false);
            DebugEx.Log("InventorySlotUI", $"格子 {SlotIndex} 高亮隐藏");
        }
    }

    #endregion

    #region 点击事件处理

    /// <summary>
    /// 处理左键点击（显示物品详情）
    /// 由 InventoryClickHandler 分发调用
    /// </summary>
    public void OnLeftClick()
    {
        var itemUI = GetItemUI();
        if (itemUI == null || !itemUI.HasItem())
        {
            DebugEx.Warning("InventorySlotUI", $"[OnLeftClick] 格子 {SlotIndex} 无物品");
            return;
        }

        var itemStack = itemUI.GetItemStack();
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning("InventorySlotUI", $"[OnLeftClick] 物品堆叠为空");
            return;
        }

        DebugEx.Log("InventorySlotUI", $"[OnLeftClick] 左键点击 格子={SlotIndex} 物品={itemStack.Item.Name}");

        ShowItemDetailPanel(itemStack);
    }

    /// <summary>
    /// 处理右键点击（显示上下文菜单）
    /// 由 InventoryClickHandler 分发调用
    /// </summary>
    public void OnRightClick(Vector2 mousePosition)
    {
        var itemUI = GetItemUI();
        if (itemUI == null || !itemUI.HasItem())
        {
            DebugEx.Warning("InventorySlotUI", $"[OnRightClick] 格子 {SlotIndex} 无物品");
            return;
        }

        var itemStack = itemUI.GetItemStack();
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning("InventorySlotUI", $"[OnRightClick] 物品堆叠为空");
            return;
        }

        // 任务道具不显示右键菜单
        if (itemStack.Item.Type == ItemType.Quest)
        {
            DebugEx.Log("InventorySlotUI", "[OnRightClick] 任务道具，跳过右键菜单");
            return;
        }

        DebugEx.Log("InventorySlotUI", $"[OnRightClick] 右键点击 格子={SlotIndex} 物品={itemStack.Item.Name} 位置={mousePosition}");

        ShowContextMenu(itemStack, SlotIndex, mousePosition, GetComponent<RectTransform>());
    }

    /// <summary>
    /// 显示物品详情面板
    /// </summary>
    private void ShowItemDetailPanel(ItemStack itemStack)
    {
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning("InventorySlotUI", "[ShowItemDetailPanel] 物品堆叠为空");
            return;
        }

        // 获取 InventoryUI（背包是包含此格子的 UI）
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.ShowItemDetail(itemStack);
            DebugEx.Success("InventorySlotUI", $"[ShowItemDetailPanel] 显示物品详情: {itemStack.Item.Name}");
        }
        else
        {
            DebugEx.Warning("InventorySlotUI", "[ShowItemDetailPanel] 无法获取 InventoryUI");
        }
    }

    /// <summary>
    /// 显示上下文菜单
    /// </summary>
    private void ShowContextMenu(ItemStack itemStack, int slotIndex, Vector2 position, RectTransform slotRect)
    {
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning("InventorySlotUI", "[ShowContextMenu] 物品堆叠为空");
            return;
        }

        // 尝试获取 InventoryUI
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.ShowItemContextMenu(itemStack, slotIndex, slotRect);
            DebugEx.Success("InventorySlotUI", $"[ShowContextMenu] 显示上下文菜单（来自InventoryUI）: {itemStack.Item.Name}");
            return;
        }

        // 尝试获取 WarehouseUI
        var warehouseUI = GetComponentInParent<WarehouseUI>();
        if (warehouseUI != null)
        {
            warehouseUI.ShowItemContextMenu(itemStack, slotIndex, slotRect);
            DebugEx.Success("InventorySlotUI", $"[ShowContextMenu] 显示上下文菜单（来自WarehouseUI）: {itemStack.Item.Name}");
            return;
        }

        DebugEx.Error("InventorySlotUI", "[ShowContextMenu] 无法获取 InventoryUI 或 WarehouseUI");
    }

    #endregion
}
