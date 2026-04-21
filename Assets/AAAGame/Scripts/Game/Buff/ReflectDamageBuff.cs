using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 反伤之盾 Buff
/// 当拥有此 Buff 时受到伤害，返回伤害的一定百分比给攻击者（真实伤害）
/// CustomData 格式：{"reflectDamageRatio":0.3}
/// </summary>
public class ReflectDamageBuff : BuffBase
{
    private double m_ReflectRatio;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        // 从 CustomData 读取反伤比例
        m_ReflectRatio = 0.3; // 默认 30%
        if (!string.IsNullOrEmpty(config?.CustomData) && config.CustomData != "{}")
        {
            try
            {
                var json = JObject.Parse(config.CustomData);
                if (json.TryGetValue("reflectDamageRatio", out var token))
                {
                    m_ReflectRatio = token.ToObject<double>();
                }
            }
            catch { }
        }
    }


    public double GetReflectRatio() => m_ReflectRatio;
}
