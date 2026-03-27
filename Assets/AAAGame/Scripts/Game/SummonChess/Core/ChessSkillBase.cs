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
        return true;
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

        DebugEx.LogModule(
            GetType().Name,
            $"技能释放成功！消耗MP={m_Config.MpCost}, 原始冷却={m_Config.Cooldown}s, "
                + $"冷却缩减={cdReduce:P0}, 实际冷却={m_CooldownRemain:F1}s"
        );

        return true;
    }

    public float GetCooldownRemaining() => m_CooldownRemain;

    // 核心方法：子类必须实现
    public abstract void ExecuteSkill(ChessEntity caster);

    #endregion

    #region 通用辅助方法

    /// <summary>
    /// 查找最近的敌人
    /// </summary>
    protected ChessEntity FindNearestEnemy(ChessEntity caster)
    {
        if (caster == null)
            return null;

        var enemies = CombatEntityTracker.Instance.GetEnemies(caster.Camp);
        if (enemies == null || enemies.Count == 0)
            return null;

        ChessEntity nearest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy.CurrentState == ChessState.Dead)
                continue;

            float dist = Vector3.Distance(caster.transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 计算技能伤害
    /// </summary>
    protected double CalculateDamage(ChessEntity caster, out bool isCritical)
    {
        // 根据伤害类型选择属性
        double scalingStat =
            m_Config.DamageType == 2
                ? caster.Attribute.SpellPower // 魔法伤害用法强
                : caster.Attribute.AtkDamage; // 物理伤害用攻击力

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

            DebugEx.LogModule(
                GetType().Name,
                $"{caster.Config?.Name} 播放技能特效: ID={m_Config.EffectId}, 高度={spawnHeight:F2}"
            );
        }
    }

    #endregion
}
