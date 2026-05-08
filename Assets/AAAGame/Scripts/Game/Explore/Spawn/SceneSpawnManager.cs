using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.DataTable;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 场景生成管理器
/// 根据 MapSpawnTable 配置和 ResourceConfigTable，动态生成敌人/宝箱
/// 由 GameProcedure 自动创建和管理
/// </summary>
public class SceneSpawnManager : MonoBehaviour
{
    private int m_MapId;
    private bool m_ShowSpawnLogs = true;

    /// <summary>生成统计数据</summary>
    private SpawnStatistics m_Statistics = new();

    /// <summary>缓存的NavMesh三角剖分（性能优化）</summary>
    private NavMeshTriangulation m_CachedTriangulation;

    #region 敌人生成参数

    [Header("敌人生成参数")]
    [SerializeField]
    [Tooltip("敌人最大尝试次数")]
    private int m_EnemyMaxAttempts = 100;

    [SerializeField]
    [Tooltip("敌人安全区域检查半径")]
    private float m_EnemySafetyRadius = 0.5f;

    [SerializeField]
    [Range(1, 5)]
    [Tooltip("敌人安全区域采样密度")]
    private int m_EnemyGridSampleDensity = 2;

    [SerializeField]
    [Tooltip("敌人之间的最小间隔距离")]
    private float m_EnemyMinSpacing = 1.5f;

    #endregion

    #region 宝箱生成参数

    [Header("宝箱生成参数")]
    [SerializeField]
    [Tooltip("宝箱最大尝试次数")]
    private int m_ChestMaxAttempts = 50;

    [SerializeField]
    [Tooltip("宝箱之间的最小间隔距离")]
    private float m_ChestMinSpacing = 2.0f;

    #endregion

    /// <summary>已生成的敌人位置列表</summary>
    private List<Vector3> m_SpawnedEnemyPositions = new();

    /// <summary>已生成的宝箱位置列表</summary>
    private List<Vector3> m_SpawnedChestPositions = new();

    /// <summary>生成统计数据结构</summary>
    private class SpawnStatistics
    {
        public int TotalSpawnPoints = 0;
        public int SuccessfulSpawns = 0;
        public int NavMeshSampleFailures = 0;
        public int SafetyCheckFailures = 0;
        public int PrefabLoadFailures = 0;
        public int InstantiateFailures = 0;

        public void Reset()
        {
            TotalSpawnPoints = 0;
            SuccessfulSpawns = 0;
            NavMeshSampleFailures = 0;
            SafetyCheckFailures = 0;
            PrefabLoadFailures = 0;
            InstantiateFailures = 0;
        }

        public string GetReport()
        {
            return $"\n" +
                $"  ├─ 生成点总数: {TotalSpawnPoints}\n" +
                $"  ├─ 成功: {SuccessfulSpawns}\n" +
                $"  ├─ NavMesh采样失败: {NavMeshSampleFailures}\n" +
                $"  ├─ 安全检查失败: {SafetyCheckFailures}\n" +
                $"  ├─ 预制体加载失败: {PrefabLoadFailures}\n" +
                $"  └─ 实例化失败: {InstantiateFailures}";
        }
    }

    /// <summary>
    /// 由 GameProcedure 调用初始化
    /// </summary>
    public void Initialize(int mapId)
    {
        m_MapId = mapId;
        DebugEx.Log("SceneSpawnManager", $"[初始化] SceneSpawnManager.Initialize 被调用，MapId={mapId}");
        SpawnAllAsync().Forget();
    }

    private async UniTask SpawnAllAsync()
    {
        m_Statistics.Reset();

        await UniTask.Yield();

        // 初始化 NavMesh 缓存
        m_CachedTriangulation = NavMesh.CalculateTriangulation();
        if (m_CachedTriangulation.indices.Length == 0)
        {
            DebugEx.Error("SceneSpawnManager", "NavMesh 为空或未烘烤");
            return;
        }

        // 读表获取地图生成配置
        var mapSpawnTable = GF.DataTable.GetDataTable<MapSpawnTable>();
        if (mapSpawnTable == null)
        {
            DebugEx.Error("SceneSpawnManager", "MapSpawnTable 未加载");
            return;
        }

        var mapConfig = mapSpawnTable.GetDataRow(m_MapId);
        if (mapConfig == null)
        {
            DebugEx.Warning("SceneSpawnManager", $"地图 {m_MapId} 在 MapSpawnTable 中无配置");
            return;
        }

        // 解析等级系数
        float levelCoefficient = SpawnConfigParser.RandomFromRangeFloat(mapConfig.LevelCoefficient);

        DebugEx.Log("SceneSpawnManager", $"========== 场景对象生成开始 ==========\n" +
            $"  ├─ MapId: {m_MapId}\n" +
            $"  ├─ 等级系数: {levelCoefficient:F2} (范围 {mapConfig.LevelCoefficient[0]}-{mapConfig.LevelCoefficient[1]})\n" +
            $"  ├─ NavMesh三角形数: {m_CachedTriangulation.indices.Length / 3}\n" +
            $"  ├─ 敌人参数: 最大尝试={m_EnemyMaxAttempts}, 安全半径={m_EnemySafetyRadius:F2}, 最小间隔={m_EnemyMinSpacing:F2}\n" +
            $"  └─ 宝箱参数: 最大尝试={m_ChestMaxAttempts}, 最小间隔={m_ChestMinSpacing:F2}");

        // 解析敌人配置
        var enemyWeightDict = SpawnConfigParser.ParseWeightedConfig(mapConfig.SpawnEnemys);
        int enemyCount = SpawnConfigParser.RandomFromRange(mapConfig.EnemyNums);

        if (enemyWeightDict.Count > 0 && enemyCount > 0)
        {
            m_Statistics.TotalSpawnPoints = enemyCount;
            DebugEx.Log("SceneSpawnManager",
                $"[生成敌人] 敌人类型数={enemyWeightDict.Count}, 目标数量={enemyCount}");

            for (int i = 0; i < enemyCount; i++)
            {
                int enemyId = SpawnConfigParser.PickWeightedRandom(enemyWeightDict);
                await TrySpawnEnemyAsync(enemyId, levelCoefficient);
            }
        }

        // 解析宝箱配置
        var chestWeightDict = SpawnConfigParser.ParseWeightedConfig(mapConfig.SpawnTreasures);
        int chestCount = SpawnConfigParser.RandomFromRange(mapConfig.TreasureNums);

        if (chestWeightDict.Count > 0 && chestCount > 0)
        {
            m_Statistics.TotalSpawnPoints += chestCount;
            DebugEx.Log("SceneSpawnManager",
                $"[生成宝箱] 宝箱类型数={chestWeightDict.Count}, 目标数量={chestCount}");

            for (int i = 0; i < chestCount; i++)
            {
                int chestId = SpawnConfigParser.PickWeightedRandom(chestWeightDict);
                await TrySpawnChestAsync(chestId, levelCoefficient);
            }
        }

        string resultMessage = $"========== 场景对象生成完成 ==========\n" +
            $"  ├─ 成功: {m_Statistics.SuccessfulSpawns}/{m_Statistics.TotalSpawnPoints}\n" +
            $"{m_Statistics.GetReport()}";

        if (m_Statistics.SuccessfulSpawns == m_Statistics.TotalSpawnPoints)
            DebugEx.Success("SceneSpawnManager", resultMessage);
        else if (m_Statistics.SuccessfulSpawns > 0)
            DebugEx.Warning("SceneSpawnManager", resultMessage);
        else
            DebugEx.Error("SceneSpawnManager", resultMessage);
    }

    /// <summary>
    /// 生成单个敌人
    /// </summary>
    private async UniTask TrySpawnEnemyAsync(int enemyId, float levelCoefficient)
    {
        // 获取敌人基础难度
        var enemyEntityTable = GF.DataTable.GetDataTable<EnemyEntityTable>();
        if (enemyEntityTable == null)
        {
            m_Statistics.InstantiateFailures++;
            DebugEx.Warning("SceneSpawnManager", "EnemyEntityTable 未加载");
            return;
        }

        var enemyEntityRow = enemyEntityTable.GetDataRow(enemyId);
        if (enemyEntityRow == null)
        {
            m_Statistics.InstantiateFailures++;
            DebugEx.Warning("SceneSpawnManager", $"敌人ID {enemyId} 在 EnemyEntityTable 中不存在");
            return;
        }

        // 获取玩家等级
        var saveData = PlayerAccountDataManager.Instance?.CurrentSaveData;
        if (saveData == null)
        {
            m_Statistics.InstantiateFailures++;
            DebugEx.Warning("SceneSpawnManager", "无法获取玩家存档数据");
            return;
        }

        // 计算难度（新算法：非线性，6:4权重）
        float difficulty = DifficultyCalculator.CalculateEnemyDifficulty(
            levelCoefficient,
            saveData.GlobalLevel,
            enemyEntityRow.EnemyDifficulty
        );

        // 映射到 1-10 级别
        int difficultyLevel = DifficultyCalculator.MapDifficultyToLevel(difficulty);

        // 创建配置对象用于生成，包含难度等级
        var config = new { SpawnTargetId = (long)enemyId, ComputedDifficultyLevel = difficultyLevel };
        await TrySpawnAsync(config, isEnemy: true);
    }

    /// <summary>
    /// 生成单个宝箱
    /// </summary>
    private async UniTask TrySpawnChestAsync(int chestId, float levelCoefficient)
    {
        // 创建配置对象用于生成（需要传递等级系数给 SetTreasureBoxData）
        var config = new { SpawnTargetId = (long)chestId, LevelCoefficient = levelCoefficient };
        await TrySpawnAsync(config, isEnemy: false);
    }

    private async UniTask TrySpawnAsync(dynamic config, bool isEnemy, float difficulty = 0f, float levelCoefficient = 0f)
    {
        // 获取对应类型的参数
        int maxAttempts = isEnemy ? m_EnemyMaxAttempts : m_ChestMaxAttempts;
        float minSpacing = isEnemy ? m_EnemyMinSpacing : m_ChestMinSpacing;
        var existingPositions = isEnemy ? m_SpawnedEnemyPositions : m_SpawnedChestPositions;

        // 尝试在 NavMesh 上找到有效位置
        if (!TryFindValidPosition(maxAttempts, minSpacing, existingPositions, isEnemy, out Vector3 spawnPos))
        {
            m_Statistics.NavMeshSampleFailures++;
            DebugEx.Warning("SceneSpawnManager",
                $"❌ {(isEnemy ? "敌人" : "宝箱")}生成失败: 无法找到有效位置");
            return;
        }

        // 记录生成位置
        existingPositions.Add(spawnPos);

        // 获取预制体 ID
        int targetId = (int)config.SpawnTargetId;
        int prefabId = isEnemy
            ? GetEnemyPrefabId(targetId)
            : GetTreasureBoxPrefabId(targetId);

        if (prefabId == 0)
        {
            m_Statistics.PrefabLoadFailures++;
            DebugEx.Warning("SceneSpawnManager",
                $"❌ 找不到预制体配置: 目标ID={config.SpawnTargetId}");
            return;
        }

        // 异步加载预制体
        var prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(prefabId);
        if (prefab == null)
        {
            m_Statistics.PrefabLoadFailures++;
            DebugEx.Warning("SceneSpawnManager",
                $"❌ 预制体加载失败: prefabId={prefabId}");
            return;
        }

        try
        {
            var spawnedObject = Instantiate(prefab, spawnPos, Quaternion.identity);

            if (spawnedObject == null)
            {
                m_Statistics.InstantiateFailures++;
                DebugEx.Error("SceneSpawnManager", "❌ 生成失败: Instantiate 返回 null");
                return;
            }

            AdjustPositionToNavMesh(spawnedObject, spawnPos);

            // 初始化
            if (isEnemy)
            {
                if (!spawnedObject.TryGetComponent<EnemyEntity>(out var enemyEntity))
                {
                    m_Statistics.InstantiateFailures++;
                    DebugEx.Warning("SceneSpawnManager",
                        $"⚠️ 敌人预制体缺少 EnemyEntity 组件");
                    DestroyImmediate(spawnedObject);
                    return;
                }

                try
                {
                    enemyEntity.SetEntityConfigId(targetId);

                    // 设置动态难度等级（来自dynamic对象或默认值）
                    int difficultyLevel = 0;
                    if (config.ComputedDifficultyLevel != null)
                        difficultyLevel = (int)config.ComputedDifficultyLevel;
                    if (difficultyLevel > 0)
                        enemyEntity.SetComputedDifficultyLevel(difficultyLevel);

                    m_Statistics.SuccessfulSpawns++;
                    DebugEx.Success("SceneSpawnManager",
                        $"✓ 敌人生成成功: ID={targetId}, 难度等级={difficultyLevel}\n" +
                        $"  └─ 位置: ({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2})");
                }
                catch (System.Exception initEx)
                {
                    m_Statistics.InstantiateFailures++;
                    DebugEx.Error("SceneSpawnManager",
                        $"❌ 敌人初始化失败: {initEx.GetType().Name}\n" +
                        $"  ├─ ID: {targetId}\n" +
                        $"  └─ {initEx.Message}");
                    DestroyImmediate(spawnedObject);
                }
            }
            else
            {
                if (!spawnedObject.TryGetComponent<TreasureChestInteractable>(out var chest))
                {
                    m_Statistics.InstantiateFailures++;
                    DebugEx.Warning("SceneSpawnManager",
                        $"⚠️ 宝箱预制体缺少 TreasureChestInteractable 组件");
                    DestroyImmediate(spawnedObject);
                    return;
                }

                try
                {
                    // 获取等级系数（来自dynamic对象或默认值）
                    float levelCoeff = 100f; // 默认值
                    if (config.LevelCoefficient != null)
                        levelCoeff = (float)config.LevelCoefficient;

                    chest.SetTreasureBoxData(targetId, levelCoeff);
                    m_Statistics.SuccessfulSpawns++;
                    DebugEx.Success("SceneSpawnManager",
                        $"✓ 宝箱生成成功: ID={targetId} (等级系数{levelCoeff:F2})\n" +
                        $"  └─ 位置: ({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2})");
                }
                catch (System.Exception initEx)
                {
                    m_Statistics.InstantiateFailures++;
                    DebugEx.Error("SceneSpawnManager",
                        $"❌ 宝箱初始化失败: {initEx.GetType().Name}\n" +
                        $"  ├─ ID: {targetId}\n" +
                        $"  └─ {initEx.Message}");
                    DestroyImmediate(spawnedObject);
                }
            }
        }
        catch (System.Exception ex)
        {
            m_Statistics.InstantiateFailures++;
            DebugEx.Error("SceneSpawnManager",
                $"❌ 对象生成失败: {ex.GetType().Name}\n" +
                $"  └─ {ex.Message}\n\n堆栈:\n{ex.StackTrace}");
        }

        await UniTask.Yield();
    }

    /// <summary>
    /// 直接在 NavMesh 上随机找到有效位置（参照 EnemySpawnDebugger 设计）
    /// </summary>
    private bool TryFindValidPosition(int maxAttempts, float minSpacing, List<Vector3> existingPositions, bool isEnemy, out Vector3 result)
    {
        result = Vector3.zero;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            attempts++;

            // 在 NavMesh 上随机找一个点
            if (!RandomNavMeshPoint(out Vector3 randomPos))
            {
                m_Statistics.NavMeshSampleFailures++;
                continue;
            }

            // 检查与现有位置的间隔
            if (!IsValidSpacing(randomPos, existingPositions, minSpacing))
                continue;

            // 敌人需要安全区域检查，宝箱不需要
            if (isEnemy && !IsAreaSafe(randomPos))
            {
                m_Statistics.SafetyCheckFailures++;
                continue;
            }

            result = randomPos;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查新位置与已有位置的间隔是否满足要求
    /// </summary>
    private bool IsValidSpacing(Vector3 pos, List<Vector3> existingPositions, float minSpacing)
    {
        float minSpacingSqr = minSpacing * minSpacing;

        foreach (var existingPos in existingPositions)
        {
            if ((pos - existingPos).sqrMagnitude < minSpacingSqr)
            {
                m_Statistics.SafetyCheckFailures++;
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 在 NavMesh 上随机找一个点（使用缓存的三角剖分）
    /// 完全复用 EnemySpawnDebugger 的逻辑
    /// </summary>
    private bool RandomNavMeshPoint(out Vector3 result)
    {
        result = Vector3.zero;

        if (m_CachedTriangulation.indices.Length == 0)
            return false;

        // 随机选择一个三角形
        int triangleIndex = Random.Range(0, m_CachedTriangulation.indices.Length / 3);
        int vertIndex = triangleIndex * 3;

        Vector3 v0 = m_CachedTriangulation.vertices[m_CachedTriangulation.indices[vertIndex]];
        Vector3 v1 = m_CachedTriangulation.vertices[m_CachedTriangulation.indices[vertIndex + 1]];
        Vector3 v2 = m_CachedTriangulation.vertices[m_CachedTriangulation.indices[vertIndex + 2]];

        // 在三角形内随机生成点（使用重心坐标）
        float r1 = Random.value;
        float r2 = Random.value;

        if (r1 + r2 > 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }

        result = v0 + r1 * (v1 - v0) + r2 * (v2 - v0);
        return true;
    }

    /// <summary>
    /// 检查敌人位置周围的安全区域（所有采样点都必须在NavMesh上）
    /// 只用于敌人，宝箱不需要此检查
    /// </summary>
    private bool IsAreaSafe(Vector3 centerPos)
    {
        float step = m_EnemySafetyRadius * 2f / (m_EnemyGridSampleDensity + 1);

        for (int x = -m_EnemyGridSampleDensity; x <= m_EnemyGridSampleDensity; x++)
        {
            for (int z = -m_EnemyGridSampleDensity; z <= m_EnemyGridSampleDensity; z++)
            {
                Vector3 samplePos = centerPos + new Vector3(x * step, 0, z * step);
                if (!NavMesh.SamplePosition(samplePos, out _, 0f, NavMesh.AllAreas))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 从 EnemyEntityTable 获取预制体资源 ID
    /// </summary>
    private int GetEnemyPrefabId(int enemyEntityTableId)
    {
        var enemyEntityTable = GF.DataTable.GetDataTable<EnemyEntityTable>();
        if (enemyEntityTable == null)
            return 0;

        var enemyData = enemyEntityTable.GetDataRow(enemyEntityTableId);
        if (enemyData == null)
        {
            if (m_ShowSpawnLogs)
                DebugEx.Log("SceneSpawnManager", $"EnemyEntityTable 中找不到 ID {enemyEntityTableId}");
            return 0;
        }

        return (int)enemyData.PrefabId;
    }

    /// <summary>
    /// 从 TreasureBoxTable 获取预制体资源 ID
    /// </summary>
    private int GetTreasureBoxPrefabId(int treasureBoxTableId)
    {
        var treasureBoxTable = GF.DataTable.GetDataTable<TreasureBoxTable>();
        if (treasureBoxTable == null)
            return 0;

        var treasureBoxData = treasureBoxTable.GetDataRow(treasureBoxTableId);
        if (treasureBoxData == null)
        {
            if (m_ShowSpawnLogs)
                DebugEx.Log("SceneSpawnManager", $"TreasureBoxTable 中找不到 ID {treasureBoxTableId}");
            return 0;
        }

        return (int)treasureBoxData.PrefabId;
    }

    /// <summary>
    /// 调整对象位置，使其底部贴在 NavMesh 上
    /// 使用 EntityPositionHelper 工具类正确计算底部偏移
    /// </summary>
    private void AdjustPositionToNavMesh(GameObject obj, Vector3 navMeshSurfacePos)
    {
        float heightOffset = EntityPositionHelper.CalculateBottomOffset(obj);
        Vector3 bottomPos = EntityPositionHelper.GetBottomPosition(obj);
        Vector3 adjustedPos = navMeshSurfacePos + Vector3.up * heightOffset;

        DebugEx.Log("SceneSpawnManager", $"[调整位置计算] {obj.name}");
        DebugEx.Log("SceneSpawnManager", $"  NavMesh表面Y={navMeshSurfacePos.y:F3}");
        DebugEx.Log("SceneSpawnManager", $"  当前底部Y={bottomPos.y:F3}");
        DebugEx.Log("SceneSpawnManager", $"  底部偏移={heightOffset:F3}");
        DebugEx.Log("SceneSpawnManager", $"  目标Y位置={adjustedPos.y:F3}");

        obj.transform.position = adjustedPos;

        Vector3 bottomAfter = EntityPositionHelper.GetBottomPosition(obj);
        DebugEx.Log("SceneSpawnManager", $"[调整完成] 调整后底部Y={bottomAfter.y:F3}");
    }

}
