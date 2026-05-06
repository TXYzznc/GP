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
        {
            DebugEx.Error("ResurrectionCardEffect", "❌ CombatEntityTracker.Instance 为 null");
            return null;
        }

        var allies = CombatEntityTracker.Instance.GetAlliesIncludingDead((int)CampType.Player);
        if (allies == null)
        {
            DebugEx.Error("ResurrectionCardEffect", "❌ GetAlliesIncludingDead() 返回 null");
            return null;
        }

        if (allies.Count == 0)
        {
            DebugEx.Warning("ResurrectionCardEffect", "⚠️ 没有找到任何友方（allies.Count=0）");
            return null;
        }

        float radius = m_CardData.AreaRadius;
        ChessEntity closestDead = null;
        float closestDistance = float.MaxValue;

        int deadCount = 0;
        int totalCount = allies.Count;
        DebugEx.Log("ResurrectionCardEffect", $"扫描 {totalCount} 个友方（包含已死亡），范围={radius}，释放位置={targetPosition}");

        foreach (var ally in allies)
        {
            if (ally == null)
            {
                DebugEx.Warning("ResurrectionCardEffect", "  - 发现 null 友方");
                continue;
            }

            string allyName = ally.Config?.Name ?? "Unknown";
            if (ally.CurrentState == ChessState.Dead)
            {
                deadCount++;
                float distance = Vector3.Distance(ally.transform.position, targetPosition);
                DebugEx.Log("ResurrectionCardEffect", $"  ✓ 死亡友方: {allyName}, 位置={ally.transform.position}, 距离={distance:F2}");

                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDead = ally;
                }
            }
            else
            {
                DebugEx.Log("ResurrectionCardEffect", $"  - 活着的友方: {allyName}, 状态={ally.CurrentState}");
            }
        }

        if (deadCount == 0)
            DebugEx.Warning("ResurrectionCardEffect", $"⚠️ 扫描完成：总计 {totalCount} 个友方，没有找到死亡单位");
        else if (closestDead == null)
            DebugEx.Warning("ResurrectionCardEffect", $"⚠️ 扫描完成：找到 {deadCount} 个死亡友方，但都超出范围（半径={radius}）");
        else
            DebugEx.Success("ResurrectionCardEffect", $"✓ 找到可复活目标: {closestDead.Config?.Name}");

        return closestDead;
    }
}
