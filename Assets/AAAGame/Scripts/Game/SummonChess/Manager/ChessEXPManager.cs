using UnityEngine;

/// <summary>
/// 棋子经验值管理器
/// 负责：击败敌方棋子 → 查表 → 给所有玩家棋子加经验
/// 由 SummonChessManager 在 Awake 时自动挂载
/// </summary>
public class ChessEXPManager : MonoBehaviour
{
    public static ChessEXPManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        ChessStateEvents.OnChessDied += OnChessDied;
    }

    private void OnDisable()
    {
        ChessStateEvents.OnChessDied -= OnChessDied;
    }

    private void OnChessDied(ChessEntity deadChess)
    {
        // 只处理敌方棋子死亡（Camp=1）
        if (deadChess == null || deadChess.Camp != 1) return;

        int expReward = GetEXPReward(deadChess.ChessId);
        if (expReward <= 0) return;

        // 给所有存活的玩家棋子加经验
        var allChess = SummonChessManager.Instance?.GetAllChess();
        if (allChess == null) return;

        foreach (var chess in allChess)
        {
            if (chess == null || chess.Camp != 0) continue;
            var expComp = chess.GetComponent<ChessEXPComponent>();
            expComp?.AddEXP(expReward);
        }

        DebugEx.LogModule("ChessEXPManager", $"击败敌方棋子 [{deadChess.ChessId}]，所有玩家棋子 +{expReward} EXP");
    }

    private int GetEXPReward(int enemyChessId)
    {
        // 查进阶表获取敌方棋子阶级
        var advTable = GF.DataTable.GetDataTable<ChessAdvanceTable>();
        var advRow = advTable?.GetDataRow(enemyChessId);
        if (advRow == null) return 0;

        int enemyRank = advRow.Rank;

        // 查经验规则表：RuleType=2（击败棋子），EnemyRank 匹配
        var ruleTable = GF.DataTable.GetDataTable<ChessEXPRuleTable>();
        if (ruleTable == null) return 0;

        var ruleRow = ruleTable.GetDataRow(row => row.RuleType == 2 && row.EnemyRank == enemyRank);
        return ruleRow?.EXPReward ?? 0;
    }
}
