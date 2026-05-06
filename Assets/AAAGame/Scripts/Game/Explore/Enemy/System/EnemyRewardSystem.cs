using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人奖励系统
/// 根据配置表计算战斗奖励（经验和战利品）
/// </summary>
public class EnemyRewardSystem : SingletonBase<EnemyRewardSystem>
{
    #region 常量
    private const int SOURCE_TYPE_ENEMY = 2; // ExpRuleTable 中敌人来源类型
    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
        DebugEx.Log("EnemyRewardSystem", "初始化完成");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 计算敌人战斗奖励（根据配置表）
    /// 返回经验和战利品配置，由上层系统负责发放
    /// </summary>
    public BattleRewardData CalculateRewards(List<EnemyEntity> defeatedEnemies, float rewardMultiplier)
    {
        var result = new BattleRewardData();

        if (defeatedEnemies == null || defeatedEnemies.Count == 0)
        {
            DebugEx.Warning("EnemyRewardSystem", "没有被击败的敌人");
            return result;
        }

        DebugEx.Log(
            "EnemyRewardSystem",
            $"计算奖励: 敌人数量={defeatedEnemies.Count}, 倍率={rewardMultiplier:F2}"
        );

        foreach (var enemy in defeatedEnemies)
        {
            if (enemy == null)
                continue;

            // 计算经验
            var expRule = GetExpRule(enemy.Config.Difficulty);
            if (expRule != null)
            {
                int currentLevel = PlayerAccountDataManager.Instance?.CurrentSaveData?.GlobalLevel ?? 1;
                int exp = Mathf.RoundToInt((expRule.BaseExp + expRule.ExpPerLevel * currentLevel) * rewardMultiplier);
                result.TotalExp += exp;
            }

            // 获取战利品配置（应用倍率）
            var treasureBox = GetTreasureBoxConfig(enemy.Config.RewardId);
            if (treasureBox != null)
            {
                result.TreasureBoxes.Add(new TreasureBoxData
                {
                    Config = treasureBox,
                    ItemCountMin = Mathf.Max(1, Mathf.RoundToInt(treasureBox.ItemCountMin * rewardMultiplier)),
                    ItemCountMax = Mathf.RoundToInt(treasureBox.ItemCountMax * rewardMultiplier),
                    MinCoins = treasureBox.MinCoins,
                    MaxCoins = treasureBox.MaxCoins,
                    CoinsProbability = (float)treasureBox.CoinsProbability,
                });
            }

            DebugEx.Log(
                "EnemyRewardSystem",
                $"{enemy.Config.Name} 奖励: 经验={result.TotalExp}"
            );
        }

        return result;
    }

    #endregion

    #region 私有方法

    private ExpRuleTable GetExpRule(int enemyDifficulty)
    {
        var table = GF.DataTable.GetDataTable<ExpRuleTable>();
        if (table == null)
        {
            DebugEx.Warning("EnemyRewardSystem", "ExpRuleTable 未加载");
            return null;
        }

        return table.GetDataRow(r => r.SourceType == SOURCE_TYPE_ENEMY && r.SourceParam == enemyDifficulty);
    }

    private TreasureBoxTable GetTreasureBoxConfig(int rewardId)
    {
        var table = GF.DataTable.GetDataTable<TreasureBoxTable>();
        if (table == null)
        {
            DebugEx.Warning("EnemyRewardSystem", "TreasureBoxTable 未加载");
            return null;
        }

        return table.GetDataRow(rewardId);
    }

    #endregion

    #region 数据结构

    /// <summary>
    /// 战斗奖励数据
    /// </summary>
    public class BattleRewardData
    {
        public int TotalExp { get; set; }
        public List<TreasureBoxData> TreasureBoxes { get; set; } = new List<TreasureBoxData>();
    }

    /// <summary>
    /// 宝箱奖励数据（应用倍率）
    /// </summary>
    public class TreasureBoxData
    {
        public TreasureBoxTable Config { get; set; }
        public int ItemCountMin { get; set; }
        public int ItemCountMax { get; set; }
        public int MinCoins { get; set; }
        public int MaxCoins { get; set; }
        public float CoinsProbability { get; set; }
    }

    #endregion
}
