using UnityEngine;

/// <summary>
/// 后羿大招：日陨 (ID=14)
/// 向前方发射贯穿投掷物，对路径上的所有敌人造成伤害
/// 并对受到伤害的敌人施加2层灼烧效果
/// </summary>
public class HouyiUltimate : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 4; // 大招
    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.LogModule("HouyiUltimate", "日陨初始化完成");
    }

    public override bool TryCast()
    {
        if (!base.TryCast())
            return false;

        DebugEx.LogModule(
            "HouyiUltimate",
            $"日陨释放! 消耗MP={m_Config.MpCost}, 冷却={m_Config.Cooldown}秒"
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
            DebugEx.ErrorModule("HouyiUltimate", "ExecuteSkill: caster 为 null");
            return;
        }

        // 1. 查找目标并计算发射方向
        ChessEntity target = FindNearestEnemy(caster);
        Vector3 direction;
        Vector3 endPosition;

        if (target != null)
        {
            // 获取目标中心位置
            Vector3 targetCenter = EntityPositionHelper.GetCenterPosition(target);

            // 计算发射方向
            direction = (targetCenter - caster.transform.position).normalized;

            // 计算终点位置（沿方向飞行到最大射程）
            float maxRange = (float)m_Config.CastRange * 20;
            endPosition = caster.transform.position + direction * maxRange;

            DebugEx.LogModule(
                "HouyiUltimate",
                $"日陨穿透模式: 目标={target.Config?.Name}, 方向={direction}, 射程={maxRange}"
            );
        }
        else
        {
            // 没有目标，朝前方发射
            direction = caster.transform.forward;
            endPosition = caster.transform.position + direction * (float)m_Config.CastRange;

            DebugEx.WarningModule("HouyiUltimate", $"日陨未找到目标，朝前方发射");
        }

        // 2. 计算伤害
        double damage = CalculateDamage(caster, out bool isCritical);

        DebugEx.LogModule("HouyiUltimate", $"日陨伤害: {damage:F1}{(isCritical ? " (暴击)" : "")}");

        // 3. 构建命中检测上下文（穿透型：不锁定目标）
        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = direction, // ⭐ 使用计算出的方向
            AttackerCamp = caster.Camp,
            LockedTarget = null, // ⭐ 方向模式：不锁定目标
            TargetPosition = endPosition, // 仅用于日志，实际不使用
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = m_Config.DamageType == 2,
            IsTrueDamage = m_Config.DamageType == 3,
            Range = (float)m_Config.CastRange,
            PenetrationCount = m_Config.PenetrationCount,
            ProjectilePrefabId = m_Config.ProjectilePrefabId,
            ProjectileSpeed = (float)m_Config.ProjectileSpeed,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = OnUltimateHit,
        };

        // 4. 播放技能释放特效
        PlaySkillEffect(caster);

        // 5. 执行命中检测
        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.Projectile);
        detector.Execute(context);

        DebugEx.LogModule(
            "HouyiUltimate",
            $"日陨发射完成: 方向={direction}, 终点={endPosition}, 穿透={m_Config.PenetrationCount}"
        );
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 投掷物命中回调（每命中一个目标调用一次）
    /// </summary>
    private void OnUltimateHit(ChessEntity target, double damage, bool isCritical)
    {
        if (target == null)
            return;

        // 施加2层灼烧
        ApplyBurnStacks(target, 2);

        DebugEx.LogModule(
            "HouyiUltimate",
            $"日陨命中: {target.Config?.Name}, 伤害={damage:F1}, +2层灼烧"
        );
    }

    /// <summary>
    /// 施加灼烧层数
    /// </summary>
    private void ApplyBurnStacks(ChessEntity target, int stacks)
    {
        if (target == null || target.BuffManager == null)
            return;

        var burnBuff = target.BuffManager.GetBuff(1) as BurnBuff;
        if (burnBuff != null)
        {
            // 已有灼烧，直接加层数
            burnBuff.AddStacks(stacks);
            DebugEx.LogModule("HouyiUltimate", $"灼烧叠加: {target.Config?.Name}, +{stacks}层");
        }
        else
        {
            // 没有灼烧，先添加1层，再加剩余层数
            target.BuffManager.AddBuff(1, m_Ctx.Owner, m_Ctx.Attribute);
            burnBuff = target.BuffManager.GetBuff(1) as BurnBuff;
            if (burnBuff != null && stacks > 1)
            {
                burnBuff.AddStacks(stacks - 1);
            }
            DebugEx.LogModule("HouyiUltimate", $"灼烧施加: {target.Config?.Name}, {stacks}层");
        }
    }

    #endregion
}
