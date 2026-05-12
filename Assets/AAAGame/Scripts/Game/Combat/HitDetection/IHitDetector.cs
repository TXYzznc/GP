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

public interface IEndableHitDetector : IHitDetector
{
    void End();
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
            DebugEx.Error(nameof(HitDetectorBase), "Execute: context is null");
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
        if (target == null)
            return;

        // 检查目标是否符合目标死亡状态要求
        if (!ChessTargetFinder.IsValidTargetByDeadState(target, context.SkillConfig?.TargetDeadState ?? 0))
        {
            DebugEx.Log(
                nameof(HitDetectorBase),
                $"[命中被过滤] [{context.Attacker.gameObject.name}] {context.Attacker.Config?.Name} → [{target.gameObject.name}] {target.Config?.Name} (目标状态不符)"
            );
            return;
        }

        DebugEx.Log(
            nameof(HitDetectorBase),
            $"[命中] [{context.Attacker.gameObject.name}] {context.Attacker.Config?.Name} → [{target.gameObject.name}] {target.Config?.Name}"
        );

        // ⭐ 1. 动态计算伤害（支持根据目标属性调整）
        double finalDamage = context.BaseDamage;
        bool isCritical = context.IsCritical;

        if (context.CalculateDamageCallback != null)
        {
            DebugEx.Log(
                nameof(HitDetectorBase),
                $"[延迟伤害计算] 调用委托计算 [{context.Attacker.gameObject.name}] {context.Attacker.Config?.Name} → [{target.gameObject.name}] {target.Config?.Name} 的伤害"
            );
            (finalDamage, isCritical) = context.CalculateDamageCallback(target);
            DebugEx.Log(
                nameof(HitDetectorBase),
                $"[延迟伤害计算完成] 最终伤害: {finalDamage:F1}{(isCritical ? " (暴击)" : "")}"
            );
        }
        else
        {
            DebugEx.Log(
                nameof(HitDetectorBase),
                $"[使用基础伤害] 无委托，使用 BaseDamage = {finalDamage:F1}"
            );
        }

        // 2. 播放受击特效
        if (context.HitEffectId > 0)
        {
            CombatVFXManager.PlayEffect(context.HitEffectId, target.transform.position);
        }

        // 3. 造成伤害（传入攻击方属性，用于反伤/吸血等事件链）
        target.Attribute.TakeDamage(
            finalDamage,
            context.IsMagicDamage,
            context.IsTrueDamage,
            isCritical,
            DamageFloatingTextManager.DamageType.普通伤害,
            context.Attacker?.Attribute
        );

        // ⭐ 4. 应用"命中时"的 Buff（BuffTriggerType=1）
        if (context.SkillConfig != null)
        {
            EffectExecutor.ApplyBuffsOnHit(context.SkillConfig, context.Attacker, target);
        }

        // 5. 触发命中回调
        context.OnHitCallback?.Invoke(target, finalDamage, isCritical);
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
