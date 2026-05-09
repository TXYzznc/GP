using System;

/// <summary>
/// 消耗品专有配置数据（对应 ConsumableTable）
/// </summary>
[Serializable]
public class ConsumableData
{
    public int Id;
    public int ItemTableId;
    public bool CanUse;
    public int UseEffectId;
}
