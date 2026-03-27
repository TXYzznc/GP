using UnityEngine;

/// <summary>
/// 玩家检测信息数据结构
/// 用于传递敌人的检测结果
/// </summary>
public class PlayerDetectionInfo
{
    /// <summary>玩家是否在周围圈内</summary>
    public bool InCircleRange { get; set; }

    /// <summary>玩家是否在扇形视野内</summary>
    public bool InConeRange { get; set; }

    /// <summary>到玩家的距离（米）</summary>
    public float Distance { get; set; }

    /// <summary>当前警觉度（0-1）</summary>
    public float AlertLevel { get; set; }

    /// <summary>警觉度百分比（0-100%，用于UI显示）</summary>
    public float AlertProgress => AlertLevel * 100f;

    /// <summary>是否触发了警戒（警觉度>=阈值）</summary>
    public bool IsAlerted { get; set; }

    public PlayerDetectionInfo()
    {
        InCircleRange = false;
        InConeRange = false;
        Distance = float.MaxValue;
        AlertLevel = 0f;
        IsAlerted = false;
    }
}
