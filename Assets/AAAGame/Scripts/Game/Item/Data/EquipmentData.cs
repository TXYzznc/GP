using System;
using System.Collections.Generic;

/// <summary>
/// 装备专有配置数据（对应 EquipmentTable）
/// </summary>
[Serializable]
public class EquipmentData
{
    public int Id;
    public int ItemTableId;
    public int SpecialEffectId;
    public Dictionary<AttributeType, float> BaseAttributes;
}
