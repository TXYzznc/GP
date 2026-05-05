using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;

public class SpawnedEffectHitDetector : HitDetectorBase, IEndableHitDetector
{
    public override AttackHitType HitType => AttackHitType.Melee;

    private GameObject m_EffectInstance;
    private SpawnedEffectHitbox m_Hitbox;
    private readonly HashSet<ChessEntity> m_HitTargets = new HashSet<ChessEntity>();
    private int m_CurrentHitCount;
    private int m_SpawnVersion;

    protected override void DoExecute(HitContext context)
    {
        if (context.EffectId <= 0)
        {
            DebugEx.Warning(nameof(SpawnedEffectHitDetector), "EffectId <= 0，降级到瞬发");
            FallbackToInstant(context);
            return;
        }

        m_HitTargets.Clear();
        m_CurrentHitCount = 0;

        m_SpawnVersion++;
        int version = m_SpawnVersion;
        SpawnAsync(context, version).Forget();
    }

    public void End()
    {
        if (!IsExecuting)
        {
            Cleanup();
            return;
        }

        Cleanup();
        Complete();
    }

    public override void Cancel()
    {
        Cleanup();
        base.Cancel();
    }

    private async UniTaskVoid SpawnAsync(HitContext context, int version)
    {
        GameObject prefab = await ResourceExtension.LoadPrefabAsync(context.EffectId);
        if (!IsExecuting || m_CurrentContext != context || version != m_SpawnVersion)
        {
            return;
        }

        if (prefab == null)
        {
            DebugEx.Warning(nameof(SpawnedEffectHitDetector), $"特效预制体加载失败: effectId={context.EffectId}，降级到瞬发");
            FallbackToInstant(context);
            return;
        }

        var attacker = context.Attacker;
        var instance = UnityEngine.Object.Instantiate(prefab, context.TargetPosition, attacker != null ? attacker.transform.rotation : Quaternion.identity);
        if (attacker != null)
        {
            instance.transform.SetParent(attacker.transform, true);
        }

        m_EffectInstance = instance;

        m_Hitbox = instance.GetComponentInChildren<SpawnedEffectHitbox>(true);
        if (m_Hitbox == null)
        {
            m_Hitbox = instance.AddComponent<SpawnedEffectHitbox>();
        }

        m_Hitbox.Initialize(context.AttackerCamp, OnHitboxTriggerEnter);
    }

    private void OnHitboxTriggerEnter(ChessEntity target)
    {
        if (!IsExecuting || m_CurrentContext == null)
            return;

        if (target == null || target.CurrentState == ChessState.Dead)
            return;

        if (m_HitTargets.Contains(target))
            return;

        int maxHits = m_CurrentContext.MaxHitCount > 0 ? m_CurrentContext.MaxHitCount : int.MaxValue;
        if (m_CurrentHitCount >= maxHits)
            return;

        if (!IsEnemy(target, m_CurrentContext.AttackerCamp))
            return;

        m_HitTargets.Add(target);
        m_CurrentHitCount++;

        ApplyDamage(target, m_CurrentContext);
    }

    private void Cleanup()
    {
        if (m_Hitbox != null)
        {
            m_Hitbox.Clear();
        }

        if (m_EffectInstance != null)
        {
            UnityEngine.Object.Destroy(m_EffectInstance);
        }

        m_EffectInstance = null;
        m_Hitbox = null;
        m_HitTargets.Clear();
        m_CurrentHitCount = 0;
    }

    private void FallbackToInstant(HitContext context)
    {
        if (context.LockedTarget != null &&
            context.LockedTarget.CurrentState != ChessState.Dead &&
            IsEnemy(context.LockedTarget, context.AttackerCamp))
        {
            ApplyDamage(context.LockedTarget, context);
        }

        Complete();
    }
}

public class SpawnedEffectHitbox : MonoBehaviour
{
    private int m_OwnerCamp;
    private Action<ChessEntity> m_OnHit;
    private bool m_IsEnabled;

    public void Initialize(int ownerCamp, Action<ChessEntity> onHit)
    {
        m_OwnerCamp = ownerCamp;
        m_OnHit = onHit;
        m_IsEnabled = true;

        var colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;
            colliders[i].isTrigger = true;
            colliders[i].enabled = true;
        }
    }

    public void Clear()
    {
        m_IsEnabled = false;
        m_OnHit = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_IsEnabled || m_OnHit == null || other == null)
            return;

        ChessEntity target = other.GetComponent<ChessEntity>() ?? other.GetComponentInParent<ChessEntity>();
        if (target == null)
            return;

        if (!CampRelationService.IsEnemy(m_OwnerCamp, target.Camp))
            return;

        m_OnHit.Invoke(target);
    }
}

