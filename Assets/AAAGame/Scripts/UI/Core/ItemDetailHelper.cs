using System.Text;
using Newtonsoft.Json.Linq;

/// <summary>
/// 宝物/装备详情文本构建工具（供 InventoryUI、AwardItemUI 等复用）
/// </summary>
public static class ItemDetailHelper
{
    /// <summary>
    /// 构建宝物/装备的效果状态文本（BaseAttributes + SpecialEffect Description）
    /// </summary>
    public static string BuildStatusText(int itemId, ItemType itemType)
    {
        if (itemType != ItemType.Treasure && itemType != ItemType.Equipment)
            return string.Empty;

        string baseAttrJson = null;
        int specialEffectId = 0;

        if (itemType == ItemType.Treasure)
        {
            var table = GF.DataTable.GetDataTable<TreasureTable>();
            var row = table?.GetDataRow(itemId);
            if (row != null)
            {
                baseAttrJson = row.BaseAttributes;
                specialEffectId = row.SpecialEffectId;
            }
        }
        else
        {
            var table = GF.DataTable.GetDataTable<EquipmentTable>();
            var row = table?.GetDataRow(r => r.ItemTableId == itemId);
            if (row != null)
            {
                baseAttrJson = row.BaseAttributes;
                specialEffectId = row.SpecialEffectId;
            }
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(baseAttrJson) && baseAttrJson != "{}")
        {
            try
            {
                var obj = JObject.Parse(baseAttrJson);
                foreach (var prop in obj.Properties())
                {
                    string label = AttributeKeyToLabel(prop.Name);
                    string value = prop.Value.ToString();
                    sb.AppendLine($"{label} +{value}");
                }
            }
            catch
            {
                sb.AppendLine(baseAttrJson);
            }
        }

        if (specialEffectId > 0)
        {
            var effectTable = GF.DataTable.GetDataTable<SpecialEffectTable>();
            var effectRow = effectTable?.GetDataRow(specialEffectId);
            if (effectRow != null && !string.IsNullOrEmpty(effectRow.Description))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.AppendLine($"[特效] {effectRow.Description}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    public static string AttributeKeyToLabel(string key)
    {
        return key switch
        {
            "MaxHP"          => "生命值上限",
            "Attack"         => "攻击力",
            "MaxMP"          => "法力值上限",
            "AttackSpeed"    => "攻击速度",
            "CritRate"       => "暴击率",
            "CritDamage"     => "暴击伤害",
            "Defense"        => "防御力",
            "MagicResist"    => "魔法抗性",
            "SpellPower"     => "法术强度",
            "MoveSpeed"      => "移动速度",
            "CooldownReduce" => "冷却缩减",
            "Evasion"        => "闪避率",
            "DamageReduction"=> "伤害减免",
            "HealthRegen"    => "生命恢复",
            "CritReduction"  => "暴击减免",
            "Regeneration"   => "回复力",
            "MagicPower"     => "魔法强度",
            _                => key,
        };
    }
}
