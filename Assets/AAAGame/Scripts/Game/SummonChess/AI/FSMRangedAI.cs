using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 基于状态机的远程AI
/// 行为：搜索敌人 → 移动到攻击范围内 → 攻击
/// AIType = 2（对应配置表中的远程AI类型）
/// </summary>
public class FSMRangedAI : ChessAIBase
{
    #region 基类重载

    #endregion

    #region 移动状态逻辑

    /// <summary>
    /// 移动状态逻辑
    /// 远程单位会移动到攻击范围内
    /// 转换规则：
    /// - 目标丢失 → 待机
    /// - 到达攻击范围 → 待机（重新决策进入攻击状态）
    /// </summary>
    protected override void TickMoving(float dt)
    {
        // 检查目标有效性
        if (!IsTargetValid())
        {
            DebugEx.LogModule("FSMRangedAI", 
                $"{m_Context.Entity.Config.Name} 目标无效，返回待机");
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 检查是否到达攻击范围
        if (IsInAttackRange(m_CurrentTarget))
        {
            DebugEx.LogModule("FSMRangedAI", 
                $"{m_Context.Entity.Config.Name} 到达攻击范围，返回待机重新决策");
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 继续移动到目标
        MoveToTarget(m_CurrentTarget);
    }

    #endregion

    #region 目标搜索

    /// <summary>
    /// 寻找攻击目标
    /// 远程单位搜索所有敌人，不限制攻击范围
    /// </summary>
    public override ChessEntity FindTarget()
    {
        if (m_Context?.Entity == null)
            return null;

        // 检查 CombatEntityTracker 是否可用
        if (CombatEntityTracker.Instance == null)
        {
            DebugEx.Warning("FSMRangedAI", "未找到 CombatEntityTracker，无法搜索目标");
            return null;
        }

        int myCamp = m_Context.Entity.Camp;
        float attackRange = (float)m_Context.Entity.Attribute.AtkRange;

        DebugEx.LogModule("FSMRangedAI", 
            $"{m_Context.Entity.Config.Name} 开始搜索目标，攻击范围={attackRange:F2}");

        // 使用敌人信息缓存
        List<EnemyInfoCache> enemyCache = CombatEntityTracker.Instance.GetEnemyCache(myCamp);

        DebugEx.LogModule("FSMRangedAI", 
            $"{m_Context.Entity.Config.Name} 获取敌人缓存，数量={enemyCache.Count}");

        if (enemyCache.Count == 0)
        {
            DebugEx.LogModule("FSMRangedAI", "没有可攻击的敌人");
            return null;
        }

        // 详细输出敌人缓存信息
        foreach (var enemyInfo in enemyCache)
        {
            DebugEx.LogModule("FSMRangedAI",
                $"敌人信息：{enemyInfo.Entity?.Config?.Name ?? (enemyInfo.SummonerProxy != null ? "召唤师" : "?")}, " +
                $"IsAlive={enemyInfo.IsAlive}, Entity={enemyInfo.Entity != null}, IsSummoner={enemyInfo.SummonerProxy != null}");
        }

        Vector3 myPosition = m_Context.Entity.transform.position;

        // 远程AI：搜索所有敌人，不限制攻击范围
        // 优先选择距离最近的敌人；同时支持召唤师条目（Entity 为 null，位置从 SummonerProxy 取）
        ChessEntity bestTarget = null;
        float minDistance = float.MaxValue;

        foreach (var enemyInfo in enemyCache)
        {
            if (!enemyInfo.IsAlive) continue;

            // 召唤师条目：Entity 为 null，从 SummonerProxy.GetComponent<ChessEntity>() 取动态组件
            ChessEntity candidate = enemyInfo.Entity;
            if (candidate == null && enemyInfo.SummonerProxy != null)
            {
                candidate = enemyInfo.SummonerProxy.GetComponent<ChessEntity>();
            }

            if (candidate == null || candidate.CurrentState == ChessState.Dead) continue;

            float distance = Vector3.Distance(myPosition, enemyInfo.CurrentPosition);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestTarget = candidate;
            }
        }

        if (bestTarget != null)
        {
            bool inRange = minDistance <= attackRange;
            DebugEx.LogModule("FSMRangedAI",
                $"{m_Context.Entity.Config.Name} 找到目标 {bestTarget.Config?.Name ?? "召唤师"}，距离={minDistance:F2}，" +
                $"攻击范围={attackRange:F2}，在范围内={inRange}");
        }
        else
        {
            DebugEx.LogModule("FSMRangedAI", "未找到有效目标");
        }

        return bestTarget;
    }

    #endregion
}
