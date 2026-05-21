using UnityEngine;

/// <summary>
/// 棋子技能基类
/// 提供通用方法，减少子类重复代码
/// </summary>
public abstract class ChessSkillBase : IChessSkill
{
    #region 字段

    protected ChessContext m_Ctx;
    protected SummonChessSkillTable m_Config;
    protected float m_CooldownRemain;
    protected float m_LastCastTime = -float.MaxValue; // 上次释放时间，初始时视为很久前

    #endregion

    #region 接口实现

    public int SkillId => m_Config?.Id ?? 0;
    public abstract int SkillType { get; }
    public SummonChessSkillTable Config => m_Config;

    public virtual void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        m_Ctx = ctx;
        m_Config = config;
        m_CooldownRemain = 0f;
    }

    public virtual void Tick(float dt)
    {
        if (m_CooldownRemain > 0)
        {
            m_CooldownRemain -= dt;
        }
    }

    public virtual bool CanCast()
    {
        if (m_CooldownRemain > 0)
            return false;
        if (m_Ctx?.Attribute == null)
            return false;
        if (m_Ctx.Attribute.CurrentMp < m_Config.MpCost)
            return false;
        // 检查沉默状态
        if (IsEntitySilenced())
            return false;
        return true;
    }

    /// <summary>
    /// 检查实体是否被沉默
    /// </summary>
    private bool IsEntitySilenced()
    {
        if (m_Ctx?.Entity == null)
            return false;

        return m_Ctx.Entity.IsSilenced;
    }

    public virtual bool TryCast()
    {
        if (!CanCast())
            return false;

        // 消耗法力
        m_Ctx.Attribute.ModifyMp(-m_Config.MpCost);

        // 进入冷却（应用冷却缩减）
        double cdReduce = m_Ctx.Attribute.CooldownReduce;
        m_CooldownRemain = (float)(m_Config.Cooldown * (1.0 - cdReduce));

        // 记录释放时间
        m_LastCastTime = Time.time;

        DebugEx.Log(
            GetType().Name,
            $"技能释放成功！消耗MP={m_Config.MpCost}, 原始冷却={m_Config.Cooldown}s, "
                + $"冷却缩减={cdReduce:P0}, 实际冷却={m_CooldownRemain:F1}s"
        );

        return true;
    }

    public float GetCooldownRemaining() => m_CooldownRemain;

    /// <summary>
    /// 计算技能欲望值（用于优先级决策）
    /// DesireValue = Strength × min(TimeSinceLastCast / MaxWaitSeconds, 1.5)
    /// </summary>
    public float GetDesireValue(float currentTime)
    {
        if (!CanCast())
            return 0f; // 无法释放则欲望值为0

        float timeSinceLastCast = currentTime - m_LastCastTime;
        float maxWaitSeconds = (float)m_Config.MaxWaitSeconds;

        if (maxWaitSeconds <= 0)
            maxWaitSeconds = 1f; // 防止除零

        float ratio = timeSinceLastCast / maxWaitSeconds;
        float cappedRatio = Mathf.Min(ratio, 1.5f); // 上限为1.5倍

        return m_Config.Strength * cappedRatio;
    }

    // 核心方法：子类必须实现
    public abstract void ExecuteSkill(ChessEntity caster);

    #endregion

    #region 通用辅助方法

    /// <summary>
    /// 查找最近的敌人
    /// </summary>
    protected ChessEntity FindNearestEnemy(ChessEntity caster)
    {
        return ChessTargetFinder.FindNearestEnemy(caster);
    }

    /// <summary>
    /// 检查目标是否在施法范围内
    /// </summary>
    protected bool IsInCastRange(ChessEntity caster, ChessEntity target)
    {
        if (caster == null || target == null || m_Config == null)
            return false;

        return ChessTargetFinder.IsInCastRange(caster, target, m_Config.CastRange);
    }

    /// <summary>
    /// 计算技能伤害
    /// </summary>
    protected double CalculateDamage(ChessEntity caster, out bool isCritical)
    {
        // 根据伤害类型选择属性
        double scalingStat = 0;
        switch (m_Config.DamageType)
        {
            case 1: // 物理伤害
            case 3: // 真实伤害（使用攻击力，但不受护甲影响）
                scalingStat = caster.Attribute.AtkDamage;
                break;
            case 2: // 魔法伤害
                scalingStat = caster.Attribute.SpellPower;
                break;
            default:
                scalingStat = caster.Attribute.AtkDamage;
                break;
        }

        double damage = scalingStat * m_Config.DamageCoeff + m_Config.BaseDamage;

        // 暴击判定
        isCritical = Random.value < caster.Attribute.CritRate;
        if (isCritical)
        {
            damage *= caster.Attribute.CritDamage;
        }

        return damage;
    }

    /// <summary>
    /// 播放技能释放特效
    /// </summary>
    protected void PlaySkillEffect(ChessEntity caster)
    {
        if (m_Config.EffectId > 0)
        {
            float spawnHeight = m_Config.EffectSpawnHeight;
            Vector3 effectPosition = caster.GetEffectSpawnPosition(spawnHeight);
            Vector3 offset = effectPosition - caster.transform.position;

            CombatVFXManager.PlaySkillEffect(caster.transform, m_Config.EffectId, offset);

            DebugEx.Log(
                GetType().Name,
                $"{caster.Config?.Name} 播放技能特效: ID={m_Config.EffectId}, 高度={spawnHeight:F2}"
            );
        }
    }

    #endregion
}
