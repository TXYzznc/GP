using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 阵营关系服务
/// 集中管理所有阵营关系判断逻辑
/// </summary>
public static class CampRelationService
{
    #region 配置

    /// <summary>本地玩家阵营（用于确定敌我显示）</summary>
    private static int s_LocalPlayerCamp = (int)CampType.Player;

    /// <summary>
    /// 阵营敌对关系表
    /// Key: 阵营ID, Value: 该阵营的敌对阵营列表
    /// </summary>
    private static readonly Dictionary<int, HashSet<int>> s_EnemyRelations = new Dictionary<int, HashSet<int>>
    {
        // PVE模式：玩家 vs 敌人
        { (int)CampType.Player, new HashSet<int> { (int)CampType.Enemy } },
        { (int)CampType.Enemy, new HashSet<int> { (int)CampType.Player } },
        
        // 中立不与任何阵营敌对
        { (int)CampType.Neutral, new HashSet<int>() },
        
        // PVP模式预留：队伍之间互为敌人
        { (int)CampType.Team1, new HashSet<int> { (int)CampType.Team2, (int)CampType.Team3, (int)CampType.Team4 } },
        { (int)CampType.Team2, new HashSet<int> { (int)CampType.Team1, (int)CampType.Team3, (int)CampType.Team4 } },
        { (int)CampType.Team3, new HashSet<int> { (int)CampType.Team1, (int)CampType.Team2, (int)CampType.Team4 } },
        { (int)CampType.Team4, new HashSet<int> { (int)CampType.Team1, (int)CampType.Team2, (int)CampType.Team3 } },
    };

    /// <summary>
    /// 阵营对应的Layer映射
    /// </summary>
    private static readonly Dictionary<int, string> s_CampLayerMap = new Dictionary<int, string>
    {
        { (int)CampType.Player, "Chess" },  // 玩家棋子统一用Chess层
        { (int)CampType.Enemy, "Chess" },   // 敌人棋子也用Chess层
        { (int)CampType.Neutral, "Chess" },
    };

    #endregion

    #region 公共API

    /// <summary>
    /// 设置本地玩家阵营
    /// 用于确定从哪个视角判断敌我（影响描边颜色等显示）
    /// </summary>
    /// <param name="camp">本地玩家的阵营ID</param>
    public static void SetLocalPlayerCamp(int camp)
    {
        s_LocalPlayerCamp = camp;
        Debug.Log($"[CampRelationService] 设置本地玩家阵营: {camp}");
    }

    /// <summary>
    /// 获取本地玩家阵营
    /// </summary>
    public static int GetLocalPlayerCamp()
    {
        return s_LocalPlayerCamp;
    }

    /// <summary>
    /// 获取两个阵营之间的关系
    /// </summary>
    /// <param name="campA">阵营A</param>
    /// <param name="campB">阵营B</param>
    /// <returns>阵营关系</returns>
    public static CampRelation GetRelation(int campA, int campB)
    {
        // 同阵营
        if (campA == campB)
        {
            return CampRelation.Ally;
        }

        // 检查是否为敌对关系
        if (s_EnemyRelations.TryGetValue(campA, out var enemies))
        {
            if (enemies.Contains(campB))
            {
                return CampRelation.Enemy;
            }
        }

        // 默认为中立
        return CampRelation.Neutral;
    }

    /// <summary>
    /// 获取实体相对于本地玩家的关系
    /// 用于UI显示（描边颜色等）
    /// </summary>
    /// <param name="entityCamp">实体阵营</param>
    /// <returns>相对于本地玩家的关系</returns>
    public static CampRelation GetRelationToLocalPlayer(int entityCamp)
    {
        if (entityCamp == s_LocalPlayerCamp)
        {
            return CampRelation.Ally;
        }
        return GetRelation(s_LocalPlayerCamp, entityCamp);
    }

    /// <summary>
    /// 判断两个阵营是否为敌对关系
    /// </summary>
    public static bool IsEnemy(int campA, int campB)
    {
        return GetRelation(campA, campB) == CampRelation.Enemy;
    }

    /// <summary>
    /// 判断两个实体是否为敌对关系
    /// </summary>
    public static bool IsEnemy(ChessEntity entityA, ChessEntity entityB)
    {
        if (entityA == null || entityB == null) return false;
        return IsEnemy(entityA.Camp, entityB.Camp);
    }

    /// <summary>
    /// 判断两个阵营是否为友军关系
    /// </summary>
    public static bool IsAlly(int campA, int campB)
    {
        return GetRelation(campA, campB) == CampRelation.Ally;
    }

    /// <summary>
    /// 判断两个实体是否为友军关系
    /// </summary>
    public static bool IsAlly(ChessEntity entityA, ChessEntity entityB)
    {
        if (entityA == null || entityB == null) return false;
        return IsAlly(entityA.Camp, entityB.Camp);
    }

    /// <summary>
    /// 获取指定阵营的所有敌对阵营
    /// </summary>
    /// <param name="camp">阵营ID</param>
    /// <returns>敌对阵营列表</returns>
    public static int[] GetEnemyCamps(int camp)
    {
        if (s_EnemyRelations.TryGetValue(camp, out var enemies))
        {
            int[] result = new int[enemies.Count];
            enemies.CopyTo(result);
            return result;
        }
        return System.Array.Empty<int>();
    }

    /// <summary>
    /// 获取敌对阵营的LayerMask
    /// 用于物理检测
    /// </summary>
    /// <param name="selfCamp">自身阵营</param>
    /// <returns>敌对阵营的LayerMask</returns>
    public static LayerMask GetEnemyLayerMask(int selfCamp)
    {
        // 当前简化实现：所有棋子都在Chess层
        // 未来可以根据阵营返回不同的Layer组合
        return LayerMask.GetMask("Chess");
    }

    /// <summary>
    /// 判断目标是否为指定阵营的敌人
    /// </summary>
    /// <param name="target">目标实体</param>
    /// <param name="attackerCamp">攻击者阵营</param>
    /// <returns>是否为敌人</returns>
    public static bool IsValidTarget(ChessEntity target, int attackerCamp)
    {
        if (target == null) return false;
        if (target.CurrentState == ChessState.Dead) return false;
        return IsEnemy(attackerCamp, target.Camp);
    }

    #endregion

    #region 扩展API（预留PVP）

    /// <summary>
    /// 注册自定义敌对关系
    /// 用于PVP模式动态配置
    /// </summary>
    public static void RegisterEnemyRelation(int campA, int campB)
    {
        if (!s_EnemyRelations.ContainsKey(campA))
        {
            s_EnemyRelations[campA] = new HashSet<int>();
        }
        s_EnemyRelations[campA].Add(campB);

        // 双向注册
        if (!s_EnemyRelations.ContainsKey(campB))
        {
            s_EnemyRelations[campB] = new HashSet<int>();
        }
        s_EnemyRelations[campB].Add(campA);
    }

    /// <summary>
    /// 清除所有自定义敌对关系
    /// 用于战斗结束后重置
    /// </summary>
    public static void ClearCustomRelations()
    {
        // 重置为默认PVE关系
        s_EnemyRelations.Clear();
        s_EnemyRelations[(int)CampType.Player] = new HashSet<int> { (int)CampType.Enemy };
        s_EnemyRelations[(int)CampType.Enemy] = new HashSet<int> { (int)CampType.Player };
        s_EnemyRelations[(int)CampType.Neutral] = new HashSet<int>();
    }

    #endregion
}
