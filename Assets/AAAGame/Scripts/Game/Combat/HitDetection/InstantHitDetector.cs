using UnityEngine;

/// <summary>
/// 瞬发命中检测器
/// 直接对锁定目标造成伤害，无飞行时间
/// </summary>
public class InstantHitDetector : HitDetectorBase
{
    public override AttackHitType HitType => AttackHitType.Instant;

    protected override void DoExecute(HitContext context)
    {
        // 检查锁定目标是否有效
        if (context.LockedTarget == null)
        {
            DebugEx.Warning("[InstantHitDetector] 锁定目标为空");
            Complete();
            return;
        }

        // 检查目标是否存活
        if (context.LockedTarget.CurrentState == ChessState.Dead)
        {
            DebugEx.LogModule("InstantHitDetector", "目标已死亡");
            Complete();
            return;
        }

        // 检查是否为敌人
        if (!IsEnemy(context.LockedTarget, context.AttackerCamp))
        {
            DebugEx.Warning("[InstantHitDetector] 目标不是敌人");
            Complete();
            return;
        }

        // 直接造成伤害
        ApplyDamage(context.LockedTarget, context);

        // 完成检测
        Complete();
    }
}
