using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌目标选择器集合
/// 职责：
/// 1. 为卡牌效果提供统一的目标选择接口（ICardTargetSelector）
/// 2. 封装复杂的目标筛选逻辑（距离、HP、死亡状态等）
/// 3. 支持卡牌配置驱动的目标选择（TargetType、AreaRadius、TargetDeadState）
///
/// 使用场景：
/// - CardEffectExecutor（通用框架）
/// - 特殊卡牌效果（LifeDrainCardEffect、ResurrectionCardEffect等）
/// - CardSlotItem（UI预览）
///
/// 注意：
/// - 不要在AI或技能系统中使用这些Selector
/// - 复杂的目标筛选应该新增Selector，而不是在CardEffect中写逻辑
/// </summary>

/// <summary>
/// 选择敌方全体
/// </summary>
public class AllEnemiesSelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        var targets = new List<ChessEntity>();

        if (CombatEntityTracker.Instance == null)
            return targets;

        var enemies = CombatEntityTracker.Instance.GetEnemies((int)CampType.Player);
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && ChessTargetFinder.IsValidTargetByDeadState(enemy, cardData.TableRow.TargetDeadState))
                    targets.Add(enemy);
            }
        }

        return targets;
    }
}

/// <summary>
/// 选择友方全体
/// </summary>
public class AllAlliesSelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        var targets = new List<ChessEntity>();

        if (CombatEntityTracker.Instance == null)
            return targets;

        var allies = CombatEntityTracker.Instance.GetAllies((int)CampType.Player);
        if (allies != null)
        {
            foreach (var ally in allies)
            {
                if (ally != null && ChessTargetFinder.IsValidTargetByDeadState(ally, cardData.TableRow.TargetDeadState))
                    targets.Add(ally);
            }
        }

        return targets;
    }
}

/// <summary>
/// 选择范围内最近的敌人
/// </summary>
public class ClosestEnemySelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        float radius = cardData.AreaRadius;
        ChessEntity closest = null;
        float closestDistance = float.MaxValue;

        if (CombatEntityTracker.Instance == null)
            return new List<ChessEntity>();

        var enemies = CombatEntityTracker.Instance.GetEnemies((int)CampType.Player);
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null || !ChessTargetFinder.IsValidTargetByDeadState(enemy, cardData.TableRow.TargetDeadState))
                    continue;

                float distance = Vector3.Distance(enemy.transform.position, targetPosition);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }
        }

        return closest != null ? new List<ChessEntity> { closest } : new List<ChessEntity>();
    }
}

/// <summary>
/// 选择范围内最近的友方
/// </summary>
public class ClosestAllySelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        float radius = cardData.AreaRadius;
        ChessEntity closest = null;
        float closestDistance = float.MaxValue;

        if (CombatEntityTracker.Instance == null)
            return new List<ChessEntity>();

        var allies = CombatEntityTracker.Instance.GetAllies((int)CampType.Player);
        if (allies != null)
        {
            foreach (var ally in allies)
            {
                if (ally == null || !ChessTargetFinder.IsValidTargetByDeadState(ally, cardData.TableRow.TargetDeadState))
                    continue;

                float distance = Vector3.Distance(ally.transform.position, targetPosition);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = ally;
                }
            }
        }

        return closest != null ? new List<ChessEntity> { closest } : new List<ChessEntity>();
    }
}

/// <summary>
/// 选择HP最低的友方
/// </summary>
public class LowestHpAllySelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        if (CombatEntityTracker.Instance == null)
            return new List<ChessEntity>();

        var allies = CombatEntityTracker.Instance.GetAllies((int)CampType.Player);
        if (allies == null || allies.Count == 0)
            return new List<ChessEntity>();

        ChessEntity lowestHp = null;
        double minHp = double.MaxValue;

        foreach (var ally in allies)
        {
            if (ally == null || ally.Attribute == null || !ChessTargetFinder.IsValidTargetByDeadState(ally, cardData.TableRow.TargetDeadState))
                continue;

            if (ally.Attribute.CurrentHp < minHp)
            {
                minHp = ally.Attribute.CurrentHp;
                lowestHp = ally;
            }
        }

        return lowestHp != null ? new List<ChessEntity> { lowestHp } : new List<ChessEntity>();
    }
}

/// <summary>
/// 选择范围内所有敌人
/// </summary>
public class EnemiesInRadiusSelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        float radius = cardData.AreaRadius;
        var targets = new List<ChessEntity>();

        if (CombatEntityTracker.Instance == null)
            return targets;

        var enemies = CombatEntityTracker.Instance.GetEnemies((int)CampType.Player);
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null || !ChessTargetFinder.IsValidTargetByDeadState(enemy, cardData.TableRow.TargetDeadState))
                    continue;

                float distance = Vector3.Distance(enemy.transform.position, targetPosition);
                if (distance <= radius)
                    targets.Add(enemy);
            }
        }

        return targets;
    }
}

/// <summary>
/// 选择施法者（召唤师）自身
/// </summary>
public class SelfSelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        var targets = new List<ChessEntity>();

        var playerCharacterManager = PlayerCharacterManager.Instance;
        if (playerCharacterManager == null)
            return targets;

        var playerCharacter = playerCharacterManager.CurrentPlayerCharacter;
        if (playerCharacter == null)
            return targets;

        var casterChess = playerCharacter.GetComponent<ChessEntity>();
        if (casterChess != null)
            targets.Add(casterChess);

        return targets;
    }
}

/// <summary>
/// 选择全体友方（不含召唤师）
/// </summary>
public class AllAllyExcludeSummonerSelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        var targets = new List<ChessEntity>();

        if (CombatEntityTracker.Instance == null)
            return targets;

        var summonerChess = GetSummonerChess();
        var allies = CombatEntityTracker.Instance.GetAllies((int)CampType.Player);

        if (allies != null)
        {
            foreach (var ally in allies)
            {
                if (ally != null && ally != summonerChess && ChessTargetFinder.IsValidTargetByDeadState(ally, cardData.TableRow.TargetDeadState))
                    targets.Add(ally);
            }
        }

        return targets;
    }

    private ChessEntity GetSummonerChess()
    {
        var playerCharacterManager = PlayerCharacterManager.Instance;
        if (playerCharacterManager == null)
            return null;

        var playerCharacter = playerCharacterManager.CurrentPlayerCharacter;
        if (playerCharacter == null)
            return null;

        return playerCharacter.GetComponent<ChessEntity>();
    }
}

/// <summary>
/// 选择范围内所有友方
/// </summary>
public class AlliesInRadiusSelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        float radius = cardData.AreaRadius;
        var targets = new List<ChessEntity>();

        if (CombatEntityTracker.Instance == null)
            return targets;

        var allies = CombatEntityTracker.Instance.GetAllies((int)CampType.Player);
        if (allies != null)
        {
            foreach (var ally in allies)
            {
                if (ally == null || !ChessTargetFinder.IsValidTargetByDeadState(ally, cardData.TableRow.TargetDeadState))
                    continue;

                float distance = Vector3.Distance(ally.transform.position, targetPosition);
                if (distance <= radius)
                    targets.Add(ally);
            }
        }

        return targets;
    }
}

/// <summary>
/// 获取所有棋子（包括死亡）
/// 参数在 CardData 的 CustomData 中指定：
/// - campType: 0=友方，1=敌方，2=全体
/// </summary>
public class AllChessIncludingDeadSelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        var targets = new List<ChessEntity>();

        if (CombatEntityTracker.Instance == null)
            return targets;

        int campType = cardData.GetParam("campType", 0); // 0=友方, 1=敌方, 2=全体

        if (campType == 0)
        {
            var allies = CombatEntityTracker.Instance.GetAlliesIncludingDead((int)CampType.Player);
            if (allies != null)
            {
                foreach (var ally in allies)
                {
                    if (ally != null && ChessTargetFinder.IsValidTargetByDeadState(ally, cardData.TableRow.TargetDeadState))
                        targets.Add(ally);
                }
            }
        }
        else if (campType == 1)
        {
            var enemies = CombatEntityTracker.Instance.GetEnemiesIncludingDead((int)CampType.Player);
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy != null && ChessTargetFinder.IsValidTargetByDeadState(enemy, cardData.TableRow.TargetDeadState))
                        targets.Add(enemy);
                }
            }
        }
        else if (campType == 2)
        {
            var allies = CombatEntityTracker.Instance.GetAlliesIncludingDead((int)CampType.Player);
            var enemies = CombatEntityTracker.Instance.GetEnemiesIncludingDead((int)CampType.Player);

            if (allies != null)
            {
                foreach (var ally in allies)
                {
                    if (ally != null && ChessTargetFinder.IsValidTargetByDeadState(ally, cardData.TableRow.TargetDeadState))
                        targets.Add(ally);
                }
            }

            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy != null && ChessTargetFinder.IsValidTargetByDeadState(enemy, cardData.TableRow.TargetDeadState))
                        targets.Add(enemy);
                }
            }
        }

        return targets;
    }
}

/// <summary>
/// 获取所有死亡的棋子（仅死亡）
/// 参数在 CardData 的 CustomData 中指定：
/// - campType: 0=友方，1=敌方，2=全体
/// </summary>
public class DeadChessOnlySelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        var targets = new List<ChessEntity>();

        if (CombatEntityTracker.Instance == null)
            return targets;

        int campType = cardData.GetParam("campType", 0); // 0=友方, 1=敌方, 2=全体

        var deadFilter = new List<ChessEntity>();

        if (campType == 0)
        {
            var allies = CombatEntityTracker.Instance.GetAlliesIncludingDead((int)CampType.Player);
            if (allies != null)
                deadFilter.AddRange(allies);
        }
        else if (campType == 1)
        {
            var enemies = CombatEntityTracker.Instance.GetEnemiesIncludingDead((int)CampType.Player);
            if (enemies != null)
                deadFilter.AddRange(enemies);
        }
        else if (campType == 2)
        {
            var allies = CombatEntityTracker.Instance.GetAlliesIncludingDead((int)CampType.Player);
            var enemies = CombatEntityTracker.Instance.GetEnemiesIncludingDead((int)CampType.Player);

            if (allies != null)
                deadFilter.AddRange(allies);
            if (enemies != null)
                deadFilter.AddRange(enemies);
        }

        foreach (var chess in deadFilter)
        {
            if (chess != null && chess.Attribute.IsDead && ChessTargetFinder.IsValidTargetByDeadState(chess, cardData.TableRow.TargetDeadState))
                targets.Add(chess);
        }

        return targets;
    }
}

/// <summary>
/// 选择范围内最近的死亡友方
/// 用于复活卡牌等需要定位死亡目标的效果
/// </summary>
public class ClosestDeadAllySelector : ICardTargetSelector
{
    public List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition)
    {
        float radius = cardData.AreaRadius;
        ChessEntity closest = null;
        float closestDistance = float.MaxValue;

        if (CombatEntityTracker.Instance == null)
            return new List<ChessEntity>();

        var allies = CombatEntityTracker.Instance.GetAlliesIncludingDead((int)CampType.Player);
        DebugEx.Log("ClosestDeadAllySelector", $"获取友方列表: 共 {allies?.Count ?? 0} 个，搜索半径={radius}");

        if (allies != null)
        {
            foreach (var ally in allies)
            {
                // 只需要检查是否死亡，因为这个Selector本身就是为死亡目标设计的
                if (ally == null || !ally.Attribute.IsDead)
                    continue;

                float distance = Vector3.Distance(ally.transform.position, targetPosition);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = ally;
                }
            }
        }

        if (closest != null)
        {
            DebugEx.Log("ClosestDeadAllySelector", $"找到目标: {closest.Config?.Name}");
        }
        else
        {
            DebugEx.Log("ClosestDeadAllySelector", "未找到任何符合条件的目标");
        }

        return closest != null ? new List<ChessEntity> { closest } : new List<ChessEntity>();
    }
}
