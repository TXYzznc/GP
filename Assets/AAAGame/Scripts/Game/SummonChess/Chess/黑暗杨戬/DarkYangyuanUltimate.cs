using UnityEngine;

/// <summary>
/// 黑暗杨戬大招：堕落劈山 (ID=55)
/// 真实伤害大招，攻击力系数 200%
/// 射程 15，范围半径 8
/// 留下黑暗裂缝，对范围内的所有敌人造成伤害
///
/// 大招分为两段动画：
/// - 第一段（大招）：蓄力/前置动画
/// - 第二段（大招Start）：真正的伤害动画，关键帧触发 AnimEvent_Skill2Execute
/// - 动画帧事件在关键帧触发伤害检测
/// </summary>
public class DarkYangyuanUltimate : ChessSkillBase
{
    #region 字段

    private ChessEntity m_CurrentCaster;

    #endregion

    #region 接口实现

    public override int SkillType => 4; // 大招

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);

        var animator = ctx.Entity?.Animator;
        if (animator != null)
        {
            // 确保使用黑暗杨戬专属的事件接收器
            var animReceiver = animator.EventReceiver as DarkYangyuanAnimationEventReceiver;
            if (animReceiver == null)
            {
                // 需要替换为DarkYangyuanAnimationEventReceiver
                animReceiver = animator.gameObject.GetComponent<DarkYangyuanAnimationEventReceiver>();
                if (animReceiver == null)
                {
                    // 移除通用接收器，添加专属接收器
                    var oldReceiver = animator.EventReceiver;
                    if (oldReceiver != null)
                    {
                        Object.DestroyImmediate(oldReceiver);
                    }
                    animReceiver = animator.gameObject.AddComponent<DarkYangyuanAnimationEventReceiver>();
                    animator.SetEventReceiver(animReceiver);
                }
            }

            // 订阅大招伤害帧事件
            animReceiver.OnSkill2Execute += OnDamageFrame;
        }

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

        m_CurrentCaster = caster;

        DebugEx.Log("DarkYangyuanUltimate", "启动大招：堕落劈山");

        // 播放大招动画（两段自动依次播放）
        // 大招Start 动画的关键帧会触发 AnimEvent_Skill2Execute，进而调用 OnDamageFrame
        if (caster.Animator != null)
        {
            caster.Animator.PlaySkill2(duration: 1.5f);
        }

        PlaySkillEffect(caster);
    }

    #endregion

    #region 动画帧事件回调

    /// <summary>
    /// 动画帧事件：在大招第二段的关键帧触发
    /// 由 AnimEvent_Skill2Execute 回调调用
    /// </summary>
    private void OnDamageFrame()
    {
        if (m_CurrentCaster == null || m_CurrentCaster.Attribute.IsDead)
        {
            DebugEx.Warning("DarkYangyuanUltimate", "伤害帧触发时，施法者已死亡或不存在");
            return;
        }

        DebugEx.Log("DarkYangyuanUltimate", "大招伤害帧触发：执行伤害检测");

        ExecuteUltimateDamage(m_CurrentCaster);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 执行大招伤害检测
    /// </summary>
    private void ExecuteUltimateDamage(ChessEntity caster)
    {
        double damage = CalculateDamage(caster, out bool isCritical);

        // 构建范围检测上下文（特殊类型伤害）
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

        // 使用范围检测（AOE）
        DebugEx.Log("DarkYangyuanUltimate", "[进入检测] 启动 AOE 范围检测 (真实伤害)");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        DebugEx.Success("DarkYangyuanUltimate", "堕落劈山 释放完成，黑暗裂缝已留下");
    }

    #endregion
}
