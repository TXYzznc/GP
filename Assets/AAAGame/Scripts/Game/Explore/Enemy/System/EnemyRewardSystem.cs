using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人奖励系统
/// 处理战斗胜利后的奖励掉落
/// </summary>
public class EnemyRewardSystem : SingletonBase<EnemyRewardSystem>
{
    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
        DebugEx.LogModule("EnemyRewardSystem", "初始化完成");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 计算并发放奖励
    /// </summary>
    public void GrantRewards(List<EnemyEntity> defeatedEnemies, float rewardMultiplier)
    {
        if (defeatedEnemies == null || defeatedEnemies.Count == 0)
        {
            DebugEx.WarningModule("EnemyRewardSystem", "没有被击败的敌人");
            return;
        }

        DebugEx.LogModule(
            "EnemyRewardSystem",
            $"开始发放奖励，敌人数量: {defeatedEnemies.Count}, 奖励倍率: {rewardMultiplier:F2}"
        );

        int totalExp = 0;
        int totalGold = 0;
        bool hasKey = false;

        foreach (var enemy in defeatedEnemies)
        {
            if (enemy == null)
                continue;

            // 根据奖励等级计算奖励
            int baseExp = GetBaseExp(enemy.Config.RewardTier);
            int baseGold = GetBaseGold(enemy.Config.RewardTier);

            // 应用倍率
            int exp = Mathf.RoundToInt(baseExp * rewardMultiplier);
            int gold = Mathf.RoundToInt(baseGold * rewardMultiplier);

            totalExp += exp;
            totalGold += gold;

            // Boss必定掉落钥匙
            if (enemy.EnemyType == EnemyType.Boss)
            {
                hasKey = true;
            }

            DebugEx.LogModule(
                "EnemyRewardSystem",
                $"{enemy.Config.Name} 奖励: 经验={exp}, 金币={gold}"
            );
        }

        // 发放奖励
        GrantExp(totalExp);
        GrantGold(totalGold);

        if (hasKey)
        {
            GrantKey();
        }

        DebugEx.LogModule(
            "EnemyRewardSystem",
            $"奖励发放完成: 总经验={totalExp}, 总金币={totalGold}, 钥匙={hasKey}"
        );
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取基础经验值
    /// </summary>
    private int GetBaseExp(int rewardTier)
    {
        switch (rewardTier)
        {
            case 1:
                return 10; // 普通敌人
            case 2:
                return 30; // 精英敌人
            case 3:
                return 100; // Boss
            default:
                return 5;
        }
    }

    /// <summary>
    /// 获取基础金币
    /// </summary>
    private int GetBaseGold(int rewardTier)
    {
        switch (rewardTier)
        {
            case 1:
                return 5; // 普通敌人
            case 2:
                return 15; // 精英敌人
            case 3:
                return 50; // Boss
            default:
                return 2;
        }
    }

    /// <summary>
    /// 发放经验
    /// </summary>
    private void GrantExp(int amount)
    {
        // TODO: 添加经验到玩家
        DebugEx.LogModule("EnemyRewardSystem", $"获得经验: {amount}");
    }

    /// <summary>
    /// 发放金币
    /// </summary>
    private void GrantGold(int amount)
    {
        // TODO: 添加金币到玩家
        DebugEx.LogModule("EnemyRewardSystem", $"获得金币: {amount}");
    }

    /// <summary>
    /// 发放钥匙
    /// </summary>
    private void GrantKey()
    {
        // TODO: 添加钥匙到玩家背包
        DebugEx.LogModule("EnemyRewardSystem", "获得钥匙（可撤离）");
    }

    #endregion
}
