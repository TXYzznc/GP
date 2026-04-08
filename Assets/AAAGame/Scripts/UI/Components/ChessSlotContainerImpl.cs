using UnityEngine;

/// <summary>
/// 棋子装备槽容器实现（DetailInfoUI 上的装备槽）
/// 规则：接受来自 Equip 的拖入，右键卸下回背包，不支持拖出
/// </summary>
public class ChessSlotContainerImpl : SlotContainerBase
{
    #region 字段

    private int m_CurrentChessId = -1;
    private InventorySlot[] m_EquipSlotData;
    private DetailInfoUI m_DetailInfoUI;

    #endregion

    /// <summary>当前关联的棋子ID</summary>
    public int CurrentChessId => m_CurrentChessId;

    public override SlotContainerType ContainerType => SlotContainerType.Chess;

    #region 初始化

    public void SetChessId(int chessId)
    {
        m_CurrentChessId = chessId;
    }

    public void SetEquipSlotData(InventorySlot[] slotData)
    {
        m_EquipSlotData = slotData;
    }

    public void SetDetailInfoUI(DetailInfoUI detailInfoUI)
    {
        m_DetailInfoUI = detailInfoUI;
    }

    #endregion

    #region ISlotContainer 实现

    public override InventorySlot GetSlot(int slotIndex)
    {
        if (m_EquipSlotData == null || slotIndex < 0 || slotIndex >= m_EquipSlotData.Length)
            return null;

        return m_EquipSlotData[slotIndex];
    }

    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        // 只接受来自装备栏（Equip）的拖入
        return otherContainerType == SlotContainerType.Equip;
    }

    protected override bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        // Chess 槽不支持拖出
        DebugEx.Warning("ChessSlotContainer", "棋子装备槽不支持拖拽移出，请使用右键卸下");
        return false;
    }

    #endregion
}
