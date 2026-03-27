using UnityEngine;

/// <summary>
/// 嫦娥：远程魔法普通攻击 (ID=22)
/// 魔法伤害：80%法强系数
/// 攻击附带冰霜效果（通过配置表 Buff 实现）
/// </summary>
public class ChangeNormalAttack : ChessNormalAttackBase
{
    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.LogModule("ChangeNormalAttack", "嫦娥普攻初始化完成");
    }

    /// <summary>
    /// 执行普攻完整流程
    /// </summary>
    public override void ExecuteAttack(ChessEntity caster, ChessEntity target)
    {
        if (caster == null)
        {
            DebugEx.ErrorModule("ChangeNormalAttack", "ExecuteAttack: caster 为 null");
            return;
        }

        if (target == null)
        {
            DebugEx.WarningModule("ChangeNormalAttack", "ExecuteAttack: target 为 null");
            return;
        }

        DebugEx.LogModule("ChangeNormalAttack", $"执行普攻 → 目标: {target.Config?.Name}");

        // 1. 计算伤害
        double damage = CalculateDamage(caster, out bool isCritical);

        // 2. 构建命中检测上下文
        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = caster.transform.forward,
            AttackerCamp = caster.Camp,
            LockedTarget = target,
            TargetPosition = EntityPositionHelper.GetCenterPosition(target), // ⭐ 使用模型中心点
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = true, // 嫦娥是魔法伤害
            IsTrueDamage = false,
            Range = (float)caster.Attribute.AtkRange,
            PenetrationCount = m_Config.PenetrationCount,
            ProjectilePrefabId = m_Config.ProjectilePrefabId,
            ProjectileSpeed = (float)m_Config.ProjectileSpeed,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
        };

        // 3. 冰霜效果通过配置表的 Buff 自动应用（BuffTriggerType=1）
        // 不需要在这里手动添加

        // 4. 播放普攻特效
        PlayAttackEffect(caster);

        // 5. 执行命中检测（使用投射物检测器）
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Projectile);
        detector.Execute(context);

        // 6. 回复蓝量
        RestoreMana(caster);

        DebugEx.LogModule("ChangeNormalAttack", "普攻执行完成，投射物已发射");
    }

    #endregion
}
