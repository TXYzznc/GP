using UnityEngine;

/// <summary>
/// 黑暗杨戬技能二：三眼天火 (ID=54)
/// 直线法术伤害技能，魔法伤害，攻击力系数 120%
/// 射程 9，范围半径 6
/// 施加灼烧效果
/// </summary>
public class DarkYangyuanSkill2 : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 3; // 主动技能

    #endregion

    #region 常量

    private const int BURN_BUFF_ID = 1; // 灼烧 Buff ID

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("DarkYangyuanSkill2", "黑暗杨戬技能二初始化完成");
    }

    public override bool TryCast()
    {
        return base.TryCast();
    }

    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.Error("DarkYangyuanSkill2", "ExecuteSkill: caster 为 null");
            return;
        }

        DebugEx.Log("DarkYangyuanSkill2", "执行三眼天火 - 范围法术伤害");

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
            OnHitCallback = (hitTarget, dmg, isCrit) => TriggerBurn(caster, hitTarget)
        };

        PlaySkillEffect(caster);

        // 使用 AOE 检测
        DebugEx.Log("DarkYangyuanSkill2", "[进入检测] 启动 AOE 范围检测");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        DebugEx.Success("DarkYangyuanSkill2", "三眼天火 释放完成");
    }

    #endregion

    #region 私有方法

    private void TriggerBurn(ChessEntity caster, ChessEntity target)
    {
        if (target?.BuffManager == null)
            return;

        // 施加灼烧效果
        target.BuffManager.AddBuff(BURN_BUFF_ID, caster.gameObject, caster.Attribute);
        DebugEx.Log("DarkYangyuanSkill2", $"三眼天火触发灼烧！对 {target.Config?.Name} 施加灼烧效果");
    }

    #endregion
}
