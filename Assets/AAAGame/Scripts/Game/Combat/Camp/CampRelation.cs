/// <summary>
/// 阵营关系枚举
/// 描述两个实体之间的关系
/// </summary>
public enum CampRelation
{
    /// <summary>自己</summary>
    Self,
    
    /// <summary>友军（同阵营）</summary>
    Ally,
    
    /// <summary>敌人（敌对阵营）</summary>
    Enemy,
    
    /// <summary>中立（无关系）</summary>
    Neutral
}
