using UnityEngine;

/// <summary>
/// 格子所属的容器类型
/// </summary>
public enum SlotContainerType
{
    Inventory,
    Warehouse,
    Equip,
}

/// <summary>
/// 背包/仓库格子UI
/// 只负责格子容器（背景槽位），InventoryItemUI 已作为子对象预置在 varInventoryItemUI 下
/// </summary>
public partial class InventorySlotUI : UIItemBase
{
    /// <summary>缓存的 InventoryItemUI 组件</summary>
    private InventoryItemUI m_ItemUI;

    /// <summary>格子索引</summary>
    public int SlotIndex { get; private set; }

    /// <summary>所属容器类型（由 InventoryUI / WarehouseUI 初始化时设置）</summary>
    public SlotContainerType ContainerType { get; private set; }

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
}
