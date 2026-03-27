using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 棋子管理器 - 负责棋子的生成、销毁与查询
/// </summary>
public class SummonChessManager : MonoBehaviour
{
    #region 单例

    public static SummonChessManager Instance { get; private set; }

    #endregion

    #region 私有字段

    private List<ChessEntity> m_AllChess = new List<ChessEntity>();
    private Dictionary<int, ChessEntity> m_ChessDict = new Dictionary<int, ChessEntity>();
    private int m_NextInstanceId = 1;

    // 资源缓存
    private Dictionary<int, GameObject> m_PrefabCache = new Dictionary<int, GameObject>();

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 挂载生命周期处理器（负责 HP 归零 → 死亡流程）
        gameObject.AddComponent<ChessLifecycleHandler>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 生成棋子

    /// <summary>
    /// 异步生成棋子
    /// </summary>
    /// <param name="chessId">召唤棋子ID</param>
    /// <param name="position">生成位置</param>
    /// <param name="camp">阵营（0=友方，1=敌人）</param>
    /// <returns>生成的棋子实体，失败返回null</returns>
    public async UniTask<ChessEntity> SpawnChessAsync(int chessId, Vector3 position, int camp)
    {
        // 1. 获取配置
        if (!ChessDataManager.Instance.TryGetConfig(chessId, out var config))
        {
            DebugEx.ErrorModule(
                "SummonChessManager",
                $"SpawnChess failed: config not found for chessId={chessId}"
            );
            return null;
        }

        // 2. 加载预制体
        GameObject prefab = await LoadPrefabAsync(config.PrefabId);
        if (prefab == null)
        {
            DebugEx.ErrorModule(
                "SummonChessManager",
                $"SpawnChess failed: prefab not found for chessId={chessId}, prefabId={config.PrefabId}"
            );
            return null;
        }

        // 3. 实例化
        GameObject chessObj = Instantiate(prefab, position, Quaternion.identity);
        chessObj.name = $"Chess_{config.Name}_{m_NextInstanceId}";

        // 3.5. 底部对齐地面
        float bottomOffset = EntityPositionHelper.CalculateBottomOffset(chessObj);
        chessObj.transform.position = new Vector3(
            position.x,
            position.y + bottomOffset,
            position.z
        );

        DebugEx.LogModule(
            "SummonChessManager",
            $"棋子底部对齐: {config.Name}, 目标Y={position.y}, 底部偏移={bottomOffset:F3}, 最终Y={chessObj.transform.position.y:F3}"
        );

        // 4. 获取ChessEntity组件
        ChessEntity entity = chessObj.GetComponent<ChessEntity>();
        if (entity == null)
        {
            entity = chessObj.AddComponent<ChessEntity>();
        }

        // 5. 初始化
        entity.Initialize(chessId, config, camp);

        // 5.5. 从全局状态加载持久化血量（战斗间血量不重置）
        BattleChessManager.Instance.RegisterChessEntity(entity);

        // 6. 注册到管理器
        int instanceId = m_NextInstanceId++;
        m_AllChess.Add(entity);
        m_ChessDict[instanceId] = entity;

        // 7. 触发生成事件（ChessLifecycleHandler 订阅此事件，负责 HP 归零后的死亡处理）
        OnChessSpawned?.Invoke(entity);

        DebugEx.LogModule(
            "SummonChessManager",
            $"SpawnChess success: chessId={chessId}, name={config.Name}, camp={camp}"
        );

        return entity;
    }

    /// <summary>
    /// 异步加载预制体（带缓存）
    /// </summary>
    private async UniTask<GameObject> LoadPrefabAsync(int resourceId)
    {
        // 检查缓存
        if (m_PrefabCache.TryGetValue(resourceId, out var cached))
        {
            return cached;
        }

        // 使用ResourceExtension加载
        var prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(resourceId);

        if (prefab != null)
        {
            m_PrefabCache[resourceId] = prefab;
            DebugEx.LogModule("SummonChessManager", $"LoadPrefab success: resourceId={resourceId}");
        }
        else
        {
            DebugEx.WarningModule(
                "SummonChessManager",
                $"LoadPrefab failed: resourceId={resourceId}"
            );
        }

        return prefab;
    }

    #endregion

    #region 销毁棋子

    /// <summary>
    /// 销毁棋子
    /// </summary>
    public void DestroyChess(ChessEntity entity)
    {
        if (entity == null)
            return;

        // 1. 从列表中移除
        m_AllChess.Remove(entity);
        m_ChessDict.Remove(entity.InstanceId);

        // 2. 触发销毁事件
        OnChessDestroyed?.Invoke(entity);

        // 3. 销毁GameObject
        Destroy(entity.gameObject);

        DebugEx.LogModule(
            "SummonChessManager",
            $"DestroyChess: chessId={entity.ChessId}, name={entity.Config.Name}"
        );
    }

    /// <summary>
    /// 销毁所有棋子（战斗结束时调用）
    /// </summary>
    public void DestroyAllChess()
    {
        // 倒序遍历，避免在遍历时修改列表
        for (int i = m_AllChess.Count - 1; i >= 0; i--)
        {
            var entity = m_AllChess[i];
            if (entity != null)
            {
                OnChessDestroyed?.Invoke(entity);
                Destroy(entity.gameObject);
            }
        }

        m_AllChess.Clear();
        m_ChessDict.Clear();

        DebugEx.LogModule("SummonChessManager", "DestroyAllChess: 已销毁所有棋子");
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 获取所有棋子
    /// </summary>
    public IReadOnlyList<ChessEntity> GetAllChess()
    {
        return m_AllChess;
    }

    /// <summary>
    /// 根据实例ID查询棋子
    /// </summary>
    public ChessEntity GetChess(int instanceId)
    {
        m_ChessDict.TryGetValue(instanceId, out var entity);
        return entity;
    }

    /// <summary>
    /// 查询指定阵营的所有棋子
    /// </summary>
    public List<ChessEntity> GetChessByCamp(int camp)
    {
        List<ChessEntity> result = new List<ChessEntity>();
        for (int i = 0; i < m_AllChess.Count; i++)
        {
            if (m_AllChess[i].Camp == camp)
            {
                result.Add(m_AllChess[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// 查询指定范围内的所有棋子
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="radius">半径</param>
    /// <param name="camp">阵营过滤（-1表示不过滤）</param>
    public List<ChessEntity> GetChessInRange(Vector3 center, float radius, int camp = -1)
    {
        List<ChessEntity> result = new List<ChessEntity>();
        float radiusSqr = radius * radius;

        for (int i = 0; i < m_AllChess.Count; i++)
        {
            var chess = m_AllChess[i];

            // 阵营过滤
            if (camp >= 0 && chess.Camp != camp)
                continue;

            // 距离检测
            float distSqr = (chess.transform.position - center).sqrMagnitude;
            if (distSqr <= radiusSqr)
            {
                result.Add(chess);
            }
        }

        return result;
    }

    /// <summary>
    /// 查询指定种族的所有棋子
    /// </summary>
    public List<ChessEntity> GetChessByRace(int raceId)
    {
        List<ChessEntity> result = new List<ChessEntity>();
        for (int i = 0; i < m_AllChess.Count; i++)
        {
            if (m_AllChess[i].HasRace(raceId))
            {
                result.Add(m_AllChess[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// 查询指定职业的所有棋子
    /// </summary>
    public List<ChessEntity> GetChessByClass(int classId)
    {
        List<ChessEntity> result = new List<ChessEntity>();
        for (int i = 0; i < m_AllChess.Count; i++)
        {
            if (m_AllChess[i].HasClass(classId))
            {
                result.Add(m_AllChess[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取指定阵营的棋子数量
    /// </summary>
    public int GetChessCountByCamp(int camp)
    {
        int count = 0;
        for (int i = 0; i < m_AllChess.Count; i++)
        {
            if (m_AllChess[i].Camp == camp)
            {
                count++;
            }
        }
        return count;
    }

    #endregion

    #region 星级进化系统

    /// <summary>
    /// 棋子进阶（占位实现）
    /// </summary>
    /// <param name="entity">要进阶的棋子</param>
    /// <returns>进阶后的棋子，失败返回null</returns>
    public async UniTask<ChessEntity> EvolveChess(ChessEntity entity)
    {
        if (entity == null)
        {
            DebugEx.ErrorModule("SummonChessManager", "EvolveChess failed: entity is null");
            return null;
        }

        var config = entity.Config;

        // 1. 检查是否可以进阶
        if (config.StarLevel >= 3)
        {
            DebugEx.WarningModule(
                "SummonChessManager",
                $"EvolveChess failed: {config.Name} is already 3-star"
            );
            return null;
        }

        if (config.NextStarId == 0)
        {
            DebugEx.WarningModule(
                "SummonChessManager",
                $"EvolveChess failed: {config.Name} has no next star config"
            );
            return null;
        }

        // 2. TODO: 验证材料消耗（未实现）
        // 这里暂时跳过材料检查

        // 3. 获取下一星级配置
        if (!ChessDataManager.Instance.TryGetConfig(config.NextStarId, out var nextConfig))
        {
            DebugEx.ErrorModule(
                "SummonChessManager",
                $"EvolveChess failed: next star config not found, nextStarId={config.NextStarId}"
            );
            return null;
        }

        // 4. 保存当前生命值和法力值比例
        double hpRatio = entity.Attribute.CurrentHp / entity.Attribute.MaxHp;
        double mpRatio = entity.Attribute.CurrentMp / entity.Attribute.MaxMp;

        // 5. 记录位置和阵营
        Vector3 position = entity.transform.position;
        int camp = entity.Camp;

        // 6. 销毁旧棋子
        DestroyChess(entity);

        // 7. 生成新星级棋子
        var newEntity = await SpawnChessAsync(config.NextStarId, position, camp);

        if (newEntity != null)
        {
            // 8. 恢复生命值和法力值比例
            newEntity.Attribute.SetHp(newEntity.Attribute.MaxHp * hpRatio);
            newEntity.Attribute.SetMp(newEntity.Attribute.MaxMp * mpRatio);

            DebugEx.LogModule(
                "SummonChessManager",
                $"EvolveChess success: {config.Name} ({config.StarLevel}星) -> {nextConfig.Name} ({nextConfig.StarLevel}星)"
            );
        }

        return newEntity;
    }

    #endregion

    #region 资源管理

    /// <summary>
    /// 清空预制体缓存
    /// </summary>
    public void ClearPrefabCache()
    {
        m_PrefabCache.Clear();
        DebugEx.LogModule("SummonChessManager", "ClearPrefabCache");
    }

    #endregion

    #region 事件

    /// <summary>
    /// 棋子生成事件
    /// </summary>
    public event Action<ChessEntity> OnChessSpawned;

    /// <summary>
    /// 棋子销毁事件
    /// </summary>
    public event Action<ChessEntity> OnChessDestroyed;

    #endregion

    #region 调试

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[SummonChessManager] TotalChess={m_AllChess.Count}, CachedPrefabs={m_PrefabCache.Count}";
    }

    #endregion
}
