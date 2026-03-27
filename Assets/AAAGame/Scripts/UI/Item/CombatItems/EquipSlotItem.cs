using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public partial class EquipSlotItem : UIItemBase
{
    #region 字段

    private int m_EquipIndex;

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置装备数据
    /// </summary>
    public void SetData(int equipIndex)
    {
        m_EquipIndex = equipIndex;
        RefreshUI();
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新UI
    /// </summary>
    private void RefreshUI()
    {
        // 绑定按钮事件
        if (varBtn != null)
        {
            varBtn.onClick.RemoveAllListeners();
            varBtn.onClick.AddListener(OnEquipClicked);
        }

        // TODO: 设置装备图标
        // TODO: 设置装备名称
    }

    #endregion

    #region 按钮回调

    /// <summary>
    /// 装备点击回调
    /// </summary>
    private void OnEquipClicked()
    {
        Log.Info($"EquipSlotItem: 点击了装备槽 {m_EquipIndex}");
        // TODO: 显示装备详情
    }

    #endregion
}
