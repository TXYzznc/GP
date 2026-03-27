using UnityEngine;

/// <summary>
/// 嫦娥大招：月华天倾 (ID=24)
/// 召唤月华之力，持续5秒对范围内敌人造成多段魔法伤害
/// 引导期间嫦娥处于 Channeling 状态
/// </summary>
public class ChangeUltimate : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 4; // 大招
    #endregion

    #region 私有字段

    /// <summary>是否正在引导</summary>
    private bool m_IsChanneling;

    /// <summary>引导剩余时间</summary>
    private float m_ChannelRemain;

    /// <summary>下次命中计时器</summary>
    private float m_HitTimer;

    /// <summary>命中间隔（引导时间 / 命中次数）</summary>
    private float m_HitInterval;

    /// <summary>单次命中伤害</summary>
    private double m_HitDamage;

    /// <summary>施法者引用</summary>
    private ChessEntity m_Caster;

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        m_IsChanneling = false;
        DebugEx.LogModule("ChangeUltimate", "月华天倾初始化完成");
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);

        // 引导中，持续命中
        if (m_IsChanneling)
        {
            m_ChannelRemain -= dt;
            m_HitTimer += dt;

            // 达到间隔，执行一次命中
            if (m_HitTimer >= m_HitInterval)
            {
                m_HitTimer -= m_HitInterval;
                PerformHit();
            }

            // 引导结束
            if (m_ChannelRemain <= 0)
            {
                EndChanneling();
            }
        }
    }

    public override bool CanCast()
    {
        if (m_IsChanneling)
            return false; // 引导中不可重复释放
        return base.CanCast();
    }

    public override bool TryCast()
    {
        if (!base.TryCast())
            return false;

        DebugEx.LogModule(
            "ChangeUltimate",
            $"月华天倾释放! 消耗MP={m_Config.MpCost}, 冷却={m_Config.Cooldown}秒"
        );

        return true;
    }

    /// <summary>
    /// 执行技能完整流程
    /// </summary>
    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.ErrorModule("ChangeUltimate", "ExecuteSkill: caster 为 null");
            return;
        }

        // 获取目标位置
        Vector3 targetPosition = GetTargetPosition(caster);

        // 播放技能释放特效（施法者位置）
        PlaySkillEffect(caster);

        // 在目标位置创建法阵
        CreateMagicCircle(targetPosition, caster);

        DebugEx.LogModule(
            "ChangeUltimate",
            $"月华天倾执行完成: 目标位置={targetPosition}, 范围={m_Config.AreaRadius}"
        );
    }

    /// <summary>
    /// 获取目标位置
    /// </summary>
    private Vector3 GetTargetPosition(ChessEntity caster)
    {
        // 从 AI 系统获取当前攻击目标
        var aiController = caster.GetComponent<ChessCombatController>();
        if (aiController != null)
        {
            // 通过 AI 基类获取当前目标
            var aiBase = caster.GetComponent<ChessAIBase>();
            if (
                aiBase != null
                && aiBase
                    .GetType()
                    .GetField(
                        "m_CurrentTarget",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    ) != null
            )
            {
                var currentTarget =
                    aiBase
                        .GetType()
                        .GetField(
                            "m_CurrentTarget",
                            System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Instance
                        )
                        .GetValue(aiBase) as ChessEntity;

                if (currentTarget != null)
                {
                    return currentTarget.transform.position;
                }
            }
        }

        // 否则使用施法者前方的位置
        return caster.transform.position + caster.transform.forward * (float)m_Config.CastRange;
    }

    /// <summary>
    /// 创建法阵
    /// </summary>
    private void CreateMagicCircle(Vector3 position, ChessEntity caster)
    {
        // 直接使用同步方式创建法阵
        CreateMagicCircleSync(position, caster);
    }

    /// <summary>
    /// 同步创建法阵
    /// </summary>
    private void CreateMagicCircleSync(Vector3 position, ChessEntity caster)
    {
        // 使用 CombatVFXManager 的公共方法播放特效
        GameObject circleInstance = null;

        // 创建一个空的 GameObject 作为法阵载体
        circleInstance = new GameObject($"MagicCircle_{m_Config.Id}");
        circleInstance.transform.position = position;

        // 播放法阵特效
        if (m_Config.EffectId > 0)
        {
            CombatVFXManager.PlayEffect(m_Config.EffectId, position);
        }

        // 添加法阵组件并初始化
        var magicCircle = circleInstance.AddComponent<ChangeMagicCircle>();
        magicCircle.Initialize(m_Config, caster, position);

        DebugEx.LogModule("ChangeUltimate", $"法阵创建成功: 位置={position}");
    }

    #endregion

    #region 引导逻辑

    /// <summary>
    /// 开始引导
    /// </summary>
    private void StartChanneling()
    {
        m_IsChanneling = true;
        m_ChannelRemain = (float)m_Config.Duration;
        m_HitTimer = 0f;

        // 计算命中间隔和单次伤害
        m_HitInterval = m_Config.HitCount > 0 ? (float)m_Config.Duration / m_Config.HitCount : 1f;

        var selfAttr = m_Ctx.Attribute;
        double scalingStat = selfAttr.SpellPower > 0 ? selfAttr.SpellPower : selfAttr.AtkDamage;
        m_HitDamage = scalingStat * m_Config.DamageCoeff + m_Config.BaseDamage;

        // 切换到引导状态
        if (m_Ctx?.Entity != null)
        {
            m_Ctx.Entity.ChangeState(ChessState.Channeling);
        }

        DebugEx.LogModule(
            "ChangeUltimate",
            $"月华天倾开始引导! 持续{m_Config.Duration}s, {m_Config.HitCount}次命中, "
                + $"间隔{m_HitInterval:F2}s, 单次伤害={m_HitDamage:F1}"
        );
    }

    /// <summary>
    /// 执行一次命中
    /// </summary>
    private void PerformHit()
    {
        if (m_Caster == null)
        {
            DebugEx.WarningModule("ChangeUltimate", "施法者为空，跳过本次命中");
            return;
        }

        // 计算本次伤害（可能暴击）
        double damage = m_HitDamage;
        bool isCritical = UnityEngine.Random.value < m_Ctx.Attribute.CritRate;
        if (isCritical)
        {
            damage *= m_Ctx.Attribute.CritDamage;
        }

        // 构建命中检测上下文
        HitContext context = new HitContext
        {
            Attacker = m_Caster,
            AttackerPosition = m_Caster.transform.position,
            AttackerForward = m_Caster.transform.forward,
            AttackerCamp = m_Caster.Camp,
            LockedTarget = null, // AOE 不需要锁定目标
            TargetPosition = m_Caster.transform.position, // AOE 以自身为中心
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = m_Config.DamageType == 2,
            IsTrueDamage = m_Config.DamageType == 3,
            AOERadius = (float)m_Config.AreaRadius,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(m_Caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = OnUltimateHit,
        };

        // 使用 AOE 检测器执行命中
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        detector.Execute(context);
    }

    /// <summary>
    /// 结束引导
    /// </summary>
    private void EndChanneling()
    {
        m_IsChanneling = false;
        m_ChannelRemain = 0f;
        m_HitTimer = 0f;
        m_Caster = null;

        // 恢复待机状态
        if (m_Ctx?.Entity != null && m_Ctx.Entity.CurrentState == ChessState.Channeling)
        {
            m_Ctx.Entity.ChangeState(ChessState.Idle);
        }

        DebugEx.LogModule("ChangeUltimate", "月华天倾引导结束");
    }

    /// <summary>
    /// 大招命中回调（每命中一个目标调用一次）
    /// </summary>
    private void OnUltimateHit(ChessEntity target, double damage, bool isCritical)
    {
        if (target == null)
            return;

        DebugEx.LogModule(
            "ChangeUltimate",
            $"月华天倾命中: {target.Config?.Name}, 伤害={damage:F1}{(isCritical ? " (暴击)" : "")}"
        );
    }

    #endregion
}
