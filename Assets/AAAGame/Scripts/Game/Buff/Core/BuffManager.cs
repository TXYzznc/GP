using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// Buff 管理器，挂载于角色或实体上
/// </summary>
public class BuffManager : MonoBehaviour
{
    // 当前所有激活的 Buff
    private List<IBuff> m_Buffs = new List<IBuff>();

    // 待移除的 Buff 列表（避免在遍历时修改集合）
    private List<IBuff> m_BuffsToRemove = new List<IBuff>();

    private BuffContext m_Context;

    /// <summary>
    /// Buff 被添加事件
    /// </summary>
    public event Action<int> OnBuffAdded;

    /// <summary>
    /// Buff 被移除事件
    /// </summary>
    public event Action<int> OnBuffRemoved;

    /// <summary>
    /// Buff 堆叠变化事件
    /// </summary>
    public event Action<int, int> OnBuffStackChanged; // buffId, newStackCount

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

        // 1. 更新所有 Buff
        for (int i = 0; i < m_Buffs.Count; i++)
        {
            var buff = m_Buffs[i];
            buff.OnUpdate(dt);

            if (buff.IsFinished)
            {
                m_BuffsToRemove.Add(buff);
            }
        }

        // 2. 移除已结束的 Buff
        if (m_BuffsToRemove.Count > 0)
        {
            foreach (var buff in m_BuffsToRemove)
            {
                RemoveBuffInstance(buff);
            }
            m_BuffsToRemove.Clear();
        }
    }

    /// <summary>
    /// 添加 Buff
    /// </summary>
    /// <param name="buffId">Buff ID</param>
    /// <param name="caster">施法者</param>
    public void AddBuff(int buffId, GameObject caster = null)
    {
        // 1. 获取配置
        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        var config = buffTable?.GetDataRow(buffId);

        if (config == null)
        {
            DebugEx.ErrorModule("BuffManager", $"无法找到 ID 为 {buffId} 的 Buff 配置");
            return;
        }

        // 2. 检查是否已存在同 ID Buff
        var existingBuff = GetBuff(buffId);
        if (existingBuff != null)
        {
            // 尝试叠层
            existingBuff.OnStack();
            DebugEx.LogModule("BuffManager", $"Buff {config.Name} (ID:{buffId}) 叠层，当前层数: {existingBuff.StackCount}");
            OnBuffStackChanged?.Invoke(buffId, existingBuff.StackCount);
            return;
        }

        // 3. 互斥逻辑 (TODO: 暂未实现 MutexGroup 逻辑，可在此处扩展)

        // 4. 创建新实例
        IBuff newBuff = BuffFactory.Create(buffId);

        // 如果工厂没有注册特定类，可以考虑使用通用 Buff 类（如果需要）
        // 目前策略：返回 null 则无法添加
        if (newBuff == null)
        {
            // DebugEx.Warning("BuffManager", $"未找到 ID 为 {buffId} 的 Buff 实现类，无法使用通用类");
            // newBuff = new GenericBuff(); // 这是一个可选的通用实现
            DebugEx.ErrorModule("BuffManager", $"无法创建 Buff 实例: {buffId}");
            return;
        }

        // 5. 初始化并生效
        m_Context.Caster = caster;
        m_Context.CasterAttribute = caster != null ? caster.GetComponent<ChessAttribute>() : null;
        m_Context.OwnerAttribute = gameObject.GetComponent<ChessAttribute>();
        m_Context.OwnerBuffManager = this;

        newBuff.Init(m_Context, config);
        m_Buffs.Add(newBuff);
        newBuff.OnEnter();

        DebugEx.LogModule("BuffManager", $"添加 Buff: {config.Name} (ID:{buffId})");
        OnBuffAdded?.Invoke(buffId);
    }

    /// <summary>
    /// 添加 Buff（扩展系统版本，支持传入施法者属性）
    /// </summary>
    /// <param name="buffId">Buff ID</param>
    /// <param name="caster">施法者 GameObject</param>
    /// <param name="casterAttr">施法者属性（用于伤害计算等）</param>
    public void AddBuff(int buffId, GameObject caster, ChessAttribute casterAttr)
    {
        // 1. 获取配置
        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        var config = buffTable?.GetDataRow(buffId);

        if (config == null)
        {
            DebugEx.ErrorModule("BuffManager", $"无法找到 ID 为 {buffId} 的 Buff 配置");
            return;
        }

        // 2. 检查是否已存在同 ID Buff
        var existingBuff = GetBuff(buffId);
        if (existingBuff != null)
        {
            existingBuff.OnStack();
            DebugEx.LogModule("BuffManager", $"Buff {config.Name} (ID:{buffId}) 叠层，当前层数: {existingBuff.StackCount}");
            OnBuffStackChanged?.Invoke(buffId, existingBuff.StackCount);
            return;
        }

        // 3. 创建新实例
        IBuff newBuff = BuffFactory.Create(buffId);
        if (newBuff == null)
        {
            DebugEx.ErrorModule("BuffManager", $"无法创建 Buff 实例: {buffId}");
            return;
        }

        // 4. 更新上下文（包含施法者属性）
        m_Context.Caster = caster;
        m_Context.CasterAttribute = casterAttr;
        m_Context.OwnerAttribute = gameObject.GetComponent<ChessAttribute>();
        m_Context.OwnerBuffManager = this;

        // 5. 初始化并生效
        newBuff.Init(m_Context, config);
        m_Buffs.Add(newBuff);
        newBuff.OnEnter();

        DebugEx.LogModule("BuffManager", $"添加 Buff: {config.Name} (ID:{buffId})");
        OnBuffAdded?.Invoke(buffId);
    }

    /// <summary>
    /// 移除指定 ID 的 Buff
    /// </summary>
    public void RemoveBuff(int buffId)
    {
        var buff = GetBuff(buffId);
        if (buff != null)
        {
            RemoveBuffInstance(buff);
        }
    }

    /// <summary>
    /// 检查是否拥有指定 Buff
    /// </summary>
    public bool HasBuff(int buffId)
    {
        return GetBuff(buffId) != null;
    }

    /// <summary>
    /// 获取指定 ID 的 Buff 实例
    /// </summary>
    public IBuff GetBuff(int buffId)
    {
        for (int i = 0; i < m_Buffs.Count; i++)
        {
            if (m_Buffs[i].BuffId == buffId)
            {
                return m_Buffs[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 获取所有当前激活的 Buff（只读）
    /// </summary>
    public IReadOnlyList<IBuff> GetAllBuffs()
    {
        return m_Buffs.AsReadOnly();
    }

    /// <summary>
    /// 内部移除逻辑
    /// </summary>
    private void RemoveBuffInstance(IBuff buff)
    {
        if (m_Buffs.Contains(buff))
        {
            int buffId = buff.BuffId;
            buff.OnExit();
            m_Buffs.Remove(buff);
            OnBuffRemoved?.Invoke(buffId);
            // DebugEx.Log("BuffManager", $"移除 Buff: {buff.BuffId}");
        }
    }

    /// <summary>
    /// 清除所有 Buff
    /// </summary>
    public void ClearAll()
    {
        foreach (var buff in m_Buffs)
        {
            buff.OnExit();
        }
        m_Buffs.Clear();
    }
}
