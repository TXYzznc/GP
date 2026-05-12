using UnityEngine;

/// <summary>
/// 恶魂普攻：黑暗能量球 (ID=42)
/// 发射投射物造成法术伤害，20%概率使目标陷入恐惧（攻击力-20%，持续2s）
/// </summary>
public class EvilSoulNormalAttack : ChessNormalAttackBase
{
    #region 常量

    private const float FEAR_TRIGGER_CHANCE = 0.2f;

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("EvilSoulNormalAttack", "恶魂普攻初始化完成");
    }

    public override void ExecuteAttack(ChessEntity caster, ChessEntity target)
    {
        if (caster == null)
        {
            DebugEx.Error("EvilSoulNormalAttack", "ExecuteAttack: caster 为 null");
            return;
        }

        if (target == null)
        {
            DebugEx.Warning("EvilSoulNormalAttack", "ExecuteAttack: target 为 null");
            return;
        }

        DebugEx.Log("EvilSoulNormalAttack", $"[{caster.gameObject.name}] 执行普攻 → 目标: [{target.gameObject.name}] {target.Config?.Name}");

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
            MaxHitCount = m_Config.HitCount > 0 ? m_Config.HitCount : 1,
            PenetrationCount = m_Config.PenetrationCount,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = (hitTarget, dmg, isCrit) => TriggerFear(caster, hitTarget),
            // ⭐ 2. 设置伤害计算委托（投射物命中时才计算伤害）
            CalculateDamageCallback = (hitTarget) =>
            {
                double damage = CalculateDamage(caster, out bool isCritical);
                DebugEx.Success("EvilSoulNormalAttack",
                    $"[延迟计算] 伤害 [{caster.gameObject.name}] {caster.Config?.Name} → [{hitTarget.gameObject.name}] {hitTarget.Config?.Name}: {damage:F1}{(isCritical ? " (暴击)" : "")}");
                return (damage, isCritical);
            }
        };

        PlayAttackEffect(caster);

        DebugEx.Log("EvilSoulNormalAttack", "[进入检测] 启动投射物检测，伤害延迟到命中时");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Projectile);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        RestoreMana(caster);

        DebugEx.Success("EvilSoulNormalAttack", "普攻启动完成，投射物已发射（伤害计算延迟到命中时刻）");
    }

    #endregion

    #region 私有方法

    private void TriggerFear(ChessEntity caster, ChessEntity target)
    {
        if (target?.BuffManager == null || Random.value > FEAR_TRIGGER_CHANCE)
            return;

        target.BuffManager.AddBuff(9, caster.gameObject, caster.Attribute); // 恐惧 ID=9

        DebugEx.Log("EvilSoulNormalAttack", $"黑暗能量球触发恐惧！对 {target.Config?.Name} 施加恐惧效果");
    }

    #endregion
}
