using UnityEngine;

/// <summary>
/// 即时效果（消耗品使用等）
/// 执行后立即完成，无需追踪和移除
/// </summary>
public class InstantEffect : SpecialEffectInstance
{
    public override void Apply()
    {
        string effectType = EffectData.GetParamValue<string>("type", "");
        if (string.IsNullOrEmpty(effectType))
        {
            DebugEx.Warning(nameof(InstantEffect), $"效果 [{EffectId}] 未配置 type 参数");
            IsActive = false;
            return;
        }

        var effect = ItemEffectFactory.Create(effectType);
        if (effect == null)
        {
            DebugEx.Warning(nameof(InstantEffect), $"未知效果类型: {effectType}");
            IsActive = false;
            return;
        }

        var context = new ItemEffectContext(EffectData, SourceId, Target, Target);
        effect.Execute(context);

        IsActive = false;
    }

    public override void Remove()
    {
        // 即时效果无需移除
    }
}
