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
    public const string P_RewardId = "RewardId";
    public const string P_Difficulty = "Difficulty";

    private readonly List<GameObject> m_SpawnedAwardItems = new List<GameObject>(8);
    private readonly List<int> m_PendingAwardItemIds = new List<int>(8);
    private CancellationTokenSource m_Cts;
    private bool m_IsVictory;
    private bool m_TreasureOpened;
    private float m_OpenTime;
    private int m_OpenFrame;
    private float m_TreasureClickTime;
    private int m_TreasureClickFrame;
    private int m_RewardId;
    private int m_Difficulty;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        m_IsVictory = Params.Get<VarBoolean>(P_IsVictory, true);
        m_RewardId = Params.Get<VarInt32>(P_RewardId, 0);
        m_Difficulty = Params.Get<VarInt32>(P_Difficulty, 0);
        m_TreasureOpened = false;
        m_OpenTime = Time.time;
        m_OpenFrame = Time.frameCount;
        DebugEx.Log(this.GetType().Name, $"OnOpen isVictory={m_IsVictory} t={Time.time:F3} f={Time.frameCount}");

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
            DebugEx.Log(this.GetType().Name, $"Treasure show t={Time.time:F3} f={Time.frameCount} dt={(Time.time - m_OpenTime):F3} df={(Time.frameCount - m_OpenFrame)}");
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
        DebugEx.Log(this.GetType().Name, $"GenerateItemAwards start t={startTime:F3} f={startFrame} dtClick={dtClick}");

        if (varAwardShowPanel == null || varAwardItemUI == null)
        {
            return;
        }

        // 从敌人配置读取 RewardId，加载宝箱配置
        var treasureBoxRow = LoadTreasureBoxConfig();
        if (treasureBoxRow == null)
        {
            return;
        }

        // 读取 RewardMultiplier 并计算实际物品数量
        float rewardMultiplier = GetRewardMultiplier();
        int minAwardCount = Mathf.Max(1, Mathf.RoundToInt(treasureBoxRow.ItemCountMin * rewardMultiplier));
        int maxAwardCount = Mathf.Max(minAwardCount, Mathf.RoundToInt(treasureBoxRow.ItemCountMax * rewardMultiplier));
        int awardCount = UnityEngine.Random.Range(minAwardCount, maxAwardCount + 1);

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

        DebugEx.Log(this.GetType().Name, $"GenerateItemAwards count={awardCount}(min={minAwardCount},max={maxAwardCount}) multiplier={rewardMultiplier:F2} t={Time.time:F3} f={Time.frameCount}");

        var itemGroupWeights = ParseItemGroupWeights(treasureBoxRow?.ItemGroupIds);

        // 根据权重选择 awardCount 个物品组（可重复）
        var selectedGroupIds = (itemGroupWeights != null && itemGroupWeights.Count > 0)
            ? SelectGroupsByWeights(itemGroupWeights, awardCount)
            : null;

        for (int i = 0; i < awardCount; i++)
        {
            token.ThrowIfCancellationRequested();

            ItemTable row = null;

            if (selectedGroupIds != null && i < selectedGroupIds.Count)
            {
                // 从选出的物品组中随机选择一个物品
                row = SelectItemFromGroup(allRows, selectedGroupIds[i]);
            }
            else if (itemGroupWeights == null || itemGroupWeights.Count == 0)
            {
                // 未配置物品组则随机选择
                row = allRows[UnityEngine.Random.Range(0, allRows.Length)];
            }

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
                DebugEx.Log(this.GetType().Name, $"First award spawned itemId={row.Id} t={Time.time:F3} f={Time.frameCount} dt={(Time.time - startTime):F3} df={(Time.frameCount - startFrame)}");
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.15f), cancellationToken: token);
        }

        // 处理金币掉落
        int totalCoins = 0;
        if (UnityEngine.Random.value < treasureBoxRow.CoinsProbability)
        {
            totalCoins = UnityEngine.Random.Range(treasureBoxRow.MinCoins, treasureBoxRow.MaxCoins + 1);
            float rewardMultiplierForCoins = GetRewardMultiplier();
            totalCoins = Mathf.RoundToInt(totalCoins * rewardMultiplierForCoins);

            if (InventoryManager.Instance?.AddItem(InventoryManager.VIRTUAL_ITEM_GOLD, totalCoins) ?? false)
            {
                m_PendingAwardItemIds.Add(InventoryManager.VIRTUAL_ITEM_GOLD);

                // 创建虚拟物品 UI（金币）
                var go = Instantiate(varAwardItemUI, varAwardShowPanel.transform);
                go.SetActive(true);
                m_SpawnedAwardItems.Add(go);

                if (go.TryGetComponent<AwardItemUI>(out var awardUI))
                {
                    awardUI.SetData(InventoryManager.VIRTUAL_ITEM_GOLD);
                }

                go.transform.localScale = Vector3.one * 1.2f;
                go.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

                DebugEx.Log(this.GetType().Name, $"金币掉落: {totalCoins}（倍率: {rewardMultiplierForCoins:F2}）");
            }
        }

        // 处理灵石掉落（连续抽奖机制）
        int totalMagicaStone = treasureBoxRow.MinMagicaStone;
        while (UnityEngine.Random.value < treasureBoxRow.MagicaStoneProbability)
        {
            totalMagicaStone++;
            if (totalMagicaStone >= treasureBoxRow.MaxMagicaStone)
            {
                totalMagicaStone = treasureBoxRow.MaxMagicaStone;
                break;
            }
        }

        if (totalMagicaStone > 0)
        {
            float rewardMultiplierForStone = GetRewardMultiplier();
            int finalMagicaStone = Mathf.RoundToInt(totalMagicaStone * rewardMultiplierForStone);

            if (InventoryManager.Instance?.AddItem(InventoryManager.VIRTUAL_ITEM_SPIRIT_STONE, finalMagicaStone) ?? false)
            {
                m_PendingAwardItemIds.Add(InventoryManager.VIRTUAL_ITEM_SPIRIT_STONE);

                // 创建虚拟物品 UI（灵石）
                var go = Instantiate(varAwardItemUI, varAwardShowPanel.transform);
                go.SetActive(true);
                m_SpawnedAwardItems.Add(go);

                if (go.TryGetComponent<AwardItemUI>(out var awardUI))
                {
                    awardUI.SetData(InventoryManager.VIRTUAL_ITEM_SPIRIT_STONE);
                }

                go.transform.localScale = Vector3.one * 1.2f;
                go.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

                DebugEx.Log(this.GetType().Name, $"灵石掉落: {finalMagicaStone}（倍率: {rewardMultiplierForStone:F2}）");
            }
        }

        DebugEx.Log(this.GetType().Name, $"奖励已入背包，共 {m_PendingAwardItemIds.Count} 件（物品+资源）");

        if (varItemAwardBtn != null)
        {
            varItemAwardBtn.gameObject.SetActive(true);
            DebugEx.Log(this.GetType().Name, $"ItemAwardBtn show t={Time.time:F3} f={Time.frameCount} totalDt={(Time.time - startTime):F3} totalDf={(Time.frameCount - startFrame)}");
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
        DebugEx.Log(this.GetType().Name, $"Treasure click t={m_TreasureClickTime:F3} f={m_TreasureClickFrame}");

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
        DebugEx.Log(this.GetType().Name, $"Treasure anim start hasDispatcher={(eventDispatcher != null)} hasAnimator={(animator != null)} t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");

        Action<string> onEventTriggered = (eventName) =>
        {
            DebugEx.Log(this.GetType().Name, $"Treasure anim event={eventName} t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
            if (eventName == "SpawnAwards" || eventName == "Open")
            {
                if (!hasAwardsStarted)
                {
                    hasAwardsStarted = true;
                    DebugEx.Log(this.GetType().Name, $"SpawnAwards start awards t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
                    GenerateItemAwardsAsync(token).Forget();
                }
            }
        };

        Action<string> onAnimComplete = (animName) =>
        {
            hasAnimationCompleted = true;
            DebugEx.Log(this.GetType().Name, $"Treasure anim complete={animName} t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
            if (!hasAwardsStarted)
            {
                hasAwardsStarted = true;
                DebugEx.Log(this.GetType().Name, $"AnimComplete start awards t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
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
            DebugEx.Log(this.GetType().Name, $"Animator.SetTrigger(Open) t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
        }
        else
        {
            DebugEx.Warning(this.GetType().Name, "Treasure 上没有 Animator 或动画事件，将使用默认延时。");
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
            if (!hasAwardsStarted)
            {
                hasAwardsStarted = true;
                DebugEx.Log(this.GetType().Name, $"Fallback start awards t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
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
        DebugEx.Log(this.GetType().Name, $"Treasure hide t={Time.time:F3} f={Time.frameCount} dtClick={(Time.time - m_TreasureClickTime):F3}");
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
            DebugEx.Log(this.GetType().Name, $"奖励已入背包，共 {m_PendingAwardItemIds.Count} 件");
        }
        m_PendingAwardItemIds.Clear();

        CloseWithAnimation();
        GameStateManager.Instance.SwitchToExploration();
    }

    /// <summary>
    /// 从保存的敌人配置读取宝箱配置
    /// </summary>
    private TreasureBoxTable LoadTreasureBoxConfig()
    {
        if (m_RewardId <= 0)
        {
            DebugEx.Warning(this.GetType().Name, "无法获取敌人配置（RewardId无效）");
            return null;
        }

        var treasureBoxTable = GF.DataTable.GetDataTable<TreasureBoxTable>();
        if (treasureBoxTable == null)
        {
            DebugEx.Error(this.GetType().Name, "TreasureBoxTable 数据表未加载");
            return null;
        }

        var treasureBoxRow = treasureBoxTable.GetDataRow(m_RewardId);
        if (treasureBoxRow == null)
        {
            DebugEx.Warning(this.GetType().Name, $"未找到宝箱配置 ID={m_RewardId}");
            return null;
        }

        return treasureBoxRow;
    }

    /// <summary>
    /// 从保存的敌人难度获取奖励倍率
    /// </summary>
    private float GetRewardMultiplier()
    {
        if (m_Difficulty <= 0)
        {
            return 1f;
        }

        var difficultyTable = GF.DataTable.GetDataTable<CombatDifficultyRule>();
        if (difficultyTable == null)
        {
            return 1f;
        }

        var difficultyRow = difficultyTable.GetDataRow(m_Difficulty);
        if (difficultyRow == null)
        {
            return 1f;
        }

        return difficultyRow.RewardMultiplier;
    }

    /// <summary>
    /// 解析物品组权重配置（格式："1:10,2:5,3:-1"）
    /// 权重为 -1 表示必定选中
    /// </summary>
    private Dictionary<int, int> ParseItemGroupWeights(string weightConfig)
    {
        var weights = new Dictionary<int, int>();
        if (string.IsNullOrEmpty(weightConfig))
        {
            return weights;
        }

        var pairs = weightConfig.Split(',');
        foreach (var pair in pairs)
        {
            var parts = pair.Trim().Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int groupId) && int.TryParse(parts[1], out int weight))
            {
                weights[groupId] = weight;
            }
        }

        return weights;
    }

    /// <summary>
    /// 根据权重多次选择物品组（可重复）
    /// 权重为 -1 表示必定选中，优先选中一次，然后在剩余位置继续按权重随机
    /// </summary>
    private List<int> SelectGroupsByWeights(Dictionary<int, int> groupWeights, int count)
    {
        var selectedGroups = new List<int>();

        if (groupWeights == null || groupWeights.Count == 0 || count <= 0)
        {
            return selectedGroups;
        }

        // 分离必定选中和可选物品组
        var mandatoryGroups = new List<int>();
        var optionalWeights = new Dictionary<int, int>();

        foreach (var kvp in groupWeights)
        {
            if (kvp.Value == -1)
            {
                mandatoryGroups.Add(kvp.Key);
            }
            else if (kvp.Value > 0)
            {
                optionalWeights[kvp.Key] = kvp.Value;
            }
        }

        int remaining = count;

        // 优先选中必定的物品组（只选一个）
        if (mandatoryGroups.Count > 0)
        {
            int mandatoryGroup = mandatoryGroups[UnityEngine.Random.Range(0, mandatoryGroups.Count)];
            selectedGroups.Add(mandatoryGroup);
            remaining--;
        }

        // 剩余位置按权重随机选择（权重为 -1 的物品组不再参与随机）
        var weightsForRandom = optionalWeights.Count > 0 ? optionalWeights : groupWeights;

        for (int i = 0; i < remaining; i++)
        {
            int selectedGroup = SelectSingleGroupByWeight(weightsForRandom);
            if (selectedGroup >= 0)
            {
                selectedGroups.Add(selectedGroup);
            }
        }

        return selectedGroups;
    }

    /// <summary>
    /// 按权重选择单个物品组
    /// </summary>
    private int SelectSingleGroupByWeight(Dictionary<int, int> groupWeights)
    {
        if (groupWeights == null || groupWeights.Count == 0)
        {
            return -1;
        }

        int totalWeight = 0;
        foreach (var weight in groupWeights.Values)
        {
            if (weight > 0)
            {
                totalWeight += weight;
            }
        }

        if (totalWeight <= 0)
        {
            return -1;
        }

        int randomWeight = UnityEngine.Random.Range(1, totalWeight + 1);
        int currentWeight = 0;

        foreach (var kvp in groupWeights)
        {
            if (kvp.Value > 0)
            {
                currentWeight += kvp.Value;
                if (randomWeight <= currentWeight)
                {
                    return kvp.Key;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// 从指定物品组中随机选择一个物品
    /// </summary>
    private ItemTable SelectItemFromGroup(ItemTable[] allRows, int groupId)
    {
        if (allRows == null || allRows.Length == 0)
        {
            return null;
        }

        var itemGroupTable = GF.DataTable.GetDataTable<ItemGroupTable>();
        if (itemGroupTable == null)
        {
            return null;
        }

        var groupRow = itemGroupTable.GetDataRow(groupId);
        if (groupRow == null || groupRow.ItemIds == null || groupRow.ItemIds.Length == 0)
        {
            return null;
        }

        // 从物品组中随机选择一个物品ID
        int randomItemId = groupRow.ItemIds[UnityEngine.Random.Range(0, groupRow.ItemIds.Length)];

        // 从 allRows 中查找对应的物品
        foreach (var row in allRows)
        {
            if (row.Id == randomItemId)
            {
                return row;
            }
        }

        return null;
    }

}
