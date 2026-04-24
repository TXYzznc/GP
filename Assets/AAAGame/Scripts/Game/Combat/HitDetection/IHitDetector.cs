using System;

/// <summary>
/// 命中检测器接口
/// 所有命中检测类型都实现此接口
/// </summary>
public interface IHitDetector
{
    /// <summary>
    /// 检测器类型
    /// </summary>
    AttackHitType HitType { get; }

    /// <summary>
    /// 执行命中检测
    /// </summary>
    /// <param name="context">命中上下文</param>
    void Execute(HitContext context);

    /// <summary>
    /// 取消检测（用于打断）
    /// </summary>
    void Cancel();

    /// <summary>
    /// 是否正在执行检测
    /// </summary>
    bool IsExecuting { get; }
}

/// <summary>
/// 命中检测器基类
/// 提供通用功能实现
/// </summary>
public abstract class HitDetectorBase : IHitDetector
{
    #region 属性

    public abstract AttackHitType HitType { get; }

    public bool IsExecuting { get; protected set; }

    protected HitContext m_CurrentContext;

    #endregion

    #region 接口实现

    public virtual void Execute(HitContext context)
    {
        if (context == null)
        {
            DebugEx.ErrorModule("HitDetector", "Execute: context is null");
            return;
        }

        m_CurrentContext = context;
        IsExecuting = true;

        DoExecute(context);
    }

    public virtual void Cancel()
    {
        IsExecuting = false;
        m_CurrentContext = null;
    }

    #endregion

    #region 抽象方法

    /// <summary>
    /// 执行具体的检测逻辑（子类实现）
    /// </summary>
    protected abstract void DoExecute(HitContext context);

    #endregion

    #region 辅助方法

    /// <summary>
    /// 对目标造成伤害
    /// </summary>
    protected void ApplyDamage(ChessEntity target, HitContext context)
    {
        if (target == null || target.CurrentState == ChessState.Dead)
            return;

        DebugEx.LogModule(
            "HitDetector",
            $"[命中] {context.Attacker.Config?.Name} → {target.Config?.Name}"
        );

        // 1. 播放受击特效
        if (context.HitEffectId > 0)
        {
            CombatVFXManager.PlayEffect(context.HitEffectId, target.transform.position);
        }

        // 2. 造成伤害（传入攻击方属性，用于反伤/吸血等事件链）
        target.Attribute.TakeDamage(
            context.BaseDamage,
            context.IsMagicDamage,
            context.IsTrueDamage,
            context.IsCritical,
            DamageFloatingTextManager.DamageType.普通伤害,
            context.Attacker?.Attribute
        );

        // ⭐ 3. 应用"命中时"的 Buff（BuffTriggerType=1）
        if (context.SkillConfig != null)
        {
            EffectExecutor.ApplyBuffsOnHit(context.SkillConfig, context.Attacker, target);
        }

        // 4. 触发命中回调
        context.OnHitCallback?.Invoke(target, context.BaseDamage, context.IsCritical);
    }

    /// <summary>
    /// 检测完成
    /// </summary>
    protected void Complete()
    {
        IsExecuting = false;
        m_CurrentContext = null;
    }

    /// <summary>
    /// 检查目标是否为敌人
    /// </summary>
    protected bool IsEnemy(ChessEntity target, int attackerCamp)
    {
        // ✅ 使用阵营服务判断
        return CampRelationService.IsValidTarget(target, attackerCamp);
    }

    #endregion
}
