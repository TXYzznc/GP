using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;

/// <summary>
/// 邪灵技能一：污染斩击 (ID=33)
/// 向前方挥出巨大斩击，对前方AOE范围内所有敌人造成大量物理伤害
/// 命中后对目标施加2层腐蚀Buff（ID=7）
/// </summary>
public class EvilSpiritSkill1 : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 3; // 主动技能
    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("EvilSpiritSkill1", "污染斩击技能初始化完成");
    }

    public override bool TryCast()
    {
        if (!base.TryCast())
            return false;
        DebugEx.Log("EvilSpiritSkill1", "污染斩击释放");
        return true;
    }

    public override bool CanCast()
    {
        if (!base.CanCast())
            return false;

        ChessEntity caster = m_Ctx?.Entity;
        if (caster == null)
            return false;

        ChessEntity nearest = FindNearestEnemy(caster);
        if (nearest == null)
            return false;

        float range = (float)m_Config.CastRange;
        if (range <= 0f)
            range = (float)caster.Attribute.AtkRange;

        float dist = Vector3.Distance(caster.transform.position, nearest.transform.position);
        return dist <= range;
    }

    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.Error("EvilSpiritSkill1", "ExecuteSkill: caster 为 null");
            return;
        }

        double damage = CalculateDamage(caster, out bool isCritical);
        DebugEx.Log("EvilSpiritSkill1", $"污染斩击伤害: {damage:F1}{(isCritical ? " (暴击)" : "")}");

        HitContext context = new HitContext
        {
            Attacker = caster,
            AttackerPosition = caster.transform.position,
            AttackerForward = caster.transform.forward,
            AttackerCamp = caster.Camp,
            TargetPosition = caster.transform.position,
            BaseDamage = damage,
            IsCritical = isCritical,
            IsMagicDamage = m_Config.DamageType == 2,
            IsTrueDamage = m_Config.DamageType == 3,
            Range = (float)caster.Attribute.AtkRange,
            AOERadius = (float)m_Config.AreaRadius,
            MaxHitCount = m_Config.HitCount,
            EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp),
            EffectId = m_Config.EffectId,
            HitEffectId = m_Config.HitEffectId,
            SkillConfig = m_Config,
            OnHitCallback = (target, dmg, isCrit) => ApplyCorrosion(caster, target),
        };

        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        SpawnSkillEffectAsync(caster).Forget();
        DebugEx.Success("EvilSpiritSkill1", "污染斩击执行完成");
    }

    #endregion

    #region 私有方法

    private int m_EffectSpawnVersion;

    private async UniTaskVoid SpawnSkillEffectAsync(ChessEntity caster)
    {
        if (caster == null || caster.CombatController == null)
            return;

        if (m_Config.EffectId <= 0)
            return;

        int version = ++m_EffectSpawnVersion;
        GameObject prefab = await ResourceExtension.LoadPrefabAsync(m_Config.EffectId);
        if (prefab == null || caster == null || caster.CurrentState == ChessState.Dead)
            return;

        if (version != m_EffectSpawnVersion)
            return;

        Vector3 pos = caster.GetEffectSpawnPosition(m_Config.EffectSpawnHeight);
        GameObject instance = Object.Instantiate(prefab, pos, caster.transform.rotation);
        caster.CombatController.SetCurrentActionEffectInstance(instance);
    }

    /// <summary>
    /// 对命中目标施加2层腐蚀
    /// </summary>
    private void ApplyCorrosion(ChessEntity caster, ChessEntity target)
    {
        if (target?.BuffManager == null)
            return;

        // 连续AddBuff两次叠加2层（MaxStack=2由BuffTable控制上限）
        target.BuffManager.AddBuff(7, caster.gameObject, caster.Attribute);
        target.BuffManager.AddBuff(7, caster.gameObject, caster.Attribute);

        DebugEx.Log("EvilSpiritSkill1", $"对 {target.Config?.Name} 施加2层腐蚀");
    }

    #endregion
}
