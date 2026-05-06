using UnityEngine;

/// <summary>
/// 不屈意志 (ID=1012)
/// 复活一个阵亡的友方单位
///
/// 注：复活必须针对已死亡的目标，因此使用自定义选择器而非通用框架
/// </summary>
public class ResurrectionCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null) return;

        // 自定义选择逻辑：在范围内找最近的死亡友方
        var closestDead = SelectClosestDeadAlly(targetPosition);
        if (closestDead == null)
            return;

        float reviveHpRatio = m_CardData.GetParam("reviveHpRatio", 0.5f);
        float reviveHp = (float)(closestDead.Attribute.MaxHp * reviveHpRatio);
        CardEffectHelper.HealTarget(closestDead, reviveHp);
        closestDead.ChangeState(ChessState.Idle);
        DebugEx.Log("ResurrectionCardEffect", $"复活 {closestDead.Config?.Name}，恢复 {reviveHpRatio * 100}% HP");

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }

    private ChessEntity SelectClosestDeadAlly(Vector3 targetPosition)
    {
        if (CombatEntityTracker.Instance == null)
            return null;

        var allies = CombatEntityTracker.Instance.GetAllies((int)CampType.Player);
        if (allies == null || allies.Count == 0)
            return null;

        float radius = m_CardData.AreaRadius;
        ChessEntity closestDead = null;
        float closestDistance = float.MaxValue;

        foreach (var ally in allies)
        {
            if (ally == null || ally.CurrentState != ChessState.Dead)
                continue;

            float distance = Vector3.Distance(ally.transform.position, targetPosition);
            if (distance <= radius && distance < closestDistance)
            {
                closestDistance = distance;
                closestDead = ally;
            }
        }

        return closestDead;
    }
}
