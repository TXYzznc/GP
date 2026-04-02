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
        var card = m_AvailableCards.FirstOrDefault(c => c.CardId == cardId);
        if (card != null)
        {
            m_AvailableCards.Remove(card);

            // 如果移除的是当前选中的卡牌，清除选中状态
            if (CurrentSelectedCard == card)
            {
                CurrentSelectedCard = null;
            }

            OnCardRemoved?.Invoke(cardId);
            DebugEx.LogModule("CardManager", $"移除卡牌: ID={cardId}");
            return true;
        }

        DebugEx.WarningModule("CardManager", $"尝试移除不存在的卡牌: ID={cardId}");
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
    /// 战斗开始时初始化（随机加载 8 张卡）
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

        // 获取所有卡牌ID（1001-1012）
        var allCardIds = new List<int>();
        for (int id = 1001; id <= 1012; id++)
        {
            var row = cardTable.GetDataRow(id);
            if (row != null)
            {
                allCardIds.Add(id);
            }
        }

        // 随机选择 8 张卡
        var random = new System.Random();
        var selectedIds = allCardIds.OrderBy(x => random.Next()).Take(8).ToList();

        foreach (var cardId in selectedIds)
        {
            var row = cardTable.GetDataRow(cardId);
            if (row != null)
            {
                var cardData = new CardData(cardId, row);
                AddCard(cardData);
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

    #endregion
}
