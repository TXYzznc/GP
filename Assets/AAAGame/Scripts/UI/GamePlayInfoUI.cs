using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameFramework.Event;
using DG.Tweening;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class GamePlayInfoUI : StateAwareUIForm
{
    /// <summary>当前订阅的隐身组件（用于取消订阅）</summary>
    private PostCombatStealth m_SubscribedStealth;
    #region 事件订阅

    protected override void SubscribeEvents()
    {
        DebugEx.LogModule("GamePlayInfoUI", "订阅局内、局外和战斗状态事件");
        // 订阅局外事件（进入基地 → 显示）
        GF.Event.Subscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Subscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);

        // 订阅局内事件（进入 → 显示）
        GF.Event.Subscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Subscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 订阅探索事件（探索 → 战斗准备/战斗）
        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);

        // 订阅战斗事件（探索 → 战斗）
        GF.Event.Subscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Subscribe(CombatEndEventArgs.EventId, OnCombatEnd);
        GF.Event.Subscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);

        // 订阅运行时数据变化事件
        SubscribeRuntimeDataEvents();
    }

    protected override void UnsubscribeEvents()
    {
        DebugEx.LogModule("GamePlayInfoUI", "取消订阅局内和战斗状态事件");
        // 取消订阅局外事件
        GF.Event.Unsubscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Unsubscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);

        // 取消订阅局内事件
        GF.Event.Unsubscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Unsubscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 取消订阅探索事件
        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);

        // 取消订阅战斗事件
        GF.Event.Unsubscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Unsubscribe(CombatEndEventArgs.EventId, OnCombatEnd);
        GF.Event.Unsubscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);

        // 取消订阅运行时数据变化事件
        UnsubscribeRuntimeDataEvents();
    }

    /// <summary>
    /// 订阅运行时数据变化事件
    /// </summary>
    private void SubscribeRuntimeDataEvents()
    {
        // 订阅玩家污染值变化事件（用于HP滑条显示）
        if (PlayerRuntimeDataManager.Instance != null)
        {
            PlayerRuntimeDataManager.Instance.OnCorruptionChanged += OnCorruptionChanged;
        }
    }

    /// <summary>
    /// 取消订阅运行时数据变化事件
    /// </summary>
    private void UnsubscribeRuntimeDataEvents()
    {
        // 取消订阅玩家污染值变化事件
        if (PlayerRuntimeDataManager.Instance != null)
        {
            PlayerRuntimeDataManager.Instance.OnCorruptionChanged -= OnCorruptionChanged;
        }
    }

    /// <summary>
    /// 初始化警示UI系统
    /// </summary>
    private void InitializeAlertUISystem()
    {
        // 获取警示指示器容器（varEnemyWarningHead）
        if (varEnemyWarningHead != null && varEnemyMask != null)
        {
            // 初始化警示UI管理器
            var alertUIManager = EnemyAlertUIManager.Instance;
            if (alertUIManager != null)
            {
                // varEnemyMask是指示器的模板预制体，需要获取它的引用
                EnemyMask indicatorTemplate = varEnemyMask.GetComponent<EnemyMask>();
                if (indicatorTemplate != null)
                {
                    // varEnemyWarningHead是HorizontalLayoutGroup，需要获取其RectTransform
                    RectTransform containerTransform = varEnemyWarningHead.GetComponent<RectTransform>();
                    if (containerTransform != null)
                    {
                        alertUIManager.Initialize(containerTransform, indicatorTemplate);
                        DebugEx.LogModule("GamePlayInfoUI", "警示UI系统已初始化");
                    }
                    else
                    {
                        DebugEx.WarningModule("GamePlayInfoUI", "varEnemyWarningHead上未找到RectTransform组件");
                    }
                }
                else
                {
                    DebugEx.WarningModule("GamePlayInfoUI", "varEnemyMask上未找到EnemyMask组件");
                }
            }
        }
        else
        {
            DebugEx.WarningModule("GamePlayInfoUI", "varEnemyWarningHead 或 varEnemyMask 未设置");
        }

        // 初始化时隐藏战斗交互UI（正常情况下应隐藏，只在触发时显示）
        HideCombatInteract();
    }

    /// <summary>
    /// 显示战斗交互UI（偷袭或遭遇战）
    /// </summary>
    public void ShowCombatInteract(CombatTriggerType triggerType)
    {
        if (varCombatInteractUI == null || varInteractIcon == null || varInteractText == null)
        {
            DebugEx.WarningModule("GamePlayInfoUI", "战斗交互UI组件未设置");
            return;
        }

        varCombatInteractUI.gameObject.SetActive(true);

        switch (triggerType)
        {
            case CombatTriggerType.SneakAttack:
                // 设置偷袭图标和文本
                // varInteractIcon.sprite = LoadSprite("Icon_SneakAttack");
                varInteractText.text = "按下空格进行偷袭";
                DebugEx.LogModule("GamePlayInfoUI", "显示偷袭交互UI");
                break;

            case CombatTriggerType.Encounter:
                // 设置遭遇战图标和文本
                // varInteractIcon.sprite = LoadSprite("Icon_Encounter");
                varInteractText.text = "按下空格进行战斗";
                DebugEx.LogModule("GamePlayInfoUI", "显示遭遇战交互UI");
                break;

            default:
                varInteractText.text = "";
                break;
        }

        // TODO: 播放跳动动画（使用DOTween或Animator）
        // PlayBounceAnimation(varInteractIcon.transform);
    }

    /// <summary>
    /// 隐藏战斗交互UI
    /// </summary>
    public void HideCombatInteract()
    {
        if (varCombatInteractUI != null)
        {
            varCombatInteractUI.gameObject.SetActive(false);
            DebugEx.LogModule("GamePlayInfoUI", "隐藏战斗交互UI");
        }
    }

    #endregion

    #region 事件处理

    private void OnOutOfGameEnter(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("GamePlayInfoUI", "收到局外进入事件 → 显示UI");
        ShowUI();
        RefreshGameInfo();
    }

    private void OnOutOfGameLeave(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("GamePlayInfoUI", "收到局外离开事件 → 隐藏UI");
        HideUI();
    }

    private void OnInGameEnter(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("GamePlayInfoUI", "收到局内进入事件 → 显示UI");
        ShowUI();
        RefreshGameInfo();
    }

    private void OnInGameLeave(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("GamePlayInfoUI", "收到局内离开事件 → 隐藏UI");
        HideUI();
    }

    private void OnExplorationEnter(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("GamePlayInfoUI", "收到探索进入事件 → 显示UI");
        ShowUI();
        RefreshGameInfo();

        // 初始化警示UI系统（在进入探索时）
        InitializeAlertUISystem();
    }

    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("GamePlayInfoUI", "收到探索离开事件 → 隐藏UI");
        UnsubscribeStealthEvents();
        HideUI();

        // 隐藏战斗交互UI
        HideCombatInteract();

        // 清空警示UI
        if (EnemyAlertUIManager.Instance != null)
        {
            EnemyAlertUIManager.Instance.ClearAll();
        }
    }

    private void OnCombatEnter(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("GamePlayInfoUI", "收到战斗进入事件 → 隐藏UI");
        HideUI();
    }

    private void OnCombatEnd(object sender, GameEventArgs e)
    {
        // 战斗结束时先订阅，等 UI 显示后（OnCombatLeave）再正式激活计时
        SubscribeStealthEvents();
    }

    private void OnCombatLeave(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("GamePlayInfoUI", "收到战斗离开事件 → 显示UI");
        ShowUI();
        RefreshGameInfo();
        // UI 已可见，正式激活隐身（开始计时 + 显示 StealthText）
        m_SubscribedStealth?.Activate();
    }

    /// <summary>
    /// 订阅玩家隐身状态事件（战斗结束返回探索时调用）
    /// </summary>
    private void SubscribeStealthEvents()
    {
        // 先取消旧订阅
        UnsubscribeStealthEvents();

        var playerGo = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        if (playerGo == null) return;

        m_SubscribedStealth = playerGo.GetComponent<PostCombatStealth>();
        if (m_SubscribedStealth != null)
            m_SubscribedStealth.OnStealthChanged += OnStealthChanged;
    }

    private void UnsubscribeStealthEvents()
    {
        if (m_SubscribedStealth != null)
        {
            m_SubscribedStealth.OnStealthChanged -= OnStealthChanged;
            m_SubscribedStealth = null;
        }
    }

    private void OnStealthChanged(bool isActive)
    {
        if (varStealthText != null)
            varStealthText.gameObject.SetActive(isActive);
    }

    /// <summary>
    /// 污染值变化回调
    /// </summary>
    private void OnCorruptionChanged(float oldValue, float newValue)
    {
        RefreshCorruption();
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新游戏信息
    /// </summary>
    private void RefreshGameInfo()
    {
        // 刷新地点信息 - 从当前场景获取
        if (varLocationText != null)
        {
            string locationName = GetCurrentSceneDisplayName();
            varLocationText.text = locationName;
        }

        // 刷新时间信息
        if (varTimeText != null)
        {
            varTimeText.text = "白天";  // TODO: 从游戏数据获取
        }

        // 刷新天气信息
        if (varWeatherText != null)
        {
            varWeatherText.text = "晴";  // TODO: 从游戏数据获取
        }

        // 隐身文本：根据当前隐身状态决定显示
        if (varStealthText != null)
        {
            bool stealthActive = m_SubscribedStealth != null && m_SubscribedStealth.IsRunning;
            varStealthText.gameObject.SetActive(stealthActive);
        }

        // 刷新污染值（使用HP滑条显示）
        RefreshCorruption();

        DebugEx.LogModule("GamePlayInfoUI", "游戏信息已刷新");
    }

    /// <summary>
    /// 获取当前场景的显示名称（本地化）
    /// </summary>
    private string GetCurrentSceneDisplayName()
    {
        var currentSceneId = SceneStateManager.Instance.CurrentSceneId;
        if (currentSceneId <= 0)
            return "未知";

        var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
        if (sceneTable == null)
            return "未知";

        var sceneRow = sceneTable.GetDataRow(currentSceneId);
        if (sceneRow == null)
            return "未知";

        // 获取本地化字符串，如果本地化未设置则返回Key
        string localizationKey = sceneRow.DisplayName;
        string displayName = GF.Localization.GetString(localizationKey);

        return string.IsNullOrEmpty(displayName) ? localizationKey : displayName;
    }

    /// <summary>
    /// 刷新污染值显示（使用HP滑条）
    /// </summary>
    private void RefreshCorruption()
    {
        if (varHPSlider != null)
        {
            // 从玩家运行时数据获取污染值百分比
            if (PlayerRuntimeDataManager.Instance != null && PlayerRuntimeDataManager.Instance.IsInitialized)
            {
                varHPSlider.value = PlayerRuntimeDataManager.Instance.CorruptionPercent;
                // DebugEx.LogModule("GamePlayInfoUI", 
                //     $"污染值显示更新: {PlayerRuntimeDataManager.Instance.CurrentCorruption:F1}/{PlayerRuntimeDataManager.Instance.MaxCorruption:F1} ({PlayerRuntimeDataManager.Instance.CorruptionPercent:P1})");
            }
            else
            {
                varHPSlider.value = 0f;  // 默认无污染
            }
        }
    }

    #endregion

    #region 生命周期

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // 更新玩家污染值增长
        if (PlayerRuntimeDataManager.Instance != null && PlayerRuntimeDataManager.Instance.IsInitialized)
        {
            PlayerRuntimeDataManager.Instance.UpdateCorruptionGrowth(elapseSeconds);
        }

        // 更新隐身倒计时显示
        if (varStealthText != null && varStealthText.gameObject.activeSelf
            && m_SubscribedStealth != null && m_SubscribedStealth.IsActive)
        {
            varStealthText.text = $"隐身 {m_SubscribedStealth.RemainingTime:F0}s";
        }
    }

    #endregion

    #region 动画

    protected new void ShowUI()
    {
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        if (cg == null) { base.ShowUI(); return; }

        DOTween.Kill(gameObject);
        cg.alpha = 0f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
        var orig = rt.anchoredPosition;
        rt.anchoredPosition = orig + new Vector2(-80f, 0);
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuart))
            .Join(rt.DOAnchorPos(orig, 0.3f).SetEase(Ease.OutQuart));
    }

    protected new void HideUI()
    {
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        if (cg == null) { base.HideUI(); return; }

        DOTween.Kill(gameObject);
        var orig = rt.anchoredPosition;
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(0f, 0.2f).SetEase(Ease.InQuart))
            .Join(rt.DOAnchorPos(orig + new Vector2(-80f, 0), 0.2f).SetEase(Ease.InQuart))
            .OnComplete(() =>
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
            });
    }

    #endregion
}
