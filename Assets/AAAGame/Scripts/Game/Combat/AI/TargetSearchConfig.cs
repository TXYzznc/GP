using UnityEngine;

/// <summary>
/// 索敌配置
/// 定义索敌的距离范围和评分权重
/// 可从配置表读取或在 Inspector 中配置
/// </summary>
[System.Serializable]
public class TargetSearchConfig
{
    #region 距离配置

    [Header("距离配置")]
    [Tooltip("索敌距离（0表示无限制，可以搜索全场）")]
    public float SearchRange = 0f;

    #endregion

    #region 评分权重

    [Header("评分权重")]
    [Tooltip("距离权重（0-1）- 距离越近分数越高")]
    [Range(0f, 1f)]
    public float DistanceWeight = 0.3f;

    [Tooltip("血量权重（0-1）- 血量越低分数越高")]
    [Range(0f, 1f)]
    public float HpWeight = 0.5f;

    [Tooltip("威胁度权重（0-1）- 攻击力越高分数越高")]
    [Range(0f, 1f)]
    public float ThreatWeight = 0.2f;

    #endregion

    #region 优先级策略

    [Header("优先级策略")]
    [Tooltip("是否优先攻击残血目标")]
    public bool PrioritizeLowHp = true;

    [Tooltip("是否优先攻击近距离目标")]
    public bool PrioritizeNearby = true;

    [Tooltip("是否优先攻击高威胁目标")]
    public bool PrioritizeHighThreat = false;

    #endregion

    #region 默认配置

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static TargetSearchConfig CreateDefault()
    {
        return new TargetSearchConfig
        {
            SearchRange = 0f,
            DistanceWeight = 0.3f,
            HpWeight = 0.5f,
            ThreatWeight = 0.2f,
            PrioritizeLowHp = true,
            PrioritizeNearby = true,
            PrioritizeHighThreat = false,
        };
    }

    /// <summary>
    /// 创建近战配置（优先近距离目标）
    /// </summary>
    public static TargetSearchConfig CreateMeleeConfig()
    {
        return new TargetSearchConfig
        {
            SearchRange = 0f,
            DistanceWeight = 0.5f,
            HpWeight = 0.3f,
            ThreatWeight = 0.2f,
            PrioritizeLowHp = true,
            PrioritizeNearby = true,
            PrioritizeHighThreat = false,
        };
    }

    /// <summary>
    /// 创建远程配置（优先残血目标）
    /// </summary>
    public static TargetSearchConfig CreateRangedConfig()
    {
        return new TargetSearchConfig
        {
            SearchRange = 0f,
            DistanceWeight = 0.2f,
            HpWeight = 0.6f,
            ThreatWeight = 0.2f,
            PrioritizeLowHp = true,
            PrioritizeNearby = false,
            PrioritizeHighThreat = false,
        };
    }

    #endregion

    #region 调试

    /// <summary>
    /// 获取配置描述
    /// </summary>
    public override string ToString()
    {
        return $"SearchRange={SearchRange:F1}, Weights(Dist={DistanceWeight:F2}, HP={HpWeight:F2}, Threat={ThreatWeight:F2})";
    }

    #endregion
}
