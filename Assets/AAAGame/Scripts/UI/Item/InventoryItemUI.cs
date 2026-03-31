using System;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class InventoryItemUI : UIItemBase
{
    #region 字段

    private ItemStack m_ItemStack; // 物品堆叠数据
    #endregion

    #region 初始化

    protected override void OnInit()
    {
        base.OnInit();

        DebugEx.Log("InventoryItemUI", "物品UI初始化");

        // 绑定按钮事件
        if (varItemBtn != null)
        {
            varItemBtn.onClick.AddListener(OnItemClick);
            
            // 添加右键点击事件处理
            var eventTrigger = varItemBtn.gameObject.AddComponent<EventTrigger>();
            var pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDownEntry.callback.AddListener((data) => OnItemRightClick((PointerEventData)data));
            eventTrigger.triggers.Add(pointerDownEntry);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(ItemStack itemStack)
    {
        m_ItemStack = itemStack;

        if (itemStack == null || itemStack.IsEmpty)
        {
            Clear();
            return;
        }

        // 刷新显示
        RefreshDisplay();
    }

    /// <summary>
    /// 清空显示
    /// </summary>
    public void Clear()
    {
        m_ItemStack = null;

        if (varItemImg != null)
        {
            varItemImg.sprite = null;
            varItemImg.color = new Color(1, 1, 1, 0);
        }

        // 隐藏整个InventoryItemUI对象（格子没有物品时隐藏）
        gameObject.SetActive(false);
        DebugEx.LogModule("InventoryItemUI", "清空物品显示");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 刷新显示
    /// </summary>
    private void RefreshDisplay()
    {
        if (m_ItemStack == null || m_ItemStack.IsEmpty)
        {
            return;
        }

        gameObject.SetActive(true);

        var item = m_ItemStack.Item;
        var itemData = item.ItemData;

        // 加载物品图标
        LoadItemIconAsync(itemData.GetIconId()).Forget();

        // 显示物品数量（可堆叠且数量>1时显示，格式为 xN）
        if (varCountText != null)
        {
            bool showCount = item.MaxStackCount > 1 && m_ItemStack.Count > 1;
            varCountText.text = $"x{m_ItemStack.Count}";  // 使用字符串插值，性能优于字符串拼接
            varCountText.gameObject.SetActive(showCount);
        }

        DebugEx.Log("InventoryItemUI", $"刷新物品显示: {item.Name}, 数量:{m_ItemStack.Count}");
    }

    /// <summary>
    /// 加载物品图标
    /// </summary>
    private async UniTask LoadItemIconAsync(int iconId)
    {
        if (iconId <= 0)
        {
            DebugEx.Warning("InventoryItemUI", "物品图标ID无效");
            return;
        }

        try
        {
            DebugEx.Log("InventoryItemUI", $"开始加载物品图标: IconId={iconId}");

            // 使用ResourceExtension加载图标到Image对象
            if (varItemImg != null)
            {
                await GameExtension.ResourceExtension.LoadSpriteAsync(iconId, varItemImg, 1f, null);
                varItemImg.color = Color.white;
                DebugEx.Success("InventoryItemUI", $"物品图标加载成功: IconId={iconId}");
            }
            else
            {
                DebugEx.Warning("InventoryItemUI", $"物品图标加载失败: Image为null, IconId={iconId}");
            }
        }
        catch (Exception e)
        {
            DebugEx.Error(
                "InventoryItemUI",
                $"加载物品图标异常: IconId={iconId}, Error:{e.Message}"
            );
        }
    }

    /// <summary>
    /// 物品点击事件
    /// </summary>
    private void OnItemClick()
    {
        if (m_ItemStack == null || m_ItemStack.IsEmpty)
        {
            return;
        }

        var item = m_ItemStack.Item;

        DebugEx.Log("InventoryItemUI", $"点击物品: {item.Name}");

        // 显示物品详情面板
        ShowItemDetailPanel();
    }

    /// <summary>
    /// 显示物品详情面板
    /// </summary>
    private void ShowItemDetailPanel()
    {
        if (m_ItemStack == null || m_ItemStack.IsEmpty)
        {
            return;
        }

        // 获取 InventoryUI 并调用其显示详情方法
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.ShowItemDetail(m_ItemStack);
            DebugEx.Success("InventoryItemUI", $"显示物品详情: {m_ItemStack.Item.Name}");
        }
        else
        {
            DebugEx.Error("InventoryItemUI", "无法获取 InventoryUI 组件");
        }
    }

    /// <summary>
    /// 物品右键点击事件
    /// </summary>
    private void OnItemRightClick(PointerEventData eventData)
    {
        // 检查是否是右键点击
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (m_ItemStack == null || m_ItemStack.IsEmpty)
            return;

        var item = m_ItemStack.Item;

        DebugEx.Log("InventoryItemUI", $"右键点击物品: {item.Name}");

        // 显示上下文菜单
        ShowContextMenu(eventData.position);
    }

    /// <summary>
    /// 显示上下文菜单
    /// </summary>
    private void ShowContextMenu(Vector2 position)
    {
        if (m_ItemStack == null || m_ItemStack.IsEmpty)
            return;

        // 获取或创建上下文菜单
        var contextMenu = GetItemContextMenu();
        if (contextMenu != null)
        {
            // 获取格子索引
            var slotUI = GetComponentInParent<InventorySlotUI>();
            int slotIndex = slotUI != null ? slotUI.SlotIndex : -1;

            contextMenu.ShowContextMenu(m_ItemStack, slotIndex, position);
            DebugEx.Success("InventoryItemUI", $"显示上下文菜单: {m_ItemStack.Item.Name}");
        }
        else
        {
            DebugEx.Error("InventoryItemUI", "无法获取上下文菜单");
        }
    }

    /// <summary>
    /// 获取物品上下文菜单（从 InventoryUI 获取）
    /// </summary>
    private ItemContextMenu GetItemContextMenu()
    {
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            return inventoryUI.GetItemContextMenu();
        }

        return null;
    }

    #endregion
}
