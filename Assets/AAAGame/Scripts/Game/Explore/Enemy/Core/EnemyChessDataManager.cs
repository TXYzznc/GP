using System.Collections.Generic;

/// <summary>
/// 敌人棋子数据全局管理器（纯 C# 单例）
/// 以 "{entityGuid}_{slotIndex}" 为 key 独立管理每个敌人实体的棋子状态
/// 与 GlobalChessManager 完全独立
/// </summary>
public class EnemyChessDataManager
{
    #region 单例

    private static EnemyChessDataManager s_Instance;

    public static EnemyChessDataManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new EnemyChessDataManager();
            }
            return s_Instance;
        }
    }

    private EnemyChessDataManager()
    {
        m_StateDict = new Dictionary<string, EnemyChessState>();
    }

    #endregion

    #region 私有字段

    private readonly Dictionary<string, EnemyChessState> m_StateDict;

    #endregion

    #region 公共方法

    /// <summary>
    /// 注册敌人棋子（已存在则跳过，保留当前HP）
    /// </summary>
    public void Register(string entityGuid, int slotIndex, int chessId, double maxHp)
    {
        string key = BuildKey(entityGuid, slotIndex);
        if (m_StateDict.ContainsKey(key))
        {
            DebugEx.LogModule("EnemyChessDataManager", $"棋子 {key} 已存在，跳过注册（保留当前HP）");
            return;
        }

        m_StateDict[key] = new EnemyChessState(chessId, maxHp);
        DebugEx.LogModule("EnemyChessDataManager",
            $"注册棋子 {key}: chessId={chessId}, maxHp={maxHp:F0}");
    }

    /// <summary>
    /// 获取棋子状态，不存在返回 null
    /// </summary>
    public EnemyChessState GetState(string key)
    {
        m_StateDict.TryGetValue(key, out var state);
        return state;
    }

    /// <summary>
    /// 获取棋子状态（通过 guid + slotIndex）
    /// </summary>
    public EnemyChessState GetState(string entityGuid, int slotIndex)
    {
        return GetState(BuildKey(entityGuid, slotIndex));
    }

    /// <summary>
    /// 更新棋子血量
    /// </summary>
    public void UpdateHp(string key, double hp)
    {
        if (m_StateDict.TryGetValue(key, out var state))
        {
            state.CurrentHp = hp;
            DebugEx.LogModule("EnemyChessDataManager", $"更新棋子 {key} HP={hp:F0}");
        }
        else
        {
            DebugEx.WarningModule("EnemyChessDataManager", $"UpdateHp: 找不到棋子 {key}");
        }
    }

    /// <summary>
    /// 清理指定敌人实体的所有棋子数据
    /// </summary>
    public void RemoveAllForEntity(string entityGuid)
    {
        var keysToRemove = new List<string>();
        string prefix = entityGuid + "_";

        foreach (var key in m_StateDict.Keys)
        {
            if (key.StartsWith(prefix))
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            m_StateDict.Remove(key);
        }

        DebugEx.LogModule("EnemyChessDataManager",
            $"已清理敌人 {entityGuid} 的所有棋子数据（共 {keysToRemove.Count} 条）");
    }

    /// <summary>
    /// 构建 key
    /// </summary>
    public static string BuildKey(string entityGuid, int slotIndex)
    {
        return $"{entityGuid}_{slotIndex}";
    }

    #endregion
}
