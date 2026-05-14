using System.Collections.Generic;

/// <summary>
/// SynergyTable 扩展类 - 提供辅助方法
/// </summary>
public partial class SynergyTable
{
    /// <summary>
    /// 获取羁绊类型（由 IsTreasureSynergy 字段驱动）
    /// </summary>
    public SynergyType GetSynergyType()
    {
        return IsTreasureSynergy == 1 ? SynergyType.Treasure : SynergyType.Chess;
    }
}
