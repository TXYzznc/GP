using UnityEngine;

/// <summary>
/// 邪灵：近战瞬发普通攻击 (ID=101)
/// 物理伤害，100%攻击系数
/// </summary>
public class EvilSpiritNormalAttack : ChessNormalAttackBase
{
    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("EvilSpiritNormalAttack", "邪灵普攻初始化完成");
    }

    /// <summary>
    /// 执行普攻完整流程
    /// </summary>
    public override void ExecuteAttack(ChessEntity caster, ChessEntity target)
    {
        if (caster == null)
        {
            DebugEx.Error("EvilSpiritNormalAttack", "ExecuteAttack: caster 为 null");
            return;
        }

        if (target == null)
        {
            DebugEx.Warning("EvilSpiritNormalAttack", "ExecuteAttack: target 为 null");
            return;
        }

        DebugEx.Log("EvilSpiritNormalAttack", $"执行普攻 → 目标: {target.Config?.Name}");

        // ⭐ 1. 构建命中检测上下文（延迟伤害计算到碰撞时刻）
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
            MaxHitCount = m_Config.HitCount > 0 ? m_Config.HitCount : 1,
            PenetrationCount = m_Config.PenetrationCount,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            // ⭐ 2. 设置伤害计算委托（在碰撞时根据目标属性动态计算）
            CalculateDamageCallback = (hitTarget) =>
            {
                double damage = CalculateDamage(caster, out bool isCritical);
                DebugEx.Success("EvilSpiritNormalAttack",
                    $"[延迟计算] 伤害 {caster.Config?.Name} → {hitTarget.Config?.Name}: {damage:F1}{(isCritical ? " (暴击)" : "")}");
                return (damage, isCritical);
            }
        };

        // 3. 播放普攻特效
        PlayAttackEffect(caster);

        // 4. 启动碰撞检测（只是启动，伤害在碰撞时计算）
        DebugEx.Log("EvilSpiritNormalAttack", "[进入检测] 启动近战碰撞检测，伤害延迟到碰撞时");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Melee);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        // 5. 回复蓝量
        RestoreMana(caster);

        DebugEx.Success("EvilSpiritNormalAttack", "普攻启动完成（伤害计算延迟到碰撞时刻）");
    }

    #endregion
}
