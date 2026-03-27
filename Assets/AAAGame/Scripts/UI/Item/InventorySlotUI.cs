using System;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.UI;

public partial class InventorySlotUI : UIItemBase
{
    #region 字段

    private InventorySlot m_SlotData; // 格子数据
    private InventoryItemUI m_ItemUI; // 物品UI实例
    #endregion

    #region 初始化

    protected override void OnInit()
    {
        base.OnInit();

        DebugEx.Log("InventorySlotUI", "格子UI初始化");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(InventorySlot slotData)
    {
        m_SlotData = slotData;

        if (slotData == null || slotData.IsEmpty)
        {
            Clear();
            return;
        }

        // 创建或更新物品UI
        if (m_ItemUI == null)
        {
            CreateItemUI();
        }

        if (m_ItemUI != null)
        {
            m_ItemUI.SetData(slotData.ItemStack);
        }
    }

    /// <summary>
    /// 清空显示
    /// </summary>
    public void Clear()
    {
        m_SlotData = null;

        if (m_ItemUI != null)
        {
            Destroy(m_ItemUI.gameObject);
            m_ItemUI = null;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 创建物品UI
    /// </summary>
    private void CreateItemUI()
    {
        if (varInventoryItemUI == null)
        {
            DebugEx.Error("InventorySlotUI", "物品UI预制体未设置");
            return;
        }

        var itemGO = Instantiate(varInventoryItemUI, varBg.transform);
        itemGO.SetActive(true);

        m_ItemUI = itemGO.GetComponent<InventoryItemUI>();
        if (m_ItemUI == null)
        {
            DebugEx.Error("InventorySlotUI", "物品UI组件未找到");
            Destroy(itemGO);
            return;
        }

        DebugEx.Log("InventorySlotUI", "创建物品UI成功");
    }

    #endregion
}
