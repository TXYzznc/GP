using UnityEngine;
using TMPro;
using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 伤害飘字单项
/// 控制单个飘字的显示与动画
/// </summary>
public class DamagePopupItem : MonoBehaviour
{
    #region 私有字段

    private TextMeshProUGUI m_Text;
    private CanvasGroup m_CanvasGroup;

    private Vector3 m_StartPosition;
    private float m_ElapsedTime;
    private bool m_IsPlaying;
    private Action m_OnComplete;
    private Camera m_Camera;
    private float m_BaseScale = 1f;  // 基础缩放

    // 动画参数（可自定义）
    private float m_Duration = 1.0f;
    private float m_RiseHeight = 1.5f;

    // ⭐ 性能优化：缓存距离缩放结果
    private float m_CachedDistanceScale = 1f;
    private int m_FrameCounter = 0;
    private const int DISTANCE_UPDATE_INTERVAL = 3;  // 每 3 帧更新一次距离缩放

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化组件
    /// </summary>
    public void Initialize(TextMeshProUGUI text)
    {
        m_Text = text;

        // 添加 CanvasGroup 用于淡出
        m_CanvasGroup = GetComponent<CanvasGroup>();
        if (m_CanvasGroup == null)
        {
            m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        DebugEx.LogModule("DamagePopupItem", "飘字组件初始化完成");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置文本
    /// </summary>
    public void SetText(string text)
    {
        if (m_Text != null)
        {
            m_Text.text = text;
        }
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    public void SetColor(Color color)
    {
        if (m_Text != null)
        {
            m_Text.color = color;
        }
    }

    /// <summary>
    /// 设置位置
    /// 飘字向摄像机方向偏移 0.1 单位，确保不被目标对象遮挡
    /// </summary>
    public void SetPosition(Vector3 worldPosition)
    {
        // 尝试获取摄像机
        if (m_Camera == null)
        {
            m_Camera = CameraRegistry.PlayerCamera;
        }

        // 向摄像机方向偏移 0.1 单位
        Vector3 adjustedPosition = worldPosition;
        if (m_Camera != null)
        {
            Vector3 cameraDir = (m_Camera.transform.position - worldPosition).normalized;
            adjustedPosition = worldPosition + cameraDir * 0.1f;
        }

        transform.position = adjustedPosition;
        m_StartPosition = adjustedPosition;
    }

    /// <summary>
    /// 设置缩放
    /// </summary>
    public void SetScale(float scale)
    {
        m_BaseScale = scale;
        transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// 设置字体大小
    /// </summary>
    public void SetFontSize(float size)
    {
        if (m_Text != null)
        {
            m_Text.fontSize = size;
        }
    }

    /// <summary>
    /// 设置文本样式（加粗 + 渐变 + 描边）
    /// </summary>
    /// <param name="useBold">是否使用加粗</param>
    /// <param name="useGradient">是否使用颜色渐变</param>
    /// <param name="gradientColors">渐变颜色（四角：左下、左上、右上、右下）</param>
    /// <param name="outlineColor">描边颜色</param>
    /// <param name="outlineWidth">描边宽度</param>
    public void SetTextStyle(bool useBold, bool useGradient = false, Color[] gradientColors = null, Color? outlineColor = null, float outlineWidth = 0.2f)
    {
        if (m_Text == null) return;

        // 设置加粗
        if (useBold)
        {
            m_Text.fontStyle = FontStyles.Bold;
            DebugEx.Log("DamagePopupItem", "启用加粗文本");
        }
        else
        {
            m_Text.fontStyle = FontStyles.Normal;
        }

        // 设置描边
        if (outlineColor.HasValue)
        {
            m_Text.outlineColor = outlineColor.Value;
            m_Text.outlineWidth = outlineWidth;
            DebugEx.Log("DamagePopupItem", $"设置描边: 颜色={outlineColor.Value}, 宽度={outlineWidth}");
        }

        // 设置颜色渐变
        if (useGradient && gradientColors != null && gradientColors.Length >= 4)
        {
            // ⚠️ 重要：启用渐变时，必须将 Vertex Color 设置为白色
            // 因为 TMP 会将 color 与 gradient 相乘，非白色会影响渐变效果
            m_Text.color = Color.white;

            m_Text.enableVertexGradient = true;
            m_Text.colorGradient = new VertexGradient(
                gradientColors[0],  // 左下
                gradientColors[1],  // 左上
                gradientColors[2],  // 右上
                gradientColors[3]   // 右下
            );
            DebugEx.Log("DamagePopupItem", $"启用颜色渐变（基础色=白色）: 左下={gradientColors[0]}, 右上={gradientColors[2]}");
        }
        else
        {
            m_Text.enableVertexGradient = false;
            // 禁用渐变时，颜色由外部 SetColor 方法设置
            DebugEx.Log("DamagePopupItem", "禁用颜色渐变");
        }
    }

    /// <summary>
    /// 设置初始透明度
    /// </summary>
    /// <param name="alpha">透明度 (0-1)</param>
    public void SetInitialAlpha(float alpha)
    {
        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.alpha = alpha;
        }
    }

    /// <summary>
    /// 设置动画参数
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <param name="riseHeight">上升高度</param>
    public void SetAnimationParams(float duration, float riseHeight)
    {
        m_Duration = duration;
        m_RiseHeight = riseHeight;
    }

    /// <summary>
    /// 播放飘字动画
    /// </summary>
    public void Play(Action onComplete = null)
    {
        m_OnComplete = onComplete;
        m_ElapsedTime = 0f;
        m_IsPlaying = true;
        m_FrameCounter = 0;  // ⭐ 重置帧计数器

        // 重置状态
        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.alpha = 1f;
        }

        // ⭐ 获取摄像机引用
        if (m_Camera == null)
        {
            m_Camera = CameraRegistry.PlayerCamera;
        }

        // ⭐ 初始化距离缩放缓存
        m_CachedDistanceScale = CalculateDistanceScale();

        DebugEx.LogModule("DamagePopupItem", "开始播放飘字动画");
        PlayAnimationAsync().Forget();
    }

    /// <summary>
    /// 让飘字面向摄像机
    /// </summary>
    public void FaceCamera(Camera camera)
    {
        if (camera == null) return;

        // Billboard 效果：始终面向摄像机
        transform.rotation = camera.transform.rotation;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 异步播放动画
    /// </summary>
    private async UniTaskVoid PlayAnimationAsync()
    {
        // 使用自定义参数（如果已设置）
        float duration = m_Duration;
        float riseHeight = m_RiseHeight;

        while (m_IsPlaying && m_ElapsedTime < duration)
        {
            m_ElapsedTime += Time.deltaTime;
            float t = m_ElapsedTime / duration;

            FaceCamera(m_Camera);

            // 位置动画：缓慢上升，缓出效果
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
            Vector3 newPosition = m_StartPosition + Vector3.up * (riseHeight * easedT);
            transform.position = newPosition;

            // 透明度动画：后半段开始淡出
            if (t > 0.5f && m_CanvasGroup != null)
            {
                float fadeT = (t - 0.5f) / 0.5f;
                m_CanvasGroup.alpha = 1f - fadeT;
            }

            // ⭐ 性能优化：每 N 帧更新一次距离缩放，而非每帧计算
            m_FrameCounter++;
            if (m_FrameCounter >= DISTANCE_UPDATE_INTERVAL)
            {
                m_CachedDistanceScale = CalculateDistanceScale();
                m_FrameCounter = 0;
            }

            // 缩放动画：开始时弹跳效果
            if (t < 0.2f)
            {
                float scaleT = t / 0.2f;
                float bounce = 1f + 0.2f * Mathf.Sin(scaleT * Mathf.PI);
                transform.localScale = Vector3.one * bounce * m_BaseScale * m_CachedDistanceScale;
            }
            else
            {
                // 正常缩放（使用缓存的距离缩放）
                transform.localScale = Vector3.one * m_BaseScale * m_CachedDistanceScale;
            }

            await UniTask.Yield();
        }

        m_IsPlaying = false;
        m_OnComplete?.Invoke();
    }

    /// <summary>
    /// 根据距离计算缩放系数
    /// 使飘字在屏幕上保持恒定大小
    /// </summary>
    private float CalculateDistanceScale()
    {
        if (m_Camera == null)
        {
            return 1f;
        }

        // 计算到摄像机的距离
        float distance = Vector3.Distance(transform.position, m_Camera.transform.position);

        // 参考距离（在这个距离下，缩放为 1.0）
        float referenceDistance = 15f;

        // 根据距离线性调整缩放
        // 距离越远，缩放越大，以保持屏幕上的恒定大小
        float scale = distance / referenceDistance;

        // 限制缩放范围，避免过大或过小
        scale = Mathf.Clamp(scale, 0.5f, 1.5f);

        return scale;
    }

    #endregion
}
