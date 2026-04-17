using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌效果目标选择器接口
/// </summary>
public interface ICardTargetSelector
{
    /// <summary>
    /// 从所有棋子中选择目标
    /// </summary>
    List<ChessEntity> SelectTargets(List<ChessEntity> allChess, CardData cardData, Vector3 targetPosition);
}
