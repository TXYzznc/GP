using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特殊效果生命周期管理器
/// 负责效果的创建、应用、追踪和移除
/// </summary>
public class SpecialEffectManager : SingletonBase<SpecialEffectManager>
{
    /// <summary>
    /// 效果所有权 Key
    /// </summary>
    private struct EffectKey : System.IEquatable<EffectKey>
    {
        public GameObject Owner;
        public EffectSourceType SourceType;
        public int SourceId;

        public EffectKey(GameObject owner, EffectSourceType sourceType, int sourceId)
        {
            Owner = owner;
            SourceType = sourceType;
            SourceId = sourceId;
        }

        public bool Equals(EffectKey other)
        {
            return Owner == other.Owner && SourceType == other.SourceType && SourceId == other.SourceId;
        }

        public override bool Equals(object obj) => obj is EffectKey other && Equals(other);

        public override int GetHashCode()
        {
            int hash = Owner != null ? Owner.GetHashCode() : 0;
            hash = hash * 31 + (int)SourceType;
            hash = hash * 31 + SourceId;
            return hash;
        }
    }

    private readonly Dictionary<EffectKey, List<SpecialEffectInstance>> m_ActiveEffects = new();

    /// <summary>
    /// 应用效果（单目标）
    /// </summary>
    public bool ApplyEffect(int effectId, GameObject target, EffectSourceType sourceType, int sourceId)
    {
        if (effectId <= 0)
            return false;

        var effectData = ItemManager.Instance?.GetSpecialEffectData(effectId);
        if (effectData == null)
        {
            DebugEx.Warning(nameof(SpecialEffectManager), $"效果数据不存在: {effectId}");
            return false;
        }

        var instance = CreateInstance(effectData);
        if (instance == null)
            return false;

        instance.Initialize(effectId, effectData, target, sourceType, sourceId);
        instance.Apply();

        // Instant 效果不追踪
        if (effectData.EffectType == SpecialEffectType.Instant)
            return true;

        // 非 Instant 效果需要 target 来追踪
        if (target == null)
            return true;

        // 追踪活跃效果
        var key = new EffectKey(target, sourceType, sourceId);
        if (!m_ActiveEffects.TryGetValue(key, out var list))
        {
            list = new List<SpecialEffectInstance>();
            m_ActiveEffects[key] = list;
        }
        list.Add(instance);

        return true;
    }

    /// <summary>
    /// 应用效果（多目标，用于羁绊等）
    /// </summary>
    public bool ApplyEffect(int effectId, List<GameObject> targets, EffectSourceType sourceType, int sourceId, GameObject caster = null)
    {
        if (effectId <= 0 || targets == null || targets.Count == 0)
            return false;

        var effectData = ItemManager.Instance?.GetSpecialEffectData(effectId);
        if (effectData == null)
        {
            DebugEx.Warning(nameof(SpecialEffectManager), $"效果数据不存在: {effectId}");
            return false;
        }

        // Buff 类型支持多目标
        if (effectData.EffectType == SpecialEffectType.Buff)
        {
            var instance = new BuffSpecialEffect();
            instance.Initialize(effectId, effectData, targets[0], sourceType, sourceId);
            instance.SetMultiTargets(targets);
            instance.Apply();

            if (instance.IsActive)
            {
                // 以第一个目标为 key 进行追踪
                var key = new EffectKey(targets[0], sourceType, sourceId);
                if (!m_ActiveEffects.TryGetValue(key, out var list))
                {
                    list = new List<SpecialEffectInstance>();
                    m_ActiveEffects[key] = list;
                }
                list.Add(instance);
            }
            return true;
        }

        // 其他类型对每个目标独立应用
        bool success = true;
        foreach (var target in targets)
        {
            if (target == null) continue;
            success &= ApplyEffect(effectId, target, sourceType, sourceId);
        }
        return success;
    }

    /// <summary>
    /// 按来源移除效果
    /// </summary>
    public void RemoveEffectBySource(GameObject owner, EffectSourceType sourceType, int sourceId)
    {
        var key = new EffectKey(owner, sourceType, sourceId);
        if (!m_ActiveEffects.TryGetValue(key, out var list))
            return;

        foreach (var instance in list)
        {
            if (instance.IsActive)
            {
                instance.Remove();
            }
        }

        m_ActiveEffects.Remove(key);
    }

    /// <summary>
    /// 按来源移除效果（多目标）
    /// </summary>
    public void RemoveEffectBySource(List<GameObject> owners, EffectSourceType sourceType, int sourceId)
    {
        if (owners == null) return;

        // 先尝试第一个目标（多目标 Buff 以第一个目标为 key）
        if (owners.Count > 0 && owners[0] != null)
        {
            RemoveEffectBySource(owners[0], sourceType, sourceId);
        }

        // 再尝试其他目标（Passive 等独立应用的场景）
        for (int i = 1; i < owners.Count; i++)
        {
            if (owners[i] != null)
            {
                RemoveEffectBySource(owners[i], sourceType, sourceId);
            }
        }
    }

    /// <summary>
    /// 获取目标身上的所有活跃效果
    /// </summary>
    public List<SpecialEffectInstance> GetActiveEffects(GameObject owner)
    {
        var result = new List<SpecialEffectInstance>();
        foreach (var kvp in m_ActiveEffects)
        {
            if (kvp.Key.Owner == owner)
            {
                result.AddRange(kvp.Value);
            }
        }
        return result;
    }

    /// <summary>
    /// 清除所有效果
    /// </summary>
    public void ClearAll()
    {
        foreach (var kvp in m_ActiveEffects)
        {
            foreach (var instance in kvp.Value)
            {
                if (instance.IsActive)
                    instance.Remove();
            }
        }
        m_ActiveEffects.Clear();
    }

    private SpecialEffectInstance CreateInstance(SpecialEffectData data)
    {
        switch (data.EffectType)
        {
            case SpecialEffectType.Instant:
                return new InstantEffect();
            case SpecialEffectType.Buff:
                return new BuffSpecialEffect();
            case SpecialEffectType.Passive:
                return new PassiveEffect();
            case SpecialEffectType.Trigger:
                return new TriggerEffect();
            default:
                DebugEx.Warning(nameof(SpecialEffectManager), $"未知效果类型: {data.EffectType}");
                return null;
        }
    }
}
