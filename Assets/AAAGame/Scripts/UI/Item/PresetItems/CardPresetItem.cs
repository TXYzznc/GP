using System;
using GameFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 出战预设界面 - 策略卡项（精简版，仅用于预设界面）
/// 相比 CardSlotItem，移除了拖拽、选中动画、销毁动画等战斗相关功能
/// </summary>
public partial class CardPresetItem : UIItemBase
{
    #region 字段

    private CardData m_CardData;
    private Action<int> m_OnClickCallback;

    #endregion

    #region 生命周期

    protected override void OnInit()
    {
        base.OnInit();

        if (varBtn != null)
        {
            varBtn.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnDestroy()
    {
        if (varBtn != null)
        {
            varBtn.onClick.RemoveListener(OnButtonClick);
        }
    }

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置卡牌数据
    /// </summary>
    public void SetData(CardData cardData, Action<int> onClickCallback = null)
    {
        m_CardData = cardData;
        m_OnClickCallback = onClickCallback;

        RefreshUI();

        DebugEx.Log("CardPresetItem", $"SetData: cardId={cardData.CardId}, name={cardData.Name}");
    }

    /// <summary>
    /// 刷新UI显示
    /// </summary>
    private void RefreshUI()
    {
        if (m_CardData == null)
            return;

        // 设置卡牌名称
        if (varNameText != null)
        {
            varNameText.text = m_CardData.Name;
        }

        // 设置卡牌描述
        if (varDecsText != null)
        {
            varDecsText.text = m_CardData.Desc;
        }

        // 设置故事文本
        if (varStoryText != null)
        {
            varStoryText.text = m_CardData.StoryText;
        }

        // 设置灵力消耗
        if (varCost != null)
        {
            varCost.text = m_CardData.SpiritCost.ToString("F0");
        }

        // 加载卡牌图标
        LoadCardImageAsync(m_CardData.IconId);
    }

    /// <summary>
    /// 异步加载卡牌图标
    /// </summary>
    private void LoadCardImageAsync(int iconId)
    {
        if (varCardImg == null)
            return;

        _ = GameExtension.ResourceExtension.LoadSpriteAsync(iconId, varCardImg, 1f);
    }

    #endregion

    #region 按钮事件

    private void OnButtonClick()
    {
        if (m_CardData == null)
            return;

        m_OnClickCallback?.Invoke(m_CardData.CardId);
        DebugEx.Log("CardPresetItem", $"OnButtonClick: cardId={m_CardData.CardId}");
    }

    #endregion
}
