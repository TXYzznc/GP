using UnityEngine;

public class ChangeStateUIProfile : MonoBehaviour, IChessStateUIProfile
{
    [SerializeField] private bool m_ShowHp = true;
    [SerializeField] private bool m_ShowMp = true;
    [SerializeField] private bool m_ShowOther = true;
    [SerializeField] private Vector3 m_FollowOffset = Vector3.zero;

    public void Apply(SummonChessStateUI ui, ChessEntity owner)
    {
        if (ui == null) return;

        ui.SetBarsVisible(m_ShowHp, m_ShowMp, m_ShowOther);

        Vector3 offset = m_FollowOffset;
        if (owner != null)
        {
            Vector3 topPos = EntityPositionHelper.GetTopPosition(owner, false);
            offset += topPos - owner.transform.position;
        }

        ui.SetFollowTarget(owner != null ? owner.transform : null, offset);
        ui.SetBillboard(true);
    }
}