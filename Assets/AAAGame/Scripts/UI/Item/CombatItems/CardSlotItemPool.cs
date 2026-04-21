using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 卡牌槽对象池（避免频繁销毁，提高性能）
/// 自动初始化，无需手动配置
/// </summary>
public class CardSlotItemPool : MonoBehaviour
{
    #region 常量

    private const string CARD_SLOT_ITEM_PREFAB_PATH = "Assets/AAAGame/Prefabs/UI/Items/CardSlotItem.prefab";
    private const int INITIAL_POOL_SIZE = 16;

    #endregion

    #region 单例

    private static CardSlotItemPool s_Instance;
    public static CardSlotItemPool Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType<CardSlotItemPool>();
                if (s_Instance == null)
                {
                    var poolObj = new GameObject("CardSlotItemPool");
                    s_Instance = poolObj.AddComponent<CardSlotItemPool>();
                }
            }
            return s_Instance;
        }
    }

    #endregion

    #region 字段

    private CardSlotItem m_CardSlotItemPrefab;
    private Transform m_PoolContainer;
    private Stack<CardSlotItem> m_AvailableCards = new Stack<CardSlotItem>();
    private HashSet<CardSlotItem> m_ActiveCards = new HashSet<CardSlotItem>();
    private bool m_Initialized = false;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }

    #endregion

    #region 初始化

    private void EnsureInitialized()
    {
        if (m_Initialized)
            return;

        m_Initialized = true;

        // 从资源文件夹加载预制体
        LoadCardSlotItemPrefab();

        if (m_CardSlotItemPrefab == null)
        {
            DebugEx.ErrorModule("CardSlotItemPool", $"无法加载 CardSlotItem 预制体: {CARD_SLOT_ITEM_PREFAB_PATH}");
            return;
        }

        // 创建容器
        if (m_PoolContainer == null)
        {
            m_PoolContainer = new GameObject("CardSlotItemPool_Container").transform;
            m_PoolContainer.SetParent(transform);
        }

        // 预生成池内对象
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            CreateNewCard();
        }

        DebugEx.LogModule("CardSlotItemPool", $"对象池初始化完成，初始大小={INITIAL_POOL_SIZE}");
    }

    private void LoadCardSlotItemPrefab()
    {
        if (m_CardSlotItemPrefab != null)
            return;

        // 尝试从资源文件夹加载
#if UNITY_EDITOR
        m_CardSlotItemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<CardSlotItem>(CARD_SLOT_ITEM_PREFAB_PATH);
        if (m_CardSlotItemPrefab != null)
        {
            DebugEx.LogModule("CardSlotItemPool", $"从资源文件夹加载 CardSlotItem 预制体");
            return;
        }
#endif

        // 运行时从 Resources 加载（需要将预制体放在 Resources 文件夹）
        m_CardSlotItemPrefab = Resources.Load<CardSlotItem>("Prefabs/UI/Items/CardSlotItem");
        if (m_CardSlotItemPrefab != null)
        {
            DebugEx.LogModule("CardSlotItemPool", $"从 Resources 加载 CardSlotItem 预制体");
            return;
        }

        DebugEx.ErrorModule("CardSlotItemPool", $"无法加载 CardSlotItem 预制体");
    }

    #endregion

    #region 对象池操作

    /// <summary>
    /// 从池中获取一张卡牌
    /// </summary>
    public CardSlotItem GetCard()
    {
        EnsureInitialized();

        CardSlotItem card;

        if (m_AvailableCards.Count > 0)
        {
            card = m_AvailableCards.Pop();
        }
        else
        {
            // 池中没有可用对象，创建新的
            card = CreateNewCard();
        }

        if (card != null)
        {
            // 确保卡牌状态完全干净
            card.ResetState();
            card.gameObject.SetActive(true);
            m_ActiveCards.Add(card);

            DebugEx.LogModule("CardSlotItemPool", $"从池中获取卡牌，可用池大小={m_AvailableCards.Count}，活跃卡数={m_ActiveCards.Count}");
        }

        return card;
    }

    /// <summary>
    /// 将卡牌归还到池中
    /// </summary>
    public void ReturnCard(CardSlotItem card)
    {
        if (card == null)
        {
            DebugEx.WarningModule("CardSlotItemPool", "尝试归还空卡牌");
            return;
        }

        if (!m_ActiveCards.Contains(card))
        {
            DebugEx.WarningModule("CardSlotItemPool", $"卡牌不在活跃列表中: {card.name}");
            return;
        }

        m_ActiveCards.Remove(card);

        // 重置卡牌状态
        card.ResetState();
        card.gameObject.SetActive(false);

        m_AvailableCards.Push(card);

        DebugEx.LogModule("CardSlotItemPool", $"将卡牌归还到池，可用池大小={m_AvailableCards.Count}，活跃卡数={m_ActiveCards.Count}");
    }

    /// <summary>
    /// 创建一张新卡牌（内部方法）
    /// </summary>
    private CardSlotItem CreateNewCard()
    {
        if (m_CardSlotItemPrefab == null)
        {
            DebugEx.ErrorModule("CardSlotItemPool", "预制体为空，无法创建卡牌");
            return null;
        }

        var card = Instantiate(m_CardSlotItemPrefab, m_PoolContainer);
        card.gameObject.SetActive(false);
        card.gameObject.name = $"CardSlotItem_Pool_{m_AvailableCards.Count + m_ActiveCards.Count}";

        DebugEx.LogModule("CardSlotItemPool", $"创建新卡牌，当前池大小={m_AvailableCards.Count + m_ActiveCards.Count}");
        return card;
    }

    #endregion

    #region 调试

    public void DebugPoolState()
    {
        DebugEx.LogModule("CardSlotItemPool", $"对象池状态 - 可用: {m_AvailableCards.Count}, 活跃: {m_ActiveCards.Count}, 总数: {m_AvailableCards.Count + m_ActiveCards.Count}");
    }

    #endregion
}
