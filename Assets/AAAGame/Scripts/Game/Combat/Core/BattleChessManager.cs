using System;
using System.Collections.Generic;

/// <summary>
/// 战斗棋子管理器（单例）
/// 负责协调战斗场景中的棋子状态与全局状态（GlobalChessManager）之间的同步
///
/// 工作流程：
/// 1. 棋子生成后调用 RegisterChessEntity()：从全局状态加载 HP 到 ChessAttribute
/// 2. 战斗结束时调用 OnBattleEnd()：将 ChessAttribute 当前 HP 回写到全局状态
/// 3. Buff 管理：提供接口供战斗逻辑调用，Buff 实际由 ChessEntity.BuffManager 管理
/// </summary>
public class BattleChessManager
{
    #region 单例

    private static BattleChessManager s_Instance;

    public static BattleChessManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new BattleChessManager();
            }
            return s_Instance;
        }
    }

    private BattleChessManager()
    {
        m_BattleDataDict = new Dictionary<int, BattleChessData>();
        m_EntityDict = new Dictionary<int, ChessEntity>();
    }

    #endregion

    #region 私有字段

    /// <summary>战斗数据字典（ChessId → BattleChessData）</summary>
    private readonly Dictionary<int, BattleChessData> m_BattleDataDict;

    /// <summary>棋子实体引用字典（ChessId → ChessEntity）</summary>
    private readonly Dictionary<int, ChessEntity> m_EntityDict;


    #endregion

    #region 事件

    /// <summary>战斗棋子数据变化事件（chessId）</summary>
    public event Action<int> OnBattleChessDataChanged;

    #endregion

    #region 战斗初始化

    /// <summary>
    /// 注册棋子实体到战斗管理器
    /// 棋子 Spawn 完成后调用，将全局血量同步到 ChessAttribute
    ///
    /// 注意：如果全局状态中该棋子已死亡（HP=0），则保持死亡状态，
    /// ChessEntity 的 IsDead 为 true，逻辑层应避免将死亡棋子部署到战场
    /// </summary>
    /// <param name="entity">已完成 Initialize 的棋子实体</param>
    public void RegisterChessEntity(ChessEntity entity)
    {
        if (entity == null)
        {
            DebugEx.WarningModule("BattleChessManager", "RegisterChessEntity: entity 为 null");
            return;
        }

        int chessId = entity.ChessId;

        // 从全局状态获取持久化血量
        var globalState = GlobalChessManager.Instance.GetChessState(chessId);

        BattleChessData battleData;

        if (globalState != null)
        {
            // 有全局记录：从全局状态创建战斗数据（Buff 初始化为空）
            battleData = BattleChessData.FromGlobalState(globalState);

            // 将全局血量同步到 ChessAttribute（覆盖 Initialize 时设置的满血值）
            entity.Attribute.SetHp(globalState.CurrentHp);

            DebugEx.LogModule(
                "BattleChessManager",
                $"棋子 {chessId} HP 从全局状态加载：{globalState.CurrentHp:F0}/{globalState.MaxHp:F0}"
            );
        }
        else
        {
            // 无全局记录：使用配置的满血值，并自动注册到全局管理器
            battleData = new BattleChessData(chessId, entity.Attribute.CurrentHp, entity.Attribute.MaxHp);

            GlobalChessManager.Instance.RegisterChess(chessId, entity.Attribute.MaxHp);

            DebugEx.LogModule(
                "BattleChessManager",
                $"棋子 {chessId} 无全局记录，使用满血值 {entity.Attribute.MaxHp:F0} 并注册"
            );
        }

        battleData.Camp = entity.Camp;
        m_BattleDataDict[chessId] = battleData;
        m_EntityDict[chessId] = entity;

        // 订阅事件：自动同步 BattleChessData，无论伤害/Buff 从哪个路径触发
        SubscribeEntityEvents(entity, battleData);
    }

    /// <summary>
    /// 订阅棋子实体的血量和 Buff 事件
    /// 无论是 EffectExecutor、技能类还是 Buff DOT 直接调用 TakeDamage/AddBuff，
    /// BattleChessData 都会通过事件自动保持同步
    /// </summary>
    private void SubscribeEntityEvents(ChessEntity entity, BattleChessData battleData)
    {
        int chessId = entity.ChessId;

        // 血量变化 → 同步到 BattleChessData 并触发 ChessStateEvents
        entity.Attribute.OnHpChanged += (oldHp, newHp) =>
        {
            battleData.CurrentHp = newHp;
            ChessStateEvents.FireBattleChessDataChanged(chessId);
        };

        // Buff 添加 → 同步到 BattleChessData.ActiveBuffIds 并触发 ChessStateEvents
        entity.BuffManager.OnBuffAdded += (buffId) =>
        {
            if (!battleData.ActiveBuffIds.Contains(buffId))
            {
                battleData.ActiveBuffIds.Add(buffId);
            }
            ChessStateEvents.FireBuffAdded(chessId, buffId);
            ChessStateEvents.FireBattleChessDataChanged(chessId);
        };

        // Buff 移除 → 同步到 BattleChessData.ActiveBuffIds 并触发 ChessStateEvents
        entity.BuffManager.OnBuffRemoved += (buffId) =>
        {
            battleData.ActiveBuffIds.Remove(buffId);
            ChessStateEvents.FireBuffRemoved(chessId, buffId);
            ChessStateEvents.FireBattleChessDataChanged(chessId);
        };

        DebugEx.LogModule("BattleChessManager",
            $"棋子 {chessId} 事件监听已注册（HP/Buff 自动同步，无论通过何种路径触发）");
    }

    /// <summary>
    /// 设置敌方棋子的 EnemyChessDataManager key（由 EnemySpawnManager 在生成后调用）
    /// </summary>
    public void SetEnemyKeyForChess(int chessId, string enemyKey)
    {
        if (m_BattleDataDict.TryGetValue(chessId, out var data))
        {
            data.EnemyKey = enemyKey;
            DebugEx.LogModule("BattleChessManager",
                $"记录敌方棋子 {chessId} → EnemyKey={enemyKey}");
        }
        else
        {
            DebugEx.WarningModule("BattleChessManager",
                $"SetEnemyKeyForChess: 找不到棋子 {chessId}");
        }
    }

    #endregion

    #region 战斗数据查询

    /// <summary>
    /// 获取棋子的战斗数据
    /// </summary>
    public BattleChessData GetBattleChessData(int chessId)
    {
        if (m_BattleDataDict.TryGetValue(chessId, out var data))
        {
            return data;
        }

        DebugEx.WarningModule("BattleChessManager", $"GetBattleChessData: 找不到棋子 {chessId}");
        return null;
    }

    /// <summary>
    /// 获取所有战斗棋子数据
    /// </summary>
    public IReadOnlyList<BattleChessData> GetAllChessData()
    {
        var result = new List<BattleChessData>(m_BattleDataDict.Values);
        return result.AsReadOnly();
    }

    /// <summary>
    /// 获取所有棋子实体
    /// </summary>
    public IReadOnlyList<ChessEntity> GetAllChessEntities()
    {
        var result = new List<ChessEntity>(m_EntityDict.Values);
        return result.AsReadOnly();
    }

    #endregion

    #region 战斗中数据修改

    /// <summary>
    /// 对棋子造成伤害（通过 ChessAttribute.TakeDamage）
    /// 实际伤害逻辑由 ChessAttribute 处理，此处仅作统一入口
    /// </summary>
    public void DamageChess(int chessId, double damageAmount, bool isMagic = false,
        bool isTrueDamage = false, bool isCritical = false)
    {
        if (!m_EntityDict.TryGetValue(chessId, out var entity))
        {
            DebugEx.WarningModule("BattleChessManager", $"DamageChess: 找不到棋子实体 {chessId}");
            return;
        }

        entity.Attribute.TakeDamage(damageAmount, isMagic, isTrueDamage, isCritical);

        // 同步 HP 到战斗数据
        SyncBattleDataHP(chessId, entity);
    }


    #endregion

    #region 战斗结束

    /// <summary>
    /// 战斗结束处理
    /// - 将所有棋子的当前 HP 回写到 GlobalChessManager
    /// - 清除所有临时 Buff（状态效果不跨战斗保留）
    /// - 清理战斗数据
    /// </summary>
    public void OnBattleEnd()
    {
        DebugEx.LogModule("BattleChessManager", $"=== 战斗结束，开始数据回写 ({m_EntityDict.Count} 个棋子) ===");

        foreach (var kvp in m_EntityDict)
        {
            int chessId = kvp.Key;
            ChessEntity entity = kvp.Value;

            if (entity == null)
            {
                // 实体已被销毁（战斗中死亡），从 BattleChessData 缓存取最终 HP（应为 0）
                if (m_BattleDataDict.TryGetValue(chessId, out var deadData))
                {
                    WriteBackHp(chessId, deadData.CurrentHp, deadData.Camp);
                    DebugEx.WarningModule("BattleChessManager",
                        $"棋子 {chessId}(camp={deadData.Camp}) 实体已销毁（死亡），使用缓存 HP={deadData.CurrentHp:F0} 回写");
                }
                else
                {
                    DebugEx.WarningModule("BattleChessManager",
                        $"棋子 {chessId} 实体已销毁且无缓存数据，跳过");
                }
                continue;
            }

            // 1. 获取战斗结束时的血量
            double finalHp = entity.Attribute.CurrentHp;

            // 2. 清除所有 Buff（状态效果不保留到下次战斗）
            entity.BuffManager.ClearAll();

            // 3. 按 camp 分支回写血量
            WriteBackHp(chessId, finalHp, entity.Camp);

            DebugEx.LogModule(
                "BattleChessManager",
                $"棋子 {chessId}(camp={entity.Camp}) 回写：HP={finalHp:F0}，Buff已清除"
            );
        }

        DebugEx.LogModule("BattleChessManager", "=== 数据回写完成 ===");

        // 清理战斗数据
        Clear();
    }

    #endregion

    #region 私有辅助

    /// <summary>
    /// 按阵营分发 HP 回写
    /// Camp=0 → GlobalChessManager；Camp=1 → EnemyChessDataManager
    /// </summary>
    private void WriteBackHp(int chessId, double hp, int camp)
    {
        if (camp == 1)
        {
            // 敌方：通过 BattleChessData 中存储的 EnemyKey 回写
            if (m_BattleDataDict.TryGetValue(chessId, out var data) && !string.IsNullOrEmpty(data.EnemyKey))
            {
                EnemyChessDataManager.Instance.UpdateHp(data.EnemyKey, hp);
            }
            else
            {
                DebugEx.WarningModule("BattleChessManager",
                    $"敌方棋子 {chessId} 缺少 EnemyKey，无法回写 EnemyChessDataManager");
            }
        }
        else
        {
            GlobalChessManager.Instance.UpdateChessHP(chessId, hp);
        }
    }

    /// <summary>
    /// 将 ChessAttribute 当前 HP 同步到 BattleChessData
    /// </summary>
    private void SyncBattleDataHP(int chessId, ChessEntity entity)
    {
        if (m_BattleDataDict.TryGetValue(chessId, out var battleData))
        {
            battleData.CurrentHp = entity.Attribute.CurrentHp;
            OnBattleChessDataChanged?.Invoke(chessId);
        }
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清理战斗数据（OnBattleEnd 后自动调用）
    /// </summary>
    public void Clear()
    {
        m_BattleDataDict.Clear();
        m_EntityDict.Clear();
        OnBattleChessDataChanged = null;

        DebugEx.LogModule("BattleChessManager", "战斗数据已清理");
    }

    #endregion

    #region 调试

    public void DebugPrintAll()
    {
        DebugEx.LogModule("BattleChessManager", $"=== 战斗棋子数据 ({m_BattleDataDict.Count} 个) ===");
        foreach (var data in m_BattleDataDict.Values)
        {
            DebugEx.LogModule("BattleChessManager", data.ToString());
        }
        DebugEx.LogModule("BattleChessManager", "==============================");
    }

    public string GetDebugInfo()
    {
        return $"[BattleChessManager] 战斗中棋子={m_EntityDict.Count} 个";
    }

    #endregion
}
