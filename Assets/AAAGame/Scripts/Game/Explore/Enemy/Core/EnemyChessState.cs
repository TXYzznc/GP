/// <summary>
/// 敌人棋子实例的持久化数据
/// key 格式："{entityGuid}_{slotIndex}"
/// </summary>
public class EnemyChessState
{
    /// <summary>棋子配置ID</summary>
    public int ChessId { get; set; }

    /// <summary>当前血量</summary>
    public double CurrentHp { get; set; }

    /// <summary>最大血量</summary>
    public double MaxHp { get; set; }

    /// <summary>是否已死亡</summary>
    public bool IsDead => CurrentHp <= 0;

    public EnemyChessState(int chessId, double maxHp)
    {
        ChessId = chessId;
        MaxHp = maxHp;
        CurrentHp = maxHp;
    }
}
