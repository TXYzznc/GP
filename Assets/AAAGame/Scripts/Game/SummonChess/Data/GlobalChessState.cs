/// <summary>
/// 全局棋子状态数据
/// 存储棋子在战斗间隙需要持久化的状态（血量等）
/// 不包含：临时状态效果（Buff），临时属性加成
///
/// 数据流：
///   战斗开始 → 从此处读取血量 → ChessAttribute 初始化
///   战斗结束 → ChessAttribute 当前血量 → 写回此处
/// </summary>
public class GlobalChessState
{
    #region 字段

    /// <summary>棋子ID（对应 SummonChessTable.Id）</summary>
    public int ChessId;

    /// <summary>当前血量（战斗间持久化）</summary>
    public double CurrentHp;

    /// <summary>最大血量（来自配置表，乘以成长系数）</summary>
    public double MaxHp;

    /// <summary>局内等级（每次从基地开始游戏时重置为1）</summary>
    public int Level;

    /// <summary>局内经验值（每次从基地开始游戏时重置为0）</summary>
    public int Experience;

    #endregion

    #region 属性

    /// <summary>是否已死亡（血量 &lt;= 0）</summary>
    public bool IsDead => CurrentHp <= 0;

    /// <summary>是否受伤（存活但血量不满）</summary>
    public bool IsInjured => !IsDead && CurrentHp < MaxHp;

    /// <summary>是否满血</summary>
    public bool IsFullHp => CurrentHp >= MaxHp;

    /// <summary>血量百分比（0~1）</summary>
    public float HpPercent => MaxHp > 0 ? (float)(CurrentHp / MaxHp) : 0f;

    #endregion

    #region 构造

    public GlobalChessState() { }

    /// <summary>
    /// 初始化一个满血的全局状态（新棋子或基地恢复后使用）
    /// </summary>
    public GlobalChessState(int chessId, double maxHp)
    {
        ChessId = chessId;
        MaxHp = maxHp;
        CurrentHp = maxHp;
        Level = 1;
        Experience = 0;
    }

    #endregion

    #region 调试

    public override string ToString()
    {
        return $"[GlobalChessState] ChessId={ChessId} HP={CurrentHp:F0}/{MaxHp:F0} "
             + $"Level={Level} Exp={Experience} Dead={IsDead}";
    }

    #endregion
}
