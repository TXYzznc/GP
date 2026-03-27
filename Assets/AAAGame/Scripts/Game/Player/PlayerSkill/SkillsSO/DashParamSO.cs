using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Skills/Params/Dash")]
public class DashParamSO : SkillParamSO
{
    [Header("手部挂点配置")]
    [Tooltip("手部骨骼的Tag（用于缩小查找范围）")]
    public string handBoneTag = "PlayerBone";

    [Tooltip("手部骨骼的名字（精确查找）")]
    public string handBoneName = "RightHand";

    [Header("道具配置")]
    [Tooltip("道具预制体资源ID（来自ResourceConfigTable配置）\n" +
             "配置步骤：\n" +
             "1. 在ResourceConfigTable中添加道具预制体路径\n" +
             "2. 记录生成的资源ID\n" +
             "3. 在此处填入该ID")]
    public int itemResourceId = 0;

    [Header("投掷参数")]
    [Tooltip("投掷力度")]
    public float throwForce = 15f;

    [Tooltip("投掷角度（向上的偏移角度，单位：度）")]
    public float throwAngle = 30f;

    [Tooltip("是否启用道具碰撞")]
    public bool enableCollision = true;
    
    [Tooltip("重力缩放系数（1.0 = 正常重力，< 1.0 = 轻飘，> 1.0 = 快速下坠）\n" +
           "推荐值：0.5-2.0\n" +
           "- 0.5：轻飘的抛物线，可达更远\n" +
           "- 1.0：正常重力\n" +
           "- 2.0：快速下坠，短距离投掷")]
    [Range(0.1f, 5.0f)]
    public float gravityScale = 1.0f;

    [Header("时间参数")]
    [Tooltip("等待状态最大时间（秒）")]
    public float waitingDuration = 3f;

    [Tooltip("飞行状态最大时间（秒）")]
    public float flyingDuration = 3f;

    #region 编辑器验证

#if UNITY_EDITOR
    /// <summary>
    /// Inspector 值验证回调
    /// </summary>
    private void OnValidate()
    {
        // 验证资源ID
        if (itemResourceId == 0)
        {
            DebugEx.Warning($"[DashParamSO] 道具资源ID未配置，请在ResourceConfigTable中配置道具预制体并填入ID", this);
        }

        // 验证投掷参数
        if (throwForce <= 0f)
        {
            DebugEx.Warning($"[DashParamSO] 投掷力度必须大于0", this);
            throwForce = 15f;
        }

        if (throwAngle < 0f || throwAngle > 90f)
        {
            DebugEx.Warning($"[DashParamSO] 投掷角度应在0-90度之间", this);
            throwAngle = Mathf.Clamp(throwAngle, 0f, 90f);
        }

        // 验证时间参数
        if (waitingDuration <= 0f)
        {
            DebugEx.Warning($"[DashParamSO] 等待时间必须大于0", this);
            waitingDuration = 3f;
        }

        if (flyingDuration <= 0f)
        {
            DebugEx.Warning($"[DashParamSO] 飞行时间必须大于0", this);
            flyingDuration = 3f;
        }
    }
#endif

    #endregion
}
