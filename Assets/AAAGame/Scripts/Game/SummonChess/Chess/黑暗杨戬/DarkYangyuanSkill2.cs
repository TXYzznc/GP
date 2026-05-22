using UnityEngine;

/// <summary>
/// 黑暗杨戬技能二：三眼天火 (ID=54)
/// 直线法术伤害技能，魔法伤害，攻击力系数 120%
/// 射程 9，范围半径 6
/// 施加灼烧效果
///
/// 执行流程：
/// 1. ExecuteSkill 播放动画
/// 2. 动画帧事件 AnimEvent_Skill2TrueExecute 触发
/// 3. OnSkill2TrueExecute 事件回调执行伤害检测
/// </summary>
public class DarkYangyuanSkill2 : ChessSkillBase
{
    #region 字段

    private ChessEntity m_CurrentCaster;

    #endregion

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

            // 订阅技能二伤害帧事件
            animReceiver.OnSkill2TrueExecute += OnSkillFrame;
        }

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

        m_CurrentCaster = caster;

        DebugEx.Log("DarkYangyuanSkill2", "启动技能二：三眼天火");

        // 播放技能二动画，动画帧事件会触发伤害执行
        if (caster.Animator != null)
        {
            caster.Animator.PlaySkill2True(duration: 1.0f);
        }

        PlaySkillEffect(caster);
    }

    #endregion

    #region 动画帧事件回调

    /// <summary>
    /// 动画帧事件：技能二伤害执行
    /// 由 AnimEvent_Skill2TrueExecute 回调触发
    /// </summary>
    private void OnSkillFrame()
    {
        if (m_CurrentCaster == null || m_CurrentCaster.Attribute.IsDead)
        {
            DebugEx.Warning("DarkYangyuanSkill2", "伤害帧触发时，施法者已死亡或不存在");
            return;
        }

        DebugEx.Log("DarkYangyuanSkill2", "技能二伤害帧触发：执行伤害检测");

        ExecuteSkill2Damage(m_CurrentCaster);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 执行技能二伤害检测
    /// </summary>
    private void ExecuteSkill2Damage(ChessEntity caster)
    {
        double damage = CalculateDamage(caster, out bool isCritical);

        // 构建射线检测上下文
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

        // 使用射线检测（直线范围技能）
        DebugEx.Log("DarkYangyuanSkill2", "[进入检测] 启动射线检测");
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Raycast);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        DebugEx.Success("DarkYangyuanSkill2", "三眼天火 释放完成");
    }

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
