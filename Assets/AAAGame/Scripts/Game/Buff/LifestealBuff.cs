using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 吸血之刃 Buff
/// 拥有此 Buff 时，造成伤害的同时恢复造成伤害的一定百分比血量
/// CustomData 格式：{"lifestealRatio":0.4}
///
/// 注意：此 Buff 的吸血效果在卡牌伤害应用器（DamageWithCoefficientApplier）中实现
/// </summary>
public class LifestealBuff : BuffBase
{
    private double m_LifestealRatio;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        // 从 CustomData 读取吸血比例
        m_LifestealRatio = 0.4; // 默认 40%
        if (!string.IsNullOrEmpty(config?.CustomData) && config.CustomData != "{}")
        {
            try
            {
                var json = JObject.Parse(config.CustomData);
                if (json.TryGetValue("lifestealRatio", out var token))
                {
                    m_LifestealRatio = token.ToObject<double>();
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 获取吸血比例，供伤害系统调用
    /// </summary>
    public double GetLifestealRatio() => m_LifestealRatio;
}
