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
        DebugEx.Log("ChessEXPManager", $"[1] OnChessDied 被调用: deadChess={deadChess?.Config?.Name ?? "null"}, Camp={deadChess?.Camp}, Rank={deadChess?.Rank}");

        // 只处理敌方棋子死亡（Camp=1）
        if (deadChess == null || deadChess.Camp != 1)
        {
            DebugEx.Log("ChessEXPManager", $"[2] ❌ 不是敌方棋子或为null，跳过");
            return;
        }

        int expReward = GetEXPReward(deadChess.Rank);
        DebugEx.Log("ChessEXPManager", $"[3] 获取经验奖励: {expReward}");
        if (expReward <= 0)
        {
            DebugEx.Log("ChessEXPManager", $"[4] ❌ 经验奖励为0或负数，跳过");
            return;
        }

        // 给所有存活的玩家棋子加经验
        var allChess = SummonChessManager.Instance?.GetAllChess();
        if (allChess == null)
        {
            DebugEx.Error("ChessEXPManager", $"[5] ❌ SummonChessManager.Instance?.GetAllChess() 返回null");
            return;
        }

        DebugEx.Log("ChessEXPManager", $"[6] 获取所有棋子数量: {allChess.Count}");

        int successCount = 0;
        foreach (var chess in allChess)
        {
            if (chess == null || chess.Camp != 0)
            {
                DebugEx.Log("ChessEXPManager", $"[7] 跳过棋子: chess={chess?.Config?.Name ?? "null"}, Camp={chess?.Camp}");
                continue;
            }

            var expComp = chess.GetComponent<ChessEXPComponent>();
            if (expComp == null)
            {
                DebugEx.Error("ChessEXPManager", $"[8] ❌ 棋子 {chess.Config.Name} 没有 ChessEXPComponent 组件");
                continue;
            }

            DebugEx.Log("ChessEXPManager", $"[9] ✅ 给棋子 {chess.Config.Name} 加经验: {expReward}");
            expComp.AddEXP(expReward);
            successCount++;
        }

        DebugEx.Log("ChessEXPManager", $"[10] 完成: 击败敌方棋子 [{deadChess.ChessId}]，成功给 {successCount} 个玩家棋子 +{expReward} EXP");
    }

    private int GetEXPReward(int enemyRank)
    {
        DebugEx.Log("ChessEXPManager", $"[GetEXPReward] 开始查询敌方棋子(Rank={enemyRank})的经验奖励");

        // 查经验规则表：RuleType=2（击败棋子），EnemyRank 匹配
        var ruleTable = GF.DataTable.GetDataTable<ChessEXPRuleTable>();
        if (ruleTable == null)
        {
            DebugEx.Error("ChessEXPManager", $"[GetEXPReward] ❌ ChessEXPRuleTable 未加载");
            return 0;
        }

        var ruleRow = ruleTable.GetDataRow(row => row.RuleType == 2 && row.EnemyRank == enemyRank);
        if (ruleRow == null)
        {
            DebugEx.Error("ChessEXPManager", $"[GetEXPReward] ❌ 在 ChessEXPRuleTable 中未找到匹配规则: RuleType=2, EnemyRank={enemyRank}");
            return 0;
        }

        DebugEx.Log("ChessEXPManager", $"[GetEXPReward] ✅ 找到规则，经验奖励: {ruleRow.EXPReward}");
        return ruleRow.EXPReward;
    }
}
