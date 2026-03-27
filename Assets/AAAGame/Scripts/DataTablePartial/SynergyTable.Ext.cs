using System.Collections.Generic;

/// <summary>
/// SynergyTable 扩展类 - 提供辅助方法
/// </summary>
public partial class SynergyTable
{
    #region 辅助属性
    
    /// <summary>
    /// 获取需求ID列表（转换为List）
    /// </summary>
    public List<int> GetRequireIdList()
    {
        if (RequireIds == null || RequireIds.Length == 0)
        {
            return new List<int>();
        }
        return new List<int>(RequireIds);
    }
    
    #endregion
}
