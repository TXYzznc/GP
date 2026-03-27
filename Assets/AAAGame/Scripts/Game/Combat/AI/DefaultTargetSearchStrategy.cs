using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 默认索敌策略
/// 基于距离、血量、威胁度的综合评分
/// 评分规则：
/// 1. 距离因素：距离越近分数越高（可配置权重）
/// 2. 血量因素：血量越低分数越高（可配置权重）
/// 3. 威胁度因素：攻击力越高分数越高（可配置权重）
/// </summary>
public class DefaultTargetSearchStrategy : ITargetSearchStrategy
{
    #region 私有字段

    /// <summary>索敌配置</summary>
    private TargetSearchConfig m_Config;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建默认索敌策略
    /// </summary>
    /// <param name="config">索敌配置，如果为null则使用默认配置</param>
    public DefaultTargetSearchStrategy(TargetSearchConfig config = null)
    {
        m_Config = config ?? TargetSearchConfig.CreateDefault();
    }

    #endregion

    #region 接口实现

    /// <summary>
    /// 从敌人列表中选择最优目标
    /// </summary>
    public ChessEntity SelectBestTarget(ChessEntity self, List<EnemyInfoCache> enemies)
    {
        if (self == null || enemies == null || enemies.Count == 0)
        {
            return null;
        }

        Vector3 myPosition = self.transform.position;
        ChessEntity bestTarget = null;
        EnemyInfoCache bestCache = null;
        float bestScore = float.MinValue;

        DebugEx.Log(
            "DefaultTargetSearchStrategy",
            $"{self.Config?.Name} 开始索敌，候选目标数量: {enemies.Count}"
        );

        foreach (var enemyCache in enemies)
        {
            // 跳过已死亡的
            if (!enemyCache.IsAlive)
            {
                continue;
            }

            // 计算距离
            float distance = Vector3.Distance(myPosition, enemyCache.CurrentPosition);

            // 检查索敌距离限制
            if (m_Config.SearchRange > 0 && distance > m_Config.SearchRange)
            {
                DebugEx.Log(
                    "DefaultTargetSearchStrategy",
                    $"  - {enemyCache.Entity?.Config?.Name ?? "召唤师"} 超出索敌范围 ({distance:F2} > {m_Config.SearchRange:F2})"
                );
                continue;
            }

            // 计算综合评分
            float score = EvaluateTarget(self, enemyCache, distance);

            DebugEx.Log(
                "DefaultTargetSearchStrategy",
                $"  - {enemyCache.Entity?.Config?.Name ?? "召唤师"}: 距离={distance:F2}, 血量={enemyCache.HpPercent:P0}, 评分={score:F2}"
            );

            if (score > bestScore)
            {
                bestCache = enemyCache;
                bestTarget = enemyCache.Entity; // 召唤师条目 Entity 为 null，后续通过 bestCache 处理
                bestScore = score;
            }
        }

        if (bestCache != null)
        {
            // 召唤师条目：Entity 为 null，但 SummonerProxy.GetComponent<ChessEntity>() 可取到动态添加的组件
            if (bestTarget == null && bestCache.SummonerProxy != null)
            {
                bestTarget = bestCache.SummonerProxy.GetComponent<ChessEntity>();
            }

            string targetName = bestTarget?.Config?.Name ?? "召唤师";
            DebugEx.Success(
                "DefaultTargetSearchStrategy",
                $"{self.Config?.Name} 选择目标: {targetName} (评分={bestScore:F2})"
            );
        }
        else
        {
            DebugEx.Warning("DefaultTargetSearchStrategy", $"{self.Config?.Name} 未找到合适的目标");
        }

        return bestTarget;
    }

    #endregion

    #region 评分逻辑

    /// <summary>
    /// 评估目标优先级
    /// </summary>
    /// <param name="self">自身</param>
    /// <param name="enemy">敌人缓存</param>
    /// <param name="distance">距离</param>
    /// <returns>综合评分</returns>
    private float EvaluateTarget(ChessEntity self, EnemyInfoCache enemy, float distance)
    {
        float score = 0f;

        // 1. 距离因素
        if (m_Config.PrioritizeNearby && m_Config.DistanceWeight > 0)
        {
            float distanceScore = CalculateDistanceScore(distance);
            score += distanceScore * m_Config.DistanceWeight;
        }

        // 2. 血量因素
        if (m_Config.PrioritizeLowHp && m_Config.HpWeight > 0)
        {
            float hpScore = CalculateHpScore(enemy.HpPercent);
            score += hpScore * m_Config.HpWeight;
        }

        // 3. 威胁度因素
        if (m_Config.PrioritizeHighThreat && m_Config.ThreatWeight > 0)
        {
            float threatScore = CalculateThreatScore(enemy.AttackPower);
            score += threatScore * m_Config.ThreatWeight;
        }

        return score;
    }

    /// <summary>
    /// 计算距离评分（距离越近分数越高）
    /// </summary>
    private float CalculateDistanceScore(float distance)
    {
        // 使用索敌距离作为最大距离，如果未设置则使用50作为默认值
        float maxDistance = m_Config.SearchRange > 0 ? m_Config.SearchRange : 50f;

        // 归一化到 0-100
        float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
        return (1f - normalizedDistance) * 100f;
    }

    /// <summary>
    /// 计算血量评分（血量越低分数越高）
    /// </summary>
    private float CalculateHpScore(float hpPercent)
    {
        // 血量百分比越低，分数越高
        return (1f - hpPercent) * 100f;
    }

    /// <summary>
    /// 计算威胁度评分（攻击力越高分数越高）
    /// </summary>
    private float CalculateThreatScore(double attackPower)
    {
        // 简单地使用攻击力作为威胁度
        // 可以根据实际情况调整计算方式
        return (float)attackPower;
    }

    #endregion
}
