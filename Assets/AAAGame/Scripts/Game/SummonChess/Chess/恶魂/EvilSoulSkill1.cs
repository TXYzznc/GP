using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;

/// <summary>
/// 恶魂技能一：心灵扭曲 (ID=43)
/// 诵读古老音节，对范围内敌人造成法术伤害
/// 使其陷入混乱状态（3s内有概率攻击队友）
/// </summary>
public class EvilSoulSkill1 : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 3; // 主动技能

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("EvilSoulSkill1", "心灵扭曲技能初始化完成");
    }

    public override bool TryCast()
    {
        if (!base.TryCast())
            return false;
        DebugEx.Log("EvilSoulSkill1", "心灵扭曲释放");
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
            DebugEx.Error("EvilSoulSkill1", "ExecuteSkill: caster 为 null");
            return;
        }

        double damage = CalculateDamage(caster, out bool isCritical);
        DebugEx.Log("EvilSoulSkill1", $"心灵扭曲伤害: {damage:F1}{(isCritical ? " (暴击)" : "")}");

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
            OnHitCallback = (target, dmg, isCrit) => ApplyConfusion(caster, target),
        };

        IHitDetector detector = HitDetectorFactory.GetDetector(AttackHitType.AOE);
        caster.CombatController?.SetCurrentHitDetector(detector);
        detector.Execute(context);

        SpawnSkillEffectAsync(caster).Forget();
        DebugEx.Success("EvilSoulSkill1", "心灵扭曲执行完成");
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

    private void ApplyConfusion(ChessEntity caster, ChessEntity target)
    {
        if (target?.BuffManager == null)
            return;

        target.BuffManager.AddBuff(5009, caster.gameObject, caster.Attribute); // 混乱 ID=5009

        DebugEx.Log("EvilSoulSkill1", $"对 {target.Config?.Name} 施加混乱");
    }

    #endregion
}
