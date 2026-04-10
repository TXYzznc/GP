using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;

/// <summary>
/// 开始菜单UI
/// </summary>
public partial class StartMenuUI : UIFormBase
{
    #region 动画配置

    // 背景
    private const float BG_FADE_DURATION = 0.6f;

    // 标题
    private const float TITLE_OFFSET_Y = 50f;
    private const float TITLE_FADE_DURATION = 0.5f;
    private const float TITLE_CN_DELAY = 0.2f;
    private const float TITLE_EN_DELAY = 0.3f;

    // 按钮组
    private const float BTN_OFFSET_X = -80f;
    private const float BTN_FADE_DURATION = 0.4f;
    private const float BTN_GROUP_START_DELAY = 0.5f;
    private const float BTN_STAGGER_INTERVAL = 0.08f;

    // 云存档按钮
    private const float CLOUD_BTN_DELAY = 1.0f;
    private const float CLOUD_BTN_DURATION = 0.35f;
    private const float CLOUD_BTN_START_SCALE = 0.8f;

    #endregion

    #region 动画缓存

    private CanvasGroup m_BgCanvasGroup;
    private CanvasGroup m_TitleCnCanvasGroup;
    private CanvasGroup m_TitleEnCanvasGroup;
    private CanvasGroup m_CloudBtnCanvasGroup;

    private RectTransform m_TitleCnRect;
    private RectTransform m_TitleEnRect;
    private RectTransform m_CloudBtnRect;

    private Vector2 m_TitleCnOriginalPos;
    private Vector2 m_TitleEnOriginalPos;

    private struct ButtonAnimData
    {
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;
        public Vector2 originalPos;
    }

    private ButtonAnimData[] m_ButtonAnimDatas;

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 设置背景图和通过资源ID加载
        varImgBackground.SetSpriteById(ResourceIds.MENU_BACKGROUND);
        varName.SetSpriteById(ResourceIds.MENU_NAME);
        varNameEn.SetSpriteById(ResourceIds.MENU_NAME_EN);
        var存档上云.image.SetSpriteById(ResourceIds.MENU_YUN);

        // 缓存动画相关组件
        CacheAnimationComponents();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        Log.Info("StartMenuUI 已打开");

        // 检查是否有存档，决定是否启用"继续游戏"按钮
        CheckSaveData();
    }

    /// <summary>
    /// 重写开场动画完成回调 — 在此启动自定义入场动画
    /// base.OnOpen 会调用 Internal_PlayOpenUIAnimation → 当无 Inspector 动画时直接触发此回调
    /// </summary>
    protected override void OnOpenAnimationComplete()
    {
        // 不调用 base（base 会直接设 Interactable = true，我们需要等动画结束）
        Interactable = false;
        PlayOpenAnimation();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 立即清理所有动画，防止池回收后继续播放
        DOTween.Kill(this);
        // 还原所有元素状态，确保下次打开时初始状态正确
        ResetAnimationState();

        base.OnClose(isShutdown, userData);
    }

    #endregion

    #region 入场动画

    private void PlayOpenAnimation()
    {
        // 设置初始状态
        SetInitialState();

        // 构建主序列
        var sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);

        // 1. 背景淡入
        sequence.Join(
            m_BgCanvasGroup.DOFade(1f, BG_FADE_DURATION).SetEase(Ease.OutQuad)
        );

        // 2. 游戏名称（中文）— 从上方滑入 + 淡入
        sequence.Insert(TITLE_CN_DELAY,
            m_TitleCnRect.DOAnchorPos(m_TitleCnOriginalPos, TITLE_FADE_DURATION).SetEase(Ease.OutBack)
        );
        sequence.Insert(TITLE_CN_DELAY,
            m_TitleCnCanvasGroup.DOFade(1f, TITLE_FADE_DURATION).SetEase(Ease.OutQuad)
        );

        // 3. 英文名称 — 同上，略延迟
        sequence.Insert(TITLE_EN_DELAY,
            m_TitleEnRect.DOAnchorPos(m_TitleEnOriginalPos, TITLE_FADE_DURATION).SetEase(Ease.OutBack)
        );
        sequence.Insert(TITLE_EN_DELAY,
            m_TitleEnCanvasGroup.DOFade(1f, TITLE_FADE_DURATION).SetEase(Ease.OutQuad)
        );

        // 4. 按钮组 — 从左侧交错滑入 + 淡入
        for (int i = 0; i < m_ButtonAnimDatas.Length; i++)
        {
            float delay = BTN_GROUP_START_DELAY + i * BTN_STAGGER_INTERVAL;
            var data = m_ButtonAnimDatas[i];

            sequence.Insert(delay,
                data.rectTransform.DOAnchorPos(data.originalPos, BTN_FADE_DURATION).SetEase(Ease.OutQuad)
            );
            sequence.Insert(delay,
                data.canvasGroup.DOFade(1f, BTN_FADE_DURATION).SetEase(Ease.OutQuad)
            );
        }

        // 5. 云存档按钮 — 缩放弹出 + 淡入
        sequence.Insert(CLOUD_BTN_DELAY,
            m_CloudBtnRect.DOScale(1f, CLOUD_BTN_DURATION).SetEase(Ease.OutBack)
        );
        sequence.Insert(CLOUD_BTN_DELAY,
            m_CloudBtnCanvasGroup.DOFade(1f, CLOUD_BTN_DURATION).SetEase(Ease.OutQuad)
        );

        // 动画完成后恢复交互
        sequence.OnComplete(() =>
        {
            Interactable = true;
        });
    }

    /// <summary>
    /// 设置所有元素到动画起始状态
    /// </summary>
    private void SetInitialState()
    {
        // 背景透明
        m_BgCanvasGroup.alpha = 0f;

        // 标题：上移 + 透明
        m_TitleCnCanvasGroup.alpha = 0f;
        m_TitleCnRect.anchoredPosition = m_TitleCnOriginalPos + new Vector2(0f, TITLE_OFFSET_Y);

        m_TitleEnCanvasGroup.alpha = 0f;
        m_TitleEnRect.anchoredPosition = m_TitleEnOriginalPos + new Vector2(0f, TITLE_OFFSET_Y);

        // 按钮组：左移 + 透明
        for (int i = 0; i < m_ButtonAnimDatas.Length; i++)
        {
            var data = m_ButtonAnimDatas[i];
            data.canvasGroup.alpha = 0f;
            data.rectTransform.anchoredPosition = data.originalPos + new Vector2(BTN_OFFSET_X, 0f);
        }

        // 云存档按钮：缩小 + 透明
        m_CloudBtnCanvasGroup.alpha = 0f;
        m_CloudBtnRect.localScale = Vector3.one * CLOUD_BTN_START_SCALE;
    }

    #endregion

    #region 动画辅助

    /// <summary>
    /// 缓存动画所需的组件引用和原始位置
    /// </summary>
    private void CacheAnimationComponents()
    {
        // 背景
        m_BgCanvasGroup = varImgBackground.gameObject.GetOrAddComponent<CanvasGroup>();

        // 标题
        m_TitleCnRect = varName.GetComponent<RectTransform>();
        m_TitleCnCanvasGroup = varName.gameObject.GetOrAddComponent<CanvasGroup>();
        m_TitleCnOriginalPos = m_TitleCnRect.anchoredPosition;

        m_TitleEnRect = varNameEn.GetComponent<RectTransform>();
        m_TitleEnCanvasGroup = varNameEn.gameObject.GetOrAddComponent<CanvasGroup>();
        m_TitleEnOriginalPos = m_TitleEnRect.anchoredPosition;

        // 菜单按钮组
        Button[] buttons = { varBtnNewGame, varBtnContinue, varBtnLoadSave, varBtnSettings, varBtnQuit };
        m_ButtonAnimDatas = new ButtonAnimData[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            var rt = buttons[i].GetComponent<RectTransform>();
            m_ButtonAnimDatas[i] = new ButtonAnimData
            {
                canvasGroup = buttons[i].gameObject.GetOrAddComponent<CanvasGroup>(),
                rectTransform = rt,
                originalPos = rt.anchoredPosition
            };
        }

        // 云存档按钮
        m_CloudBtnRect = var存档上云.GetComponent<RectTransform>();
        m_CloudBtnCanvasGroup = var存档上云.gameObject.GetOrAddComponent<CanvasGroup>();
    }

    /// <summary>
    /// 还原所有动画元素到原始状态
    /// </summary>
    private void ResetAnimationState()
    {
        if (m_BgCanvasGroup != null) m_BgCanvasGroup.alpha = 1f;
        if (m_TitleCnCanvasGroup != null) m_TitleCnCanvasGroup.alpha = 1f;
        if (m_TitleEnCanvasGroup != null) m_TitleEnCanvasGroup.alpha = 1f;
        if (m_CloudBtnCanvasGroup != null)
        {
            m_CloudBtnCanvasGroup.alpha = 1f;
            m_CloudBtnRect.localScale = Vector3.one;
        }

        if (m_TitleCnRect != null) m_TitleCnRect.anchoredPosition = m_TitleCnOriginalPos;
        if (m_TitleEnRect != null) m_TitleEnRect.anchoredPosition = m_TitleEnOriginalPos;

        if (m_ButtonAnimDatas != null)
        {
            for (int i = 0; i < m_ButtonAnimDatas.Length; i++)
            {
                var data = m_ButtonAnimDatas[i];
                if (data.canvasGroup != null) data.canvasGroup.alpha = 1f;
                if (data.rectTransform != null) data.rectTransform.anchoredPosition = data.originalPos;
            }
        }
    }

    #endregion

    #region 按钮点击事件

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);

        if (btSelf == varBtnNewGame)
        {
            OnNewGameButtonClick();
        }
        else if (btSelf == varBtnContinue)
        {
            OnContinueButtonClick();
        }
        else if (btSelf == varBtnLoadSave)
        {
            OnLoadSaveButtonClick();
        }
        else if (btSelf == varBtnSettings)
        {
            OnSettingsButtonClick();
        }
        else if (btSelf == varBtnQuit)
        {
            OnQuitButtonClick();
        }
        else if (btSelf == var存档上云)
        {
            OnCloudArchive();
        }
    }

    #endregion

    #region 各个按钮逻辑

    private void OnNewGameButtonClick()
    {
        Log.Info("点击了新游戏按钮");
        GF.UI.OpenUIForm(UIViews.NewGameUI);
        GF.UI.CloseUIForm(this.UIForm);
    }

    private void OnContinueButtonClick()
    {
        Log.Info("点击了继续游戏按钮");

        if (HasSaveData())
        {
            LoadLatestSave();
        }
        else
        {
            GF.UI.ShowToast("没有存档数据", UIExtension.ToastStyle.Red);
        }
    }

    private void OnLoadSaveButtonClick()
    {
        Log.Info("点击了加载存档按钮");
        GF.UI.OpenUIForm(UIViews.LoadGameUI);
    }

    private void OnSettingsButtonClick()
    {
        Log.Info("点击了设置按钮");
        GF.UI.OpenUIForm(UIViews.SettingDialog);
    }

    private void OnQuitButtonClick()
    {
        Log.Info("点击了退出按钮");
        QuitGame();
    }

    private void OnCloudArchive()
    {
        Log.Info("点击了存档管理按钮");
        OpenSubUIForm(UIViews.CloudArchiveUI);
    }

    #endregion

    #region 辅助方法

    private void CheckSaveData()
    {
        PlayerAccountDataManager.Instance.SetCurrentAccountId("000001");
        bool hasSave = HasSaveData();

        if (varBtnContinue != null)
        {
            varBtnContinue.interactable = hasSave;
        }
        if (varBtnLoadSave != null)
        {
            varBtnLoadSave.interactable = hasSave;
        }

        Log.Info($"存档检查完成：是否有存档: {hasSave}");
    }

    private bool HasSaveData()
    {
        var saveInfos = PlayerAccountDataManager.Instance.GetAllSaveBriefInfos();
        return saveInfos != null && saveInfos.Count > 0;
    }

    private void LoadLatestSave()
    {
        Log.Info("自动加载最新的存档");
        bool success = PlayerAccountDataManager.Instance.AutoLoadLastSave();

        if (success)
        {
            var currentSave = PlayerAccountDataManager.Instance.CurrentSaveData;
            if (currentSave != null)
            {
                GF.UI.ShowToast($"加载存档成功: {currentSave.SaveName}", UIExtension.ToastStyle.Green);
                EnterGame();
            }
        }
        else
        {
            GF.UI.ShowToast("加载存档失败", UIExtension.ToastStyle.Red);
        }
    }

    private void EnterGame()
    {
        GF.UI.CloseUIForm(this.UIForm);
        GameFlowManager.EnterGame();
    }

    private void QuitGame()
    {
        GameFlowManager.QuitGame();
    }

    #endregion
}
