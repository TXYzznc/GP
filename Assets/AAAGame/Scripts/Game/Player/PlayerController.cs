using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 玩家状态枚举
/// </summary>
public enum PlayerState
{
    Idle = 0, // 静止
    Walk = 1, // 行走
    SlowRun = 2, // 慢跑
    FastRun = 3, // 快跑
}

/// <summary>
/// 玩家角色控制器 - 基于相机方向的移动控制
/// 移动方向基于相机视角，角色模型平滑转向移动方向
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(BuffManager))]
public class PlayerController : MonoBehaviour
{
    #region 配置参数

    [Header("移动参数")]
    [SerializeField]
    private float walkSpeed = 2f;

    [SerializeField]
    private float slowRunSpeed = 4f;

    [SerializeField]
    private float fastRunSpeed = 8f;

    [SerializeField]
    private float acceleration = 10f;

    [SerializeField]
    private float deceleration = 60f;

    [SerializeField]
    private float friction = 6f;

    [Header("旋转参数")]
    [Tooltip("转向平滑时间")]
    [SerializeField]
    private float turnSmoothTime = 0.1f;

    [Header("重力参数")]
    [SerializeField]
    private float gravity = -20f;

    [SerializeField]
    private float groundCheckDistance = 0.2f;

    [SerializeField]
    private LayerMask groundLayers = -1;

    [Header("状态参数")]
    [SerializeField]
    private float doubleClickWindow = 0.5f;

    [Header("组件引用")]
    [Tooltip("第三人称相机")]
    [SerializeField]
    private ThirdPersonCamera cameraRig;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private PlayerInteraction playerInteraction;

    [Header("俯视角参数")]
    [Tooltip("俯视角旋转速度")]
    [SerializeField]
    private float topDownRotationSpeed = 10f;

    #endregion

    #region 私有字段

    private CharacterController m_CharacterController;

    // 移动状态
    private Vector3 m_Velocity;
    private Vector3 m_TargetDirection;
    private float m_CurrentSpeed;
    private bool m_IsGrounded;

    // 垂直速度（重力）
    private float m_VerticalVelocity;

    // 旋转状态
    private float m_CurrentYaw;
    private float m_TurnVelocity;

    // 状态系统
    private PlayerState m_CurrentState = PlayerState.Idle;
    private float m_CurrentMaxSpeed;

    // 双击检测
    private float m_LastMoveReleaseTime = -999f;
    private Vector2 m_LastMoveInput = Vector2.zero; // 记录上次移动输入（用于方向比较）
    private bool m_WasMoving = false;
    private bool m_DoubleClickPending = false; // 标记是否等待第二次点击

    // 斜坡处理
    private Vector3 m_SlopeNormal = Vector3.up;

    // 俯视角移动方向
    private Vector3 m_TopDownMoveDirection;

    #endregion

    #region 公共属性

    public PlayerState CurrentState => m_CurrentState;
    public Vector3 Velocity => m_Velocity;
    public bool IsGrounded => m_IsGrounded;
    public float CurrentSpeed => m_CurrentSpeed;

    #endregion

    #region 初始化

    private void Awake()
    {
        // 确保 BuffManager 存在
        if (GetComponent<BuffManager>() == null)
        {
            gameObject.AddComponent<BuffManager>();
            Log.Info("PlayerController: 自动添加了 BuffManager 组件");
        }

        m_CharacterController = GetComponent<CharacterController>();
        if (m_CharacterController == null)
        {
            m_CharacterController = gameObject.AddComponent<CharacterController>();
            m_CharacterController.radius = 0.5f;
            m_CharacterController.height = 2f;
            m_CharacterController.center = new Vector3(0, 1f, 0);
            Log.Info("PlayerController: 自动添加了 CharacterController 组件");
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (playerInteraction == null)
        {
            playerInteraction = GetComponent<PlayerInteraction>();
        }

        m_CurrentYaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        // 尝试自动查找相机
        if (cameraRig == null)
        {
            cameraRig = FindObjectOfType<ThirdPersonCamera>();
        }
    }

    private void OnEnable()
    {
        Log.Info($"PlayerController 已启用: {gameObject.name}");
    }

    private void OnDisable()
    {
        Log.Info($"PlayerController 已禁用: {gameObject.name}");
    }

    #endregion

    #region 更新逻辑

    private void Update()
    {
        // 1. 检测地面
        CheckGrounded();

        // 2. 处理移动输入
        HandleMovement();

        // 3. 处理模型旋转
        HandleModelRotation();

        // 4. 应用重力
        HandleGravity();

        // 5. 更新动画
        UpdateAnimation();

        // 6. 更新相机FOV
        UpdateCameraFOV();
    }

    #endregion

    #region 地面检测

    private void CheckGrounded()
    {
        m_IsGrounded = m_CharacterController.isGrounded;

        // 额外射线检测，用于斜坡法线
        if (
            Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance + 0.1f,
                groundLayers
            )
        )
        {
            m_SlopeNormal = hit.normal;
        }
        else
        {
            m_SlopeNormal = Vector3.up;
        }
    }

    #endregion

    #region 移动处理

    private void HandleMovement()
    {
        // 交互状态下禁止移动
        if (playerInteraction != null && playerInteraction.IsInteracting())
        {
            m_Velocity = Vector3.zero;
            m_CurrentSpeed = 0f;
            return;
        }

        // 获取输入
        Vector2 input = Vector2.zero;
        if (PlayerInputManager.Instance != null)
        {
            input = PlayerInputManager.Instance.Move;
        }

        bool isMoving = input.sqrMagnitude > 0.01f;

        // 处理状态转换
        HandleStateTransition(input, isMoving);

        // 更新当前速度
        UpdateStateSpeed();

        // ⭐ 根据视角模式选择移动逻辑
        if (PlayerInputManager.Instance != null)
        {
            if (PlayerInputManager.Instance.ViewMode == CameraViewMode.TopDown)
            {
                // 俯视角：世界坐标移动
                HandleTopDownMovement(input, isMoving);
            }
            else if (PlayerInputManager.Instance.ViewMode == CameraViewMode.Combat)
            {
                // ⭐ 战斗模式：基于相机实际朝向的移动
                HandleCombatMovement(input, isMoving);
            }
            else
            {
                // 第三人称：相机水平方向移动
                HandleThirdPersonMovement(input, isMoving);
            }
        }
        else
        {
            // 备用：默认使用第三人称移动
            HandleThirdPersonMovement(input, isMoving);
        }
    }

    /// <summary>
    /// 第三人称模式移动处理
    /// </summary>
    private void HandleThirdPersonMovement(Vector2 input, bool isMoving)
    {
        // 计算相机相对的移动方向
        m_TargetDirection = CalculateCameraRelativeDirection(input);

        // 计算目标速度
        float targetSpeed = m_TargetDirection.magnitude * m_CurrentMaxSpeed;

        // 平滑加减速
        if (targetSpeed > 0.01f)
        {
            // 加速
            m_CurrentSpeed = Mathf.MoveTowards(
                m_CurrentSpeed,
                targetSpeed,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            // 减速（带摩擦力）
            m_CurrentSpeed = Mathf.MoveTowards(m_CurrentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // 计算水平速度
        if (m_TargetDirection.sqrMagnitude > 0.01f)
        {
            m_Velocity = m_TargetDirection.normalized * m_CurrentSpeed;
        }
        else if (m_CurrentSpeed > 0.01f)
        {
            // 保持原有方向
            m_Velocity = m_Velocity.normalized * m_CurrentSpeed;
        }
        else
        {
            m_Velocity = Vector3.zero;
        }

        // 斜坡投影
        if (m_IsGrounded && m_SlopeNormal != Vector3.up)
        {
            m_Velocity = Vector3.ProjectOnPlane(m_Velocity, m_SlopeNormal);
        }

        // 应用移动
        m_CharacterController.Move(m_Velocity * Time.deltaTime);
    }

    /// <summary>
    /// 俯视角模式移动处理
    /// </summary>
    private void HandleTopDownMovement(Vector2 input, bool isMoving)
    {
        // 俯视角下，W/S 前后移动，A/D 左右移动
        // 输入直接对应世界坐标系方向
        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();
            m_TopDownMoveDirection = moveDirection;

            // 计算目标速度
            float targetSpeed = m_CurrentMaxSpeed;

            // 平滑加速
            m_CurrentSpeed = Mathf.MoveTowards(
                m_CurrentSpeed,
                targetSpeed,
                acceleration * Time.deltaTime
            );

            // 计算速度
            m_Velocity = moveDirection * m_CurrentSpeed;

            // 保存目标方向用于旋转
            m_TargetDirection = moveDirection;
        }
        else
        {
            // 减速
            m_CurrentSpeed = Mathf.MoveTowards(m_CurrentSpeed, 0f, deceleration * Time.deltaTime);

            if (m_CurrentSpeed > 0.01f)
            {
                m_Velocity = m_TopDownMoveDirection * m_CurrentSpeed;
            }
            else
            {
                m_Velocity = Vector3.zero;
            }
        }

        // 斜坡投影
        if (m_IsGrounded && m_SlopeNormal != Vector3.up)
        {
            m_Velocity = Vector3.ProjectOnPlane(m_Velocity, m_SlopeNormal);
        }

        // 应用移动
        m_CharacterController.Move(m_Velocity * Time.deltaTime);
    }

    /// <summary>
    /// 战斗模式移动处理
    /// 使用相机的实际朝向（包括俯仰角），按W键向画面上方移动
    /// </summary>
    private void HandleCombatMovement(Vector2 input, bool isMoving)
    {
        // 计算基于相机实际朝向的移动方向
        m_TargetDirection = CalculateCombatCameraRelativeDirection(input);

        // 计算目标速度
        float targetSpeed = m_TargetDirection.magnitude * m_CurrentMaxSpeed;

        // 平滑加减速
        if (targetSpeed > 0.01f)
        {
            // 加速
            m_CurrentSpeed = Mathf.MoveTowards(
                m_CurrentSpeed,
                targetSpeed,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            // 减速（带摩擦力）
            m_CurrentSpeed = Mathf.MoveTowards(m_CurrentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // 计算水平速度
        if (m_TargetDirection.sqrMagnitude > 0.01f)
        {
            m_Velocity = m_TargetDirection.normalized * m_CurrentSpeed;
        }
        else if (m_CurrentSpeed > 0.01f)
        {
            // 保持原有方向
            m_Velocity = m_Velocity.normalized * m_CurrentSpeed;
        }
        else
        {
            m_Velocity = Vector3.zero;
        }

        // 斜坡投影
        if (m_IsGrounded && m_SlopeNormal != Vector3.up)
        {
            m_Velocity = Vector3.ProjectOnPlane(m_Velocity, m_SlopeNormal);
        }

        // 应用移动
        m_CharacterController.Move(m_Velocity * Time.deltaTime);
    }

    /// <summary>
    /// 计算基于相机的移动方向
    /// </summary>
    private Vector3 CalculateCameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 cameraForward;
        Vector3 cameraRight;

        if (cameraRig != null)
        {
            // 使用相机的水平方向
            cameraForward = cameraRig.Forward;
            cameraRight = cameraRig.Right;
        }
        else
        {
            // 备用：使用主相机
            Camera mainCam = CameraRegistry.PlayerCamera;

            if (mainCam != null)
            {
                cameraForward = Vector3
                    .ProjectOnPlane(mainCam.transform.forward, Vector3.up)
                    .normalized;
                cameraRight = Vector3
                    .ProjectOnPlane(mainCam.transform.right, Vector3.up)
                    .normalized;
            }
            else
            {
                // 最备用：使用世界坐标
                cameraForward = Vector3.forward;
                cameraRight = Vector3.right;
            }
        }

        // 计算移动方向：input.y * 前方 + input.x * 右方
        Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;
        return moveDirection.normalized * input.magnitude;
    }

    /// <summary>
    /// 计算战斗模式下基于相机实际朝向的移动方向
    /// 使用相机的实际前方向（包括俯仰角），投影到水平面
    /// </summary>
    private Vector3 CalculateCombatCameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 cameraForward;
        Vector3 cameraRight;

        // 获取相机
        Camera mainCam = CameraRegistry.PlayerCamera;
        if (mainCam == null)
        {
            cameraForward = Vector3.forward;
            cameraRight = Vector3.right;
        }
        else
        {
            // 使用相机的实际朝向（包括俯仰角），然后投影到水平面
            cameraForward = new Vector3(
                mainCam.transform.forward.x,
                0f,
                mainCam.transform.forward.z
            ).normalized;
            cameraRight = new Vector3(
                mainCam.transform.right.x,
                0f,
                mainCam.transform.right.z
            ).normalized;

            // 如果投影后长度太小（相机几乎垂直向下），使用备用方向
            if (cameraForward.sqrMagnitude < 0.01f)
            {
                cameraForward = Vector3.forward;
                cameraRight = Vector3.right;
            }
        }

        // 计算移动方向：input.y * 前方 + input.x * 右方
        Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;

        return moveDirection.normalized * input.magnitude;
    }

    #endregion

    #region 模型旋转

    /// <summary>
    /// 处理模型平滑旋转
    /// </summary>
    private void HandleModelRotation()
    {
        // 只在移动时旋转
        if (m_TargetDirection.sqrMagnitude < 0.01f)
            return;

        // 计算目标角度
        float targetYaw = Mathf.Atan2(m_TargetDirection.x, m_TargetDirection.z) * Mathf.Rad2Deg;

        // 根据视角模式选择旋转速度
        float smoothTime = turnSmoothTime;
        if (
            PlayerInputManager.Instance != null
            && PlayerInputManager.Instance.ViewMode == CameraViewMode.TopDown
        )
        {
            // 俯视角下使用更快的旋转速度
            smoothTime = 1f / topDownRotationSpeed;
        }

        // 使用 SmoothDampAngle 平滑旋转
        m_CurrentYaw = Mathf.SmoothDampAngle(
            m_CurrentYaw,
            targetYaw,
            ref m_TurnVelocity,
            smoothTime
        );

        // 应用旋转
        transform.rotation = Quaternion.Euler(0f, m_CurrentYaw, 0f);
    }

    #endregion

    #region 重力处理

    private void HandleGravity()
    {
        if (m_IsGrounded)
        {
            // 在地面时重置垂直速度，但保持微小负值确保贴地
            if (m_VerticalVelocity < 0)
            {
                m_VerticalVelocity = -2f;
            }
        }
        else
        {
            // 不在地面时累积重力
            m_VerticalVelocity += gravity * Time.deltaTime;
        }

        // 应用垂直移动
        m_CharacterController.Move(Vector3.up * m_VerticalVelocity * Time.deltaTime);
    }

    #endregion

    #region 状态系统

    private void HandleStateTransition(Vector2 input, bool isMoving)
    {
        // 检测移动键的按下和释放事件（支持任意方向双击）
        bool isMovePressed = isMoving;

        // 检测释放事件 - 任何状态下释放都记录，用于双击检测
        if (!isMovePressed && m_WasMoving)
        {
            // 记录释放时间和方向，并标记等待第二次点击
            m_LastMoveReleaseTime = Time.time;
            // 注意：这里不需要记录，因为释放前的输入方向，但input此时已经是0了
            // 所以我们需要在m_WasMoving为true时保存上一次的输入
            m_DoubleClickPending = true; // 标记等待双击
            //Log.Debug($"[PlayerController] 移动键释放，等待双击检测。记录方向: {m_LastMoveInput}");
        }

        // 记录当前输入（用于下次释放时记录）
        if (isMovePressed)
        {
            m_LastMoveInput = input;
        }

        // 检测按下事件（双击逻辑）
        if (isMovePressed && !m_WasMoving)
        {
            if (m_DoubleClickPending && m_LastMoveReleaseTime > 0)
            {
                float timeSinceRelease = Time.time - m_LastMoveReleaseTime;

                // 检查是否是相同方向双击（比较两次按下的方向）。使用点积
                // input是当前按下的方向，m_LastMoveInput是上次释放前记录的方向
                bool isSameDirection = false;
                if (input.sqrMagnitude > 0.01f && m_LastMoveInput.sqrMagnitude > 0.01f)
                {
                    isSameDirection =
                        Vector2.Dot(input.normalized, m_LastMoveInput.normalized) > 0.8f;
                }

                // Log.Debug(
                //     $"[PlayerController] 移动键按下，当前方向: {input}, 上次方向: {m_LastMoveInput}, 时间差: {timeSinceRelease:F3}, 同方向: {isSameDirection}"
                // );

                if (timeSinceRelease < doubleClickWindow && isSameDirection)
                {
                    Log.Info($"[PlayerController] 双击检测成功，进入慢跑状态");
                    m_CurrentState = PlayerState.SlowRun;
                    m_LastMoveReleaseTime = -999f;
                    m_DoubleClickPending = false;
                    m_WasMoving = isMovePressed;
                    return;
                }
            }

            // 清除双击等待标记
            m_DoubleClickPending = false;
        }

        m_WasMoving = isMovePressed;

        // 常规状态转换（双击检测成功后会提前返回）
        // 注意：Idle状态下按移动键时，如果不是双击，则进入Walk（已经在上面处理）
        // 这里处理的是非双击情况的常规状态转换
        switch (m_CurrentState)
        {
            case PlayerState.Idle:
                if (isMoving)
                {
                    m_CurrentState = PlayerState.Walk;
                }
                break;

            case PlayerState.Walk:
                if (!isMoving)
                {
                    m_CurrentState = PlayerState.Idle;
                }
                break;

            case PlayerState.SlowRun:
                if (!isMoving)
                {
                    m_CurrentState = PlayerState.Idle;
                    m_LastMoveReleaseTime = -999f;
                    m_DoubleClickPending = false;
                }
                else if (
                    PlayerInputManager.Instance != null
                    && PlayerInputManager.Instance.SprintDown
                )
                {
                    m_CurrentState = PlayerState.FastRun;
                }
                break;

            case PlayerState.FastRun:
                if (!isMoving)
                {
                    m_CurrentState = PlayerState.Idle;
                    m_LastMoveReleaseTime = -999f;
                    m_DoubleClickPending = false;
                }
                else if (
                    PlayerInputManager.Instance != null
                    && PlayerInputManager.Instance.SprintDown
                )
                {
                    m_CurrentState = PlayerState.SlowRun;
                    m_CurrentSpeed = slowRunSpeed;
                }
                break;
        }

        // 超时清理
        if (
            m_DoubleClickPending
            && m_LastMoveReleaseTime > 0
            && (Time.time - m_LastMoveReleaseTime > doubleClickWindow)
        )
        {
            Log.Debug("[PlayerController] 双击等待超时");
            m_LastMoveReleaseTime = -999f;
            m_DoubleClickPending = false;
        }
    }

    private void UpdateStateSpeed()
    {
        switch (m_CurrentState)
        {
            case PlayerState.Idle:
                m_CurrentMaxSpeed = 0f;
                break;
            case PlayerState.Walk:
                m_CurrentMaxSpeed = walkSpeed;
                break;
            case PlayerState.SlowRun:
                m_CurrentMaxSpeed = slowRunSpeed;
                break;
            case PlayerState.FastRun:
                m_CurrentMaxSpeed = fastRunSpeed;
                break;
        }
    }

    #endregion

    #region 动画系统

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        float blendTreeSpeed = CalculateBlendTreeSpeed();
        animator.SetFloat("Speed", blendTreeSpeed);
    }

    private float CalculateBlendTreeSpeed()
    {
        switch (m_CurrentState)
        {
            case PlayerState.Idle:
                return 0.05f;

            case PlayerState.Walk:
                if (m_CurrentSpeed < 0.1f)
                    return 0.05f;
                float walkRatio = Mathf.Clamp01(m_CurrentSpeed / walkSpeed);
                return Mathf.Lerp(0.05f, 0.4f, walkRatio);

            case PlayerState.SlowRun:
                float slowRunRatio = Mathf.Clamp01(m_CurrentSpeed / slowRunSpeed);
                return Mathf.Lerp(0.4f, 0.7f, slowRunRatio);

            case PlayerState.FastRun:
                float fastRunRatio = Mathf.Clamp01(m_CurrentSpeed / fastRunSpeed);
                return Mathf.Lerp(0.7f, 1.0f, fastRunRatio);

            default:
                return 0.05f;
        }
    }

    #endregion

    #region 相机交互

    private void UpdateCameraFOV()
    {
        if (cameraRig == null)
            return;

        cameraRig.SetSprintMode(m_CurrentState == PlayerState.FastRun);
    }

    #endregion

    #region 公共接口

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// 传送到指定位置
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <param name="forward">可选的朝向（世界空间方向），如果为null则保持当前朝向</param>
    public void TeleportTo(Vector3 position, Vector3? forward = null)
    {
        m_CharacterController.enabled = false;
        transform.position = position;
        m_CharacterController.enabled = true;

        // 如果提供了朝向，设置角色朝向
        if (forward.HasValue && forward.Value.sqrMagnitude > 0.01f)
        {
            // 只使用水平方向（忽略Y轴）
            Vector3 horizontalForward = new Vector3(
                forward.Value.x,
                0f,
                forward.Value.z
            ).normalized;

            if (horizontalForward.sqrMagnitude > 0.01f)
            {
                // 计算目标角度
                float targetYaw =
                    Mathf.Atan2(horizontalForward.x, horizontalForward.z) * Mathf.Rad2Deg;

                // 立即设置朝向（不使用平滑）
                m_CurrentYaw = targetYaw;
                transform.rotation = Quaternion.Euler(0f, m_CurrentYaw, 0f);

                Log.Info($"角色传送到: {position}, 朝向: {horizontalForward}");
            }
            else
            {
                Log.Info($"角色传送到: {position}，朝向无效，保持原朝向");
            }
        }
        else
        {
            Log.Info($"角色传送到: {position}，保持原朝向");
        }

        m_Velocity = Vector3.zero;
        m_VerticalVelocity = 0f;
        m_CurrentSpeed = 0f;
    }

    public Vector3 GetVelocity()
    {
        return m_Velocity;
    }

    public float GetNormalizedSpeed()
    {
        if (m_CurrentMaxSpeed < 0.01f)
            return 0f;
        return Mathf.Clamp01(m_CurrentSpeed / m_CurrentMaxSpeed);
    }

    public float GetYRotation()
    {
        return m_CurrentYaw;
    }

    public PlayerState GetCurrentState()
    {
        return m_CurrentState;
    }

    public PlayerInteraction GetPlayerInteraction()
    {
        return playerInteraction;
    }

    /// <summary>
    /// 设置相机引用
    /// </summary>
    public void SetCameraRig(ThirdPersonCamera camera)
    {
        cameraRig = camera;
    }

    #endregion

    #region 生命周期

    private void OnDestroy()
    {
        // ⭐ 删除自动保存位置的机制
        // 玩家位置现在通过配置表的 DefaultSpawnPosId 来管理，不再读写存档位置
    }

    #endregion
}
