using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 模型预览控制器 - 专门用于UI模型预览的轻量级动画控制器
/// 只负责动画控制，不处理移动、重力等游戏逻辑
/// </summary>
public class ModelController : MonoBehaviour
{
    #region 私有字段

    private Animator m_Animator;
    private bool m_IsInteracting = false;

    #endregion

    #region 公共属性

    /// <summary>
    /// 是否正在播放交互动画
    /// </summary>
    public bool IsInteracting => m_IsInteracting;

    /// <summary>
    /// 获取 Animator 组件
    /// </summary>
    public Animator Animator => m_Animator;

    #endregion

    #region 初始化

    private void Awake()
    {
        // 获取 Animator 组件
        m_Animator = GetComponentInChildren<Animator>();
        if (m_Animator == null)
        {
            DebugEx.Warning("ModelController", "未找到 Animator 组件");
        }
        else
        {
            DebugEx.LogModule("ModelController", "ModelController 初始化完成，已获取 Animator 组件");
        }
    }

    private void Start()
    {
        // 启动时播放 Idle 动画
        PlayIdleAnimation();
    }

    #endregion

    #region 动画控制

    /// <summary>
    /// 播放 Idle 动画
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (m_Animator == null)
        {
            DebugEx.Warning("ModelController", "Animator 为空，无法播放 Idle 动画");
            return;
        }

        // 设置 Speed = 0.05f 让 Movement 混合树播放 Idle 动画
        m_Animator.SetFloat("Speed", 0.05f);
        // 确保 State = 0 (回到 Movement 状态)
        m_Animator.SetInteger("State", 0);
        m_IsInteracting = false;

        DebugEx.LogModule("ModelController", "播放 Idle 动画 (Speed=0.05, State=0)");
        
        // 验证参数设置
        StartCoroutine(VerifyIdleParameters());
    }

    /// <summary>
    /// 播放交互动画
    /// </summary>
    /// <param name="interactIndex">交互动画索引</param>
    public void PlayInteractAnimation(int interactIndex = 0)
    {
        if (m_Animator == null)
        {
            DebugEx.Warning("ModelController", "Animator 为空，无法播放交互动画");
            return;
        }

        if (m_IsInteracting)
        {
            DebugEx.Warning("ModelController", "正在播放交互动画，忽略新的请求");
            return;
        }

        m_IsInteracting = true;

        // 设置交互索引
        m_Animator.SetInteger("InteractIndex", interactIndex);

        // 设置 State = 4 触发交互动画（从 Movement 跳转到 Interact 状态）
        m_Animator.SetInteger("State", 4);

        DebugEx.LogModule("ModelController", $"播放交互动画 (State=4, InteractIndex={interactIndex})");
        
        // 添加状态检查日志
        StartCoroutine(CheckInteractAnimationState());

        // 延迟恢复到 Idle 状态（根据动画长度调整）
        Invoke(nameof(EndInteractAnimation), 2f);
    }

    /// <summary>
    /// 结束交互动画
    /// </summary>
    private void EndInteractAnimation()
    {
        if (m_Animator == null)
            return;

        m_IsInteracting = false;

        // 恢复到 Movement 状态，并设置 Speed = 0.05 播放 Idle
        m_Animator.SetInteger("State", 0);
        m_Animator.SetFloat("Speed", 0.05f);

        DebugEx.LogModule("ModelController", "交互动画结束，恢复 Idle (State=0, Speed=0)");
        
        // 验证状态是否正确切换
        StartCoroutine(VerifyReturnToIdle());
    }

    /// <summary>
    /// 强制停止交互动画
    /// </summary>
    public void StopInteractAnimation()
    {
        if (m_IsInteracting)
        {
            CancelInvoke(nameof(EndInteractAnimation));
            EndInteractAnimation();
            DebugEx.LogModule("ModelController", "强制停止交互动画");
        }
    }

    #endregion

    #region 验证和调试方法

    /// <summary>
    /// 验证 Idle 参数设置
    /// </summary>
    private System.Collections.IEnumerator VerifyIdleParameters()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (m_Animator != null)
        {
            float currentSpeed = m_Animator.GetFloat("Speed");
            int currentState = m_Animator.GetInteger("State");
            var animatorState = m_Animator.GetCurrentAnimatorStateInfo(0);
            
            DebugEx.LogModule("ModelController", $"参数验证 - Speed: {currentSpeed}, State: {currentState}, 当前状态哈希: {animatorState.shortNameHash}");
        }
    }

    /// <summary>
    /// 检查交互动画状态（用于调试）
    /// </summary>
    private System.Collections.IEnumerator CheckInteractAnimationState()
    {
        yield return new WaitForSeconds(0.1f); // 等待状态机更新
        
        if (m_Animator != null)
        {
            var currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
            int stateParam = m_Animator.GetInteger("State");
            int interactIndex = m_Animator.GetInteger("InteractIndex");
            
            DebugEx.LogModule("ModelController", $"交互动画状态检查 - State参数: {stateParam}, InteractIndex: {interactIndex}");
            DebugEx.LogModule("ModelController", $"当前动画状态哈希: {currentState.shortNameHash}");
        }
    }

    /// <summary>
    /// 验证是否成功返回到 Idle 状态
    /// </summary>
    private System.Collections.IEnumerator VerifyReturnToIdle()
    {
        yield return new WaitForSeconds(0.2f); // 等待状态转换完成
        
        if (m_Animator != null)
        {
            var currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
            float currentSpeed = m_Animator.GetFloat("Speed");
            int currentStateParam = m_Animator.GetInteger("State");
            
            DebugEx.LogModule("ModelController", $"返回验证 - State: {currentStateParam}, Speed: {currentSpeed}");
            DebugEx.LogModule("ModelController", $"当前状态哈希: {currentState.shortNameHash}");
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 检查是否有有效的 Animator
    /// </summary>
    public bool HasValidAnimator()
    {
        return m_Animator != null;
    }

    /// <summary>
    /// 获取当前动画状态信息
    /// </summary>
    public AnimatorStateInfo GetCurrentAnimatorState()
    {
        if (m_Animator != null)
        {
            return m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        return default(AnimatorStateInfo);
    }

    #endregion

    #region 清理

    private void OnDestroy()
    {
        // 取消所有延迟调用
        CancelInvoke();
        DebugEx.LogModule("ModelController", "ModelController 已销毁");
    }

    #endregion
}