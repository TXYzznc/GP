using UnityEngine;

/// <summary>
/// 玩家战斗状态标记
/// 挂在玩家 GameObject 上，供敌人 AI 判断该玩家是否正在战斗中
/// 多人场景下，每个玩家独立标记，敌人只跳过战斗中的玩家
/// </summary>
public class PlayerCombatFlag : MonoBehaviour
{
    /// <summary>该玩家是否正在战斗中</summary>
    public bool IsInCombat { get; set; }
}
