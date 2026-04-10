using System;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 玩家交互控制器
/// 独立于 PlayerController，用于角色展示等场景中的单独使用
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private Animator animator;

    private bool m_IsInteracting;
    private int m_CurrentInteractIndex;
    private Action m_OnInteractComplete;

    #region 初始化

    private void Awake()
    {
        // 获取 Animator 组件
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    #endregion

    #region 交互控制

    /// <summary>
    /// 触发交互动画
    /// </summary>
    /// <param name="interactIndex">交互动画索引</param>
    public void TriggerInteract(int interactIndex)
    {
        m_IsInteracting = true;
        m_CurrentInteractIndex = interactIndex;

        if (animator != null)
        {
            animator.SetBool("IsInteracting", true);
            animator.SetInteger("InteractIndex", interactIndex);
        }

        DebugEx.LogModule("PlayerInteraction", $"触发交互动画，索引: {interactIndex}");
    }

    /// <summary>
    /// 触发交互动画（带完成回调）
    /// 动画播放完毕后自动调用 onComplete
    /// </summary>
    public void TriggerInteractWithCallback(int interactIndex, Action onComplete)
    {
        if (m_IsInteracting)
        {
            DebugEx.WarningModule("PlayerInteraction", "已在交互中，拒绝重复触发");
            return;
        }
        m_OnInteractComplete = onComplete;
        TriggerInteract(interactIndex);
    }

    /// <summary>
    /// 结束交互状态
    /// </summary>
    public void EndInteract()
    {
        m_IsInteracting = false;

        if (animator != null)
        {
            animator.SetBool("IsInteracting", false);
        }

        // 触发完成回调
        var callback = m_OnInteractComplete;
        m_OnInteractComplete = null;
        callback?.Invoke();

        DebugEx.LogModule("PlayerInteraction", "交互结束");
    }

    /// <summary>
    /// 获取是否正在交互
    /// </summary>
    public bool IsInteracting()
    {
        return m_IsInteracting;
    }

    /// <summary>
    /// 获取当前交互索引
    /// </summary>
    public int GetCurrentInteractIndex()
    {
        return m_CurrentInteractIndex;
    }

    /// <summary>
    /// 设置 Animator（供外部调用）
    /// </summary>
    public void SetAnimator(Animator anim)
    {
        animator = anim;
    }

    #endregion

    #region 动画事件回调

    /// <summary>
    /// 动画事件：交互动画播放完毕时调用
    /// 在 Animator 的交互动画末尾通过 Animation Event 调用此方法
    /// </summary>
    public void OnInteractAnimationEnd()
    {
        EndInteract();
    }

    #endregion
}
