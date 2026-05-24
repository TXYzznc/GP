using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.EventSystems;

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

    /// <summary>当前显示的悬浮提示框ID</summary>
    private int m_FloatingTipId = -1;

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

    private void OnDestroy()
    {
        // 清理提示框
        HideTooltip();

        // 清理 InventoryItemUI 引用
        if (m_ItemUI != null)
        {
            Destroy(m_ItemUI.gameObject);
            m_ItemUI = null;
        }
        m_IsItemUILoaded = false;
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
    /// 获取宝物ItemUI（用于宝物仓库）
    /// </summary>
    public TreasureItemUI GetTreasureItemUI()
    {
        return GetComponentInChildren<TreasureItemUI>(true);
    }

    /// <summary>
    /// 检查格子是否有物品（兼容InventoryItemUI和TreasureItemUI）
    /// </summary>
    public bool HasAnyItem()
    {
        // 检查 InventoryItemUI
        if (m_ItemUI != null && m_ItemUI.HasItem())
        {
            return true;
        }

        // 检查 TreasureItemUI
        var treasureItemUI = GetTreasureItemUI();
        if (treasureItemUI != null && treasureItemUI.gameObject.activeSelf)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 设置格子数据（延迟加载 InventoryItemUI）
    /// 有物品时：如果已有 ItemUI 则复用，否则异步加载
    /// 无物品时：隐藏 ItemUI（不销毁，复用）
    /// </summary>
    public void SetData(ItemStack itemStack)
    {
        // 暂存数据（可能需要异步加载后才能设置）
        m_PendingItemStack = itemStack;

        // 情况1：有物品 + 已有ItemUI → 直接复用
        if (itemStack != null && !itemStack.IsEmpty && m_ItemUI != null)
        {
            DebugEx.Log(
                GetType().Name,
                $"复用 ItemUI: 格子={SlotIndex}, 物品={itemStack.Item.Name}"
            );
            ApplyItemData(itemStack);
            return;
        }

        // 情况2：有物品 + 无ItemUI → 异步加载（只有真的没有ItemUI对象时才加载）
        if (itemStack != null && !itemStack.IsEmpty && m_ItemUI == null)
        {
            LoadItemUISync();
            return; // 异步加载完成后会设置数据，这里先返回
        }

        // 情况3：无物品 → 隐藏ItemUI（不销毁，保留复用）
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
        // 关键修复：检查m_ItemUI而不是m_IsItemUILoaded，避免重复创建
        if (m_ItemUI != null)
        {
            DebugEx.Log(this.GetType().Name, $"ItemUI已存在，直接复用: 格子={SlotIndex}");
            ApplyItemData(m_PendingItemStack);
            return;
        }

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

        // 再次检查，防止异步期间已经创建
        if (m_ItemUI != null)
        {
            DebugEx.Warning(
                this.GetType().Name,
                $"异步期间ItemUI已创建，取消重复加载: 格子={SlotIndex}"
            );
            ApplyItemData(m_PendingItemStack);
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

        // 如果是宝物格子，显示提示框
        var treasureItemUI = GetTreasureItemUI();
        if (treasureItemUI != null && treasureItemUI.HasItem())
        {
            ShowTreasureTooltip();
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

        // 隐藏提示框
        HideTooltip();
    }

    #endregion

    #region 点击事件处理


    /// <summary>
    /// 处理左键点击（显示物品详情）
    /// 由 InventoryClickHandler 分发调用
    /// </summary>
    public void OnLeftClick()
    {
        // 优先检查 InventoryItemUI
        var itemUI = GetItemUI();
        if (itemUI != null && itemUI.HasItem())
        {
            var itemStack = itemUI.GetItemStack();
            if (itemStack != null && !itemStack.IsEmpty)
            {
                DebugEx.Log(
                    this.GetType().Name,
                    $"[OnLeftClick] 左键点击 格子={SlotIndex} 物品={itemStack.Item.Name}"
                );
                ShowItemDetailPanel(itemStack);
                return;
            }
        }

        // 检查 TreasureItemUI
        var treasureItemUI = GetTreasureItemUI();
        if (treasureItemUI != null && treasureItemUI.gameObject.activeSelf)
        {
            int treasureId = treasureItemUI.GetTreasureId();
            DebugEx.Log(
                this.GetType().Name,
                $"[OnLeftClick] 左键点击宝物 格子={SlotIndex} treasureId={treasureId}"
            );
            ShowTreasureTooltip();
            return;
        }

        DebugEx.Warning(this.GetType().Name, $"[OnLeftClick] 格子 {SlotIndex} 无物品");
    }

    /// <summary>
    /// 显示宝物提示框
    /// </summary>
    private void ShowTreasureTooltip()
    {
        var treasureItemUI = GetTreasureItemUI();
        if (treasureItemUI == null || !treasureItemUI.HasItem())
        {
            DebugEx.Warning(GetType().Name, "[ShowTreasureTooltip] 宝物ItemUI为空或无数据");
            return;
        }

        int treasureId = treasureItemUI.GetTreasureId();
        string detailText = BuildTreasureDetailText(treasureId);

        if (string.IsNullOrEmpty(detailText))
        {
            DebugEx.Warning(
                GetType().Name,
                $"[ShowTreasureTooltip] 宝物详情文本为空: treasureId={treasureId}"
            );
            return;
        }

        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            DebugEx.Error(GetType().Name, "[ShowTreasureTooltip] RectTransform为空");
            return;
        }

        // 先隐藏旧的提示框
        HideTooltip();

        // 显示新的提示框（在格子上方，水平居中）
        m_FloatingTipId = GF.UI.ShowFloatingTipAt(detailText, rectTransform, new Vector2(0f, 10f));

        DebugEx.Log(
            GetType().Name,
            $"[ShowTreasureTooltip] 显示宝物提示框: treasureId={treasureId}, tipId={m_FloatingTipId}"
        );
    }

    /// <summary>
    /// 构建宝物详情文本
    /// </summary>
    private string BuildTreasureDetailText(int treasureId)
    {
        // 从 TreasureTable 获取宝物数据
        var dtTreasure = GF.DataTable.GetDataTable<TreasureTable>();
        var treasureRow = dtTreasure?.GetDataRow(treasureId);

        if (treasureRow == null)
        {
            DebugEx.Error(
                GetType().Name,
                $"[BuildTreasureDetailText] 未找到宝物数据: treasureId={treasureId}"
            );
            return string.Empty;
        }

        // 从 ItemTable 获取物品基础数据
        var dtItem = GF.DataTable.GetDataTable<ItemTable>();
        var itemRow = dtItem?.GetDataRow(treasureRow.Id);

        if (itemRow == null)
        {
            DebugEx.Error(
                GetType().Name,
                $"[BuildTreasureDetailText] 未找到物品数据: ItemTableId={treasureRow.Id}"
            );
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();

        // 标题：宝物名称
        sb.AppendLine($"<b>{itemRow.Name}</b>");
        sb.AppendLine();

        // 品质
        if (itemRow.Rarity > 0)
        {
            string rarityText = itemRow.Rarity switch
            {
                1 => "普通",
                2 => "稀有",
                3 => "史诗",
                4 => "传奇",
                5 => "神话",
                _ => itemRow.Rarity.ToString(),
            };
            sb.AppendLine($"品质: {rarityText}");
        }

        // 重量
        if (itemRow.Weight > 0)
        {
            sb.AppendLine($"重量: {itemRow.Weight}g");
        }

        // 从 ItemManager 获取宝物详细数据
        var treasureData = ItemManager.Instance?.GetTreasureData(treasureRow.Id);
        if (treasureData != null)
        {
            // 羁绊
            if (treasureData.SynergyIds != null && treasureData.SynergyIds.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[羁绊]");
                foreach (var synergyId in treasureData.SynergyIds)
                {
                    // TODO: 从 SynergyTable 获取羁绊名称
                    sb.AppendLine($"  羁绊ID: {synergyId}");
                }
            }

            // 基础属性
            if (treasureData.BaseAttributes != null && treasureData.BaseAttributes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[基础属性]");
                foreach (var attr in treasureData.BaseAttributes)
                {
                    string attrName = GetAttributeName(attr.Key.ToString());
                    sb.AppendLine($"  {attrName}: +{attr.Value}");
                }
            }

            // 特殊效果
            if (treasureData.SpecialEffectId > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"特殊效果ID: {treasureData.SpecialEffectId}");
                // TODO: 从 SpecialEffectTable 获取效果描述
            }
        }

        // 描述
        if (!string.IsNullOrEmpty(itemRow.Description))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#808080>{itemRow.Description}</color>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取属性名称（中文）
    /// </summary>
    private string GetAttributeName(string attrKey)
    {
        return attrKey switch
        {
            "MaxHP" => "生命值",
            "Attack" => "攻击力",
            "Defense" => "防御力",
            "MagicAttack" => "魔法攻击",
            "MagicDefense" => "魔法防御",
            "Speed" => "速度",
            "CritRate" => "暴击率",
            "CritDamage" => "暴击伤害",
            "Dodge" => "闪避",
            "Hit" => "命中",
            _ => attrKey,
        };
    }

    /// <summary>
    /// 隐藏提示框
    /// </summary>
    private void HideTooltip()
    {
        if (m_FloatingTipId > 0)
        {
            GF.UI.CloseUIForm(m_FloatingTipId);
            m_FloatingTipId = -1;

            DebugEx.Log(GetType().Name, "[HideTooltip] 隐藏提示框");
        }
    }

    /// <summary>
    /// 处理右键点击
    /// 宝箱格子：快捷键直接移入背包（不显示菜单）
    /// 其他容器：显示上下文菜单
    /// </summary>
    public void OnRightClick(Vector2 mousePosition)
    {
        // 优先检查 InventoryItemUI
        var itemUI = GetItemUI();
        if (itemUI != null && itemUI.HasItem())
        {
            var itemStack = itemUI.GetItemStack();
            if (itemStack != null && !itemStack.IsEmpty)
            {
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
                            bool ok =
                                InventoryManager.Instance?.AddItem(slot.ItemId, slot.Count)
                                ?? false;
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
                                DebugEx.Warning(
                                    this.GetType().Name,
                                    "[OnRightClick] 背包已满，无法放入"
                                );
                            }
                        }
                    }
                    return;
                }

                // 其他容器：显示上下文菜单
                ShowContextMenu(itemStack, SlotIndex, mousePosition, GetComponent<RectTransform>());
                return;
            }
        }

        // 检查 TreasureItemUI
        var treasureItemUI = GetTreasureItemUI();
        if (treasureItemUI != null && treasureItemUI.gameObject.activeSelf)
        {
            int treasureId = treasureItemUI.GetTreasureId();
            DebugEx.Log(
                this.GetType().Name,
                $"[OnRightClick] 右键点击宝物 格子={SlotIndex} treasureId={treasureId}"
            );
            // TODO: 显示宝物右键菜单（如果需要）
            return;
        }

        DebugEx.Warning(this.GetType().Name, $"[OnRightClick] 格子 {SlotIndex} 无物品");
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
    /// 加载并初始化TreasureItemUI（用于CharacterBagUI的宝物仓库）
    /// </summary>
    public void LoadTreasureItemUI(int treasureId)
    {
        if (varTreasureItemUI == null)
        {
            DebugEx.Error(GetType().Name, "[LoadTreasureItemUI] varTreasureItemUI预制体未配置");
            return;
        }

        // 检查是否已经实例化过
        TreasureItemUI existingItem = GetComponentInChildren<TreasureItemUI>(true);

        if (existingItem == null)
        {
            // 实例化预制体
            GameObject treasureItemObj = Instantiate(varTreasureItemUI, transform);
            treasureItemObj.name = "TreasureItemUI";

            // 设置大小和分层
            SetupChildUITransform(treasureItemObj);

            existingItem = treasureItemObj.GetComponent<TreasureItemUI>();

            DebugEx.Log(
                GetType().Name,
                $"[LoadTreasureItemUI] 实例化TreasureItemUI: treasureId={treasureId}"
            );
        }

        // 初始化宝物数据
        if (existingItem != null)
        {
            existingItem.InitTreasure(treasureId);
            existingItem.gameObject.SetActive(true);

            DebugEx.Success(
                GetType().Name,
                $"[LoadTreasureItemUI] 宝物加载成功: treasureId={treasureId}"
            );
        }
    }

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
