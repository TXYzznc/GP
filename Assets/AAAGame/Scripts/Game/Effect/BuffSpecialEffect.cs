using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 类型效果
/// 通过 BuffManager 添加 Buff，有持续时间/图标/可驱散
/// </summary>
public class BuffSpecialEffect : SpecialEffectInstance
{
    private readonly List<int> m_AppliedBuffIds = new();
    private readonly List<GameObject> m_MultiTargets = new();

    /// <summary>
    /// 设置多目标（羁绊等场景需要对多个棋子应用 Buff）
    /// </summary>
    public void SetMultiTargets(List<GameObject> targets)
    {
        m_MultiTargets.Clear();
        if (targets != null)
            m_MultiTargets.AddRange(targets);
    }

    public override void Apply()
    {
        var buffIds = EffectData.GetParamValue<int[]>("BuffIds", null);
        var selfBuffIds = EffectData.GetParamValue<int[]>("SelfBuffIds", null);

        var targets = m_MultiTargets.Count > 0 ? m_MultiTargets : new List<GameObject> { Target };

        if (buffIds != null && buffIds.Length > 0)
        {
            foreach (int buffId in buffIds)
            {
                if (buffId <= 0) continue;
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    BuffApplyHelper.ApplyBuff(buffId, target, false, null);
                }
                m_AppliedBuffIds.Add(buffId);
            }
        }

        if (selfBuffIds != null && selfBuffIds.Length > 0)
        {
            foreach (int buffId in selfBuffIds)
            {
                if (buffId <= 0) continue;
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    BuffApplyHelper.ApplyBuff(buffId, target, false, null);
                }
                m_AppliedBuffIds.Add(buffId);
            }
        }

        IsActive = m_AppliedBuffIds.Count > 0;
    }

    public override void Remove()
    {
        if (!IsActive) return;

        var targets = m_MultiTargets.Count > 0 ? m_MultiTargets : new List<GameObject> { Target };

        foreach (int buffId in m_AppliedBuffIds)
        {
            foreach (var target in targets)
            {
                if (target == null) continue;
                var buffManager = target.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.RemoveBuff(buffId);
                }
            }
        }

        m_AppliedBuffIds.Clear();
        IsActive = false;
    }
}
