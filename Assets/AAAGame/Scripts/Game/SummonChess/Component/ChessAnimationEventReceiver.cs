using System;
using UnityEngine;

/// <summary>
/// 棋子动画事件接收器
/// 挂载在棋子 Animator 的 GameObject 上，接收 Animation Event 并转发
/// </summary>
public class ChessAnimationEventReceiver : MonoBehaviour
{
    #region 事件

    /// <summary>普攻执行事件（命中帧事件）：表示开始执行攻击行为</summary>
    public event Action OnAttackExecute;

    /// <summary>技能1执行事件</summary>
    public event Action OnSkill1Execute;

    /// <summary>技能2执行事件</summary>
    public event Action OnSkill2Execute;

    /// <summary>动画播放完成事件</summary>
    public event Action<string> OnAnimationComplete;

    /// <summary>近战攻击结束事件（用于关闭持续碰撞）</summary>
    public event Action OnMeleeAttackEnd;

    #endregion

    #region Animation Event 回调函数（由动画帧事件调用）

    /// <summary>
    /// 普攻执行帧事件
    /// 在攻击动画的某个帧上通过 Animation Event 调用此方法
    /// 用于触发伤害计算、碰撞检测/发射弹道/执行技能
    /// </summary>
    public void AnimEvent_AttackExecute()
    {
        OnAttackExecute?.Invoke();
        DebugEx.LogModule("ChessAnimationEventReceiver", $"{gameObject.name} 执行普攻效果");
    }

    /// <summary>
    /// 技能1执行帧事件
    /// </summary>
    public void AnimEvent_Skill1Execute()
    {
        OnSkill1Execute?.Invoke();
        DebugEx.LogModule("ChessAnimationEventReceiver", $"{gameObject.name} 执行技能1效果");
    }

    /// <summary>
    /// 技能2执行帧事件
    /// </summary>
    public void AnimEvent_Skill2Execute()
    {
        OnSkill2Execute?.Invoke();
        DebugEx.LogModule("ChessAnimationEventReceiver", $"{gameObject.name} 执行大招效果");
    }

    /// <summary>
    /// 近战攻击结束帧事件
    /// 在挥砍动画结束时调用，用于关闭持续碰撞
    /// </summary>
    public void AnimEvent_MeleeAttackEnd()
    {
        OnMeleeAttackEnd?.Invoke();
    }

    /// <summary>
    /// 动画播放完成事件
    /// 在动画最后一帧通过 Animation Event 调用此方法
    /// </summary>
    /// <param name="animName">动画名称（Attack/Skill1/Skill2）</param>
    public void AnimEvent_AnimationComplete(string animName)
    {
        OnAnimationComplete?.Invoke(animName);
        DebugEx.LogModule("ChessAnimationEventReceiver", $"{animName} 动画完成");
    }

    #endregion
}
