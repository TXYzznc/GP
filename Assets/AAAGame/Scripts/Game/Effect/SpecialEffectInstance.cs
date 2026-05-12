using UnityEngine;

/// <summary>
/// 特殊效果实例基类
/// 管理效果的应用和移除生命周期
/// </summary>
public abstract class SpecialEffectInstance
{
    public int EffectId { get; private set; }
    public SpecialEffectData EffectData { get; private set; }
    public GameObject Target { get; private set; }
    public EffectSourceType SourceType { get; private set; }
    public int SourceId { get; private set; }
    public bool IsActive { get; protected set; }

    public void Initialize(int effectId, SpecialEffectData data, GameObject target, EffectSourceType sourceType, int sourceId)
    {
        EffectId = effectId;
        EffectData = data;
        Target = target;
        SourceType = sourceType;
        SourceId = sourceId;
    }

    public abstract void Apply();
    public abstract void Remove();
}
