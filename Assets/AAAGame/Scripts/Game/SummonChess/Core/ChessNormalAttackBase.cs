using UnityEngine;

/// <summary>
/// 棋子普攻基类
/// 提供通用方法，减少子类重复代码
///
/// 设计说明：
/// - OnAttackExecute 只启动碰撞检测，不计算伤害
/// - 伤害计算延迟到碰撞时刻，根据实际击中的目标属性进行动态调整
/// - 这样可以支持多目标时每个目标有不同伤害值的场景
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
            DebugEx.Log(
                GetType().Name,
                $"{caster.Config?.Name} 回复蓝量: {m_Config.MpRestore}"
            );
        }
    }

    #endregion

    #region 通用辅助方法

    /// <summary>
    /// 计算普攻伤害（根据配置的伤害类型选择属性）
    /// DamageType: 0=无伤害, 1=物理(AtkDamage), 2=魔法(SpellPower), 3=真实(SpellPower)
    /// </summary>
    protected double CalculateDamage(ChessEntity caster, out bool isCritical)
    {
        double scalingStat = 0;

        // 根据伤害类型选择属性
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
                scalingStat = caster.Attribute.AtkDamage; // 默认物理
                break;
        }

        double damage = scalingStat * m_Config.DamageCoeff + m_Config.BaseDamage;

        isCritical = Random.value < caster.Attribute.CritRate;
        if (isCritical)
        {
            damage *= caster.Attribute.CritDamage;
        }

        return damage;
    }

    /// <summary>
    /// 根据 DamageType 判断是否为魔法伤害
    /// </summary>
    protected bool IsMagicDamage()
    {
        return m_Config.DamageType == 2;
    }

    /// <summary>
    /// 根据 DamageType 判断是否为真实伤害
    /// </summary>
    protected bool IsTrueDamage()
    {
        return m_Config.DamageType == 3;
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
