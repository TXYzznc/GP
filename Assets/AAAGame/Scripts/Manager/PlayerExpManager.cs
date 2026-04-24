using UnityEngine;

/// <summary>
/// 玩家经验管理器，封装三种经验来源的计算与发放
/// </summary>
public class PlayerExpManager
{
    private static PlayerExpManager s_Instance;
    public static PlayerExpManager Instance
    {
        get
        {
            if (s_Instance == null)
                s_Instance = new PlayerExpManager();
            return s_Instance;
        }
    }

    // SourceType 常量，与 ExpRuleTable 中一致
    private const int SOURCE_ITEM  = 1;
    private const int SOURCE_ENEMY = 2;
    private const int SOURCE_QUEST = 3;

    /// <summary>
    /// 获取物品时给予经验（SourceParam = 物品稀有度 Quality 1-5）
    /// </summary>
    public void GainExpFromItem(int quality)
    {
        int exp = CalculateExp(SOURCE_ITEM, quality);
        if (exp <= 0) return;
        PlayerAccountDataManager.Instance.AddExp(exp);
        DebugEx.LogModule("PlayerExpManager", $"物品经验 +{exp}（稀有度={quality}）");
    }

    /// <summary>
    /// 击败敌人时给予经验（SourceParam = 敌人难度 Difficulty 1-5）
    /// </summary>
    public void GainExpFromEnemy(int difficulty)
    {
        int exp = CalculateExp(SOURCE_ENEMY, difficulty);
        if (exp <= 0) return;
        PlayerAccountDataManager.Instance.AddExp(exp);
        DebugEx.LogModule("PlayerExpManager", $"击败敌人经验 +{exp}（难度={difficulty}）");
    }

    /// <summary>
    /// 完成任务时给予经验（SourceParam = 任务类型 1=主线 2=支线 3=日常）
    /// </summary>
    public void GainExpFromQuest(int questType)
    {
        int exp = CalculateExp(SOURCE_QUEST, questType);
        if (exp <= 0) return;
        PlayerAccountDataManager.Instance.AddExp(exp);
        DebugEx.LogModule("PlayerExpManager", $"任务经验 +{exp}（类型={questType}）");
    }

    /// <summary>
    /// 根据 ExpRuleTable 计算经验：基础值 + ExpPerLevel × 当前等级
    /// </summary>
    private int CalculateExp(int sourceType, int sourceParam)
    {
        var expTable = GF.DataTable.GetDataTable<ExpRuleTable>();
        if (expTable == null)
        {
            DebugEx.WarningModule("PlayerExpManager", "ExpRuleTable 未加载");
            return 0;
        }

        var rule = expTable.GetDataRow(r => r.SourceType == sourceType && r.SourceParam == sourceParam);
        if (rule == null)
        {
            DebugEx.WarningModule("PlayerExpManager",
                $"未找到经验规则: SourceType={sourceType}, SourceParam={sourceParam}");
            return 0;
        }

        int currentLevel = PlayerAccountDataManager.Instance.CurrentSaveData?.GlobalLevel ?? 1;
        return rule.BaseExp + Mathf.RoundToInt(rule.ExpPerLevel * currentLevel);
    }
}
