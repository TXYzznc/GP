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

        // 绑定按钮事件
        if (varItemBtn != null)
        {
            varItemBtn.onClick.AddListener(OnItemClick);
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

        // ✅ 不隐藏GameObject，只清空显示内容
        // 这样可以保持UI结构完整，避免频繁创建销毁
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
        LoadItemIcon(itemData.GetIconId());

        // TODO: 显示物品数量（如果可堆叠且数量>1）
        // 可以在UI上添加一个Text组件来显示数量

        DebugEx.Log("InventoryItemUI", $"刷新物品显示: {item.Name}, 数量:{m_ItemStack.Count}");
    }

    /// <summary>
    /// 加载物品图标
    /// </summary>
    private async void LoadItemIcon(int iconId)
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

        // TODO: 显示物品详情面板或使用物品
        // 可以根据物品类型执行不同操作:
        // - 消耗品: 使用
        // - 装备: 装备/卸下
        // - 宝物: 查看详情
        // - 任务道具: 查看描述

        ShowItemTooltip();
    }

    /// <summary>
    /// 显示物品提示
    /// </summary>
    private void ShowItemTooltip()
    {
        if (m_ItemStack == null || m_ItemStack.IsEmpty)
        {
            return;
        }

        var item = m_ItemStack.Item;
        var detailInfo = item.GetDetailInfo();

        DebugEx.Log("InventoryItemUI", $"物品信息:\n{detailInfo}");

        // TODO: 显示物品提示面板
        // 可以创建一个Tooltip UI来显示物品详细信息
    }

    #endregion
}
