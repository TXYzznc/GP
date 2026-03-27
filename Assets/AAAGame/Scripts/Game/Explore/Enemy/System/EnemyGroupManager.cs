using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人群体管理器
/// 负责检测和触发群体战斗
/// </summary>
public class EnemyGroupManager : SingletonBase<EnemyGroupManager>
{
    #region 常量

    /// <summary>战斗检测范围（米）</summary>
    private const float COMBAT_DETECTION_RANGE = 15f;

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
        DebugEx.LogModule("EnemyGroupManager", "初始化完成");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 检测并触发群体战斗
    /// </summary>
    /// <param name="triggerEnemy">触发战斗的敌人</param>
    public void DetectAndTriggerGroupCombat(EnemyEntity triggerEnemy)
    {
        if (triggerEnemy == null)
        {
            DebugEx.ErrorModule("EnemyGroupManager", "触发敌人为空");
            return;
        }

        // 查找范围内处于 AlertedByBroadcast 状态的敌人
        float detectionRange = triggerEnemy.Config.CombatDistance * 2f;

        DebugEx.LogModule(
            "EnemyGroupManager",
            $"检测群体战斗，触发者={triggerEnemy.Config.Name}，检测范围={detectionRange}m"
        );

        Collider[] nearbyColliders = Physics.OverlapSphere(
            triggerEnemy.transform.position,
            detectionRange,
            LayerMask.GetMask("Enemy")
        );

        List<EnemyEntity> combatGroup = new List<EnemyEntity>();
        combatGroup.Add(triggerEnemy);

        foreach (var collider in nearbyColliders)
        {
            EnemyEntity nearbyEnemy = collider.GetComponent<EnemyEntity>();
            if (
                nearbyEnemy != null
                && nearbyEnemy != triggerEnemy
                && nearbyEnemy.AI.CurrentState == EnemyAIState.AlertedByBroadcast
                && nearbyEnemy.Status == EnemyStatus.Alive
            )
            {
                combatGroup.Add(nearbyEnemy);
                DebugEx.LogModule(
                    "EnemyGroupManager",
                    $"{nearbyEnemy.Config.Name} 加入群体战斗（AlertedByBroadcast状态）"
                );
            }
        }

        DebugEx.LogModule("EnemyGroupManager", $"触发战斗，敌人数量: {combatGroup.Count}");

        // 触发战斗
        if (combatGroup.Count > 1)
        {
            EnemyEntityManager.Instance.TriggerGroupCombat(combatGroup);
        }
        else
        {
            // 单个敌人战斗
            EnemyEntityManager.Instance.TriggerCombat(triggerEnemy);
        }
    }

    /// <summary>
    /// 广播玩家位置给范围内的敌人
    /// </summary>
    /// <param name="broadcaster">广播者</param>
    /// <param name="playerPosition">玩家位置</param>
    /// <param name="broadcastRange">广播范围</param>
    public void BroadcastPlayerPosition(
        EnemyEntity broadcaster,
        Vector3 playerPosition,
        float broadcastRange
    )
    {
        if (broadcaster == null)
        {
            DebugEx.ErrorModule("EnemyGroupManager", "广播者为空");
            return;
        }

        DebugEx.LogModule(
            "EnemyGroupManager",
            $"{broadcaster.Config.Name} 开始广播玩家位置，范围={broadcastRange}m"
        );

        // 查找范围内的其他敌人
        Collider[] nearbyColliders = Physics.OverlapSphere(
            broadcaster.transform.position,
            broadcastRange,
            LayerMask.GetMask("Enemy")
        );

        int notifiedCount = 0;
        foreach (var collider in nearbyColliders)
        {
            EnemyEntity nearbyEnemy = collider.GetComponent<EnemyEntity>();
            if (
                nearbyEnemy != null
                && nearbyEnemy != broadcaster
                && nearbyEnemy.Status == EnemyStatus.Alive
            )
            {
                // 通知敌人玩家位置
                nearbyEnemy.AI.OnReceiveBroadcast(playerPosition);
                notifiedCount++;
            }
        }

        DebugEx.LogModule("EnemyGroupManager", $"广播完成，通知了 {notifiedCount} 个敌人");
    }

    #endregion
}
