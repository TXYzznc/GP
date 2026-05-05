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

        // 1. 计算伤害
        double damage = CalculateDamage(caster, out bool isCritical);
        DebugEx.Log("EvilSpiritNormalAttack", $"普攻伤害: {damage:F1}{(isCritical ? " (暴击)" : "")}");

        // 2. 构建命中检测上下文（近战：武器Collider持续检测）
        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = caster.transform.forward,
            AttackerCamp = caster.Camp,
            LockedTarget = target,
            TargetPosition = EntityPositionHelper.GetCenterPosition(target),
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = false,
            IsTrueDamage = false,
            Range = (float)caster.Attribute.AtkRange,
            MaxHitCount = m_Config.HitCount > 0 ? m_Config.HitCount : 1,
            PenetrationCount = m_Config.PenetrationCount,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
        };

        // 3. 播放普攻特效
        PlayAttackEffect(caster);

        // 4. 执行命中检测（近战碰撞）
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Melee);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        // 5. 回复蓝量
        RestoreMana(caster);

        DebugEx.Log("EvilSpiritNormalAttack", "普攻执行完成");
    }

    #endregion
}
