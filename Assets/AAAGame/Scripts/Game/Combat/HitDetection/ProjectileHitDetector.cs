using UnityEngine;
using Cysharp.Threading.Tasks;
using GameExtension;

/// <summary>
/// 投射物命中检测器
/// 生成子弹/箭矢,通过碰撞检测命中
/// </summary>
public class ProjectileHitDetector : HitDetectorBase
{
    public override AttackHitType HitType => AttackHitType.Projectile;

    /// <summary>当前投射物实例</summary>
    private ChessProjectile m_CurrentProjectile;

    protected override void DoExecute(HitContext context)
    {
        // ⭐ 使用异步加载投射物
        LoadAndSpawnProjectile(context).Forget();
    }

    /// <summary>
    /// 异步加载并生成投射物
    /// </summary>
    private async UniTaskVoid LoadAndSpawnProjectile(HitContext context)
    {
        // ⭐ 1. 通过 ResourceExtension 异步加载投射物预制体
        GameObject prefab = await ResourceExtension.LoadPrefabAsync(context.ProjectilePrefabId);

        if (prefab == null)
        {
            DebugEx.WarningModule("ProjectileHitDetector",
                $"投射物预制体加载失败 (ConfigId={context.ProjectilePrefabId}),降级到瞬发模式");
            FallbackToInstant(context);
            return;
        }

        // ⭐ 2. 计算投射物生成位置（攻击者前方）
        Vector3 spawnPos = context.AttackerPosition + Vector3.up * 1f + context.AttackerForward * 0.5f;

        // ⭐ 3. 计算目标中心点位置
        Vector3 targetCenter = context.TargetPosition;
        if (context.LockedTarget != null)
        {
            targetCenter = EntityPositionHelper.GetCenterPosition(context.LockedTarget, true);
            DebugEx.LogModule("ProjectileHitDetector",
                $"目标中心点: {targetCenter}, 目标名称: {context.LockedTarget.Config?.Name}");
        }

        // ⭐ 4. 计算发射方向（从生成位置指向目标中心点）
        Vector3 launchDirection = (targetCenter - spawnPos).normalized;

        DebugEx.LogModule("ProjectileHitDetector",
            $"投射物发射 - 生成位置: {spawnPos}, 目标位置: {targetCenter}, 发射方向: {launchDirection}");

        // ⭐ 5. 生成投射物（使用计算出的发射方向设置朝向）
        GameObject projectileObj = Object.Instantiate(
            prefab,
            spawnPos,
            Quaternion.LookRotation(launchDirection)  // ✅ 使用发射方向，不依赖攻击者朝向
        );

        m_CurrentProjectile = projectileObj.GetComponent<ChessProjectile>();

        if (m_CurrentProjectile == null)
        {
            m_CurrentProjectile = projectileObj.AddComponent<ChessProjectile>();
        }

        // ⭐ 6. 使用闭包捕获当前的 context，避免被后续投射物覆盖
        HitContext capturedContext = context;

        // ⭐ 7. 根据是否有锁定目标，选择初始化方式
        if (context.LockedTarget != null)
        {
            // 追踪模式：传入目标引用和发射方向
            m_CurrentProjectile.Initialize(
                context.AttackerCamp,
                context.LockedTarget,
                targetCenter,
                launchDirection,  // ✅ 传入计算好的发射方向
                context.ProjectileSpeed,
                context.PenetrationCount,
                (target) => OnProjectileHit(target, capturedContext)
            );

            DebugEx.Success("ProjectileHitDetector",
                $"投射物生成成功（追踪模式）: ConfigId={context.ProjectilePrefabId}, Target={context.LockedTarget.Config?.Name}");
        }
        else
        {
            // 方向模式：只传入发射方向，不传入目标
            m_CurrentProjectile.InitializeWithDirection(
                context.AttackerCamp,
                launchDirection,  // ✅ 使用计算好的发射方向
                context.ProjectileSpeed,
                context.PenetrationCount,
                (target) => OnProjectileHit(target, capturedContext)
            );

            DebugEx.Success("ProjectileHitDetector",
                $"投射物生成成功（方向模式）: ConfigId={context.ProjectilePrefabId}, Direction={launchDirection}");
        }

        m_CurrentProjectile.SetDestroyCallback(OnProjectileDestroyed);
    }

    /// <summary>
    /// 投射物命中回调（由投射物调用）
    /// </summary>
    /// <param name="target">命中的目标</param>
    /// <param name="context">捕获的命中上下文</param>
    private void OnProjectileHit(ChessEntity target, HitContext context)
    {
        DebugEx.LogModule("ProjectileHitDetector",
            $"OnProjectileHit 被调用: target={target?.Config?.Name}, attackerCamp={context.AttackerCamp}");

        if (context == null)
        {
            DebugEx.ErrorModule("ProjectileHitDetector", "context 为 null");
            return;
        }

        // 检查是否为敌人
        bool isEnemy = IsEnemy(target, context.AttackerCamp);
        DebugEx.LogModule("ProjectileHitDetector",
            $"阵营检查: target={target.Config?.Name}, targetCamp={target.Camp}, attackerCamp={context.AttackerCamp}, isEnemy={isEnemy}");

        if (!isEnemy)
        {
            DebugEx.WarningModule("ProjectileHitDetector",
                $"目标不是敌人，忽略命中: {target.Config?.Name} (Camp={target.Camp}) vs AttackerCamp={context.AttackerCamp}");
            return;
        }

        // 检查是否存活
        if (target.CurrentState == ChessState.Dead)
        {
            DebugEx.LogModule("ProjectileHitDetector", $"目标已死亡，忽略命中: {target.Config?.Name}");
            return;
        }

        // ⭐ 通过统一的 ApplyDamage 处理伤害、Buff 和特效
        ApplyDamage(target, context);

        DebugEx.LogModule("ProjectileHitDetector", $"投射物命中目标: {target.Config?.Name}");
    }

    /// <summary>
    /// 投射物销毁回调
    /// </summary>
    private void OnProjectileDestroyed()
    {
        m_CurrentProjectile = null;
        Complete();
    }

    public override void Cancel()
    {
        if (m_CurrentProjectile != null)
        {
            Object.Destroy(m_CurrentProjectile.gameObject);
            m_CurrentProjectile = null;
        }
        base.Cancel();
    }

    /// <summary>
    /// 降级到瞬发模式（当投射物预制体缺失时）
    /// </summary>
    private void FallbackToInstant(HitContext context)
    {
        // ✅ 检查目标有效性
        if (context.LockedTarget != null &&
            context.LockedTarget.CurrentState != ChessState.Dead &&
            IsEnemy(context.LockedTarget, context.AttackerCamp))
        {
            // ✅ 通过统一的 ApplyDamage 处理伤害、Buff 和特效
            ApplyDamage(context.LockedTarget, context);

            DebugEx.LogModule("ProjectileHitDetector",
                $"投射物预制体缺失,降级为瞬发模式命中: {context.LockedTarget.Config?.Name}");
        }

        // 完成检测
        Complete();
    }
}
