using DG.Tweening;
using UnityEngine;

/// <summary>
/// 卡牌预览显示管理器
/// - 蓝色圆形：作用范围预览
/// - 红色覆盖：无效区域预览
/// </summary>
public class CardPreviewDisplay : MonoBehaviour
{
    #region 单例

    private static CardPreviewDisplay s_Instance;
    public static CardPreviewDisplay Instance => s_Instance;

    #endregion

    #region 字段

    // 作用范围预览（蓝色）
    private Vector3 m_ActionPreviewPosition = Vector3.zero;
    private float m_ActionPreviewRadius = 0f;
    private bool m_IsShowingActionPreview = false;
    private float m_ActionAlpha = 0f;
    private Tween m_ActionFadeTween;
    private Tween m_ActionPulseTween;
    private float m_ActionPulseScale = 1f;

    // 无效区域预览（红色）
    private bool m_IsShowingInvalidPreview = false;
    private float m_InvalidAlpha = 0f;
    private Tween m_InvalidFadeTween;

    private const float FADE_DURATION = 0.15f;
    private const float PULSE_DURATION = 0.5f;
    private const float PULSE_MIN_SCALE = 1f;
    private const float PULSE_MAX_SCALE = 1.05f;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this;
            DebugEx.LogModule("CardPreviewDisplay", "初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
            m_ActionFadeTween?.Kill();
            m_ActionPulseTween?.Kill();
            m_InvalidFadeTween?.Kill();
        }
    }

    private void OnDrawGizmos()
    {
        // 绘制作用范围（蓝色）
        if (m_IsShowingActionPreview)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, m_ActionAlpha * 0.6f);
            DrawCircle(m_ActionPreviewPosition, m_ActionPreviewRadius * m_ActionPulseScale, 32);
        }

        // 绘制无效区域（红色全屏覆盖）
        if (m_IsShowingInvalidPreview)
        {
            Gizmos.color = new Color(1f, 0f, 0f, m_InvalidAlpha * 0.3f);
            DrawFullScreenQuad();
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示作用范围预览（蓝色圆形）
    /// </summary>
    public void ShowActionPreview(Vector3 position, float radius)
    {
        m_ActionPreviewPosition = position;
        m_ActionPreviewRadius = radius;
        m_IsShowingActionPreview = true;

        // 杀死旧动画
        m_ActionFadeTween?.Kill();
        m_ActionPulseTween?.Kill();

        // 淡入
        m_ActionAlpha = 0f;
        m_ActionFadeTween = DOTween.To(() => m_ActionAlpha, x => m_ActionAlpha = x, 1f, FADE_DURATION)
            .SetEase(Ease.OutQuad);

        // 脉冲
        PlayActionPulseAnimation();

        DebugEx.LogModule("CardPreviewDisplay", $"显示作用范围：位置={position}，半径={radius}");
    }

    /// <summary>
    /// 隐藏作用范围预览
    /// </summary>
    public void HideActionPreview()
    {
        if (!m_IsShowingActionPreview)
            return;

        m_ActionFadeTween?.Kill();
        m_ActionPulseTween?.Kill();

        m_ActionFadeTween = DOTween.To(() => m_ActionAlpha, x => m_ActionAlpha = x, 0f, FADE_DURATION)
            .SetEase(Ease.InQuad)
            .OnComplete(() => m_IsShowingActionPreview = false);
    }

    /// <summary>
    /// 显示无效区域预览（红色覆盖）
    /// </summary>
    public void ShowInvalidPreview()
    {
        m_IsShowingInvalidPreview = true;

        m_InvalidFadeTween?.Kill();
        m_InvalidAlpha = 0f;
        m_InvalidFadeTween = DOTween.To(() => m_InvalidAlpha, x => m_InvalidAlpha = x, 1f, FADE_DURATION)
            .SetEase(Ease.OutQuad);

        DebugEx.LogModule("CardPreviewDisplay", "显示无效区域预览");
    }

    /// <summary>
    /// 隐藏无效区域预览
    /// </summary>
    public void HideInvalidPreview()
    {
        if (!m_IsShowingInvalidPreview)
            return;

        m_InvalidFadeTween?.Kill();
        m_InvalidFadeTween = DOTween.To(() => m_InvalidAlpha, x => m_InvalidAlpha = x, 0f, FADE_DURATION)
            .SetEase(Ease.InQuad)
            .OnComplete(() => m_IsShowingInvalidPreview = false);
    }

    /// <summary>
    /// 隐藏所有预览
    /// </summary>
    public void HideAll()
    {
        HideActionPreview();
        HideInvalidPreview();
    }

    #endregion

    #region 动效方法

    /// <summary>
    /// 播放作用范围脉冲动画
    /// </summary>
    private void PlayActionPulseAnimation()
    {
        m_ActionPulseTween?.Kill();

        var sequence = DOTween.Sequence();
        sequence.Append(DOTween.To(() => m_ActionPulseScale, x => m_ActionPulseScale = x, PULSE_MAX_SCALE, PULSE_DURATION * 0.5f).SetEase(Ease.InOutQuad));
        sequence.Append(DOTween.To(() => m_ActionPulseScale, x => m_ActionPulseScale = x, PULSE_MIN_SCALE, PULSE_DURATION * 0.5f).SetEase(Ease.InOutQuad));
        sequence.SetLoops(-1, LoopType.Restart);

        m_ActionPulseTween = sequence;
    }

    #endregion

    #region 绘制方法

    /// <summary>
    /// 绘制圆形
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

    /// <summary>
    /// 绘制全屏四边形（用于无效区域覆盖）
    /// </summary>
    private void DrawFullScreenQuad()
    {
        // 绘制一个大的四边形覆盖整个视图
        float size = 500f;
        Vector3 center = Camera.main.transform.position + Camera.main.transform.forward * 10f;

        Vector3 topLeft = center + Vector3.left * size + Vector3.up * size;
        Vector3 topRight = center + Vector3.right * size + Vector3.up * size;
        Vector3 bottomLeft = center + Vector3.left * size + Vector3.down * size;
        Vector3 bottomRight = center + Vector3.right * size + Vector3.down * size;

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

    #endregion
}
