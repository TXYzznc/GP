using UnityEngine;

/// <summary>
/// 触发效果（预留框架）
/// 满足条件时触发，如"受到攻击时 30% 概率反击"
/// </summary>
public class TriggerEffect : SpecialEffectInstance
{
    public override void Apply()
    {
        // TODO: 订阅 ChessAttribute 事件
        IsActive = true;
        DebugEx.Log(nameof(TriggerEffect), $"触发效果 [{EffectId}] 已注册（预留框架）");
    }

    public override void Remove()
    {
        if (!IsActive) return;
        // TODO: 取消事件订阅
        IsActive = false;
        DebugEx.Log(nameof(TriggerEffect), $"触发效果 [{EffectId}] 已注销");
    }
}
