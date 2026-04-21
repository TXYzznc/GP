using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.DataTable;
using UnityEngine;

/// <summary>
/// 场景生成管理器
/// 根据 MapSpawnTable 配置和 ResourceConfigTable，动态生成敌人/宝箱
/// 由 GameProcedure 自动创建和管理
/// </summary>
public class SceneSpawnManager : MonoBehaviour
{
    private int m_MapId;
    private bool m_ShowSpawnLogs = true;

    /// <summary>
    /// 由 GameProcedure 调用初始化
    /// </summary>
    public void Initialize(int mapId)
    {
        m_MapId = mapId;
        DebugEx.Log("SceneSpawn", $"[初始化] SceneSpawnManager.Initialize 被调用，MapId={mapId}");
        SpawnAllAsync().Forget();
    }

    private async UniTask SpawnAllAsync()
    {
        DebugEx.Log("SceneSpawn", "[开始生成] SpawnAllAsync 开始执行");

        // 等一帧确保 DataTable 加载完成
        await UniTask.Yield();
        DebugEx.Log("SceneSpawn", "[Yield完成] 等待一帧后继续");

        // 读表获取配置
        var mapSpawnTable = GF.DataTable.GetDataTable<MapSpawnTable>();
        if (mapSpawnTable == null)
        {
            DebugEx.Log("SceneSpawn", "[错误] MapSpawnTable 未加载");
            return;
        }

        DebugEx.Log("SceneSpawn", "[表加载] MapSpawnTable 已加载");

        // 获取当前地图的所有生成配置
        var mapConfigs = GetMapConfigs(mapSpawnTable);
        DebugEx.Log("SceneSpawn", $"[配置查询] MapId={m_MapId}, 找到 {mapConfigs.Count} 个配置");

        if (mapConfigs.Count == 0)
        {
            DebugEx.Log("SceneSpawn", $"[警告] 地图 {m_MapId} 无生成配置，检查MapSpawnTable是否有数据");
            return;
        }

        // 收集场景中的生成点
        var spawnPoints = FindObjectsOfType<SpawnPoint>();
        DebugEx.Log("SceneSpawn", $"[SpawnPoint查询] 找到 {spawnPoints.Length} 个生成点");

        if (spawnPoints.Length == 0)
        {
            DebugEx.Log("SceneSpawn", "[警告] 场景中无 SpawnPoint，请检查场景配置或使用编辑器工具生成");
            return;
        }

        DebugEx.Log("SceneSpawn", $"[开始生成敌人宝箱] 地图 {m_MapId} 有 {spawnPoints.Length} 个生成点");

        // 按类型分组配置
        var enemyConfigs = new List<MapSpawnTable>();
        var chestConfigs = new List<MapSpawnTable>();

        foreach (var config in mapConfigs)
        {
            if (config.SpawnType == 0)
            {
                enemyConfigs.Add(config);
                DebugEx.Log("SceneSpawn", $"  [敌人配置] Id={config.Id}, TargetId={config.SpawnTargetId}, Weight={config.Weight}");
            }
            else if (config.SpawnType == 1)
            {
                chestConfigs.Add(config);
                DebugEx.Log("SceneSpawn", $"  [宝箱配置] Id={config.Id}, TargetId={config.SpawnTargetId}, Level={config.ChestLevel}, Weight={config.Weight}");
            }
        }

        DebugEx.Log("SceneSpawn", $"[配置分类] 敌人配置数={enemyConfigs.Count}, 宝箱配置数={chestConfigs.Count}");

        // 对每个生成点执行生成
        int spawnCount = 0;
        foreach (var spawnPoint in spawnPoints)
        {
            DebugEx.Log("SceneSpawn", $"[处理生成点] {spawnPoint.name}, Type={spawnPoint.Type}, Pos={spawnPoint.transform.position}");

            if (spawnPoint.Type == SpawnPointType.Enemy && enemyConfigs.Count > 0)
            {
                DebugEx.Log("SceneSpawn", $"  └─ 执行敌人生成");
                await TrySpawnAsync(spawnPoint, enemyConfigs, isEnemy: true);
                spawnCount++;
            }
            else if (spawnPoint.Type == SpawnPointType.TreasureBox && chestConfigs.Count > 0)
            {
                DebugEx.Log("SceneSpawn", $"  └─ 执行宝箱生成");
                await TrySpawnAsync(spawnPoint, chestConfigs, isEnemy: false);
                spawnCount++;
            }
            else
            {
                DebugEx.Log("SceneSpawn", $"  └─ 跳过（无匹配的配置）");
            }
        }

        DebugEx.Log("SceneSpawn", $"[生成完成] 共处理 {spawnCount} 个生成点");
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

    private async UniTask TrySpawnAsync(SpawnPoint spawnPoint, List<MapSpawnTable> configs, bool isEnemy)
    {
        // 加权随机选择一个配置
        var selectedConfig = PickWeightedRandom(configs);
        if (selectedConfig == null)
            return;

        // 尝试找到可用的生成位置（最多 3 次）
        Vector3 spawnPos = Vector3.zero;
        bool foundValidPos = false;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            // 随机偏移
            Vector3 randomOffset = UnityEngine.Random.insideUnitCircle * spawnPoint.Radius;
            Vector3 candidatePos = spawnPoint.transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);

            // NavMesh 采样
            if (UnityEngine.AI.NavMesh.SamplePosition(candidatePos, out UnityEngine.AI.NavMeshHit hit, spawnPoint.NavSampleRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
                foundValidPos = true;
                break;
            }
        }

        if (!foundValidPos)
        {
            if (m_ShowSpawnLogs)
                DebugEx.Log("SceneSpawn", $"生成点 {spawnPoint.name} 无法找到有效 NavMesh 位置");
            return;
        }

        // 获取预制体配置 ID 并异步加载
        int prefabId = 0;

        if (isEnemy)
        {
            prefabId = GetEnemyPrefabId((int)selectedConfig.SpawnTargetId);
        }
        else
        {
            prefabId = GetTreasureBoxPrefabId((int)selectedConfig.SpawnTargetId);
        }

        if (prefabId == 0)
        {
            if (m_ShowSpawnLogs)
                DebugEx.Log("SceneSpawn", $"找不到生成目标 {selectedConfig.SpawnTargetId} 的预制体配置");
            return;
        }

        // 异步加载预制体
        var prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(prefabId);

        if (prefab == null)
        {
            if (m_ShowSpawnLogs)
                DebugEx.Log("SceneSpawn", $"加载预制体失败：prefabId={prefabId}");
            return;
        }

        var spawnedObject = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 调整敌人/宝箱底部贴在 NavMesh 上（计算 Collider 高度偏移）
        AdjustPositionToNavMesh(spawnedObject, spawnPos);

        // 初始化
        if (isEnemy)
        {
            var enemyEntity = spawnedObject.GetComponent<EnemyEntity>();
            if (enemyEntity != null)
            {
                enemyEntity.SetEntityConfigId((int)selectedConfig.SpawnTargetId);
                if (m_ShowSpawnLogs)
                    DebugEx.Log("SceneSpawn", $"生成敌人 {selectedConfig.SpawnTargetId} at {spawnPos}");
            }
        }
        else
        {
            var chest = spawnedObject.GetComponent<TreasureChestInteractable>();
            if (chest != null)
            {
                chest.SetTreasureBoxData((int)selectedConfig.SpawnTargetId, (int)selectedConfig.ChestLevel);
                if (m_ShowSpawnLogs)
                    DebugEx.Log("SceneSpawn", $"生成宝箱 {selectedConfig.SpawnTargetId}(等级{selectedConfig.ChestLevel}) at {spawnPos}");
            }
        }

        await UniTask.Yield();
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
                DebugEx.Log("SceneSpawn", $"EnemyEntityTable 中找不到 ID {enemyEntityTableId}");
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
                DebugEx.Log("SceneSpawn", $"TreasureBoxTable 中找不到 ID {treasureBoxTableId}");
            return 0;
        }

        return (int)treasureBoxData.PrefabId;
    }

    /// <summary>
    /// 调整对象位置，使其底部贴在 NavMesh 上
    /// 通过计算 Collider 的 bounds，调整 Y 坐标
    /// </summary>
    private void AdjustPositionToNavMesh(GameObject obj, Vector3 navMeshSurfacePos)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider == null)
            return;

        // 获取 Collider 的 bounds，计算底部到中心的偏移
        var bounds = collider.bounds;
        float heightOffset = bounds.extents.y; // Collider 底部到中心的距离

        // 调整位置：NavMesh 表面点 + 高度偏移
        Vector3 adjustedPos = new Vector3(navMeshSurfacePos.x, navMeshSurfacePos.y + heightOffset, navMeshSurfacePos.z);
        obj.transform.position = adjustedPos;

        if (m_ShowSpawnLogs)
            DebugEx.Log("SceneSpawn", $"位置已调整：底部贴 NavMesh，高度偏移={heightOffset:F2}m");
    }

    private MapSpawnTable PickWeightedRandom(List<MapSpawnTable> configs)
    {
        if (configs.Count == 0)
            return null;

        if (configs.Count == 1)
            return configs[0];

        // 计算总权重
        int totalWeight = 0;
        foreach (var config in configs)
        {
            totalWeight += (int)config.Weight;
        }

        if (totalWeight <= 0)
            return configs[0];

        // 随机选择
        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int accumulated = 0;

        foreach (var config in configs)
        {
            accumulated += (int)config.Weight;
            if (randomValue < accumulated)
                return config;
        }

        return configs[configs.Count - 1];
    }
}
