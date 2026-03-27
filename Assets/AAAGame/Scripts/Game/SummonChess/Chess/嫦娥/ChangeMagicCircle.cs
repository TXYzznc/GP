using UnityEngine;

/// <summary>
/// 嫦娥法阵效果组件
/// 自管理生命周期和子弹发射
/// </summary>
public class ChangeMagicCircle : MonoBehaviour
{
    #region 私有字段

    /// <summary>技能配置</summary>
    private SummonChessSkillTable m_Config;

    /// <summary>施法者</summary>
    private ChessEntity m_Caster;

    /// <summary>法阵剩余时间</summary>
    private float m_RemainingTime;

    /// <summary>子弹发射计时器</summary>
    private float m_ProjectileTimer;

    /// <summary>子弹发射间隔</summary>
    private float m_ProjectileInterval;

    /// <summary>已发射子弹数量</summary>
    private int m_ProjectilesFired;

    /// <summary>单发伤害</summary>
    private double m_ProjectileDamage;

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化法阵
    /// </summary>
    /// <param name="config">技能配置</param>
    /// <param name="caster">施法者</param>
    /// <param name="targetPosition">目标位置</param>
    public void Initialize(SummonChessSkillTable config, ChessEntity caster, Vector3 targetPosition)
    {
        m_Config = config;
        m_Caster = caster;
        m_RemainingTime = (float)config.Duration;

        // 计算子弹发射间隔
        m_ProjectileInterval = config.HitCount > 0 ? (float)config.Duration / config.HitCount : 1f;

        // ⭐ 设置初始延迟，避免第一帧就发射子弹
        m_ProjectileTimer = -0.1f; // 延迟 0.1 秒后开始发射第一枚子弹

        // 计算单发伤害
        var selfAttr = caster.Attribute;
        double scalingStat = selfAttr.SpellPower > 0 ? selfAttr.SpellPower : selfAttr.AtkDamage;
        m_ProjectileDamage = scalingStat * config.DamageCoeff + config.BaseDamage;

        // 设置法阵位置
        transform.position = targetPosition;

        m_IsInitialized = true;

        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"法阵初始化完成: 位置={targetPosition}, 持续={config.Duration}s, "
                + $"子弹数={config.HitCount}, 间隔={m_ProjectileInterval:F2}s, 单发伤害={m_ProjectileDamage:F1}"
        );
    }

    #endregion

    #region Unity 生命周期

    private void Update()
    {
        if (!m_IsInitialized)
            return;

        float deltaTime = Time.deltaTime;

        // 更新剩余时间
        m_RemainingTime -= deltaTime;

        // 更新子弹发射计时器
        m_ProjectileTimer += deltaTime;

        // ⭐ 优化子弹发射逻辑：确保所有子弹都能发射
        bool shouldFire = false;

        if (m_ProjectilesFired < m_Config.HitCount)
        {
            // 正常间隔发射
            if (m_ProjectileTimer >= m_ProjectileInterval)
            {
                shouldFire = true;
            }
            // ⭐ 法阵即将结束时，强制发射剩余子弹
            else if (m_RemainingTime <= 0.1f && m_ProjectileTimer > 0f)
            {
                shouldFire = true;
            }
        }

        if (shouldFire)
        {
            FireProjectile();
            m_ProjectileTimer = 0f; // ⭐ 重置为0，而不是减去间隔
            m_ProjectilesFired++;
        }

        // 检查是否结束（所有子弹发射完毕或时间耗尽）
        if (m_RemainingTime <= 0f || m_ProjectilesFired >= m_Config.HitCount)
        {
            OnComplete();
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 发射子弹
    /// </summary>
    private void FireProjectile()
    {
        // ⭐ 检查施法者是否仍然有效
        if (m_Caster == null || m_Caster.CurrentState == ChessState.Dead)
        {
            DebugEx.WarningModule("ChangeMagicCircle", "施法者已死亡或无效，停止发射子弹");
            OnComplete();
            return;
        }
        // 子弹起始位置（法阵上方）
        Vector3 startPosition = transform.position + Vector3.up * 5f;

        // 子弹目标位置（法阵中心）
        Vector3 targetPosition = transform.position;

        // 计算本次伤害（可能暴击）
        double damage = m_ProjectileDamage;
        bool isCritical = Random.value < m_Caster.Attribute.CritRate;
        if (isCritical)
        {
            damage *= m_Caster.Attribute.CritDamage;
        }

        // 构建命中检测上下文
        HitContext context = new HitContext
        {
            Attacker = m_Caster,
            AttackerPosition = startPosition,
            AttackerForward = Vector3.down,
            AttackerCamp = m_Caster.Camp,
            LockedTarget = null,
            TargetPosition = targetPosition,
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = m_Config.DamageType == 2,
            IsTrueDamage = m_Config.DamageType == 3,
            AOERadius = (float)m_Config.AreaRadius,
            ProjectilePrefabId = m_Config.ProjectilePrefabId,
            ProjectileSpeed = (float)m_Config.ProjectileSpeed,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(m_Caster.Camp),
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = OnProjectileHit,
        };

        // 使用投射物检测器发射子弹
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Projectile);
        detector.Execute(context);

        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"发射子弹 {m_ProjectilesFired + 1}/{m_Config.HitCount}: "
                + $"伤害={damage:F1}{(isCritical ? " (暴击)" : "")}"
        );
    }

    /// <summary>
    /// 子弹命中回调
    /// </summary>
    /// <param name="target">命中目标</param>
    /// <param name="damage">伤害值</param>
    /// <param name="isCritical">是否暴击</param>
    private void OnProjectileHit(ChessEntity target, double damage, bool isCritical)
    {
        if (target == null)
            return;

        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"法阵子弹命中: {target.Config?.Name}, 伤害={damage:F1}{(isCritical ? " (暴击)" : "")}"
        );
    }

    /// <summary>
    /// 法阵效果结束
    /// </summary>
    private void OnComplete()
    {
        DebugEx.LogModule("ChangeMagicCircle", $"法阵效果结束: 共发射{m_ProjectilesFired}枚子弹");

        // 销毁自身
        Destroy(gameObject);
    }

    #endregion
}
