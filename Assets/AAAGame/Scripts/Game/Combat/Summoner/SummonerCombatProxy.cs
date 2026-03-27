using UnityEngine;

/// <summary>
/// 召唤师战斗代理
/// 挂载在玩家角色 GameObject 上，作为召唤师在战斗中的实体身份。
/// 持有阵营（Camp=0，玩家方），可被敌人 AI 选为攻击目标。
/// 伤害由 ChessAttribute 处理，本类负责同步 IsDead 并将 HP 变化回写到 SummonerRuntimeDataManager（供 UI 使用）。
/// </summary>
public class SummonerCombatProxy : MonoBehaviour
{
    #region 属性

    /// <summary>阵营（0=玩家方）</summary>
    public int Camp => 0;

    /// <summary>是否已死亡（ChessAttribute.HP 归零后为 true）</summary>
    public bool IsDead { get; private set; }

    #endregion

    #region 战斗接口

    /// <summary>
    /// 重置死亡状态（每场战斗开始时由 CombatManager 调用）
    /// </summary>
    public void ResetDeadState()
    {
        IsDead = false;
    }

    /// <summary>
    /// 绑定 ChessAttribute 事件（战斗开始时由 CombatManager 调用）
    /// ChessAttribute 负责实际伤害计算；本方法监听结果并同步到 SummonerRuntimeDataManager（供 CombatUI 使用）
    /// </summary>
    public void BindAttribute(ChessAttribute attribute)
    {
        if (attribute == null) return;
        attribute.OnHpChanged += OnAttributeHpChanged;
    }

    /// <summary>
    /// 解绑 ChessAttribute 事件（战斗结束时由 CombatManager 调用）
    /// </summary>
    public void UnbindAttribute(ChessAttribute attribute)
    {
        if (attribute == null) return;
        attribute.OnHpChanged -= OnAttributeHpChanged;
    }

    #endregion

    #region 私有方法

    private void OnAttributeHpChanged(double oldHp, double newHp)
    {
        // 同步到 SummonerRuntimeDataManager，让 CombatUI.varHPSlider 收到事件刷新
        SummonerRuntimeDataManager.Instance?.SetHP((float)newHp);

        if (newHp <= 0 && !IsDead)
        {
            IsDead = true;
            DebugEx.WarningModule("SummonerCombatProxy", "召唤师已死亡");
        }
    }

    #endregion
}
