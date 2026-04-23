using UnityEngine;

/// <summary>
/// 可交互对象基类
/// 提供 IInteractable 的 MonoBehaviour 默认实现，简化子类开发
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("交互配置")]
    [SerializeField] protected string interactionTip = "交互";
    [SerializeField] protected int priority = 0;
    [SerializeField] protected int interactAnimIndex = -1;

    [Header("检测范围")]
    [Tooltip("触发器半径，用于 InteractionDetector 检测进入/离开")]
    [SerializeField] protected float interactionRadius = 1f;

    /// <summary>标记是否已开始交互（用于控制提示显示）</summary>
    protected bool m_HasStartedInteraction = false;

    public virtual string InteractionTip => interactionTip;
    public virtual int Priority => priority;
    public virtual Transform InteractionPoint => transform;
    public virtual int InteractAnimIndex => interactAnimIndex;
    public virtual bool HasStartedInteraction => m_HasStartedInteraction;

    public abstract bool CanInteract(GameObject player);
    public abstract void OnInteract(GameObject player);

    /// <summary>设置交互开始标记，并自动隐藏描边（通用规则）</summary>
    public virtual void SetInteractionStarted(bool started)
    {
        if (m_HasStartedInteraction != started)
        {
            m_HasStartedInteraction = started;
            // 开始交互时自动隐藏描边，这样所有子类都不需要重复实现
            if (started)
            {
                OnSetAsTarget(false);
            }
        }
    }

    /// <summary>当本对象成为/取消交互目标时由 InteractionDetector 调用</summary>
    public virtual void OnSetAsTarget(bool isTarget) { }

    protected virtual void Awake()
    {
        EnsureTriggerCollider();
    }

    /// <summary>
    /// 确保对象上有 Trigger Collider 用于被 InteractionDetector 检测
    /// </summary>
    private void EnsureTriggerCollider()
    {
        var col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            // 无 Collider 或已有非 Trigger Collider，额外添加 Trigger 用于检测
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = interactionRadius;
        }
    }
}
