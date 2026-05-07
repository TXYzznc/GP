using UnityEngine;

/// <summary>
/// 黑暗杨戬大招：堕落劈山 (ID=55)
/// 真实伤害大招，攻击力系数 200%
/// 射程 15，范围半径 8
/// 留下黑暗裂缝，对范围内的所有敌人造成伤害
/// </summary>
public class DarkYangyuanUltimate : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 4; // 大招

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("DarkYangyuanUltimate", "黑暗杨戬大招初始化完成");
    }

    public override bool TryCast()
    {
        return base.TryCast();
    }

    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.Error("DarkYangyuanUltimate", "ExecuteSkill: caster 为 null");
            return;
        }

        DebugEx.Log("DarkYangyuanUltimate", "执行堕落劈山 - 真实伤害大招");

        double damage = CalculateDamage(caster, out bool isCritical);

        // 构建范围检测上下文（真实伤害）
        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = caster.transform.forward,
            AttackerCamp = caster.Camp,
            TargetPosition = caster.transform.position,
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = false,
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
                DebugEx.Success("DarkYangyuanUltimate", $"堕落劈山命中 {hitTarget.Config?.Name}，造成真实伤害 {dmg}");
            }
        };

        PlaySkillEffect(caster);

        // 使用 AOE 检测（范围大招）
        DebugEx.Log("DarkYangyuanUltimate", "[进入检测] 启动 AOE 范围检测 (真实伤害)");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        DebugEx.Success("DarkYangyuanUltimate", "堕落劈山 释放完成，黑暗裂缝已留下");
    }

    #endregion
}
