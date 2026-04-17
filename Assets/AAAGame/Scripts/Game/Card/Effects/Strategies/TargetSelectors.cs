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
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
                targets.Add(chess);
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
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
                targets.Add(chess);
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

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = chess;
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

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = chess;
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
        ChessEntity lowestHp = null;
        double minHp = double.MaxValue;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player && chess.Attribute != null)
            {
                if (chess.Attribute.CurrentHp < minHp)
                {
                    minHp = chess.Attribute.CurrentHp;
                    lowestHp = chess;
                }
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

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance <= radius)
                    targets.Add(chess);
            }
        }

        return targets;
    }
}
