using System;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 动作类型
/// </summary>
public enum ChessActionType
{
    /// <summary>无动作</summary>
    None,
    /// <summary>普通攻击</summary>
    Attack,
    /// <summary>技能1</summary>
    Skill1,
    /// <summary>技能2/大招</summary>
    Skill2
}


/// <summary>
/// 通用棋子动画控制器
/// 所有棋子共用，通过统一的动画参数约定驱动
/// </summary>
public class ChessAnimator : MonoBehaviour
{
    #region 动画参数名称（统一约定）

    // Bool 参数
    private static readonly int PARAM_IS_MOVING = Animator.StringToHash("IsMoving");

    // Trigger 参数
    private static readonly int PARAM_ATTACK = Animator.StringToHash("Attack");
    private static readonly int PARAM_SKILL1 = Animator.StringToHash("Skill1");
    private static readonly int PARAM_SKILL2 = Animator.StringToHash("Skill2");
    private static readonly int PARAM_DEATH = Animator.StringToHash("Death");

    // Float 参数（攻速倍率）
    private static readonly int PARAM_ATTACK_SPEED = Animator.StringToHash("AttackSpeed");

    // 动画状态名称
    private const string STATE_ATTACK = "Attack";
    private const string STATE_SKILL1 = "Skill1";
    private const string STATE_SKILL2 = "Skill2";

    #endregion

    #region 配置

    /// <summary>基础攻击动画时长（秒），用于计算攻速倍率</summary>
    [SerializeField]
    [Tooltip("基础攻击动画时长（秒）")]
    private float m_BaseAttackDuration = 1.0f;

    /// <summary>最大攻速倍率（防止过快）</summary>
    [SerializeField]
    [Tooltip("最大攻速倍率")]
    private float m_MaxAttackSpeedMultiplier = 3.0f;

    /// <summary>最小攻速倍率（防止过慢）</summary>
    [SerializeField]
    [Tooltip("最小攻速倍率")]
    private float m_MinAttackSpeedMultiplier = 0.5f;

    #endregion

    #region 组件引用

    /// <summary>Unity Animator 组件</summary>
    private Animator m_Animator;

    /// <summary>棋子实体引用</summary>
    private ChessEntity m_Entity;

    /// <summary>移动控制器引用</summary>
    private IChessMovement m_Movement;

    /// <summary>动画事件接收器</summary>
    private ChessAnimationEventReceiver m_EventReceiver;

    #endregion

    #region 状态

    /// <summary>是否已死亡</summary>
    private bool m_IsDead;

    /// <summary>是否正在播放动作（攻击/技能）</summary>
    private bool m_IsPlayingAction;

    /// <summary>当前动作类型</summary>
    private ChessActionType m_CurrentActionType = ChessActionType.None;

    /// <summary>当前攻速倍率</summary>
    private float m_CurrentAttackSpeedMultiplier = 1.0f;

    #endregion

    #region 公共属性

    /// <summary>是否已初始化</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>是否正在播放动作动画</summary>
    public bool IsPlayingAction => m_IsPlayingAction;

    /// <summary>当前动作类型</summary>
    public ChessActionType CurrentActionType => m_CurrentActionType;

    /// <summary>是否正在使用技能</summary>
    public bool IsPlayingSkill => m_CurrentActionType == ChessActionType.Skill1 ||
                                   m_CurrentActionType == ChessActionType.Skill2;

    /// <summary>是否正在普攻</summary>
    public bool IsPlayingAttack => m_CurrentActionType == ChessActionType.Attack;

    /// <summary>是否已死亡</summary>
    public bool IsDead => m_IsDead;

    /// <summary>动画事件接收器</summary>
    public ChessAnimationEventReceiver EventReceiver => m_EventReceiver;

    /// <summary>当前攻击动画实际时长（考虑攻速后）</summary>
    public float CurrentAttackDuration => m_BaseAttackDuration / m_CurrentAttackSpeedMultiplier;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化动画控制器
    /// </summary>
    /// <param name="entity">棋子实体</param>
    public void Initialize(ChessEntity entity)
    {
        m_Entity = entity;

        // 获取 Animator 组件
        m_Animator = GetComponentInChildren<Animator>();
        if (m_Animator == null)
        {
            DebugEx.WarningModule("ChessAnimator", $"{gameObject.name} 未找到 Animator 组件");
            return;
        }

        // 获取或添加动画事件接收器
        m_EventReceiver = m_Animator.GetComponent<ChessAnimationEventReceiver>();
        if (m_EventReceiver == null)
        {
            m_EventReceiver = m_Animator.gameObject.AddComponent<ChessAnimationEventReceiver>();
        }

        // 监听动画完成事件
        m_EventReceiver.OnAnimationComplete += OnAnimationComplete;

        // 获取移动组件
        m_Movement = entity.Movement;

        // 监听状态变化事件
        entity.OnStateChanged += OnEntityStateChanged;

        // 初始化攻速
        UpdateAttackSpeed(1.0f);

        IsInitialized = true;
        DebugEx.LogModule("ChessAnimator", $"{gameObject.name} 初始化完成");
    }

    #endregion

    #region Unity 生命周期

    private void Update()
    {
        if (!IsInitialized || m_IsDead) return;

        // 更新移动动画状态
        UpdateMovementAnimation();
    }

    private void OnDestroy()
    {
        // 取消监听
        if (m_Entity != null)
        {
            m_Entity.OnStateChanged -= OnEntityStateChanged;
        }

        if (m_EventReceiver != null)
        {
            m_EventReceiver.OnAnimationComplete -= OnAnimationComplete;
        }
    }

    #endregion

    #region 攻速控制

    /// <summary>
    /// 根据攻击属性更新动画播放速度
    /// </summary>
    /// <param name="attackSpeed">攻速值（次/秒）</param>
    public void UpdateAttackSpeed(float attackSpeed)
    {
        // 计算攻速倍率：攻速 * 基础动画时长
        // 例如：攻速=2次/秒，基础动画=1秒，倍率=2（播放2倍速）
        float multiplier = attackSpeed * m_BaseAttackDuration;

        // 限制在合理范围内
        m_CurrentAttackSpeedMultiplier = Mathf.Clamp(multiplier, m_MinAttackSpeedMultiplier, m_MaxAttackSpeedMultiplier);

        // 更新 Animator 参数
        if (m_Animator != null)
        {
            m_Animator.SetFloat(PARAM_ATTACK_SPEED, m_CurrentAttackSpeedMultiplier);
        }
    }

    #endregion

    #region 动画播放接口

    /// <summary>
    /// 播放普攻动画
    /// </summary>
    /// <returns>返回实际播放时长（秒）</returns>
    public float PlayAttack()
    {
        if (m_IsDead || m_Animator == null) return 0f;

        m_IsPlayingAction = true;
        m_CurrentActionType = ChessActionType.Attack;  // ⭐ 记录动作类型
        m_Animator.SetTrigger(PARAM_ATTACK);

        float duration = CurrentAttackDuration;
        DebugEx.LogModule("ChessAnimator", $"{gameObject.name} 播放普攻动画，时长: {duration:F2}s，攻速倍率: {m_CurrentAttackSpeedMultiplier:F2}");

        return duration;
    }

    /// <summary>
    /// 播放技能1动画
    /// </summary>
    /// <param name="duration">动画持续时间（秒）</param>
    /// <returns>返回实际播放时长</returns>
    public float PlaySkill1(float duration = 1.0f)
    {
        if (m_IsDead || m_Animator == null) return 0f;

        m_IsPlayingAction = true;
        m_CurrentActionType = ChessActionType.Skill1;  // ⭐ 记录动作类型
        m_Animator.SetTrigger(PARAM_SKILL1);
        DebugEx.LogModule("ChessAnimator", $"{gameObject.name} 播放技能1动画");

        return duration;
    }

    /// <summary>
    /// 播放技能2/大招动画
    /// </summary>
    /// <param name="duration">动画持续时间（秒）</param>
    /// <returns>返回实际播放时长</returns>
    public float PlaySkill2(float duration = 1.5f)
    {
        if (m_IsDead || m_Animator == null) return 0f;

        m_IsPlayingAction = true;
        m_CurrentActionType = ChessActionType.Skill2;  // ⭐ 记录动作类型
        m_Animator.SetTrigger(PARAM_SKILL2);
        DebugEx.LogModule("ChessAnimator", $"{gameObject.name} 播放技能2/大招动画");

        return duration;
    }

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    public void PlayDeath()
    {
        if (m_IsDead || m_Animator == null) return;

        m_IsDead = true;
        m_IsPlayingAction = false;

        m_Animator.SetTrigger(PARAM_DEATH);
        DebugEx.LogModule("ChessAnimator", $"{gameObject.name} 播放死亡动画");
    }

    /// <summary>
    /// 强制结束动作播放
    /// </summary>
    public void EndAction()
    {
        m_IsPlayingAction = false;
        m_CurrentActionType = ChessActionType.None;  // ⭐ 清除动作类型
    }

    /// <summary>
    /// 强制打断当前动作（用于玩家移动打断普攻）
    /// </summary>
    /// <returns>是否成功打断</returns>
    public bool ForceInterruptAction()
    {
        if (!m_IsPlayingAction)
        {
            return false;  // 没有正在播放的动作
        }

        // 只能打断普攻，不能打断技能
        if (m_CurrentActionType != ChessActionType.Attack)
        {
            DebugEx.LogModule("ChessAnimator",
                $"{gameObject.name} 无法打断技能动作: {m_CurrentActionType}");
            return false;
        }

        // ⭐ 强制结束动作状态
        m_IsPlayingAction = false;
        m_CurrentActionType = ChessActionType.None;

        // ⭐ 关键：直接切换到 Idle 状态，然后立即切换到 Move
        if (m_Animator != null)
        {
            // 方法1：使用 CrossFade 强制切换到 Idle（推荐）
            m_Animator.CrossFade("Idle", 0.1f, 0);

            // 立即设置移动参数，让动画系统自动过渡到 Move
            m_Animator.SetBool(PARAM_IS_MOVING, true);

            DebugEx.LogModule("ChessAnimator",
                $"{gameObject.name} 强制打断普攻，切换到移动状态");
        }

        return true;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新移动动画状态
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (m_Animator == null || m_IsPlayingAction) return;

        bool isMoving = m_Movement != null && m_Movement.IsMoving;
        m_Animator.SetBool(PARAM_IS_MOVING, isMoving);
    }

    /// <summary>
    /// 实体状态变化回调
    /// </summary>
    private void OnEntityStateChanged(ChessState oldState, ChessState newState)
    {
        if (newState == ChessState.Dead)
        {
            PlayDeath();
        }
    }

    /// <summary>
    /// 动画播放完成回调
    /// </summary>
    private void OnAnimationComplete(string animName)
    {
        m_IsPlayingAction = false;
        m_CurrentActionType = ChessActionType.None;  // ⭐ 清除动作类型
        DebugEx.LogModule("ChessAnimator", $"{gameObject.name} 动画 {animName} 播放完成");
    }

    #endregion
}
