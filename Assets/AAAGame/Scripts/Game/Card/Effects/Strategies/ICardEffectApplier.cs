using System.Collections.Generic;

/// <summary>
/// 卡牌效果应用器接口
/// </summary>
public interface ICardEffectApplier
{
    /// <summary>
    /// 对目标应用效果
    /// </summary>
    void ApplyEffect(List<ChessEntity> targets, CardData cardData);
}
