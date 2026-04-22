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

        // 防御 / 受击属性从配置读取
        m_Armor = config?.Armor ?? 0;
        m_MagicResist = config?.MagicResist ?? 0;
        m_AtkRange = config?.AtkRange ?? 0;  // 受击检测范围复用此字段
        m_MoveSpeed = config?.MoveSpeed ?? 0; // 实际移动由玩家控制器负责

        // 召唤师不攻击
        m_AtkDamage = 0;
        m_AtkSpeed = 0;
        m_CritRate = 0;
        m_CritDamage = 1.0;
        m_SpellPower = 0;
        m_Shield = 0;
        m_CooldownReduce = 0;
        m_DamageTakenMultiplier = 1.0;

        DebugEx.LogModule("ChessAttribute",
            $"InitializeAsSummoner: HP:{m_CurrentHp}/{m_MaxHp}, Armor:{m_Armor}, MR:{m_MagicResist}, AtkRange:{m_AtkRange}");
    }

    /// <summary>
    /// 初始化属性
    /// </summary>
    /// <param name="owner">所属棋子实体</param>
    /// <param name="config">棋子配置数据</param>
    public void Initialize(ChessEntity owner, SummonChessConfig config)
    {
        m_Owner = owner;
        if (config == null)
        {
            DebugEx.ErrorModule("ChessAttribute", "Initialize: config is null");
            return;
        }

        // 初始化最大值
        m_MaxHp = config.MaxHp;
        m_MaxMp = config.MaxMp;

        // 初始化当前值
        m_CurrentHp = config.MaxHp;
        m_CurrentMp = config.InitialMp;

        // 初始化战斗属性
        m_AtkDamage = config.AtkDamage;
        m_AtkSpeed = config.AtkSpeed;
        m_AtkRange = config.AtkRange;
        m_Armor = config.Armor;
        m_MagicResist = config.MagicResist;
        m_MoveSpeed = config.MoveSpeed;
        m_CritRate = config.CritRate;
        m_CritDamage = config.CritDamage;
        m_SpellPower = config.SpellPower;
        m_Shield = config.Shield;
        m_CooldownReduce = config.CooldownReduce;
        m_DamageTakenMultiplier = 1.0;

        DebugEx.LogModule("ChessAttribute", $"Initialize: {config.Name} - HP:{m_CurrentHp}/{m_MaxHp} MP:{m_CurrentMp}/{m_MaxMp}");
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

        // 限制生命值在有效范围[0, MaxHp]内
        m_CurrentHp = Math.Clamp(m_CurrentHp + delta, 0, m_MaxHp);

        // 如果值真的发生变化，触发事件
        if (Math.Abs(m_CurrentHp - oldValue) > 0.001)
        {
            OnHpChanged?.Invoke(oldValue, m_CurrentHp);

            // 如果生命值降为0，输出日志
            if (m_CurrentHp <= 0 && oldValue > 0)
            {
                DebugEx.LogModule("ChessAttribute", $"棋子死亡 (HP: {oldValue} -> {m_CurrentHp})");

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
                DebugEx.LogModule("ChessAttribute", $"法力值满了 (MP: {m_CurrentMp}/{m_MaxMp})");
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
            DebugEx.WarningModule("ChessAttribute", $"SetMaxHp: invalid value {value}");
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
            DebugEx.WarningModule("ChessAttribute", $"SetMaxMp: invalid value {value}");
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

        // ⭐ 调试：打印护甲和减伤倍数
        DebugEx.LogModule("ChessAttribute", $"[伤害计算] 基础伤害={baseDamage:F1}, 护甲={m_Armor:F1}, 减伤倍数={damageReduction:F3}, 实际伤害={actualDamage:F1}");

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
    public void TakeDamage(double damage, bool isMagic = false, bool isTrueDamage = false, bool isCritical = false,
        DamageFloatingTextManager.DamageType damageType = DamageFloatingTextManager.DamageType.普通伤害)
    {
        if (damage <= 0)
        {
            return;
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

            if (actualDamage <= 0)
            {
                OnDamageTaken?.Invoke(0, isMagic);
                return;
            }
        }

        // 应用伤害
        ModifyHp(-actualDamage);

        // 显示伤害飘字
        // 如果指定了 damageType，使用指定类型；否则根据参数推断
        DamageFloatingTextManager.DamageType finalDamageType = damageType;
        if (damageType == DamageFloatingTextManager.DamageType.普通伤害 && (isMagic || isCritical || isTrueDamage))
        {
            // 自动推断类型
            if (isCritical)
            {
                finalDamageType = DamageFloatingTextManager.DamageType.暴击伤害;
            }
            else if (isMagic)
            {
                finalDamageType = DamageFloatingTextManager.DamageType.法术伤害;
            }
            else if (isTrueDamage)
            {
                finalDamageType = DamageFloatingTextManager.DamageType.真实伤害;
            }
            else
            {
                finalDamageType = DamageFloatingTextManager.DamageType.普通伤害;
            }
        }

        // 获取摄像机
        Camera playerCamera = CameraRegistry.PlayerCamera;
        Vector3 basePosition = transform.position;

        // 计算相对屏幕的偏移方向
        Vector3 screenRight = Vector3.right;      // 屏幕右方向
        Vector3 screenForward = Vector3.forward;  // 屏幕内侧方向
        if (playerCamera != null)
        {
            screenRight = playerCamera.transform.right;
            screenForward = playerCamera.transform.forward;
        }

        // 计算飘字位置：基础位置 + Y轴随机偏移 + 相对屏幕的左右偏移 + 相对屏幕的内外偏移
        float yOffset = 2f + UnityEngine.Random.Range(-0.8f, 0.8f);
        float screenRightOffset = UnityEngine.Random.Range(-1f, 1f);     // 相对屏幕左右
        float screenForwardOffset = UnityEngine.Random.Range(0f, 1f);    // 相对屏幕内侧

        Vector3 popupPosition = basePosition
            + Vector3.up * yOffset
            + screenRight * screenRightOffset
            + screenForward * screenForwardOffset;

        // 向摄像机方向偏移 0.1 单位，避免被目标对象遮挡
        if (playerCamera != null)
        {
            Vector3 cameraDir = (playerCamera.transform.position - popupPosition).normalized;
            popupPosition += cameraDir * 0.1f;
        }

        DamageFloatingTextManager.Instance.ShowDamageText(finalDamageType, (float)actualDamage, popupPosition);

        // 触发伤害事件
        OnDamageTaken?.Invoke(actualDamage, isMagic);

        DebugEx.LogModule("ChessAttribute", $"TakeDamage: 受到{(isTrueDamage ? "真实" : isMagic ? "魔法" : "物理")}伤害 {actualDamage:F1} " +
                 $"(原始:{damage:F1}) HP: {m_CurrentHp:F1}/{m_MaxHp:F1} Shield: {m_Shield:F1}");
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
    /// 受到伤害事件
    /// 参数：(伤害值, 是否为魔法伤害)
    /// </summary>
    public event Action<double, bool> OnDamageTaken;

    /// <summary>
    /// 护盾值变化事件
    /// 参数：(旧值, 新值)
    /// </summary>
    public event Action<double, double> OnShieldChanged;

    #endregion

    #region Unity生命周期

    private void OnDestroy()
    {
        // 清理事件订阅
        OnHpChanged = null;
        OnMpChanged = null;
        OnDamageTaken = null;
        OnShieldChanged = null;
    }

    #endregion

    #region 调试方法

    /// <summary>
    /// 打印当前属性信息（测试用）
    /// </summary>
    public void DebugPrintAttributes()
    {
        DebugEx.LogModule("ChessAttribute", "=== ChessAttribute 属性信息 ===");
        DebugEx.LogModule("ChessAttribute", $"生命值: {m_CurrentHp:F1}/{m_MaxHp:F1}");
        DebugEx.LogModule("ChessAttribute", $"法力值: {m_CurrentMp:F1}/{m_MaxMp:F1}");
        DebugEx.LogModule("ChessAttribute", $"攻击力: {m_AtkDamage:F1}");
        DebugEx.LogModule("ChessAttribute", $"攻击速度: {m_AtkSpeed:F2}");
        DebugEx.LogModule("ChessAttribute", $"攻击范围: {m_AtkRange:F1}");
        DebugEx.LogModule("ChessAttribute", $"护甲: {m_Armor:F1}");
        DebugEx.LogModule("ChessAttribute", $"魔抗: {m_MagicResist:F1}");
        DebugEx.LogModule("ChessAttribute", $"移动速度: {m_MoveSpeed:F1}");
        DebugEx.LogModule("ChessAttribute", "==============================");
    }

    #endregion
}
