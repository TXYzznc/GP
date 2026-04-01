using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 扩展的 Layout Element，支持 Max Width 和 Max Height 限制
/// 继承自 LayoutElement，保持所有原有功能，添加最大值限制
///
/// 使用方法：
/// 1. 在需要限制最大尺寸的组件上添加此脚本（而不是 LayoutElement）
/// 2. 设置 Max Width 和/或 Max Height（-1 表示不限制）
/// 3. 其他设置与 LayoutElement 相同
/// </summary>
[AddComponentMenu("Layout/Extended Layout Element", 141)]
[RequireComponent(typeof(RectTransform))]
public class ExtendedLayoutElement : LayoutElement
{
    #region 字段

    [SerializeField]
    [Tooltip("最大宽度，-1 表示不限制")]
    private float m_MaxWidth = -1f;

    [SerializeField]
    [Tooltip("最大高度，-1 表示不限制")]
    private float m_MaxHeight = -1f;

    private RectTransform m_RectTransform;
    private bool m_LayoutDirty = false;

    #endregion

    #region 属性

    /// <summary>
    /// 最大宽度（-1 表示不限制）
    /// </summary>
    public float maxWidth
    {
        get => m_MaxWidth;
        set
        {
            if (Mathf.Approximately(m_MaxWidth, value))
                return;
            m_MaxWidth = value;
            SetDirty();
        }
    }

    /// <summary>
    /// 最大高度（-1 表示不限制）
    /// </summary>
    public float maxHeight
    {
        get => m_MaxHeight;
        set
        {
            if (Mathf.Approximately(m_MaxHeight, value))
                return;
            m_MaxHeight = value;
            SetDirty();
        }
    }

    #endregion

    #region Unity 生命周期

    protected override void OnEnable()
    {
        base.OnEnable();
        m_RectTransform = GetComponent<RectTransform>();
        SetDirty();
    }

    protected override void OnDisable()
    {
        if (m_RectTransform != null)
            LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);
        base.OnDisable();
    }

    /// <summary>
    /// 编辑器中实时预览
    /// </summary>
    protected override void OnValidate()
    {
        // 在编辑器中验证数值范围
        if (!enabled)
            return;

        m_MaxWidth = Mathf.Max(-1f, m_MaxWidth);
        m_MaxHeight = Mathf.Max(-1f, m_MaxHeight);

        // 标记需要重建
        if (IsActive())
        {
            m_LayoutDirty = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // 编辑器中立即应用
                ApplyMaxConstraints();
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
#endif
        }
    }

    /// <summary>
    /// LateUpdate 中应用最大值限制
    /// </summary>
    private void LateUpdate()
    {
        if (m_LayoutDirty)
        {
            ApplyMaxConstraints();
            m_LayoutDirty = false;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器更新，用于实时预览
    /// </summary>
    private void Update()
    {
        if (!Application.isPlaying && m_LayoutDirty)
        {
            ApplyMaxConstraints();
            m_LayoutDirty = false;
        }
    }
#endif

    #endregion

    #region Layout 约束应用

    /// <summary>
    /// 应用最大值约束
    /// </summary>
    private void ApplyMaxConstraints()
    {
        if (m_RectTransform == null)
            m_RectTransform = GetComponent<RectTransform>();

        if (m_RectTransform == null)
            return;

        Vector2 sizeDelta = m_RectTransform.sizeDelta;
        bool changed = false;

        // 应用最大宽度
        if (m_MaxWidth > 0 && sizeDelta.x > m_MaxWidth)
        {
            sizeDelta.x = m_MaxWidth;
            changed = true;
        }

        // 应用最大高度
        if (m_MaxHeight > 0 && sizeDelta.y > m_MaxHeight)
        {
            sizeDelta.y = m_MaxHeight;
            changed = true;
        }

        // 如果有变化，应用到 RectTransform
        if (changed)
        {
            m_RectTransform.sizeDelta = sizeDelta;
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 标记布局需要重建
    /// </summary>
    private void SetDirty()
    {
        if (!IsActive())
            return;

        m_LayoutDirty = true;
        if (m_RectTransform == null)
            m_RectTransform = GetComponent<RectTransform>();
        if (m_RectTransform != null)
            LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);
    }

    #endregion
}
