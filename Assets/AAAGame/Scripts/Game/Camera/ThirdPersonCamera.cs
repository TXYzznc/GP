using UnityEngine;

/// <summary>
/// 第三人称相机系统
/// 实现轨道旋转、平滑跟随、缩放、动态FOV、遮挡防穿等
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    #region 配置参数

    [Header("目标设置")]
    [Tooltip("相机的目标对象")]
    [SerializeField]
    private Transform target;

    [Tooltip("目标偏移，通常设置为角色头部位置")]
    [SerializeField]
    private Vector3 targetOffset = new(0f, 1.5f, 0f);

    [Header("视角模式")]
    [Tooltip("当前视角模式")]
    [SerializeField]
    private CameraViewMode viewMode = CameraViewMode.ThirdPerson;

    [Tooltip("俯视角目标偏移（独立于第三人称偏移）")]
    [SerializeField]
    private Vector3 topDownTargetOffset = new(0f, 1.5f, -5f);

    [Tooltip("俯视角默认高度")]
    [SerializeField]
    private readonly float topDownHeight = 22f;

    [Tooltip("俯视角最小高度")]
    [SerializeField]
    private readonly float topDownMinHeight = 20f;

    [Tooltip("俯视角最大高度")]
    [SerializeField]
    private readonly float topDownMaxHeight = 25f;

    [Tooltip("俯视角高度调整速度")]
    [SerializeField]
    private readonly float topDownHeightSpeed = 3f;

    [Tooltip("俯视角平滑跟随时间")]
    [SerializeField]
    private readonly float topDownFollowSmoothTime = 0.2f;

    [Tooltip("俯视角俯仰角度，90=垂直向下，50=斜向下")]
    [SerializeField]
    private readonly float topDownPitchAngle = 50f;

    [Header("轨道设置")]
    [Tooltip("水平旋转灵敏度")]
    [SerializeField]
    private readonly float orbitSensitivityX = 3f;

    [Tooltip("垂直旋转灵敏度")]
    [SerializeField]
    private readonly float orbitSensitivityY = 2f;

    [Tooltip("旋转阻尼时间（缓冲效果）")]
    [SerializeField]
    private readonly float orbitDamping = 0.1f;

    [Tooltip("最小俯仰角")]
    [SerializeField]
    private readonly float minPitch = -40f;

    [Tooltip("最大俯仰角")]
    [SerializeField]
    private readonly float maxPitch = 40f;

    [Header("跟随设置")]
    [Tooltip("位置跟随平滑时间")]
    [SerializeField]
    private readonly float followSmoothTime = 0.1f;

    [Header("缩放设置")]
    [Tooltip("默认距离")]
    [SerializeField]
    private readonly float defaultDistance = 3f;

    [Tooltip("最小距离")]
    [SerializeField]
    private readonly float minDistance = 5f;

    [Tooltip("最大距离")]
    [SerializeField]
    private readonly float maxDistance = 7f;

    [Tooltip("缩放速度")]
    [SerializeField]
    private readonly float zoomSpeed = 2f;

    [Tooltip("缩放平滑时间")]
    [SerializeField]
    private readonly float zoomSmoothTime = 0.2f;

    [Header("动态FOV")]
    [Tooltip("默认视野角度")]
    [SerializeField]
    private readonly float defaultFOV = 60f;

    [Tooltip("冲刺时视野角度")]
    [SerializeField]
    private readonly float sprintFOV = 70f;

    [Tooltip("俯视角视野角度（从配置表读取，此为默认值）")]
    [SerializeField]
    private float topDownFOV = 0f;

    [Tooltip("FOV平滑时间")]
    [SerializeField]
    private readonly float fovSmoothTime = 0.3f;

    [Header("遮挡防穿")]
    [Tooltip("是否启用遮挡检测")]
    [SerializeField]
    private readonly bool enableOcclusion = true;

    [Tooltip("检测球半径")]
    [SerializeField]
    private readonly float occlusionRadius = 0.3f;

    [Tooltip("遮挡缓冲距离")]
    [SerializeField]
    private readonly float occlusionBuffer = 0.1f;

    [Tooltip("遮挡恢复速度")]
    [SerializeField]
    private readonly float occlusionRecoverySpeed = 2f;

    #endregion

    #region 私有字段

    private Camera m_Camera;

    // 轨道旋转状态
    private float m_CurrentYaw;
    private float m_CurrentPitch;
    private Vector2 m_OrbitVelocity;
    private Vector2 m_SmoothOrbitVelocity;

    // 跟随状态
    private Vector3 m_CurrentPivotPosition;
    private Vector3 m_FollowVelocity;

    // 缩放状态
    private float m_TargetDistance;
    private float m_CurrentDistance;
    private float m_DistanceVelocity;

    // FOV状态
    private float m_TargetFOV;
    private float m_CurrentFOV;
    private float m_FOVVelocity;
    private bool m_IsSprinting;

    // 遮挡状态
    private float m_OccludedDistance;
    private bool m_IsOccluded;

    // 俯视角状态
    private float m_CurrentTopDownHeight;
    private float m_TargetTopDownHeight;
    private float m_HeightVelocity;

    // ⭐ 视角锁定状态（新增）
    private bool m_IsViewModeLocked;
    private CameraViewMode m_CachedViewMode;

    // ⭐ 平滑移动状态（新增）
    private bool m_IsSmoothMoving;
    private Vector3 m_TargetPosition;
    private Quaternion m_TargetRotation;
    private float m_SmoothMoveTime;
    private float m_SmoothMoveElapsed;

    // ⭐ FOV 覆盖状态（新增）
    private bool m_IsFOVOverridden;
    private float m_OverrideFOV;

    #endregion

    #region 公共属性

    /// <summary>
    /// 相机水平前方（忽略俯仰角）
    /// </summary>
    public Vector3 Forward
    {
        get
        {
            float yawRad = m_CurrentYaw * Mathf.Deg2Rad;
            return new(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
        }
    }

    /// <summary>
    /// 相机水平右方
    /// </summary>
    public Vector3 Right
    {
        get
        {
            float yawRad = (m_CurrentYaw + 90f) * Mathf.Deg2Rad;
            return new(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
        }
    }

    /// <summary>
    /// 当前水平角度（Yaw）
    /// </summary>
    public float CurrentYaw => m_CurrentYaw;

    /// <summary>
    /// 当前俯仰角度（Pitch）
    /// </summary>
    public float CurrentPitch => m_CurrentPitch;

    /// <summary>
    /// 当前距离
    /// </summary>
    public float CurrentDistance => m_CurrentDistance;

    #endregion

    #region 初始化

    private void Awake()
    {
        m_Camera = GetComponentInChildren<Camera>();
        if (m_Camera == null)
        {
            m_Camera = GetComponent<Camera>();
        }

        // 初始化状态
        m_TargetDistance = defaultDistance;
        m_CurrentDistance = defaultDistance;
        m_OccludedDistance = defaultDistance;

        m_TargetFOV = defaultFOV;
        m_CurrentFOV = defaultFOV;

        // 初始化俯视角高度
        m_CurrentTopDownHeight = topDownHeight;
        m_TargetTopDownHeight = topDownHeight;

        // 初始化角度，如果有目标，从目标朝向初始化
        if (target != null)
        {
            m_CurrentYaw = target.eulerAngles.y;
            m_CurrentPivotPosition = target.position + targetOffset;
        }
    }

    private void Start()
    {
        if (target != null)
        {
            // 立即更新到正确位置
            UpdateCameraPosition(true);
        }

        if (m_Camera != null)
        {
            m_Camera.fieldOfView = m_CurrentFOV;
            // ⭐ 设置剔除遮罩，排除UI Layer，避免伤害飘字重复显示
            SetupCameraCullingMask();

            // 禁用相机上的AudioListener组件，避免多个AudioListener冲突
            var audioListener = m_Camera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = false;
            }
        }

        // ⭐ 从配置表读取俯视角 FOV
        LoadTopDownFOVFromConfig();
    }

    /// <summary>
    /// 从配置表读取俯视角 FOV 设置
    /// </summary>
    private void LoadTopDownFOVFromConfig()
    {
        try
        {
            var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
            if (ruleRow != null)
            {
                topDownFOV = ruleRow.CameraView;
                DebugEx.LogModule("ThirdPersonCamera", $"从配置表读取俯视角 FOV: {topDownFOV}");
            }
            else
            {
                DebugEx.Warning("ThirdPersonCamera", "未找到 CombatRuleTable 配置，使用默认俯视角 FOV");
            }
        }
        catch (System.Exception ex)
        {
            DebugEx.Error("ThirdPersonCamera", $"读取俯视角 FOV 配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置摄像机剔除遮罩，排除UI相关Layer
    /// </summary>
    private void SetupCameraCullingMask()
    {
        if (m_Camera == null)
            return;

        // 获取默认的剔除遮罩（渲染所有Layer）
        int defaultMask = -1;

        // 使用LayerHelper排除UI相关Layer，避免伤害飘字重复显示
        LayerMask uiMask = LayerHelper.UIMask;
        defaultMask &= ~uiMask;

        // 应用剔除遮罩
        m_Camera.cullingMask = defaultMask;

        DebugEx.LogModule(
            "ThirdPersonCamera",
            $"摄像机剔除遮罩设置完成: 排除UI相关Layer (UIMask={uiMask.value})"
        );
        DebugEx.LogModule(
            "ThirdPersonCamera",
            $"最终剔除遮罩: {System.Convert.ToString(defaultMask, 2).PadLeft(32, '0')}"
        );
    }

    #endregion

    #region 更新逻辑

    private void LateUpdate()
    {
        if (target == null)
            return;

        // ⭐ 如果正在平滑移动，执行移动逻辑（新增）
        if (m_IsSmoothMoving)
        {
            UpdateSmoothMove();
            return;
        }

        // 同步视角模式
        SyncViewMode();

        if (viewMode == CameraViewMode.TopDown)
        {
            // 俯视角模式更新逻辑
            HandleTopDownMode();
        }
        else if (viewMode == CameraViewMode.Combat) // ⭐ 战斗模式分支
        {
            // ⭐ 战斗模式：相机位置固定，只处理动态FOV
            HandleCombatMode();
        }
        else
        {
            // 第三人称模式更新逻辑
            // 1. 处理鼠标输入（轨道旋转）
            HandleOrbitInput();

            // 2. 处理滚轮输入（缩放）
            HandleZoomInput();

            // 3. 更新跟随位置
            HandleFollow();

            // 4. 处理动态FOV
            HandleDynamicFOV();

            // 5. 处理遮挡
            HandleOcclusion();

            // 6. 更新相机位置
            UpdateCameraPosition(false);
        }
    }

    /// <summary>
    /// 同步视角模式（从InputManager获取）
    /// </summary>
    private void SyncViewMode()
    {
        // ⭐ 如果视角被锁定，跳过同步（新增）
        if (m_IsViewModeLocked)
        {
            return;
        }

        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.ViewMode != viewMode)
        {
            CameraViewMode previousMode = viewMode;
            viewMode = PlayerInputManager.Instance.ViewMode;

            DebugEx.LogModule("ThirdPersonCamera", $"视角模式切换: {previousMode} -> {viewMode}");

            // 切换视角时重置一些状态
            if (viewMode == CameraViewMode.TopDown)
            {
                // 切换到俯视角时重置目标高度
                m_TargetTopDownHeight = topDownHeight;
                DebugEx.LogModule("ThirdPersonCamera", $"切换到俯视角，设置 FOV: {topDownFOV}");
            }
            else if (previousMode == CameraViewMode.TopDown)
            {
                // 从俯视角切换出来时，恢复默认 FOV
                DebugEx.LogModule("ThirdPersonCamera", $"从俯视角切换出来，恢复默认 FOV: {defaultFOV}");
            }
        }
    }

    /// <summary>
    /// 处理俯视角模式
    /// </summary>
    private void HandleTopDownMode()
    {
        // 1. 处理高度调整（滚轮缩放）
        HandleTopDownHeight();

        // 2. 平滑跟随目标
        HandleTopDownFollow();

        // 3. 处理俯视角 FOV
        HandleTopDownFOV();

        // 4. 更新俯视角相机位置
        UpdateTopDownCameraPosition();
    }

    /// <summary>
    /// 处理俯视角高度调整
    /// </summary>
    private void HandleTopDownHeight()
    {
        if (PlayerInputManager.Instance == null)
            return;

        float scrollDelta = PlayerInputManager.Instance.ScrollDelta;

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            // 滚轮调整高度
            m_TargetTopDownHeight -= scrollDelta * topDownHeightSpeed;
            m_TargetTopDownHeight = Mathf.Clamp(
                m_TargetTopDownHeight,
                topDownMinHeight,
                topDownMaxHeight
            );
        }

        // 平滑高度变化
        m_CurrentTopDownHeight = Mathf.SmoothDamp(
            m_CurrentTopDownHeight,
            m_TargetTopDownHeight,
            ref m_HeightVelocity,
            zoomSmoothTime
        );
    }

    /// <summary>
    /// 俯视角平滑跟随
    /// </summary>
    private void HandleTopDownFollow()
    {
        // ⭐ 使用俯视角专用的目标偏移
        Vector3 targetPivot = target.position + topDownTargetOffset;

        // 使用SmoothDamp平滑跟随
        m_CurrentPivotPosition = Vector3.SmoothDamp(
            m_CurrentPivotPosition,
            targetPivot,
            ref m_FollowVelocity,
            topDownFollowSmoothTime
        );
    }

    /// <summary>
    /// 处理俯视角 FOV
    /// </summary>
    private void HandleTopDownFOV()
    {
        if (m_Camera == null)
            return;

        // 俯视角模式下使用配置表中的 FOV 值
        if (m_IsFOVOverridden)
        {
            m_TargetFOV = m_OverrideFOV;
        }
        else
        {
            m_TargetFOV = topDownFOV;
        }

        // 平滑过渡FOV
        m_CurrentFOV = Mathf.SmoothDamp(
            m_CurrentFOV,
            m_TargetFOV,
            ref m_FOVVelocity,
            fovSmoothTime
        );

        m_Camera.fieldOfView = m_CurrentFOV;
    }

    #region 战斗模式处理

    /// <summary>
    /// 处理战斗模式
    /// 战斗模式下相机位置固定，不支持缩放
    /// </summary>
    private void HandleCombatMode()
    {
        // ⭐ 战斗模式下相机位置固定，不处理缩放输入
        // 只处理动态FOV
        HandleDynamicFOV();
    }

    #endregion

    /// <summary>
    /// 更新俯视角相机位置
    /// </summary>
    private void UpdateTopDownCameraPosition()
    {
        // 计算相机方向（基于俯仰角）
        float pitchRad = topDownPitchAngle * Mathf.Deg2Rad;

        // 计算偏移（Y分量 = sin(pitch) * height，Z分量 = -cos(pitch) * height
        Vector3 offset = new Vector3(0f, Mathf.Sin(pitchRad), -Mathf.Cos(pitchRad)) * m_CurrentTopDownHeight;

        // 俯视角位置（向上和向后）
        Vector3 targetPosition = m_CurrentPivotPosition + offset;

        // 俯视角旋转（使用设定的俯仰角度）
        Quaternion targetRotation = Quaternion.Euler(topDownPitchAngle, 0f, 0f);

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    #endregion

    #region 输入处理

    /// <summary>
    /// 处理鼠标输入（轨道旋转）
    /// </summary>
    private void HandleOrbitInput()
    {
        if (PlayerInputManager.Instance == null)
            return;
        if (!PlayerInputManager.Instance.IsMouseLocked)
            return;

        Vector2 mouseDelta = PlayerInputManager.Instance.MouseDelta;

        // 应用灵敏度
        Vector2 targetVelocity = new(
            mouseDelta.x * orbitSensitivityX,
            -mouseDelta.y * orbitSensitivityY
        );

        // 使用阻尼平滑（缓冲效果）
        m_OrbitVelocity = Vector2.SmoothDamp(
            m_OrbitVelocity,
            targetVelocity,
            ref m_SmoothOrbitVelocity,
            orbitDamping
        );

        // 应用旋转
        m_CurrentYaw += m_OrbitVelocity.x;
        m_CurrentPitch -= m_OrbitVelocity.y; // Y轴反转

        // 限制俯仰角
        m_CurrentPitch = Mathf.Clamp(m_CurrentPitch, minPitch, maxPitch);

        // 规范化Yaw角度
        if (m_CurrentYaw > 360f)
            m_CurrentYaw -= 360f;
        if (m_CurrentYaw < 0f)
            m_CurrentYaw += 360f;
    }

    #endregion

    #region 跟随逻辑

    /// <summary>
    /// 处理平滑跟随
    /// </summary>
    private void HandleFollow()
    {
        Vector3 targetPivot = target.position + targetOffset;

        // 使用SmoothDamp平滑跟随
        m_CurrentPivotPosition = Vector3.SmoothDamp(
            m_CurrentPivotPosition,
            targetPivot,
            ref m_FollowVelocity,
            followSmoothTime
        );
    }

    #endregion

    #region 缩放控制

    /// <summary>
    /// 处理滚轮缩放
    /// </summary>
    private void HandleZoomInput()
        {
            // ⭐ 战斗模式下不处理缩放输入
            if (viewMode == CameraViewMode.Combat)
            {
                return;
            }

            if (PlayerInputManager.Instance == null)
                return;

            float scrollDelta = PlayerInputManager.Instance.ScrollDelta;

            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                // 滚轮向前（正值）= 拉近，滚轮向后（负值）= 拉远
                m_TargetDistance -= scrollDelta * zoomSpeed;
                m_TargetDistance = Mathf.Clamp(m_TargetDistance, minDistance, maxDistance);
            }

            // 平滑缩放
            m_CurrentDistance = Mathf.SmoothDamp(
                m_CurrentDistance,
                m_TargetDistance,
                ref m_DistanceVelocity,
                zoomSmoothTime
            );
        }


    #endregion

    #region 动态FOV

    /// <summary>
    /// 处理动态FOV
    /// </summary>
    private void HandleDynamicFOV()
    {
        if (m_Camera == null)
            return;

        if (m_IsFOVOverridden)
        {
            m_TargetFOV = m_OverrideFOV;
        }
        else
        {
            m_TargetFOV = m_IsSprinting ? sprintFOV : defaultFOV;
        }

        // 平滑过渡FOV
        m_CurrentFOV = Mathf.SmoothDamp(
            m_CurrentFOV,
            m_TargetFOV,
            ref m_FOVVelocity,
            fovSmoothTime
        );

        m_Camera.fieldOfView = m_CurrentFOV;
    }

    /// <summary>
    /// 设置冲刺模式（影响FOV）
    /// </summary>
    public void SetSprintMode(bool isSprinting)
    {
        m_IsSprinting = isSprinting;
    }

    #endregion

    #region 遮挡系统

    /// <summary>
    /// 处理遮挡检测
    /// </summary>
    private void HandleOcclusion()
    {
        if (!enableOcclusion)
            return;

        Vector3 pivotPosition = m_CurrentPivotPosition;
        Vector3 cameraDirection = CalculateCameraDirection();
        float desiredDistance = m_CurrentDistance;

        // 球扫描检测（避免相机穿过几何体）
        if (
            Physics.SphereCast(
                pivotPosition,
                occlusionRadius,
                -cameraDirection,
                out RaycastHit hit,
                desiredDistance,
                LayerHelper.OcclusionMask, // ✅ 使用预设Mask
                QueryTriggerInteraction.Ignore
            )
        )
        {
            // 检测到遮挡，拉近相机
            float newDistance = Mathf.Max(hit.distance - occlusionBuffer, minDistance * 0.5f);

            m_OccludedDistance = newDistance;
            m_IsOccluded = true;
        }
        else
        {
            // 没有遮挡，平滑恢复
            if (m_IsOccluded)
            {
                m_OccludedDistance = Mathf.MoveTowards(
                    m_OccludedDistance,
                    desiredDistance,
                    occlusionRecoverySpeed * Time.deltaTime
                );

                if (Mathf.Approximately(m_OccludedDistance, desiredDistance))
                {
                    m_IsOccluded = false;
                }
            }
            else
            {
                // ⭐ 即使没有遮挡，也要平滑过渡到目标距离（处理缩放变化）
                m_OccludedDistance = Mathf.MoveTowards(
                    m_OccludedDistance,
                    desiredDistance,
                    occlusionRecoverySpeed * Time.deltaTime
                );
            }
        }
    }

    #endregion

    #region 位置更新

    /// <summary>
    /// 计算相机方向（基于Yaw和Pitch）
    /// </summary>
    private Vector3 CalculateCameraDirection()
    {
        float yawRad = m_CurrentYaw * Mathf.Deg2Rad;
        float pitchRad = m_CurrentPitch * Mathf.Deg2Rad;

        return new(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        );
    }

    /// <summary>
    /// 更新相机位置和旋转
    /// </summary>
    private void UpdateCameraPosition(bool immediate)
    {
        Vector3 cameraDirection = CalculateCameraDirection();

        // 使用遮挡后的距离
        float actualDistance = m_IsOccluded ? m_OccludedDistance : m_CurrentDistance;

        // 计算相机位置
        Vector3 targetPosition = m_CurrentPivotPosition - cameraDirection * actualDistance;

        // 计算相机旋转（朝向枢轴点）
        Quaternion targetRotation = Quaternion.LookRotation(cameraDirection);

        if (immediate)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            m_CurrentYaw = target.eulerAngles.y;
            m_CurrentPivotPosition = target.position + targetOffset;
            UpdateCameraPosition(true);
        }
    }

    // ⭐ 在这里添加平滑移动方法
    #region 平滑移动

    /// <summary>
    /// 平滑移动到目标位置和旋转
    /// </summary>
    /// <param name="targetPosition">目标位置</param>
    /// <param name="targetRotation">目标旋转</param>
    /// <param name="duration">移动时间（秒）</param>
    public void SmoothMoveTo(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        m_IsSmoothMoving = true;
        m_TargetPosition = targetPosition;
        m_TargetRotation = targetRotation;
        m_SmoothMoveTime = duration;
        m_SmoothMoveElapsed = 0f;
    }

    /// <summary>
    /// 更新平滑移动
    /// </summary>
    private void UpdateSmoothMove()
    {
        m_SmoothMoveElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(m_SmoothMoveElapsed / m_SmoothMoveTime);

        // 使用 SmoothStep 曲线使移动更自然
        t = Mathf.SmoothStep(0f, 1f, t);

        // 插值位置和旋转
        transform.position = Vector3.Lerp(transform.position, m_TargetPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, m_TargetRotation, t);

        // 移动完成
        if (m_SmoothMoveElapsed >= m_SmoothMoveTime)
        {
            transform.position = m_TargetPosition;
            transform.rotation = m_TargetRotation;
            m_IsSmoothMoving = false;
        }
    }

    #endregion

    /// <summary>
    /// 重置相机
    /// </summary>
    public void ResetCamera()
    {
        if (target != null)
        {
            m_CurrentYaw = target.eulerAngles.y;
            m_CurrentPitch = 0f;
            m_TargetDistance = defaultDistance;
            m_CurrentDistance = defaultDistance;
            m_OccludedDistance = defaultDistance;
            m_IsOccluded = false;
            m_IsFOVOverridden = false; // 重置时取消FOV覆盖

            m_CurrentPivotPosition = target.position + targetOffset;
            m_TargetFOV = defaultFOV;
            m_CurrentFOV = defaultFOV;

            UpdateCameraPosition(true);

            if (m_Camera != null)
            {
                m_Camera.fieldOfView = m_CurrentFOV;
            }
        }
    }

    /// <summary>
    /// 设置并锁定FOV（覆盖默认逻辑）
    /// </summary>
    public void SetOverrideFOV(float fov)
    {
        m_IsFOVOverridden = true;
        m_OverrideFOV = fov;
        DebugEx.LogModule("ThirdPersonCamera", $"设置覆盖FOV: {fov}");
    }

    /// <summary>
    /// 清除FOV覆盖，恢复默认逻辑
    /// </summary>
    public void ClearOverrideFOV()
    {
        m_IsFOVOverridden = false;
        DebugEx.LogModule("ThirdPersonCamera", "清除覆盖FOV，恢复默认逻辑");
    }

    /// <summary>
    /// 获取相机组件
    /// </summary>
    public Camera GetCamera()
    {
        return m_Camera;
    }

    /// <summary>
    /// 获取目标
    /// </summary>
    public Transform GetTarget()
    {
        return target;
    }

    /// <summary>
    /// 获取当前视角模式
    /// </summary>
    public CameraViewMode GetViewMode()
    {
        return viewMode;
    }

    /// <summary>
    /// 设置视角模式
    /// </summary>
    public void SetViewMode(CameraViewMode mode)
    {
        // ⭐ 检查视角是否被锁定（新增）
        if (m_IsViewModeLocked && mode != viewMode)
        {
            DebugEx.WarningModule("ThirdPersonCamera", $"视角模式已锁定，无法切换到 {mode}");
            return;
        }

        if (viewMode != mode)
        {
            DebugEx.LogModule("ThirdPersonCamera", $"切换视角模式: {viewMode} -> {mode}");
            viewMode = mode;
        }
    }

    #region 视角锁定

    /// <summary>
    /// 设置视角模式锁定状态
    /// </summary>
    /// <param name="locked">是否锁定</param>
    public void SetViewModeLocked(bool locked)
    {
        m_IsViewModeLocked = locked;
        DebugEx.LogModule("ThirdPersonCamera", $"视角模式锁定状态: {(locked ? "锁定" : "解锁")}");
    }

    /// <summary>
    /// 获取视角模式是否被锁定
    /// </summary>
    public bool IsViewModeLocked()
    {
        return m_IsViewModeLocked;
    }

    /// <summary>
    /// 缓存当前视角模式
    /// </summary>
    public void CacheCurrentViewMode()
    {
        m_CachedViewMode = viewMode;
        DebugEx.LogModule("ThirdPersonCamera", $"缓存视角模式: {m_CachedViewMode}");
    }

    /// <summary>
    /// 恢复缓存的视角模式
    /// </summary>
    public void RestoreCachedViewMode()
    {
        if (m_CachedViewMode != viewMode)
        {
            DebugEx.LogModule("ThirdPersonCamera", $"恢复视角模式: {m_CachedViewMode}");
            SetViewMode(m_CachedViewMode);
        }
    }

    /// <summary>
    /// 设置相机的水平旋转角度（Yaw）
    /// </summary>
    /// <param name="yaw">目标 Yaw 角度（0-360）</param>
    public void SetYaw(float yaw)
    {
        m_CurrentYaw = yaw;

        // 规范化角度到 0-360 范围
        while (m_CurrentYaw >= 360f)
            m_CurrentYaw -= 360f;
        while (m_CurrentYaw < 0f)
            m_CurrentYaw += 360f;

        DebugEx.LogModule("ThirdPersonCamera", $"设置相机 Yaw: {m_CurrentYaw}°");
    }

    /// <summary>
    /// 同步相机 Yaw 到目标的 Y 轴旋转
    /// </summary>
    public void SyncYawToTarget()
    {
        if (target != null)
        {
            float targetYaw = target.eulerAngles.y;
            SetYaw(targetYaw);
            DebugEx.LogModule("ThirdPersonCamera", $"同步相机 Yaw 到目标: {targetYaw}°");
        }
        else
        {
            DebugEx.WarningModule("ThirdPersonCamera", "无法同步 Yaw：目标为空");
        }
    }

    #endregion

    #endregion

    #region 调试绘制

    private void OnDrawGizmosSelected()
    {
        if (target == null)
            return;

        Vector3 pivotPosition = Application.isPlaying
            ? m_CurrentPivotPosition
            : target.position + targetOffset;

        // 绘制枢轴点
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pivotPosition, 0.2f);

        // 绘制目标距离范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivotPosition, minDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pivotPosition, maxDistance);

        // 绘制当前相机位置连线
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pivotPosition, transform.position);
            Gizmos.DrawWireSphere(transform.position, occlusionRadius);
        }
    }

    #endregion
}
