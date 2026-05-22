using System;
using UnityEngine;

/// <summary>
/// 黑暗杨戬专属动画事件接收器
/// 继承通用的ChessAnimationEventReceiver，添加独立的技能二事件（Skill2_True）
/// </summary>
public class DarkYangyuanAnimationEventReceiver : ChessAnimationEventReceiver
{
    #region 事件

    /// <summary>技能2_True执行事件（独立技能二）</summary>
    public event Action OnSkill2TrueExecute;

    /// <summary>武器显示事件（参数为武器索引）</summary>
    public event Action<int> OnWeaponShow;

    /// <summary>武器隐藏事件（参数为武器索引）</summary>
    public event Action<int> OnWeaponHide;

    #endregion

    #region Animation Event 回调函数

    /// <summary>
    /// 技能2_True执行帧事件（独立技能二）
    /// </summary>
    public void AnimEvent_Skill2TrueExecute()
    {
        OnSkill2TrueExecute?.Invoke();
        DebugEx.Log(nameof(DarkYangyuanAnimationEventReceiver), $"{gameObject.name} 执行技能二效果");
    }

    /// <summary>
    /// 武器显示帧事件（溶解显示）
    /// Animation Event 参数：weaponIndex (0=武器1, 1=武器2)
    /// </summary>
    public void AnimEvent_ShowWeapon(int weaponIndex)
    {
        OnWeaponShow?.Invoke(weaponIndex);
        DebugEx.Log(nameof(DarkYangyuanAnimationEventReceiver), $"武器 {weaponIndex} 显示");
    }

    /// <summary>
    /// 武器隐藏帧事件（溶解隐藏）
    /// Animation Event 参数：weaponIndex (0=武器1, 1=武器2)
    /// </summary>
    public void AnimEvent_HideWeapon(int weaponIndex)
    {
        OnWeaponHide?.Invoke(weaponIndex);
        DebugEx.Log(nameof(DarkYangyuanAnimationEventReceiver), $"武器 {weaponIndex} 隐藏");
    }

    #endregion
}
