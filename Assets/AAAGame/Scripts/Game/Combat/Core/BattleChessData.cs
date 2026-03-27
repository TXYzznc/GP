using System.Collections.Generic;

/// <summary>
/// 战斗场景中的棋子临时数据
/// 生命周期：战斗开始时创建，战斗结束时销毁
/// 仅作为数据记录使用，实际运行逻辑仍由 ChessAttribute / BuffManager 驱动
///
/// 职责：
/// - 记录战斗开始时从全局状态同步的初始血量
/// - 跟踪当前战斗中已激活的 Buff ID 列表（镜像 BuffManager 数据）
/// - 战斗结束时作为数据载体，将最终血量写回全局状态
/// </summary>
public class BattleChessData
{
    #region 字段

    /// <summary>棋子ID</summary>
    public int ChessId { get; }

    /// <summary>战斗中当前血量（从 ChessAttribute 同步）</summary>
    public double CurrentHp { get; set; }

    /// <summary>最大血量</summary>
    public double MaxHp { get; }

    /// <summary>阵营（0=玩家，1=敌方）</summary>
    public int Camp { get; set; }

    /// <summary>敌方棋子在 EnemyChessDataManager 中的 key（Camp=1 时有效）</summary>
    public string EnemyKey { get; set; }

    /// <summary>当前激活的 Buff ID 列表（战斗结束时需清除）</summary>
    public List<int> ActiveBuffIds { get; } = new();

    #endregion

    #region 属性

    /// <summary>是否已死亡（血量 &lt;= 0）</summary>
    public bool IsDead => CurrentHp <= 0;

    /// <summary>是否可以通过道具/技能恢复血量（存活且血量不满）</summary>
    public bool CanRecover => !IsDead && CurrentHp < MaxHp;

    #endregion

    #region 构造

    /// <summary>
    /// 从全局棋子状态创建战斗数据副本
    /// 战斗开始时调用，Buff 列表初始化为空
    /// </summary>
    public static BattleChessData FromGlobalState(GlobalChessState globalState)
    {
        if (globalState == null)
        {
            DebugEx.ErrorModule("BattleChessData", "FromGlobalState: globalState 为 null");
            return null;
        }

        return new BattleChessData(globalState.ChessId, globalState.CurrentHp, globalState.MaxHp);
    }

    public BattleChessData(int chessId, double currentHp, double maxHp)
    {
        ChessId = chessId;
        CurrentHp = currentHp;
        MaxHp = maxHp;
    }

    #endregion

    #region 调试

    public override string ToString()
    {
        return $"[BattleChessData] ChessId={ChessId} HP={CurrentHp:F0}/{MaxHp:F0} "
             + $"Dead={IsDead} Buffs={ActiveBuffIds.Count}";
    }

    #endregion
}
