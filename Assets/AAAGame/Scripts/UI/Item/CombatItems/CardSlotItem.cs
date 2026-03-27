using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public partial class CardSlotItem : UIItemBase
{
    #region 字段

    private int m_CardIndex;

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置卡牌数据
    /// </summary>
    public void SetData(int cardIndex)
    {
        m_CardIndex = cardIndex;
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
            varBtn.onClick.AddListener(OnCardClicked);
        }

        // TODO: 设置卡牌图标
        // TODO: 设置卡牌名称
        // TODO: 设置卡牌描述
    }

    #endregion

    #region 按钮回调

    /// <summary>
    /// 卡牌点击回调
    /// </summary>
    private void OnCardClicked()
    {
        Log.Info($"CardSlotItem: 点击了卡牌槽 {m_CardIndex}");
        // TODO: 使用卡牌
    }

    #endregion
}
