using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子目标查找工具
/// 静态工具类，提供各种目标查找策略
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
        if (self == null || SummonChessManager.Instance == null)
            return null;

        // 使用阵营服务获取敌对阵营
        int[] enemyCamps = CampRelationService.GetEnemyCamps(self.Camp);
        var enemies = new System.Collections.Generic.List<ChessEntity>();
        for (int c = 0; c < enemyCamps.Length; c++)
        {
            enemies.AddRange(SummonChessManager.Instance.GetChessByCamp(enemyCamps[c]));
        }

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

    #region 查找攻击范围内的敌人

    /// <summary>
    /// 查找攻击范围内的敌人
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <returns>攻击范围内的敌人列表</returns>
    public static List<ChessEntity> FindEnemiesInRange(ChessEntity self)
    {
        List<ChessEntity> result = new List<ChessEntity>();

        if (self == null || SummonChessManager.Instance == null)
            return result;

        // 使用阵营服务获取敌对阵营
        int[] enemyCamps = CampRelationService.GetEnemyCamps(self.Camp);
        var enemies = new System.Collections.Generic.List<ChessEntity>();
        for (int c = 0; c < enemyCamps.Length; c++)
        {
            enemies.AddRange(SummonChessManager.Instance.GetChessByCamp(enemyCamps[c]));
        }

        float atkRange = (float)self.Attribute.AtkRange;
        Vector3 selfPos = self.transform.position;
        float rangeSqr = atkRange * atkRange;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            float distSqr = (enemy.transform.position - selfPos).sqrMagnitude;
            if (distSqr <= rangeSqr)
            {
                result.Add(enemy);
            }
        }

        return result;
    }

    /// <summary>
    /// 查找攻击范围内最近的敌人
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <returns>攻击范围内最近的敌人，没有则返回null</returns>
    public static ChessEntity FindNearestEnemyInRange(ChessEntity self)
    {
        if (self == null || SummonChessManager.Instance == null)
            return null;

        // 使用阵营服务获取敌对阵营
        int[] enemyCamps = CampRelationService.GetEnemyCamps(self.Camp);
        var enemies = new System.Collections.Generic.List<ChessEntity>();
        for (int c = 0; c < enemyCamps.Length; c++)
        {
            enemies.AddRange(SummonChessManager.Instance.GetChessByCamp(enemyCamps[c]));
        }

        float atkRange = (float)self.Attribute.AtkRange;
        Vector3 selfPos = self.transform.position;
        float rangeSqr = atkRange * atkRange;

        ChessEntity nearest = null;
        float minDistSqr = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            float distSqr = (enemy.transform.position - selfPos).sqrMagnitude;
            if (distSqr <= rangeSqr && distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

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

    #endregion

    #region 查找生命值最低的敌人

    /// <summary>
    /// 查找生命值最低的敌人
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <returns>生命值最低的敌人，没有则返回null</returns>
    public static ChessEntity FindLowestHpEnemy(ChessEntity self)
    {
        if (self == null || SummonChessManager.Instance == null)
            return null;

        // 使用阵营服务获取敌对阵营
        int[] enemyCamps = CampRelationService.GetEnemyCamps(self.Camp);
        var enemies = new System.Collections.Generic.List<ChessEntity>();
        for (int c = 0; c < enemyCamps.Length; c++)
        {
            enemies.AddRange(SummonChessManager.Instance.GetChessByCamp(enemyCamps[c]));
        }

        ChessEntity lowestHp = null;
        double minHp = double.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            if (enemy.Attribute.CurrentHp < minHp)
            {
                minHp = enemy.Attribute.CurrentHp;
                lowestHp = enemy;
            }
        }

        return lowestHp;
    }

    /// <summary>
    /// 查找攻击范围内生命值最低的敌人
    /// </summary>
    /// <param name="self">自身棋子</param>
    /// <returns>攻击范围内生命值最低的敌人，没有则返回null</returns>
    public static ChessEntity FindLowestHpEnemyInRange(ChessEntity self)
    {
        var enemiesInRange = FindEnemiesInRange(self);

        ChessEntity lowestHp = null;
        double minHp = double.MaxValue;

        for (int i = 0; i < enemiesInRange.Count; i++)
        {
            var enemy = enemiesInRange[i];
            if (enemy.Attribute.CurrentHp < minHp)
            {
                minHp = enemy.Attribute.CurrentHp;
                lowestHp = enemy;
            }
        }

        return lowestHp;
    }

    #endregion
}
