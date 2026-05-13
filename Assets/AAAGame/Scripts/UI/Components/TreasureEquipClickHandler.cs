using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 宝物快速装备点击处理器
/// 用于处理宝物仓库中未装备宝物的快速装备功能
/// </summary>
public class TreasureEquipClickHandler : MonoBehaviour, IPointerClickHandler
{
    private int m_TreasureInstanceId;
    private System.Action<int> m_OnLeftClicked;

    public void Initialize(int treasureInstanceId, System.Action<int> onLeftClicked)
    {
        m_TreasureInstanceId = treasureInstanceId;
        m_OnLeftClicked = onLeftClicked;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 检查是否为左键点击
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (m_OnLeftClicked != null)
        {
            DebugEx.Log(nameof(TreasureEquipClickHandler), $"点击装备宝物: {m_TreasureInstanceId}");
            m_OnLeftClicked.Invoke(m_TreasureInstanceId);
        }
    }
}
