using System;
using System.Collections.Generic;

/// <summary>
/// 宝物专有配置数据（对应 TreasureTable）
/// </summary>
[Serializable]
public class TreasureData
{
    public int Id;
    public int ItemTableId;
    public int SpecialEffectId;
    public List<int> SynergyIds;
    public Dictionary<AttributeType, float> BaseAttributes;
}
