using UnityEngine;

/// <summary>
/// 邪灵技能一：污染斩击 (ID=33)
/// 向前方挥出巨大斩击，对前方AOE范围内所有敌人造成大量物理伤害
/// 命中后对目标施加2层腐蚀Buff（ID=7）
/// </summary>
public class EvilSpiritSkill1 : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 3; // 主动技能
    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("EvilSpiritSkill1", "污染斩击技能初始化完成");
    }

    public override bool TryCast()
    {
        if (!base.TryCast())
            return false;
        DebugEx.Log("EvilSpiritSkill1", "污染斩击释放");
        return true;
    }

    /// <summary>
    /// 执行技能：前方AOE物理伤害 + 2层腐蚀
    /// </summary>
    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.Error("EvilSpiritSkill1", "ExecuteSkill: caster 为 null");
            return;
        }

        // 计算物理伤害
        double damage = CalculateDamage(caster, out bool isCritical);

        DebugEx.Log(
            "EvilSpiritSkill1",
            $"执行污染斩击，伤害: {damage:F1}，AOE半径: {m_Config.AreaRadius}"
        );

        // 构建AOE命中上下文（以施法者前方为中心）
        Vector3 aoeCenter =
            caster.transform.position
            + caster.transform.forward * (float)(m_Config.CastRange * 0.5f);

        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = caster.transform.forward,
            AttackerCamp = caster.Camp,
            TargetPosition = aoeCenter,
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = false,
            IsTrueDamage = false,
            Range = (float)m_Config.CastRange,
            AOERadius = (float)m_Config.AreaRadius,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = (target, dmg, isCrit) => ApplyCorrosion(caster, target),
        };

        // 播放技能特效
        PlaySkillEffect(caster);

        // AOE命中检测
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        detector.Execute(context);

        DebugEx.Success("EvilSpiritSkill1", "污染斩击执行完成");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 对命中目标施加2层腐蚀
    /// </summary>
    private void ApplyCorrosion(ChessEntity caster, ChessEntity target)
    {
        if (target?.BuffManager == null)
            return;

        // 连续AddBuff两次叠加2层（MaxStack=2由BuffTable控制上限）
        target.BuffManager.AddBuff(7, caster.gameObject, caster.Attribute);
        target.BuffManager.AddBuff(7, caster.gameObject, caster.Attribute);

        DebugEx.Log("EvilSpiritSkill1", $"对 {target.Config?.Name} 施加2层腐蚀");
    }

    #endregion
}
