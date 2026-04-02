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

        // 查找或初始化菜单按钮
        InitializeButtons();

        // 初始状态隐藏
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 初始化菜单按钮（从子物体中查找）
    /// </summary>
    private void InitializeButtons()
    {
        var buttons = GetComponentsInChildren<Button>(true);

        if (buttons.Length > 0)
            m_UseBtn = buttons[0];
        if (buttons.Length > 1)
            m_SplitBtn = buttons[1];
        if (buttons.Length > 2)
            m_DiscardBtn = buttons[2];

        DebugEx.Log("ItemContextMenu", $"按钮初始化完成：UseBtn={m_UseBtn != null}, SplitBtn={m_SplitBtn != null}, DiscardBtn={m_DiscardBtn != null}");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示上下文菜单
    /// </summary>
    public void ShowContextMenu(ItemStack itemStack, int slotIndex, Vector2 position, RectTransform slotRect = null)
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
            if (slotRect != null)
            {
                var parentCanvas = GetComponentInParent<Canvas>();
                var canvasRect = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;
                if (canvasRect == null)
                {
                    DebugEx.Error("ItemContextMenu", "未找到 Canvas");
                    return;
                }

                // 格子中心的屏幕坐标
                Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
                Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(cam, slotRect.position);

                // 偏移规则：(格子宽度 + 菜单宽度) / 2 + 5，在屏幕坐标里偏移像素
                float offsetX = (slotRect.rect.width + rectTransform.rect.width) / 2f;
                // rect.width 是 Unity 单位，需要乘以 canvas scale 转换为屏幕像素
                float canvasScale = canvasRect.localScale.x;
                Vector2 menuScreenPos = slotScreenPos + new Vector2(offsetX * canvasScale, 0);

                // 屏幕坐标转 Canvas 本地坐标，设置 anchoredPosition
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, menuScreenPos, cam, out var localPos);
                rectTransform.anchoredPosition = localPos;

                DebugEx.Log("ItemContextMenu", $"格子屏幕坐标: {slotScreenPos}, 偏移(px): {offsetX * canvasScale}, 菜单屏幕坐标: {menuScreenPos}, anchoredPosition: {localPos}");
            }
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

        // 隐藏所有按钮，确保 MenuBg 显示
        HideAllButtons();
        if (varMenuBg != null) varMenuBg.SetActive(true);

        int itemType = m_CurrentItemRow.Type;

        // 根据物品类型显示不同的选项
        // 物品类型：1=消耗品, 2=任务道具, 3=宝物, 4=装备
        switch (itemType)
        {
            case 1: // 消耗品
                ShowButton(m_UseBtn, "使用", OnClickUse);
                if (m_CurrentItemStack.Count > 1)
                {
                    ShowButton(m_SplitBtn, "拆分", OnClickSplit);
                }
                ShowButton(m_DiscardBtn, "丢弃", OnClickDiscard);
                break;

            case 2: // 任务道具
                // 任务道具隐藏菜单背景
                if (varMenuBg != null) varMenuBg.SetActive(false);
                DebugEx.Log("ItemContextMenu", "任务道具，隐藏菜单背景");
                break;

            case 3: // 宝物
                ShowButton(m_DiscardBtn, "丢弃", OnClickDiscard);
                break;

            case 4: // 装备
                ShowButton(m_UseBtn, "装备", OnClickUse);
                ShowButton(m_DiscardBtn, "丢弃", OnClickDiscard);
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
            bool success = inventoryManager.RemoveItem(m_CurrentItemStack.Item.ItemId, m_CurrentItemStack.Count);
            if (success)
            {
                DebugEx.Success("ItemContextMenu", $"丢弃物品: {m_CurrentItemStack.Item.Name} x{m_CurrentItemStack.Count}");
            }
            else
            {
                DebugEx.Warning("ItemContextMenu", $"丢弃物品失败: {m_CurrentItemStack.Item.Name}");
            }
        }

        HideContextMenu();
    }

    /// <summary>
    /// 限制菜单位置在屏幕范围内
    /// </summary>
    private void ClampMenuPositionToScreen(RectTransform menuRT, RectTransform canvasRT)
    {
        if (menuRT == null || canvasRT == null)
            return;

        var menuSize = menuRT.sizeDelta;
        var canvasSize = canvasRT.sizeDelta;
        var pos = menuRT.anchoredPosition;

        // 右边界检查
        if (pos.x + menuSize.x / 2 > canvasSize.x / 2)
        {
            pos.x = canvasSize.x / 2 - menuSize.x / 2;
        }

        // 左边界检查
        if (pos.x - menuSize.x / 2 < -canvasSize.x / 2)
        {
            pos.x = -canvasSize.x / 2 + menuSize.x / 2;
        }

        // 上边界检查
        if (pos.y + menuSize.y / 2 > canvasSize.y / 2)
        {
            pos.y = canvasSize.y / 2 - menuSize.y / 2;
        }

        // 下边界检查
        if (pos.y - menuSize.y / 2 < -canvasSize.y / 2)
        {
            pos.y = -canvasSize.y / 2 + menuSize.y / 2;
        }

        menuRT.anchoredPosition = pos;
    }

    #endregion
}
