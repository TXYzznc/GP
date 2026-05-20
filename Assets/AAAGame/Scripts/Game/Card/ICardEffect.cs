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
    /// 执行效果，返回 true 表示成功作用于目标，false 表示无有效目标（卡牌应返回手牌）
    /// </summary>
    bool Execute(Vector3 targetPosition);
}
