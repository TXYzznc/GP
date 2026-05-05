using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// AOE 范围命中检测器
/// 使用 OverlapSphere 检测范围内的所有敌人
/// </summary>
public class AOEHitDetector : HitDetectorBase
    , IEndableHitDetector
{
    public override AttackHitType HitType => AttackHitType.AOE;

    [Serializable]
    private class AOEOptions
    {
        public bool IsContinuous;
        public string AOEShape = "Circle";
        public float TickInterval = 0.05f;
        public float SectorAngle = 0f;
        public float InnerRadius = 0f;
        public string HitPolicy = "OncePerTarget";
    }

    private enum AOEHitPolicy
    {
        OncePerTarget,
        EveryTick,
        OncePerTick,
    }

    private readonly Collider[] m_HitBuffer = new Collider[64];
    private readonly HashSet<ChessEntity> m_HitTargets = new HashSet<ChessEntity>();
    private readonly HashSet<ChessEntity> m_HitTargetsThisTick = new HashSet<ChessEntity>();
    private CancellationTokenSource m_Cts;
    private CancellationTokenSource m_LinkedCts;
    private AOEOptions m_Options;
    private AOEHitPolicy m_HitPolicy;
    private HitContext m_Context;

    protected override void DoExecute(HitContext context)
    {
        m_Context = context;
        m_Options = ParseOptions(context.SkillConfig?.CustomData);
        m_HitPolicy = ParseHitPolicy(m_Options?.HitPolicy);
        m_HitTargets.Clear();
        m_HitTargetsThisTick.Clear();

        if (m_Options != null && m_Options.IsContinuous)
        {
            StartContinuous();
            return;
        }

        ScanAndApplyOncePerExecute();
        Complete();
    }

    public void End()
    {
        if (!IsExecuting)
            return;

        m_Cts?.Cancel();
        m_LinkedCts?.Cancel();
        m_Cts?.Dispose();
        m_LinkedCts?.Dispose();
        m_Cts = null;
        m_LinkedCts = null;
        Complete();
    }

    public override void Cancel()
    {
        End();
    }

    private void StartContinuous()
    {
        m_Cts?.Cancel();
        m_LinkedCts?.Cancel();
        m_Cts?.Dispose();
        m_LinkedCts?.Dispose();
        m_Cts = new CancellationTokenSource();

        CancellationToken destroyToken = m_Context.Attacker != null
            ? m_Context.Attacker.GetCancellationTokenOnDestroy()
            : CancellationToken.None;

        m_LinkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_Cts.Token, destroyToken);
        RunContinuousAsync(m_LinkedCts.Token).Forget();
    }

    private async UniTaskVoid RunContinuousAsync(CancellationToken token)
    {
        float interval = m_Options != null ? Mathf.Max(0f, m_Options.TickInterval) : 0.05f;
        while (!token.IsCancellationRequested && IsExecuting)
        {
            ScanAndApplyOncePerTick();
            if (interval <= 0f)
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            else
                await UniTask.Delay(TimeSpan.FromSeconds(interval), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
        }
    }

    private void ScanAndApplyOncePerExecute()
    {
        m_HitTargetsThisTick.Clear();
        ScanAndApplyInternal(isTick: false);
    }

    private void ScanAndApplyOncePerTick()
    {
        m_HitTargetsThisTick.Clear();
        ScanAndApplyInternal(isTick: true);
    }

    private void ScanAndApplyInternal(bool isTick)
    {
        Vector3 center = m_Context.TargetPosition;
        float radius = m_Context.AOERadius > 0 ? m_Context.AOERadius : m_Context.Range;
        if (radius <= 0f)
            return;

        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, m_HitBuffer, m_Context.EnemyLayerMask);

        int maxHits = m_Context.MaxHitCount > 0 ? m_Context.MaxHitCount : int.MaxValue;
        int actualHitCount = 0;

        for (int i = 0; i < hitCount && actualHitCount < maxHits; i++)
        {
            Collider col = m_HitBuffer[i];
            if (col == null)
                continue;

            ChessEntity target = col.GetComponent<ChessEntity>() ?? col.GetComponentInParent<ChessEntity>();
            if (target == null)
                continue;

            if (target == m_Context.Attacker)
                continue;

            if (target.CurrentState == ChessState.Dead)
                continue;

            if (!IsEnemy(target, m_Context.AttackerCamp))
                continue;

            if (!PassShapeFilter(target.transform.position, center, radius))
                continue;

            if (!PassHitPolicy(target, isTick))
                continue;

            ApplyDamage(target, m_Context);
            actualHitCount++;
        }
    }

    private bool PassHitPolicy(ChessEntity target, bool isTick)
    {
        switch (m_HitPolicy)
        {
            case AOEHitPolicy.EveryTick:
                return true;

            case AOEHitPolicy.OncePerTick:
                return m_HitTargetsThisTick.Add(target);

            case AOEHitPolicy.OncePerTarget:
            default:
                if (!m_HitTargets.Add(target))
                    return false;
                if (isTick)
                    m_HitTargetsThisTick.Add(target);
                return true;
        }
    }

    private bool PassShapeFilter(Vector3 targetPos, Vector3 center, float outerRadius)
    {
        if (m_Options == null)
            return true;

        string shape = m_Options.AOEShape ?? "Circle";
        if (string.Equals(shape, "Circle", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(shape, "Ring", StringComparison.OrdinalIgnoreCase))
        {
            float sqrDist = (targetPos - center).sqrMagnitude;
            float inner = Mathf.Max(0f, m_Options.InnerRadius);
            if (inner <= 0f)
                return true;
            return sqrDist >= inner * inner && sqrDist <= outerRadius * outerRadius;
        }

        if (string.Equals(shape, "Sector", StringComparison.OrdinalIgnoreCase))
        {
            float angle = Mathf.Max(0f, m_Options.SectorAngle);
            if (angle <= 0f || angle >= 360f)
                return true;

            Vector3 toTarget = targetPos - center;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
                return true;

            Vector3 forward = m_Context.AttackerForward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector3.forward;

            float halfAngle = angle * 0.5f;
            float actualAngle = Vector3.Angle(forward.normalized, toTarget.normalized);
            return actualAngle <= halfAngle;
        }

        return true;
    }

    private AOEOptions ParseOptions(string customData)
    {
        if (string.IsNullOrEmpty(customData) || customData == "{}")
            return null;

        try
        {
            return JsonUtility.FromJson<AOEOptions>(customData);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private AOEHitPolicy ParseHitPolicy(string policy)
    {
        if (string.IsNullOrEmpty(policy))
            return AOEHitPolicy.OncePerTarget;

        if (string.Equals(policy, "EveryTick", StringComparison.OrdinalIgnoreCase))
            return AOEHitPolicy.EveryTick;

        if (string.Equals(policy, "OncePerTick", StringComparison.OrdinalIgnoreCase))
            return AOEHitPolicy.OncePerTick;

        return AOEHitPolicy.OncePerTarget;
    }
}
