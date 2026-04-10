using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 交互提示 UI - 显示 "[F] 交互提示文本"
/// 跟随当前交互目标的世界位置显示
/// 需要用户创建 Prefab 并在 UITable 中注册
/// </summary>
public class InteractionPromptUI : UIFormBase
{
    [Header("组件引用（由 Variables 生成或手动绑定）")]
    [SerializeField] private Text tipText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("显示配置")]
    [Tooltip("提示框在交互点上方的偏移（世界空间 Y 轴）")]
    [SerializeField] private float worldYOffset = 1.5f;

    private RectTransform m_RectTransform;
    private InteractionDetector m_Detector;
    private IInteractable m_CurrentTarget;
    private Camera m_MainCamera;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_RectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        m_MainCamera = Camera.main;

        // 禁用射线拦截
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
        }

        // 查找 InteractionDetector 并订阅事件
        if (PlayerCharacterManager.Instance != null
            && PlayerCharacterManager.Instance.CurrentPlayerCharacter != null)
        {
            m_Detector = PlayerCharacterManager.Instance.CurrentPlayerCharacter
                .GetComponent<InteractionDetector>();
        }

        if (m_Detector != null)
        {
            m_Detector.OnTargetChanged += OnTargetChanged;
            // 同步当前状态
            OnTargetChanged(m_Detector.CurrentTarget);
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (m_Detector != null)
        {
            m_Detector.OnTargetChanged -= OnTargetChanged;
            m_Detector = null;
        }

        m_CurrentTarget = null;
        base.OnClose(isShutdown, userData);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        if (m_CurrentTarget == null) return;

        UpdatePosition();
    }

    private void OnTargetChanged(IInteractable newTarget)
    {
        m_CurrentTarget = newTarget;

        if (m_CurrentTarget != null)
        {
            // 显示提示
            if (tipText != null)
                tipText.text = $"[F] {m_CurrentTarget.InteractionTip}";

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
        else
        {
            // 隐藏提示
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }
    }

    private void UpdatePosition()
    {
        if (m_CurrentTarget == null || m_RectTransform == null) return;

        // 懒更新摄像机引用
        if (m_MainCamera == null)
            m_MainCamera = Camera.main;
        if (m_MainCamera == null) return;

        Transform point = m_CurrentTarget.InteractionPoint;
        if (point == null) return;

        // 世界坐标 → 屏幕坐标
        Vector3 worldPos = point.position + Vector3.up * worldYOffset;
        Vector3 screenPos = m_MainCamera.WorldToScreenPoint(worldPos);

        // 目标在相机背后时隐藏
        if (screenPos.z < 0)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            return;
        }

        if (canvasGroup != null && m_CurrentTarget != null)
            canvasGroup.alpha = 1f;

        // 屏幕坐标 → Canvas 本地坐标（Screen Space - Camera 模式）
        var parentRect = m_RectTransform.parent as RectTransform;
        if (parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPos,
            GF.UICamera,
            out Vector2 localPoint
        );
        m_RectTransform.anchoredPosition = localPoint;
    }
}
