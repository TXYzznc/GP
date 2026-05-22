using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;


/// <summary>
/// 背包/仓库格子UI
/// 只负责格子容器（背景槽位），InventoryItemUI 通过延迟加载实现
/// 有物品时异步加载，无物品时隐藏（不销毁）
/// </summary>
public partial class InventorySlotUI : UIItemBase, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>缓存的 InventoryItemUI 组件</summary>
    private InventoryItemUI m_ItemUI;

    /// <summary>InventoryItemUI 是否已实例化</summary>
    private bool m_IsItemUILoaded = false;

    /// <summary>待设置的物品数据（等待异步加载完成后设置）</summary>
    private ItemStack m_PendingItemStack;

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
        m_ItemUI = null;
        m_IsItemUILoaded = false;

        // 为 TreasureItemUI 也设置大小和分层
        if (varTreasureItemUI != null)
        {
            SetupChildUITransform(varTreasureItemUI);
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
            DebugEx.Warning(
                this.GetType().Name,
                $"格子容器类型不匹配: ContainerType={ContainerType}, Container.Type={container.ContainerType}"
            );
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


    public InventoryItemUI GetItemUI() => m_ItemUI;

    /// <summary>
    /// 设置格子数据（延迟加载 InventoryItemUI）
    /// 有物品时：加载并显示 ItemUI
    /// 无物品时：隐藏 ItemUI（不销毁，复用）
    /// </summary>
    public void SetData(ItemStack itemStack)
    {
        // 暂存数据（可能需要异步加载后才能设置）
        m_PendingItemStack = itemStack;

        // 1. 判断是否需要加载 InventoryItemUI
        if (itemStack != null && !itemStack.IsEmpty && !m_IsItemUILoaded)
        {
            LoadItemUISync();
            return;  // 异步加载完成后会设置数据，这里先返回
        }

        // 2. 如果已经加载完成或无物品，立即设置数据
        ApplyItemData(itemStack);
    }

    /// <summary>
    /// 应用物品数据到 ItemUI（确保加载完成后调用）
    /// </summary>
    private void ApplyItemData(ItemStack itemStack)
    {
        var itemUI = m_ItemUI;
        if (itemUI == null)
            return;

        itemUI.SetData(itemStack);
        itemUI.gameObject.SetActive(itemStack != null && !itemStack.IsEmpty);

    }

    /// <summary>
    /// 设置子 UI（如 InventoryItemUI、TreasureItemUI）的大小和分层
    /// 确保子 UI 铺满整个槽位，且 varLock 始终在最上层
    /// </summary>
    public void SetupChildUITransform(GameObject childUI)
    {
        if (childUI == null)
            return;

        // 设置大小为与 InventorySlotUI 一致（铺满整个槽位）
        var childRect = childUI.GetComponent<RectTransform>();
        if (childRect != null)
        {
            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.one;
            childRect.offsetMin = Vector2.zero;
            childRect.offsetMax = Vector2.zero;
        }

        // 确保 varLock 始终在最上层（最后面）
        if (varLock != null)
        {
            varLock.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// 清理（保留接口兼容性，不再有实际订阅需要清理）
    /// </summary>
    public void ClearItemQuantitySubscription() { }

    /// <summary>
    /// 加载 InventoryItemUI（异步）
    /// </summary>
    private void LoadItemUISync()
    {
        if (m_IsItemUILoaded || m_ItemUI != null)
            return;

        LoadItemUIAsync().Forget();
    }

    /// <summary>
    /// 异步加载 InventoryItemUI 预制体并实例化
    /// </summary>
    private async UniTaskVoid LoadItemUIAsync()
    {
        if (m_IsItemUILoaded || m_ItemUI != null)
            return;

        if (varInventoryItemUI == null)
        {
            DebugEx.Error(this.GetType().Name, "InventoryItemUI 预制体未设置");
            return;
        }

        DebugEx.Log(this.GetType().Name, $"→ 加载 InventoryItemUI");

        // 下一帧再加载，避免同一帧实例化太多对象
        await UniTask.Yield();

        if (!gameObject.activeInHierarchy)
        {
            DebugEx.Warning(this.GetType().Name, "格子已销毁，取消加载");
            return;
        }

        // 实例化预制体为子对象
        GameObject instance = Instantiate(varInventoryItemUI, transform);
        instance.name = "InventoryItemUI";

        m_ItemUI = instance.GetComponent<InventoryItemUI>();
        if (m_ItemUI == null)
        {
            DebugEx.Error(this.GetType().Name, "InventoryItemUI 预制体缺少组件");
            Destroy(instance);
            return;
        }

        // 设置子 UI 的大小和分层
        SetupChildUITransform(instance);

        m_IsItemUILoaded = true;
        DebugEx.Log(this.GetType().Name, $"✓ InventoryItemUI 加载完成");

        // 加载完成后，应用待设置的数据
        ApplyItemData(m_PendingItemStack);
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
            DebugEx.Warning(this.GetType().Name, $"[OnLeftClick] 格子 {SlotIndex} 无物品");
            return;
        }

        var itemStack = itemUI.GetItemStack();
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning(this.GetType().Name, $"[OnLeftClick] 物品堆叠为空");
            return;
        }

        DebugEx.Log(
            this.GetType().Name,
            $"[OnLeftClick] 左键点击 格子={SlotIndex} 物品={itemStack.Item.Name}"
        );

        ShowItemDetailPanel(itemStack);
    }

    /// <summary>
    /// 处理右键点击
    /// 宝箱格子：快捷键直接移入背包（不显示菜单）
    /// 其他容器：显示上下文菜单
    /// </summary>
    public void OnRightClick(Vector2 mousePosition)
    {
        var itemUI = GetItemUI();
        if (itemUI == null || !itemUI.HasItem())
        {
            DebugEx.Warning(this.GetType().Name, $"[OnRightClick] 格子 {SlotIndex} 无物品");
            return;
        }

        var itemStack = itemUI.GetItemStack();
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning(this.GetType().Name, $"[OnRightClick] 物品堆叠为空");
            return;
        }

        DebugEx.Log(
            this.GetType().Name,
            $"[OnRightClick] 右键点击 格子={SlotIndex} 物品={itemStack.Item.Name} 容器={ContainerType}"
        );

        // 宝箱格子：右键快捷键直接移入背包
        if (ContainerType == SlotContainerType.TreasureBox)
        {
            var treasureContainer = SlotContainer as TreasureBoxSlotContainerImpl;
            if (treasureContainer != null)
            {
                var slot = treasureContainer.GetSlot(SlotIndex);
                if (slot != null && !slot.IsEmpty)
                {
                    // 尝试添加到背包
                    bool ok = InventoryManager.Instance?.AddItem(slot.ItemId, slot.Count) ?? false;
                    if (ok)
                    {
                        treasureContainer.RemoveItem(SlotIndex, slot.Count);
                        DebugEx.Success(
                            this.GetType().Name,
                            $"[OnRightClick] 宝箱物品快捷放入背包: {itemStack.Item.Name}"
                        );
                    }
                    else
                    {
                        DebugEx.Warning(this.GetType().Name, "[OnRightClick] 背包已满，无法放入");
                    }
                }
            }
            return;
        }

        // 其他容器：显示上下文菜单
        ShowContextMenu(itemStack, SlotIndex, mousePosition, GetComponent<RectTransform>());
    }

    /// <summary>
    /// 显示物品详情面板
    /// </summary>
    private void ShowItemDetailPanel(ItemStack itemStack)
    {
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning(this.GetType().Name, "[ShowItemDetailPanel] 物品堆叠为空");
            return;
        }

        // 获取 InventoryUI（背包是包含此格子的 UI）
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.ShowItemDetail(itemStack);
            DebugEx.Success(
                this.GetType().Name,
                $"[ShowItemDetailPanel] 显示物品详情: {itemStack.Item.Name}"
            );
        }
        else
        {
            DebugEx.Warning(this.GetType().Name, "[ShowItemDetailPanel] 无法获取 InventoryUI");
        }
    }

    /// <summary>
    /// 显示上下文菜单
    /// </summary>
    private void ShowContextMenu(
        ItemStack itemStack,
        int slotIndex,
        Vector2 position,
        RectTransform slotRect
    )
    {
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning(this.GetType().Name, "[ShowContextMenu] 物品堆叠为空");
            return;
        }

        // 尝试获取 InventoryUI
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.ShowItemContextMenu(itemStack, slotIndex, slotRect);
            DebugEx.Success(
                this.GetType().Name,
                $"[ShowContextMenu] 显示上下文菜单（来自InventoryUI）: {itemStack.Item.Name}"
            );
            return;
        }

        // 尝试获取 WarehouseUI
        var warehouseUI = GetComponentInParent<WarehouseUI>();
        if (warehouseUI != null)
        {
            warehouseUI.ShowItemContextMenu(itemStack, slotIndex, slotRect);
            DebugEx.Success(
                this.GetType().Name,
                $"[ShowContextMenu] 显示上下文菜单（来自WarehouseUI）: {itemStack.Item.Name}"
            );
            return;
        }

        // 尝试获取 TreasureBoxUI
        var treasureBoxUI = GetComponentInParent<TreasureBoxUI>();
        if (treasureBoxUI != null)
        {
            treasureBoxUI.ShowItemContextMenu(itemStack, slotIndex, slotRect);
            DebugEx.Success(
                this.GetType().Name,
                $"[ShowContextMenu] 显示上下文菜单（来自TreasureBoxUI）: {itemStack.Item.Name}"
            );
            return;
        }

        DebugEx.Error(
            this.GetType().Name,
            "[ShowContextMenu] 无法获取 InventoryUI、WarehouseUI 或 TreasureBoxUI"
        );
    }

    #endregion

    #region 公共方法 - 用于设置宝物显示

    /// <summary>
    /// 获取宝物物品UI容器GameObject
    /// </summary>
    public GameObject GetTreasureItemUIContainer() => varTreasureItemUI;

    /// <summary>
    /// 显示或隐藏格子锁定状态
    /// </summary>
    public void SetLockVisible(bool visible)
    {
        if (varLock != null)
            varLock.SetActive(visible);
    }

    /// <summary>
    /// 设置锁定文本内容
    /// </summary>
    public void SetLockText(string text)
    {
        if (varLockText != null)
            varLockText.text = text;
    }

    #endregion
}
