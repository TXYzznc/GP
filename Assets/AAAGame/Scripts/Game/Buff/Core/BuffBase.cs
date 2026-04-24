using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// Buff 基类，实现了通用的计时和叠层逻辑
/// </summary>
public abstract class BuffBase : IBuff
{
    public int BuffId => Config != null ? Config.Id : 0;
    public int StackCount { get; protected set; } = 1;
    public bool IsFinished { get; protected set; } = false;

    /// <summary>激活条件，null 表示始终激活</summary>
    public System.Func<bool> ActivationCondition { get; set; }

    protected BuffContext Ctx;
    protected BuffTable Config;

    // 计时器
    protected float DurationRemain;
    protected float IntervalTimer;

    public virtual void Init(BuffContext ctx, BuffTable config)
    {
        // 深拷贝上下文，避免多个 Buff 共享同一 Context 导致施法者信息错乱
        Ctx = new BuffContext
        {
            Owner = ctx.Owner,
            Transform = ctx.Transform,
            Caster = ctx.Caster,
            OwnerAttribute = ctx.OwnerAttribute,
            CasterAttribute = ctx.CasterAttribute,
            OwnerBuffManager = ctx.OwnerBuffManager,
        };
        Config = config;

        // 初始化剩余时间
        DurationRemain = (float)Config.Duration;

        // 初始化叠层数量
        StackCount = 1;

        IsFinished = false;
        IntervalTimer = 0f;
    }

    public virtual void OnEnter()
    {
        // 播放 Buff 特效
        if (Config.EffectId > 0 && Ctx.Transform != null)
        {
            CombatVFXManager.PlayBuffEffect(Ctx.Transform, Config.EffectId);
        }
    }

    public virtual void OnUpdate(float dt)
    {
        if (IsFinished)
            return;

        // 1. 更新剩余时间
        // Duration <= 0 表示永久 Buff，不需要倒计时
        if (Config.Duration > 0)
        {
            DurationRemain -= dt;
            if (DurationRemain <= 0)
            {
                IsFinished = true;
                return;
            }
        }

        // 2. 更新周期性触发
        if (Config.Interval > 0)
        {
            IntervalTimer += dt;
            if (IntervalTimer >= Config.Interval)
            {
                IntervalTimer -= (float)Config.Interval;
                OnTick();
            }
        }
    }

    public virtual void OnExit()
    {
        // 停止 Buff 特效
        if (Config.EffectId > 0 && Ctx.Transform != null)
        {
            CombatVFXManager.StopBuffEffect(Ctx.Transform, Config.EffectId);
        }
    }

    public virtual bool OnStack()
    {
        // 默认叠层逻辑：
        // 1. 刷新持续时间
        if (Config.Duration > 0)
        {
            DurationRemain = (float)Config.Duration;
        }

        // 2. 增加层数
        if (StackCount < Config.MaxStack)
        {
            StackCount++;
            OnStackCountChanged();
        }

        return true;
    }

    /// <summary>
    /// 减少层数（用于融化效果消耗灼烧层数）
    /// </summary>
    /// <param name="count">要减少的层数</param>
    public virtual void ReduceStacks(int count)
    {
        if (count <= 0)
            return;

        int oldStacks = StackCount;
        StackCount = Mathf.Max(0, StackCount - count);

        if (StackCount != oldStacks)
        {
            OnStackCountChanged();
        }

        // 如果层数归零则结束
        if (StackCount <= 0)
        {
            IsFinished = true;
        }
    }

    /// <summary>
    /// 周期性触发逻辑（子类实现）
    /// </summary>
    protected virtual void OnTick() { }

    /// <summary>
    /// 层数变化时调用
    /// </summary>
    protected virtual void OnStackCountChanged() { }
}
