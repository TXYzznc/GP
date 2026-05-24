using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 宝物槽位右键点击处理器
/// 处理右键点击卸下宝物
/// </summary>
public class TreasureSlotRightClickHandler : MonoBehaviour, IPointerClickHandler
{
    private int m_TreasureInstanceId = -1;
    private System.Action<int> m_OnRightClickCallback;

    /// <summary>
    /// 初始化（由 CharacterBagUI 调用）
    /// </summary>
    public void Initialize(int treasureInstanceId, System.Action<int> onRightClickCallback)
    {
        m_TreasureInstanceId = treasureInstanceId;
        m_OnRightClickCallback = onRightClickCallback;
    }

    /// <summary>
    /// 鼠标点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 只处理右键点击
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (m_TreasureInstanceId > 0)
            {
                DebugEx.Log(
                    nameof(TreasureSlotRightClickHandler),
                    $"[OnPointerClick] 右键点击宝物槽: treasureInstanceId={m_TreasureInstanceId}"
                );

                // 调用回调函数（由 CharacterBagUI 处理卸装逻辑）
                m_OnRightClickCallback?.Invoke(m_TreasureInstanceId);
            }
            else
            {
                DebugEx.Warning(
                    nameof(TreasureSlotRightClickHandler),
                    "[OnPointerClick] 宝物槽为空，无法卸装"
                );
            }
        }
    }
}
