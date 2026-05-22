using System;
using UnityEngine;

/// <summary>
/// 棋子属性组件
/// 管理棋子的数值、伤害计算
/// </summary>
public class ChessAttribute : MonoBehaviour
{
    #region 引用

    /// <summary>所属棋子实体</summary>
    private ChessEntity m_Owner;

    #endregion

    #region 数值

    /// <summary>难度系数（敌人真实基础属性倍率，所有加成的基础）</summary>
    private float m_DifficultyCoef = 1f;

    private double m_CurrentHp;
    private double m_CurrentMp;
    private double m_MaxHp;
    private double m_MaxMp;
    private double m_AtkDamage;
    private double m_AtkSpeed;
    private double m_AtkRange;
    private double m_Armor;
    private double m_MagicResist;
    private double m_MoveSpeed;
    private double m_CritRate;
    private double m_CritDamage;
    private double m_SpellPower;
    private double m_Shield;
    private double m_CooldownReduce;
    private double m_DamageTakenMultiplier = 1.0;
    private double m_HpFloor = 0;


    #endregion

    #region 属性访问

    /// <summary>当前生命值</summary>
    public double CurrentHp => m_CurrentHp;

    /// <summary>当前法力值</summary>
    public double CurrentMp => m_CurrentMp;

    /// <summary>最大生命值</summary>
    public double MaxHp => m_MaxHp;

    /// <summary>最大法力值</summary>
    public double MaxMp => m_MaxMp;

    /// <summary>攻击力</summary>
    public double AtkDamage => m_AtkDamage;

    /// <summary>攻击速度</summary>
    public double AtkSpeed => m_AtkSpeed;

    /// <summary>攻击范围</summary>
    public double AtkRange => m_AtkRange;

    /// <summary>护甲</summary>
    public double Armor => m_Armor;

    /// <summary>魔抗</summary>
    public double MagicResist => m_MagicResist;

    /// <summary>移动速度</summary>
    public double MoveSpeed => m_MoveSpeed;

    /// <summary>暴击率（0-1）</summary>
    public double CritRate => m_CritRate;

    /// <summary>暴击伤害倍率</summary>
    public double CritDamage => m_CritDamage;

    /// <summary>法术强度</summary>
    public double SpellPower => m_SpellPower;

    /// <summary>当前护盾值</summary>
    public double Shield => m_Shield;

    /// <summary>冷却缩减（0-1）</summary>
    public double CooldownReduce => m_CooldownReduce;

    public double DamageTakenMultiplier => m_DamageTakenMultiplier;

    /// <summary>是否死亡</summary>
    public bool IsDead => m_CurrentHp <= 0;

    /// <summary>HP下限（锁血用，设为>0则不会死亡）</summary>
    public double HpFloor { get => m_HpFloor; set => m_HpFloor = Math.Max(0, value); }

    /// <summary>难度系数（所有基础属性的倍率基础）</summary>
    public float DifficultyCoef => m_DifficultyCoef;

    #endregion

    #region 初始化

    /// <summary>
    /// 作为召唤师战斗实体初始化。
    /// 防御 / 移动属性从 SummonChessTable 配置行读取；
    /// HP 使用 SummonerTable.BaseHP（由调用方传入），不读 config.MaxHp；
    /// MP 固定为 0（召唤师使用 SummonerRuntimeDataManager 的灵力，不走棋子MP体系）。
    /// </summary>
    /// <param name="owner">所属 ChessEntity</param>
    /// <param name="config">SummonChessTable 中的召唤师配置行</param>
    /// <param name="maxHp">来自 SummonerRuntimeDataManager.MaxHP</param>
    public void InitializeAsSummoner(ChessEntity owner, SummonChessConfig config, double maxHp)
    {
        m_Owner = owner;

        // HP 来自 SummonerTable.BaseHP，不用 config.MaxHp
        m_MaxHp = maxHp;
        m_CurrentHp = maxHp;

        // 召唤师无法力（灵力由 SummonerRuntimeDataManager 独立管理）
        m_MaxMp = 0;
        m_CurrentMp = 0;

        // 防御 / 受击属性从配置读取（召唤师无等级，取数组第一个元素）
        m_Armor = config != null ? config.GetArmor(1) : 0;
        m_MagicResist = config != null ? config.GetMagicResist(1) : 0;
        m_AtkRange = config != null ? config.GetAtkRange(1) : 0;  // 受击检测范围复用此字段
        m_MoveSpeed = config?.MoveSpeed ?? 0; // 实际移动由玩家控制器负责

        // 召唤师有战斗属性，但不分等级（都取数组第一个元素）
        m_AtkDamage = config != null ? config.GetAtkDamage(1) : 0;
        m_AtkSpeed = config != null ? config.GetAtkSpeed(1) : 0;
        m_CritRate = config != null ? config.GetCritRate(1) : 0;
        m_CritDamage = config != null ? config.GetCritDamage(1) : 1.0;
        m_SpellPower = config != null ? config.GetSpellPower(1) : 0;
        m_Shield = 0;
        m_CooldownReduce = 0;
        m_DamageTakenMultiplier = 1.0;

        DebugEx.Log("ChessAttribute",
            $"InitializeAsSummoner: HP:{m_CurrentHp}/{m_MaxHp}, Armor:{m_Armor}, MR:{m_MagicResist}, AtkRange:{m_AtkRange}");
    }

    /// <summary>
    /// 初始化属性
    /// </summary>
    /// <param name="owner">所属棋子实体</param>
    /// <param name="config">棋子配置数据</param>
    /// <param name="rank">棋子等级（1-3）</param>
    /// <summary>
    /// 初始化属性（包含难度系数应用）
    ///
    /// 【设计说明 - 难度系数时间窗口】
    /// 1. 敌人棋子生成时：
    ///    a. Initialize() 被调用，使用 difficultyCoef 参数（默认 1.0）
    ///    b. 属性初始化为配置值 × difficultyCoef
    ///    c. 【重要】此时若 difficultyCoef=1.0，敌人属性尚未应用真实的难度系数
    ///    d. EnemySpawnManager.SpawnEnemyAsync() 立即调用 ApplyDifficultyCoef() 应用真实系数
    ///
    /// 2. 为什么不在 SpawnChessAsync 就传递难度系数？
    ///    - 避免系统耦合：SummonChessManager 不应该知道敌人难度的概念
    ///    - 玩家棋子保护：玩家棋子永远不需要难度系数
    ///    - 职责清晰：生成 vs. 敌人难度应用是不同的系统
    ///    - 扩展性：将来添加其他系数时不影响生成系统
    ///
    /// 3. 【时间窗口风险】
    ///    Initialize() 后、ApplyDifficultyCoef() 前：
    ///    - 如果此时有代码获取属性，会得到系数 1.0 的值
    ///    - 实际上没有这个风险，因为 ApplyDifficultyCoef() 在 Initialize() 后立即调用
    ///    - 而且都在 EnemySpawnManager.SpawnEnemyAsync() 的同一个函数中
    /// </summary>
    public void Initialize(ChessEntity owner, SummonChessConfig config, int rank, float difficultyCoef = 1f)
    {
        m_Owner = owner;
        if (config == null)
        {
            DebugEx.Error("ChessAttribute", "Initialize: config is null");
            return;
        }

        // 保存难度系数（敌人真实基础属性，会应用到 6 大属性：HP、MP、ATK、Armor、MagicResist、SpellPower）
        m_DifficultyCoef = Mathf.Max(difficultyCoef, 0.1f);

        // 初始化最大值（使用等级对应的数据），应用难度系数到 MaxHp 和 MaxMp
        m_MaxHp = config.GetMaxHp(rank) * m_DifficultyCoef;
        m_MaxMp = config.GetMaxMp(rank) * m_DifficultyCoef;

        // 初始化当前值
        m_CurrentHp = m_MaxHp;
        m_CurrentMp = config.GetInitialMp(rank) * m_DifficultyCoef;

        // 初始化战斗属性（应用难度系数到 6 大基础属性）
        m_AtkDamage = config.GetAtkDamage(rank) * m_DifficultyCoef;
        m_AtkSpeed = config.GetAtkSpeed(rank);
        m_AtkRange = config.GetAtkRange(rank);
        m_Armor = config.GetArmor(rank) * m_DifficultyCoef;
        m_MagicResist = config.GetMagicResist(rank) * m_DifficultyCoef;
        m_MoveSpeed = config.MoveSpeed;
        m_CritRate = config.GetCritRate(rank);
        m_CritDamage = config.GetCritDamage(rank);
        m_SpellPower = config.GetSpellPower(rank) * m_DifficultyCoef;
        m_Shield = config.Shield;
        m_CooldownReduce = config.CooldownReduce;
        m_DamageTakenMultiplier = 1.0;

        DebugEx.Log("ChessAttribute",
            $"Initialize: {config.Name} (难度系数:{m_DifficultyCoef:F2}) - HP:{m_CurrentHp:F0}/{m_MaxHp:F0} MP:{m_CurrentMp:F0}/{m_MaxMp:F0} ATK:{m_AtkDamage:F0} ARM:{m_Armor:F0}");
    }

    /// <summary>
    /// 从属单位属性初始化（属性由主人继承）
    /// 仅保留移动速度和MoveSpeed，其他属性继承自主人
    ///
    /// 【继承的是"真实基础属性"】
    /// masterAttribute 中的属性值已经应用了难度系数（如果是敌人从属单位）
    /// 所以从属单位继承到的是：配置值 × 难度系数 × inheritRatio
    /// 这确保从属单位的强度与主人匹配，包括难度系数的影响
    /// </summary>
    /// <param name="owner">所属棋子实体</param>
    /// <param name="config">从属单位配置（PopCost=0）</param>
    /// <param name="masterAttribute">主人的属性组件（已应用难度系数）</param>
    /// <param name="inheritRatio">属性继承比例（如0.8表示继承主人80%的属性）</param>
    public void InitializeAsSubordinate(ChessEntity owner, SummonChessConfig config, ChessAttribute masterAttribute, double inheritRatio)
    {
        m_Owner = owner;
        if (masterAttribute == null)
        {
            DebugEx.Error("ChessAttribute", "InitializeAsSubordinate: masterAttribute is null");
            return;
        }

        inheritRatio = Math.Clamp(inheritRatio, 0.1, 1.0);

        // 属性继承自主人
        m_MaxHp = masterAttribute.MaxHp * inheritRatio;
        m_CurrentHp = m_MaxHp;
        m_MaxMp = masterAttribute.MaxMp * inheritRatio;
        m_CurrentMp = m_MaxMp * 0.5; // 初始法力为最大值的50%

        m_AtkDamage = masterAttribute.AtkDamage * inheritRatio;
        m_AtkSpeed = masterAttribute.AtkSpeed;
        m_AtkRange = masterAttribute.AtkRange;
        m_Armor = masterAttribute.Armor * inheritRatio;
        m_MagicResist = masterAttribute.MagicResist * inheritRatio;
        m_CritRate = masterAttribute.CritRate;
        m_CritDamage = masterAttribute.CritDamage;
        m_SpellPower = masterAttribute.SpellPower * inheritRatio;
        m_Shield = 0;
        m_CooldownReduce = 0;
        m_DamageTakenMultiplier = 1.0;

        // 移动速度从配置读取（从属单位有独立的MoveSpeed）
        m_MoveSpeed = config?.MoveSpeed ?? 4;

        DebugEx.Log("ChessAttribute", $"InitializeAsSubordinate: {config?.Name} - HP:{m_CurrentHp}/{m_MaxHp} (继承比例:{inheritRatio})");
    }

    /// <summary>
    /// 应用难度系数到基础属性（用于敌人在初始化后应用动态难度）
    /// 根据当前难度系数重新计算所有基础属性
    ///
    /// 【调用时机】
    /// - 由 EnemySpawnManager.SpawnEnemyAsync() 在敌人棋子初始化后立即调用
    /// - 时机：Initialize() 完成 → 设置棋子等级 → ApplyDifficultyCoef()
    ///
    /// 【应用范围 - 应用到 6 大基础属性】
    /// ✅ MaxHp、MaxMp、AtkDamage、Armor、MagicResist、SpellPower
    /// ❌ 不应用：Shield、AtkSpeed、AtkRange、MoveSpeed、CritRate、CritDamage、CooldownReduce
    ///
    /// 【继承关系的正确性】
    /// - 从属单位通过 InitializeAsSubordinate() 继承主人属性
    /// - 继承的是已应用系数后的"真实基础属性"，确保从属单位的属性匹配主人
    /// </summary>
    public void ApplyDifficultyCoef(float newDifficultyCoef, SummonChessConfig config, int rank)
    {
        if (config == null)
        {
            DebugEx.Error("ChessAttribute", "ApplyDifficultyCoef: config is null");
            return;
        }

        // 更新难度系数
        float oldCoef = m_DifficultyCoef;
        m_DifficultyCoef = Mathf.Max(newDifficultyCoef, 0.1f);

        // 计算属性倍率（旧系数 → 新系数）
        float coefRatio = m_DifficultyCoef / Mathf.Max(oldCoef, 0.1f);

        // 重新计算基础属性（应用难度系数到 6 大基础属性：MaxHp、MaxMp、AtkDamage、Armor、MagicResist、SpellPower）
        double baseMaxHp = config.GetMaxHp(rank);
        double baseMaxMp = config.GetMaxMp(rank);
        double baseAtkDamage = config.GetAtkDamage(rank);
        double baseArmor = config.GetArmor(rank);
        double baseMagicResist = config.GetMagicResist(rank);
        double baseSpellPower = config.GetSpellPower(rank);

        // 应用新的难度系数（到 6 大属性）
        m_MaxHp = baseMaxHp * m_DifficultyCoef;
        m_MaxMp = baseMaxMp * m_DifficultyCoef;
        m_AtkDamage = baseAtkDamage * m_DifficultyCoef;
        m_Armor = baseArmor * m_DifficultyCoef;
        m_MagicResist = baseMagicResist * m_DifficultyCoef;
        m_SpellPower = baseSpellPower * m_DifficultyCoef;

        // 调整当前血量（按倍率缩放）
        m_CurrentHp = Math.Max(m_CurrentHp * coefRatio, 0);
        if (m_CurrentHp > m_MaxHp)
            m_CurrentHp = m_MaxHp;

        // 调整当前法力（按倍率缩放）
        m_CurrentMp = Math.Max(m_CurrentMp * coefRatio, 0);
        if (m_CurrentMp > m_MaxMp)
            m_CurrentMp = m_MaxMp;

        DebugEx.Log("ChessAttribute",
            $"ApplyDifficultyCoef: {m_Owner?.Config.Name} ({oldCoef:F2} → {m_DifficultyCoef:F2}) - HP:{m_CurrentHp:F0}/{m_MaxHp:F0} MP:{m_CurrentMp:F0}/{m_MaxMp:F0} ATK:{m_AtkDamage:F0} ARM:{m_Armor:F0}");
    }

    #endregion

    #region 数值修改

    /// <summary>
    /// 修改生命值
    /// </summary>
    /// <param name="delta">变化量（正数为增加，负数为减少）</param>
    public void ModifyHp(double delta)
    {
        double oldValue = m_CurrentHp;

        m_CurrentHp = Math.Clamp(m_CurrentHp + delta, m_HpFloor, m_MaxHp);

        // 如果值真的发生变化，触发事件
        if (Math.Abs(m_CurrentHp - oldValue) > 0.001)
        {
            OnHpChanged?.Invoke(oldValue, m_CurrentHp);

            // 如果生命值降为0，输出日志
            if (m_CurrentHp <= 0 && oldValue > 0)
            {
                DebugEx.Log("ChessAttribute", $"棋子死亡 (HP: {oldValue} -> {m_CurrentHp})");

                // ⭐ 从棋子管理器注销
                if (CombatEntityTracker.Instance != null && m_Owner != null)
                {
                    CombatEntityTracker.Instance.UnregisterChess(m_Owner);
                }
            }
        }
    }

    /// <summary>
    /// 修改法力值
    /// </summary>
    /// <param name="delta">变化量（正数为增加，负数为减少）</param>
    public void ModifyMp(double delta)
    {
        double oldValue = m_CurrentMp;

        // 限制法力值在有效范围[0, MaxMp]内
        m_CurrentMp = Math.Clamp(m_CurrentMp + delta, 0, m_MaxMp);

        // 如果值真的发生变化，触发事件
        if (Math.Abs(m_CurrentMp - oldValue) > 0.001)
        {
            OnMpChanged?.Invoke(oldValue, m_CurrentMp);

            // 如果法力值达到最大值，可以释放技能
            if (m_CurrentMp >= m_MaxMp && oldValue < m_MaxMp)
            {
                // 法力充能完毕事件由 OnMpChanged 事件处理，此处不需打印日志
            }
        }
    }

    /// <summary>
    /// 设置生命值
    /// </summary>
    /// <param name="value">新的生命值</param>
    public void SetHp(double value)
    {
        ModifyHp(value - m_CurrentHp);
    }

    /// <summary>
    /// 设置法力值
    /// </summary>
    /// <param name="value">新的法力值</param>
    public void SetMp(double value)
    {
        ModifyMp(value - m_CurrentMp);
    }

    /// <summary>
    /// 设置最大生命值（不会改变当前值）
    /// </summary>
    /// <param name="value">新的最大生命值</param>
    public void SetMaxHp(double value)
    {
        if (value <= 0)
        {
            DebugEx.Warning("ChessAttribute", $"SetMaxHp: invalid value {value}");
            return;
        }

        m_MaxHp = value;

        // 如果当前生命值超过新的最大值，限制到最大值
        if (m_CurrentHp > m_MaxHp)
        {
            SetHp(m_MaxHp);
        }
    }

    /// <summary>
    /// 设置最大法力值（不会改变当前值）
    /// </summary>
    /// <param name="value">新的最大法力值</param>
    public void SetMaxMp(double value)
    {
        if (value < 0)
        {
            DebugEx.Warning("ChessAttribute", $"SetMaxMp: invalid value {value}");
            return;
        }

        m_MaxMp = value;

        // 如果当前法力值超过新的最大值，限制到最大值
        if (m_CurrentMp > m_MaxMp)
        {
            SetMp(m_MaxMp);
        }
    }

    /// <summary>
    /// 修改护盾值（护盾无上限，只限制最小值为0）
    /// </summary>
    public void ModifyShield(double delta)
    {
        double oldValue = m_Shield;
        m_Shield = Math.Max(m_Shield + delta, 0);

        if (Math.Abs(m_Shield - oldValue) > 0.001)
        {
            OnShieldChanged?.Invoke(oldValue, m_Shield);
        }
    }

    /// <summary>
    /// 设置护盾值
    /// </summary>
    public void SetShield(double value)
    {
        ModifyShield(value - m_Shield);
    }

    /// <summary>
    /// 清空护盾（技能效果）
    /// </summary>
    public void ClearShield()
    {
        SetShield(0);
    }

    /// <summary>
    /// 修改法术强度
    /// </summary>
    public void ModifySpellPower(double delta)
    {
        m_SpellPower = Math.Max(0, m_SpellPower + delta);
    }

    /// <summary>
    /// 修改暴击率
    /// </summary>
    public void ModifyCritRate(double delta)
    {
        m_CritRate = Math.Clamp(m_CritRate + delta, 0, 1);
    }

    /// <summary>
    /// 修改攻击力
    /// </summary>
    public void ModifyAtkDamage(double delta)
    {
        m_AtkDamage = Math.Max(0, m_AtkDamage + delta);
    }

    /// <summary>
    /// 修改攻击速度
    /// </summary>
    public void ModifyAtkSpeed(double delta)
    {
        m_AtkSpeed = Math.Max(0.01, m_AtkSpeed + delta);
    }

    /// <summary>
    /// 修改攻击范围
    /// </summary>
    public void ModifyAtkRange(double delta)
    {
        m_AtkRange = Math.Max(0, m_AtkRange + delta);
    }

    /// <summary>
    /// 修改护甲
    /// </summary>
    public void ModifyArmor(double delta)
    {
        m_Armor += delta;
    }

    /// <summary>
    /// 修改魔抗
    /// </summary>
    public void ModifyMagicResist(double delta)
    {
        m_MagicResist += delta;
    }

    /// <summary>
    /// 修改移动速度
    /// </summary>
    public void ModifyMoveSpeed(double delta)
    {
        m_MoveSpeed = Math.Max(0, m_MoveSpeed + delta);
    }

    /// <summary>
    /// 修改暴击伤害倍率
    /// </summary>
    public void ModifyCritDamage(double delta)
    {
        m_CritDamage = Math.Max(1.0, m_CritDamage + delta);
    }

    /// <summary>
    /// 修改冷却缩减
    /// </summary>
    public void ModifyCooldownReduce(double delta)
    {
        m_CooldownReduce = Math.Clamp(m_CooldownReduce + delta, 0, 1);
    }

    public void ModifyDamageTakenMultiplier(double delta)
    {
        m_DamageTakenMultiplier = Math.Max(0, m_DamageTakenMultiplier + delta);
    }

    #endregion

    #region 伤害计算

    /// <summary>
    /// 计算物理伤害（考虑护甲减伤）
    /// </summary>
    /// <param name="baseDamage">基础伤害</param>
    /// <returns>实际伤害</returns>
    public double CalculatePhysicalDamage(double baseDamage)
    {
        // 简化的护甲减伤公式：实际伤害 = 基础伤害 * (100 / (100 + 护甲))
        double damageReduction = 100.0 / (100.0 + m_Armor);
        double actualDamage = baseDamage * damageReduction;

        return Math.Max(0, actualDamage);
    }

    /// <summary>
    /// 计算魔法伤害（考虑魔抗减伤）
    /// </summary>
    /// <param name="baseDamage">基础伤害</param>
    /// <returns>实际伤害</returns>
    public double CalculateMagicDamage(double baseDamage)
    {
        // 简化的魔抗减伤公式：实际伤害 = 基础伤害 * (100 / (100 + 魔抗))
        double damageReduction = 100.0 / (100.0 + m_MagicResist);
        double actualDamage = baseDamage * damageReduction;
        return Math.Max(0, actualDamage);
    }

    /// <summary>
    /// 受到伤害（支持护盾吸收）
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="isMagic">是否为魔法伤害</param>
    /// <param name="isTrueDamage">是否为真实伤害（忽略护甲/魔抗）</param>
    /// <param name="isCritical">是否暴击</param>
    /// <param name="damageType">伤害类型（用于飘字显示）</param>
    /// <param name="attacker">攻击来源属性组件，为 null 表示无来源（如 DOT）</param>
    /// <returns>实际造成的伤害值（护盾/防御后）</returns>
    public double TakeDamage(double damage, bool isMagic = false, bool isTrueDamage = false, bool isCritical = false,
        DamageFloatingTextManager.DamageType damageType = DamageFloatingTextManager.DamageType.普通伤害,
        ChessAttribute attacker = null)
    {
        if (damage <= 0)
        {
            return 0;
        }

        // 计算实际伤害
        double actualDamage;
        if (isTrueDamage)
        {
            actualDamage = damage; // 真实伤害忽略防御
        }
        else
        {
            actualDamage = isMagic ? CalculateMagicDamage(damage) : CalculatePhysicalDamage(damage);
        }

        if (m_DamageTakenMultiplier != 1.0)
        {
            actualDamage *= m_DamageTakenMultiplier;
        }

        // 护盾吸收
        if (m_Shield > 0)
        {
            double shieldAbsorb = Math.Min(m_Shield, actualDamage);
            ModifyShield(-shieldAbsorb);
            actualDamage -= shieldAbsorb;

            // 发送护盾受击事件
            OnShieldHit?.Invoke(shieldAbsorb);

            if (actualDamage <= 0)
            {
                OnDamageTaken?.Invoke(0, isMagic);
                OnDamageTakenWithSource?.Invoke(0, isMagic, attacker);
                return 0;
            }
        }

        // 应用伤害
        ModifyHp(-actualDamage);

        // 显示伤害飘字
        DamageFloatingTextManager.DamageType finalDamageType = damageType;
        if (damageType == DamageFloatingTextManager.DamageType.普通伤害 && (isMagic || isCritical || isTrueDamage))
        {
            if (isCritical)
                finalDamageType = DamageFloatingTextManager.DamageType.暴击伤害;
            else if (isMagic)
                finalDamageType = DamageFloatingTextManager.DamageType.法术伤害;
            else if (isTrueDamage)
                finalDamageType = DamageFloatingTextManager.DamageType.真实伤害;
        }

        Camera playerCamera = CameraRegistry.PlayerCamera;
        Vector3 basePosition = transform.position;
        Vector3 screenRight = Vector3.right;
        Vector3 screenForward = Vector3.forward;
        if (playerCamera != null)
        {
            screenRight = playerCamera.transform.right;
            screenForward = playerCamera.transform.forward;
        }

        float yOffset = 2f + UnityEngine.Random.Range(-0.8f, 0.8f);
        float screenRightOffset = UnityEngine.Random.Range(-1f, 1f);
        float screenForwardOffset = UnityEngine.Random.Range(0f, 1f);
        Vector3 popupPosition = basePosition
            + Vector3.up * yOffset
            + screenRight * screenRightOffset
            + screenForward * screenForwardOffset;
        if (playerCamera != null)
        {
            Vector3 cameraDir = (playerCamera.transform.position - popupPosition).normalized;
            popupPosition += cameraDir * 0.1f;
        }

        DamageFloatingTextManager.Instance.ShowDamageText(finalDamageType, (float)actualDamage, popupPosition);

        // 触发受伤事件
        OnDamageTaken?.Invoke(actualDamage, isMagic);
        OnDamageTakenWithSource?.Invoke(actualDamage, isMagic, attacker);

        // 通知攻击方已造成伤害
        if (attacker != null)
            attacker.OnDamageDealt?.Invoke(actualDamage, this);

        return actualDamage;
    }

    #endregion

    #region 事件

    /// <summary>
    /// 生命值变化事件
    /// 参数：(旧值, 新值)
    /// </summary>
    public event Action<double, double> OnHpChanged;

    /// <summary>
    /// 法力值变化事件
    /// 参数：(旧值, 新值)
    /// </summary>
    public event Action<double, double> OnMpChanged;

    /// <summary>
    /// 受到伤害事件（无来源）
    /// 参数：(伤害值, 是否为魔法伤害)
    /// </summary>
    public event Action<double, bool> OnDamageTaken;

    /// <summary>
    /// 受到伤害事件（携带攻击来源，attacker 可为 null 表示 DOT）
    /// 参数：(伤害值, 是否为魔法伤害, 攻击来源 ChessAttribute)
    /// </summary>
    public event Action<double, bool, ChessAttribute> OnDamageTakenWithSource;

    /// <summary>
    /// 造成伤害事件（在被攻击方身上触发后，由 TakeDamage 回调给攻击方）
    /// 参数：(实际造成的伤害值, 被攻击方 ChessAttribute)
    /// </summary>
    public event Action<double, ChessAttribute> OnDamageDealt;

    /// <summary>
    /// 护盾值变化事件
    /// 参数：(旧值, 新值)
    /// </summary>
    public event Action<double, double> OnShieldChanged;

    /// <summary>
    /// 护盾受击事件
    /// 参数：吸收的伤害值
    /// </summary>
    public event Action<double> OnShieldHit;

    #endregion

    #region Unity生命周期

    private void OnDestroy()
    {
        // 清理事件订阅
        OnHpChanged = null;
        OnMpChanged = null;
        OnDamageTaken = null;
        OnDamageTakenWithSource = null;
        OnDamageDealt = null;
        OnShieldChanged = null;
        OnShieldHit = null;
    }

    #endregion

    #region 调试方法

    /// <summary>
    /// 打印当前属性信息（测试用）
    /// </summary>
    public void DebugPrintAttributes()
    {
        DebugEx.Log("ChessAttribute", "=== ChessAttribute 属性信息 ===");
        DebugEx.Log("ChessAttribute", $"生命值: {m_CurrentHp:F1}/{m_MaxHp:F1}");
        DebugEx.Log("ChessAttribute", $"法力值: {m_CurrentMp:F1}/{m_MaxMp:F1}");
        DebugEx.Log("ChessAttribute", $"攻击力: {m_AtkDamage:F1}");
        DebugEx.Log("ChessAttribute", $"攻击速度: {m_AtkSpeed:F2}");
        DebugEx.Log("ChessAttribute", $"攻击范围: {m_AtkRange:F1}");
        DebugEx.Log("ChessAttribute", $"护甲: {m_Armor:F1}");
        DebugEx.Log("ChessAttribute", $"魔抗: {m_MagicResist:F1}");
        DebugEx.Log("ChessAttribute", $"移动速度: {m_MoveSpeed:F1}");
        DebugEx.Log("ChessAttribute", "==============================");
    }

    #endregion
}
