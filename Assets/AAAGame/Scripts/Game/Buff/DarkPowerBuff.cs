using UnityEngine;

/// <summary>
/// 黑暗神力 Buff（ID=21）
/// 所有攻击附带腐蚀效果
/// </summary>
public class DarkPowerBuff : BuffBase
{
    private const int CORROSION_BUFF_ID = 7; // 腐蚀 Buff ID
    private ChessAttribute m_OwnerAttr;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);
        m_OwnerAttr = ctx?.OwnerAttribute;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        if (m_OwnerAttr != null)
            m_OwnerAttr.OnDamageDealt += OnDamageDealt;
    }

    public override void OnExit()
    {
        if (m_OwnerAttr != null)
            m_OwnerAttr.OnDamageDealt -= OnDamageDealt;
        base.OnExit();
    }

    private void OnDamageDealt(double damage, ChessAttribute target)
    {
        if (damage <= 0 || target == null) return;

        // 对被击中的目标应用腐蚀 Buff
        var targetEntity = target.GetComponent<ChessEntity>();
        if (targetEntity != null)
        {
            BuffApplyHelper.ApplyBuff(CORROSION_BUFF_ID, targetEntity.gameObject, false, m_OwnerAttr.gameObject);
        }
    }
}
