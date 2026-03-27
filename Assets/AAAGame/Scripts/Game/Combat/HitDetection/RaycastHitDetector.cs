using UnityEngine;

/// <summary>
/// 射线命中检测器
/// 使用 Raycast 检测射线路径上的目标
/// </summary>
public class RaycastHitDetector : HitDetectorBase
{
    public override AttackHitType HitType => AttackHitType.Raycast;

    /// <summary>射线检测缓冲区</summary>
    private static readonly RaycastHit[] s_HitBuffer = new RaycastHit[16];

    protected override void DoExecute(HitContext context)
    {
        Vector3 origin = context.AttackerPosition + Vector3.up; // 稍微抬高起点
        Vector3 direction = context.AttackerForward;
        float maxDistance = context.Range > 0 ? context.Range : 50f;

        // 如果有锁定目标，朝向目标方向
        if (context.LockedTarget != null)
        {
            direction = (context.LockedTarget.transform.position - origin).normalized;
        }

        DebugEx.LogModule("RaycastHitDetector", $"起点: {origin}, 方向: {direction}, 距离: {maxDistance}");

        // 执行射线检测
        int hitCount = Physics.RaycastNonAlloc(origin, direction, s_HitBuffer, maxDistance, context.EnemyLayerMask);

        // 按距离排序（由近到远）
        System.Array.Sort(s_HitBuffer, 0, hitCount, new RaycastHitDistanceComparer());

        int actualHitCount = 0;
        int maxHits = context.MaxHitCount > 0 ? context.MaxHitCount : 1; // 默认只命中 1 个

        for (int i = 0; i < hitCount && actualHitCount < maxHits; i++)
        {
            RaycastHit hit = s_HitBuffer[i];
            if (hit.collider == null) continue;

            // 获取棋子实体
            ChessEntity target = hit.collider.GetComponent<ChessEntity>();
            if (target == null)
            {
                target = hit.collider.GetComponentInParent<ChessEntity>();
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

            DebugEx.LogModule("RaycastHitDetector", $"射线命中: {target.Config?.Name}, 距离: {hit.distance:F2}");
        }

        // 完成检测
        Complete();
    }

    /// <summary>
    /// 射线检测距离比较器
    /// </summary>
    private class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public int Compare(RaycastHit x, RaycastHit y)
        {
            return x.distance.CompareTo(y.distance);
        }
    }
}
