using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIAdaptiveScaleBySize : MonoBehaviour
{
    [Header("参考尺寸（设计稿/标准UI宽高）")]
    [SerializeField]
    private Vector2 m_ReferenceSize = new(180, 270);

    [Header("缩放目标列表")]
    [SerializeField]
    private List<RectTransform> m_ScaleTargets = new();

    private RectTransform m_SelfRect;
    private Vector2 m_LastSize;

    // 记录每个目标的初始状态和 CanvasGroup
    private Dictionary<RectTransform, TargetInitialState> m_InitialStates = new();
    private Dictionary<RectTransform, CanvasGroup> m_TargetCanvasGroups = new();

    private struct TargetInitialState
    {
        public Vector2 AnchoredPosition;
        public Vector3 LocalScale;
    }

    #region Unity 生命周期

    private void Awake()
    {
        m_SelfRect = transform as RectTransform;
        if (m_SelfRect == null)
        {
            DebugEx.Error("UIAdaptiveScaleBySize", "组件必须挂载在 RectTransform 上");
            enabled = false;
            return;
        }

        DebugEx.Log(
            "UIAdaptiveScaleBySize",
            $"[Awake] 初始化 - 对象:{gameObject.name}, 目标数量:{m_ScaleTargets.Count}"
        );

        // 只在 Awake 时捕获初始状态（对象池复用时不会重新捕获）
        if (m_InitialStates.Count == 0)
        {
            CaptureInitialStates();
            SetupCanvasGroups();
        }

        m_LastSize = m_SelfRect.rect.size;
        DebugEx.Log("UIAdaptiveScaleBySize", $"[Awake] 初始尺寸:{m_LastSize}");

        // 立即应用缩放
        ApplyScale();

        // 缩放完成后淡入显示
        StartCoroutine(FadeInTargets());
    }

    private void OnEnable()
    {
        if (m_SelfRect == null)
            return;

        DebugEx.Log("UIAdaptiveScaleBySize", $"[OnEnable] 对象激活 - 对象:{gameObject.name}");

        // 应用缩放
        ApplyScale();

        // 淡入显示
        StartCoroutine(FadeInTargets());
    }

    private void OnDisable()
    {
        // 对象池回收时，重置所有目标的 alpha 为 0
        ResetTargetsAlpha();
    }

    private void Update()
    {
        if (!enabled || m_SelfRect == null)
            return;

        Vector2 currentSize = m_SelfRect.rect.size;
        // 使用距离判断避免浮点误差
        if (Vector2.Distance(currentSize, m_LastSize) > 0.01f)
        {
            DebugEx.Log(
                "UIAdaptiveScaleBySize",
                $"[Update] 尺寸变化 - 对象:{gameObject.name}, {m_LastSize} → {currentSize}"
            );
            m_LastSize = currentSize;
            ApplyScale();
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 添加缩放目标
    /// </summary>
    public void AddTarget(RectTransform target)
    {
        if (target == null || m_ScaleTargets.Contains(target))
            return;

        m_ScaleTargets.Add(target);

        // 记录初始状态
        if (!m_InitialStates.ContainsKey(target))
        {
            m_InitialStates[target] = new TargetInitialState
            {
                AnchoredPosition = target.anchoredPosition,
                LocalScale = target.localScale,
            };
        }

        // 添加 CanvasGroup
        if (!m_TargetCanvasGroups.ContainsKey(target))
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            m_TargetCanvasGroups[target] = canvasGroup;
        }

        ApplyScaleToTarget(target);
    }

    /// <summary>
    /// 移除缩放目标
    /// </summary>
    public void RemoveTarget(RectTransform target)
    {
        m_ScaleTargets.Remove(target);
        m_InitialStates.Remove(target);
        m_TargetCanvasGroups.Remove(target);
    }

    /// <summary>
    /// 清空所有缩放目标
    /// </summary>
    public void ClearTargets()
    {
        m_ScaleTargets.Clear();
        m_InitialStates.Clear();
        m_TargetCanvasGroups.Clear();
    }

    /// <summary>
    /// 立即应用缩放到所有目标
    /// </summary>
    public void ApplyScale()
    {
        if (m_SelfRect == null || m_ScaleTargets == null)
            return;

        foreach (var target in m_ScaleTargets)
        {
            if (target != null)
            {
                ApplyScaleToTarget(target);
            }
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 捕获所有目标的初始状态
    /// </summary>
    private void CaptureInitialStates()
    {
        m_InitialStates.Clear();

        DebugEx.Log(
            "UIAdaptiveScaleBySize",
            $"[CaptureInitialStates] 开始捕获 - 对象:{gameObject.name}, 目标数量:{m_ScaleTargets.Count}"
        );

        foreach (var target in m_ScaleTargets)
        {
            if (target != null)
            {
                m_InitialStates[target] = new TargetInitialState
                {
                    AnchoredPosition = target.anchoredPosition,
                    LocalScale = target.localScale,
                };

                DebugEx.Log(
                    "UIAdaptiveScaleBySize",
                    $"[CaptureInitialStates] 目标:{target.name}, "
                        + $"初始位置:{target.anchoredPosition}, "
                        + $"初始缩放:{target.localScale}, "
                        + $"锚点:({target.anchorMin}, {target.anchorMax})"
                );
            }
            else
            {
                DebugEx.Warning("UIAdaptiveScaleBySize", $"[CaptureInitialStates] 发现空目标!");
            }
        }
    }

    /// <summary>
    /// 设置所有目标的 CanvasGroup（初始 alpha=0）
    /// </summary>
    private void SetupCanvasGroups()
    {
        m_TargetCanvasGroups.Clear();

        DebugEx.Log(
            "UIAdaptiveScaleBySize",
            $"[SetupCanvasGroups] 开始设置 - 对象:{gameObject.name}, 目标数量:{m_ScaleTargets.Count}"
        );

        foreach (var target in m_ScaleTargets)
        {
            if (target != null)
            {
                var canvasGroup = target.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
                }

                // 初始设置为完全透明
                canvasGroup.alpha = 0f;
                m_TargetCanvasGroups[target] = canvasGroup;

                DebugEx.Log(
                    "UIAdaptiveScaleBySize",
                    $"[SetupCanvasGroups] 目标:{target.name}, 已设置 CanvasGroup alpha=0"
                );
            }
        }
    }

    /// <summary>
    /// 淡入显示所有目标（0.2秒内从 alpha=0 到 alpha=1）
    /// </summary>
    private IEnumerator FadeInTargets()
    {
        DebugEx.Log(
            "UIAdaptiveScaleBySize",
            $"[FadeInTargets] 开始淡入 - 对象:{gameObject.name}, 目标数量:{m_TargetCanvasGroups.Count}"
        );

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);

            foreach (var kvp in m_TargetCanvasGroups)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    kvp.Value.alpha = alpha;
                }
            }

            yield return null;
        }

        // 确保最终 alpha 为 1
        foreach (var kvp in m_TargetCanvasGroups)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Value.alpha = 1f;
            }
        }

        DebugEx.Log("UIAdaptiveScaleBySize", $"[FadeInTargets] 淡入完成 - 对象:{gameObject.name}");
    }

    /// <summary>
    /// 重置所有目标的 alpha 为 0（对象池回收时调用）
    /// </summary>
    private void ResetTargetsAlpha()
    {
        DebugEx.Log(
            "UIAdaptiveScaleBySize",
            $"[ResetTargetsAlpha] 重置透明度 - 对象:{gameObject.name}, 目标数量:{m_TargetCanvasGroups.Count}"
        );

        foreach (var kvp in m_TargetCanvasGroups)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Value.alpha = 0f;
            }
        }
    }

    private void ApplyScaleToTarget(RectTransform target)
    {
        if (m_SelfRect == null || target == null)
            return;

        // 如果没有初始状态，先记录
        if (!m_InitialStates.ContainsKey(target))
        {
            m_InitialStates[target] = new TargetInitialState
            {
                AnchoredPosition = target.anchoredPosition,
                LocalScale = target.localScale,
            };
        }

        Vector2 sourceSize = m_SelfRect.rect.size;

        // 验证参数有效性
        if (sourceSize.x <= 0f || sourceSize.y <= 0f)
        {
            return;
        }

        if (m_ReferenceSize.x <= 0f || m_ReferenceSize.y <= 0f)
        {
            DebugEx.Warning("UIAdaptiveScaleBySize", $"无效的参考尺寸: {m_ReferenceSize}");
            return;
        }

        // 计算缩放比例：取宽高比例的最小值（保持宽高比）
        float scaleX = sourceSize.x / m_ReferenceSize.x;
        float scaleY = sourceSize.y / m_ReferenceSize.y;
        float scale = Mathf.Min(scaleX, scaleY);

        var initialState = m_InitialStates[target];

        // 应用缩放到 localScale
        target.localScale = initialState.LocalScale * scale;

        // 判断是否为 Stretch 锚点（Min != Max）
        bool isStretchX = Mathf.Abs(target.anchorMin.x - target.anchorMax.x) > 0.01f;
        bool isStretchY = Mathf.Abs(target.anchorMin.y - target.anchorMax.y) > 0.01f;

        // 只对非 Stretch 的轴缩放 anchoredPosition（保持相对位置）
        Vector2 scaledPosition = initialState.AnchoredPosition;
        if (!isStretchX)
            scaledPosition.x *= scale;
        if (!isStretchY)
            scaledPosition.y *= scale;
        target.anchoredPosition = scaledPosition;

        // sizeDelta 保持不变（通过 localScale 已经实现了尺寸缩放）

        DebugEx.Log(
            "UIAdaptiveScaleBySize",
            $"应用缩放 - 源:{gameObject.name}({sourceSize}), 目标:{target.name}, "
                + $"参考:{m_ReferenceSize}, 缩放:{scale:F3}, "
                + $"位置:{initialState.AnchoredPosition} → {target.anchoredPosition}"
        );
    }

    #endregion
}
