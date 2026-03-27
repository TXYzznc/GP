using UnityEngine;

/// <summary>
/// 命中检测上下文
/// 传递执行命中检测所需的所有信息
/// </summary>
public class HitContext
{
    #region 攻击者信息

    /// <summary>攻击者实体</summary>
    public ChessEntity Attacker;

    /// <summary>攻击者位置</summary>
    public Vector3 AttackerPosition;

    /// <summary>攻击者朝向</summary>
    public Vector3 AttackerForward;

    /// <summary>攻击者阵营</summary>
    public int AttackerCamp;

    #endregion

    #region 目标信息

    /// <summary>锁定的目标（瞬发攻击使用）</summary>
    public ChessEntity LockedTarget;

    /// <summary>目标位置（AOE/射线使用）</summary>
    public Vector3 TargetPosition;

    #endregion

    #region 伤害信息

    /// <summary>基础伤害值</summary>
    public double BaseDamage;

    /// <summary>伤害值（别名，方便访问）</summary>
    public double Damage
    {
        get => BaseDamage;
        set => BaseDamage = value;
    }

    /// <summary>是否暴击</summary>
    public bool IsCritical;

    /// <summary>是否魔法伤害</summary>
    public bool IsMagicDamage;

    /// <summary>是否真实伤害</summary>
    public bool IsTrueDamage;

    #endregion

    #region 检测参数

    /// <summary>攻击范围/射程半径</summary>
    public float Range;

    /// <summary>AOE半径</summary>
    public float AOERadius;

    /// <summary>最大命中数（多段伤害次数，0=单次）</summary>
    public int MaxHitCount;

    /// <summary>穿透数量（投射物可命中的敌人数量，0=单体不穿透）</summary>
    public int PenetrationCount;

    /// <summary>投射物速度</summary>
    public float ProjectileSpeed;

    /// <summary>投射物预制体路径</summary>
    public int ProjectilePrefabId;

    /// <summary>敌人Layer掩码</summary>
    public LayerMask EnemyLayerMask;

    #endregion

    #region 回调

    /// <summary>命中回调（每命中一个目标调用一次）</summary>
    public System.Action<ChessEntity, double, bool> OnHitCallback;

    #endregion

    #region 效果配置

    /// <summary>技能配置（用于应用 Buff 效果）</summary>
    public SummonChessSkillTable SkillConfig;

    #endregion

    /// <summary>释放特效资源ID（释放时播放）</summary>
    public int EffectId;

    /// <summary>受击特效资源ID（命中时播放）</summary>
    public int HitEffectId;

    #region 工厂方法

    /// <summary>
    /// 创建普通攻击上下文
    /// </summary>
    public static HitContext CreateForNormalAttack(
        ChessEntity attacker,
        ChessEntity target,
        double damage,
        bool isCritical,
        SummonChessSkillTable skillConfig = null
    )
    {
        return new HitContext
        {
            Attacker = attacker,
            AttackerPosition = attacker.transform.position,
            AttackerForward = attacker.transform.forward,
            AttackerCamp = attacker.Camp,
            LockedTarget = target,
            TargetPosition = target?.transform.position ?? Vector3.zero,
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = false,
            IsTrueDamage = false,
            Range = (float)attacker.Attribute.AtkRange,
            MaxHitCount = 1,
            EnemyLayerMask = GetEnemyLayerMask(attacker.Camp),
            SkillConfig = skillConfig,
        };
    }

    /// <summary>
    /// 创建技能攻击上下文
    /// </summary>
    public static HitContext CreateForSkill(
        ChessEntity attacker,
        ChessEntity target,
        SummonChessSkillTable skillConfig,
        double calculatedDamage,
        bool isCritical
    )
    {
        return new HitContext
        {
            Attacker = attacker,
            AttackerPosition = attacker.transform.position,
            AttackerForward = attacker.transform.forward,
            AttackerCamp = attacker.Camp,
            LockedTarget = target,
            TargetPosition =
                target?.transform.position
                ?? attacker.transform.position
                    + attacker.transform.forward * (float)skillConfig.CastRange,
            BaseDamage = calculatedDamage,
            IsCritical = isCritical,
            IsMagicDamage = skillConfig.DamageType == 2,
            IsTrueDamage = skillConfig.DamageType == 3,
            Range = (float)skillConfig.CastRange,
            AOERadius = (float)skillConfig.AreaRadius,
            MaxHitCount = skillConfig.HitCount,
            PenetrationCount = skillConfig.PenetrationCount,

            // ⭐ 投掷物配置（从配置表读取）
            ProjectilePrefabId = skillConfig.ProjectilePrefabId,
            ProjectileSpeed = (float)skillConfig.ProjectileSpeed,

            EnemyLayerMask = GetEnemyLayerMask(attacker.Camp),
            EffectId = skillConfig.EffectId,
            HitEffectId = skillConfig.HitEffectId,
            SkillConfig = skillConfig,
        };
    }

    /// <summary>
    /// 获取敌人Layer掩码
    /// </summary>
    private static LayerMask GetEnemyLayerMask(int camp)
    {
        // 使用阵营服务获取敌对LayerMask
        return CampRelationService.GetEnemyLayerMask(camp);
    }

    #endregion
}
