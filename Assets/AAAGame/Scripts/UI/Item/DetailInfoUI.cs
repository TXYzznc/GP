using System;
using UnityEngine;
using UnityEngine.UI;
using GameExtension;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
using DG.Tweening;

public partial class DetailInfoUI : UIItemBase
{
    #region 常量

    private const float SLIDE_IN_DURATION = 0.4f;
    private const float INITIAL_X = 360f;
    private const float TARGET_X = 0f;

    #endregion

    #region 字段

    private CardData m_CardData;
    private ChessEntity m_ChessEntity;
    private RectTransform m_RectTransform;
    private Tween m_SlideInTween;
    private System.Collections.Generic.Dictionary<int, BuffItem> m_BuffItems = new System.Collections.Generic.Dictionary<int, BuffItem>();
    private int m_CurrentMode = 0; // 0=卡牌, 1=棋子

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();
        if (m_RectTransform == null)
        {
            DebugEx.ErrorModule("DetailInfoUI", "未找到 RectTransform 组件");
        }
    }

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置卡牌数据
    /// </summary>
    public void SetData(CardData cardData)
    {
        m_CardData = cardData;
        m_ChessEntity = null;
        m_CurrentMode = 0;
        DebugEx.LogModule("DetailInfoUI", $"设置卡牌数据: {cardData?.Name ?? "null"}");
    }

    /// <summary>
    /// 设置棋子数据
    /// </summary>
    public void SetChessUnitData(ChessEntity chessEntity)
    {
        m_ChessEntity = chessEntity;
        m_CardData = null;
        m_CurrentMode = 1;
        DebugEx.LogModule("DetailInfoUI", $"设置棋子数据: {chessEntity?.Config?.Name ?? "null"}");
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新UI显示（自动判断模式）
    /// </summary>
    public void RefreshUI()
    {
        if (m_CurrentMode == 0)
        {
            RefreshCardUI();
        }
        else if (m_CurrentMode == 1)
        {
            RefreshChessUnitUI();
        }
    }

    /// <summary>
    /// 刷新卡牌UI显示
    /// </summary>
    private void RefreshCardUI()
    {
        if (m_CardData == null)
        {
            DebugEx.WarningModule("DetailInfoUI", "卡牌数据为空，无法刷新UI");
            return;
        }

        // 隐藏Buff显示面板
        if (varBuffBg != null)
        {
            varBuffBg.gameObject.SetActive(false);
        }

        // 显示卡牌名称
        if (varTitleText != null)
        {
            varTitleText.text = m_CardData.Name;
        }

        // 显示卡牌描述
        if (varDescText != null)
        {
            varDescText.text = m_CardData.Desc;
        }

        // 显示其他信息（灵力消耗、目标类型等）
        if (varOtherText != null)
        {
            string otherInfo = $"灵力消耗: {m_CardData.SpiritCost}\n范围: {m_CardData.AreaRadius}";
            varOtherText.text = otherInfo;
        }

        DebugEx.LogModule("DetailInfoUI", $"卡牌UI已刷新: {m_CardData.Name}");
    }

    /// <summary>
    /// 刷新棋子UI显示
    /// </summary>
    private void RefreshChessUnitUI()
    {
        if (m_ChessEntity == null || m_ChessEntity.Config == null)
        {
            DebugEx.WarningModule("DetailInfoUI", "棋子数据为空，无法刷新UI");
            return;
        }

        // 显示Buff显示面板
        if (varBuffBg != null)
        {
            varBuffBg.gameObject.SetActive(true);
        }

        var config = m_ChessEntity.Config;

        // 显示棋子名称 + 星级
        if (varTitleText != null)
        {
            string starText = new string('★', config.StarLevel);
            varTitleText.text = $"{config.Name} {starText}";
        }

        // 显示棋子描述
        if (varDescText != null)
        {
            varDescText.text = config.Description;
        }

        // OtherText暂时不显示（可设置为空或隐藏）
        if (varOtherText != null)
        {
            varOtherText.text = "";
        }

        // 刷新Buff显示
        RefreshAllBuffs();

        DebugEx.LogModule("DetailInfoUI", $"棋子UI已刷新: {config.Name}");
    }

    #endregion

    #region Buff管理

    /// <summary>
    /// 刷新所有Buff显示
    /// </summary>
    private void RefreshAllBuffs()
    {
        ClearAllBuffItems();

        if (m_ChessEntity == null || m_ChessEntity.BuffManager == null) return;

        var allBuffs = m_ChessEntity.BuffManager.GetAllBuffs();
        foreach (var buff in allBuffs)
        {
            AddBuffItem(buff.BuffId, buff.StackCount);
        }
    }

    /// <summary>
    /// 添加单个BuffItem
    /// </summary>
    private void AddBuffItem(int buffId, int stackCount)
    {
        if (varBuffBg == null || varBuffItem == null) return;

        // 检查是否已存在
        if (m_BuffItems.ContainsKey(buffId))
        {
            return;
        }

        // 实例化BuffItem
        GameObject buffItemGo = Instantiate(varBuffItem, varBuffBg.transform, false);
        BuffItem buffItem = buffItemGo.GetComponent<BuffItem>();

        if (buffItem != null)
        {
            buffItem.SetData(buffId);
            buffItem.SetStackCount(stackCount);
            m_BuffItems[buffId] = buffItem;
            buffItemGo.SetActive(true);
        }
    }

    /// <summary>
    /// 清除所有BuffItem
    /// </summary>
    private void ClearAllBuffItems()
    {
        foreach (var buffItem in m_BuffItems.Values)
        {
            if (buffItem != null && buffItem.gameObject != null)
            {
                Destroy(buffItem.gameObject);
            }
        }
        m_BuffItems.Clear();
    }

    #endregion

    #region 动画

    /// <summary>
    /// 显示 DetailInfoUI 并播放滑入动画
    /// </summary>
    public void ShowWithAnimation()
    {
        if (m_RectTransform == null)
        {
            DebugEx.ErrorModule("DetailInfoUI", "RectTransform 为空，无法播放动画");
            gameObject.SetActive(true);
            return;
        }

        // 杀死之前的动画
        m_SlideInTween?.Kill();

        // 设置初始位置（x = 360）
        var anchoredPos = m_RectTransform.anchoredPosition;
        anchoredPos.x = INITIAL_X;
        m_RectTransform.anchoredPosition = anchoredPos;

        // 激活 GameObject
        gameObject.SetActive(true);

        // 播放滑入动画（x: 360 → 0）
        m_SlideInTween = m_RectTransform.DOAnchorPosX(TARGET_X, SLIDE_IN_DURATION)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                DebugEx.LogModule("DetailInfoUI", "滑入动画完成");
            });

        DebugEx.LogModule("DetailInfoUI", "开始播放滑入动画");
    }

    #endregion
}

