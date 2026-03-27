using System;
using UnityEngine;

/// <summary>
/// 战后隐身效果 - 挂载在玩家角色上
/// 分两个阶段：
///   1. Arm()   —— 战斗结束时立即调用，屏蔽视野检测，不计时，不触发事件
///   2. Activate() —— 探索 UI 显示后调用，开始计时并触发 OnStealthChanged
/// 玩家主动触发战斗时立即结束
/// </summary>
public class PostCombatStealth : MonoBehaviour
{
    #region 私有字段

    /// <summary>是否处于屏蔽检测状态（Arm 或 Active 均为 true）</summary>
    private bool m_IsArmed;

    /// <summary>是否处于正式激活状态（计时中）</summary>
    private bool m_IsActive;

    /// <summary>隐身剩余时间（秒）</summary>
    private float m_RemainingTime;
    private float ALLRemainingTime = 10;

    /// <summary>隐身时的透明度</summary>
    [SerializeField] private float m_StealthAlpha = 0.6f;

    private Renderer[] m_Renderers;
    private MaterialPropertyBlock m_PropertyBlock;
    private static readonly int s_StealthAlphaId = Shader.PropertyToID("_StealthAlpha");

    #endregion

    #region 属性

    /// <summary>是否屏蔽敌人检测（Arm 或 Active 均返回 true）</summary>
    public bool IsActive => m_IsArmed;

    /// <summary>是否正在计时（UI 可见阶段）</summary>
    public bool IsRunning => m_IsActive;

    /// <summary>隐身剩余时间（秒）</summary>
    public float RemainingTime => m_RemainingTime;

    #endregion

    #region 事件

    /// <summary>正式激活/结束事件（true=开始计时，false=结束）</summary>
    public event Action<bool> OnStealthChanged;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_Renderers = GetComponentsInChildren<Renderer>();
        m_PropertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (!m_IsActive) return;

        m_RemainingTime -= Time.deltaTime;
        if (m_RemainingTime <= 0f)
        {
            m_RemainingTime = 0f;
            Deactivate();
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 第一阶段：战斗结束时立即调用
    /// 立刻屏蔽视野检测，但不计时、不触发 UI 事件
    /// </summary>
    public void Arm()
    {
        if (m_IsArmed) return;
        m_IsArmed = true;
        m_RemainingTime = ALLRemainingTime;
        ApplyStealthVisual(m_StealthAlpha);
        DebugEx.LogModule("PostCombatStealth", "隐身预备（屏蔽检测），待 UI 就绪后正式激活");
    }

    /// <summary>
    /// 第二阶段：探索 UI 显示后调用
    /// 开始计时，触发 OnStealthChanged 通知 UI
    /// </summary>
    public void Activate()
    {
        if (!m_IsArmed) Arm();
        if (m_IsActive) return;
        m_IsActive = true;
        OnStealthChanged?.Invoke(true);
        DebugEx.LogModule("PostCombatStealth", $"隐身正式激活，持续 {m_RemainingTime:F0}s");
    }

    /// <summary>
    /// 立即结束隐身效果（玩家主动触发战斗时调用）
    /// </summary>
    public void Deactivate()
    {
        if (!m_IsArmed) return;

        m_IsArmed = false;
        m_IsActive = false;
        m_RemainingTime = 0f;
        ApplyStealthVisual(1f);
        OnStealthChanged?.Invoke(false);
        DebugEx.LogModule("PostCombatStealth", "隐身已结束");
    }

    #endregion

    #region 私有方法

    private void ApplyStealthVisual(float alpha)
    {
        foreach (var r in m_Renderers)
        {
            r.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetFloat(s_StealthAlphaId, alpha);
            r.SetPropertyBlock(m_PropertyBlock);
        }
    }

    #endregion
}
