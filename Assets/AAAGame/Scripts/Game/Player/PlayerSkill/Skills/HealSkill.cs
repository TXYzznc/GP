using UnityEngine;

public class HealSkill : IPlayerSkill
{
    public int SkillId => common.Id;

    private PlayerSkillContext ctx;
    private SkillCommonConfig common;
    private float cdRemain;

    private HealParamSO param;

    public void Init(PlayerSkillContext ctx, SkillCommonConfig common, SkillParamSO _param)
    {
        this.ctx = ctx;
        this.common = common;

        param = _param as HealParamSO;
        if (param == null)
            DebugEx.Error($"HealSkill missing HealParamSO for skillId={common.Id}");
    }

    public void Tick(float dt)
    {
        if (cdRemain > 0f) cdRemain -= dt;
    }

    public bool TryCast()
    {
        if (cdRemain > 0f) return false;

        // 输出使用技能日志
        DebugEx.Log($"使用技能：{common.Name}");

        // 执行治疗逻辑
        ExecuteHeal();

        // 进入冷却
        cdRemain = common.Cooldown;
        return true;
    }

    /// <summary>
    /// 执行治疗逻辑（占位实现）
    /// </summary>
    private void ExecuteHeal()
    {
        if (ctx == null || ctx.Owner == null)
        {
            DebugEx.Warning("[HealSkill] 上下文为空，无法执行治疗");
            return;
        }

        // TODO: 实现治疗逻辑
        // 1. 获取玩家生命值组件
        // 2. 恢复生命值
        // 3. 播放治疗特效和音效

        DebugEx.Log($"[HealSkill] 恢复生命值，目标: {ctx.Owner.name}");
    }
}
