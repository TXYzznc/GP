using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;

/// <summary>
/// 敌人生成管理器
/// 负责战斗阶段的敌人生成和管理
/// </summary>
public class EnemySpawnManager
{
    #region 单例

    private static EnemySpawnManager s_Instance;
    public static EnemySpawnManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new EnemySpawnManager();
            }
            return s_Instance;
        }
    }

    private EnemySpawnManager() { }

    #endregion

    #region 字段

    /// <summary>当前波次配置</summary>
    private EnemyWaveConfig m_CurrentWave;

    /// <summary>已经生成的敌人实例列表</summary>
    private List<ChessEntity> m_SpawnedEnemies = new List<ChessEntity>();

    /// <summary>当前战斗敌人实体的 GUID（用于从 EnemyChessDataManager 读取 HP）</summary>
    private string m_CurrentEnemyGuid;

    /// <summary>敌人阵营ID</summary>
    private const int ENEMY_CAMP = 1;

    #endregion

    #region 公共方法 - 配置加载

    /// <summary>
    /// 从 EnemyTable 加载战斗配置
    /// </summary>
    /// <param name="enemyTableId">EnemyTable 配置ID</param>
    /// <param name="enemyGuid">对应 EnemyEntity 的 GUID（用于读取历史 HP）</param>
    public void LoadFromEnemyTable(int enemyTableId, string enemyGuid = null)
    {
        var dataTable = GF.DataTable.GetDataTable<EnemyTable>();
        if (dataTable == null)
        {
            DebugEx.ErrorModule("EnemySpawnManager", "EnemyTable 数据表未加载");
            return;
        }

        var enemyData = dataTable.GetDataRow(enemyTableId);
        if (enemyData == null)
        {
            DebugEx.ErrorModule("EnemySpawnManager", $"未找到敌人配置 ID={enemyTableId}");
            return;
        }

        // 从 EnemyTable 读取配置
        if (enemyData.ChessIds == null || enemyData.ChessIds.Length == 0)
        {
            DebugEx.WarningModule("EnemySpawnManager", "敌人配置中没有棋子数据");
            return;
        }

        // 转换为 List
        List<int> chessIds = new List<int>(enemyData.ChessIds);

        m_CurrentWave = new EnemyWaveConfig
        {
            WaveId = enemyTableId,
            EnemyChessIds = chessIds,
            FormationType = enemyData.FormationType,
            Spacing = enemyData.Spacing
        };

        m_CurrentEnemyGuid = enemyGuid;

        DebugEx.LogModule("EnemySpawnManager",
            $"从 EnemyTable 加载配置: ID={enemyTableId}, Name={enemyData.EnemyName}, " +
            $"棋子数量={m_CurrentWave.EnemyChessIds.Count}, 阵型={m_CurrentWave.FormationType}, " +
            $"间距={m_CurrentWave.Spacing}");
    }

    /// <summary>
    /// 从配置表加载敌人波次（旧方法，保留兼容）
    /// </summary>
    public void LoadWaveFromConfig(int waveId, string enemyGuid = null)
    {
        LoadFromEnemyTable(waveId, enemyGuid);
    }

    /// <summary>
    /// 设置本局战斗的敌人数据（手动设置）
    /// </summary>
    /// <param name="enemyChessIds">敌人棋子ID列表</param>
    /// <param name="formationType">阵型类型</param>
    /// <param name="spacing">间距</param>
    public void SetEnemyData(List<int> enemyChessIds, int formationType = 1, float spacing = 2.0f)
    {
        m_CurrentWave = new EnemyWaveConfig
        {
            WaveId = 0,
            EnemyChessIds = new List<int>(enemyChessIds),
            FormationType = formationType,
            Spacing = spacing
        };

        DebugEx.LogModule("EnemySpawnManager",
            $"手动设置敌人数据，数量={enemyChessIds.Count}");
    }

    /// <summary>
    /// 设置本局战斗的敌人数据（单个敌人）
    /// </summary>
    /// <param name="enemyChessId">敌人棋子ID</param>
    public void SetEnemyData(int enemyChessId)
    {
        SetEnemyData(new List<int> { enemyChessId });
    }

    #endregion

    #region 公共方法 - 生成管理

    /// <summary>
    /// 根据配置生成所有敌人（带站位）
    /// </summary>
    public async UniTask SpawnWaveAsync()
    {
        if (m_CurrentWave == null || m_CurrentWave.EnemyChessIds.Count == 0)
        {
            DebugEx.WarningModule("EnemySpawnManager", "没有敌人数据，跳过生成");
            return;
        }

        DebugEx.LogModule("EnemySpawnManager",
            $"开始生成敌人波次，数量={m_CurrentWave.EnemyChessIds.Count}");

        // 获取敌方区域中心点
        Vector3 enemyZoneCenter = BattleArenaManager.Instance.GetEnemyZoneCenter();

        // 计算站位
        List<Vector3> positions = EnemyFormationManager.CalculateFormation(
            enemyZoneCenter,
            m_CurrentWave.EnemyChessIds.Count,
            m_CurrentWave.FormationType,
            m_CurrentWave.Spacing
        );

        // 生成敌人
        for (int i = 0; i < m_CurrentWave.EnemyChessIds.Count; i++)
        {
            int chessId = m_CurrentWave.EnemyChessIds[i];
            Vector3 position = positions[i];

            var entity = await SpawnEnemyAsync(chessId, position);
            if (entity != null)
            {
                m_SpawnedEnemies.Add(entity);

                // 应用历史 HP 并建立 BattleChessManager 映射
                ApplyHistoricalHp(entity, chessId, i);
            }
        }

        DebugEx.LogModule("EnemySpawnManager",
            $"敌人波次生成完成，成功数量={m_SpawnedEnemies.Count}");
    }

    /// <summary>
    /// 生成单个敌人
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成的敌人实例</returns>
    public async UniTask<ChessEntity> SpawnEnemyAsync(int chessId, Vector3 position)
    {
        if (SummonChessManager.Instance == null)
        {
            DebugEx.ErrorModule("EnemySpawnManager", "SummonChessManager.Instance is null");
            return null;
        }

        var entity = await SummonChessManager.Instance.SpawnChessAsync(chessId, position, ENEMY_CAMP);
        if (entity != null)
        {
            DebugEx.LogModule("EnemySpawnManager",
                $"敌人生成成功 ID={chessId}, Name={entity.Config.Name}");
        }
        else
        {
            DebugEx.ErrorModule("EnemySpawnManager", $"敌人生成失败 ID={chessId}");
        }

        return entity;
    }

    /// <summary>
    /// 销毁所有敌人
    /// </summary>
    public void DestroyAllEnemies()
    {
        for (int i = m_SpawnedEnemies.Count - 1; i >= 0; i--)
        {
            var entity = m_SpawnedEnemies[i];
            if (entity != null && SummonChessManager.Instance != null)
            {
                SummonChessManager.Instance.DestroyChess(entity);
            }
        }
        m_SpawnedEnemies.Clear();
        DebugEx.LogModule("EnemySpawnManager", "已销毁所有敌人");
    }

    /// <summary>
    /// 清理数据
    /// 在战斗结束时调用
    /// </summary>
    public void Cleanup()
    {
        m_CurrentWave = null;
        m_CurrentEnemyGuid = null;
        m_SpawnedEnemies.Clear();
        DebugEx.LogModule("EnemySpawnManager", "已清理");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 从 EnemyChessDataManager 读取历史 HP 并应用到棋子，同时向 BattleChessManager 注册 enemy key
    /// </summary>
    private void ApplyHistoricalHp(ChessEntity entity, int chessId, int slotIndex)
    {
        if (string.IsNullOrEmpty(m_CurrentEnemyGuid))
            return;

        string enemyKey = EnemyChessDataManager.BuildKey(m_CurrentEnemyGuid, slotIndex);

        // 向 BattleChessManager 注册 enemy key（注册发生在 SummonChessManager.SpawnChessAsync 内部）
        BattleChessManager.Instance.SetEnemyKeyForChess(chessId, enemyKey);

        // 从 EnemyChessDataManager 读取历史 HP 并覆盖
        var state = EnemyChessDataManager.Instance.GetState(enemyKey);
        if (state != null)
        {
            entity.Attribute.SetHp(state.CurrentHp);
            DebugEx.LogModule("EnemySpawnManager",
                $"敌方棋子 {chessId} 继承历史 HP={state.CurrentHp:F0}/{state.MaxHp:F0} (key={enemyKey})");
        }
    }

    #endregion

    #region 公共方法 - 查询

    /// <summary>
    /// 获取已经生成的敌人列表
    /// </summary>
    public IReadOnlyList<ChessEntity> GetSpawnedEnemies()
    {
        return m_SpawnedEnemies;
    }

    /// <summary>
    /// 获取存活的敌人数量
    /// </summary>
    public int GetAliveEnemyCount()
    {
        int count = 0;
        for (int i = 0; i < m_SpawnedEnemies.Count; i++)
        {
            if (m_SpawnedEnemies[i] != null && m_SpawnedEnemies[i].CurrentState != ChessState.Dead)
            {
                count++;
            }
        }
        return count;
    }

    #endregion
}

/// <summary>
/// 敌人波次配置（临时数据结构，后续替换为配置表）
/// </summary>
public class EnemyWaveConfig
{
    public int WaveId;
    public List<int> EnemyChessIds;
    public int FormationType;
    public float Spacing;
}
