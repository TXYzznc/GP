using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 棋子测试输入控制器
/// 仅用于开发测试，正式版本需移除
/// 空格=普攻，1=技能1，2=技能2，3=死亡
/// </summary>
public class ChessTestInput : MonoBehaviour
{
    #region 组件引用

    private ChessEntity m_Entity;
    private ChessAnimator m_Animator;

    #endregion

    #region 状态

    /// <summary>是否选中（只有选中的棋子才响应输入）</summary>
    private bool m_IsSelected;

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize(ChessEntity entity, ChessAnimator animator)
    {
        m_Entity = entity;
        m_Animator = animator;
    }

    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        m_IsSelected = selected;
    }

    /// <summary>
    /// 是否选中
    /// </summary>
    public bool IsSelected => m_IsSelected;

    #endregion

    #region Unity 生命周期

// 快捷键已移至 Tools > Clash of Gods > Test Manager 窗口管理
// #if UNITY_EDITOR || DEVELOPMENT_BUILD
//     private void Update()
//     {
//         if (!m_IsSelected || m_Entity == null) return;
//         if (m_Animator != null && m_Animator.IsDead) return;

//         // 空格 - 普攻
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             DoNormalAttack();
//         }

//         // 1 - 技能1
//         if (Input.GetKeyDown(KeyCode.Alpha1))
//         {
//             DoSkill1();
//         }

//         // 2 - 技能2/大招
//         if (Input.GetKeyDown(KeyCode.Alpha2))
//         {
//             DoSkill2();
//         }

//         // 3 - 死亡
//         if (Input.GetKeyDown(KeyCode.Alpha3))
//         {
//             DoDeath();
//         }
//     }
// #endif

    #endregion

    #region 动作执行

    /// <summary>
    /// 执行普攻
    /// </summary>
    private void DoNormalAttack()
    {
        if (m_Animator != null && m_Animator.IsPlayingAction) return;

        // 播放动画
        m_Animator?.PlayAttack();

        // TODO: 执行普攻逻辑（查找目标等）
        DebugEx.LogModule("ChessTestInput", $"{gameObject.name} 执行普攻");
    }

    /// <summary>
    /// 执行技能1
    /// </summary>
    private void DoSkill1()
    {
        if (m_Animator != null && m_Animator.IsPlayingAction) return;
        if (m_Entity?.Skill1 == null) return;

        // 尝试释放技能
        if (m_Entity.Skill1.TryCast())
        {
            m_Animator?.PlaySkill1();
            DebugEx.LogModule("ChessTestInput", $"{gameObject.name} 技能1释放成功");
        }
        else
        {
            DebugEx.WarningModule("ChessTestInput", $"{gameObject.name} 技能1无法释放");
        }
    }

    /// <summary>
    /// 执行技能2/大招
    /// </summary>
    private void DoSkill2()
    {
        if (m_Animator != null && m_Animator.IsPlayingAction) return;
        if (m_Entity?.Skill2 == null) return;

        // 尝试释放技能
        if (m_Entity.Skill2.TryCast())
        {
            m_Animator?.PlaySkill2();
            DebugEx.LogModule("ChessTestInput", $"{gameObject.name} 大招释放成功");
        }
        else
        {
            DebugEx.WarningModule("ChessTestInput", $"{gameObject.name} 大招无法释放");
        }
    }

    /// <summary>
    /// 执行死亡
    /// </summary>
    private void DoDeath()
    {
        if (m_Entity?.Attribute == null) return;

        // 将生命值设为0，触发死亡
        m_Entity.Attribute.TakeDamage(m_Entity.Attribute.CurrentHp + 1, true, true);
        DebugEx.LogModule("ChessTestInput", $"{gameObject.name} 执行死亡");
    }

    #endregion
}
