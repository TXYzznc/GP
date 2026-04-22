using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 召唤师死亡处理器
/// 监控玩家的腐蚀度，当腐蚀度达到100%且持续3秒时触发死亡结算
/// 支持通过物品（复活卡、护盾等）规避死亡
/// </summary>
public class SummonerDeathHandler : MonoBehaviour
{
    #region 字段

    /// <summary>腐蚀度阈值百分比（>=此值触发倒计时）</summary>
    [SerializeField]
    private float m_CorruptionThreshold = 1.0f; // 100%

    /// <summary>死亡确认倒计时（秒）</summary>
    [SerializeField]
    private float m_DeathConfirmationDelay = 3.0f;

    /// <summary>当前倒计时</summary>
    private float m_CurrentDeathCountdown = 0f;

    /// <summary>是否处于死亡倒计时状态</summary>
    private bool m_IsCountingDownToDeath = false;

    #endregion

    #region 生命周期

    private void Update()
    {
        if (!IsGameRunning())
            return;

        MonitorCorruptionAndTriggerDeath();
    }

    #endregion

    #region 腐蚀度监控和死亡触发

    /// <summary>监控腐蚀度并在条件满足时触发死亡</summary>
    private void MonitorCorruptionAndTriggerDeath()
    {
        var runtimeDataManager = PlayerRuntimeDataManager.Instance;
        if (runtimeDataManager == null)
            return;

        float currentCorruptionPercent = runtimeDataManager.CorruptionPercent;

        // 检查腐蚀度是否达到阈值
        if (currentCorruptionPercent >= m_CorruptionThreshold)
        {
            // 进入死亡倒计时
            if (!m_IsCountingDownToDeath)
            {
                DebugEx.LogModule("SummonerDeathHandler", "腐蚀度达到100%，开始3秒倒计时");
                m_IsCountingDownToDeath = true;
                m_CurrentDeathCountdown = 0f;
            }

            // 更新倒计时
            m_CurrentDeathCountdown += Time.deltaTime;

            // 在倒计时期间检查是否有物品可以救命
            if (m_CurrentDeathCountdown >= m_DeathConfirmationDelay)
            {
                // 倒计时完成，开始最终检查
                TriggerDeathIfNoProtection();
            }
        }
        else
        {
            // 腐蚀度降低到阈值以下，重置倒计时
            if (m_IsCountingDownToDeath)
            {
                DebugEx.LogModule("SummonerDeathHandler", "腐蚀度降低到100%以下，重置死亡倒计时");
                m_IsCountingDownToDeath = false;
                m_CurrentDeathCountdown = 0f;
            }
        }
    }

    /// <summary>触发死亡，如果没有救命物品则进行结算</summary>
    private void TriggerDeathIfNoProtection()
    {
        DebugEx.LogModule("SummonerDeathHandler", "死亡倒计时完成，检查救命物品");

        // 重置倒计时状态，防止重复触发
        m_IsCountingDownToDeath = false;
        m_CurrentDeathCountdown = 0f;

        // 检查复活卡
        if (CheckAndConsumeResurrectionItem())
        {
            DebugEx.LogModule("SummonerDeathHandler", "使用复活卡，规避死亡");
            return;
        }

        // 检查死亡护盾
        if (CheckDeathShield())
        {
            DebugEx.LogModule("SummonerDeathHandler", "触发死亡护盾，规避死亡");
            return;
        }

        // 没有救命物品，触发完全死亡
        DebugEx.LogModule("SummonerDeathHandler", "没有救命物品，触发完全死亡结算");
        TriggerCompleteDeath();
    }

    /// <summary>检查并消耗复活卡</summary>
    private bool CheckAndConsumeResurrectionItem()
    {
        // TODO: 与背包系统集成，检查是否有复活卡
        // 如果有，消耗一张并重置腐蚀度
        // 暂时返回 false（没有复活卡）

        return false;
    }

    /// <summary>检查死亡护盾效果</summary>
    private bool CheckDeathShield()
    {
        // TODO: 检查玩家是否有活跃的死亡护盾效果
        // 例如：特定Buff、装备效果等
        // 暂时返回 false（没有护盾）

        return false;
    }

    /// <summary>触发完全死亡，进行结算</summary>
    private void TriggerCompleteDeath()
    {
        DebugEx.LogModule("SummonerDeathHandler", "触发死亡结算");

        // 异步调用结算流程，使用 Forget() 因为 MonoBehaviour 的 Update 不支持 async
        SettlementManager.Instance.TriggerSettlementAsync("BaseScene", SettlementTriggerSource.Death).Forget();
    }

    /// <summary>检查游戏是否仍在运行</summary>
    private bool IsGameRunning()
    {
        // 简单检查：玩家对象存活且不在菜单
        return gameObject.activeInHierarchy && Time.timeScale > 0;
    }

    #endregion
}
