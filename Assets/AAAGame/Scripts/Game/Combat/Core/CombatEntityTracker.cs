using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 战斗实体追踪器
/// 职责：
/// 1. 统一管理场上所有棋子
/// 2. 按阵营分组存储，支持快速查询
/// 3. 提供高性能的敌人搜索接口
/// 4. 避免使用 FindObjectsOfType
/// </summary>
public class CombatEntityTracker : MonoBehaviour
{
    #region 单例

    private static CombatEntityTracker s_Instance;

    /// <summary>获取单例实例（懒加载模式：不存在时自动创建）</summary>
    public static CombatEntityTracker Instance
    {
        get
        {
            if (s_Instance == null)
            {
                // 先尝试在场景中查找
                s_Instance = FindObjectOfType<CombatEntityTracker>();

                // 如果场景中不存在，则自动创建
                if (s_Instance == null)
                {
                    GameObject managerObj = new("CombatEntityTracker");
                    s_Instance = managerObj.AddComponent<CombatEntityTracker>();
                    DebugEx.LogModule("CombatEntityTracker", "单例实例不存在，已自动创建");
                }
            }
            return s_Instance;
        }
    }

    #endregion

    #region 私有字段

    /// <summary>按阵营分组的棋子字典（Camp -> List<ChessEntity>）</summary>
    private readonly Dictionary<int, List<ChessEntity>> m_ChessByCamp = new();

    /// <summary>所有棋子列表（用于快速遍历）</summary>
    private readonly List<ChessEntity> m_AllChess = new();

    /// <summary>清理计时器</summary>
    private float m_CleanupTimer = 0f;

    /// <summary>清理间隔（秒）</summary>
    private const float CLEANUP_INTERVAL = 2f;

    #endregion

    #region 初始化

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            DebugEx.WarningModule("CombatEntityTracker", "检测到重复实例，销毁当前对象");
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        DebugEx.LogModule("CombatEntityTracker", "棋子管理器已初始化");
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
            DebugEx.LogModule("CombatEntityTracker", "棋子管理器已销毁");
        }
    }

    #endregion

    #region 每帧更新

    private void Update()
    {
        // 定期清理已死亡或被销毁的棋子
        m_CleanupTimer -= Time.deltaTime;
        if (m_CleanupTimer <= 0f)
        {
            CleanupDeadChess();
            m_CleanupTimer = CLEANUP_INTERVAL;
        }
    }

    #endregion

    #region 棋子注册/注销

    /// <summary>
    /// 注册棋子（棋子生成时调用）
    /// </summary>
    /// <param name="chess">棋子实体</param>
    public void RegisterChess(ChessEntity chess)
    {
        if (chess == null)
        {
            DebugEx.WarningModule("CombatEntityTracker", "尝试注册空棋子");
            return;
        }

        int camp = chess.Camp;

        // 添加到阵营分组
        if (!m_ChessByCamp.ContainsKey(camp))
        {
            m_ChessByCamp[camp] = new();
        }

        if (!m_ChessByCamp[camp].Contains(chess))
        {
            m_ChessByCamp[camp].Add(chess);
        }

        // 添加到总列表
        if (!m_AllChess.Contains(chess))
        {
            m_AllChess.Add(chess);
        }

        // ⭐ 自动维护敌人缓存
        AddEnemyToCache(chess);

        DebugEx.LogModule(
            "CombatEntityTracker",
            $"注册棋子: {chess.Config?.Name}, Camp={camp}, 当前总数={m_AllChess.Count}"
        );
    }

    /// <summary>
    /// 注销棋子（棋子死亡或销毁时调用）
    /// </summary>
    /// <param name="chess">棋子实体</param>
    public void UnregisterChess(ChessEntity chess)
    {
        if (chess == null)
        {
            return;
        }

        int camp = chess.Camp;

        // 从阵营分组移除
        if (m_ChessByCamp.TryGetValue(camp, out var list))
        {
            list.Remove(chess);
        }

        // 从总列表移除
        m_AllChess.Remove(chess);

        // ⭐ 自动维护敌人缓存
        RemoveEnemyFromCache(chess);

        DebugEx.LogModule(
            "CombatEntityTracker",
            $"注销棋子: {chess.Config?.Name}, Camp={camp}, 剩余总数={m_AllChess.Count}"
        );

        TryEndCombatIfOneSideAllDead();
    }

    private void TryEndCombatIfOneSideAllDead()
    {
        if (CombatManager.Instance == null || !CombatManager.Instance.IsInCombat)
        {
            return;
        }

        int playerCamp = (int)CampType.Player;
        int enemyCamp = (int)CampType.Enemy;

        int playerChessAlive = GetChessCount(playerCamp);
        int enemyAlive = GetChessCount(enemyCamp);

        // 召唤师存活时视为玩家方仍有战斗单位
        bool summonerAlive = m_SummonerProxy != null && !m_SummonerProxy.IsDead;
        int playerAlive = playerChessAlive + (summonerAlive ? 1 : 0);

        if (playerAlive > 0 && enemyAlive > 0)
        {
            return;
        }

        bool isVictory = enemyAlive <= 0 && playerAlive > 0;
        CombatManager.Instance.EndCombat(isVictory);
    }

    /// <summary>
    /// 清理已死亡或被销毁的棋子
    /// </summary>
    private void CleanupDeadChess()
    {
        int removedCount = 0;

        // 清理总列表
        m_AllChess.RemoveAll(chess =>
        {
            bool shouldRemove = chess == null || chess.Attribute.IsDead;
            if (shouldRemove)
                removedCount++;
            return shouldRemove;
        });

        // 清理阵营分组
        foreach (var kvp in m_ChessByCamp)
        {
            kvp.Value.RemoveAll(chess => chess == null || chess.Attribute.IsDead);
        }
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 获取指定阵营的所有敌人（存活）
    /// </summary>
    /// <param name="myCamp">我方阵营</param>
    /// <returns>敌人列表</returns>
    public List<ChessEntity> GetEnemies(int myCamp)
    {
        List<ChessEntity> enemies = new();

        foreach (var kvp in m_ChessByCamp)
        {
            int camp = kvp.Key;
            CampRelation relation = CampRelationService.GetRelation(myCamp, camp);

            if (relation == CampRelation.Enemy)
            {
                // 只返回存活的敌人
                foreach (var chess in kvp.Value)
                {
                    if (chess != null && !chess.Attribute.IsDead)
                    {
                        enemies.Add(chess);
                    }
                }
            }
        }

        return enemies;
    }

    /// <summary>
    /// 获取指定阵营的所有友军（存活）
    /// </summary>
    /// <param name="myCamp">我方阵营</param>
    /// <returns>友军列表</returns>
    public List<ChessEntity> GetAllies(int myCamp)
    {
        List<ChessEntity> allies = new();

        if (m_ChessByCamp.TryGetValue(myCamp, out var list))
        {
            foreach (var chess in list)
            {
                if (chess != null && !chess.Attribute.IsDead)
                {
                    allies.Add(chess);
                }
            }
        }

        return allies;
    }

    /// <summary>
    /// 获取所有存活的棋子
    /// </summary>
    /// <returns>棋子列表</returns>
    public List<ChessEntity> GetAllAliveChess()
    {
        List<ChessEntity> aliveChess = new();

        foreach (var chess in m_AllChess)
        {
            if (chess != null && !chess.Attribute.IsDead)
            {
                aliveChess.Add(chess);
            }
        }

        return aliveChess;
    }

    /// <summary>
    /// 获取指定阵营的棋子数量（存活）
    /// </summary>
    /// <param name="camp">阵营</param>
    /// <returns>棋子数量</returns>
    public int GetChessCount(int camp)
    {
        if (!m_ChessByCamp.TryGetValue(camp, out var list))
        {
            return 0;
        }

        int count = 0;
        foreach (var chess in list)
        {
            if (chess != null && !chess.Attribute.IsDead)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 获取总棋子数量（存活）
    /// </summary>
    /// <returns>棋子数量</returns>
    public int GetTotalChessCount()
    {
        int count = 0;
        foreach (var chess in m_AllChess)
        {
            if (chess != null && !chess.Attribute.IsDead)
            {
                count++;
            }
        }
        return count;
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清空所有棋子（战斗结束时调用）
    /// </summary>
    public void Clear()
    {
        m_ChessByCamp.Clear();
        m_AllChess.Clear();

        // 同时清空敌人信息缓存
        ClearEnemyCache();

        DebugEx.Log("CombatEntityTracker", "已清空所有棋子数据和敌人缓存");
    }

    #endregion

    #region 调试

    /// <summary>
    /// 打印当前棋子统计信息
    /// </summary>
    public void DebugPrintStats()
    {
        DebugEx.LogModule("CombatEntityTracker", "=== 棋子统计 ===");
        DebugEx.LogModule("CombatEntityTracker", $"总数: {m_AllChess.Count}");

        foreach (var kvp in m_ChessByCamp)
        {
            int camp = kvp.Key;
            int count = kvp.Value.Count;
            DebugEx.LogModule("CombatEntityTracker", $"阵营 {camp}: {count} 个棋子");
        }

        DebugEx.LogModule("CombatEntityTracker", "================");
    }

    #endregion

    #region 敌人信息缓存（改进版 - 持久化列表）

    /// <summary>按阵营缓存敌人信息（我方阵营 -> 敌人缓存列表）</summary>
    private readonly Dictionary<int, List<EnemyInfoCache>> m_EnemyCacheByMyCamp = new();

    /// <summary>缓存是否已构建</summary>
    private bool m_IsCacheBuilt = false;

    /// <summary>召唤师战斗代理（战斗期间注册，作为敌方目标之一）</summary>
    private SummonerCombatProxy m_SummonerProxy;

    /// <summary>
    /// 构建敌人信息缓存
    /// 战斗开始时调用，预加载所有敌人信息
    /// </summary>
    public void BuildEnemyCache()
    {
        DebugEx.LogModule("CombatEntityTracker", "开始构建敌人信息缓存...");

        m_EnemyCacheByMyCamp.Clear();

        // 获取所有阵营
        HashSet<int> allCamps = new HashSet<int>(m_ChessByCamp.Keys);

        int totalCacheCount = 0;

        // 为每个阵营构建其敌人缓存
        foreach (int myCamp in allCamps)
        {
            List<EnemyInfoCache> enemyCaches = new();

            // 获取该阵营的所有敌人
            List<ChessEntity> enemies = GetEnemies(myCamp);

            foreach (var enemy in enemies)
            {
                var cache = EnemyInfoCache.FromEntity(enemy);
                if (cache != null)
                {
                    enemyCaches.Add(cache);
                    totalCacheCount++;
                }
            }

            m_EnemyCacheByMyCamp[myCamp] = enemyCaches;

            DebugEx.LogModule("CombatEntityTracker", $"阵营 {myCamp} 缓存了 {enemyCaches.Count} 个敌人");
        }

        m_IsCacheBuilt = true;

        DebugEx.Success(
            "CombatEntityTracker",
            $"敌人信息缓存构建完成 - {allCamps.Count} 个阵营，{totalCacheCount} 条缓存"
        );
    }

    /// <summary>
    /// 向敌人缓存中添加新敌人（敌人生成时调用）
    /// </summary>
    /// <param name="enemy">新生成的敌人棋子</param>
    public void AddEnemyToCache(ChessEntity enemy)
    {
        if (enemy == null)
        {
            DebugEx.WarningModule("CombatEntityTracker", "尝试添加空敌人到缓存");
            return;
        }

        if (!m_IsCacheBuilt)
        {
            DebugEx.WarningModule("CombatEntityTracker", "缓存未构建，跳过添加敌人");
            return;
        }

        var cache = EnemyInfoCache.FromEntity(enemy);
        if (cache == null)
        {
            DebugEx.WarningModule("CombatEntityTracker", $"无法为敌人 {enemy.Config?.Name} 创建缓存");
            return;
        }

        int enemyCamp = enemy.Camp;

        // 为所有其他阵营添加这个敌人到缓存
        foreach (var kvp in m_EnemyCacheByMyCamp)
        {
            int myCamp = kvp.Key;
            var cacheList = kvp.Value;

            // 检查是否为敌对关系
            CampRelation relation = CampRelationService.GetRelation(myCamp, enemyCamp);
            if (relation == CampRelation.Enemy)
            {
                // 检查是否已存在
                bool exists = cacheList.Any(c => c.Entity == enemy);
                if (!exists)
                {
                    cacheList.Add(cache);
                    DebugEx.LogModule("CombatEntityTracker",
                        $"为阵营 {myCamp} 添加敌人缓存: {enemy.Config?.Name}");
                }
            }
        }
    }

    /// <summary>
    /// 从敌人缓存中移除敌人（敌人死亡时调用）
    /// </summary>
    /// <param name="enemy">死亡的敌人棋子</param>
    public void RemoveEnemyFromCache(ChessEntity enemy)
    {
        if (enemy == null || !m_IsCacheBuilt)
        {
            return;
        }

        int removedCount = 0;

        // 从所有阵营的缓存中移除这个敌人
        foreach (var kvp in m_EnemyCacheByMyCamp)
        {
            var cacheList = kvp.Value;
            int beforeCount = cacheList.Count;

            cacheList.RemoveAll(cache => cache.Entity == enemy);

            int afterCount = cacheList.Count;
            if (beforeCount > afterCount)
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            DebugEx.LogModule("CombatEntityTracker",
                $"从 {removedCount} 个阵营缓存中移除敌人: {enemy.Config?.Name}");
        }
    }

    /// <summary>
    /// 获取指定阵营的敌人信息缓存（仅返回存活的）
    /// </summary>
    /// <param name="myCamp">我方阵营</param>
    /// <returns>敌人信息缓存列表</returns>
    public List<EnemyInfoCache> GetEnemyCache(int myCamp)
    {
        if (!m_IsCacheBuilt)
        {
            DebugEx.Warning("CombatEntityTracker", "敌人缓存未构建，尝试自动构建");
            BuildEnemyCache();
        }

        if (!m_EnemyCacheByMyCamp.TryGetValue(myCamp, out var cacheList))
        {
            return new();
        }

        // 过滤掉已死亡的（实时检查）
        var result = cacheList.FindAll(cache => cache.IsAlive);

        // 如果查询方是敌方（Camp=1），且召唤师已注册且存活，将其追加到目标列表
        if (myCamp == 1 && m_SummonerProxy != null && !m_SummonerProxy.IsDead)
        {
            var summonerCache = EnemyInfoCache.FromSummonerProxy(m_SummonerProxy);
            if (summonerCache != null)
                result.Add(summonerCache);
        }

        return result;
    }

    /// <summary>
    /// 清理敌人信息缓存
    /// 战斗结束时调用
    /// </summary>
    public void ClearEnemyCache()
    {
        m_EnemyCacheByMyCamp.Clear();
        m_IsCacheBuilt = false;

        DebugEx.Log("CombatEntityTracker", "敌人信息缓存已清空");
    }

    /// <summary>
    /// 检查缓存是否已构建
    /// </summary>
    public bool IsCacheBuilt => m_IsCacheBuilt;

    #endregion

    #region 召唤师注册

    /// <summary>
    /// 注册召唤师战斗代理（战斗开始时由 CombatManager 调用）
    /// 召唤师将作为 Camp=0 的目标出现在敌方（Camp=1）的目标列表中
    /// </summary>
    public void RegisterSummoner(SummonerCombatProxy proxy)
    {
        if (proxy == null)
        {
            DebugEx.WarningModule("CombatEntityTracker", "尝试注册空召唤师代理");
            return;
        }

        m_SummonerProxy = proxy;
        DebugEx.LogModule("CombatEntityTracker", "召唤师战斗代理已注册");
    }

    /// <summary>
    /// 注销召唤师战斗代理（战斗结束时由 CombatManager 调用）
    /// </summary>
    public void UnregisterSummoner()
    {
        m_SummonerProxy = null;
        DebugEx.LogModule("CombatEntityTracker", "召唤师战斗代理已注销");
    }

    #endregion
}
