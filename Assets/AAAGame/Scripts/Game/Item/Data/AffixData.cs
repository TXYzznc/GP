using System;
using UnityEngine;

/// <summary>
/// 词条配置数据
/// </summary>
[Serializable]
public class AffixData
{
    public int Id; // 词条ID
    public string Name; // 词条名称
    public string Description; // 词条描述
    public AffixType AffixType; // 词条类型
    public AttributeType AttributeType; // 属性类型
    public ValueType ValueType; // 数值类型
    public float ValueMin; // 最小值
    public float ValueMax; // 最大值
    public int Weight; // 权重

    /// <summary>
    /// 生成随机数值
    /// </summary>
    public float GenerateRandomValue()
    {
        float value = UnityEngine.Random.Range(ValueMin, ValueMax);

        // 如果是百分比类型，保留1位小数
        if (ValueType == ValueType.Percent)
        {
            value = Mathf.Round(value * 10f) / 10f;
        }
        else
        {
            // 固定值取整
            value = Mathf.Round(value);
        }

        return value;
    }

    /// <summary>
    /// 格式化描述文本
    /// </summary>
    public string FormatDescription(float value)
    {
        string valueStr = ValueType == ValueType.Percent ? $"{value}%" : value.ToString();
        return Description.Replace("{0}", valueStr);
    }
}
