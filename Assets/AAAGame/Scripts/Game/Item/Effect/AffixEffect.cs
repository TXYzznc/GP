using System;

/// <summary>
/// 词条效果实例
/// </summary>
[Serializable]
public class AffixEffect
{
    #region 字段

    public int AffixId; // 词条ID
    public string Name; // 词条名称
    public string Description; // 词条描述
    public AffixType AffixType; // 词条类型
    public AttributeType AttributeType; // 属性类型
    public ValueType ValueType; // 数值类型
    public float Value; // 实际数值
    #endregion

    #region 构造函数

    public AffixEffect(AffixData affixData, float value)
    {
        AffixId = affixData.Id;
        Name = affixData.Name;
        AffixType = affixData.AffixType;
        AttributeType = affixData.AttributeType;
        ValueType = affixData.ValueType;
        Value = value;
        Description = affixData.FormatDescription(value);

        DebugEx.Log("AffixEffect", $"生成词条效果: {Description}");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取格式化的描述
    /// </summary>
    public string GetFormattedDescription()
    {
        return Description;
    }

    #endregion
}
