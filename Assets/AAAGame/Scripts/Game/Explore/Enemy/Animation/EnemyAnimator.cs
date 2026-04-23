using UnityEngine;

/// <summary>
/// 敌人动画控制器
/// 负责驱动敌人的 Animator，根据 AI 状态播放对应动画
/// 完全独立于 AI 状态机，通过组件通信实现松耦合
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    #region 私有字段

    /// <summary>Animator 组件</summary>
    private Animator m_Animator;

    /// <summary>敌人实体</summary>
    private EnemyEntity m_Entity;

    /// <summary>当前播放的动画类型</summary>
    private EnemyAnimationType m_CurrentAnimType = EnemyAnimationType.Idle;

    /// <summary>动画参数哈希值（提高性能）</summary>
    private static readonly int PARAM_ANIM_TYPE = Animator.StringToHash("AnimType");
    private static readonly int PARAM_MOVE_SPEED = Animator.StringToHash("MoveSpeed");

    #endregion

    #region 属性

    /// <summary>当前动画类型</summary>
    public EnemyAnimationType CurrentAnimType => m_CurrentAnimType;

    /// <summary>是否有有效的 Animator 组件</summary>
    public bool HasAnimator => m_Animator != null;

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化动画控制器
    /// </summary>
    public void Initialize(EnemyEntity entity)
    {
        m_Entity = entity;
        m_Animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (m_Animator != null)
        {
            m_Animator.SetInteger(PARAM_ANIM_TYPE, (int)EnemyAnimationType.Idle);
            DebugEx.LogModule("EnemyAnimator", $"{m_Entity.Config.Name} 动画控制器初始化完成");
        }
        else
        {
            DebugEx.WarningModule("EnemyAnimator",
                $"{m_Entity.Config.Name} 未找到 Animator 组件，动画功能不可用");
        }
    }

    /// <summary>
    /// 播放指定动画类型
    /// </summary>
    /// <param name="animType">动画类型</param>
    /// <param name="moveSpeed">移动速度（可选）</param>
    public void PlayAnimation(EnemyAnimationType animType, float moveSpeed = 1f)
    {
        if (m_Animator == null) return;

        // 相同动画不重复播放
        if (m_CurrentAnimType == animType) return;

        m_CurrentAnimType = animType;
        m_Animator.SetInteger(PARAM_ANIM_TYPE, (int)animType);
        m_Animator.SetFloat(PARAM_MOVE_SPEED, moveSpeed);

        DebugEx.LogModule("EnemyAnimator",
            $"{m_Entity.Config.Name} 播放动画: {animType} (速度: {moveSpeed:F2})");
    }

    /// <summary>
    /// 停止当前动画（恢复到待机）
    /// </summary>
    public void StopAnimation()
    {
        PlayAnimation(EnemyAnimationType.Idle);
    }

    #endregion
}
