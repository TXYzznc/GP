using Cysharp.Threading.Tasks;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 战斗脱战系统
/// 管理玩家在战斗中尝试脱战的逻辑
/// 包括成功率计算、成本和惩罚
/// </summary>
public class CombatEscapeSystem : SingletonBase<CombatEscapeSystem>
{
    #region 私有字段

    /// <summary>当前战斗中的敌人实体</summary>
    private EnemyEntity m_CurrentEnemy;

    /// <summary>当前战斗回合数</summary>
    private int m_CurrentTurn;

    /// <summary>脱战冷却回合数（0表示无冷却）</summary>
    private int m_EscapeFailCooldown;

    /// <summary>脱战规则表</summary>
    private EscapeRuleTable m_CurrentRule;

    #endregion

    #region 属性

    /// <summary>是否在脱战冷却中</summary>
    public bool IsOnCooldown => m_EscapeFailCooldown > 0;

    /// <summary>剩余冷却回合数</summary>
    public int CooldownRemaining => m_EscapeFailCooldown;

    /// <summary>当前战斗回合数</summary>
    public int CurrentTurn => m_CurrentTurn;

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化脱战系统
    /// 在战斗开始时调用
    /// </summary>
    public void Initialize(EnemyEntity enemy)
    {
        m_CurrentEnemy = enemy;
        m_CurrentTurn = 0;
        m_EscapeFailCooldown = 0;
        m_CurrentRule = null;

        LoadEscapeRule();

        DebugEx.LogModule("CombatEscapeSystem", $"脱战系统已初始化，敌人: {enemy?.Config?.Name}");
    }

    /// <summary>
    /// 清除系统状态
    /// 在战斗结束时调用
    /// </summary>
    public void Clear()
    {
        m_CurrentEnemy = null;
        m_CurrentTurn = 0;
        m_EscapeFailCooldown = 0;
        m_CurrentRule = null;

        DebugEx.LogModule("CombatEscapeSystem", "脱战系统已清除");
    }

    /// <summary>
    /// 回合开始时调用
    /// 更新冷却和回合计数
    /// </summary>
    public void OnTurnStart()
    {
        m_CurrentTurn++;

        if (IsOnCooldown)
        {
            m_EscapeFailCooldown--;
            DebugEx.LogModule(
                "CombatEscapeSystem",
                $"脱战冷却减少，剩余: {m_EscapeFailCooldown}回合"
            );
        }
    }

    /// <summary>
    /// 计算当前脱战成功率
    /// 返回值范围：0-1
    /// </summary>
    public float CalculateSuccessRate()
    {
        if (m_CurrentRule == null)
        {
            LoadEscapeRule();
            if (m_CurrentRule == null)
            {
                DebugEx.WarningModule("CombatEscapeSystem", "脱战规则未找到，返回默认成功率50%");
                return 0.5f;
            }
        }

        // 基础成功率
        float rate = (float)m_CurrentRule.BaseSuccessRate;

        // 添加随回合数的成功率增长
        rate += m_CurrentTurn * (float)m_CurrentRule.TimeBonus;

        // 限制最大成功率
        rate = Mathf.Min(rate, (float)m_CurrentRule.MaxSuccessRate);

        // 冷却期间成功率减半
        if (IsOnCooldown)
        {
            rate *= 0.5f;
        }

        return rate;
    }

    /// <summary>
    /// 尝试脱战
    /// </summary>
    public async UniTask<bool> AttemptEscape()
    {
        if (IsOnCooldown)
        {
            DebugEx.WarningModule("CombatEscapeSystem", "脱战在冷却中，无法尝试");
            return false;
        }

        float successRate = CalculateSuccessRate();
        bool success = Random.value <= successRate;

        if (success)
        {
            await OnEscapeSuccess();
        }
        else
        {
            await OnEscapeFail();
        }

        return success;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 加载脱战规则
    /// 根据当前敌人类型获取对应的规则
    /// </summary>
    private void LoadEscapeRule()
    {
        if (m_CurrentEnemy == null)
        {
            DebugEx.WarningModule("CombatEscapeSystem", "当前敌人为空，无法加载脱战规则");
            return;
        }

        // TODO: 实现敌人类型到脱战规则的映射
        // 当前方案：根据敌人配置的Type字段匹配到对应的EscapeRule
        // 需要EscapeRuleTable.xlsx中添加敌人类型匹配

        var escapeRuleTable = GF.DataTable.GetDataTable<EscapeRuleTable>();
        if (escapeRuleTable == null)
        {
            DebugEx.WarningModule("CombatEscapeSystem", "EscapeRuleTable未加载");
            return;
        }

        // 暂时使用规则ID 1作为默认规则
        m_CurrentRule = escapeRuleTable.GetDataRow(1);

        if (m_CurrentRule != null)
        {
            DebugEx.LogModule(
                "CombatEscapeSystem",
                $"已加载脱战规则: 基础成功率={m_CurrentRule.BaseSuccessRate:P0}"
            );
        }
    }

    /// <summary>
    /// 脱战成功
    /// </summary>
    private async UniTask OnEscapeSuccess()
    {
        if (m_CurrentRule == null)
            return;

        DebugEx.LogModule(
            "CombatEscapeSystem",
            $"脱战成功！消耗污染值: {m_CurrentRule.CorruptionCost}"
        );

        // 增加污染值
        if (PlayerRuntimeDataManager.Instance != null)
        {
            PlayerRuntimeDataManager.Instance.AddCorruption(m_CurrentRule.CorruptionCost);
        }

        // 显示结果UI
        await ShowEscapeResult(true);

        // 退出战斗，返回探索
        ExitCombat();
    }

    /// <summary>
    /// 脱战失败
    /// </summary>
    private async UniTask OnEscapeFail()
    {
        if (m_CurrentRule == null)
            return;

        DebugEx.LogModule(
            "CombatEscapeSystem",
            $"脱战失败！召唤师生命损失: {m_CurrentRule.HealthLossPenalty:P0}, 冷却: {m_CurrentRule.CooldownTurns}回合"
        );

        // 设置冷却
        m_EscapeFailCooldown = m_CurrentRule.CooldownTurns;

        // 显示结果UI
        await ShowEscapeResult(false);
    }

    /// <summary>
    /// 显示脱战结果UI
    /// </summary>
    private async UniTask ShowEscapeResult(bool success)
    {
        // 创建结果数据
        EscapeResultData resultData = new EscapeResultData
        {
            Success = success,
            CorruptionCost = success ? m_CurrentRule.CorruptionCost : 0,
            HealthLoss = success ? 0 : (float)m_CurrentRule.HealthLossPenalty,
            CooldownTurns = success ? 0 : m_CurrentRule.CooldownTurns,
        };

        // 打开EscapeResultUI并等待关闭
        UIParams uiParams = UIParams.Create();
        uiParams.Set("EscapeResultData", resultData);

        try
        {
            // 使用字符串形式打开UI
            await GF.UI.OpenUIFormAwait(
                "EscapeResultUI",
                "Dialog",
                priority: 50,
                pauseCoveredUIForm: true,
                userData: uiParams
            );
        }
        catch (System.Exception ex)
        {
            DebugEx.ErrorModule("CombatEscapeSystem", $"打开EscapeResultUI失败: {ex.Message}");
        }

        DebugEx.LogModule(
            "CombatEscapeSystem",
            success ? "显示脱战成功结果UI" : "显示脱战失败结果UI"
        );
    }

    /// <summary>
    /// 退出战斗
    /// </summary>
    private void ExitCombat()
    {
        DebugEx.LogModule("CombatEscapeSystem", "脱战成功，准备返回探索");

        // 清除脱战系统状态
        Clear();

        // 清除战斗触发上下文
        if (CombatTriggerManager.Instance != null)
        {
            CombatTriggerManager.Instance.ClearContext();
            DebugEx.LogModule("CombatEscapeSystem", "战斗触发上下文已清除");
        }

        // 关键：不需要手动转移状态，由于将不再满足继续战斗的条件，
        // CombatManager 会自动检测到并通过 CombatEndEventArgs 通知系统
        // 或者玩家可以在 EscapeResultUI 的确认按钮回调中主动切换场景
    }

    #endregion
}
