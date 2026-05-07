using UnityEngine;

/// <summary>
/// 黑暗杨戬普攻：黑暗戟横扫 (ID=52)
/// 近战物理伤害，攻击力系数 120%
/// 所有攻击附带腐蚀效果（由被动处理）
/// </summary>
public class DarkYangyuanNormalAttack : ChessNormalAttackBase
{
    #region 常量

    private const int CORROSION_BUFF_ID = 7; // 腐蚀 Buff ID

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("DarkYangyuanNormalAttack", "黑暗杨戬普攻初始化完成");
    }

    public override void ExecuteAttack(ChessEntity caster, ChessEntity target)
    {
        if (caster == null)
        {
            DebugEx.Error("DarkYangyuanNormalAttack", "ExecuteAttack: caster 为 null");
            return;
        }

        if (target == null)
        {
            DebugEx.Warning("DarkYangyuanNormalAttack", "ExecuteAttack: target 为 null");
            return;
        }

        DebugEx.Log("DarkYangyuanNormalAttack", $"执行普攻 → 目标: {target.Config?.Name}");

        double damage = CalculateDamage(caster, out bool isCritical);

        // ⭐ 1. 构建命中检测上下文（近战瞬发）
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
            IsMagicDamage = m_Config.DamageType == 2,
            IsTrueDamage = m_Config.DamageType == 3,
            Range = (float)caster.Attribute.AtkRange,
            MaxHitCount = m_Config.HitCount > 0 ? m_Config.HitCount : 1,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = (hitTarget, dmg, isCrit) => TriggerCorrosion(caster, hitTarget)
        };

        PlayAttackEffect(caster);

        DebugEx.Log("DarkYangyuanNormalAttack", "[进入检测] 启动近战检测");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Melee);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        RestoreMana(caster);

        DebugEx.Success("DarkYangyuanNormalAttack", "普攻启动完成");
    }

    #endregion

    #region 私有方法

    private void TriggerCorrosion(ChessEntity caster, ChessEntity target)
    {
        if (target?.BuffManager == null)
            return;

        // 附带腐蚀 Buff
        target.BuffManager.AddBuff(CORROSION_BUFF_ID, caster.gameObject, caster.Attribute);
        DebugEx.Log("DarkYangyuanNormalAttack", $"黑暗戟横扫触发腐蚀！对 {target.Config?.Name} 施加腐蚀效果");
    }

    #endregion
}
