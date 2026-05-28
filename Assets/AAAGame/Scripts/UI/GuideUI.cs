using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 操作指南 UI
/// 显示多张漫画图片，支持翻页，最后一页有"开始游戏"按钮
/// 打开方式：GF.UI.OpenUI(UIViews.GuideUI)
/// </summary>
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class GuideUI : UIFormBase
{
    #region SerializeField

    [Header("漫画页面（按顺序配置）")]
    [SerializeField] private Sprite[] m_Pages;

    #endregion

    #region 私有字段

    private int m_CurrentPage = 0;

    #endregion

    #region 生命周期

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        m_CurrentPage = 0;

        varBtnClose.onClick.AddListener(OnClose);
        varBtnPrev.onClick.AddListener(OnPrev);
        varBtnNext.onClick.AddListener(OnNext);
        varBtnStartGame.onClick.AddListener(OnClose);

        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.RequestMouseUnlock();

        RefreshPage();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varBtnClose.onClick.RemoveListener(OnClose);
        varBtnPrev.onClick.RemoveListener(OnPrev);
        varBtnNext.onClick.RemoveListener(OnNext);
        varBtnStartGame.onClick.RemoveListener(OnClose);

        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.RequestMouseLock();

        base.OnClose(isShutdown, userData);
    }

    #endregion

    #region 翻页逻辑

    private void OnPrev()
    {
        if (m_CurrentPage > 0)
        {
            m_CurrentPage--;
            RefreshPage();
        }
    }

    private void OnNext()
    {
        if (m_CurrentPage < m_Pages.Length - 1)
        {
            m_CurrentPage++;
            RefreshPage();
        }
    }

    private void OnClose()
    {
        GF.UI.CloseUIForm(Id);
    }

    private void RefreshPage()
    {
        if (m_Pages == null || m_Pages.Length == 0)
            return;

        // 更新图片
        if (varImgPage != null && m_CurrentPage < m_Pages.Length)
            varImgPage.sprite = m_Pages[m_CurrentPage];

        bool isFirst = m_CurrentPage == 0;
        bool isLast  = m_CurrentPage == m_Pages.Length - 1;

        // 上一页按钮：第一页禁用
        varBtnPrev.interactable = !isFirst;

        // 下一页按钮：最后一页禁用
        varBtnNext.interactable = !isLast;

        // 开始游戏按钮：只在最后一页显示
        varBtnStartGame.gameObject.SetActive(isLast);
    }

    #endregion
}
