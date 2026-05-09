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
    /// 根据敌人类型（普通/Boss）加载不同的配置
    /// </summary>
    /// <param name="enemyTableId">EnemyTable 配置ID</param>
    /// <param name="enemyGuid">对应 EnemyEntity 的 GUID（用于读取历史 HP）</param>
    public void LoadFromEnemyTable(int enemyTableId, string enemyGuid = null)
    {
        var enemyTable = GF.DataTable.GetDataTable<EnemyTable>();
        if (enemyTable == null)
        {
            DebugEx.Error("EnemySpawnManager", "EnemyTable 数据表未加载");
            return;
        }

        var enemyData = enemyTable.GetDataRow(enemyTableId);
        if (enemyData == null)
        {
            DebugEx.Error("EnemySpawnManager", $"未找到敌人配置 ID={enemyTableId}");
            return;
        }

        var entityTable = GF.DataTable.GetDataTable<EnemyEntityTable>();
        if (entityTable == null)
        {
            DebugEx.Error("EnemySpawnManager", "EnemyEntityTable 数据表未加载");
            return;
        }

        var entityRow = entityTable.GetDataRow(enemyTableId);
        if (entityRow == null)
        {
            DebugEx.Error("EnemySpawnManager", $"未找到敌人实体配置 ID={enemyTableId}");
            return;
        }

        int enemyDifficulty = entityRow.EnemyDifficulty;
        int enemyType = entityRow.EnemyType;
        List<int> randomizedChessIds = new List<int>();

        // 根据敌人类型加载配置
        if (enemyType == 2)
        {
            // Boss 类型：直接使用 ChessIds_Boss，解析波次数据
            if (string.IsNullOrEmpty(enemyData.ChessIds_Boss))
            {
                DebugEx.Warning("EnemySpawnManager", $"Boss 敌人 {enemyData.EnemyName} 没有配置 ChessIds_Boss");
                return;
            }

            randomizedChessIds = ParseBossWaves(enemyData.ChessIds_Boss);
            DebugEx.Log("EnemySpawnManager",
                $"加载 Boss 敌人: {enemyData.EnemyName}, 难度={enemyDifficulty}, 敌人总数={randomizedChessIds.Count}");
        }
        else
        {
            // 普通敌人：从 CombatDifficultyRule 读取配置
            var difficultyTable = GF.DataTable.GetDataTable<CombatDifficultyRule>();
            if (difficultyTable == null)
            {
                DebugEx.Error("EnemySpawnManager", "CombatDifficultyRule 数据表未加载");
                return;
            }

            var difficultyRow = difficultyTable.GetDataRow(enemyDifficulty);
            if (difficultyRow == null)
            {
                DebugEx.Error("EnemySpawnManager", $"未找到难度规则 Difficulty={enemyDifficulty}");
                return;
            }

            if (enemyData.ChessIds == null || enemyData.ChessIds.Length == 0)
            {
                DebugEx.Warning("EnemySpawnManager", "敌人配置中没有棋子数据");
                return;
            }

            // 根据难度规则随机确定敌人数量
            int minPopulation = Mathf.Max(1, difficultyRow.MinPopulation);
            int maxPopulation = Mathf.Max(minPopulation, difficultyRow.MaxPopulation);
            int targetCount = Random.Range(minPopulation, maxPopulation + 1);

            // 从 ChessIds 中随机生成目标数量的敌人
            for (int i = 0; i < targetCount; i++)
            {
                int randomIndex = Random.Range(0, enemyData.ChessIds.Length);
                randomizedChessIds.Add(enemyData.ChessIds[randomIndex]);
            }

            DebugEx.Log("EnemySpawnManager",
                $"加载普通敌人: {enemyData.EnemyName}, 难度={enemyDifficulty}, 敌人数量={targetCount} (范围 {minPopulation}-{maxPopulation})");
        }

        m_CurrentWave = new EnemyWaveConfig
        {
            WaveId = enemyTableId,
            EnemyChessIds = randomizedChessIds,
            FormationType = enemyData.FormationType,
            Spacing = enemyData.Spacing,
            EnemyDifficulty = enemyDifficulty
        };

        m_CurrentEnemyGuid = enemyGuid;
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

        DebugEx.Log("EnemySpawnManager",
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
            DebugEx.Warning("EnemySpawnManager", "没有敌人数据，跳过生成");
            return;
        }

        DebugEx.Log("EnemySpawnManager",
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

        DebugEx.Log("EnemySpawnManager",
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
            DebugEx.Error("EnemySpawnManager", "SummonChessManager.Instance is null");
            return null;
        }

        DebugEx.Log("EnemySpawnManager", $"⭐ 开始生成敌人: ChessId={chessId}, Position={position}, Camp={ENEMY_CAMP}");

        var entity = await SummonChessManager.Instance.SpawnChessAsync(chessId, position, ENEMY_CAMP);
        if (entity != null)
        {
            DebugEx.Log("EnemySpawnManager", $"✅ 敌人生成完成: {entity.Config.Name}, Camp={entity.Camp}, IsValid={entity != null}");

            // 根据敌方难度设置棋子等级
            int difficultyRank = CalculateRankFromDifficulty(m_CurrentWave.EnemyDifficulty);
            if (difficultyRank > 1)
            {
                entity.SetRank(difficultyRank);
                // 更新全局状态
                GlobalChessManager.Instance.UpdateChessLevelAndExp(chessId, difficultyRank, 0);
                DebugEx.Log("EnemySpawnManager",
                    $"敌人生成成功 ID={chessId}, Name={entity.Config.Name}, 难度={m_CurrentWave.EnemyDifficulty}, 等级={difficultyRank}");
            }
            else
            {
                DebugEx.Log("EnemySpawnManager",
                    $"敌人生成成功 ID={chessId}, Name={entity.Config.Name}, 难度={m_CurrentWave.EnemyDifficulty}, 等级=1");
            }

            // 【关键】应用难度系数到敌人基础属性（敌人真实强度的体现）
            // 从 CombatDifficultyRule 查询该难度等级的 EnemyDifficultyCoef（0.4-5.0）
            // 应用到敌人的 6 大基础属性：HP、MP、ATK、Armor、MagicResist、SpellPower
            // 这使得敌人的属性成为"真实基础属性"，所有后续加成（继承、装备、Buff）都基于这个值
            float difficultyCoef = GetEnemyDifficultyCoef(m_CurrentWave.EnemyDifficulty);
            if (difficultyCoef > 0 && entity.Attribute != null)
            {
                entity.Attribute.ApplyDifficultyCoef(difficultyCoef, entity.Config, entity.Rank);
                DebugEx.Log("EnemySpawnManager",
                    $"敌人难度系数已应用: {entity.Config.Name}, 难度等级={m_CurrentWave.EnemyDifficulty}, 系数={difficultyCoef:F2}");
            }
        }
        else
        {
            DebugEx.Error("EnemySpawnManager", $"❌ 敌人生成失败 ID={chessId}");
        }

        return entity;
    }

    /// <summary>
    /// 根据难度等级计算棋子等级
    /// 难度 1-3 → 等级1，难度 4-6 → 等级2，难度 7-10 → 等级3
    /// </summary>
    private int CalculateRankFromDifficulty(int difficultyLevel)
    {
        // 将 1-10 的难度等级均匀分配到 1-3 的棋子等级
        if (difficultyLevel <= 3)
            return 1;
        else if (difficultyLevel <= 6)
            return 2;
        else
            return 3;
    }

    /// <summary>
    /// 从 CombatDifficultyRule 获取敌人难度系数（0.4-5.0）
    ///
    /// 【难度系数的含义】
    /// 难度1: 0.4× (新手难度，敌人属性 40%)
    /// 难度2: 0.6×
    /// 难度3: 0.8×
    /// 难度4: 1.0× (平衡难度)
    /// 难度5: 1.4× (开始困难)
    /// ...
    /// 难度10: 5.0× (极难，敌人属性 500%)
    ///
    /// 【应用对象】
    /// 应用到敌人的 6 大基础属性：MaxHp、MaxMp、AtkDamage、Armor、MagicResist、SpellPower
    /// 这确保高难度敌人确实更强、更耐打，不仅仅是靠数量堆砌
    /// </summary>
    private float GetEnemyDifficultyCoef(int difficultyLevel)
    {
        var difficultyTable = GF.DataTable.GetDataTable<CombatDifficultyRule>();
        if (difficultyTable == null)
        {
            DebugEx.Warning("EnemySpawnManager", "CombatDifficultyRule 数据表未加载");
            return 1f;
        }

        var difficultyRow = difficultyTable.GetDataRow(difficultyLevel);
        if (difficultyRow == null)
        {
            DebugEx.Warning("EnemySpawnManager", $"未找到难度等级 {difficultyLevel} 的配置");
            return 1f;
        }

        return difficultyRow.EnemyDifficultyCoef;
    }

    /// <summary>
    /// 解析 Boss 波次配置字符串
    /// 格式：波次通过 | 分隔，敌人通过 , 分隔
    /// 示例：1,3,1|2,3,2 表示第一波[1,3,1]，第二波[2,3,2]
    /// </summary>
    private List<int> ParseBossWaves(string bossWaveConfig)
    {
        var result = new List<int>();

        if (string.IsNullOrEmpty(bossWaveConfig))
            return result;

        var waves = bossWaveConfig.Split('|');

        foreach (var wave in waves)
        {
            if (string.IsNullOrWhiteSpace(wave))
                continue;

            var chessIds = wave.Split(',');
            foreach (var chessIdStr in chessIds)
            {
                if (int.TryParse(chessIdStr.Trim(), out int chessId))
                {
                    result.Add(chessId);
                }
                else
                {
                    DebugEx.Warning("EnemySpawnManager", $"无法解析Boss波次敌人ID: {chessIdStr}");
                }
            }
        }

        return result;
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
        DebugEx.Log("EnemySpawnManager", "已销毁所有敌人");
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
        DebugEx.Log("EnemySpawnManager", "已清理");
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
            DebugEx.Log("EnemySpawnManager",
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
    public int EnemyDifficulty;  // 敌方实体的难度系数（1-5）
}
