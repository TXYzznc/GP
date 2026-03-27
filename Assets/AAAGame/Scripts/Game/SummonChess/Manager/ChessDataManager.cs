using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 召唤棋子数据管理器
/// 负责加载和管理棋子配置数据
/// </summary>
public class ChessDataManager
{
    #region 单例

    private static ChessDataManager s_Instance;
    public static ChessDataManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new ChessDataManager();
            }
            return s_Instance;
        }
    }

    private ChessDataManager()
    {
        m_ConfigDict = new Dictionary<int, SummonChessConfig>();
    }

    #endregion

    #region 私有字段

    /// <summary>棋子配置字典（ChessId -> Config）</summary>
    private Dictionary<int, SummonChessConfig> m_ConfigDict;

    /// <summary>是否已加载配置</summary>
    private bool m_IsLoaded = false;

    #endregion

    #region 公共属性

    /// <summary>是否已加载配置</summary>
    public bool IsLoaded => m_IsLoaded;

    /// <summary>配置数量</summary>
    public int ConfigCount => m_ConfigDict.Count;

    #endregion

    #region 配置加载

    /// <summary>
    /// 加载配置表
    /// 在游戏启动时调用一次
    /// </summary>
    public void LoadConfigs()
    {
        if (m_IsLoaded)
        {
            DebugEx.WarningModule("ChessDataManager", "配置已加载，跳过重复加载");
            return;
        }

        m_ConfigDict.Clear();

        // 获取配置表数据表
        var dataTable = GF.DataTable.GetDataTable<SummonChessTable>();
        if (dataTable == null)
        {
            DebugEx.ErrorModule("ChessDataManager", "无法获取 SummonChessTable 配置表");
            return;
        }

        int loadedCount = 0;
        int errorCount = 0;

        // 遍历所有数据行
        foreach (var row in dataTable.GetAllDataRows())
        {
            // 构建配置对象
            var config = new SummonChessConfig
            {
                Id = row.Id,
                Name = row.Name,
                Quality = row.Quality,
                PopCost = row.PopCost,
                Description = row.Description,
                Races = row.Races,
                Classes = row.Classes,
                StarLevel = row.StarLevel,
                NextStarId = row.NextStarId,
                PrefabId = row.PrefabId,
                IconId = row.IconId,
                MaxHp = row.MaxHp,
                MaxMp = row.MaxMp,
                InitialMp = row.InitialMp,
                AtkDamage = row.AtkDamage,
                AtkSpeed = row.AtkSpeed,
                AtkRange = row.AtkRange,
                Armor = row.Armor,
                MagicResist = row.MagicResist,
                MoveSpeed = row.MoveSpeed,
                CritRate = row.CritRate,
                CritDamage = row.CritDamage,
                SpellPower = row.SpellPower,
                Shield = row.Shield,
                CooldownReduce = row.CooldownReduce,
                PassiveIds = row.PassiveIds,
                NormalAtkId = row.NormalAtkId,
                Skill1Id = row.Skill1Id,
                Skill2Id = row.Skill2Id,
                AIType = row.AIType,
            };

            // 验证配置
            if (!ValidateConfig(config))
            {
                errorCount++;
                continue;
            }

            // 添加到字典
            m_ConfigDict[config.Id] = config;
            loadedCount++;
        }

        m_IsLoaded = true;

        DebugEx.LogModule(
            "ChessDataManager",
            $"配置加载完成：成功: {loadedCount}, 失败: {errorCount}"
        );
    }

    /// <summary>
    /// 重新加载配置（热更新）
    /// </summary>
    public void ReloadConfigs()
    {
        m_IsLoaded = false;
        LoadConfigs();
    }

    #endregion

    #region 配置查询

    /// <summary>
    /// 获取棋子配置
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <returns>配置数据，不存在则返回null</returns>
    public SummonChessConfig GetConfig(int chessId)
    {
        if (!m_IsLoaded)
        {
            DebugEx.WarningModule("ChessDataManager", "配置尚未加载");
            return null;
        }

        if (m_ConfigDict.TryGetValue(chessId, out var config))
        {
            return config;
        }

        DebugEx.WarningModule("ChessDataManager", $"找不到棋子配置 Id={chessId}");
        return null;
    }

    /// <summary>
    /// 尝试获取棋子配置
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <param name="config">输出配置数据</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetConfig(int chessId, out SummonChessConfig config)
    {
        config = null;

        if (!m_IsLoaded)
        {
            return false;
        }

        return m_ConfigDict.TryGetValue(chessId, out config);
    }

    /// <summary>
    /// 检查棋子是否存在
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <returns>是否存在</returns>
    public bool HasConfig(int chessId)
    {
        if (!m_IsLoaded)
        {
            return false;
        }

        return m_ConfigDict.ContainsKey(chessId);
    }

    /// <summary>
    /// 获取所有棋子ID
    /// </summary>
    /// <returns>棋子ID列表</returns>
    public List<int> GetAllConfigIds()
    {
        if (!m_IsLoaded)
        {
            return new List<int>();
        }

        return new List<int>(m_ConfigDict.Keys);
    }

    /// <summary>
    /// 获取指定品质的棋子配置ID
    /// </summary>
    /// <param name="quality">品质（1-4）</param>
    /// <returns>棋子ID列表</returns>
    public List<int> GetConfigIdsByQuality(int quality)
    {
        var result = new List<int>();

        if (!m_IsLoaded)
        {
            return result;
        }

        foreach (var kvp in m_ConfigDict)
        {
            if (kvp.Value.Quality == quality)
            {
                result.Add(kvp.Key);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定星级的棋子配置ID
    /// </summary>
    /// <param name="starLevel">星级（1-3）</param>
    /// <returns>棋子ID列表</returns>
    public List<int> GetConfigIdsByStarLevel(int starLevel)
    {
        var result = new List<int>();

        if (!m_IsLoaded)
        {
            return result;
        }

        foreach (var kvp in m_ConfigDict)
        {
            if (kvp.Value.StarLevel == starLevel)
            {
                result.Add(kvp.Key);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定种族的棋子配置ID
    /// </summary>
    /// <param name="raceId">种族ID</param>
    /// <returns>棋子ID列表</returns>
    public List<int> GetConfigIdsByRace(int raceId)
    {
        var result = new List<int>();

        if (!m_IsLoaded)
        {
            return result;
        }

        foreach (var kvp in m_ConfigDict)
        {
            var config = kvp.Value;
            if (config.Races != null)
            {
                for (int i = 0; i < config.Races.Length; i++)
                {
                    if (config.Races[i] == raceId)
                    {
                        result.Add(kvp.Key);
                        break;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定职业的棋子配置ID
    /// </summary>
    /// <param name="classId">职业ID</param>
    /// <returns>棋子ID列表</returns>
    public List<int> GetConfigIdsByClass(int classId)
    {
        var result = new List<int>();

        if (!m_IsLoaded)
        {
            return result;
        }

        foreach (var kvp in m_ConfigDict)
        {
            var config = kvp.Value;
            if (config.Classes != null)
            {
                for (int i = 0; i < config.Classes.Length; i++)
                {
                    if (config.Classes[i] == classId)
                    {
                        result.Add(kvp.Key);
                        break;
                    }
                }
            }
        }

        return result;
    }

    #endregion

    #region 配置验证

    /// <summary>
    /// 验证配置数据的有效性
    /// </summary>
    /// <param name="config">棋子配置</param>
    /// <returns>是否有效</returns>
    private bool ValidateConfig(SummonChessConfig config)
    {
        if (config == null)
        {
            DebugEx.ErrorModule("ChessDataManager", "配置对象为null");
            return false;
        }

        // 使用配置类自带的验证方法
        if (!config.Validate(out string errorMsg))
        {
            DebugEx.ErrorModule("ChessDataManager", $"配置验证失败 - {errorMsg}");
            return false;
        }

        // 检查重复ID
        if (m_ConfigDict.ContainsKey(config.Id))
        {
            DebugEx.ErrorModule("ChessDataManager", $"重复的棋子ID {config.Id}");
            return false;
        }

        return true;
    }

    #endregion

    #region 调试方法

    /// <summary>
    /// 打印所有棋子信息（测试用）
    /// </summary>
    public void DebugPrintAllConfigs()
    {
        if (!m_IsLoaded)
        {
            DebugEx.WarningModule("ChessDataManager", "配置尚未加载");
            return;
        }

        DebugEx.LogModule(
            "ChessDataManager",
            $"=== ChessDataManager 配置列表 (共{m_ConfigDict.Count}个) ==="
        );

        foreach (var kvp in m_ConfigDict)
        {
            var config = kvp.Value;
            DebugEx.LogModule(
                "ChessDataManager",
                $"[{config.Id}] {config.Name} - 品质:{config.Quality} 星级:{config.StarLevel} "
                    + $"生命:{config.MaxHp} 攻击:{config.AtkDamage} AI:{config.AIType}"
            );
        }

        DebugEx.LogModule("ChessDataManager", "===========================================");
    }

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[ChessDataManager] 已加载={m_IsLoaded}, 配置数量={m_ConfigDict.Count}";
    }

    #endregion
}
