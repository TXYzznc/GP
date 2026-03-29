using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包格子UI组件
/// 显示单个物品的图标、稀有度背景、耐久度条等信息
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    #region 字段

    [SerializeField]
    private Image m_ItemIcon;

    [SerializeField]
    private Image m_RarityBackground;

    [SerializeField]
    private Image m_DurabilityBar;

    [SerializeField]
    private Text m_CountText;

    [SerializeField]
    private Button m_SlotButton;

    /// <summary>当前格子数据</summary>
    private InventorySlot m_CurrentSlot;

    /// <summary>格子索引</summary>
    private int m_SlotIndex = -1;

    /// <summary>点击事件</summary>
    public event System.Action<InventorySlotUI> OnSlotClicked;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        if (m_SlotButton != null)
        {
            m_SlotButton.onClick.AddListener(OnSlotButtonClicked);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置格子数据
    /// </summary>
    public void SetData(InventorySlot slot)
    {
        m_CurrentSlot = slot;
        m_SlotIndex = slot?.SlotIndex ?? -1;

        if (slot == null || slot.IsEmpty)
        {
            Clear();
            return;
        }

        // 获取物品堆叠和物品
        var itemStack = slot.ItemStack;
        if (itemStack == null || itemStack.IsEmpty)
        {
            Clear();
            return;
        }

        var item = itemStack.Item;
        if (item == null)
        {
            DebugEx.Warning("InventorySlotUI", $"物品为空: 格子 {m_SlotIndex}");
            Clear();
            return;
        }

        // 获取物品配置数据
        var itemData = item.ItemData;
        if (itemData == null)
        {
            DebugEx.Warning("InventorySlotUI", $"物品配置不存在: {item.ItemId}");
            Clear();
            return;
        }

        // 加载物品图标
        LoadItemIcon(itemData.GetIconId());

        // 设置品质背景色
        SetQualityBackground(itemData.Quality);

        // 隐藏耐久度条（暂未实现）
        if (m_DurabilityBar != null)
            m_DurabilityBar.gameObject.SetActive(false);

        // 设置数量文本
        if (item.MaxStackCount > 1 && itemStack.Count > 1)
        {
            if (m_CountText != null)
            {
                m_CountText.text = itemStack.Count.ToString();
                m_CountText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (m_CountText != null)
                m_CountText.gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 清空格子
    /// </summary>
    public void Clear()
    {
        m_CurrentSlot = null;
        m_SlotIndex = -1;

        if (m_ItemIcon != null)
        {
            m_ItemIcon.sprite = null;
            m_ItemIcon.gameObject.SetActive(false);
        }

        if (m_RarityBackground != null)
            m_RarityBackground.gameObject.SetActive(false);

        if (m_DurabilityBar != null)
            m_DurabilityBar.gameObject.SetActive(false);

        if (m_CountText != null)
            m_CountText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 获取当前格子数据
    /// </summary>
    public InventorySlot GetCurrentSlot()
    {
        return m_CurrentSlot;
    }

    /// <summary>
    /// 获取格子索引
    /// </summary>
    public int GetSlotIndex()
    {
        return m_SlotIndex;
    }

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty()
    {
        return m_CurrentSlot == null || m_CurrentSlot.IsEmpty;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 加载物品图标
    /// </summary>
    private void LoadItemIcon(int itemId)
    {
        if (m_ItemIcon == null)
            return;

        try
        {
            // 使用 fire-and-forget 方式加载图标
            _ = GameExtension.ResourceExtension.LoadSpriteAsync(itemId, m_ItemIcon);
            m_ItemIcon.gameObject.SetActive(true);
        }
        catch (System.Exception e)
        {
            DebugEx.Error("InventorySlotUI", $"加载物品图标失败: {e.Message}");
        }
    }

    /// <summary>
    /// 设置品质背景色
    /// </summary>
    private void SetQualityBackground(ItemQuality quality)
    {
        if (m_RarityBackground == null)
            return;

        // 根据品质设置背景色
        Color qualityColor = GetQualityColor(quality);
        m_RarityBackground.color = qualityColor;
        m_RarityBackground.gameObject.SetActive(true);
    }

    /// <summary>
    /// 根据品质获取颜色
    /// </summary>
    private Color GetQualityColor(ItemQuality quality)
    {
        return quality switch
        {
            ItemQuality.Common => new Color(0.8f, 0.8f, 0.8f, 1f), // 普通 - 灰色
            ItemQuality.Uncommon => new Color(0.2f, 0.8f, 0.2f, 1f), // 稀有 - 绿色
            ItemQuality.Rare => new Color(0.2f, 0.5f, 1f, 1f), // 精良 - 蓝色
            ItemQuality.Epic => new Color(0.64f, 0.21f, 0.93f, 1f), // 史诗 - 紫色
            ItemQuality.Legendary => new Color(1f, 0.5f, 0f, 1f), // 传说 - 橙色
            _ => new Color(1f, 1f, 1f, 1f), // 默认 - 白色
        };
    }

    /// <summary>
    /// 格子按钮点击
    /// </summary>
    private void OnSlotButtonClicked()
    {
        DebugEx.Log("InventorySlotUI", $"格子 {m_SlotIndex} 被点击");
        OnSlotClicked?.Invoke(this);
    }

    #endregion
}
