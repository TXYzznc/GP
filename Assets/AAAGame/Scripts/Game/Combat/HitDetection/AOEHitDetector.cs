using UnityEngine;

/// <summary>
/// AOE 范围命中检测器
/// 使用 OverlapSphere 检测范围内的所有敌人
/// </summary>
public class AOEHitDetector : HitDetectorBase
{
    public override AttackHitType HitType => AttackHitType.AOE;

    /// <summary>碰撞缓冲区（避免 GC）</summary>
    private static readonly Collider[] s_HitBuffer = new Collider[32];

    protected override void DoExecute(HitContext context)
    {
        // 确定检测中心点
        Vector3 center = context.TargetPosition;
        float radius = context.AOERadius > 0 ? context.AOERadius : context.Range;

        if (radius <= 0)
        {
            DebugEx.Warning("[AOEHitDetector] 检测半径为 0");
            Complete();
            return;
        }

        // 执行范围检测
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, s_HitBuffer, context.EnemyLayerMask);

        DebugEx.LogModule("AOEHitDetector", $"检测位置: {center}, 半径: {radius}, 检测到数量: {hitCount}");

        int actualHitCount = 0;
        int maxHits = context.MaxHitCount > 0 ? context.MaxHitCount : int.MaxValue;

        for (int i = 0; i < hitCount && actualHitCount < maxHits; i++)
        {
            Collider col = s_HitBuffer[i];
            if (col == null) continue;

            // 获取棋子实体
            ChessEntity target = col.GetComponent<ChessEntity>();
            if (target == null)
            {
                target = col.GetComponentInParent<ChessEntity>();
            }

            if (target == null) continue;

            // 排除自己
            if (target == context.Attacker) continue;

            // 检查是否为敌人
            if (!IsEnemy(target, context.AttackerCamp)) continue;

            // 检查是否存活
            if (target.CurrentState == ChessState.Dead) continue;

            // 造成伤害
            ApplyDamage(target, context);
            actualHitCount++;
        }

        DebugEx.LogModule("AOEHitDetector", $"实际命中: {actualHitCount} 个目标");

        // 完成检测
        Complete();
    }
}
