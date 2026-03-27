using UnityEngine;

/// <summary>
/// 近战攻击命中检测器
/// 通过武器上的 Collider 进行碰撞检测
/// </summary>
public class MeleeHitDetector : HitDetectorBase
{
    public override AttackHitType HitType => AttackHitType.Melee;

    /// <summary>武器碰撞器</summary>
    private ChessWeaponCollider m_WeaponCollider;

    /// <summary>已命中的目标（防止重复命中）</summary>
    private System.Collections.Generic.HashSet<ChessEntity> m_HitTargets;

    /// <summary>当前命中数量</summary>
    private int m_CurrentHitCount;

    public MeleeHitDetector()
    {
        m_HitTargets = new System.Collections.Generic.HashSet<ChessEntity>();
    }

    protected override void DoExecute(HitContext context)
    {
        // 获取武器碰撞器
        m_WeaponCollider = context.Attacker?.GetComponentInChildren<ChessWeaponCollider>();

        if (m_WeaponCollider == null)
        {
            DebugEx.Warning($"[MeleeHitDetector] {context.Attacker?.Config?.Name} 没有武器碰撞器，降级到瞬发模式");
            // 降级到瞬发模式
            FallbackToInstant(context);
            return;
        }

        // 重置状态
        m_HitTargets.Clear();
        m_CurrentHitCount = 0;

        // 设置武器碰撞回调
        m_WeaponCollider.SetHitCallback(OnWeaponHit);
        m_WeaponCollider.SetOwnerCamp(context.AttackerCamp);

        // 启用武器碰撞
        m_WeaponCollider.EnableCollider();

        DebugEx.LogModule("MeleeHitDetector", $"{context.Attacker?.Config?.Name} 启用武器碰撞");

        // 注意：这里不会立即完成，需要等待动画事件调用
        // 这里不调用 Complete()
    }

    /// <summary>
    /// 武器命中回调
    /// </summary>
    private void OnWeaponHit(ChessEntity target)
    {
        if (!IsExecuting || m_CurrentContext == null) return;

        // 检查是否已经命中过
        if (m_HitTargets.Contains(target)) return;

        // 检查命中数量限制
        int maxHits = m_CurrentContext.MaxHitCount > 0 ? m_CurrentContext.MaxHitCount : int.MaxValue;
        if (m_CurrentHitCount >= maxHits) return;

        // 检查是否为敌人
        if (!IsEnemy(target, m_CurrentContext.AttackerCamp)) return;

        // 检查是否存活
        if (target.CurrentState == ChessState.Dead) return;

        // 记录命中
        m_HitTargets.Add(target);
        m_CurrentHitCount++;

        // 造成伤害
        ApplyDamage(target, m_CurrentContext);

        DebugEx.LogModule("MeleeHitDetector", $"武器命中: {target.Config?.Name}，当前命中数: {m_CurrentHitCount}");
    }

    /// <summary>
    /// 结束近战检测（外部调用，如动画结束事件）
    /// </summary>
    public void EndMeleeDetection()
    {
        if (m_WeaponCollider != null)
        {
            m_WeaponCollider.DisableCollider();
            m_WeaponCollider.ClearHitCallback();
        }

        m_HitTargets.Clear();
        Complete();

        DebugEx.LogModule("MeleeHitDetector", $"近战检测结束，命中: {m_CurrentHitCount} 个目标");
    }

    public override void Cancel()
    {
        if (m_WeaponCollider != null)
        {
            m_WeaponCollider.DisableCollider();
            m_WeaponCollider.ClearHitCallback();
        }

        m_HitTargets.Clear();
        base.Cancel();
    }

    /// <summary>
    /// 降级到瞬发模式
    /// </summary>
    private void FallbackToInstant(HitContext context)
    {
        if (context.LockedTarget != null && 
            context.LockedTarget.CurrentState != ChessState.Dead &&
            IsEnemy(context.LockedTarget, context.AttackerCamp))
        {
            ApplyDamage(context.LockedTarget, context);
        }
        Complete();
    }
}
