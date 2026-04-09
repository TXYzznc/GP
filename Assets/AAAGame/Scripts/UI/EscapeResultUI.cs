using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// 脱战结果UI
/// 显示脱战成功或失败的结果
/// 自动延迟2秒后隐藏
///
/// 需要的UI变量（需要用户创建预制体）：
/// - varResultTitle (Text) - 结果标题（成功/失败）
/// - varResultIcon (Image) - 结果图标
/// - varResultMessage (Text) - 结果信息（污染值/生命值变化）
/// </summary>
public partial class EscapeResultUI : UIFormBase
{
    #region 私有字段

    /// <summary>自动隐藏的取消令牌</summary>
    private System.Threading.CancellationTokenSource m_AutoHideCts;

    #endregion

    #region UIFormBase重写

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        DebugEx.LogModule("EscapeResultUI", "初始化完成");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // userData 可能是 UIParams 或 EscapeResultData
        EscapeResultData resultData = null;

        if (userData is UIParams uiParams)
        {
            // 从 UIParams 中获取结果数据
            resultData = uiParams.Get("EscapeResultData") as EscapeResultData;
        }
        else if (userData is EscapeResultData data)
        {
            resultData = data;
        }

        if (resultData != null)
        {
            ShowResult(resultData);
            PlayOpenAnimation();
            // 延迟2秒后自动隐藏
            ScheduleAutoHide();
        }
        else
        {
            DebugEx.WarningModule("EscapeResultUI", "未收到有效的结果数据");
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(gameObject, true);
        base.OnClose(isShutdown, userData);

        // 取消待处理的自动隐藏
        if (m_AutoHideCts != null)
        {
            m_AutoHideCts.Cancel();
            m_AutoHideCts.Dispose();
            m_AutoHideCts = null;
        }

        DebugEx.LogModule("EscapeResultUI", "已关闭");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 显示脱战结果
    /// </summary>
    private void ShowResult(EscapeResultData resultData)
    {
        if (resultData == null) return;

        // 更新UI显示
        if (varResultTitle != null)
        {
            varResultTitle.text = resultData.Success ? "脱战成功" : "脱战失败";
            // 设置颜色
            varResultTitle.color = resultData.Success
                ? new Color(0.2f, 1f, 0.2f, 1f) // 绿色
                : new Color(1f, 0.2f, 0.2f, 1f); // 红色
        }

        if (varResultIcon != null)
        {
            // TODO: 从资源系统加载成功/失败图标
            // varResultIcon.sprite = GF.Resource.LoadSprite(resultData.Success ? "Icon_Success" : "Icon_Fail");
            varResultIcon.enabled = false; // 暂时隐藏
        }

        if (varResultMessage != null)
        {
            if (resultData.Success)
            {
                varResultMessage.text = $"消耗污染值：{resultData.CorruptionCost}";
            }
            else
            {
                string message = $"召唤师生命损失：{resultData.HealthLoss:P0}";
                if (resultData.CooldownTurns > 0)
                {
                    message += $"\n脱战冷却：{resultData.CooldownTurns}回合";
                }
                varResultMessage.text = message;
            }
        }

        DebugEx.LogModule("EscapeResultUI",
            resultData.Success
                ? $"脱战成功，消耗污染值: {resultData.CorruptionCost}"
                : $"脱战失败，生命损失: {resultData.HealthLoss:P0}, 冷却: {resultData.CooldownTurns}回合");
    }

    private void PlayOpenAnimation()
    {
        DOTween.Kill(gameObject);
        var rt = GetComponent<RectTransform>();
        var cg = GetComponent<CanvasGroup>();
        var orig = rt.anchoredPosition;
        rt.anchoredPosition = orig + new Vector2(0, 100f);
        cg.alpha = 0f;
        DOTween.Sequence().SetUpdate(true)
            .Join(rt.DOAnchorPos(orig, 0.3f).SetEase(Ease.OutQuart))
            .Join(cg.DOFade(1f, 0.25f).SetEase(Ease.OutQuart));
    }

    /// <summary>
    /// 安排自动隐藏
    /// 延迟2秒后自动关闭UI
    /// </summary>
    private void ScheduleAutoHide()
    {
        // 创建取消令牌源
        m_AutoHideCts = new System.Threading.CancellationTokenSource();

        // 使用 UniTask 实现延迟隐藏
        AutoHideAsync(m_AutoHideCts.Token).Forget();
    }

    /// <summary>
    /// 异步自动隐藏
    /// </summary>
    private async UniTask AutoHideAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            // 延迟 2 秒
            await UniTask.Delay(2000, cancellationToken: cancellationToken);

            // 退场动画后关闭
            if (this.UIForm != null)
            {
                var rt = GetComponent<RectTransform>();
                var cg = GetComponent<CanvasGroup>();
                DOTween.Kill(gameObject);
                bool closed = false;
                DOTween.Sequence().SetUpdate(true)
                    .Join(rt.DOAnchorPos(rt.anchoredPosition + new Vector2(0, 100f), 0.25f).SetEase(Ease.InQuart))
                    .Join(cg.DOFade(0f, 0.25f).SetEase(Ease.InQuart))
                    .OnComplete(() =>
                    {
                        if (!closed && this.UIForm != null)
                        {
                            closed = true;
                            GF.UI.Close(this.UIForm);
                        }
                    });
                DebugEx.LogModule("EscapeResultUI", "自动隐藏触发");
            }
        }
        catch (System.OperationCanceledException)
        {
            DebugEx.LogModule("EscapeResultUI", "自动隐藏被取消");
        }
    }

    #endregion
}

/// <summary>
/// 脱战结果数据
/// </summary>
public class EscapeResultData
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>消耗的污染值（成功时）</summary>
    public int CorruptionCost { get; set; }

    /// <summary>生命值损失比例（失败时）</summary>
    public float HealthLoss { get; set; }

    /// <summary>冷却回合数（失败时）</summary>
    public int CooldownTurns { get; set; }
}
