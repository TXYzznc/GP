using UnityEngine;

/// <summary>
/// 恶魂大招：邪念潮涌 (ID=44)
/// 释放压抑已久的邪念怨恨，对范围内所有敌人造成法术伤害
/// 施加混乱状态，并60%概率造成恐惧
/// </summary>
public class EvilSoulUltimate : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 4; // 大招

    #endregion

    #region 常量

    private const float FEAR_TRIGGER_CHANCE = 0.6f;

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("EvilSoulUltimate", "邪念潮涌技能初始化完成");
    }

    public override bool TryCast()
    {
        if (!base.TryCast())
            return false;

        DebugEx.Log("EvilSoulUltimate", "邪念潮涌释放");
        return true;
    }

    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.Error("EvilSoulUltimate", "ExecuteSkill: caster 为 null");
            return;
        }

        double damage = CalculateDamage(caster, out bool isCritical);
        DebugEx.Log("EvilSoulUltimate", $"邪念潮涌伤害: {damage:F1}{(isCritical ? " (暴击)" : "")}");

        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = caster.transform.forward,
            AttackerCamp = caster.Camp,
            TargetPosition = caster.transform.position,
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = m_Config.DamageType == 2,
            IsTrueDamage = m_Config.DamageType == 3,
            Range = (float)caster.Attribute.AtkRange,
            AOERadius = (float)m_Config.AreaRadius,
            MaxHitCount = m_Config.HitCount,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = (target, dmg, isCrit) => ApplyDebuff(caster, target),
        };

        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        DebugEx.Success("EvilSoulUltimate", "邪念潮涌执行完成");
    }

    #endregion

    #region 私有方法

    private void ApplyDebuff(ChessEntity caster, ChessEntity target)
    {
        if (target?.BuffManager == null)
            return;

        // 必定施加混乱
        target.BuffManager.AddBuff(5009, caster.gameObject, caster.Attribute); // 混乱 ID=5009

        // 60%概率施加恐惧
        if (Random.value < FEAR_TRIGGER_CHANCE)
        {
            target.BuffManager.AddBuff(9, caster.gameObject, caster.Attribute); // 恐惧 ID=9
            DebugEx.Log("EvilSoulUltimate", $"对 {target.Config?.Name} 额外施加恐惧");
        }

        DebugEx.Log("EvilSoulUltimate", $"对 {target.Config?.Name} 施加混乱");
    }

    #endregion
}
