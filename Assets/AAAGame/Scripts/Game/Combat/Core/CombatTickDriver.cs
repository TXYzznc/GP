using UnityEngine;

/// <summary>
/// 战斗帧循环驱动器
/// 为纯C#战斗管理器提供 Update Tick
/// </summary>
public class CombatTickDriver : SingletonBase<CombatTickDriver>
{
    private void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        // 更新放置管理器
        ChessPlacementManager.Instance?.Tick();

        // 更新选中管理器
        ChessSelectionManager.Instance?.Tick();
    }

    private void OnDestroy()
    {
        base.OnDestroy();
    }
}
