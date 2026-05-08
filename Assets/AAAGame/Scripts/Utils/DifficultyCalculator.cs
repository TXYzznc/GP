using UnityEngine;

/// <summary>
/// 难度计算工具
/// 根据：等级系数、召唤师等级、敌人基础难度 计算最终难度
/// </summary>
public static class DifficultyCalculator
{
    /// <summary>
    /// 获取召唤师等级系数（非线性）
    /// 范围: 1.0 ~ 2.5（玩家0级 ~ 玩家最高等级）
    /// 公式: 1 + (PlayerLevel / MaxLevel) ^ 0.4 × 1.5
    /// </summary>
    public static float GetSummonerLevelCoefficient(int globalLevel, int maxLevel = 60)
    {
        if (maxLevel <= 0)
            return 1f;

        float playerRatio = Mathf.Clamp01((float)globalLevel / maxLevel);
        return 1f + Mathf.Pow(playerRatio, 0.4f) * 1.5f;
    }

    /// <summary>
    /// 计算敌人真实难度（非线性算法）
    /// 公式: (地图系数 / 50) ^ 0.6 × 0.4 + (召唤师系数 - 1) / 1.5 × 0.6 × 敌人基础难度
    ///
    /// 特点：
    /// - 召唤师系数影响 60%，地图系数影响 40%
    /// - 最高可达等级 10，最低等级 1
    /// - 玩家满级时充分享受敌人难度的增长
    /// </summary>
    public static float CalculateEnemyDifficulty(
        float levelCoefficient,
        int globalLevel,
        float enemyBaseDifficulty,
        int maxLevel = 60)
    {
        if (levelCoefficient <= 0 || maxLevel <= 0)
            return enemyBaseDifficulty;

        // 计算召唤师系数
        float summonerCoef = GetSummonerLevelCoefficient(globalLevel, maxLevel);

        // 计算地图影响因子
        float mapInfluence = Mathf.Pow(levelCoefficient / 50f, 0.6f);

        // 计算召唤师影响因子
        float summonerInfluence = (summonerCoef - 1) / 1.5f;

        // 最终难度 = 加权影响因子 × 敌人基础难度
        float finalDifficulty = (mapInfluence * 0.4f + summonerInfluence * 0.6f) * enemyBaseDifficulty;

        return finalDifficulty;
    }

    /// <summary>
    /// 从难度系数映射到CombatDifficultyRule等级(1-10)
    /// 使用分段线性插值
    /// </summary>
    public static int MapDifficultyToLevel(float difficulty)
    {
        // 难度系数 → 等级映射表
        // Level:  1    2    3    4    5    6    7    8    9    10
        // Coef: 0.4  0.6  0.8  1.0  1.4  1.9  2.5  3.2  4.0  5.0

        if (difficulty < 0.5f) return 1;
        if (difficulty < 0.7f) return 2;
        if (difficulty < 0.9f) return 3;
        if (difficulty < 1.2f) return 4;
        if (difficulty < 1.65f) return 5;
        if (difficulty < 2.2f) return 6;
        if (difficulty < 2.9f) return 7;
        if (difficulty < 3.6f) return 8;
        if (difficulty < 4.5f) return 9;
        return 10;
    }

    /// <summary>
    /// 计算宝箱等级（非线性算法）
    /// 公式: LevelCoefficient × (PlayerLevel / MaxLevel) ^ 0.4
    ///
    /// 特点：
    /// - 玩家等级达到上限时，宝箱等级 = LevelCoefficient
    /// - 玩家等级较低时，仍能获得合理的宝箱等级（非线性保底）
    /// - 例：系数80，玩家10/50级 → 约41级；玩家50/50级 → 80级
    /// </summary>
    public static int CalculateChestLevel(int globalLevel, float levelCoefficient, int maxLevel = 60)
    {
        if (levelCoefficient <= 0 || maxLevel <= 0)
            return 1;

        // 计算玩家的进度比例
        float playerRatio = Mathf.Clamp01((float)globalLevel / maxLevel);

        // 使用非线性函数（幂 0.4）确保低等级玩家也有合理的宝箱等级
        float playerCoefficient = Mathf.Pow(playerRatio, 0.4f);

        // 最终宝箱等级 = 关卡系数 × 玩家系数
        float chestLevelFloat = levelCoefficient * playerCoefficient;
        int chestLevel = Mathf.RoundToInt(chestLevelFloat);

        // 限制范围1-100
        return Mathf.Clamp(chestLevel, 1, 100);
    }

    /// <summary>
    /// 调试输出：打印难度计算过程
    /// </summary>
    public static void DebugDifficultyCalculation(
        int globalLevel,
        float levelCoefficient,
        float enemyBaseDifficulty,
        string logPrefix = "",
        int maxLevel = 60)
    {
        float summonerCoef = GetSummonerLevelCoefficient(globalLevel, maxLevel);
        float difficulty = CalculateEnemyDifficulty(levelCoefficient, globalLevel, enemyBaseDifficulty, maxLevel);
        int level = MapDifficultyToLevel(difficulty);
        int chestLevel = CalculateChestLevel(globalLevel, levelCoefficient, maxLevel);

        float mapInfluence = Mathf.Pow(levelCoefficient / 50f, 0.6f);
        float summonerInfluence = (summonerCoef - 1) / 1.5f;

        DebugEx.Log("DifficultyCalculator",
            $"{logPrefix}\n" +
            $"  ├─ 玩家等级: {globalLevel}/{maxLevel} → 召唤师系数: {summonerCoef:F2}\n" +
            $"  ├─ 地图系数: {levelCoefficient:F2} → 地图影响: {mapInfluence:F2} (权重40%)\n" +
            $"  ├─ 召唤师影响: {summonerInfluence:F2} (权重60%)\n" +
            $"  ├─ 敌人基础难度: {enemyBaseDifficulty:F2}\n" +
            $"  ├─ 最终敌人难度: {difficulty:F2} → 等级: {level}\n" +
            $"  └─ 宝箱等级: {chestLevel}");
    }
}
