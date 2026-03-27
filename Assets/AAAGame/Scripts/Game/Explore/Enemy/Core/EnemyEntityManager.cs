using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人战斗数据
/// 保存触发战斗时的敌人配置信息
/// </summary>
public class EnemyCombatData
{
    /// <summary>是否为群体战斗</summary>
    public bool IsGroupCombat { get; set; }

    /// <summary>参战敌人数据列表</summary>
    public List<SingleEnemyData> EnemyDataList { get; set; }

    /// <summary>
    /// 从单个敌人实体创建战斗数据
    /// </summary>
    public static EnemyCombatData FromEntity(EnemyEntity entity)
    {
        if (entity == null || entity.Config == null)
            return null;

        // 通过 BattleConfigId 获取 EnemyTable 数据
        var enemyTable = GF.DataTable.GetDataTable<EnemyTable>();
        if (enemyTable == null)
        {
            DebugEx.ErrorModule("EnemyCombatData", "EnemyTable 数据表未加载");
            return null;
        }

        var enemyData = enemyTable.GetDataRow(entity.Config.BattleConfigId);
        if (enemyData == null)
        {
            DebugEx.ErrorModule(
                "EnemyCombatData",
                $"未找到 EnemyTable 配置: BattleConfigId={entity.Config.BattleConfigId}"
            );
            return null;
        }

        return new EnemyCombatData
        {
            IsGroupCombat = false,
            EnemyDataList = new List<SingleEnemyData>
            {
                new SingleEnemyData
                {
                    EntityConfigId = entity.EntityConfigId,
                    EnemyName = entity.Config.Name,
                    BattleConfigId = entity.Config.BattleConfigId,
                    EnemyType = entity.EnemyType,
                    MinPopulation = enemyData.MinPopulation,
                    MaxPopulation = enemyData.MaxPopulation,
                    ChessIds = enemyData.ChessIds,
                    TriggerPosition = entity.transform.position,
                },
            },
        };
    }

    /// <summary>
    /// 从多个敌人实体创建战斗数据
    /// </summary>
    public static EnemyCombatData FromMultipleEnemies(List<EnemyEntity> enemies)
    {
        if (enemies == null || enemies.Count == 0)
            return null;

        // 获取 EnemyTable
        var enemyTable = GF.DataTable.GetDataTable<EnemyTable>();
        if (enemyTable == null)
        {
            DebugEx.ErrorModule("EnemyCombatData", "EnemyTable 数据表未加载");
            return null;
        }

        var data = new EnemyCombatData
        {
            IsGroupCombat = true,
            EnemyDataList = new List<SingleEnemyData>(),
        };

        // 第一个是触发者
        var firstEnemy = enemies[0];
        var firstEnemyData = enemyTable.GetDataRow(firstEnemy.Config.BattleConfigId);
        if (firstEnemyData == null)
        {
            DebugEx.ErrorModule(
                "EnemyCombatData",
                $"未找到触发者的 EnemyTable 配置: BattleConfigId={firstEnemy.Config.BattleConfigId}"
            );
            return null;
        }

        data.EnemyDataList.Add(
            new SingleEnemyData
            {
                EntityConfigId = firstEnemy.EntityConfigId,
                EnemyName = firstEnemy.Config.Name,
                BattleConfigId = firstEnemy.Config.BattleConfigId,
                EnemyType = firstEnemy.EnemyType,
                MinPopulation = firstEnemyData.MinPopulation,
                MaxPopulation = firstEnemyData.MaxPopulation,
                ChessIds = firstEnemyData.ChessIds,
                TriggerPosition = firstEnemy.transform.position,
            }
        );

        // 其他敌人随机排序
        List<EnemyEntity> others = new List<EnemyEntity>();
        for (int i = 1; i < enemies.Count; i++)
        {
            others.Add(enemies[i]);
        }

        // 随机打乱
        for (int i = others.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = others[i];
            others[i] = others[j];
            others[j] = temp;
        }

        // 添加到列表
        foreach (var enemy in others)
        {
            var enemyData = enemyTable.GetDataRow(enemy.Config.BattleConfigId);
            if (enemyData == null)
            {
                DebugEx.WarningModule(
                    "EnemyCombatData",
                    $"跳过敌人（未找到配置）: {enemy.Config.Name}, BattleConfigId={enemy.Config.BattleConfigId}"
                );
                continue;
            }

            data.EnemyDataList.Add(
                new SingleEnemyData
                {
                    EntityConfigId = enemy.EntityConfigId,
                    EnemyName = enemy.Config.Name,
                    BattleConfigId = enemy.Config.BattleConfigId,
                    EnemyType = enemy.EnemyType,
                    MinPopulation = enemyData.MinPopulation,
                    MaxPopulation = enemyData.MaxPopulation,
                    ChessIds = enemyData.ChessIds,
                    TriggerPosition = enemy.transform.position,
                }
            );
        }

        return data;
    }
}

/// <summary>
/// 单个敌人的战斗数据
/// </summary>
public class SingleEnemyData
{
    public int EntityConfigId { get; set; }
    public string EnemyName { get; set; }
    public int BattleConfigId { get; set; }
    public EnemyType EnemyType { get; set; }
    public int MinPopulation { get; set; }
    public int MaxPopulation { get; set; }
    public int[] ChessIds { get; set; }
    public Vector3 TriggerPosition { get; set; }
}

/// <summary>
/// 敌人实体管理器
/// 管理场景中所有敌人实体，处理战斗触发
/// </summary>
public class EnemyEntityManager : SingletonBase<EnemyEntityManager>
{
    #region 私有字段

    /// <summary>场景中所有敌人实体</summary>
    private List<EnemyEntity> m_Entities = new List<EnemyEntity>();

    /// <summary>当前触发战斗的敌人实体（战斗准备阶段隐藏，战斗结束按胜负决定是否销毁）</summary>
    private EnemyEntity m_CurrentCombatEnemy;

    /// <summary>当前战斗敌人的 GUID（即使实体被隐藏也保留）</summary>
    private string m_CurrentCombatEnemyGuid;

    /// <summary>当前战斗数据（保存配置信息，不依赖实体引用）</summary>
    private EnemyCombatData m_CurrentCombatData;

    /// <summary>当前参与战斗的敌人列表（群体战斗）</summary>
    private List<EnemyEntity> m_CurrentCombatEnemies = new List<EnemyEntity>();

    /// <summary>是否在战斗中</summary>
    private bool m_IsInCombat;

    /// <summary>战斗结束后待恢复的敌人列表（溶解完成后才实际恢复）</summary>
    private readonly List<EnemyEntity> m_PendingRestoreEnemies = new List<EnemyEntity>();

    /// <summary>保底视野屏蔽：溶解过渡期间所有敌人视野检测被强制关闭</summary>
    private bool m_IsDetectionBlocked;

    #endregion

    #region 属性

    /// <summary>当前触发战斗的敌人</summary>
    public EnemyEntity CurrentCombatEnemy => m_CurrentCombatEnemy;

    /// <summary>当前战斗敌人的 GUID</summary>
    public string CurrentCombatEnemyGuid => m_CurrentCombatEnemyGuid;

    /// <summary>当前战斗数据（保存配置信息）</summary>
    public EnemyCombatData CurrentCombatData => m_CurrentCombatData;

    /// <summary>是否在战斗中</summary>
    public bool IsInCombat => m_IsInCombat;

    /// <summary>保底视野屏蔽是否开启（溶解过渡期间）</summary>
    public bool IsDetectionBlocked => m_IsDetectionBlocked;


    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
        GF.Event.Subscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);
        DebugEx.LogModule("EnemyEntityManager", "初始化完成");
    }

    protected override void OnDestroy()
    {
        GF.Event.Unsubscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);
        base.OnDestroy();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 注册敌人实体
    /// </summary>
    public void RegisterEntity(EnemyEntity entity)
    {
        if (entity == null)
            return;

        if (!m_Entities.Contains(entity))
        {
            m_Entities.Add(entity);
            DebugEx.LogModule(
                "EnemyEntityManager",
                $"注册敌人实体: {entity.Config.Name}, 当前总数={m_Entities.Count}"
            );
        }
    }

    /// <summary>
    /// 注销敌人实体
    /// </summary>
    public void UnregisterEntity(EnemyEntity entity)
    {
        if (entity == null)
            return;

        if (m_Entities.Remove(entity))
        {
            DebugEx.LogModule(
                "EnemyEntityManager",
                $"注销敌人实体: {entity.Config.Name}, 当前总数={m_Entities.Count}"
            );
        }
    }

    /// <summary>
    /// 触发战斗
    /// </summary>
    public void TriggerCombat(EnemyEntity entity)
    {
        if (entity == null)
        {
            DebugEx.ErrorModule("EnemyEntityManager", "触发战斗失败：敌人实体为空");
            return;
        }

        if (m_IsInCombat)
        {
            DebugEx.WarningModule("EnemyEntityManager", "已经在战斗中，忽略触发请求");
            return;
        }

        // ⭐ 保存战斗数据（不依赖实体引用）
        m_CurrentCombatData = EnemyCombatData.FromEntity(entity);
        if (m_CurrentCombatData == null)
        {
            DebugEx.ErrorModule("EnemyEntityManager", "创建战斗数据失败");
            return;
        }

        // 保存实体引用和 GUID
        m_CurrentCombatEnemy = entity;
        m_CurrentCombatEnemyGuid = entity.EntityGuid;
        m_IsInCombat = true;

        // 让敌人进入战斗状态
        entity.EnterCombat();

        DebugEx.LogModule(
            "EnemyEntityManager",
            $"触发单敌人战斗: {m_CurrentCombatData.EnemyDataList[0].EnemyName}, BattleConfigId={m_CurrentCombatData.EnemyDataList[0].BattleConfigId}"
        );

        // 调用CombatTriggerManager处理战斗触发（敌方追击触发为敌方先手）
        CombatTriggerManager.Instance.TriggerCombat(entity, CombatTriggerType.EnemyInitiated);

        // 通知所有追击状态的敌人：玩家进入战斗
        NotifyPlayerEnteredCombat();

        // 切换到战斗准备状态
        SwitchToCombatPreparation();
    }

    /// <summary>
    /// 从玩家检测器触发战斗（支持不同的触发方式）
    /// </summary>
    public void TriggerCombatFromOpportunity(EnemyEntity entity, CombatTriggerType triggerType)
    {
        // 对于敌人追击触发的战斗，标记为敌方先手
        if (
            triggerType == CombatTriggerType.Normal
            || triggerType == CombatTriggerType.EnemyInitiated
        )
        {
            TriggerCombat(entity);
        }
        else
        {
            // 偷袭和遭遇战由CombatTriggerManager处理，已经调用了TriggerCombat
            TriggerCombat(entity);
        }
    }

    /// <summary>
    /// 战斗结束
    /// </summary>
    public void OnCombatEnd(bool playerWin)
    {
        if (!m_IsInCombat)
        {
            DebugEx.WarningModule("EnemyEntityManager", "当前不在战斗中");
            return;
        }

        DebugEx.LogModule("EnemyEntityManager", $"战斗结束: 玩家{(playerWin ? "胜利" : "失败")}");

        // 注意：敌人实体已经在战斗准备阶段销毁了
        // 这里只需要清理数据

        if (playerWin)
        {
            // 玩家胜利：销毁敌人实体，EnemyChessDataManager 数据由 OnDestroy 自动清理
            string enemyNames = "";
            if (m_CurrentCombatData != null)
            {
                foreach (var enemyData in m_CurrentCombatData.EnemyDataList)
                    enemyNames += enemyData.EnemyName + " ";
            }
            DebugEx.LogModule("EnemyEntityManager", $"玩家胜利，销毁敌人: {enemyNames}");

            if (m_CurrentCombatEnemy != null)
            {
                UnregisterEntity(m_CurrentCombatEnemy);
                Destroy(m_CurrentCombatEnemy.gameObject);
            }
            foreach (var enemy in m_CurrentCombatEnemies)
            {
                if (enemy != null && enemy != m_CurrentCombatEnemy)
                {
                    UnregisterEntity(enemy);
                    Destroy(enemy.gameObject);
                }
            }

            // TODO: 发放奖励
            // TODO: 如果可净化，给予卡牌
        }
        else
        {
            // 玩家失败：将敌人存入待恢复列表，溶解完成后（CombatLeaveEvent）再恢复
            DebugEx.LogModule("EnemyEntityManager", "玩家失败，敌人将在溶解完成后恢复");
            m_PendingRestoreEnemies.Clear();
            if (m_CurrentCombatEnemy != null)
                m_PendingRestoreEnemies.Add(m_CurrentCombatEnemy);
            foreach (var enemy in m_CurrentCombatEnemies)
            {
                if (enemy != null && enemy != m_CurrentCombatEnemy)
                    m_PendingRestoreEnemies.Add(enemy);
            }
        }

        // 重置所有敌人的警觉度
        ResetAllEnemyAlertLevels();

        // 开启保底视野屏蔽，阻止溶解过渡期间的视野检测和追击触发
        m_IsDetectionBlocked = true;

        // Arm 隐身：屏蔽视野，不计时
        var playerGo = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        if (playerGo != null)
            playerGo.GetComponent<PostCombatStealth>()?.Arm();

        // 清理数据
        m_CurrentCombatEnemy = null;
        m_CurrentCombatEnemyGuid = null;
        m_CurrentCombatData = null;
        m_CurrentCombatEnemies.Clear();
        m_IsInCombat = false;
    }

    /// <summary>
    /// 溶解过渡完成，正式退出战斗（CombatLeaveEvent 回调）
    /// 此时恢复待处理的敌人实体，并激活玩家隐身计时
    /// </summary>
    public void OnCombatLeave(object sender, GameFramework.Event.GameEventArgs e)
    {
        // 恢复待处理的敌人实体
        foreach (var enemy in m_PendingRestoreEnemies)
            RestoreEntityAfterCombat(enemy);
        m_PendingRestoreEnemies.Clear();

        // 解除保底视野屏蔽（隐身 Arm 已生效，由 PostCombatStealth.IsActive 继续屏蔽检测）
        m_IsDetectionBlocked = false;
    }

    /// <summary>
    /// 触发群体战斗（多个敌人同时参战）
    /// </summary>
    public void TriggerGroupCombat(List<EnemyEntity> combatEnemies)
    {
        if (combatEnemies == null || combatEnemies.Count == 0)
        {
            DebugEx.ErrorModule("EnemyEntityManager", "触发群体战斗失败：敌人列表为空");
            return;
        }

        if (m_IsInCombat)
        {
            DebugEx.WarningModule("EnemyEntityManager", "已经在战斗中，忽略群体战斗触发");
            return;
        }

        // 主敌人（触发者）
        EnemyEntity mainEnemy = combatEnemies[0];

        // 保存战斗数据（多敌人）
        m_CurrentCombatData = EnemyCombatData.FromMultipleEnemies(combatEnemies);
        if (m_CurrentCombatData == null)
        {
            DebugEx.ErrorModule("EnemyEntityManager", "创建群体战斗数据失败");
            return;
        }

        // 保存敌人列表
        m_CurrentCombatEnemies.Clear();
        m_CurrentCombatEnemies.AddRange(combatEnemies);
        m_CurrentCombatEnemy = mainEnemy;
        m_IsInCombat = true;

        // 让所有敌人进入战斗状态
        foreach (var enemy in combatEnemies)
        {
            enemy.EnterCombat();
        }

        DebugEx.LogModule(
            "EnemyEntityManager",
            $"触发群体战斗: 主敌人={mainEnemy.Config.Name}, 参战敌人数={combatEnemies.Count}"
        );

        // 通知所有追击状态的敌人：玩家进入战斗
        NotifyPlayerEnteredCombat();

        // 切换到战斗准备状态
        SwitchToCombatPreparation();
    }

    /// <summary>
    /// 清空所有敌人实体
    /// </summary>
    public void Clear()
    {
        m_Entities.Clear();
        m_CurrentCombatEnemy = null;
        m_IsInCombat = false;

        DebugEx.LogModule("EnemyEntityManager", "已清空所有敌人实体");
    }

    /// <summary>
    /// 隐藏当前战斗的敌人实体（战斗准备阶段调用，不销毁）
    /// </summary>
    public void DestroyCurrentCombatEnemy()
    {
        HideEntityForCombat(m_CurrentCombatEnemy);

        foreach (var enemy in m_CurrentCombatEnemies)
        {
            if (enemy != null && enemy != m_CurrentCombatEnemy)
            {
                HideEntityForCombat(enemy);
            }
        }
    }

    /// <summary>
    /// 隐藏敌人实体（战斗准备阶段：先停止 NavMeshAgent 再 SetActive(false)）
    /// </summary>
    public void HideEntityForCombat(EnemyEntity entity)
    {
        if (entity == null) return;

        DebugEx.LogModule("EnemyEntityManager", $"隐藏敌人实体: {entity.Config.Name}");

        // 必须先停止再 SetActive(false)，否则 NavMesh 状态残留
        if (entity.NavAgent != null && entity.NavAgent.isOnNavMesh)
            entity.NavAgent.isStopped = true;

        entity.gameObject.SetActive(false);
    }

    /// <summary>
    /// 恢复敌人实体（玩家失败后：SetActive(true) + 重新放置到 NavMesh + 重置 AI）
    /// </summary>
    public void RestoreEntityAfterCombat(EnemyEntity entity)
    {
        if (entity == null) return;

        DebugEx.LogModule("EnemyEntityManager", $"恢复敌人实体: {entity.Config.Name}");

        entity.gameObject.SetActive(true);

        // 尝试 Warp 回出生点（SetActive(true) 后 agent 可能还未放置到 NavMesh，失败则静默忽略）
        if (entity.NavAgent != null && entity.NavAgent.isOnNavMesh)
            entity.NavAgent.Warp(entity.SpawnPosition);

        // ExitCombat 内部已有 isOnNavMesh 保护
        entity.ExitCombat();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 重置场景中所有敌人的警觉度（战斗结束后调用）
    /// </summary>
    private void ResetAllEnemyAlertLevels()
    {
        int count = 0;
        foreach (var entity in m_Entities)
        {
            if (entity != null && entity.VisionDetector != null)
            {
                entity.VisionDetector.ResetAlert();
                count++;
            }
        }
        DebugEx.LogModule("EnemyEntityManager", $"已重置 {count} 个敌人的警觉度");
    }

    /// <summary>
    /// 通知所有敌人：玩家进入战斗
    /// </summary>
    private void NotifyPlayerEnteredCombat()
    {
        int notifiedCount = 0;
        foreach (var entity in m_Entities)
        {
            if (
                entity != null
                && (
                    entity.AI.CurrentState == EnemyAIState.Chase
                    || entity.AI.CurrentState == EnemyAIState.AlertedByBroadcast
                )
            )
            {
                // 让敌人恢复巡逻状态
                entity.AI.OnPlayerEnteredCombat();
                notifiedCount++;
            }
        }

        DebugEx.LogModule("EnemyEntityManager", $"已通知 {notifiedCount} 个追击敌人：玩家进入战斗");
    }

    /// <summary>
    /// 切换到战斗准备状态
    /// </summary>
    private void SwitchToCombatPreparation()
    {
        // 通过 GameStateManager 切换到战斗准备状态
        var gameStateManager = GameStateManager.Instance;
        if (gameStateManager != null)
        {
            DebugEx.LogModule("EnemyEntityManager", "请求切换到战斗准备状态");
            gameStateManager.SwitchToCombatPreparation();
        }
        else
        {
            DebugEx.ErrorModule("EnemyEntityManager", "GameStateManager 不存在！");
        }
    }

    /// <summary>
    /// 计算战斗难度（根据参战敌人数量和类型）
    /// </summary>
    private int CalculateCombatDifficulty()
    {
        int totalDifficulty = 0;

        foreach (var enemy in m_CurrentCombatEnemies)
        {
            if (enemy != null && enemy.Config != null)
            {
                totalDifficulty += enemy.Config.Difficulty;
            }
        }

        DebugEx.LogModule(
            "EnemyEntityManager",
            $"战斗难度计算: 参战敌人数={m_CurrentCombatEnemies.Count}, 总难度={totalDifficulty}"
        );

        return totalDifficulty;
    }

    #endregion
}

/// <summary>
/// 触发战斗事件参数
/// </summary>
public class TriggerCombatEventArgs : GameFramework.Event.GameEventArgs
{
    public static readonly int EventId = typeof(TriggerCombatEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
