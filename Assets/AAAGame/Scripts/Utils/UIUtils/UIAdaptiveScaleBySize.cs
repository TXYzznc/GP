using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[ExecuteAlways]
[DisallowMultipleComponent]
public class UIAdaptiveScaleBySize : MonoBehaviour
{
    public enum ScaleMode
    {
        UniformMin = 0,   // 取宽高缩放系数的较小值（常用：保证不超出）
        UniformMax = 1,   // 取宽高缩放系数的较大值（常用：保证能铺满）
        MatchWidth = 2,   // 只按宽度缩放
        MatchHeight = 3,  // 只按高度缩放
        NonUniform = 4,   // 宽高分别缩放（会拉伸）
    }

    [Header("参考尺寸（设计稿/标准UI宽高）")]
    [SerializeField] private Vector2 m_ReferenceSize = new Vector2(1920, 1080);

    [Header("当前尺寸来源（不填则默认用父节点RectTransform）")]
    [SerializeField] private RectTransform m_SizeSource = null;

    [Header("缩放目标（不填则缩放自身）")]
    [SerializeField] private Transform m_Target = null;

    [Header("缩放模式")]
    [SerializeField] private ScaleMode m_ScaleMode = ScaleMode.UniformMin;

    [Header("其他")]
    [SerializeField] private bool m_KeepZScale = true;

    private RectTransform m_SelfRect;
    private Vector2 m_LastAppliedSourceSize;
    private Vector2 m_LastAppliedReferenceSize;
    private ScaleMode m_LastAppliedMode;
    private bool m_LastAppliedKeepZ;

    private Vector3 m_BaseLocalScale;
    private bool m_BaseScaleCaptured;

    private void Awake()
    {
        CacheRefs();
        CaptureBaseScaleIfNeeded();
    }

    private void OnEnable()
    {
        CacheRefs();
        CaptureBaseScaleIfNeeded();
        ApplyIfNeeded(force: true);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        ApplyIfNeeded(force: false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheRefs();
        CaptureBaseScaleIfNeeded();
        ApplyIfNeeded(force: true);
    }
#endif

    public void ApplyNow()
    {
        CacheRefs();
        CaptureBaseScaleIfNeeded();
        ApplyIfNeeded(force: true);
    }

    public void CaptureCurrentAsBase()
    {
        CacheRefs();
        var target = GetTargetTransform();
        if (target == null) return;

        m_BaseLocalScale = target.localScale;
        m_BaseScaleCaptured = true;

        ApplyIfNeeded(force: true);
    }

    public void ResetToBase()
    {
        CacheRefs();
        var target = GetTargetTransform();
        if (target == null) return;

        CaptureBaseScaleIfNeeded();
        target.localScale = m_BaseLocalScale;

        m_LastAppliedSourceSize = default;
        m_LastAppliedReferenceSize = default;
    }

    private void CacheRefs()
    {
        if (m_SelfRect == null) m_SelfRect = transform as RectTransform;
    }

    private Transform GetTargetTransform()
    {
        return m_Target != null ? m_Target : transform;
    }

    private RectTransform GetSizeSource()
    {
        if (m_SizeSource != null) return m_SizeSource;

        if (m_SelfRect != null && m_SelfRect.parent is RectTransform parentRect)
            return parentRect;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null) return canvas.transform as RectTransform;

        return null;
    }

    private void CaptureBaseScaleIfNeeded()
    {
        if (m_BaseScaleCaptured) return;

        var target = GetTargetTransform();
        if (target == null) return;

        m_BaseLocalScale = target.localScale;
        m_BaseScaleCaptured = true;
    }

    private void ApplyIfNeeded(bool force)
    {
        var target = GetTargetTransform();
        if (target == null) return;

        if (m_ReferenceSize.x <= 0f || m_ReferenceSize.y <= 0f) return;

        var source = GetSizeSource();
        if (source == null) return;

        Vector2 sourceSize = source.rect.size;
        if (sourceSize.x <= 0f || sourceSize.y <= 0f) return;

        if (!force &&
            sourceSize == m_LastAppliedSourceSize &&
            m_ReferenceSize == m_LastAppliedReferenceSize &&
            m_ScaleMode == m_LastAppliedMode &&
            m_KeepZScale == m_LastAppliedKeepZ)
        {
            return;
        }

        float sx = sourceSize.x / m_ReferenceSize.x;
        float sy = sourceSize.y / m_ReferenceSize.y;

        float outX = 1f;
        float outY = 1f;

        switch (m_ScaleMode)
        {
            case ScaleMode.UniformMin:
            {
                float s = Mathf.Min(sx, sy);
                outX = s;
                outY = s;
                break;
            }
            case ScaleMode.UniformMax:
            {
                float s = Mathf.Max(sx, sy);
                outX = s;
                outY = s;
                break;
            }
            case ScaleMode.MatchWidth:
                outX = sx;
                outY = sx;
                break;
            case ScaleMode.MatchHeight:
                outX = sy;
                outY = sy;
                break;
            case ScaleMode.NonUniform:
                outX = sx;
                outY = sy;
                break;
        }

        CaptureBaseScaleIfNeeded();

        float zMul = m_KeepZScale ? 1f : ((m_ScaleMode == ScaleMode.NonUniform) ? 1f : outX);

        var mul = new Vector3(outX, outY, zMul);
        target.localScale = new Vector3(
            m_BaseLocalScale.x * mul.x,
            m_BaseLocalScale.y * mul.y,
            m_BaseLocalScale.z * mul.z
        );

        m_LastAppliedSourceSize = sourceSize;
        m_LastAppliedReferenceSize = m_ReferenceSize;
        m_LastAppliedMode = m_ScaleMode;
        m_LastAppliedKeepZ = m_KeepZScale;
    }
}