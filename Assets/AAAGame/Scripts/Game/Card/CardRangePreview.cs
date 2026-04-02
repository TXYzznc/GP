using UnityEngine;
using DG.Tweening;

/// <summary>
/// 卡牌范围预览管理器（临时使用 Gizmos 显示，后续改成 Shader）
/// </summary>
public class CardRangePreview : MonoBehaviour
{
    #region 单例

    private static CardRangePreview s_Instance;
    public static CardRangePreview Instance => s_Instance;

    #endregion

    #region 字段

    private Vector3 m_CurrentPreviewPosition = Vector3.zero;
    private float m_CurrentPreviewRadius = 0f;
    private bool m_IsShowingPreview = false;
    private float m_CurrentAlpha = 1f;
    private float m_PulseScale = 1f;
    private Tween m_PulseTween;
    private Tween m_FadeTween;

    private const float PULSE_MIN_SCALE = 1f;
    private const float PULSE_MAX_SCALE = 1.05f;
    private const float PULSE_DURATION = 0.6f;
    private const float FADE_DURATION = 0.2f;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DebugEx.LogModule("CardRangePreview", "范围预览系统已初始化（Gizmos 模式）");
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
            m_PulseTween?.Kill();
            m_FadeTween?.Kill();
        }
    }

    private void OnDrawGizmos()
    {
        if (!m_IsShowingPreview)
            return;

        // 绘制黄色圆形范围预览（带脉冲和透明度）
        Gizmos.color = new Color(1f, 1f, 0f, m_CurrentAlpha * 0.5f);
        DrawCircle(m_CurrentPreviewPosition, m_CurrentPreviewRadius * m_PulseScale, 32);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示范围预览
    /// </summary>
    public void ShowPreview(Vector3 position, float radius)
    {
        m_CurrentPreviewPosition = position;
        m_CurrentPreviewRadius = radius;
        m_IsShowingPreview = true;

        // 杀死之前的动画
        m_FadeTween?.Kill();
        m_PulseTween?.Kill();

        // 淡入动画
        m_CurrentAlpha = 0f;
        m_FadeTween = DOTween.To(() => m_CurrentAlpha, x => m_CurrentAlpha = x, 1f, FADE_DURATION)
            .SetEase(Ease.OutQuad);

        // 启动脉冲动画
        PlayPulseAnimation();

        DebugEx.LogModule("CardRangePreview", $"显示范围预览: 位置={position}, 半径={radius}");
    }

    /// <summary>
    /// 隐藏范围预览
    /// </summary>
    public void HidePreview()
    {
        if (!m_IsShowingPreview)
            return;

        // 杀死之前的动画
        m_FadeTween?.Kill();
        m_PulseTween?.Kill();

        // 淡出动画
        m_FadeTween = DOTween.To(() => m_CurrentAlpha, x => m_CurrentAlpha = x, 0f, FADE_DURATION)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                m_IsShowingPreview = false;
                DebugEx.LogModule("CardRangePreview", "范围预览已隐藏");
            });
    }

    /// <summary>
    /// 隐藏所有预览
    /// </summary>
    public void HideAllPreviews()
    {
        HidePreview();
    }

    #endregion

    #region 动效方法

    /// <summary>
    /// 播放脉冲动画
    /// </summary>
    private void PlayPulseAnimation()
    {
        m_PulseTween?.Kill();

        // 脉冲序列：1.0 → 1.05 → 1.0（循环）
        var sequence = DOTween.Sequence();
        sequence.Append(DOTween.To(() => m_PulseScale, x => m_PulseScale = x, PULSE_MAX_SCALE, PULSE_DURATION * 0.5f).SetEase(Ease.InOutQuad));
        sequence.Append(DOTween.To(() => m_PulseScale, x => m_PulseScale = x, PULSE_MIN_SCALE, PULSE_DURATION * 0.5f).SetEase(Ease.InOutQuad));
        sequence.SetLoops(-1, LoopType.Restart);

        m_PulseTween = sequence;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 绘制圆形（用于 Gizmos）
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 lastPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }

    #endregion
}
