using UnityEngine;

/// <summary>
/// 棋子普攻基类
/// 提供通用方法，减少子类重复代码
/// </summary>
public abstract class ChessNormalAttackBase : IChessNormalAttack
{
    #region 字段

    protected ChessContext m_Ctx;
    protected SummonChessSkillTable m_Config;

    #endregion

    #region 接口实现

    public int AttackId => m_Config?.Id ?? 0;
    public SummonChessSkillTable Config => m_Config;

    public virtual void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        m_Ctx = ctx;
        m_Config = config;
    }

    // 核心方法：子类必须实现
    public abstract void ExecuteAttack(ChessEntity caster, ChessEntity target);

    /// <summary>
    /// 回复蓝量（普攻结束后）
    /// 子类在 ExecuteAttack 末尾调用此方法
    /// </summary>
    protected void RestoreMana(ChessEntity caster)
    {
        if (m_Config != null && m_Config.MpRestore > 0)
        {
            caster.Attribute.ModifyMp(m_Config.MpRestore);
            DebugEx.LogModule(
                GetType().Name,
                $"{caster.Config?.Name} 回复蓝量: {m_Config.MpRestore}"
            );
        }
    }

    #endregion

    #region 通用辅助方法

    /// <summary>
    /// 计算普攻伤害
    /// </summary>
    protected double CalculateDamage(ChessEntity caster, out bool isCritical)
    {
        double damage = caster.Attribute.AtkDamage * m_Config.DamageCoeff + m_Config.BaseDamage;

        isCritical = Random.value < caster.Attribute.CritRate;
        if (isCritical)
        {
            damage *= caster.Attribute.CritDamage;
        }

        return damage;
    }

    /// <summary>
    /// 播放普攻特效
    /// </summary>
    protected void PlayAttackEffect(ChessEntity caster)
    {
        if (m_Config.EffectId > 0)
        {
            float spawnHeight = m_Config.EffectSpawnHeight;
            Vector3 effectPosition = caster.GetEffectSpawnPosition(spawnHeight);
            Vector3 offset = effectPosition - caster.transform.position;

            CombatVFXManager.PlaySkillEffect(caster.transform, m_Config.EffectId, offset);
        }
    }

    #endregion
}
