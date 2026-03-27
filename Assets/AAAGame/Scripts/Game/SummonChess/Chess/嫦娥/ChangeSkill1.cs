using UnityEngine;

/// <summary>
/// 嫦娥技能一：朔月飞轮 (ID=23)
/// 向前方发射月形冰刃，往返命中2次
/// 配置表中 HitCount=2 自动处理往返命中
/// </summary>
public class ChangeSkill1 : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 3; // 主动技能
    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.LogModule("ChangeSkill1", "朔月飞轮初始化完成");
    }

    public override bool TryCast()
    {
        if (!base.TryCast())
            return false;

        DebugEx.LogModule(
            "ChangeSkill1",
            $"朔月飞轮释放! 消耗MP={m_Config.MpCost}, 冷却={m_Config.Cooldown}秒"
        );

        return true;
    }

    /// <summary>
    /// 执行技能完整流程
    /// </summary>
    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.ErrorModule("ChangeSkill1", "ExecuteSkill: caster 为 null");
            return;
        }

        // 1. 查找目标
        ChessEntity target = FindNearestEnemy(caster);
        if (target == null)
        {
            DebugEx.WarningModule("ChangeSkill1", "未找到目标");
            return;
        }

        // 2. 计算伤害
        double damage = CalculateDamage(caster, out bool isCritical);

        DebugEx.LogModule(
            "ChangeSkill1",
            $"朔月飞轮伤害: {damage:F1}{(isCritical ? " (暴击)" : "")}"
        );

        // 3. 构建命中检测上下文
        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = caster.transform.forward,
            AttackerCamp = caster.Camp,
            LockedTarget = target,
            TargetPosition = EntityPositionHelper.GetCenterPosition(target), // ⭐ 使用模型中心点
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = m_Config.DamageType == 2,
            IsTrueDamage = m_Config.DamageType == 3,
            Range = (float)m_Config.CastRange,
            MaxHitCount = m_Config.HitCount, // 往返命中2次
            ProjectilePrefabId = m_Config.ProjectilePrefabId,
            ProjectileSpeed = (float)m_Config.ProjectileSpeed,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = OnSkillHit,
        };

        // 4. 播放技能释放特效
        PlaySkillEffect(caster);

        // 5. 执行命中检测（使用投射物检测器）
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Projectile);
        detector.Execute(context);

        DebugEx.LogModule(
            "ChangeSkill1",
            $"朔月飞轮发射完成: 目标={target.Config?.Name}, 命中次数={m_Config.HitCount}"
        );
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 技能命中回调（每命中一次调用）
    /// </summary>
    private void OnSkillHit(ChessEntity target, double damage, bool isCritical)
    {
        if (target == null)
            return;

        DebugEx.LogModule(
            "ChangeSkill1",
            $"朔月飞轮命中: {target.Config?.Name}, 伤害={damage:F1}{(isCritical ? " (暴击)" : "")}"
        );
    }

    #endregion
}
