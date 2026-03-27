using UnityEngine;

/// <summary>
/// 后羿：远程投射物普通攻击 (ID=12)
/// 造成伤害：100%攻击系数
/// 烈焰箭激活时，攻击附带灼烧效果
/// </summary>
public class HouyiNormalAttack : ChessNormalAttackBase
{
    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.LogModule("HouyiNormalAttack", "后羿普攻初始化完成");
    }

    /// <summary>
    /// 执行普攻完整流程
    /// </summary>
    public override void ExecuteAttack(ChessEntity caster, ChessEntity target)
    {
        if (caster == null)
        {
            DebugEx.ErrorModule("HouyiNormalAttack", "ExecuteAttack: caster 为 null");
            return;
        }

        if (target == null)
        {
            DebugEx.WarningModule("HouyiNormalAttack", "ExecuteAttack: target 为 null");
            return;
        }

        DebugEx.LogModule("HouyiNormalAttack", $"执行普攻 → 目标: {target.Config?.Name}");

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
            IsMagicDamage = false,
            IsTrueDamage = false,
            Range = (float)caster.Attribute.AtkRange,
            PenetrationCount = m_Config.PenetrationCount,
            ProjectilePrefabId = m_Config.ProjectilePrefabId,
            ProjectileSpeed = (float)m_Config.ProjectileSpeed,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = OnAttackHit, // ⭐ 设置命中回调
        };

        // 3. 播放普攻特效
        PlayAttackEffect(caster);

        // 4. 执行命中检测（使用投射物检测器）
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Projectile);
        detector.Execute(context);

        // 5. 回复蓝量
        RestoreMana(caster);

        DebugEx.LogModule("HouyiNormalAttack", "普攻执行完成，投射物已发射");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 普攻命中回调（投射物命中目标时调用）
    /// </summary>
    private void OnAttackHit(ChessEntity target, double damage, bool isCritical)
    {
        if (target == null || m_Ctx == null)
            return;

        // ⭐ 在投射物命中后才应用灼烧 Buff
        if (m_Ctx.BuffManager != null && m_Ctx.BuffManager.HasBuff(4)) // 烈焰箭 Buff ID=4
        {
            if (target.BuffManager != null)
            {
                target.BuffManager.AddBuff(1, m_Ctx.Owner, m_Ctx.Attribute); // 灼烧 ID=1
                DebugEx.LogModule(
                    "HouyiNormalAttack",
                    $"烈焰箭激活，对 {target.Config?.Name} 附带灼烧效果"
                );
            }
        }
    }

    #endregion
}
