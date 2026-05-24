using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 宝物槽位交互处理器
/// 处理左键点击和鼠标悬浮时显示宝物提示框
/// </summary>
public class TreasureSlotInteractionHandler
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
{
    private int m_CurrentFloatingTipId = -1;
    private int m_TreasureId = -1;

    /// <summary>
    /// 初始化（由 CharacterBagUI 调用）
    /// </summary>
    public void Initialize(int treasureId)
    {
        m_TreasureId = treasureId;
    }

    /// <summary>
    /// 鼠标进入时显示提示框
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_TreasureId > 0)
        {
            ShowTreasureTooltip();
        }
    }

    /// <summary>
    /// 鼠标离开时隐藏提示框
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    /// <summary>
    /// 鼠标点击时显示提示框（左键）
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && m_TreasureId > 0)
        {
            ShowTreasureTooltip();
        }
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    /// <summary>
    /// 显示宝物提示框
    /// </summary>
    private void ShowTreasureTooltip()
    {
        if (m_TreasureId <= 0)
        {
            DebugEx.Warning(
                nameof(TreasureSlotInteractionHandler),
                "[ShowTreasureTooltip] 宝物ID无效"
            );
            return;
        }

        string detailText = BuildTreasureDetailText(m_TreasureId);

        if (string.IsNullOrEmpty(detailText))
        {
            DebugEx.Warning(
                nameof(TreasureSlotInteractionHandler),
                $"[ShowTreasureTooltip] 宝物详情文本为空: treasureId={m_TreasureId}"
            );
            return;
        }

        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            DebugEx.Error(
                nameof(TreasureSlotInteractionHandler),
                "[ShowTreasureTooltip] RectTransform为空"
            );
            return;
        }

        // 先隐藏旧的提示框
        HideTooltip();

        // 显示新的提示框（在格子上方，水平居中）
        m_CurrentFloatingTipId = GF.UI.ShowFloatingTipAt(
            detailText,
            rectTransform,
            new Vector2(0f, 10f)
        );

        DebugEx.Log(
            nameof(TreasureSlotInteractionHandler),
            $"[ShowTreasureTooltip] 显示宝物提示框: treasureId={m_TreasureId}, tipId={m_CurrentFloatingTipId}"
        );
    }

    /// <summary>
    /// 隐藏提示框
    /// </summary>
    private void HideTooltip()
    {
        if (m_CurrentFloatingTipId > 0)
        {
            GF.UI.CloseUIForm(m_CurrentFloatingTipId);
            m_CurrentFloatingTipId = -1;

            DebugEx.Log(nameof(TreasureSlotInteractionHandler), "[HideTooltip] 隐藏提示框");
        }
    }

    /// <summary>
    /// 构建宝物详情文本（与 InventorySlotUI 中的实现一致）
    /// </summary>
    private string BuildTreasureDetailText(int treasureId)
    {
        // 从 TreasureTable 获取宝物数据
        var dtTreasure = GF.DataTable.GetDataTable<TreasureTable>();
        var treasureRow = dtTreasure?.GetDataRow(treasureId);

        if (treasureRow == null)
        {
            DebugEx.Error(
                nameof(TreasureSlotInteractionHandler),
                $"[BuildTreasureDetailText] 未找到宝物数据: treasureId={treasureId}"
            );
            return string.Empty;
        }

        // 从 ItemTable 获取物品基础数据
        var dtItem = GF.DataTable.GetDataTable<ItemTable>();
        var itemRow = dtItem?.GetDataRow(treasureRow.Id);

        if (itemRow == null)
        {
            DebugEx.Error(
                nameof(TreasureSlotInteractionHandler),
                $"[BuildTreasureDetailText] 未找到物品数据: ItemTableId={treasureRow.Id}"
            );
            return string.Empty;
        }

        var sb = new StringBuilder();

        // 标题：宝物名称
        sb.AppendLine($"<b>{itemRow.Name}</b>");
        sb.AppendLine();

        // 品质
        if (itemRow.Rarity > 0)
        {
            string rarityText = itemRow.Rarity switch
            {
                1 => "普通",
                2 => "稀有",
                3 => "史诗",
                4 => "传奇",
                5 => "神话",
                _ => itemRow.Rarity.ToString(),
            };
            sb.AppendLine($"品质: {rarityText}");
        }

        // 重量
        if (itemRow.Weight > 0)
        {
            sb.AppendLine($"重量: {itemRow.Weight}g");
        }

        // 从 ItemManager 获取宝物详细数据
        var treasureData = ItemManager.Instance?.GetTreasureData(treasureRow.Id);
        if (treasureData != null)
        {
            // 羁绊
            if (treasureData.SynergyIds != null && treasureData.SynergyIds.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[羁绊]");
                foreach (var synergyId in treasureData.SynergyIds)
                {
                    // TODO: 从 SynergyTable 获取羁绊名称
                    sb.AppendLine($"  羁绊ID: {synergyId}");
                }
            }

            // 基础属性
            if (treasureData.BaseAttributes != null && treasureData.BaseAttributes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[基础属性]");
                foreach (var attr in treasureData.BaseAttributes)
                {
                    string attrName = GetAttributeName(attr.Key.ToString());
                    sb.AppendLine($"  {attrName}: +{attr.Value}");
                }
            }

            // 特殊效果
            if (treasureData.SpecialEffectId > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"特殊效果ID: {treasureData.SpecialEffectId}");
                // TODO: 从 SpecialEffectTable 获取效果描述
            }
        }

        // 描述
        if (!string.IsNullOrEmpty(itemRow.Description))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#808080>{itemRow.Description}</color>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取属性名称（中文）
    /// </summary>
    private string GetAttributeName(string attrKey)
    {
        return attrKey switch
        {
            "MaxHP" => "生命值",
            "Attack" => "攻击力",
            "Defense" => "防御力",
            "MagicAttack" => "魔法攻击",
            "MagicDefense" => "魔法防御",
            "Speed" => "速度",
            "CritRate" => "暴击率",
            "CritDamage" => "暴击伤害",
            "Dodge" => "闪避",
            "Hit" => "命中",
            _ => attrKey,
        };
    }
}
