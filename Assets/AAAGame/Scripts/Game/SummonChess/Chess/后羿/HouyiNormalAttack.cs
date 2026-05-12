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
        DebugEx.Log("HouyiNormalAttack", "后羿普攻初始化完成");
    }

    /// <summary>
    /// 执行普攻完整流程
    /// </summary>
    public override void ExecuteAttack(ChessEntity caster, ChessEntity target)
    {
        if (caster == null)
        {
            DebugEx.Error("HouyiNormalAttack", "ExecuteAttack: caster 为 null");
            return;
        }

        if (target == null)
        {
            DebugEx.Warning("HouyiNormalAttack", "ExecuteAttack: target 为 null");
            return;
        }

        DebugEx.Log("HouyiNormalAttack", $"[{caster.gameObject.name}] 执行普攻 → 目标: [{target.gameObject.name}] {target.Config?.Name}");

        // ⭐ 1. 构建命中检测上下文（延迟伤害计算到投射物命中时刻）
        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = caster.transform.forward,
            AttackerCamp = caster.Camp,
            LockedTarget = target,
            TargetPosition = EntityPositionHelper.GetCenterPosition(target),
            BaseDamage = 0,  // 占位符，实际伤害由 CalculateDamageCallback 计算
            IsCritical = false,  // 占位符
            IsMagicDamage = IsMagicDamage(),
            IsTrueDamage = IsTrueDamage(),
            Range = (float)caster.Attribute.AtkRange,
            PenetrationCount = m_Config.PenetrationCount,
            ProjectilePrefabId = m_Config.ProjectilePrefabId,
            ProjectileSpeed = (float)m_Config.ProjectileSpeed,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = OnAttackHit,
            // ⭐ 2. 设置伤害计算委托（投射物命中时才计算伤害）
            CalculateDamageCallback = (hitTarget) =>
            {
                double damage = CalculateDamage(caster, out bool isCritical);
                DebugEx.Success("HouyiNormalAttack",
                    $"[延迟计算] 伤害 [{caster.gameObject.name}] {caster.Config?.Name} → [{hitTarget.gameObject.name}] {hitTarget.Config?.Name}: {damage:F1}{(isCritical ? " (暴击)" : "")}");
                return (damage, isCritical);
            }
        };

        // 3. 播放普攻特效
        PlayAttackEffect(caster);

        // 4. 启动命中检测（投射物会飞向目标，命中时伤害才会计算）
        DebugEx.Log("HouyiNormalAttack", "[进入检测] 启动投射物检测，伤害延迟到命中时");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Projectile);
        detector.Execute(context);

        // 5. 回复蓝量
        RestoreMana(caster);

        DebugEx.Success("HouyiNormalAttack", "普攻启动完成，投射物已发射（伤害计算延迟到命中时刻）");
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
                DebugEx.Log(
                    "HouyiNormalAttack",
                    $"烈焰箭激活，对 {target.Config?.Name} 附带灼烧效果"
                );
            }
        }
    }

    #endregion
}
