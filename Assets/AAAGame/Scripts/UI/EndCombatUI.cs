using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class EndCombatUI : UIFormBase
{
    public const string P_IsVictory = "IsVictory";

    private readonly List<GameObject> m_SpawnedAwardItems = new List<GameObject>(8);
    private readonly List<int> m_PendingAwardItemIds = new List<int>(8);
    private CancellationTokenSource m_Cts;
    private bool m_IsVictory;
    private bool m_TreasureOpened;
    private float m_OpenTime;
    private int m_OpenFrame;
    private float m_TreasureClickTime;
    private int m_TreasureClickFrame;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        m_IsVictory = Params.Get<VarBoolean>(P_IsVictory, true);
        m_TreasureOpened = false;
        m_OpenTime = Time.time;
        m_OpenFrame = Time.frameCount;
        DebugEx.LogModule("EndCombatUI", $"OnOpen isVictory={m_IsVictory} t={Time.time:F3} f={Time.frameCount}");

        if (varVector != null) varVector.SetActive(m_IsVictory);
        if (varDefeat != null) varDefeat.SetActive(!m_IsVictory);

        if (varItemAwardBtn != null)
        {
            varItemAwardBtn.onClick.RemoveListener(OnClickItemAwardBtn);
            varItemAwardBtn.onClick.AddListener(OnClickItemAwardBtn);
            varItemAwardBtn.gameObject.SetActive(false);
        }

        if (varTreasure != null)
        {
            varTreasure.onClick.RemoveListener(OnClickTreasure);
            varTreasure.onClick.AddListener(OnClickTreasure);
            varTreasure.gameObject.SetActive(false);
            varTreasure.interactable = true;
        }

        CleanupAwardItems();
        m_PendingAwardItemIds.Clear();

        m_Cts?.Cancel();
        m_Cts?.Dispose();
        m_Cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        if (m_IsVictory)
        {
            DelayShowTreasureAsync(m_Cts.Token).Forget();
        }
        else
        {
            DelayReturnToExplorationAsync(m_Cts.Token).Forget();
        }
    }

    private async UniTaskVoid DelayShowTreasureAsync(CancellationToken token)
    {
        var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
        float delaySeconds = ruleRow != null ? ruleRow.VictoryTreasureDelaySeconds : 0.3f;
        await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);
        if (varTreasure != null)
        {
            varTreasure.gameObject.SetActive(true);
            DebugEx.LogModule("EndCombatUI", $"Treasure show t={Time.time:F3} f={Time.frameCount} dt={(Time.time - m_OpenTime):F3} df={(Time.frameCount - m_OpenFrame)}");
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        m_Cts?.Cancel();
        m_Cts?.Dispose();
        m_Cts = null;
        GF.UI.CloseAllFloatingTips();
        CleanupAwardItems();
        base.OnClose(isShutdown, userData);
    }

    private void CleanupAwardItems()
    {
        for (int i = 0; i < m_SpawnedAwardItems.Count; i++)
        {
            if (m_SpawnedAwardItems[i] != null)
            {
                Destroy(m_SpawnedAwardItems[i]);
            }
        }
        m_SpawnedAwardItems.Clear();
    }

    private async UniTaskVoid GenerateItemAwardsAsync(CancellationToken token)
    {
        float startTime = Time.time;
        int startFrame = Time.frameCount;
        string dtClick = m_TreasureClickTime > 0f ? (startTime - m_TreasureClickTime).ToString("F3") : "n/a";
        DebugEx.LogModule("EndCombatUI", $"GenerateItemAwards start t={startTime:F3} f={startFrame} dtClick={dtClick}");

        if (varAwardShowPanel == null || varAwardItemUI == null)
        {
            return;
        }

        // ✅ 不隐藏模板预制体，而是在Instantiate后管理实例状态

        var table = GF.DataTable.GetDataTable<ItemTable>();
        if (table == null)
        {
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length <= 0)
        {
            return;
        }

        var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
        int minAwardCount = ruleRow != null ? ruleRow.MinItemAwardCount : 2;
        int maxAwardCount = ruleRow != null ? ruleRow.MaxItemAwardCount : 6;
        int min = Mathf.Max(0, minAwardCount);
        int max = Mathf.Max(min, maxAwardCount);
        int awardCount = UnityEngine.Random.Range(min, max + 1);
        DebugEx.LogModule("EndCombatUI", $"GenerateItemAwards count={awardCount} rows={allRows.Length} t={Time.time:F3} f={Time.frameCount}");

        for (int i = 0; i < awardCount; i++)
        {
            token.ThrowIfCancellationRequested();

            var row = allRows[UnityEngine.Random.Range(0, allRows.Length)];
            if (row == null)
            {
                continue;
            }

            var go = Instantiate(varAwardItemUI, varAwardShowPanel.transform);
            go.SetActive(true);
            m_SpawnedAwardItems.Add(go);
            m_PendingAwardItemIds.Add(row.Id);

            if (go.TryGetComponent<AwardItemUI>(out var awardUI))
            {
                awardUI.SetData(row.Id);
            }

            go.transform.localScale = Vector3.one * 1.2f;
            go.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

            if (i == 0)
            {
                DebugEx.LogModule("EndCombatUI", $"First award spawned itemId={row.Id} t={Time.time:F3} f={Time.frameCount} dt={(Time.time - startTime):F3} df={(Time.frameCount - startFrame)}");
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.15f), cancellationToken: token);
        }

        if (varItemAwardBtn != null)
        {
            varItemAwardBtn.gameObject.SetActive(true);
            DebugEx.LogModule("EndCombatUI", $"ItemAwardBtn show t={Time.time:F3} f={Time.frameCount} totalDt={(Time.time - startTime):F3} totalDf={(Time.frameCount - startFrame)}");
        }
    }

    private async UniTaskVoid DelayReturnToExplorationAsync(CancellationToken token)
    {
        if (varItemAwardBtn != null)
        {
            varItemAwardBtn.gameObject.SetActive(false);
        }

        var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
        float delaySeconds = ruleRow != null ? ruleRow.DefeatAutoReturnDelaySeconds : 1f;
        await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);

        CloseWithAnimation();
        GameStateManager.Instance.SwitchToExploration();
    }

    private void OnClickTreasure()
    {
        if (!m_IsVictory || m_TreasureOpened)
        {
            return;
        }

        m_TreasureOpened = true;
        GF.UI.CloseAllFloatingTips();
        m_TreasureClickTime = Time.time;
        m_TreasureClickFrame = Time.frameCount;
        DebugEx.LogModule("EndCombatUI", $"Treasure click t={m_TreasureClickTime:F3} f={m_TreasureClickFrame}");

        if (varTreasure != null)
        {
            varTreasure.interactable = false;
        }

        CleanupAwardItems();
        m_PendingAwardItemIds.Clear();
        if (m_Cts != null)
        {
            PlayTreasureAnimationAndSpawnAwardsAsync(m_Cts.Token).Forget();
        }
    }

    private async UniTaskVoid PlayTreasureAnimationAndSpawnAwardsAsync(CancellationToken token)
    {
        if (varTreasure == null) return;

        bool hasAnimationCompleted = false;
        bool hasAwardsStarted = false;

        var eventDispatcher = varTreasure.GetComponent<UIAnimationEventDispatcher>();
        var animator = varTreasure.GetComponent<Animator>();
        DebugEx.LogModule("EndCombatUI", $"Treasure anim start hasDispatcher={(eventDispatcher != null)} hasAnimator={(animator != null)} t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");

        Action<string> onEventTriggered = (eventName) =>
        {
            DebugEx.LogModule("EndCombatUI", $"Treasure anim event={eventName} t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
            if (eventName == "SpawnAwards" || eventName == "Open")
            {
                if (!hasAwardsStarted)
                {
                    hasAwardsStarted = true;
                    DebugEx.LogModule("EndCombatUI", $"SpawnAwards start awards t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
                    GenerateItemAwardsAsync(token).Forget();
                }
            }
        };

        Action<string> onAnimComplete = (animName) =>
        {
            hasAnimationCompleted = true;
            DebugEx.LogModule("EndCombatUI", $"Treasure anim complete={animName} t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
            if (!hasAwardsStarted)
            {
                hasAwardsStarted = true;
                DebugEx.LogModule("EndCombatUI", $"AnimComplete start awards t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
                GenerateItemAwardsAsync(token).Forget();
            }
        };

        if (eventDispatcher != null)
        {
            eventDispatcher.OnAnimationEventTriggered += onEventTriggered;
            eventDispatcher.OnAnimationComplete += onAnimComplete;
        }

        if (animator != null)
        {
            animator.SetTrigger("Open");
            DebugEx.LogModule("EndCombatUI", $"Animator.SetTrigger(Open) t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
        }
        else
        {
            DebugEx.Warning("EndCombatUI", "Treasure 上没有 Animator 或动画事件，将使用默认延时。");
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
            if (!hasAwardsStarted)
            {
                hasAwardsStarted = true;
                DebugEx.LogModule("EndCombatUI", $"Fallback start awards t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
                GenerateItemAwardsAsync(token).Forget();
            }
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
            hasAnimationCompleted = true;
        }

        float waitTime = 0f;
        if (eventDispatcher != null)
        {
            while (!hasAwardsStarted && waitTime < 2f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                waitTime += Time.deltaTime;
            }
            if (!hasAwardsStarted)
            {
                hasAwardsStarted = true;
                GenerateItemAwardsAsync(token).Forget();
            }
        }

        waitTime = 0f;
        while (!hasAnimationCompleted && waitTime < 2f)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            waitTime += Time.deltaTime;
        }

        if (eventDispatcher != null)
        {
            eventDispatcher.OnAnimationEventTriggered -= onEventTriggered;
            eventDispatcher.OnAnimationComplete -= onAnimComplete;
        }

        varTreasure.gameObject.SetActive(false);
        DebugEx.LogModule("EndCombatUI", $"Treasure hide t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
    }

    private void OnClickItemAwardBtn()
    {
        GF.UI.CloseAllFloatingTips();

        // 将所有奖励物品加入背包
        if (InventoryManager.Instance != null)
        {
            foreach (int itemId in m_PendingAwardItemIds)
            {
                InventoryManager.Instance.AddItem(itemId, 1);
            }
            DebugEx.LogModule("EndCombatUI", $"奖励已入背包，共 {m_PendingAwardItemIds.Count} 件");
        }
        m_PendingAwardItemIds.Clear();

        CloseWithAnimation();
        GameStateManager.Instance.SwitchToExploration();
    }
}
