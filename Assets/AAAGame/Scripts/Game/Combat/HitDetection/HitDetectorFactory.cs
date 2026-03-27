using System.Collections.Generic;

/// <summary>
/// 命中检测器工厂
/// 创建和缓存各类型的检测器实例
/// </summary>
public static class HitDetectorFactory
{
    #region 检测器缓存

    private static readonly Dictionary<AttackHitType, IHitDetector> s_DetectorCache = new Dictionary<AttackHitType, IHitDetector>();

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取检测器实例
    /// </summary>
    /// <param name="hitType">攻击类型</param>
    /// <returns>检测器实例</returns>
    public static IHitDetector GetDetector(AttackHitType hitType)
    {
        // 检查缓存
        if (s_DetectorCache.TryGetValue(hitType, out IHitDetector detector))
        {
            return detector;
        }

        // 创建新实例
        detector = CreateDetector(hitType);
        if (detector != null)
        {
            s_DetectorCache[hitType] = detector;
        }

        return detector;
    }

    /// <summary>
    /// 创建新的检测器实例（不使用缓存）
    /// </summary>
    /// <param name="hitType">攻击类型</param>
    /// <returns>检测器实例</returns>
    public static IHitDetector CreateDetector(AttackHitType hitType)
    {
        switch (hitType)
        {
            case AttackHitType.Instant:
                return new InstantHitDetector();

            case AttackHitType.Melee:
                return new MeleeHitDetector();

            case AttackHitType.Projectile:
                return new ProjectileHitDetector();

            case AttackHitType.AOE:
                return new AOEHitDetector();

            case AttackHitType.Raycast:
                return new RaycastHitDetector();

            default:
                DebugEx.WarningModule("HitDetectorFactory", $"未知的攻击类型: {hitType}，使用瞬发检测器");
                return new InstantHitDetector();
        }
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public static void ClearCache()
    {
        s_DetectorCache.Clear();
    }

    #endregion
}
