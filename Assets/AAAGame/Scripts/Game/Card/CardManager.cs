using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameFramework.Event;

/// <summary>
/// 策略卡管理器
/// </summary>
public class CardManager : MonoBehaviour
{
    #region 常量

    /// <summary>每次刷新时的卡牌数量</summary>
    public const int REFRESH_CARD_COUNT = 8;

    #endregion

    #region 单例

    private static CardManager s_Instance;
    public static CardManager Instance => s_Instance;

    #endregion

    #region 字段

    private List<CardData> m_AvailableCards = new List<CardData>();
    private CardData m_CurrentSelectedCard;

    #endregion

    #region 属性

    /// <summary>当前选中的卡牌</summary>
    public CardData CurrentSelectedCard
    {
        get => m_CurrentSelectedCard;
        set
        {
            if (m_CurrentSelectedCard != value)
            {
                // 取消之前选中的卡牌
                if (m_CurrentSelectedCard != null)
                {
                    m_CurrentSelectedCard.IsSelected = false;
                }

                m_CurrentSelectedCard = value;

                if (m_CurrentSelectedCard != null)
                {
                    m_CurrentSelectedCard.IsSelected = true;
                }
            }
        }
    }

    #endregion

    #region 事件

    /// <summary>卡牌添加事件</summary>
    public event Action<CardData> OnCardAdded;

    /// <summary>卡牌移除事件</summary>
    public event Action<int> OnCardRemoved;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取所有可用卡牌
    /// </summary>
    public List<CardData> GetAvailableCards()
    {
        return m_AvailableCards;
    }

    /// <summary>
    /// 移除卡牌
    /// </summary>
    public bool RemoveCard(int cardId)
    {
        DebugEx.LogModule("CardManager", $"[RemoveCard] 尝试移除卡牌 ID={cardId}，当前卡牌总数={m_AvailableCards.Count}");

        var card = m_AvailableCards.FirstOrDefault(c => c.CardId == cardId);
        if (card != null)
        {
            DebugEx.LogModule("CardManager", $"[RemoveCard] 找到卡牌 ID={cardId}，移除前数量={m_AvailableCards.Count}");
            m_AvailableCards.Remove(card);

            // 如果移除的是当前选中的卡牌，清除选中状态
            if (CurrentSelectedCard == card)
            {
                CurrentSelectedCard = null;
            }

            DebugEx.LogModule("CardManager", $"[RemoveCard] 移除完成，移除后数量={m_AvailableCards.Count}，触发 OnCardRemoved 事件");
            OnCardRemoved?.Invoke(cardId);
            DebugEx.LogModule("CardManager", $"[RemoveCard] 卡牌 ID={cardId} 已移除");
            return true;
        }

        DebugEx.WarningModule("CardManager", $"[RemoveCard] 未找到卡牌 ID={cardId}");
        return false;
    }

    /// <summary>
    /// 检查卡牌是否存在
    /// </summary>
    public bool HasCard(int cardId)
    {
        return m_AvailableCards.Any(c => c.CardId == cardId);
    }

    /// <summary>
    /// 添加卡牌
    /// </summary>
    public void AddCard(CardData cardData)
    {
        if (cardData == null)
        {
            DebugEx.ErrorModule("CardManager", "尝试添加空卡牌数据");
            return;
        }

        m_AvailableCards.Add(cardData);
        OnCardAdded?.Invoke(cardData);
        DebugEx.LogModule("CardManager", $"添加卡牌: ID={cardData.CardId}, Name={cardData.Name}");
    }

    /// <summary>
    /// 战斗开始时初始化（从预设随机选择8张卡牌）
    /// </summary>
    public void InitializeForCombat()
    {
        Clear();

        var cardTable = GF.DataTable.GetDataTable<CardTable>();
        if (cardTable == null)
        {
            DebugEx.ErrorModule("CardManager", "CardTable 未加载");
            return;
        }

        // 从预设获取策略卡ID
        var preparedCardIds = BattleLoadoutProvider.Instance.GetPreparedStrategyCardIds();
        if (preparedCardIds == null || preparedCardIds.Count == 0)
        {
            DebugEx.WarningModule("CardManager", "预设中没有策略卡");
            return;
        }

        // 随机选择 REFRESH_CARD_COUNT 张卡（可重复）
        var random = new System.Random();
        for (int i = 0; i < REFRESH_CARD_COUNT; i++)
        {
            int randomIndex = random.Next(0, preparedCardIds.Count);
            int cardId = preparedCardIds[randomIndex];

            var row = cardTable.GetDataRow(cardId);
            if (row != null)
            {
                var cardData = new CardData(cardId, row);
                AddCard(cardData);
            }
            else
            {
                DebugEx.WarningModule("CardManager", $"卡牌ID {cardId} 不存在于 CardTable");
            }
        }

        DebugEx.LogModule("CardManager", $"战斗初始化完成，加载了 {m_AvailableCards.Count} 张卡牌");
    }

    /// <summary>
    /// 战斗结束时清理
    /// </summary>
    public void Clear()
    {
        m_AvailableCards.Clear();
        CurrentSelectedCard = null;
        DebugEx.LogModule("CardManager", "清理卡牌数据");
    }

    /// <summary>
    /// 刷新卡牌（从预设中随机选择 8 张，可重复）
    /// </summary>
    public void RefreshCards()
    {
        m_AvailableCards.Clear();
        CurrentSelectedCard = null;

        var cardTable = GF.DataTable.GetDataTable<CardTable>();
        if (cardTable == null)
        {
            DebugEx.ErrorModule("CardManager", "CardTable 未加载，刷新失败");
            return;
        }

        // 从预设获取策略卡ID
        var preparedCardIds = BattleLoadoutProvider.Instance.GetPreparedStrategyCardIds();
        if (preparedCardIds == null || preparedCardIds.Count == 0)
        {
            DebugEx.WarningModule("CardManager", "预设中没有策略卡");
            return;
        }

        // 随机选择 8 张卡（可重复）
        var random = new System.Random();
        for (int i = 0; i < REFRESH_CARD_COUNT; i++)
        {
            int randomIndex = random.Next(0, preparedCardIds.Count);
            int cardId = preparedCardIds[randomIndex];

            var row = cardTable.GetDataRow(cardId);
            if (row != null)
            {
                var cardData = new CardData(cardId, row);
                AddCard(cardData);
            }
            else
            {
                DebugEx.WarningModule("CardManager", $"卡牌ID {cardId} 不存在于 CardTable");
            }
        }

        DebugEx.LogModule("CardManager", $"卡牌已刷新，重新加载了 {m_AvailableCards.Count} 张卡牌");
    }

    #endregion
}
