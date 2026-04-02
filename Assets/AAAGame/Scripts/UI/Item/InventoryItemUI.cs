using System;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.UI;

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
    }

    #endregion

    #region 事件处理

    // 点击事件处理已移至 InventorySlotUI（OnLeftClick/OnRightClick）
    // 由 InventoryClickHandler 分发处理

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取当前物品堆叠（供外部查询）
    /// </summary>
    public ItemStack GetItemStack() => m_ItemStack;

    /// <summary>
    /// 检查是否有物品
    /// </summary>
    public bool HasItem() => m_ItemStack != null && !m_ItemStack.IsEmpty;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(ItemStack itemStack)
    {
        m_ItemStack = itemStack;

        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.LogModule("InventoryItemUI", $"SetData: 清空物品显示");
            Clear();
            return;
        }

        // 刷新显示
        DebugEx.LogModule("InventoryItemUI", $"SetData: 显示物品 {itemStack.Item.Name}");
        RefreshDisplay();
    }

    /// <summary>
    /// 清空显示
    /// </summary>
    public void Clear()
    {
        m_ItemStack = null;

        // 隐藏物品数量文本
        if (varCountText != null)
        {
            varCountText.gameObject.SetActive(false);
            varCountText.text = "";
        }

        // 隐藏 InventoryItemUI 本身以完全清除显示
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

        // 激活 InventoryItemUI 以显示物品且支持拖拽
        gameObject.SetActive(true);

        var item = m_ItemStack.Item;
        var itemData = item.ItemData;

        // 确保 ItemImg 激活以显示物品
        if (varItemImg != null)
        {
            varItemImg.gameObject.SetActive(true);
        }

        // 加载物品图标
        LoadItemIconAsync(itemData.GetIconId()).Forget();

        // 显示物品数量（可堆叠且数量>1时显示，格式为 xN）
        if (varCountText != null)
        {
            bool showCount = item.MaxStackCount > 1 && m_ItemStack.Count > 1;
            varCountText.text = $"x{m_ItemStack.Count}";
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

    // 点击事件处理已移至 InventorySlotUI
    // InventorySlotUI.OnLeftClick() → 显示物品详情
    // InventorySlotUI.OnRightClick(position) → 显示上下文菜单

    #endregion
}
