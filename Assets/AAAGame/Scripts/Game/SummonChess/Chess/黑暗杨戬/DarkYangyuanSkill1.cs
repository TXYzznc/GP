using UnityEngine;

/// <summary>
/// 黑暗杨戬技能一：天威圣戟·黑暗 (ID=53)
/// 范围清盾技能，物理伤害，攻击力系数 150%
/// 射程 8，范围半径 4
/// 主要用于破除护盾
/// </summary>
public class DarkYangyuanSkill1 : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 3; // 主动技能

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("DarkYangyuanSkill1", "黑暗杨戬技能一初始化完成");
    }

    public override bool TryCast()
    {
        return base.TryCast();
    }

    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.Error("DarkYangyuanSkill1", "ExecuteSkill: caster 为 null");
            return;
        }

        DebugEx.Log("DarkYangyuanSkill1", "执行天威圣戟·黑暗 - 范围清盾技能");

        double damage = CalculateDamage(caster, out bool isCritical);

        // 构建范围检测上下文
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
            MaxHitCount = m_Config.HitCount > 0 ? m_Config.HitCount : 1,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = (hitTarget, dmg, isCrit) =>
            {
                DebugEx.Log("DarkYangyuanSkill1", $"命中 {hitTarget.Config?.Name}，造成伤害 {dmg}");
            }
        };

        PlaySkillEffect(caster);

        // 使用 AOE 检测
        DebugEx.Log("DarkYangyuanSkill1", "[进入检测] 启动 AOE 范围检测");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        DebugEx.Success("DarkYangyuanSkill1", "天威圣戟·黑暗 释放完成");
    }

    #endregion
}
