using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 宝物槽位右键点击处理器
/// 用于处理装备槽位的右键卸装功能
/// </summary>
public class TreasureSlotRightClickHandler : MonoBehaviour, IPointerClickHandler
{
    private int m_TreasureInstanceId;
    private System.Action<int> m_OnRightClicked;

    public void Initialize(int treasureInstanceId, System.Action<int> onRightClicked)
    {
        m_TreasureInstanceId = treasureInstanceId;
        m_OnRightClicked = onRightClicked;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 检查是否为右键点击
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (m_OnRightClicked != null)
        {
            DebugEx.Log(nameof(TreasureSlotRightClickHandler), $"右键点击宝物槽位: {m_TreasureInstanceId}");
            m_OnRightClicked.Invoke(m_TreasureInstanceId);
        }
    }
}
