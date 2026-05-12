using UnityEngine;

/// <summary>
/// 脚部 IK 贴地系统
/// 挂在带有 Animator 的角色根对象上，Animator 的 Avatar 必须是 Humanoid
/// </summary>
[RequireComponent(typeof(Animator))]
public class FootIKController : MonoBehaviour
{
    private const string MODULE = "FootIKController";

    #region Inspector 参数

    [Header("启用控制")]
    [SerializeField, Tooltip("触发脚部 IK 的动画状态名（支持多个）")]
    private string[] m_IKStateNames = { "Idle", "Walk" };

    [Header("射线检测")]
    [SerializeField, Tooltip("射线起点相对脚踝的向上偏移")]
    private float m_RaycastOriginOffset = 0.5f;

    [SerializeField, Tooltip("射线向下检测的最大距离")]
    private float m_RaycastDistance = 1.5f;

    [SerializeField, Tooltip("地面层级")]
    private LayerMask m_GroundLayer = 1; // Default layer

    [Header("IK 权重平滑")]
    [SerializeField, Tooltip("IK 权重插值速度")]
    private float m_IKWeightSpeed = 10f;

    [Header("骨盆调整")]
    [SerializeField, Tooltip("骨盆高度调整的插值速度")]
    private float m_PelvisSpeed = 5f;

    [SerializeField, Tooltip("骨盆最大下移量，防止过度下蹲")]
    private float m_MaxPelvisOffset = 0.3f;

    #endregion

    #region 私有字段

    private Animator m_Animator;

    // 当前 IK 权重（平滑过渡用）
    private float m_LeftFootWeight;
    private float m_RightFootWeight;

    // 目标 IK 权重
    private float m_LeftFootTargetWeight;
    private float m_RightFootTargetWeight;

    // 骨盆原始 Y 偏移
    private float m_PelvisOffset;
    private float m_LastPelvisOffset;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        if (m_Animator == null)
        {
            DebugEx.Error(MODULE, "未找到 Animator 组件");
            enabled = false;
            return;
        }

        if (!m_Animator.isHuman)
        {
            DebugEx.Warning(MODULE, "Animator Avatar 不是 Humanoid，脚部 IK 无法工作");
            enabled = false;
            return;
        }

        DebugEx.Success(MODULE, "FootIKController 初始化完成");
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (m_Animator == null)
            return;

        // 检查当前是否处于需要 IK 的状态
        bool shouldApplyIK = IsInIKState();

        m_LeftFootTargetWeight = shouldApplyIK ? 1f : 0f;
        m_RightFootTargetWeight = shouldApplyIK ? 1f : 0f;

        // 平滑过渡权重
        m_LeftFootWeight = Mathf.Lerp(
            m_LeftFootWeight,
            m_LeftFootTargetWeight,
            Time.deltaTime * m_IKWeightSpeed
        );
        m_RightFootWeight = Mathf.Lerp(
            m_RightFootWeight,
            m_RightFootTargetWeight,
            Time.deltaTime * m_IKWeightSpeed
        );

        // 设置 IK 权重
        m_Animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, m_LeftFootWeight);
        m_Animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, m_LeftFootWeight);
        m_Animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, m_RightFootWeight);
        m_Animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, m_RightFootWeight);

        if (!shouldApplyIK && m_LeftFootWeight < 0.01f)
            return;

        // 处理左脚
        ProcessFootIK(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot);

        // 处理右脚
        ProcessFootIK(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot);

        // 调整骨盆高度，防止腿部过度拉伸
        AdjustPelvis();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 检查当前动画状态是否需要应用 IK
    /// </summary>
    private bool IsInIKState()
    {
        AnimatorStateInfo stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
        foreach (string stateName in m_IKStateNames)
        {
            if (stateInfo.IsName(stateName))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 处理单脚 IK：射线检测地面，设置 IK 目标位置和旋转
    /// </summary>
    private void ProcessFootIK(AvatarIKGoal goal, HumanBodyBones footBone)
    {
        // 获取动画系统计算出的脚踝位置
        Vector3 footPos = m_Animator.GetIKPosition(goal);
        Vector3 rayOrigin = footPos + Vector3.up * m_RaycastOriginOffset;

        if (
            Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                m_RaycastOriginOffset + m_RaycastDistance,
                m_GroundLayer
            )
        )
        {
            // 设置脚的目标位置（贴地）
            Vector3 targetPos = hit.point;
            m_Animator.SetIKPosition(goal, targetPos);

            // 根据地面法线调整脚的旋转
            Quaternion footRotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.forward, hit.normal),
                hit.normal
            );
            m_Animator.SetIKRotation(goal, footRotation);
        }
        else
        {
            // 没有检测到地面，保持动画原始位置
            m_Animator.SetIKPosition(goal, footPos);
            m_Animator.SetIKRotation(goal, m_Animator.GetIKRotation(goal));
        }
    }

    /// <summary>
    /// 调整骨盆高度：取两脚中较低的那只脚的偏移量，下移骨盆防止腿部拉伸
    /// </summary>
    private void AdjustPelvis()
    {
        Vector3 leftFootPos = m_Animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        Vector3 rightFootPos = m_Animator.GetIKPosition(AvatarIKGoal.RightFoot);

        // 计算两脚相对于动画原始位置的偏移
        float leftOffset =
            leftFootPos.y - m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
        float rightOffset =
            rightFootPos.y - m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y;

        // 取较低的偏移（需要下移骨盆时为负值）
        float targetOffset = Mathf.Min(leftOffset, rightOffset);
        targetOffset = Mathf.Clamp(targetOffset, -m_MaxPelvisOffset, 0f);

        // 平滑插值骨盆偏移
        m_PelvisOffset = Mathf.Lerp(
            m_LastPelvisOffset,
            targetOffset,
            Time.deltaTime * m_PelvisSpeed
        );
        m_LastPelvisOffset = m_PelvisOffset;

        // 应用骨盆偏移
        Transform pelvis = m_Animator.GetBoneTransform(HumanBodyBones.Hips);
        if (pelvis != null)
        {
            pelvis.position += Vector3.up * m_PelvisOffset;
        }
    }

    #endregion
}
