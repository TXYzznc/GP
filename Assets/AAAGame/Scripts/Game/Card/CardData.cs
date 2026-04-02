using UnityEngine;

/// <summary>
/// 策略卡数据类
/// </summary>
public class CardData
{
    #region 字段

    /// <summary>卡牌ID</summary>
    public int CardId { get; private set; }

    /// <summary>配置表行引用</summary>
    public CardTable TableRow { get; private set; }

    /// <summary>是否已使用</summary>
    public bool IsUsed { get; set; }

    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }

    #endregion

    #region 构造函数

    public CardData(int cardId, CardTable tableRow)
    {
        CardId = cardId;
        TableRow = tableRow;
        IsUsed = false;
        IsSelected = false;
    }

    #endregion

    #region 便捷属性

    public string Name => TableRow?.Name ?? "";
    public string Desc => TableRow?.Desc ?? "";
    public int IconId => TableRow?.IconId ?? 0;
    public float SpiritCost => TableRow?.SpiritCost ?? 0;
    public int TargetType => TableRow?.TargetType ?? 0;
    public float AreaRadius => TableRow?.AreaRadius ?? 0;

    #endregion
}
