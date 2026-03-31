using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品上下文菜单
/// 根据物品类型动态显示不同的操作选项
/// </summary>
public partial class ItemContextMenu : UIItemBase
{
    #region 字段

    private ItemStack m_CurrentItemStack;
    private ItemTable m_CurrentItemRow;
    private int m_CurrentSlotIndex;

    // 菜单选项按钮
    private Button m_UseBtn;
    private Button m_SplitBtn;
    private Button m_DiscardBtn;

    #endregion

    #region 初始化

    protected override void OnInit()
    {
        base.OnInit();

        DebugEx.Log("ItemContextMenu", "物品上下文菜单初始化");

        // 初始状态隐藏
        gameObject.SetActive(false);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示上下文菜单
    /// </summary>
    public void ShowContextMenu(ItemStack itemStack, int slotIndex, Vector2 position)
    {
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning("ItemContextMenu", "物品堆叠为空，无法显示菜单");
            return;
        }

        m_CurrentItemStack = itemStack;
        m_CurrentSlotIndex = slotIndex;

        // 从 ItemTable 获取物品配置
        var itemTable = GF.DataTable.GetDataTable<ItemTable>();
        if (itemTable == null)
        {
            DebugEx.Error("ItemContextMenu", "ItemTable 未加载");
            return;
        }

        m_CurrentItemRow = itemTable.GetDataRow(itemStack.Item.ItemId);
        if (m_CurrentItemRow == null)
        {
            DebugEx.Error("ItemContextMenu", $"ItemTable 中不存在 ID={itemStack.Item.ItemId} 的物品");
            return;
        }

        // 刷新菜单选项
        RefreshMenuOptions();

        // 设置位置并显示
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.position = position;
        }

        gameObject.SetActive(true);

        DebugEx.Success("ItemContextMenu", $"显示上下文菜单: {itemStack.Item.Name}");
    }

    /// <summary>
    /// 隐藏上下文菜单
    /// </summary>
    public void HideContextMenu()
    {
        gameObject.SetActive(false);
        m_CurrentItemStack = null;
        m_CurrentItemRow = null;

        DebugEx.Log("ItemContextMenu", "上下文菜单已隐藏");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 刷新菜单选项（根据物品类型显示不同选项）
    /// </summary>
    private void RefreshMenuOptions()
    {
        if (m_CurrentItemRow == null)
            return;

        // 隐藏所有按钮
        HideAllButtons();

        int itemType = m_CurrentItemRow.Type;

        // 根据物品类型显示不同的选项
        // 物品类型：0=消耗品, 1=装备, 2=宝物, 3=任务道具
        switch (itemType)
        {
            case 0: // 消耗品
                ShowButton(m_UseBtn, "使用", OnClickUse);
                if (m_CurrentItemStack.Count > 1)
                {
                    ShowButton(m_SplitBtn, "拆分", OnClickSplit);
                }
                ShowButton(m_DiscardBtn, "丢弃", OnClickDiscard);
                break;

            case 1: // 装备
                ShowButton(m_UseBtn, "装备", OnClickUse);
                ShowButton(m_DiscardBtn, "丢弃", OnClickDiscard);
                break;

            case 2: // 宝物
                ShowButton(m_DiscardBtn, "丢弃", OnClickDiscard);
                break;

            case 3: // 任务道具
                // 任务道具不可丢弃，不显示任何选项
                DebugEx.Log("ItemContextMenu", "任务道具不可操作");
                break;

            default:
                DebugEx.Warning("ItemContextMenu", $"未知的物品类型: {itemType}");
                break;
        }
    }

    /// <summary>
    /// 隐藏所有按钮
    /// </summary>
    private void HideAllButtons()
    {
        if (m_UseBtn != null) m_UseBtn.gameObject.SetActive(false);
        if (m_SplitBtn != null) m_SplitBtn.gameObject.SetActive(false);
        if (m_DiscardBtn != null) m_DiscardBtn.gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示按钮并绑定事件
    /// </summary>
    private void ShowButton(Button button, string text, UnityEngine.Events.UnityAction callback)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(true);
        
        // 设置按钮文本
        var textComponent = button.GetComponentInChildren<Text>();
        if (textComponent != null)
        {
            textComponent.text = text;
        }

        // 清除旧的监听器并添加新的
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(callback);
    }

    /// <summary>
    /// 使用物品
    /// </summary>
    private void OnClickUse()
    {
        if (m_CurrentItemStack == null || m_CurrentSlotIndex < 0)
            return;

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            if (inventoryManager.UseItem(m_CurrentSlotIndex))
            {
                DebugEx.Success("ItemContextMenu", $"使用物品: {m_CurrentItemStack.Item.Name}");
            }
        }

        HideContextMenu();
    }

    /// <summary>
    /// 拆分物品
    /// </summary>
    private void OnClickSplit()
    {
        if (m_CurrentItemStack == null || m_CurrentSlotIndex < 0)
            return;

        // TODO: 显示拆分对话框
        DebugEx.Log("ItemContextMenu", $"拆分物品: {m_CurrentItemStack.Item.Name}");

        HideContextMenu();
    }

    /// <summary>
    /// 丢弃物品
    /// </summary>
    private void OnClickDiscard()
    {
        if (m_CurrentItemStack == null || m_CurrentSlotIndex < 0)
            return;

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            inventoryManager.RemoveItem(m_CurrentSlotIndex, m_CurrentItemStack.Count);
            DebugEx.Success("ItemContextMenu", $"丢弃物品: {m_CurrentItemStack.Item.Name}");
        }

        HideContextMenu();
    }

    #endregion
}
