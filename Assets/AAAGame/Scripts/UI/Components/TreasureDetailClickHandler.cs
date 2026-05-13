using UnityEngine;
using UnityEngine.EventSystems;
using GameFramework;

/// <summary>
/// 宝物详情点击处理器
/// 用于处理点击宝物时显示详情信息
/// </summary>
public class TreasureDetailClickHandler : MonoBehaviour, IPointerClickHandler
{
    private int m_TreasureInstanceId;
    private RectTransform m_TargetRect;

    public void Initialize(int treasureInstanceId, RectTransform targetRect)
    {
        m_TreasureInstanceId = treasureInstanceId;
        m_TargetRect = targetRect;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        var treasureManager = PlayerAccountDataManager.Instance;
        var treasureData = treasureManager.GetTreasureInstanceById(m_TreasureInstanceId);

        if (treasureData == null)
        {
            DebugEx.Warning(nameof(TreasureDetailClickHandler), $"宝物实例未找到: {m_TreasureInstanceId}");
            return;
        }

        var treasureTable = GF.DataTable.GetDataTable<TreasureTable>();
        var treasureRow = treasureTable.GetDataRow(treasureData.TreasureId);

        if (treasureRow == null)
        {
            DebugEx.Warning(nameof(TreasureDetailClickHandler), $"宝物配置未找到: {treasureData.TreasureId}");
            return;
        }

        string detailText = FormatTreasureDetail(treasureData, treasureRow);

        var ui = GF.UI;
        if (m_TargetRect != null)
        {
            ui.ShowFloatingTipAt(detailText, m_TargetRect, new Vector2(10f, 10f));
        }
        else
        {
            ui.ShowFloatingTip(detailText, new Vector2(20f, 20f));
        }

        DebugEx.Log(nameof(TreasureDetailClickHandler), $"显示宝物详情: {treasureData.TreasureId}");
    }

    private string FormatTreasureDetail(TreasureInstanceData instanceData, TreasureTable configData)
    {
        var detail = new System.Text.StringBuilder();

        detail.AppendLine($"<b>{configData.Name}</b>");
        detail.AppendLine($"强化等级: +{instanceData.EnhanceLevel}");

        if (instanceData.EquippedChessId > 0)
        {
            var chessConfig = ChessDataManager.Instance.GetConfig(instanceData.EquippedChessId);
            string chessName = chessConfig?.Name ?? $"棋子{instanceData.EquippedChessId}";
            detail.AppendLine($"已装备: {chessName}");
        }
        else
        {
            detail.AppendLine($"位置: {(instanceData.Location == TreasureLocation.Inventory ? "背包" : "仓库")}");
        }

        if (!string.IsNullOrEmpty(configData.BaseAttributes))
        {
            detail.AppendLine($"基础属性: {configData.BaseAttributes}");
        }

        if (instanceData.Affixes != null && instanceData.Affixes.Count > 0)
        {
            detail.AppendLine("词条:");
            foreach (var affix in instanceData.Affixes)
            {
                string valueStr = affix.ValueType == ValueType.Percent ? $"{affix.ValueMin}%" : affix.ValueMin.ToString();
                detail.AppendLine($"  • {affix.Name}: {valueStr}");
            }
        }

        return detail.ToString();
    }
}
