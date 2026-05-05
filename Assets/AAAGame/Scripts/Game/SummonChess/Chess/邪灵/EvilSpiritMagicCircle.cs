using System;
using System.Collections.Generic;
using UnityEngine;

public partial class EvilSpiritMagicCircle : MonoBehaviour
{
    [Serializable]
    private class CustomDataWrapper
    {
        public float DotInterval = 1f;
        public float CorrosionInterval = 5f;
        public float DotDamageCoeff = 0.25f;
        public float DotBaseDamage = 10f;
        public float ExplosionDamageCoeff = 1.2f;
        public float ExplosionBaseDamage = 30f;
    }

    private SummonChessSkillTable m_Config;
    private ChessEntity m_Caster;
    private int m_EnemyLayerMask;

    private float m_Radius;
    private bool m_IsActive;
    private bool m_HasExploded;
    private bool m_IsFinished;

    private float m_DotTimer;
    private float m_DotInterval;
    private float m_CorrosionInterval;
    private double m_DotDamagePerTick;
    private double m_ExplosionDamage;

    private readonly Dictionary<int, float> m_StayTimers = new();
    private readonly HashSet<int> m_EverEntered = new();

    private CustomDataWrapper m_CustomData;

    public void Initialize(SummonChessSkillTable config, ChessEntity caster, Vector3 center)
    {
        m_Config = config;
        m_Caster = caster;
        m_EnemyLayerMask = CampRelationService.GetEnemyLayerMask(caster.Camp);
        m_Radius = (float)config.AreaRadius;

        transform.position = center;

        m_CustomData = ParseCustomData(config.CustomData) ?? new CustomDataWrapper();
        m_DotInterval = Mathf.Max(0.1f, m_CustomData.DotInterval);
        m_CorrosionInterval = Mathf.Max(0.1f, m_CustomData.CorrosionInterval);

        double spellPower = caster.Attribute != null ? caster.Attribute.SpellPower : 0;
        m_DotDamagePerTick = spellPower * m_CustomData.DotDamageCoeff + m_CustomData.DotBaseDamage;
        m_ExplosionDamage = spellPower * m_CustomData.ExplosionDamageCoeff + m_CustomData.ExplosionBaseDamage;

        m_IsActive = false;
        m_HasExploded = false;
        m_IsFinished = false;
        m_DotTimer = 0f;
        m_StayTimers.Clear();
        m_EverEntered.Clear();
    }

    public void AnimEvent_MagicCircleSpawn()
    {
        if (m_IsFinished || m_IsActive)
            return;

        m_IsActive = true;
        m_DotTimer = 0f;

        ApplyEnterEffectsForCurrentEnemies();
    }

    public void AnimEvent_MagicCircleExplosion()
    {
        if (m_IsFinished || !m_IsActive || m_HasExploded)
            return;

        m_HasExploded = true;
        ApplyExplosionEffects();
    }

    public void AnimEvent_MagicCircleDisappear()
    {
        if (m_IsFinished)
            return;

        m_IsFinished = true;
        m_IsActive = false;
        Destroy(gameObject);
    }

    private void Update()
    {
        if (!m_IsActive || m_IsFinished || m_Caster == null || m_Caster.CurrentState == ChessState.Dead)
        {
            return;
        }

        float dt = Time.deltaTime;

        var insideEnemies = GetEnemiesInCircle();
        UpdateStayTimers(insideEnemies, dt);

        m_DotTimer += dt;
        if (m_DotTimer >= m_DotInterval)
        {
            m_DotTimer -= m_DotInterval;
            ApplyDotDamage(insideEnemies);
        }
    }

    private void ApplyEnterEffectsForCurrentEnemies()
    {
        var insideEnemies = GetEnemiesInCircle();
        for (int i = 0; i < insideEnemies.Count; i++)
        {
            var enemy = insideEnemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            int id = enemy.GetInstanceID();
            if (!m_EverEntered.Contains(id))
            {
                ApplyCorrosion(enemy, 2);
                m_EverEntered.Add(id);
            }

            if (!m_StayTimers.ContainsKey(id))
            {
                m_StayTimers[id] = 0f;
            }
        }
    }

    private void UpdateStayTimers(List<ChessEntity> insideEnemies, float dt)
    {
        var currentInside = new HashSet<int>();
        for (int i = 0; i < insideEnemies.Count; i++)
        {
            var enemy = insideEnemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            int id = enemy.GetInstanceID();
            currentInside.Add(id);

            if (!m_StayTimers.TryGetValue(id, out float stayTimer))
            {
                stayTimer = 0f;

                if (!m_EverEntered.Contains(id))
                {
                    ApplyCorrosion(enemy, 2);
                    m_EverEntered.Add(id);
                }
            }

            stayTimer += dt;
            if (stayTimer >= m_CorrosionInterval)
            {
                ApplyCorrosion(enemy, 1);
                stayTimer -= m_CorrosionInterval;
            }

            m_StayTimers[id] = stayTimer;
        }

        if (m_StayTimers.Count == 0)
            return;

        var keys = new List<int>(m_StayTimers.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int id = keys[i];
            if (!currentInside.Contains(id))
            {
                m_StayTimers[id] = 0f;
            }
        }
    }

    private void ApplyDotDamage(List<ChessEntity> insideEnemies)
    {
        for (int i = 0; i < insideEnemies.Count; i++)
        {
            var enemy = insideEnemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            enemy.Attribute?.TakeDamage(m_DotDamagePerTick, true, false, false);
        }
    }

    private void ApplyExplosionEffects()
    {
        var insideEnemies = GetEnemiesInCircle();
        for (int i = 0; i < insideEnemies.Count; i++)
        {
            var enemy = insideEnemies[i];
            if (enemy == null || enemy.CurrentState == ChessState.Dead)
                continue;

            if (m_Config.HitEffectId > 0)
            {
                CombatVFXManager.PlayEffect(m_Config.HitEffectId, enemy.transform.position);
            }

            enemy.Attribute?.TakeDamage(m_ExplosionDamage, true, false, false);
            ApplyCorrosion(enemy, 1);
        }
    }

    private void ApplyCorrosion(ChessEntity target, int stacks)
    {
        if (target?.BuffManager == null || stacks <= 0)
            return;

        for (int i = 0; i < stacks; i++)
        {
            target.BuffManager.AddBuff(7, m_Caster.gameObject, m_Caster.Attribute);
        }
    }

    private List<ChessEntity> GetEnemiesInCircle()
    {
        var results = new List<ChessEntity>();

        if (m_EnemyLayerMask == 0)
            return results;

        Collider[] colliders = Physics.OverlapSphere(transform.position, m_Radius, m_EnemyLayerMask);
        for (int i = 0; i < colliders.Length; i++)
        {
            var collider = colliders[i];
            if (collider == null)
                continue;

            var entity = collider.GetComponentInParent<ChessEntity>();
            if (entity == null || entity.CurrentState == ChessState.Dead)
                continue;

            bool exists = false;
            for (int j = 0; j < results.Count; j++)
            {
                if (results[j] == entity)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
                results.Add(entity);
        }

        return results;
    }

    private CustomDataWrapper ParseCustomData(string customData)
    {
        if (string.IsNullOrEmpty(customData) || customData == "{}")
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<CustomDataWrapper>(customData);
        }
        catch (Exception)
        {
            return null;
        }
    }

}

public class EvilSpiritMagicCircleAnimBridge : MonoBehaviour
{
    [SerializeField] private EvilSpiritMagicCircle m_Target;

    public void Bind(EvilSpiritMagicCircle target)
    {
        m_Target = target;
    }

    public void AnimEvent_MagicCircleSpawn()
    {
        m_Target?.AnimEvent_MagicCircleSpawn();
    }

    public void AnimEvent_MagicCircleExplosion()
    {
        m_Target?.AnimEvent_MagicCircleExplosion();
    }

    public void AnimEvent_MagicCircleDisappear()
    {
        m_Target?.AnimEvent_MagicCircleDisappear();
    }
}
