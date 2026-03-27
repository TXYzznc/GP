using UnityEngine;

public class FireBallSkill : IPlayerSkill
{
    public int SkillId => common.Id;

    private PlayerSkillContext ctx;
    private SkillCommonConfig common;
    private float cdRemain;

    private FireBallParamSO param;

    public void Init(PlayerSkillContext ctx, SkillCommonConfig common, SkillParamSO _param)
    {
        this.ctx = ctx;
        this.common = common;

        param = _param as FireBallParamSO;
        if (param == null)
            DebugEx.Error($"FireBallSkill missing FireBallParamSO for skillId={common.Id}");
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

        // 执行火球逻辑
        ExecuteFireBall();

        // 进入冷却
        cdRemain = common.Cooldown;
        return true;
    }

    /// <summary>
    /// 执行火球逻辑（占位实现）
    /// </summary>
    private void ExecuteFireBall()
    {
        if (ctx == null || ctx.Transform == null)
        {
            DebugEx.Warning("[FireBallSkill] 上下文为空，无法执行火球");
            return;
        }

        // TODO: 实现火球发射逻辑
        // 1. 实例化火球预制体
        // 2. 设置火球飞行速度
        // 3. 处理碰撞和伤害逻辑

        DebugEx.Log($"[FireBallSkill] 发射火球，位置: {ctx.Transform.position}");
    }
}
