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
        if (deadChess == null || deadChess.Camp != 1)
        {
            return;
        }

        int expReward = GetEXPReward(deadChess.Rank);
        if (expReward <= 0)
        {
            return;
        }

        // 给所有存活的玩家棋子加经验
        var allChess = SummonChessManager.Instance?.GetAllChess();
        if (allChess == null)
        {
            DebugEx.Error("ChessEXPManager", $"无法获取棋子列表，经验分配失败");
            return;
        }

        int successCount = 0;
        foreach (var chess in allChess)
        {
            if (chess == null || chess.Camp != 0)
            {
                continue;
            }

            // 通过 GlobalChessManager 添加经验（所有经验处理统一在全局管理器中）
            GlobalChessManager.Instance.AddChessExperience(chess.ChessId, expReward);

            // 同步到本地的 ChessEXPComponent（用于 UI 显示）
            var expComp = chess.GetComponent<ChessEXPComponent>();
            if (expComp != null)
            {
                var globalState = GlobalChessManager.Instance.GetChessState(chess.ChessId);
                if (globalState != null)
                {
                    expComp.SetEXP(globalState.Experience);
                }
            }

            successCount++;
        }

        DebugEx.Success("ChessEXPManager", $"击败敌方棋子 [{deadChess.Config?.Name}]，{successCount} 个玩家棋子获得 +{expReward} EXP");
    }

    private int GetEXPReward(int enemyRank)
    {
        // 查经验规则表：RuleType=2（击败棋子），EnemyRank 匹配
        var ruleTable = GF.DataTable.GetDataTable<ChessEXPRuleTable>();
        if (ruleTable == null)
        {
            DebugEx.Error("ChessEXPManager", $"ChessEXPRuleTable 数据表未加载");
            return 0;
        }

        var ruleRow = ruleTable.GetDataRow(row => row.RuleType == 2 && row.EnemyRank == enemyRank);
        if (ruleRow == null)
        {
            DebugEx.Error("ChessEXPManager", $"未找到敌方棋子等级 {enemyRank} 的经验规则");
            return 0;
        }

        return ruleRow.EXPReward;
    }
}
