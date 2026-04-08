using DG.Tweening;
using UnityEngine;

/// <summary>
/// 卡牌预览显示管理器（Shader 版本）
/// - 蓝色圆形：作用范围预览（Shader 实现）
/// - 绿色矩形：用户自己制作的 UI Image
/// - 红色覆盖：用户自己制作的 UI Image
/// </summary>
public class CardPreviewDisplayShader : MonoBehaviour
{
    #region 单例

    private static CardPreviewDisplayShader s_Instance;
    public static CardPreviewDisplayShader Instance => s_Instance;

    #endregion

    #region 字段

    // 蓝色圆形预览
    private GameObject m_CirclePreviewQuad;
    private Material m_CircleMaterial;
    private bool m_IsShowingActionPreview = false;
    private float m_CurrentAlpha = 0f;
    private Tween m_AlphaTween;
    private Color m_CircleBaseColor;  // 缓存基础颜色，避免频繁调用 GetColor

    // ⭐ [调试功能] 暂停预览显示，防止鼠标离开时消失（便于查看 Inspector）
    [SerializeField]
    private bool m_PausePreviewDisplay = false;

    private const float FADE_DURATION = 0.15f;
    private const string CIRCLE_SHADER_NAME = "Custom/CirclePreview";
    private const string COLOR_PROPERTY = "_Color";
    private const string RADIUS_PROPERTY = "_Radius";

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this;
            DebugEx.LogModule("CardPreviewDisplayShader", "管理器已初始化");
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
            m_AlphaTween?.Kill();

            if (m_CirclePreviewQuad != null)
            {
                Destroy(m_CirclePreviewQuad);
            }
        }
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化蓝色圆形预览
    /// </summary>
    private void InitializeCirclePreview()
    {
        // 创建 Quad
        m_CirclePreviewQuad = new GameObject("CirclePreviewQuad");
        m_CirclePreviewQuad.transform.SetParent(transform);
        m_CirclePreviewQuad.transform.localPosition = Vector3.zero;
        m_CirclePreviewQuad.transform.localRotation = Quaternion.Euler(90, 0, 0);
        m_CirclePreviewQuad.transform.localScale = Vector3.one * 0.2f;  // 高度为 0.2f

        // 添加 MeshFilter 和 MeshRenderer
        var meshFilter = m_CirclePreviewQuad.AddComponent<MeshFilter>();
        meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        var meshRenderer = m_CirclePreviewQuad.AddComponent<MeshRenderer>();

        // 创建 Material
        var shader = Shader.Find(CIRCLE_SHADER_NAME);
        if (shader == null)
        {
            DebugEx.ErrorModule("CardPreviewDisplayShader", $"未找到 Shader: {CIRCLE_SHADER_NAME}");
            return;
        }

        m_CircleMaterial = new Material(shader);
        m_CircleBaseColor = new Color(0, 0.5f, 1, 1f);  // 蓝色，透明度在 UpdateCircleAlpha 中控制
        m_CircleMaterial.SetColor(COLOR_PROPERTY, new Color(0, 0.5f, 1, 0f));
        m_CircleMaterial.SetFloat(RADIUS_PROPERTY, 1f);

        meshRenderer.material = m_CircleMaterial;

        // 禁用碰撞和阴影
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        m_CirclePreviewQuad.SetActive(false);

        DebugEx.LogModule("CardPreviewDisplayShader", "蓝色圆形预览已初始化");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示作用范围预览（蓝色圆形）
    /// </summary>
    public void ShowActionPreview(Vector3 position, float radius)
    {
        // Lazy initialization - 第一次使用时才创建
        if (m_CirclePreviewQuad == null)
        {
            InitializeCirclePreview();
        }

        if (!m_IsShowingActionPreview)
        {
            m_IsShowingActionPreview = true;
            m_CirclePreviewQuad.SetActive(true);
        }

        // 更新位置（高度偏移 0.5f，平行于战场）
        m_CirclePreviewQuad.transform.position = new Vector3(position.x, 0.5f, position.z);

        // 更新半径（缩放）
        // Quad 原始尺寸为 1x1，绕 X 轴旋转 90 度后，X/Y 轴是宽深，Z 轴是高度
        float scale = radius * 2f;
        m_CirclePreviewQuad.transform.localScale = new Vector3(scale, scale, 0.2f);

        // 更新 Shader 中的半径参数（0-1 范围）
        m_CircleMaterial.SetFloat(RADIUS_PROPERTY, 0.95f);

        // 淡入动画
        m_AlphaTween?.Kill();
        m_CurrentAlpha = 0f;
        m_AlphaTween = DOTween.To(() => m_CurrentAlpha, x => m_CurrentAlpha = x, 1f, FADE_DURATION)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() => UpdateCircleAlpha());

        //DebugEx.LogModule("CardPreviewDisplayShader", $"显示作用范围：位置={position}，半径={radius}");
    }

    /// <summary>
    /// 隐藏作用范围预览（⭐ 受 m_PausePreviewDisplay 影响）
    /// </summary>
    public void HideActionPreview()
    {
        // ⭐ [调试功能] 如果暂停预览显示，则不隐藏
        if (m_PausePreviewDisplay)
            return;

        if (!m_IsShowingActionPreview)
            return;

        m_AlphaTween?.Kill();
        m_AlphaTween = DOTween.To(() => m_CurrentAlpha, x => m_CurrentAlpha = x, 0f, FADE_DURATION)
            .SetEase(Ease.InQuad)
            .OnUpdate(() => UpdateCircleAlpha())
            .OnComplete(() =>
            {
                m_IsShowingActionPreview = false;
                m_CirclePreviewQuad.SetActive(false);
            });
    }

    /// <summary>
    /// 隐藏所有预览
    /// </summary>
    public void HideAll()
    {
        HideActionPreview();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 更新圆形透明度（缓存颜色，避免频繁调用 GetColor）
    /// </summary>
    private void UpdateCircleAlpha()
    {
        var color = m_CircleBaseColor;
        color.a = 0.8f;  // 稳定透明度 80%
        m_CircleMaterial.SetColor(COLOR_PROPERTY, color);
    }

    #endregion
}
