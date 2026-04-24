using System;
using Newtonsoft.Json.Linq;

/// <summary>
/// 最大生命值 Buff（ID=5014 血源强化）
/// StatMods 字段使用 {"MaxHP":"30%"} 格式。
/// 附加时：MaxHP 按比例提升，当前 HP 等比例增加；
/// 移除时：MaxHP 恢复，当前 HP 若超出新上限则裁剪。
/// </summary>
public class MaxHpBuff : BuffBase
{
    private double m_MaxHpDelta;    // 实际增加的 MaxHP 绝对值
    private bool m_Applied;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);
        m_Applied = false;
        m_MaxHpDelta = ParseMaxHpDelta(config, ctx?.OwnerAttribute);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Apply();
    }

    public override void OnExit()
    {
        Restore();
        base.OnExit();
    }

    public override bool OnStack()
    {
        // 刷新持续时间，不重叠 MaxHP 效果
        if (Config != null && Config.Duration > 0)
            DurationRemain = (float)Config.Duration;
        return true;
    }

    // ── 私有 ──────────────────────────────────────────────────────────
    private void Apply()
    {
        if (m_Applied || m_MaxHpDelta <= 0 || Ctx?.OwnerAttribute == null) return;

        var attr = Ctx.OwnerAttribute;
        double oldMax = attr.MaxHp;
        double newMax = oldMax + m_MaxHpDelta;

        attr.SetMaxHp(newMax);
        // 同步按相同绝对值增加当前 HP（体感更好）
        attr.ModifyHp(m_MaxHpDelta);

        m_Applied = true;
        DebugEx.LogModule("MaxHpBuff", $"MaxHP +{m_MaxHpDelta:F0} ({oldMax:F0} → {newMax:F0})");
    }

    private void Restore()
    {
        if (!m_Applied || Ctx?.OwnerAttribute == null) return;

        var attr = Ctx.OwnerAttribute;
        double savedHp = attr.CurrentHp;            // 记住当前血量
        double newMax = Math.Max(1, attr.MaxHp - m_MaxHpDelta);
        attr.SetMaxHp(newMax);
        // SetMaxHp 可能裁剪了 CurrentHp，恢复到保存值（上限为新 MaxHp）
        attr.SetHp(Math.Min(savedHp, newMax));

        m_Applied = false;
        DebugEx.LogModule("MaxHpBuff", $"MaxHP -{m_MaxHpDelta:F0} (恢复至 {newMax:F0})，当前HP保持 {Math.Min(savedHp, newMax):F0}");
    }

    private static double ParseMaxHpDelta(BuffTable config, ChessAttribute attr)
    {
        if (attr == null || string.IsNullOrEmpty(config?.StatMods) || config.StatMods == "{}")
            return 0;

        try
        {
            var json = JObject.Parse(config.StatMods);
            if (!json.TryGetValue("MaxHP", out var token)) return 0;

            string s = token.ToString().Trim();
            if (s.EndsWith("%"))
            {
                if (double.TryParse(s.TrimEnd('%'), out double pct))
                    return attr.MaxHp * (pct / 100.0);
            }
            else
            {
                if (double.TryParse(s, out double flat))
                    return flat;
            }
        }
        catch { }

        return 0;
    }
}
