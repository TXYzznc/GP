using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 护盾再生 Buff
/// 每隔一段时间恢复指定数量的护盾
/// CustomData 格式：{"ShieldRegenPerSecond":50}
/// </summary>
public class ShieldRegenBuff : BuffBase
{
    private double m_ShieldRegenPerTick;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        // 从 CustomData 读取护盾恢复数值
        m_ShieldRegenPerTick = 50; // 默认 50
        if (!string.IsNullOrEmpty(config?.CustomData) && config.CustomData != "{}")
        {
            try
            {
                var json = JObject.Parse(config.CustomData);
                if (json.TryGetValue("ShieldRegenPerSecond", out var token))
                {
                    m_ShieldRegenPerTick = token.ToObject<double>();
                }
            }
            catch { }
        }
    }

    protected override void OnTick()
    {
        if (Ctx?.OwnerAttribute == null) return;

        Ctx.OwnerAttribute.ModifyShield(m_ShieldRegenPerTick);
    }
}
