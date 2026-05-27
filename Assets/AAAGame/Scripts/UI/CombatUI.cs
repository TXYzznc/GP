using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class CombatUI : StateAwareUIForm
{
    #region 字段

    /// <summary>⭐ 新增：当前显示详情的棋子实体</summary>
    private ChessEntity m_CurrentDetailChess;

    /// <summary>⭐ 新增：当前战斗中的刷新次数</summary>
    private int m_RefreshCount = 0;

    /// <summary>⭐ 新增：羁绊Buff UI 缓存 (synergyId → BuffUI GameObject)</summary>
    private Dictionary<int, GameObject> m_SynergyBuffUICache = new();

    private CardSlotContainer m_CardSlotContainerCache;

    #endregion

    #region 事件订阅

    protected override void SubscribeEvents()
    {
        DebugEx.Log(nameof(CombatUI), "订阅战斗状态事件");
        GF.Event.Subscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Subscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);
        GF.Event.Subscribe(PlayerLevelUpEventArgs.EventId, OnPlayerLevelUp);

        // 订阅运行时数据变化事件
        SubscribeRuntimeDataEvents();

        // 订阅玩家棋子选中事件
        ChessSelectionManager.OnChessSelected += OnChessSelectedForDetail;
        ChessSelectionManager.OnChessDeselected += OnChessDeselectedForDetail;

        // 订阅敌方棋子详情事件
        EnemyChessDetailManager.OnEnemyChessClicked += OnEnemyChessClickedForDetail;
        EnemyChessDetailManager.OnEnemyChessDeselected += OnEnemyChessDeselectedForDetail;

        // ⭐ 新增：订阅羁绊状态变化事件
        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyStateChanged += OnSynergyStateChanged;
        }
    }

    protected override void UnsubscribeEvents()
    {
        DebugEx.Log(nameof(CombatUI), "取消订阅战斗状态事件");
        GF.Event.Unsubscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Unsubscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);
        GF.Event.Unsubscribe(PlayerLevelUpEventArgs.EventId, OnPlayerLevelUp);

        // 取消订阅运行时数据变化事件
        UnsubscribeRuntimeDataEvents();

        // 取消订阅玩家棋子选中事件
        ChessSelectionManager.OnChessSelected -= OnChessSelectedForDetail;
        ChessSelectionManager.OnChessDeselected -= OnChessDeselectedForDetail;

        // 取消订阅敌方棋子详情事件
        EnemyChessDetailManager.OnEnemyChessClicked -= OnEnemyChessClickedForDetail;
        EnemyChessDetailManager.OnEnemyChessDeselected -= OnEnemyChessDeselectedForDetail;

        // ⭐ 新增：取消订阅羁绊状态变化事件
        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyStateChanged -= OnSynergyStateChanged;
        }
    }

    /// <summary>
    /// 订阅运行时数据变化事件
    /// </summary>
    private void SubscribeRuntimeDataEvents()
    {
        // 订阅召唤师HP/MP变化事件
        if (SummonerRuntimeDataManager.Instance != null)
        {
            SummonerRuntimeDataManager.Instance.OnHPChanged += OnSummonerHPChanged;
            SummonerRuntimeDataManager.Instance.OnMPChanged += OnSummonerMPChanged;
        }
    }

    /// <summary>
    /// 取消订阅运行时数据变化事件
    /// </summary>
    private void UnsubscribeRuntimeDataEvents()
    {
        // 取消订阅召唤师HP/MP变化事件
        if (SummonerRuntimeDataManager.Instance != null)
        {
            SummonerRuntimeDataManager.Instance.OnHPChanged -= OnSummonerHPChanged;
            SummonerRuntimeDataManager.Instance.OnMPChanged -= OnSummonerMPChanged;
        }
    }

    #endregion

    #region 事件处理

    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        RefreshPlayerInfo();
    }

    private void OnCombatEnter(object sender, GameEventArgs e)
    {
        DebugEx.Log(nameof(CombatUI), "收到战斗进入事件");

        // ⭐ 新增：创建卡牌预览管理器
        CreateCardPreviewManager();

        // ⭐ 新增：初始化刷新次数
        m_RefreshCount = 0;

        // CardManager 已在 CombatState 中初始化
        if (CardManager.Instance != null)
        {
            DebugEx.Log(nameof(CombatUI), "CardManager 已初始化");
        }

        ShowUI();
        RefreshCombatUI();
    }

    private void OnCombatLeave(object sender, GameEventArgs e)
    {
        DebugEx.Log(nameof(CombatUI), "收到战斗离开事件");

        // ⭐ 新增：清空羁绊Buff UI
        ClearAllSynergyBuffUI();

        // ⭐ 新增：销毁卡牌预览管理器
        if (CardPreviewDisplayShader.Instance != null)
        {
            Destroy(CardPreviewDisplayShader.Instance.gameObject);
            DebugEx.Log(nameof(CombatUI), "卡牌预览管理器已销毁");
        }

        // 清理 CardManager
        if (CardManager.Instance != null)
        {
            CardManager.Instance.Clear();
            DebugEx.Log(nameof(CombatUI), "CardManager 已清理");
        }

        // 清理 CardSlotContainer 的状态
        var container = GetCardSlotContainer();
        if (container != null)
        {
            container.ClearState();
        }

        HideUI();
    }

    /// <summary>
    /// 创建卡牌预览管理器（生成到 WorldCanvas 下）
    /// </summary>
    private void CreateCardPreviewManager()
    {
        // 检查是否已存在
        if (CardPreviewDisplayShader.Instance != null)
            return;

        // 查找 WorldCanvas
        var worldCanvas = FindObjectOfType<Canvas>();
        Transform parentTransform = transform.parent;

        // 尝试找到名字为 "WorldCanvas" 的 Canvas
        var allCanvas = FindObjectsOfType<Canvas>();
        foreach (var canvas in allCanvas)
        {
            if (canvas.gameObject.name == "WorldCanvas")
            {
                parentTransform = canvas.transform;
                break;
            }
        }

        var go = new GameObject("CardPreviewDisplayShader");
        go.transform.SetParent(parentTransform);
        go.AddComponent<CardPreviewDisplayShader>();

        DebugEx.Log(nameof(CombatUI), "卡牌预览管理器已创建");
    }

    /// <summary>
    /// 获取详情UI
    /// </summary>
    public DetailInfoUI GetDetailInfoUI()
    {
        if (varDetailInfoUI != null)
        {
            return varDetailInfoUI.GetComponent<DetailInfoUI>();
        }
        return null;
    }

    /// <summary>
    /// 获取卡槽吸附区域（使用绿色区域作为吸附检测）
    /// </summary>
    public Image GetCardSlotAdsorptionArea()
    {
        return varGreenArea;
    }

    /// <summary>
    /// 获取无效区域预览（红色覆盖，用于碰撞检测）
    /// </summary>
    public Image GetInvalidAreaPreview()
    {
        return varRedArea;
    }

    /// <summary>
    /// 战斗阶段棋子被选中，显示详情
    /// ⭐ 修改：订阅棋子属性变化事件，实现动态更新
    /// </summary>
    private void OnChessSelectedForDetail(ChessEntity entity)
    {
        var detailUI = GetDetailInfoUI();
        if (detailUI == null)
            return;

        m_CurrentDetailChess = entity;

        // ⭐ 新增：订阅棋子属性变化事件
        if (entity.Attribute != null)
        {
            entity.Attribute.OnHpChanged += OnDetailChessHpChanged;
            entity.Attribute.OnMpChanged += OnDetailChessMpChanged;
            DebugEx.Log(nameof(CombatUI), $"已订阅棋子 {entity.Config?.Name} 的属性变化事件");
        }

        // 订阅Buff变化事件
        ChessStateEvents.OnBuffAdded += OnDetailChessBuffChanged;
        ChessStateEvents.OnBuffRemoved += OnDetailChessBuffChanged;

        detailUI.SetChessUnitData(entity);
        detailUI.RefreshUI();
        detailUI.ShowWithAnimation();
        DebugEx.Log(nameof(CombatUI), $"显示棋子详情: {entity.Config?.Name}");
    }

    /// <summary>
    /// ⭐ 新增：棋子HP变化时，动态更新DetailInfoUI
    /// </summary>
    private void OnDetailChessHpChanged(double oldHp, double newHp)
    {
        if (m_CurrentDetailChess == null)
            return;

        var detailUI = GetDetailInfoUI();
        if (detailUI != null)
        {
            detailUI.RefreshUI();
            DebugEx.Log(nameof(CombatUI), $"DetailInfoUI已刷新（HP变化 {oldHp:F0} -> {newHp:F0}）");
        }
    }

    /// <summary>
    /// ⭐ 新增：棋子MP变化时，动态更新DetailInfoUI
    /// </summary>
    private void OnDetailChessMpChanged(double oldMp, double newMp)
    {
        if (m_CurrentDetailChess == null)
            return;

        var detailUI = GetDetailInfoUI();
        if (detailUI != null)
        {
            detailUI.RefreshUI();
            DebugEx.Log(nameof(CombatUI), $"DetailInfoUI已刷新（MP变化 {oldMp:F0} -> {newMp:F0}）");
        }
    }

    /// <summary>
    /// ⭐ 新增：棋子Buff变化时，动态更新DetailInfoUI
    /// </summary>
    private void OnDetailChessBuffChanged(int chessId, int buffId)
    {
        if (m_CurrentDetailChess == null)
            return;

        // 只更新当前显示的棋子
        if (m_CurrentDetailChess.Config?.Id != chessId)
            return;

        var detailUI = GetDetailInfoUI();
        if (detailUI != null)
        {
            detailUI.RefreshUI();
            DebugEx.Log(nameof(CombatUI), $"DetailInfoUI已刷新（Buff变化 ID={buffId}）");
        }
    }

    /// <summary>
    /// 战斗阶段棋子取消选中，隐藏详情
    /// ⭐ 修改：取消订阅棋子属性变化事件
    /// </summary>
    private void OnChessDeselectedForDetail()
    {
        // ⭐ 新增：取消订阅属性变化事件
        if (m_CurrentDetailChess != null && m_CurrentDetailChess.Attribute != null)
        {
            m_CurrentDetailChess.Attribute.OnHpChanged -= OnDetailChessHpChanged;
            m_CurrentDetailChess.Attribute.OnMpChanged -= OnDetailChessMpChanged;
            DebugEx.Log(
                nameof(CombatUI),
                $"已取消订阅棋子 {m_CurrentDetailChess.Config?.Name} 的属性变化事件"
            );
        }

        ChessStateEvents.OnBuffAdded -= OnDetailChessBuffChanged;
        ChessStateEvents.OnBuffRemoved -= OnDetailChessBuffChanged;

        m_CurrentDetailChess = null;

        if (varDetailInfoUI != null)
        {
            if (varDetailInfoUI.TryGetComponent<DetailInfoUI>(out var detailUI))
                detailUI.HideWithAnimation();
            else
                varDetailInfoUI.SetActive(false);
        }
    }

    /// <summary>
    /// 召唤师HP变化回调
    /// </summary>
    private void OnSummonerHPChanged(float oldValue, float newValue)
    {
        RefreshPlayerHP();
    }

    /// <summary>
    /// 召唤师MP变化回调
    /// </summary>
    private void OnSummonerMPChanged(float oldValue, float newValue)
    {
        RefreshPlayerMP();
    }

    /// <summary>
    /// ⭐ 新增：敌方棋子被点击时显示详情
    /// </summary>
    private void OnEnemyChessClickedForDetail(ChessEntity enemyChess)
    {
        var detailUI = GetDetailInfoUI();
        if (detailUI == null)
            return;

        m_CurrentDetailChess = enemyChess;

        // 订阅敌方棋子属性变化事件
        if (enemyChess.Attribute != null)
        {
            enemyChess.Attribute.OnHpChanged += OnDetailChessHpChanged;
            enemyChess.Attribute.OnMpChanged += OnDetailChessMpChanged;
            DebugEx.Log(
                nameof(CombatUI),
                $"已订阅敌方棋子 {enemyChess.Config?.Name} 的属性变化事件"
            );
        }

        // 订阅Buff变化事件
        ChessStateEvents.OnBuffAdded += OnDetailChessBuffChanged;
        ChessStateEvents.OnBuffRemoved += OnDetailChessBuffChanged;

        detailUI.SetChessUnitData(enemyChess);
        detailUI.RefreshUI();
        detailUI.ShowWithAnimation();
        DebugEx.Log(nameof(CombatUI), $"显示敌方棋子详情: {enemyChess.Config?.Name}");
    }

    /// <summary>
    /// ⭐ 新增：敌方棋子被取消点击时隐藏详情
    /// </summary>
    private void OnEnemyChessDeselectedForDetail()
    {
        // 取消订阅属性变化事件
        if (m_CurrentDetailChess != null && m_CurrentDetailChess.Attribute != null)
        {
            m_CurrentDetailChess.Attribute.OnHpChanged -= OnDetailChessHpChanged;
            m_CurrentDetailChess.Attribute.OnMpChanged -= OnDetailChessMpChanged;
            DebugEx.Log(
                nameof(CombatUI),
                $"已取消订阅敌方棋子 {m_CurrentDetailChess.Config?.Name} 的属性变化事件"
            );
        }

        ChessStateEvents.OnBuffAdded -= OnDetailChessBuffChanged;
        ChessStateEvents.OnBuffRemoved -= OnDetailChessBuffChanged;

        m_CurrentDetailChess = null;

        if (varDetailInfoUI != null)
        {
            if (varDetailInfoUI.TryGetComponent<DetailInfoUI>(out var detailUI))
                detailUI.HideWithAnimation();
            else
                varDetailInfoUI.SetActive(false);
        }
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新战斗UI
    /// </summary>
    private void RefreshCombatUI()
    {
        RefreshEnemyInfo();
        RefreshPlayerStatus();
        RefreshPlayerInfo();
        RefreshCardSlots();
        BindButtonEvents();

        DebugEx.Log(nameof(CombatUI), "战斗UI已刷新");
    }

    /// <summary>
    /// 刷新玩家信息：头像、召唤师名+等级、经验条
    /// </summary>
    private void RefreshPlayerInfo()
    {
        var saveData = PlayerAccountDataManager.Instance?.CurrentSaveData;
        if (saveData == null)
            return;

        int level = saveData.GlobalLevel;

        // 召唤师名称 + 等级
        if (varPlayerInfo != null)
        {
            var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
            var summonerRow = summonerTable?.GetDataRow(saveData.CurrentSummonerId);
            string summonerName = summonerRow != null ? summonerRow.Name : "召唤师";
            varPlayerInfo.text = $"{summonerName}·{level}级";

            // 加载头像（异步）
            if (varPlayerImg != null && summonerRow != null && summonerRow.HeadImgId > 0)
                RefreshSummonerAvatarAsync(summonerRow.HeadImgId).Forget();
        }

        // 经验条 + 经验文本
        var levelTable = GF.DataTable.GetDataTable<PlayerDataTable>();
        var levelRow = levelTable?.GetDataRow(level);
        int currentExp = saveData.CurrentExp;
        int requiredExp = levelRow != null ? levelRow.RequiredExp : 0;

        if (varPlayerEXP != null)
        {
            // 满级（RequiredExp == 0）时填满
            varPlayerEXP.fillAmount =
                requiredExp > 0 ? Mathf.Clamp01((float)currentExp / requiredExp) : 1f;
        }

        if (varPlayerEXPText != null)
        {
            varPlayerEXPText.text =
                requiredExp > 0 ? $"{currentExp}/{requiredExp}" : $"{currentExp}/--";
        }
    }

    private async UniTaskVoid RefreshSummonerAvatarAsync(int headImgId)
    {
        await GameExtension.ResourceExtension.LoadSpriteAsync(headImgId, varPlayerImg, 1f, null);
    }

    /// <summary>
    /// 刷新敌人信息
    /// </summary>
    private void RefreshEnemyInfo()
    {
        if (varEnemyTitle != null)
        {
            varEnemyTitle.SetActive(true);
        }

        // 从 EnemySpawnManager 获取当前战斗的敌人配置ID
        if (EnemySpawnManager.Instance != null)
        {
            var spawnedEnemies = EnemySpawnManager.Instance.GetSpawnedEnemies();
            if (spawnedEnemies != null && spawnedEnemies.Count > 0)
            {
                // 获取第一个敌人的配置ID（假设同一波次的敌人使用相同的 BattleConfigId）
                var firstEnemy = spawnedEnemies[0];
                if (firstEnemy != null && firstEnemy.Config != null)
                {
                    // 通过 EnemyEntityTable 获取 BattleConfigId
                    var entityTable = GF.DataTable.GetDataTable<EnemyEntityTable>();
                    if (entityTable != null)
                    {
                        // 遍历查找匹配的 EnemyEntity（通过棋子ID反查）
                        foreach (var entityRow in entityTable.GetAllDataRows())
                        {
                            var enemyTable = GF.DataTable.GetDataTable<EnemyTable>();
                            var enemyRow = enemyTable?.GetDataRow(entityRow.BattleConfigId);

                            if (enemyRow != null && enemyRow.ChessIds != null)
                            {
                                // 检查是否包含当前棋子ID
                                bool containsChess = false;
                                foreach (int chessId in enemyRow.ChessIds)
                                {
                                    if (chessId == firstEnemy.Config.Id)
                                    {
                                        containsChess = true;
                                        break;
                                    }
                                }

                                if (containsChess)
                                {
                                    // 找到匹配的敌人配置，显示名称
                                    if (varEnemyName != null)
                                    {
                                        varEnemyName.text = enemyRow.EnemyName;
                                    }

                                    if (varEnemyNum != null)
                                    {
                                        varEnemyNum.text = $"x{spawnedEnemies.Count}";
                                    }

                                    DebugEx.Log(
                                        nameof(CombatUI),
                                        $"刷新敌人信息: {enemyRow.EnemyName}, 数量={spawnedEnemies.Count}"
                                    );
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // 默认值（如果无法获取）
        if (varEnemyName != null && string.IsNullOrEmpty(varEnemyName.text))
        {
            varEnemyName.text = "未知敌人";
        }

        if (varEnemyNum != null && string.IsNullOrEmpty(varEnemyNum.text))
        {
            varEnemyNum.text = "x?";
        }

        if (varEnemyWave != null)
        {
            varEnemyWave.text = "1/1"; // TODO: 多波次支持
        }
    }

    /// <summary>
    /// 刷新玩家状态
    /// </summary>
    private void RefreshPlayerStatus()
    {
        RefreshPlayerHP();
        RefreshPlayerMP();
    }

    /// <summary>
    /// 刷新玩家HP显示
    /// </summary>
    private void RefreshPlayerHP()
    {
        // 刷新HP滑条
        if (varHPSlider != null)
        {
            // 从召唤师运行时数据获取HP
            if (
                SummonerRuntimeDataManager.Instance != null
                && SummonerRuntimeDataManager.Instance.IsInitialized
            )
            {
                varHPSlider.value = SummonerRuntimeDataManager.Instance.HPPercent;
            }
            else
            {
                varHPSlider.value = 1.0f; // 默认满血
            }
        }

        // 刷新HP文本
        if (varHpText != null)
        {
            if (
                SummonerRuntimeDataManager.Instance != null
                && SummonerRuntimeDataManager.Instance.IsInitialized
            )
            {
                int currentHP = Mathf.RoundToInt(SummonerRuntimeDataManager.Instance.CurrentHP);
                int maxHP = Mathf.RoundToInt(SummonerRuntimeDataManager.Instance.MaxHP);
                varHpText.text = $"{currentHP}/{maxHP}";
            }
            else
            {
                varHpText.text = "100/100"; // 默认值
            }
        }
    }

    /// <summary>
    /// 刷新玩家MP显示
    /// </summary>
    private void RefreshPlayerMP()
    {
        // 刷新MP滑条
        if (varMPSlider != null)
        {
            // 从召唤师运行时数据获取MP
            if (
                SummonerRuntimeDataManager.Instance != null
                && SummonerRuntimeDataManager.Instance.IsInitialized
            )
            {
                varMPSlider.value = SummonerRuntimeDataManager.Instance.MPPercent;
            }
            else
            {
                varMPSlider.value = 1.0f; // 默认满MP
            }
        }

        // 刷新MP文本
        if (varMpText != null)
        {
            if (
                SummonerRuntimeDataManager.Instance != null
                && SummonerRuntimeDataManager.Instance.IsInitialized
            )
            {
                int currentMP = Mathf.RoundToInt(SummonerRuntimeDataManager.Instance.CurrentMP);
                int maxMP = Mathf.RoundToInt(SummonerRuntimeDataManager.Instance.MaxMP);
                varMpText.text = $"{currentMP}/{maxMP}";
            }
            else
            {
                varMpText.text = "50/50"; // 默认值
            }
        }
    }

    /// <summary>
    /// 刷新卡牌槽
    /// </summary>
    private void RefreshCardSlots()
    {
        RefreshCardSlotsAsync().Forget();
    }

    /// <summary>
    /// 异步刷新卡牌槽（带动效）
    /// </summary>
    private async UniTask RefreshCardSlotsAsync()
    {
        if (varCardSlots == null || varCardSlotItem == null)
        {
            return;
        }

        // 确保对象池已初始化（打包后需要异步加载Prefab）
        if (CardSlotItemPool.Instance != null)
            await CardSlotItemPool.Instance.InitializeAsync();

        var container = GetCardSlotContainer();
        if (container == null)
        {
            DebugEx.Error(nameof(CombatUI), "未找到 CardSlotContainer 组件");
            return;
        }

        // 清理所有卡牌槽（通过容器的正式接口）
        var oldCards = new List<CardSlotItem>();
        for (int i = varCardSlots.transform.childCount - 1; i >= 0; i--)
        {
            var child = varCardSlots.transform.GetChild(i);
            if (child.gameObject != varCardSlotItem)
            {
                var cardSlotItem = child.GetComponent<CardSlotItem>();
                if (cardSlotItem != null)
                {
                    oldCards.Add(cardSlotItem);
                }
            }
        }

        // 回收旧卡牌到对象池
        foreach (var card in oldCards)
        {
            container.RemoveCard(card);
            CardSlotItemPool.Instance?.ReturnCard(card);
        }

        // 等待一帧，确保旧卡牌已移除
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());

        // 清理容器状态
        container.ClearState();

        // 从 CardManager 获取卡牌列表并创建卡牌槽
        if (CardManager.Instance != null)
        {
            var cards = CardManager.Instance.GetAvailableCards();

            // 第一步：先创建所有卡牌 UI，但不播放动画
            var cardSlots = new List<CardSlotItem>();
            for (int i = 0; i < cards.Count; i++)
            {
                // 从对象池获取卡牌
                CardSlotItem slotItem = CardSlotItemPool.Instance?.GetCard();
                if (slotItem != null)
                {
                    // 重新设置 parent（确保卡牌在正确的容器下）
                    slotItem.transform.SetParent(varCardSlots.transform, worldPositionStays: false);
                    slotItem.gameObject.name = $"CardSlot_{i}";

                    slotItem.SetData(cards[i]);
                    // 仅添加到容器，不播放动画
                    container.AddCardSilent(slotItem);
                    cardSlots.Add(slotItem);

                    DebugEx.Log(nameof(CombatUI), $"从对象池获取卡牌槽 {i}");
                }
                else
                {
                    DebugEx.Error(nameof(CombatUI), "无法从对象池获取卡牌槽");
                }
            }

            // 第二步：统一启动所有卡牌的进场动画（此时所有卡都已添加，位置计算基于最终的卡牌数量）
            await container.PlayAllCardAnimationsAsync();

            DebugEx.Log(nameof(CombatUI), $"刷新卡牌槽完成，共 {cards.Count} 张卡牌");
        }
        else
        {
            DebugEx.Warning(nameof(CombatUI), "CardManager 未初始化");
        }
    }

    /// <summary>
    /// 获取卡牌容器
    /// </summary>
    private CardSlotContainer GetCardSlotContainer()
    {
        if (m_CardSlotContainerCache == null)
            m_CardSlotContainerCache = varCardSlots?.GetComponent<CardSlotContainer>();
        return m_CardSlotContainerCache;
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        // 人口按钮
        if (varBtn_Population != null)
        {
            varBtn_Population.onClick.RemoveAllListeners();
            varBtn_Population.onClick.AddListener(OnPopulationButtonClicked);
        }

        // 刷新按钮
        if (varBtn_Refresh != null)
        {
            varBtn_Refresh.onClick.RemoveAllListeners();
            varBtn_Refresh.onClick.AddListener(OnRefreshButtonClicked);
        }

        // 召唤师技能按钮
        if (varBtn1Arr != null)
        {
            for (int i = 0; i < varBtn1Arr.Length; i++)
            {
                int index = i; // 闭包捕获
                varBtn1Arr[i].onClick.RemoveAllListeners();
                varBtn1Arr[i].onClick.AddListener(() => OnSummonerSkillClicked(index));
            }
        }

        // 加载技能图标（异步）
        RefreshSummonerSkillButtonsAsync().Forget();

        // 设置消耗数值
        if (varConsumeNum_Population != null)
        {
            varConsumeNum_Population.text = "2"; // TODO: 从配置获取
        }

        if (varConsumeNum_Refresh != null)
        {
            varConsumeNum_Refresh.text = "1"; // TODO: 从配置获取
        }
    }

    /// <summary>
    /// 刷新人口显示
    /// </summary>
    private void RefreshPopulationDisplay()
    {
        if (CombatSessionData.Instance.IsInitialized)
        {
            // 更新人口文本（如果有对应UI元素显示人口）
            // TODO: 根据实际UI元素更新
            // varPopulationText.text = $"{CombatSessionData.Instance.UsedPopulation}/{CombatSessionData.Instance.CurrentMaxDomination}";
        }
    }

    #endregion

    #region 按钮回调

    /// <summary>
    /// 人口按钮点击回调
    /// </summary>
    private void OnPopulationButtonClicked()
    {
        DebugEx.Log(nameof(CombatUI), "点击了人口按钮");

        if (CombatSessionData.Instance.TryUpgradePopulation())
        {
            // 刷新UI显示
            RefreshPopulationDisplay();
            DebugEx.Log(nameof(CombatUI), "统治值升级成功");
        }
        else
        {
            DebugEx.Warning(nameof(CombatUI), "统治值升级失败（金币不足）");
            // TODO: 显示提示信息
        }
    }

    /// <summary>
    /// 刷新按钮点击回调
    /// </summary>
    private void OnRefreshButtonClicked()
    {
        OnRefreshButtonClickedAsync().Forget();
    }

    /// <summary>
    /// 刷新按钮点击回调（异步）
    /// </summary>
    private async UniTask OnRefreshButtonClickedAsync()
    {
        DebugEx.Log(nameof(CombatUI), "点击了刷新按钮");

        // 检查是否还有刷新次数
        if (m_RefreshCount >= 3)
        {
            DebugEx.Warning(nameof(CombatUI), "本战斗已达到最大刷新次数（3次）");
            // TODO: 显示提示信息
            return;
        }

        // 检查召唤师运行时数据
        if (
            SummonerRuntimeDataManager.Instance == null
            || !SummonerRuntimeDataManager.Instance.IsInitialized
        )
        {
            DebugEx.Error(nameof(CombatUI), "召唤师数据未初始化");
            return;
        }

        var summonerData = SummonerRuntimeDataManager.Instance;
        float costAmount = 0;
        string costType = "";

        // 计算消耗
        if (m_RefreshCount == 0)
        {
            // 第一次：消耗 40% 最大灵力
            costAmount = summonerData.MaxMP * 0.4f;
            costType = "MP";
            if (summonerData.CurrentMP < costAmount)
            {
                DebugEx.Warning(
                    nameof(CombatUI),
                    $"灵力不足（需要 {costAmount:F0}，当前 {summonerData.CurrentMP:F0}）"
                );
                return;
            }
        }
        else if (m_RefreshCount == 1)
        {
            // 第二次：消耗 30% 最大生命值
            costAmount = summonerData.MaxHP * 0.3f;
            costType = "HP";
            if (summonerData.CurrentHP < costAmount)
            {
                DebugEx.Warning(
                    nameof(CombatUI),
                    $"生命值不足（需要 {costAmount:F0}，当前 {summonerData.CurrentHP:F0}）"
                );
                return;
            }
        }
        else if (m_RefreshCount == 2)
        {
            // 第三次：消耗 40% 最大生命值
            costAmount = summonerData.MaxHP * 0.4f;
            costType = "HP";
            if (summonerData.CurrentHP < costAmount)
            {
                DebugEx.Warning(
                    nameof(CombatUI),
                    $"生命值不足（需要 {costAmount:F0}，当前 {summonerData.CurrentHP:F0}）"
                );
                return;
            }
        }

        // 扣除资源
        if (costType == "MP")
        {
            summonerData.ConsumeMP(costAmount);
        }
        else if (costType == "HP")
        {
            summonerData.ReduceHP(costAmount);
        }

        // 刷新卡牌
        if (CardManager.Instance != null)
        {
            CardManager.Instance.RefreshCards();
            await RefreshCardSlotsAsync();
            m_RefreshCount++;
            DebugEx.Log(
                nameof(CombatUI),
                $"卡牌已刷新（第 {m_RefreshCount} 次），消耗 {costAmount:F0} {costType}"
            );
        }
    }

    /// <summary>
    /// 召唤师技能按钮点击回调（slot = index+1，所有输入走 PlayerInputManager）
    /// </summary>
    private void OnSummonerSkillClicked(int index)
    {
        int slot = index + 1;
        DebugEx.Log(nameof(CombatUI), $"点击了召唤师技能按钮，slot={slot}");
        PlayerInputManager.Instance?.TriggerSummonerSkill(slot);
    }

    /// <summary>
    /// 刷新召唤师技能按钮图标与可用状态
    /// </summary>
    private async UniTaskVoid RefreshSummonerSkillButtonsAsync()
    {
        if (varBtn1Arr == null || varBtn1Arr.Length == 0)
            return;

        var ct = this.GetCancellationTokenOnDestroy();

        var playerCharacter = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        var skillManager =
            playerCharacter != null ? playerCharacter.GetComponent<SummonerSkillManager>() : null;

        var skillTable = GF.DataTable.GetDataTable<SummonerSkillTable>();

        for (int i = 0; i < varBtn1Arr.Length; i++)
        {
            if (ct.IsCancellationRequested)
                return;

            var btn = varBtn1Arr[i];
            if (btn == null)
                continue;

            bool hasSkill = skillManager != null && i < skillManager.Skills.Count;
            btn.gameObject.SetActive(hasSkill);

            if (!hasSkill || skillTable == null)
                continue;

            int skillId = skillManager.Skills[i].SkillId;
            var row = skillTable.GetDataRow(skillId);
            if (row == null)
                continue;

            var btnImage = btn.GetComponent<Image>();
            if (btnImage != null && row.IconId > 0)
            {
                await GameExtension.ResourceExtension.LoadSpriteAsync(
                    row.IconId,
                    btnImage,
                    1f,
                    null
                );
                if (ct.IsCancellationRequested)
                    return;
            }
        }
    }

    #endregion

    #region 羁绊Buff显示

    /// <summary>
    /// 羁绊状态变化处理（激活/失活）
    /// </summary>
    private void OnSynergyStateChanged(int synergyId, bool isActivated)
    {
        if (isActivated)
        {
            DisplaySynergyBuff(synergyId);
        }
        else
        {
            RemoveSynergyBuff(synergyId);
        }
    }

    /// <summary>
    /// 显示羁绊对应的Buff（IsHidden=1的Buff）
    /// </summary>
    private void DisplaySynergyBuff(int synergyId)
    {
        if (varSynergyPanel == null || varBuffItem == null)
        {
            DebugEx.Warning(nameof(CombatUI), "varSynergyPanel 或 varBuffItem 未配置");
            return;
        }

        // 获取羁绊配置
        var dtSynergy = GF.DataTable.GetDataTable<SynergyTable>();
        var drSynergy = dtSynergy?.GetDataRow(synergyId);
        if (drSynergy == null)
        {
            DebugEx.Warning(nameof(CombatUI), $"羁绊 {synergyId} 配置未找到");
            return;
        }

        // 获取特效配置
        var dtEffect = GF.DataTable.GetDataTable<SpecialEffectTable>();
        var drEffect = dtEffect?.GetDataRow(drSynergy.EffectId);
        if (drEffect == null)
        {
            DebugEx.Warning(nameof(CombatUI), $"特效 {drSynergy.EffectId} 配置未找到");
            return;
        }

        // 获取Buff列表（从EffectParams解析BuffIds）
        var buffIds = ExtractBuffIds(drEffect);
        if (buffIds == null || buffIds.Count == 0)
        {
            DebugEx.Log(nameof(CombatUI), $"羁绊 {synergyId} 无对应Buff");
            return;
        }

        // 获取Buff表
        var dtBuff = GF.DataTable.GetDataTable<BuffTable>();

        // 为每个Buff生成UI（只显示IsHidden=1的Buff）
        var createdBuffUI = new List<GameObject>();
        foreach (int buffId in buffIds)
        {
            var drBuff = dtBuff?.GetDataRow(buffId);
            if (drBuff == null || drBuff.IsHidden == 0)
                continue;

            // 克隆Buff UI
            var buffUI = Instantiate(varBuffItem, varSynergyPanel.transform);
            buffUI.SetActive(true);

            // 加载图标
            if (drBuff.SpriteId > 0)
            {
                var img = buffUI.GetComponent<Image>();
                if (img != null)
                {
                    GameExtension.ResourceExtension.LoadSpriteAsync(drBuff.SpriteId, img).Forget();
                }
            }

            createdBuffUI.Add(buffUI);
        }

        if (createdBuffUI.Count > 0)
        {
            m_SynergyBuffUICache[synergyId] = createdBuffUI[0]; // 存储第一个（可扩展为列表）
            DebugEx.Success(
                nameof(CombatUI),
                $"羁绊 {synergyId} Buff UI已显示 (共 {createdBuffUI.Count} 个)"
            );
        }
    }

    /// <summary>
    /// 移除羁绊Buff UI
    /// </summary>
    private void RemoveSynergyBuff(int synergyId)
    {
        if (!m_SynergyBuffUICache.TryGetValue(synergyId, out var buffUI))
            return;

        if (buffUI != null)
        {
            Destroy(buffUI);
            DebugEx.Log(nameof(CombatUI), $"羁绊 {synergyId} Buff UI已移除");
        }

        m_SynergyBuffUICache.Remove(synergyId);
    }

    /// <summary>
    /// 从EffectParams中提取BuffIds
    /// 支持 SelfBuffIds 和 BuffIds 字段
    /// </summary>
    private List<int> ExtractBuffIds(SpecialEffectTable drEffect)
    {
        var result = new List<int>();
        if (string.IsNullOrEmpty(drEffect.EffectParams))
            return result;

        try
        {
            // 简单JSON解析（假设格式为 {"SelfBuffIds":[7001,7002]} 或 {"BuffIds":[...]}）
            if (drEffect.EffectParams.Contains("SelfBuffIds"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    drEffect.EffectParams,
                    @"""SelfBuffIds""\s*:\s*\[([\d,\s]+)\]"
                );
                if (match.Success)
                {
                    foreach (var id in match.Groups[1].Value.Split(','))
                    {
                        if (int.TryParse(id.Trim(), out int buffId))
                            result.Add(buffId);
                    }
                }
            }
            else if (drEffect.EffectParams.Contains("BuffIds"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    drEffect.EffectParams,
                    @"""BuffIds""\s*:\s*\[([\d,\s]*)\]"
                );
                if (match.Success)
                {
                    var ids = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(ids))
                    {
                        foreach (var id in ids.Split(','))
                        {
                            if (int.TryParse(id.Trim(), out int buffId))
                                result.Add(buffId);
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            DebugEx.Error(nameof(CombatUI), $"解析EffectParams失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 清空所有羁绊Buff UI（战斗结束时调用）
    /// </summary>
    private void ClearAllSynergyBuffUI()
    {
        foreach (var buffUI in m_SynergyBuffUICache.Values)
        {
            if (buffUI != null)
                Destroy(buffUI);
        }
        m_SynergyBuffUICache.Clear();
    }

    #endregion

    #region 生命周期

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // 更新召唤师MP恢复
        if (
            SummonerRuntimeDataManager.Instance != null
            && SummonerRuntimeDataManager.Instance.IsInitialized
        )
        {
            SummonerRuntimeDataManager.Instance.UpdateMPRegen(elapseSeconds);
        }

        // ⭐ 新增：更新敌方棋子详情管理器（处理点击检测）
        if (EnemyChessDetailManager.Instance != null)
        {
            EnemyChessDetailManager.Instance.Tick();
        }
    }

    #endregion
}
