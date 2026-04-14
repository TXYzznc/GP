using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class CombatPreparationUI : UIFormBase
{
    #region 字段

    /// <summary>倒计时剩余时间</summary>
    private float m_CountdownRemain;

    /// <summary>是否已准备完毕</summary>
    private bool m_IsReady;

    /// <summary>已生成的棋子UI项</summary>
    private List<GameObject> m_SpawnedChessItems = new List<GameObject>();

    /// <summary>实例ID到棋子UI的映射</summary>
    private Dictionary<string, ChessItemUI> m_ChessItemUIDict =
        new Dictionary<string, ChessItemUI>();

    /// <summary>当前选中的棋子实例ID</summary>
    private string m_SelectedChessInstanceId = string.Empty;

    /// <summary>选中棋子卡片的位置偏移</summary>
    private const float SELECTED_OFFSET = 20f;

    /// <summary>选中动画时长</summary>
    private const float SELECTED_ANIMATION_DURATION = 0.3f;

    /// <summary>可选的Buff ID列表（用于三选一）</summary>
    private List<int> m_AvailableBuffIds = new List<int>();

    /// <summary>选中的Buff ID</summary>
    private int m_SelectedBuffId;

    /// <summary>当前Buff选择模式</summary>
    private BuffSelectionMode m_BuffSelectionMode;

    /// <summary>敌方先手Buff通知的取消令牌</summary>
    private System.Threading.CancellationTokenSource m_NotificationCts;

    /// <summary>Buff选择自动确认的取消令牌</summary>
    private System.Threading.CancellationTokenSource m_BuffSelectionCts;

    /// <summary>已生成的 BuffChooseItem 列表</summary>
    private List<GameObject> m_BuffChooseItems = new List<GameObject>();

    /// <summary>⭐ 新增：当前显示详情的棋子实体</summary>
    private ChessEntity m_CurrentDetailChess;

    /// <summary>⭐ 新增：棋子卡容器（管理扇形排列和进场动效）</summary>
    private ChessSlotContainer m_ChessSlotContainer;

    /// <summary>装备槽列表（预创建固定数量）</summary>
    private List<InventorySlotUI> m_EquipSlots = new List<InventorySlotUI>();

    /// <summary>装备槽容器</summary>
    private EquipSlotContainerImpl m_EquipSlotContainer;

    #endregion

    #region 枚举

    /// <summary>Buff选择模式</summary>
    private enum BuffSelectionMode
    {
        None, // 无
        SneakDebuff, // 偷袭Debuff三选一
        InitiativeBuff, // 先手Buff三选一
    }

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        Log.Info("CombatPreparationUI: 初始化");

        // 获取棋子卡容器
        if (varChessPanel != null)
        {
            m_ChessSlotContainer = varChessPanel.GetComponent<ChessSlotContainer>();
            if (m_ChessSlotContainer == null)
            {
                DebugEx.ErrorModule(
                    "CombatPreparationUI",
                    "ChessPanel 上未找到 ChessSlotContainer 组件"
                );
            }
            else
            {
                DebugEx.LogModule("CombatPreparationUI", "ChessSlotContainer 初始化成功");
            }
        }
        else
        {
            DebugEx.ErrorModule(
                "CombatPreparationUI",
                "varChessPanel 为空，无法获取 ChessSlotContainer"
            );
        }
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Log.Info("CombatPreparationUI: 已打开");

        // 隐藏Buff选择面板（正常情况下应隐藏，只在触发偷袭或先手效果时显示）
        if (varBuffSelection != null)
        {
            varBuffSelection.alpha = 0f;
            varBuffSelection.interactable = false;
            varBuffSelection.blocksRaycasts = false;
            Log.Info("CombatPreparationUI: Buff选择面板已隐藏");
        }

        // 隐藏敌方先手Buff通知（正常情况下应隐藏，只有敌方先手时才显示）
        if (varInitiativeBuffNotification != null)
        {
            varInitiativeBuffNotification.alpha = 0f;
            varInitiativeBuffNotification.interactable = false;
            varInitiativeBuffNotification.blocksRaycasts = false;
            Log.Info("CombatPreparationUI: 敌方先手Buff通知已隐藏");
        }

        // 隐藏棋子详细信息面板（正常情况下应隐藏，只在点击棋子时显示）
        if (varDetailInfoUI != null)
        {
            varDetailInfoUI.SetActive(false);
            Log.Info("CombatPreparationUI: 棋子详细信息面板已隐藏");
        }

        // 重置状态
        var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
        float preparationSeconds = ruleRow != null ? ruleRow.PreparationDurationSeconds : 30f;
        m_CountdownRemain = Mathf.Max(0f, preparationSeconds);
        m_IsReady = false;

        // 订阅棋子库存事件
        ChessDeploymentTracker.Instance.OnChessDeployed += OnChessDeployedHandler;
        ChessDeploymentTracker.Instance.OnChessRecalled += OnChessRecalledHandler;

        // ⭐ 新增：订阅场景棋子选择事件（点击场景中的棋子时显示详情）
        ChessSelectionManager.OnChessSelected += OnSceneChessSelectedHandler;
        ChessSelectionManager.OnChessDeselected += OnSceneChessDeselectedHandler;

        // 订阅背包数据变化事件
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += OnInventoryChanged;

        // 初始化装备槽（仅首次打开）
        if (m_EquipSlots.Count == 0)
            InitializeEquipSlots();

        // 刷新各个面板
        RefreshEquipmentPanel();
        RefreshChessPanel();

        // 更新准备按钮状态
        UpdateReadyButton(false);

        // 更新倒计时显示
        UpdateCountdownText();
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        if (m_IsReady)
            return;

        // 倒计时
        m_CountdownRemain -= elapseSeconds;
        UpdateCountdownText();

        // 倒计时结束，自动准备完毕
        if (m_CountdownRemain <= 0f)
        {
            m_CountdownRemain = 0f;
            OnPreparationComplete();
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 取消订阅棋子库存事件
        ChessDeploymentTracker.Instance.OnChessDeployed -= OnChessDeployedHandler;
        ChessDeploymentTracker.Instance.OnChessRecalled -= OnChessRecalledHandler;

        // ⭐ 新增：取消订阅场景棋子选择事件
        ChessSelectionManager.OnChessSelected -= OnSceneChessSelectedHandler;
        ChessSelectionManager.OnChessDeselected -= OnSceneChessDeselectedHandler;

        // 取消订阅背包数据变化事件
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= OnInventoryChanged;

        // 清理生成的UI项
        ClearSpawnedItems();

        // 清理Buff选择相关
        ClearBuffChooseItems();
        m_BuffSelectionCts?.Cancel();
        m_BuffSelectionCts?.Dispose();
        m_BuffSelectionCts = null;
        m_NotificationCts?.Cancel();
        m_NotificationCts?.Dispose();
        m_NotificationCts = null;

        base.OnClose(isShutdown, userData);
        Log.Info("CombatPreparationUI: 已关闭");
    }

    #endregion

    #region 按钮事件

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);

        if (btSelf == varReadyBtn)
        {
            OnReadyButtonClick();
        }
    }

    /// <summary>
    /// 准备按钮点击
    /// </summary>
    private void OnReadyButtonClick()
    {
        if (m_IsReady)
            return;

        Log.Info("CombatPreparationUI: 玩家点击准备完毕");
        OnPreparationComplete();
    }

    #endregion

    #region 装备面板

    /// <summary>
    /// 初始化装备槽（首次打开时调用一次）
    /// 预创建 9 个装备槽，始终显示
    /// </summary>
    private void InitializeEquipSlots()
    {
        if (varEquipmentPanel == null || varInventorySlotUI == null)
            return;

        // 初始化容器（如果还未初始化）
        if (m_EquipSlotContainer == null)
        {
            m_EquipSlotContainer = GetComponent<EquipSlotContainerImpl>();
            if (m_EquipSlotContainer == null)
                m_EquipSlotContainer = gameObject.AddComponent<EquipSlotContainerImpl>();
        }

        const int equipSlotsCount = 9;
        for (int i = 0; i < equipSlotsCount; i++)
        {
            var go = Instantiate(varInventorySlotUI, varEquipmentPanel.transform);
            if (!go.TryGetComponent<InventorySlotUI>(out var slotUI))
                continue;

            slotUI.SetSlotIndex(i);
            slotUI.SetContainerType(SlotContainerType.Equip);
            slotUI.SetSlotContainer(m_EquipSlotContainer);
            slotUI.gameObject.SetActive(true);
            slotUI.gameObject.name = $"EquipSlot_{i}";
            m_EquipSlots.Add(slotUI);
        }

        DebugEx.LogModule("CombatPreparationUI", $"装备栏初始化完成，共 {m_EquipSlots.Count} 个装备槽");
    }

    /// <summary>
    /// 刷新装备面板：从背包显示装备到预创建的槽位
    /// </summary>
    private void RefreshEquipmentPanel()
    {
        if (m_EquipSlots.Count == 0)
            return;

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            DebugEx.WarningModule("CombatPreparationUI", "InventoryManager 未初始化");
            return;
        }

        var itemTable = GF.DataTable.GetDataTable<ItemTable>();
        var allSlots = inventoryManager.GetAllSlots();
        int displayIndex = 0;

        // 遍历背包所有槽位，过滤装备类型并填充到预创建的槽位
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot.IsEmpty)
                continue;

            var row = itemTable?.GetDataRow(slot.ItemStack.Item.ItemId);
            if (row == null || row.Type != (int)ItemType.Equipment)
                continue;

            if (displayIndex >= m_EquipSlots.Count)
                break;

            m_EquipSlots[displayIndex].SetSlotIndex(i); // 使用实际背包槽位索引
            m_EquipSlots[displayIndex].SetData(slot.ItemStack);
            displayIndex++;
        }

        // 清空未填充的槽位
        for (int i = displayIndex; i < m_EquipSlots.Count; i++)
        {
            m_EquipSlots[i].SetData(null);
        }

        DebugEx.LogModule("CombatPreparationUI", $"装备面板已刷新，显示 {displayIndex} 个装备");
    }

    /// <summary>
    /// 背包数据变化回调
    /// </summary>
    private void OnInventoryChanged()
    {
        RefreshEquipmentPanel();
    }

    #endregion

    #region 棋子面板

    /// <summary>
    /// 刷新备战棋子面板
    /// </summary>
    private void RefreshChessPanel()
    {
        // 获取所有棋子实例
        var allChess = ChessDeploymentTracker.Instance.GetAllChessInstances();

        if (allChess.Count == 0)
        {
            Log.Warning("CombatPreparationUI: 没有棋子");
            return;
        }

        // 第一次打开，创建所有棋子UI
        if (m_ChessItemUIDict.Count == 0)
        {
            InitializeChessPanelAsync(allChess).Forget();
        }
        else
        {
            // 后续刷新，只更新状态（不重新创建）
            foreach (var instance in allChess)
            {
                if (m_ChessItemUIDict.TryGetValue(instance.InstanceId, out var chessItemUI))
                {
                    // 更新出战状态（显示/隐藏灰色）
                    chessItemUI.SetDeployedState();
                }
            }

            Log.Info($"CombatPreparationUI: 棋子面板状态已更新");
        }
    }

    /// <summary>
    /// 异步初始化棋子面板（包含进场动画）
    /// </summary>
    private async UniTask InitializeChessPanelAsync(
        List<ChessDeploymentTracker.ChessInstanceData> allChess
    )
    {
        if (m_ChessSlotContainer == null)
        {
            DebugEx.ErrorModule("CombatPreparationUI", "ChessSlotContainer 未初始化");
            return;
        }

        foreach (var instance in allChess)
        {
            // 验证配置是否存在
            if (!ChessDataManager.Instance.HasConfig(instance.ChessId))
            {
                Log.Warning($"CombatPreparationUI: 棋子配置不存在 Id={instance.ChessId}，跳过");
                continue;
            }

            var item = SpawnItem<UIItemObject>(varChessItemUI, varChessPanel.transform);
            var chessItemUI = item.itemLogic as ChessItemUI;

            if (chessItemUI != null)
            {
                // 设置数据和回调
                chessItemUI.SetData(
                    instance.InstanceId,
                    instance.ChessId,
                    OnChessItemSelected, // 点击回调（选中/取消选中）
                    OnChessItemDragEnd, // 拖拽结束回调
                    OnChessItemDragBegin // 拖拽开始回调
                );

                // 设置出战状态
                chessItemUI.SetDeployedState();

                // 保存到字典
                m_ChessItemUIDict[instance.InstanceId] = chessItemUI;

                // 添加到容器并播放进场动画
                await m_ChessSlotContainer.AddCardAsync(chessItemUI);
            }

            m_SpawnedChessItems.Add(item.gameObject);
        }

        Log.Info($"CombatPreparationUI: 棋子面板初始化完成，共 {m_SpawnedChessItems.Count} 个棋子");
    }

    /// <summary>
    /// 棋子选中回调（点击时触发，实现选中/取消选中切换）
    /// ⭐ 修改：支持已出战棋子的点击（显示详情）
    /// </summary>
    private void OnChessItemSelected(string instanceId)
    {
        Log.Info($"CombatPreparationUI: 棋子点击 instanceId={instanceId}");

        if (string.IsNullOrEmpty(instanceId))
            return;

        // 获取棋子实例数据
        var instance = ChessDeploymentTracker.Instance.GetInstance(instanceId);
        if (instance == null)
        {
            DebugEx.ErrorModule("CombatPreparationUI", $"无法找到棋子实例: {instanceId}");
            return;
        }

        // ⭐ 已出战的棋子只显示详情，不支持选中/取消选中切换
        if (instance.IsDeployed)
        {
            Log.Info($"CombatPreparationUI: 棋子已出战，仅显示详情 instanceId={instanceId}");

            // 直接显示详情（无动效）
            if (varDetailInfoUI != null)
            {
                var detailUI = varDetailInfoUI.GetComponent<DetailInfoUI>();
                if (detailUI != null)
                {
                    if (ChessDataManager.Instance.TryGetConfig(instance.ChessId, out var config))
                    {
                        var globalState = GlobalChessManager.Instance.GetChessState(
                            instance.ChessId
                        );
                        if (globalState != null)
                        {
                            detailUI.SetChessConfig(config, globalState);
                            detailUI.RefreshUI();
                            detailUI.ShowWithAnimation();
                        }
                    }
                }
            }
            return;
        }

        // 未出战的棋子支持选中/取消选中切换
        // 如果点击的是已选中的棋子，则取消选中
        if (m_SelectedChessInstanceId == instanceId)
        {
            DeselectChess();
            return;
        }

        // 取消之前选中的棋子
        if (!string.IsNullOrEmpty(m_SelectedChessInstanceId))
        {
            DeselectChess();
        }

        // 选中新棋子
        SelectChess(instanceId);
    }

    /// <summary>
    /// 选中棋子
    /// </summary>
    private void SelectChess(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return;

        m_SelectedChessInstanceId = instanceId;

        // 获取棋子实例数据
        var instance = ChessDeploymentTracker.Instance.GetInstance(instanceId);
        if (instance == null)
        {
            DebugEx.ErrorModule("CombatPreparationUI", $"无法找到棋子实例: {instanceId}");
            m_SelectedChessInstanceId = string.Empty;
            return;
        }

        Log.Info($"CombatPreparationUI: 选中棋子 instanceId={instanceId}");

        // 显示棋子信息到 DetailInfoUI
        if (varDetailInfoUI != null)
        {
            var detailUI = varDetailInfoUI.GetComponent<DetailInfoUI>();
            if (detailUI != null)
            {
                // ⭐ 修改：棋子全局状态已在 InGameState.OnEnter() 时初始化，直接使用
                if (ChessDataManager.Instance.TryGetConfig(instance.ChessId, out var config))
                {
                    var globalState = GlobalChessManager.Instance.GetChessState(instance.ChessId);

                    if (globalState != null)
                    {
                        detailUI.SetChessConfig(config, globalState);
                        detailUI.RefreshUI();
                        detailUI.ShowWithAnimation();
                    }
                    else
                    {
                        // 不应该出现这个情况（如果出现说明InGameState初始化有问题）
                        DebugEx.ErrorModule(
                            "CombatPreparationUI",
                            $"棋子 {config.Name} (ID={instance.ChessId}) 全局状态未初始化"
                        );
                    }
                }
            }
        }

        // 播放选中动效
        PlayChessSelectAnimation(instanceId);
    }

    /// <summary>
    /// 取消选中棋子
    /// </summary>
    private void DeselectChess()
    {
        if (string.IsNullOrEmpty(m_SelectedChessInstanceId))
            return;

        var previousInstanceId = m_SelectedChessInstanceId;
        m_SelectedChessInstanceId = string.Empty;

        Log.Info($"CombatPreparationUI: 取消选中棋子 instanceId={previousInstanceId}");

        // 播放取消选中动效
        PlayChessDeselectAnimation(previousInstanceId);

        // 隐藏 DetailInfoUI（带滑出动画）
        if (varDetailInfoUI != null)
        {
            if (varDetailInfoUI.TryGetComponent<DetailInfoUI>(out var detailUI))
                detailUI.HideWithAnimation();
            else
                varDetailInfoUI.SetActive(false);
        }
    }

    /// <summary>
    /// 播放棋子选中动效（参考策略卡）
    /// ⭐ 修改：使用容器的基准位置计算目标位置
    /// </summary>
    private void PlayChessSelectAnimation(string instanceId)
    {
        if (!m_ChessItemUIDict.TryGetValue(instanceId, out var chessItemUI))
            return;

        var varBtn = chessItemUI.GetComponent<Button>();
        if (varBtn == null)
            return;

        var btnRectTransform = varBtn.GetComponent<RectTransform>();
        if (btnRectTransform == null)
            return;

        // 基于容器保存的基准位置计算目标位置
        Vector2 basePos = chessItemUI.GetBaseAnchoredPos();
        Vector3 targetPos = basePos + Vector2.up * SELECTED_OFFSET;

        // 播放位置动画
        btnRectTransform.DOAnchorPos(targetPos, SELECTED_ANIMATION_DURATION).SetEase(Ease.OutQuad);

        // 播放脉冲动效
        var btnTransform = varBtn.transform;
        var sequence = DOTween.Sequence();
        sequence.Append(
            btnTransform
                .DOScale(new Vector3(1.1f, 1.1f, 1f), SELECTED_ANIMATION_DURATION * 0.5f)
                .SetEase(Ease.OutQuad)
        );
        sequence.Append(
            btnTransform
                .DOScale(Vector3.one, SELECTED_ANIMATION_DURATION * 0.5f)
                .SetEase(Ease.InQuad)
        );

        DebugEx.LogModule("CombatPreparationUI", $"播放选中动效: instanceId={instanceId}");
    }

    /// <summary>
    /// 播放棋子取消选中动效
    /// ⭐ 修改：使用容器的基准位置恢复到原始位置
    /// </summary>
    private void PlayChessDeselectAnimation(string instanceId)
    {
        if (!m_ChessItemUIDict.TryGetValue(instanceId, out var chessItemUI))
            return;

        var varBtn = chessItemUI.GetComponent<Button>();
        if (varBtn == null)
            return;

        var btnRectTransform = varBtn.GetComponent<RectTransform>();
        if (btnRectTransform == null)
            return;

        // 恢复到容器的基准位置
        Vector2 basePos = chessItemUI.GetBaseAnchoredPos();
        btnRectTransform.DOAnchorPos(basePos, SELECTED_ANIMATION_DURATION).SetEase(Ease.OutQuad);

        // 恢复缩放
        var btnTransform = varBtn.transform;
        btnTransform.DOScale(Vector3.one, SELECTED_ANIMATION_DURATION).SetEase(Ease.OutQuad);

        DebugEx.LogModule("CombatPreparationUI", $"播放取消选中动效: instanceId={instanceId}");
    }

    /// <summary>
    /// 棋子拖拽开始回调（进入放置模式）
    /// ⭐ 修改：启动放置系统，显示Ghost预览
    /// </summary>
    private void OnChessItemDragBegin(string instanceId)
    {
        Log.Info($"CombatPreparationUI: 拖拽开始 instanceId={instanceId}");

        // 启动放置系统（拖拽模式）
        if (ChessPlacementManager.Instance != null)
        {
            ChessPlacementManager.Instance.StartPlacement(instanceId, isDragMode: true);
        }
    }

    /// <summary>
    /// 棋子拖拽结束回调
    /// ⭐ 修改：直接确认放置，无需再点击
    /// </summary>
    private void OnChessItemDragEnd(string instanceId)
    {
        Log.Info($"CombatPreparationUI: 拖拽结束 instanceId={instanceId}");

        // 拖拽结束时直接确认放置（如果在有效区域）
        if (ChessPlacementManager.Instance != null)
        {
            ChessPlacementManager.Instance.ConfirmPlacementFromDrag();
        }
    }

    #endregion

    #region 倒计时与准备

    /// <summary>
    /// 更新倒计时文本
    /// </summary>
    private void UpdateCountdownText()
    {
        if (varCountdownText != null)
        {
            int seconds = Mathf.CeilToInt(m_CountdownRemain);
            varCountdownText.text = $"{seconds}s";
        }
    }

    /// <summary>
    /// 更新准备按钮状态
    /// </summary>
    private void UpdateReadyButton(bool isReady)
    {
        if (varReadyTxt != null)
        {
            varReadyTxt.text = isReady ? "已准备" : "准备完毕";
        }

        if (varReadyBtn != null)
        {
            varReadyBtn.interactable = !isReady;
        }
    }

    /// <summary>
    /// 准备完毕（玩家点击或倒计时结束）
    /// </summary>
    private void OnPreparationComplete()
    {
        if (m_IsReady)
            return;

        m_IsReady = true;
        UpdateReadyButton(true);

        Log.Info("CombatPreparationUI: 准备完毕，通知战斗系统");

        // 触发准备完成事件
        GF.Event.Fire(this, ReferencePool.Acquire<CombatPreparationReadyEventArgs>());

        // 关闭准备界面
        GF.UI.CloseUIForm(this.UIForm);
    }

    #endregion

    #region Buff 选择（先手Buff/偷袭Debuff）

    /// <summary>
    /// 显示偷袭Debuff三选一
    /// </summary>
    public void ShowSneakDebuffSelection(List<int> debuffIds)
    {
        m_AvailableBuffIds = debuffIds;
        m_BuffSelectionMode = BuffSelectionMode.SneakDebuff;
        ShowBuffSelectionPanel();
        SetupBuffSelectionOptions("选择一个Debuff应用到敌人");
    }

    /// <summary>
    /// 显示先手Buff三选一（5秒后自动选择第一个）
    /// </summary>
    public void ShowInitiativeBuffSelection(List<int> buffIds)
    {
        m_AvailableBuffIds = buffIds;
        m_BuffSelectionMode = BuffSelectionMode.InitiativeBuff;
        ShowBuffSelectionPanel();
        SetupBuffSelectionOptions("选择一个先手Buff");

        // 启动5秒自动选择计时
        m_BuffSelectionCts?.Cancel();
        m_BuffSelectionCts?.Dispose();
        m_BuffSelectionCts = new System.Threading.CancellationTokenSource();
        AutoSelectBuffAfterTimeoutAsync(5f, m_BuffSelectionCts.Token).Forget();
    }

    /// <summary>
    /// 显示Buff选择面板
    /// </summary>
    private void ShowBuffSelectionPanel()
    {
        if (varBuffSelection != null)
        {
            varBuffSelection.alpha = 1f;
            varBuffSelection.interactable = true;
            varBuffSelection.blocksRaycasts = true;
            Log.Info("CombatPreparationUI: Buff选择面板已显示");
        }
    }

    /// <summary>
    /// 隐藏Buff选择面板
    /// </summary>
    private void HideBuffSelectionPanel()
    {
        if (varBuffSelection != null)
        {
            varBuffSelection.alpha = 0f;
            varBuffSelection.interactable = false;
            varBuffSelection.blocksRaycasts = false;
            Log.Info("CombatPreparationUI: Buff选择面板已隐藏");
        }
    }

    /// <summary>
    /// 显示敌方先手Buff通知
    /// </summary>
    public void ShowEnemyInitiativeBuffNotification(int buffId)
    {
        // 取消之前的通知
        if (m_NotificationCts != null)
        {
            m_NotificationCts.Cancel();
            m_NotificationCts.Dispose();
        }

        m_NotificationCts = new System.Threading.CancellationTokenSource();
        DisplayEnemyBuffNotificationAsync(buffId, m_NotificationCts.Token);
    }

    /// <summary>
    /// 设置效果选择选项UI
    /// </summary>
    private void SetupBuffSelectionOptions(string title)
    {
        if (m_AvailableBuffIds == null || m_AvailableBuffIds.Count == 0)
        {
            DebugEx.WarningModule("CombatPreparationUI", "没有可用的效果");
            return;
        }

        var specialEffectTable = GF.DataTable.GetDataTable<SpecialEffectTable>();
        if (specialEffectTable == null)
        {
            DebugEx.WarningModule("CombatPreparationUI", "SpecialEffectTable未加载");
            return;
        }

        // 清空之前的选项
        ClearBuffChooseItems();

        if (varBuffChooseItem == null || varPanel == null)
        {
            DebugEx.ErrorModule("CombatPreparationUI", "varBuffChooseItem 或 varPanel 为空");
            return;
        }

        // 隐藏模板
        varBuffChooseItem.SetActive(false);

        int count = Mathf.Min(m_AvailableBuffIds.Count, 3);
        for (int i = 0; i < count; i++)
        {
            int effectId = m_AvailableBuffIds[i];
            var effect = specialEffectTable.GetDataRow(effectId);

            if (effect == null)
            {
                DebugEx.WarningModule("CombatPreparationUI", $"特殊效果配置未找到: ID={effectId}");
                continue;
            }

            // 实例化选项
            GameObject itemGo = Object.Instantiate(varBuffChooseItem, varPanel.transform);
            itemGo.SetActive(true);

            var chooseItem = itemGo.GetComponent<BuffChooseItem>();
            if (chooseItem != null)
            {
                chooseItem.SetEffectData(effectId, effect);
            }

            m_BuffChooseItems.Add(itemGo);
        }

        Log.Info($"CombatPreparationUI: {title} - 已生成 {m_BuffChooseItems.Count} 个选项");
    }

    /// <summary>
    /// 清空已生成的 BuffChooseItem
    /// </summary>
    private void ClearBuffChooseItems()
    {
        foreach (var item in m_BuffChooseItems)
        {
            if (item != null)
            {
                Object.Destroy(item);
            }
        }
        m_BuffChooseItems.Clear();
    }

    /// <summary>
    /// 超时自动选择第一个Buff
    /// </summary>
    private async UniTaskVoid AutoSelectBuffAfterTimeoutAsync(float timeout, System.Threading.CancellationToken ct)
    {
        try
        {
            int totalMs = (int)(timeout * 1000);
            await UniTask.Delay(totalMs, cancellationToken: ct);

            // 超时未选择，自动选择第一个
            if (m_AvailableBuffIds != null && m_AvailableBuffIds.Count > 0 && m_BuffSelectionMode != BuffSelectionMode.None)
            {
                int firstBuffId = m_AvailableBuffIds[0];
                DebugEx.LogModule("CombatPreparationUI", $"选择超时，自动选择第一个Buff: {firstBuffId}");
                OnBuffItemSelected(firstBuffId);
            }
        }
        catch (System.OperationCanceledException)
        {
            // 玩家已手动选择，正常取消
        }
    }

    /// <summary>
    /// Buff选项被选中
    /// </summary>
    public void OnBuffItemSelected(int buffId)
    {
        m_SelectedBuffId = buffId;
        Log.Info($"CombatPreparationUI: 选中Buff {buffId}，模式={m_BuffSelectionMode}");

        // 取消自动选择计时
        m_BuffSelectionCts?.Cancel();
        m_BuffSelectionCts?.Dispose();
        m_BuffSelectionCts = null;

        // 应用Buff
        ApplySelectedBuff();

        // 清理选项并隐藏选择面板
        ClearBuffChooseItems();
        HideBuffSelectionPanel();
        m_BuffSelectionMode = BuffSelectionMode.None;
    }

    /// <summary>
    /// 应用选中的效果（延迟应用：存储到Context，战斗开始后由CombatState统一应用）
    /// 原因：准备阶段敌方棋子尚未生成，无法直接应用Buff
    /// </summary>
    private void ApplySelectedBuff()
    {
        if (m_SelectedBuffId <= 0)
            return;

        // 将选中的效果ID存储到战斗上下文，等战斗开始后棋子就绪时再应用
        var context = CombatTriggerManager.Instance?.CurrentContext;
        if (context != null)
        {
            context.SelectedEffectId = m_SelectedBuffId;
            DebugEx.LogModule("CombatPreparationUI",
                $"已存储选中效果到战斗上下文: EffectId={m_SelectedBuffId}, 模式={m_BuffSelectionMode}, 将在棋子就绪后应用");
        }
        else
        {
            DebugEx.WarningModule("CombatPreparationUI",
                $"战斗上下文为空，无法存储选中效果: EffectId={m_SelectedBuffId}");
        }
    }

    /// <summary>
    /// 从特殊效果中应用所有包含的Buff到目标
    /// BuffIds: 应用到目标方（全体）
    /// SelfBuffIds: 玩家偷袭时应用到敌人的自身Buff
    /// </summary>
    private void ApplyBuffsFromSpecialEffect(SpecialEffectTable effect, EnemyEntity targetEnemy)
    {
        if (effect == null || targetEnemy == null)
            return;

        // 应用给目标的Buff（BuffIds）- 偷袭效果的主要debuff
        if (effect.BuffIds != null && effect.BuffIds.Length > 0)
        {
            foreach (int buffId in effect.BuffIds)
            {
                if (buffId > 0)
                {
                    BuffApplyHelper.ApplyBuff(buffId, targetEnemy.gameObject, true, null);
                    Log.Info($"CombatPreparationUI:   应用Buff {buffId} 到敌人(全体-TargetBuff)");
                }
            }
        }

        // 应用给自身的Buff（SelfBuffIds）- 玩家获得的增益buff
        if (effect.SelfBuffIds != null && effect.SelfBuffIds.Length > 0)
        {
            foreach (int buffId in effect.SelfBuffIds)
            {
                if (buffId > 0)
                {
                    // TODO: 获取玩家实体，应用Buff到玩家方（全体）
                    // BuffApplyHelper.ApplyBuff(buffId, playerEntity, true, null);
                    Log.Info($"CombatPreparationUI:   应用Buff {buffId} 到玩家(全体-SelfBuff)");
                }
            }
        }
    }

    /// <summary>
    /// 异步显示敌方先手效果通知
    /// </summary>
    private async void DisplayEnemyBuffNotificationAsync(
        int effectId,
        System.Threading.CancellationToken cancellationToken
    )
    {
        try
        {
            var specialEffectTable = GF.DataTable.GetDataTable<SpecialEffectTable>();
            if (specialEffectTable == null)
            {
                Log.Error($"[DisplayEnemyBuffNotificationAsync] SpecialEffectTable 未加载");
                return;
            }

            var effect = specialEffectTable.GetDataRow(effectId);
            if (effect == null)
            {
                Log.Error($"[DisplayEnemyBuffNotificationAsync] 未找到 EffectId={effectId}");
                return;
            }

            // 显示敌方先手效果通知UI
            if (varInitiativeBuffNotification != null)
            {
                // 更新文本显示效果信息
                if (varBuffName != null)
                    varBuffName.text = effect.Name;
                if (varBuffDescription != null)
                    varBuffDescription.text = effect.Description;

                // 加载和设置Icon到Image对象（使用UniTask版本）
                if (varBuffIcon != null && effect.IconId > 0)
                {
                    await GameExtension.ResourceExtension.LoadSpriteAsync(
                        effect.IconId,
                        varBuffIcon,
                        1f,
                        null
                    );
                }

                // 淡入动画（0 -> 1）
                varInitiativeBuffNotification.alpha = 0f;
                float fadeInDuration = 0.3f;
                float elapsed = 0f;
                while (elapsed < fadeInDuration && !cancellationToken.IsCancellationRequested)
                {
                    elapsed += Time.deltaTime;
                    varInitiativeBuffNotification.alpha = Mathf.Lerp(
                        0f,
                        1f,
                        elapsed / fadeInDuration
                    );
                    await UniTask.Yield(cancellationToken: cancellationToken);
                }
                varInitiativeBuffNotification.alpha = 1f;

                Log.Info($"CombatPreparationUI: 敌方先手效果显示 {effect.Name}");

                // 显示3秒钟
                await UniTask.Delay(3000, cancellationToken: cancellationToken);

                // 淡出动画（1 -> 0）
                float fadeOutDuration = 0.3f;
                elapsed = 0f;
                while (elapsed < fadeOutDuration && !cancellationToken.IsCancellationRequested)
                {
                    elapsed += Time.deltaTime;
                    varInitiativeBuffNotification.alpha = Mathf.Lerp(
                        1f,
                        0f,
                        elapsed / fadeOutDuration
                    );
                    await UniTask.Yield(cancellationToken: cancellationToken);
                }
                varInitiativeBuffNotification.alpha = 0f;

                Log.Info($"CombatPreparationUI: 敌方Buff通知隐藏");
            }
        }
        catch (System.OperationCanceledException)
        {
            Log.Info("CombatPreparationUI: 敌方Buff通知被取消");
        }
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清理所有已生成的UI项
    /// </summary>
    private void ClearSpawnedItems()
    {
        ClearChessItems();

        // 清理选中状态
        m_SelectedChessInstanceId = string.Empty;

        // 清理Buff通知任务
        if (m_NotificationCts != null)
        {
            m_NotificationCts.Cancel();
            m_NotificationCts.Dispose();
            m_NotificationCts = null;
        }
    }

    /// <summary>
    /// 清理棋子UI项
    /// </summary>
    private void ClearChessItems()
    {
        // 先清理容器状态（动画、列表）
        if (m_ChessSlotContainer != null)
        {
            m_ChessSlotContainer.ClearState();
        }

        if (varChessItemUI != null && m_SpawnedChessItems.Count > 0)
        {
            UnspawnAllItem<UIItemObject>(varChessItemUI);
            m_SpawnedChessItems.Clear();
            m_ChessItemUIDict.Clear();
        }
    }

    #endregion

    #region 场景棋子选择事件处理

    /// <summary>
    /// ⭐ 新增：场景棋子被选中时的处理（点击场景中的棋子时显示详情）
    /// </summary>
    private void OnSceneChessSelectedHandler(ChessEntity entity)
    {
        if (entity == null)
            return;

        m_CurrentDetailChess = entity;
        Log.Info($"CombatPreparationUI: 场景棋子被选中 chessId={entity.Config.Name}");

        // 订阅棋子属性变化事件，实现动态更新
        if (entity.Attribute != null)
        {
            entity.Attribute.OnHpChanged += OnDetailChessHpChanged;
            Log.Info("CombatPreparationUI: 已订阅棋子HP变化事件");
        }

        // 订阅Buff变化事件
        ChessStateEvents.OnBuffAdded += OnDetailChessBuffChanged;
        ChessStateEvents.OnBuffRemoved += OnDetailChessBuffChanged;
        Log.Info("CombatPreparationUI: 已订阅棋子Buff变化事件");

        // 显示棋子详情
        if (varDetailInfoUI != null)
        {
            var detailUI = varDetailInfoUI.GetComponent<DetailInfoUI>();
            if (detailUI != null)
            {
                if (ChessDataManager.Instance.TryGetConfig(entity.Config.Id, out var config))
                {
                    var globalState = GlobalChessManager.Instance.GetChessState(config.Id);
                    if (globalState != null)
                    {
                        detailUI.SetChessConfig(config, globalState);
                        // ⭐ 新增：关联 ChessEntity 以显示实时属性
                        detailUI.SetChessEntityForPreparation(entity);
                        detailUI.RefreshUI();
                        detailUI.ShowWithAnimation();
                        Log.Info(
                            $"CombatPreparationUI: 显示棋子详情 {config.Name}（已关联实体用于实时显示）"
                        );
                    }
                }
            }
        }
    }

    /// <summary>
    /// ⭐ 新增：棋子HP变化时，动态更新DetailInfoUI
    /// </summary>
    private void OnDetailChessHpChanged(double oldHp, double newHp)
    {
        if (m_CurrentDetailChess == null || varDetailInfoUI == null)
            return;

        var detailUI = varDetailInfoUI.GetComponent<DetailInfoUI>();
        if (detailUI != null)
        {
            detailUI.RefreshUI();
            Log.Info($"CombatPreparationUI: DetailInfoUI已刷新（HP变化 {oldHp:F0} -> {newHp:F0}）");
        }
    }

    /// <summary>
    /// ⭐ 新增：棋子Buff变化时，动态更新DetailInfoUI
    /// </summary>
    private void OnDetailChessBuffChanged(int chessId, int buffId)
    {
        if (m_CurrentDetailChess == null || varDetailInfoUI == null)
            return;

        // 只更新当前显示的棋子
        if (m_CurrentDetailChess.Config.Id != chessId)
            return;

        var detailUI = varDetailInfoUI.GetComponent<DetailInfoUI>();
        if (detailUI != null)
        {
            detailUI.RefreshUI();
            Log.Info($"CombatPreparationUI: DetailInfoUI已刷新（Buff变化 ID={buffId}）");
        }
    }

    /// <summary>
    /// ⭐ 新增：场景棋子被取消选中时的处理
    /// </summary>
    private void OnSceneChessDeselectedHandler()
    {
        Log.Info("CombatPreparationUI: 场景棋子被取消选中");

        // 取消订阅属性变化事件
        if (m_CurrentDetailChess != null && m_CurrentDetailChess.Attribute != null)
        {
            m_CurrentDetailChess.Attribute.OnHpChanged -= OnDetailChessHpChanged;
            Log.Info("CombatPreparationUI: 已取消订阅棋子HP变化事件");
        }

        ChessStateEvents.OnBuffAdded -= OnDetailChessBuffChanged;
        ChessStateEvents.OnBuffRemoved -= OnDetailChessBuffChanged;
        Log.Info("CombatPreparationUI: 已取消订阅棋子Buff变化事件");

        m_CurrentDetailChess = null;

        // 隐藏棋子详情（带滑出动画）
        if (varDetailInfoUI != null)
        {
            if (varDetailInfoUI.TryGetComponent<DetailInfoUI>(out var detailUI))
                detailUI.HideWithAnimation();
            else
                varDetailInfoUI.SetActive(false);
        }
    }

    #endregion

    #region 库存事件处理

    /// <summary>
    /// 棋子出战事件处理
    /// </summary>
    private void OnChessDeployedHandler(ChessDeploymentTracker.ChessInstanceData instance)
    {
        Log.Info($"CombatPreparationUI: 棋子已部署 instanceId={instance.InstanceId}");

        // 如果部署的是当前选中的棋子，清除选中状态并隐藏 DetailInfoUI
        if (m_SelectedChessInstanceId == instance.InstanceId)
        {
            DeselectChess();
        }

        // 刷新棋子面板
        RefreshChessPanel();
    }

    /// <summary>
    /// 棋子撤回事件处理
    /// </summary>
    private void OnChessRecalledHandler(ChessDeploymentTracker.ChessInstanceData instance)
    {
        Log.Info($"CombatPreparationUI: 棋子已撤回 instanceId={instance.InstanceId}");
        // 刷新棋子面板
        RefreshChessPanel();
    }

    #endregion
}
