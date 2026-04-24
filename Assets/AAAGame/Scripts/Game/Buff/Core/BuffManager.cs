using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// Buff 管理器，挂载于角色或实体上。
///
/// 两个列表：
///   m_Buffs         — 激活中的 Buff（每帧 Update，计时，触发效果）
///   m_InactiveBuffs — 休眠中的 Buff（有 ActivationCondition 但当前不满足）
///
/// 每帧检测休眠列表的条件是否满足 → 满足则迁移到激活列表并调用 OnEnter。
/// 激活列表中有条件的 Buff 若条件不再满足 → 调用 OnExit 并迁移回休眠列表。
/// </summary>
public class BuffManager : MonoBehaviour
{
    private List<IBuff> m_Buffs = new();
    private List<IBuff> m_InactiveBuffs = new();
    private List<IBuff> m_BuffsToRemove = new();
    // 迁移缓冲，避免遍历时修改集合
    private List<IBuff> m_ToDeactivate = new();
    private List<IBuff> m_ToActivate = new();

    private BuffContext m_Context;
    private int m_ConditionCheckFrame;
    private const int CONDITION_CHECK_INTERVAL = 3;

    public event Action<int> OnBuffAdded;
    public event Action<int> OnBuffRemoved;
    public event Action<int, int> OnBuffStackChanged;

    private void Awake()
    {
        m_Context = new BuffContext
        {
            Owner = gameObject,
            Transform = transform,
            Caster = null,
            OwnerAttribute = GetComponent<ChessAttribute>(),
            CasterAttribute = null,
            OwnerBuffManager = this
        };
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        bool checkCondition = m_ConditionCheckFrame % CONDITION_CHECK_INTERVAL == 0;
        m_ConditionCheckFrame++;

        // ── 1. 检测休眠 Buff 是否满足激活条件（每3帧一次）────────────────
        if (checkCondition)
        {
            for (int i = 0; i < m_InactiveBuffs.Count; i++)
            {
                var buff = m_InactiveBuffs[i];
                if (buff.ActivationCondition != null && buff.ActivationCondition())
                    m_ToActivate.Add(buff);
            }
            for (int i = 0; i < m_ToActivate.Count; i++)
                ActivateBuffInstance(m_ToActivate[i]);
            m_ToActivate.Clear();
        }

        // ── 2. 更新激活 Buff ─────────────────────────────────────────────
        for (int i = 0; i < m_Buffs.Count; i++)
        {
            var buff = m_Buffs[i];
            buff.OnUpdate(dt);

            if (buff.IsFinished)
            {
                m_BuffsToRemove.Add(buff);
                continue;
            }

            // 有条件 Buff：条件不再满足 → 回到休眠（每3帧一次）
            if (checkCondition && buff.ActivationCondition != null && !buff.ActivationCondition())
                m_ToDeactivate.Add(buff);
        }

        // ── 3. 回到休眠 ──────────────────────────────────────────────────
        for (int i = 0; i < m_ToDeactivate.Count; i++)
        {
            DeactivateBuffInstance(m_ToDeactivate[i]);
        }
        m_ToDeactivate.Clear();

        // ── 4. 移除已结束的 Buff ─────────────────────────────────────────
        if (m_BuffsToRemove.Count > 0)
        {
            for (int i = 0; i < m_BuffsToRemove.Count; i++)
                RemoveBuffInstance(m_BuffsToRemove[i]);
            m_BuffsToRemove.Clear();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 公开 API
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 添加激活 Buff（无条件，立即生效）。
    /// </summary>
    public void AddBuff(int buffId, GameObject caster = null, ChessAttribute casterAttr = null)
    {
        IBuff newBuff = CreateAndInit(buffId, caster, casterAttr);
        if (newBuff == null) return;

        // 已存在同 ID 的激活 Buff → 叠层
        var existing = GetBuff(buffId);
        if (existing != null)
        {
            existing.OnStack();
            OnBuffStackChanged?.Invoke(buffId, existing.StackCount);
            return;
        }

        // 已存在同 ID 的休眠 Buff → 先移除再重新添加
        RemoveInactiveBuff(buffId);

        m_Buffs.Add(newBuff);
        newBuff.OnEnter();
        DebugEx.LogModule("BuffManager", $"激活 Buff: {newBuff.BuffId}");
        OnBuffAdded?.Invoke(buffId);
    }

    /// <summary>
    /// 添加条件型 Buff 到休眠列表。
    /// condition 为 null 时使用 Buff 自身在 Init 里设置的 ActivationCondition。
    /// 若两者都为 null，则视为无条件，直接走 AddBuff 激活。
    /// </summary>
    public void AddInactiveBuff(int buffId, Func<bool> condition = null,
        GameObject caster = null, ChessAttribute casterAttr = null)
    {
        // 已存在同 ID 的激活 Buff → 仅更新条件
        var active = GetBuff(buffId);
        if (active != null)
        {
            if (condition != null) active.ActivationCondition = condition;
            return;
        }

        // 已存在同 ID 的休眠 Buff → 仅更新条件
        var inactive = GetInactiveBuff(buffId);
        if (inactive != null)
        {
            if (condition != null) inactive.ActivationCondition = condition;
            return;
        }

        IBuff newBuff = CreateAndInit(buffId, caster, casterAttr);
        if (newBuff == null) return;

        // 外部条件优先；否则用 Buff 自身在 Init 里设置的条件
        if (condition != null) newBuff.ActivationCondition = condition;

        if (newBuff.ActivationCondition == null)
        {
            // 没有任何条件，直接激活
            m_Buffs.Add(newBuff);
            newBuff.OnEnter();
            OnBuffAdded?.Invoke(buffId);
            return;
        }

        m_InactiveBuffs.Add(newBuff);
        DebugEx.LogModule("BuffManager", $"休眠 Buff 加入: {buffId}");
    }

    /// <summary>
    /// 彻底移除（从激活或休眠列表中都删掉）。
    /// </summary>
    public void RemoveBuff(int buffId)
    {
        var active = GetBuff(buffId);
        if (active != null) { RemoveBuffInstance(active); return; }
        RemoveInactiveBuff(buffId);
    }

    /// <summary>
    /// 将激活中的 Buff 转入休眠（不彻底删除，等条件满足再激活）。
    /// 适用于"日落长弓"昼夜切换等场景。
    /// </summary>
    public void DeactivateBuff(int buffId)
    {
        var buff = GetBuff(buffId);
        if (buff != null) DeactivateBuffInstance(buff);
    }

    public bool HasBuff(int buffId)        => GetBuff(buffId) != null;
    public bool HasInactiveBuff(int buffId) => GetInactiveBuff(buffId) != null;

    public IBuff GetBuff(int buffId)
    {
        for (int i = 0; i < m_Buffs.Count; i++)
            if (m_Buffs[i].BuffId == buffId) return m_Buffs[i];
        return null;
    }

    public IBuff GetInactiveBuff(int buffId)
    {
        for (int i = 0; i < m_InactiveBuffs.Count; i++)
            if (m_InactiveBuffs[i].BuffId == buffId) return m_InactiveBuffs[i];
        return null;
    }

    public IReadOnlyList<IBuff> GetAllBuffs()         => m_Buffs.AsReadOnly();
    public IReadOnlyList<IBuff> GetAllInactiveBuffs() => m_InactiveBuffs.AsReadOnly();

    public void ClearAll()
    {
        foreach (var buff in m_Buffs)   buff.OnExit();
        foreach (var buff in m_InactiveBuffs) { /* 休眠 Buff 未激活，无需 OnExit */ }
        m_Buffs.Clear();
        m_InactiveBuffs.Clear();
    }

    // ══════════════════════════════════════════════════════════════════
    // 私有辅助
    // ══════════════════════════════════════════════════════════════════

    /// <summary>休眠 → 激活</summary>
    private void ActivateBuffInstance(IBuff buff)
    {
        m_InactiveBuffs.Remove(buff);

        // 若激活列表已有同 ID → 叠层
        var existing = GetBuff(buff.BuffId);
        if (existing != null)
        {
            existing.OnStack();
            OnBuffStackChanged?.Invoke(buff.BuffId, existing.StackCount);
            return;
        }

        m_Buffs.Add(buff);
        buff.OnEnter();
        DebugEx.LogModule("BuffManager", $"条件满足，Buff 激活: {buff.BuffId}");
        OnBuffAdded?.Invoke(buff.BuffId);
    }

    /// <summary>激活 → 休眠（条件不满足）</summary>
    private void DeactivateBuffInstance(IBuff buff)
    {
        if (!m_Buffs.Contains(buff)) return;

        int id = buff.BuffId;
        buff.OnExit();
        m_Buffs.Remove(buff);
        // 重置计时，等待下次激活时从头开始
        buff.Init(m_Context, GF.DataTable.GetDataTable<BuffTable>()?.GetDataRow(id));
        m_InactiveBuffs.Add(buff);
        DebugEx.LogModule("BuffManager", $"条件不满足，Buff 回到休眠: {id}");
        OnBuffRemoved?.Invoke(id);
    }

    /// <summary>彻底移除激活 Buff</summary>
    private void RemoveBuffInstance(IBuff buff)
    {
        if (!m_Buffs.Contains(buff)) return;
        int id = buff.BuffId;
        buff.OnExit();
        m_Buffs.Remove(buff);
        OnBuffRemoved?.Invoke(id);
    }

    /// <summary>彻底移除休眠 Buff</summary>
    private void RemoveInactiveBuff(int buffId)
    {
        for (int i = m_InactiveBuffs.Count - 1; i >= 0; i--)
        {
            if (m_InactiveBuffs[i].BuffId == buffId)
            {
                m_InactiveBuffs.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>创建并初始化 Buff 实例，不加入任何列表</summary>
    private IBuff CreateAndInit(int buffId, GameObject caster, ChessAttribute casterAttr)
    {
        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        var config = buffTable?.GetDataRow(buffId);
        if (config == null)
        {
            DebugEx.ErrorModule("BuffManager", $"无法找到 ID 为 {buffId} 的 Buff 配置");
            return null;
        }

        IBuff buff = BuffFactory.Create(buffId);
        if (buff == null)
        {
            DebugEx.ErrorModule("BuffManager", $"无法创建 Buff 实例: {buffId}");
            return null;
        }

        m_Context.Caster = caster;
        if (casterAttr != null)
            m_Context.CasterAttribute = casterAttr;
        else
            m_Context.CasterAttribute = caster != null ? caster.GetComponent<ChessAttribute>() : null;
        m_Context.OwnerAttribute = gameObject.GetComponent<ChessAttribute>();
        m_Context.OwnerBuffManager = this;

        buff.Init(m_Context, config);
        return buff;
    }
}
