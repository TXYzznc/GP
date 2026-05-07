using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子目标查找工具
/// 职责：
/// 1. 为AI和技能系统提供便捷的查询方法（FindNearestEnemy/FindNearestAlly）
/// 2. 提供通用的距离和范围检查工具
/// 3. 提供目标状态检查工具（死亡状态）
///
/// 注意：
/// - 卡牌目标选择请使用 TargetSelectors
/// - AI系统应优先使用 CombatEntityTracker.GetEnemyCache()
/// - 复杂的多目标筛选请使用 TargetSelectors
/// </summary>
public static class ChessTargetFinder
{
    #region 查找最近的敌人

    /// <summary>
    /// 查找最近的敌人
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <returns>最近的敌人，没有则返回null</returns>
    public static ChessEntity FindNearestEnemy(ChessEntity self)
    {
        if (self == null || CombatEntityTracker.Instance == null)
            return null;

        var enemies = CombatEntityTracker.Instance.GetEnemies(self.Camp);
        if (enemies == null || enemies.Count == 0)
            return null;

        ChessEntity nearest = null;
        float minDistSqr = float.MaxValue;
        Vector3 selfPos = self.transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            float distSqr = (enemy.transform.position - selfPos).sqrMagnitude;
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    #endregion

    #region 攻击范围检查

    /// <summary>
    /// 查找攻击范围内最近的敌人
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <returns>攻击范围内最近的敌人，没有则返回null</returns>
    #endregion

    #region 检查目标是否在攻击范围内

    /// <summary>
    /// 检查目标是否在攻击范围内
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <param name="target">目标棋子</param>
    /// <returns>是否在攻击范围内</returns>
    public static bool IsInAttackRange(ChessEntity self, ChessEntity target)
    {
        if (self == null || target == null)
            return false;

        float atkRange = (float)self.Attribute.AtkRange;
        float distSqr = (target.transform.position - self.transform.position).sqrMagnitude;

        return distSqr <= atkRange * atkRange;
    }

    /// <summary>
    /// 获取到目标的距离
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <param name="target">目标棋子</param>
    /// <returns>距离</returns>
    public static float GetDistanceTo(ChessEntity self, ChessEntity target)
    {
        if (self == null || target == null)
            return float.MaxValue;

        return Vector3.Distance(self.transform.position, target.transform.position);
    }

    /// <summary>
    /// 检查目标是否在施法范围内（用于技能）
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <param name="target">目标棋子</param>
    /// <param name="castRange">施法范围</param>
    /// <returns>是否在施法范围内</returns>
    public static bool IsInCastRange(ChessEntity self, ChessEntity target, double castRange)
    {
        if (self == null || target == null)
            return false;

        float range = (float)castRange;
        float distSqr = (target.transform.position - self.transform.position).sqrMagnitude;

        return distSqr <= range * range;
    }

    #endregion

    #region 查找友方目标

    /// <summary>
    /// 查找最近的友方
    /// </summary>
    public static ChessEntity FindNearestAlly(ChessEntity self)
    {
        if (self == null || CombatEntityTracker.Instance == null)
            return null;

        var allies = CombatEntityTracker.Instance.GetAllies(self.Camp);
        if (allies == null || allies.Count == 0)
            return null;

        ChessEntity nearest = null;
        float minDistSqr = float.MaxValue;
        Vector3 selfPos = self.transform.position;

        for (int i = 0; i < allies.Count; i++)
        {
            var ally = allies[i];
            if (ally == null || ally == self || ally.CurrentState == ChessState.Dead)
                continue;

            float distSqr = (ally.transform.position - selfPos).sqrMagnitude;
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                nearest = ally;
            }
        }

        return nearest;
    }

    #endregion

    #region 目标状态检查

    /// <summary>
    /// 检查目标是否符合目标死亡状态要求
    /// </summary>
    /// <param name="target">目标棋子</param>
    /// <param name="targetDeadState">目标死亡状态：0=只对活着，1=只对死亡，2=全部</param>
    /// <returns>是否符合要求</returns>
    public static bool IsValidTargetByDeadState(ChessEntity target, int targetDeadState)
    {
        if (target == null)
            return false;

        bool isTargetDead = target.CurrentState == ChessState.Dead;

        return targetDeadState switch
        {
            0 => !isTargetDead,      // 只对活着的目标
            1 => isTargetDead,       // 只对死亡的目标
            2 => true,               // 对所有目标
            _ => !isTargetDead       // 默认只对活着的目标
        };
    }

    #endregion

    #region 技能目标选择工具

    /// <summary>
    /// 查找综合状态最好的敌人
    /// 综合状态 = (当前HP / 最大HP) + (当前MP / 最大MP)
    /// 分数越高代表状态越好（综合能力最强）
    /// </summary>
    public static ChessEntity FindBestStateEnemy(ChessEntity self)
    {
        if (self == null || CombatEntityTracker.Instance == null)
            return null;

        var enemies = CombatEntityTracker.Instance.GetEnemies(self.Camp);
        if (enemies == null || enemies.Count == 0)
            return null;

        ChessEntity bestEnemy = null;
        double maxScore = -1.0;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            // 计算综合状态评分
            double hpPercent = enemy.Attribute.CurrentHp / enemy.Attribute.MaxHp;
            double mpPercent = enemy.Attribute.CurrentMp / enemy.Attribute.MaxMp;
            double score = hpPercent + mpPercent;

            if (score > maxScore)
            {
                maxScore = score;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    /// <summary>
    /// 查找范围内敌人最密集的位置（不考虑Y轴）
    /// 算法：计算所有敌人在 XZ 平面的质心
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <param name="searchRadius">搜索半径</param>
    /// <returns>敌人最密集的位置中心</returns>
    public static Vector3 FindMostDensePosition(ChessEntity self, float searchRadius)
    {
        if (self == null || CombatEntityTracker.Instance == null)
            return self.transform.position;

        var enemies = CombatEntityTracker.Instance.GetEnemies(self.Camp);
        if (enemies == null || enemies.Count == 0)
            return self.transform.position;

        Vector3 selfPos = self.transform.position;
        Vector3 center = Vector3.zero;
        int validCount = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            float distXZ = new Vector2(
                enemy.transform.position.x - selfPos.x,
                enemy.transform.position.z - selfPos.z
            ).magnitude;

            if (distXZ <= searchRadius)
            {
                center += enemy.transform.position;
                validCount++;
            }
        }

        if (validCount == 0)
            return selfPos;

        // 计算质心，保持原始 Y 值（不移动到敌人的 Y）
        Vector3 density = center / validCount;
        density.y = selfPos.y;  // 保留施法者的 Y 位置

        return density;
    }

    /// <summary>
    /// 查找指定目标的方向（用于范围型大招）
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <param name="target">目标棋子</param>
    /// <returns>从 self 指向 target 的方向</returns>
    public static Vector3 GetDirectionToTarget(ChessEntity self, ChessEntity target)
    {
        if (self == null || target == null)
            return Vector3.forward;

        Vector3 direction = (target.transform.position - self.transform.position).normalized;
        return direction;
    }

    #endregion
}
