using UnityEngine;

/// <summary>
/// 卡牌效果接口
/// </summary>
public interface ICardEffect
{
    /// <summary>
    /// 初始化效果
    /// </summary>
    void Init(CardData cardData);

    /// <summary>
    /// 执行效果
    /// </summary>
    void Execute(Vector3 targetPosition);
}
