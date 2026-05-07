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

    /// <summary>是否使用增强检查模式</summary>
    [SerializeField]
    private bool m_UseEnhancedSpawning = true;

    /// <summary>安全区域检查半径</summary>
    [SerializeField]
    private float m_SafetyRadius = 0.5f;

    /// <summary>安全区域采样密度（每轴采样点数）</summary>
    [SerializeField]
    private int m_GridSampleDensity = 2;

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

        DebugEx.Log("SceneSpawnManager", $"========== 场景对象生成开始 ==========\n" +
            $"  ├─ MapId: {m_MapId}\n" +
            $"  ├─ NavMesh三角形数: {m_CachedTriangulation.indices.Length / 3}\n" +
            $"  ├─ 安全检查半径: {m_SafetyRadius:F2}\n" +
            $"  └─ 采样密度: {m_GridSampleDensity}");

        // 读表获取配置
        var mapSpawnTable = GF.DataTable.GetDataTable<MapSpawnTable>();
        if (mapSpawnTable == null)
        {
            DebugEx.Error("SceneSpawnManager", "MapSpawnTable 未加载");
            return;
        }

        var mapConfigs = GetMapConfigs(mapSpawnTable);
        if (mapConfigs.Count == 0)
        {
            DebugEx.Warning("SceneSpawnManager", $"地图 {m_MapId} 无生成配置");
            return;
        }

        // 按类型分组配置
        var enemyConfigs = new List<MapSpawnTable>();
        var chestConfigs = new List<MapSpawnTable>();

        foreach (var config in mapConfigs)
        {
            if (config.SpawnType == 0)
                enemyConfigs.Add(config);
            else if (config.SpawnType == 1)
                chestConfigs.Add(config);
        }

        DebugEx.Log("SceneSpawnManager", $"配置加载: 敌人配置={enemyConfigs.Count}, 宝箱配置={chestConfigs.Count}");

        // 生成敌人和宝箱
        if (enemyConfigs.Count > 0)
        {
            m_Statistics.TotalSpawnPoints = enemyConfigs.Count;
            DebugEx.Log("SceneSpawnManager", $"[生成敌人] 开始生成 {enemyConfigs.Count} 个敌人");
            foreach (var config in enemyConfigs)
            {
                await TrySpawnAsync(config, isEnemy: true);
            }
        }

        if (chestConfigs.Count > 0)
        {
            m_Statistics.TotalSpawnPoints += chestConfigs.Count;
            DebugEx.Log("SceneSpawnManager", $"[生成宝箱] 开始生成 {chestConfigs.Count} 个宝箱");
            foreach (var config in chestConfigs)
            {
                await TrySpawnAsync(config, isEnemy: false);
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

    private List<MapSpawnTable> GetMapConfigs(IDataTable<MapSpawnTable> dataTable)
    {
        var result = new List<MapSpawnTable>();
        var allRows = dataTable.GetAllDataRows();

        foreach (var row in allRows)
        {
            if (row.MapId == m_MapId)
                result.Add(row);
        }

        return result;
    }

    private async UniTask TrySpawnAsync(MapSpawnTable config, bool isEnemy)
    {
        // 尝试在 NavMesh 上找到有效位置
        if (!TryFindValidPosition(out Vector3 spawnPos))
        {
            m_Statistics.NavMeshSampleFailures++;
            DebugEx.Warning("SceneSpawnManager",
                $"❌ 尝试次数过多无法找到有效位置");
            return;
        }

        // 获取预制体 ID
        int prefabId = isEnemy
            ? GetEnemyPrefabId(config.SpawnTargetId)
            : GetTreasureBoxPrefabId(config.SpawnTargetId);

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
            AdjustPositionToNavMesh(spawnedObject, spawnPos);

            // 初始化
            if (isEnemy)
            {
                if (spawnedObject.TryGetComponent<EnemyEntity>(out var enemyEntity))
                {
                    enemyEntity.SetEntityConfigId(config.SpawnTargetId);
                    m_Statistics.SuccessfulSpawns++;
                    DebugEx.Success("SceneSpawnManager",
                        $"✓ 敌人生成成功: {enemyEntity.Config.Name}\n" +
                        $"  └─ 位置: ({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2})");
                }
            }
            else
            {
                if (spawnedObject.TryGetComponent<TreasureChestInteractable>(out var chest))
                {
                    chest.SetTreasureBoxData(config.SpawnTargetId, config.ChestLevel);
                    m_Statistics.SuccessfulSpawns++;
                    DebugEx.Success("SceneSpawnManager",
                        $"✓ 宝箱生成成功 (等级{config.ChestLevel})\n" +
                        $"  └─ 位置: ({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2})");
                }
            }
        }
        catch (System.Exception ex)
        {
            m_Statistics.InstantiateFailures++;
            DebugEx.Error("SceneSpawnManager",
                $"❌ 生成失败: {ex.GetType().Name} - {ex.Message}");
        }

        await UniTask.Yield();
    }

    /// <summary>
    /// 直接在 NavMesh 上随机找到有效位置（参照 EnemySpawnDebugger 设计）
    /// </summary>
    private bool TryFindValidPosition(out Vector3 result)
    {
        result = Vector3.zero;
        int maxAttempts = 100;
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

            // 检查周围安全区域
            if (!IsAreaSafe(randomPos))
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
    /// 检查位置周围的安全区域（所有采样点都必须在NavMesh上）
    /// </summary>
    private bool IsAreaSafe(Vector3 centerPos)
    {
        float step = m_SafetyRadius * 2f / (m_GridSampleDensity + 1);

        for (int x = -m_GridSampleDensity; x <= m_GridSampleDensity; x++)
        {
            for (int z = -m_GridSampleDensity; z <= m_GridSampleDensity; z++)
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
