using System.Collections.Generic;
using UnityEngine;

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
            targets.AddRange(enemies);
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
            targets.AddRange(allies);
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
                if (enemy == null)
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
                if (ally == null)
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
            if (ally == null || ally.Attribute == null || ally.CurrentState == ChessState.Dead)
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
                if (enemy == null)
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
                if (ally != null && ally != summonerChess)
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
                if (ally == null)
                    continue;

                float distance = Vector3.Distance(ally.transform.position, targetPosition);
                if (distance <= radius)
                    targets.Add(ally);
            }
        }

        return targets;
    }
}
