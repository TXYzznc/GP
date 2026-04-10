using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 交互检测器 - 挂在玩家角色上
/// 负责检测范围内的可交互对象，选择最佳候选，并处理交互输入
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("检测配置")]
    [Tooltip("检测半径")]
    [SerializeField] private float detectionRadius = 3f;

    [Tooltip("可交互物体层")]
    [SerializeField] private LayerMask interactableLayer = -1;

    [Header("评分权重")]
    [SerializeField] private float priorityWeight = 10f;
    [SerializeField] private float distanceWeight = 5f;
    [SerializeField] private float facingWeight = 3f;
    [SerializeField] private float facingThreshold = 0.5f;

    [Header("组件引用")]
    [SerializeField] private PlayerInteraction playerInteraction;

    /// <summary>当前最佳交互目标</summary>
    public IInteractable CurrentTarget { get; private set; }

    /// <summary>当前目标变更事件（newTarget 可能为 null）</summary>
    public event Action<IInteractable> OnTargetChanged;

    private readonly List<IInteractable> m_Candidates = new List<IInteractable>();
    private SphereCollider m_DetectionCollider;

    #region 初始化

    private void Awake()
    {
        if (playerInteraction == null)
        {
            playerInteraction = GetComponent<PlayerInteraction>();
        }

        EnsureDetectionCollider();
    }

    private void EnsureDetectionCollider()
    {
        // 使用独立的子对象挂 Trigger，避免和角色自身的 Collider 冲突
        var detectorGo = new GameObject("InteractionTrigger");
        detectorGo.transform.SetParent(transform);
        detectorGo.transform.localPosition = Vector3.zero;
        detectorGo.layer = gameObject.layer;

        m_DetectionCollider = detectorGo.AddComponent<SphereCollider>();
        m_DetectionCollider.isTrigger = true;
        m_DetectionCollider.radius = detectionRadius;

        // 添加转发脚本，将触发事件转发到本组件
        var forwarder = detectorGo.AddComponent<InteractionTriggerForwarder>();
        forwarder.Init(this);
    }

    #endregion

    #region 更新逻辑

    private void Update()
    {
        // 清理已销毁的候选对象
        CleanupCandidates();

        // 评分选择最佳目标
        var bestTarget = EvaluateBestTarget();
        if (bestTarget != CurrentTarget)
        {
            CurrentTarget = bestTarget;
            OnTargetChanged?.Invoke(CurrentTarget);
        }

        // 处理交互输入
        HandleInteractInput();
    }

    #endregion

    #region 候选管理

    /// <summary>外部调用：对象进入检测范围</summary>
    public void OnCandidateEnter(Collider other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !m_Candidates.Contains(interactable))
        {
            m_Candidates.Add(interactable);
        }
    }

    /// <summary>外部调用：对象离开检测范围</summary>
    public void OnCandidateExit(Collider other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            m_Candidates.Remove(interactable);
            // 不在此处触发事件，由下一帧 Update 的 EvaluateBestTarget 统一处理
        }
    }

    private void CleanupCandidates()
    {
        for (int i = m_Candidates.Count - 1; i >= 0; i--)
        {
            // 所有 IInteractable 实现类均为 MonoBehaviour，通过 Unity 的 == 运算符检测已销毁对象
            if (m_Candidates[i] is MonoBehaviour mb && mb == null)
            {
                m_Candidates.RemoveAt(i);
            }
        }
    }

    #endregion

    #region 评分系统

    private IInteractable EvaluateBestTarget()
    {
        if (m_Candidates.Count == 0) return null;

        IInteractable best = null;
        float bestScore = float.MinValue;
        Vector3 playerPos = transform.position;
        Vector3 playerForward = transform.forward;

        for (int i = 0; i < m_Candidates.Count; i++)
        {
            var candidate = m_Candidates[i];
            if (!candidate.CanInteract(gameObject)) continue;

            Transform point = candidate.InteractionPoint;
            if (point == null) continue;

            Vector3 dirToTarget = (point.position - playerPos);
            float distance = dirToTarget.magnitude;
            float normalizedDist = Mathf.Clamp01(distance / detectionRadius);

            // 朝向加分：玩家面向目标时加分
            float dot = Vector3.Dot(playerForward.normalized, dirToTarget.normalized);
            float facingBonus = dot > facingThreshold ? 1f : 0f;

            float score = candidate.Priority * priorityWeight
                        + (1f - normalizedDist) * distanceWeight
                        + facingBonus * facingWeight;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    #endregion

    #region 交互处理

    private void HandleInteractInput()
    {
        if (PlayerInputManager.Instance == null) return;
        if (!PlayerInputManager.Instance.InteractKeyDown) return;
        if (CurrentTarget == null) return;
        if (!CurrentTarget.CanInteract(gameObject)) return;

        // 正在交互中，不重复触发
        if (playerInteraction != null && playerInteraction.IsInteracting()) return;

        ExecuteInteraction(CurrentTarget);
    }

    private void ExecuteInteraction(IInteractable target)
    {
        int animIndex = target.InteractAnimIndex;

        if (animIndex >= 0 && playerInteraction != null)
        {
            // 需要播放交互动画 → 注册回调 → 动画结束后执行逻辑
            playerInteraction.TriggerInteractWithCallback(animIndex, () =>
            {
                target.OnInteract(gameObject);
            });
        }
        else
        {
            // 无需动画，直接执行
            target.OnInteract(gameObject);
        }

        DebugEx.LogModule("Interaction", $"执行交互: {target.InteractionTip}");
    }

    #endregion

    #region 禁用时清理

    private void OnDisable()
    {
        m_Candidates.Clear();
        if (CurrentTarget != null)
        {
            CurrentTarget = null;
            OnTargetChanged?.Invoke(null);
        }
    }

    #endregion

    #region 提示 UI 管理

    private int m_PromptUIFormId = -1;

    private void Start()
    {
        OpenPromptUI();
    }

    private void OnDestroy()
    {
        ClosePromptUI();
    }

    private void OpenPromptUI()
    {
        // TODO: 用户需要先在 UITable 中注册 InteractionPromptUI，运行 DataTableGenerator 后取消注释
        // if (!GF.UI.HasUIForm(m_PromptUIFormId))
        // {
        //     m_PromptUIFormId = GF.UI.OpenUIForm(UIViews.InteractionPromptUI);
        // }
    }

    private void ClosePromptUI()
    {
        if (GF.UI.HasUIForm(m_PromptUIFormId))
        {
            GF.UI.CloseUIForm(m_PromptUIFormId);
            m_PromptUIFormId = -1;
        }
    }

    #endregion
}

/// <summary>
/// 将子对象的 Trigger 事件转发到 InteractionDetector
/// 独立类避免 Unity 序列化问题
/// </summary>
internal class InteractionTriggerForwarder : MonoBehaviour
{
    private InteractionDetector m_Detector;

    public void Init(InteractionDetector detector)
    {
        m_Detector = detector;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_Detector != null)
            m_Detector.OnCandidateEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (m_Detector != null)
            m_Detector.OnCandidateExit(other);
    }
}
