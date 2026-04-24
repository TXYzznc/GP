using System.Collections.Generic;

/// <summary>
/// 伤害应用器
/// </summary>
public class DamageApplier : ICardEffectApplier
{
    public void ApplyEffect(List<ChessEntity> targets, CardData cardData)
    {
        float damage = cardData.TableRow.BaseDamage;
        int damageType = cardData.TableRow.DamageType;

        foreach (var target in targets)
        {
            if (target != null)
                CardEffectHelper.DealDamage(target, damage, damageType);
        }
    }
}

/// <summary>
/// 治疗应用器
/// </summary>
public class HealApplier : ICardEffectApplier
{
    public void ApplyEffect(List<ChessEntity> targets, CardData cardData)
    {
        float healAmount = cardData.GetParam("healAmount", 150f);

        foreach (var target in targets)
        {
            if (target != null)
                CardEffectHelper.HealTarget(target, healAmount);
        }
    }
}

/// <summary>
/// 命中时应用 Buff（HitBuffIds）
/// </summary>
public class BuffApplier : ICardEffectApplier
{
    private ChessEntity m_CasterChess;

    public BuffApplier(ChessEntity casterChess = null)
    {
        m_CasterChess = casterChess;
    }

    public void ApplyEffect(List<ChessEntity> targets, CardData cardData)
    {
        var casterGO = m_CasterChess != null ? m_CasterChess.gameObject : null;
        foreach (var target in targets)
        {
            if (target != null)
            {
                foreach (int buffId in cardData.HitBuffIds)
                {
                    CardEffectHelper.ApplyBuff(target, buffId, casterGO);
                }
            }
        }
    }
}

/// <summary>
/// 立即应用 Buff（InstantBuffIds）
/// </summary>
public class InstantBuffApplier : ICardEffectApplier
{
    private ChessEntity m_CasterChess;

    public InstantBuffApplier(ChessEntity casterChess = null)
    {
        m_CasterChess = casterChess;
    }

    public void ApplyEffect(List<ChessEntity> targets, CardData cardData)
    {
        var casterGO = m_CasterChess != null ? m_CasterChess.gameObject : null;
        foreach (var target in targets)
        {
            if (target != null)
            {
                foreach (int buffId in cardData.InstantBuffIds)
                {
                    CardEffectHelper.ApplyBuff(target, buffId, casterGO);
                }
            }
        }
    }
}

/// <summary>
/// 带伤害系数的伤害应用器
/// 伤害 = BaseDamage + DamageCoeff × 施法者攻击力
/// </summary>
public class DamageWithCoefficientApplier : ICardEffectApplier
{
    private ChessEntity m_CasterChess;

    public DamageWithCoefficientApplier(ChessEntity casterChess = null)
    {
        m_CasterChess = casterChess;
    }

    public void ApplyEffect(List<ChessEntity> targets, CardData cardData)
    {
        float casterAtk = (float)(m_CasterChess?.Attribute?.AtkDamage ?? 0d);
        float damage = cardData.TableRow.BaseDamage + cardData.TableRow.DamageCoeff * casterAtk;
        int damageType = cardData.TableRow.DamageType;

        foreach (var target in targets)
        {
            if (target != null)
            {
                CardEffectHelper.DealDamage(target, damage, damageType, m_CasterChess != null ? m_CasterChess.Attribute : null);
            }
        }
    }

}
