using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 被动效果（装备/宝物穿戴期间永久生效）
/// 直接修改 ChessAttribute，无图标、不可驱散、卸下即移除
/// </summary>
public class PassiveEffect : SpecialEffectInstance
{
    private readonly Dictionary<AttributeType, float> m_AppliedModifiers = new();

    public override void Apply()
    {
        var entity = GetChessEntity();
        if (entity == null || entity.Attribute == null)
        {
            DebugEx.Warning(nameof(PassiveEffect), $"效果 [{EffectId}] 无法应用：目标无 ChessAttribute");
            return;
        }

        var modifiers = ParseAttributeModifiers();
        if (modifiers == null || modifiers.Count == 0)
            return;

        foreach (var kvp in modifiers)
        {
            ApplyModifier(entity.Attribute, kvp.Key, kvp.Value);
            m_AppliedModifiers[kvp.Key] = kvp.Value;
        }

        IsActive = true;
        DebugEx.Log(nameof(PassiveEffect), $"应用被动效果 [{EffectId}]，修改了 {m_AppliedModifiers.Count} 项属性");
    }

    public override void Remove()
    {
        if (!IsActive) return;

        var entity = GetChessEntity();
        if (entity == null || entity.Attribute == null)
        {
            DebugEx.Warning(nameof(PassiveEffect), $"效果 [{EffectId}] 无法移除：目标无 ChessAttribute");
            IsActive = false;
            m_AppliedModifiers.Clear();
            return;
        }

        foreach (var kvp in m_AppliedModifiers)
        {
            ApplyModifier(entity.Attribute, kvp.Key, -kvp.Value);
        }

        DebugEx.Log(nameof(PassiveEffect), $"移除被动效果 [{EffectId}]，还原了 {m_AppliedModifiers.Count} 项属性");
        m_AppliedModifiers.Clear();
        IsActive = false;
    }

    private Dictionary<AttributeType, float> ParseAttributeModifiers()
    {
        var paramsObj = EffectData.GetParams();
        if (paramsObj == null)
            return null;

        var modToken = paramsObj["AttributeModifiers"];
        if (modToken == null || modToken.Type != JTokenType.Object)
            return null;

        var result = new Dictionary<AttributeType, float>();
        try
        {
            var obj = (JObject)modToken;
            foreach (var prop in obj.Properties())
            {
                if (System.Enum.TryParse<AttributeType>(prop.Name, true, out var attrType))
                {
                    result[attrType] = prop.Value.ToObject<float>();
                }
            }
        }
        catch (System.Exception e)
        {
            DebugEx.Error(nameof(PassiveEffect), $"解析 AttributeModifiers 失败 EffectId={EffectId}: {e.Message}");
        }

        return result;
    }

    private void ApplyModifier(ChessAttribute attribute, AttributeType type, float value)
    {
        switch (type)
        {
            case AttributeType.Attack:
                attribute.ModifyAtkDamage(value);
                break;
            case AttributeType.MaxHP:
                attribute.SetMaxHp(attribute.MaxHp + value);
                break;
            case AttributeType.CritRate:
                attribute.ModifyCritRate(value);
                break;
            case AttributeType.AttackSpeed:
                attribute.ModifyAtkSpeed(value);
                break;
            case AttributeType.MoveSpeed:
                attribute.ModifyMoveSpeed(value);
                break;
            case AttributeType.Defense:
                attribute.ModifyArmor(value);
                break;
            case AttributeType.SpellPower:
                attribute.ModifySpellPower(value);
                break;
            default:
                DebugEx.Warning(nameof(PassiveEffect), $"未处理的 AttributeType: {type}");
                break;
        }
    }

    private ChessEntity GetChessEntity()
    {
        if (Target == null) return null;
        return Target.GetComponent<ChessEntity>();
    }
}
