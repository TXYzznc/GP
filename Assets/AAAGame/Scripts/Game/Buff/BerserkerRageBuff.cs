using Newtonsoft.Json.Linq;

/// <summary>
/// 狂怒之心 Buff（ID=4003）
/// 继承 StatModBuff，对普通单位应用 AtkDamage+15%（来自 StatMods），
/// 对召唤师自身则改为应用 CustomData 中的 SummonerAtkDamage+30%。
/// </summary>
public class BerserkerRageBuff : StatModBuff
{
    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        // 检测目标是否为召唤师
        bool isSummoner = ctx.Owner != null &&
                          ctx.Owner.GetComponent<SummonerCombatProxy>() != null;

        if (!isSummoner || string.IsNullOrEmpty(config?.CustomData)) return;

        JObject json;
        try { json = JObject.Parse(config.CustomData); }
        catch { return; }

        if (!json.TryGetValue("SummonerAtkDamage", out var token)) return;

        string s = token.ToString().Trim();
        bool isPercent = s.EndsWith("%");
        if (!double.TryParse(isPercent ? s.TrimEnd('%') : s, out double raw)) return;

        double value = isPercent ? raw / 100.0 : raw;

        // 覆盖为召唤师专属的伤害加成
        SetMods(new StatMod(StatType.AtkDamage, value, isPercent));
    }
}
