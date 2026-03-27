using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 召唤师技能管理器（MonoBehaviour）
/// 管理主动技能与被动技能的生命周期，驱动输入检测
/// </summary>
public class SummonerSkillManager : MonoBehaviour
{
    public List<ISummonerSkill> Skills { get; private set; } = new();
    public List<ISummonerPassive> Passives { get; private set; } = new();

    private SummonerSkillContext m_Ctx;
    private bool m_IsActive;

    /// <summary>设置运行时上下文（UpdateSkillsFromData 前必须先调用）</summary>
    public void SetContext(SummonerSkillContext ctx)
    {
        m_Ctx = ctx;
    }

    /// <summary>战斗开始/结束时调用；false 时立即 Dispose 所有被动</summary>
    public void SetActive(bool active)
    {
        m_IsActive = active;

        if (!active)
        {
            for (int i = 0; i < Passives.Count; i++)
                Passives[i].Dispose();
        }
    }

    /// <summary>
    /// 根据已解锁技能 ID 列表重建技能实例
    /// 会查 SummonerSkillTable 区分主动/被动，用 Factory 创建并 Init
    /// </summary>
    public void UpdateSkillsFromData(IReadOnlyList<int> skillIds)
    {
        Skills.Clear();
        Passives.Clear();

        if (skillIds == null || m_Ctx == null)
            return;

        var table = GF.DataTable.GetDataTable<SummonerSkillTable>();
        if (table == null)
        {
            DebugEx.Error("[SummonerSkillManager] SummonerSkillTable 未加载");
            return;
        }

        for (int i = 0; i < skillIds.Count; i++)
        {
            int id = skillIds[i];
            var row = table.GetDataRow(id);
            if (row == null)
            {
                DebugEx.Error($"[SummonerSkillManager] SummonerSkillTable 中未找到 id={id}");
                continue;
            }

            // SkillType: 1=被动, 2=主动
            if (row.SkillType == 1)
            {
                var passive = SummonerSkillFactory.CreatePassive(id);
                if (passive == null) continue;
                passive.Init(m_Ctx, row);
                Passives.Add(passive);
            }
            else
            {
                var skill = SummonerSkillFactory.Create(id);
                if (skill == null) continue;
                skill.Init(m_Ctx, row);
                Skills.Add(skill);
            }
        }

        DebugEx.Log($"[SummonerSkillManager] 已加载 {Skills.Count} 个主动技能，{Passives.Count} 个被动技能");
    }

    private void Update()
    {
        if (!m_IsActive)
            return;

        float dt = Time.deltaTime;

        // Tick 所有主动技能（冷却倒计时）
        for (int i = 0; i < Skills.Count; i++)
            Skills[i].Tick(dt);

        // Tick 所有被动技能（条件检测）
        for (int i = 0; i < Passives.Count; i++)
            Passives[i].Tick(dt);

        // 检测召唤师技能输入（Q/E/R → 槽位 1/2/3）
        if (PlayerInputManager.Instance != null)
        {
            for (int slot = 1; slot <= 3; slot++)
            {
                if (PlayerInputManager.Instance.SummonerSkillDown(slot) && slot - 1 < Skills.Count)
                    Skills[slot - 1].TryCast();
            }
        }
    }
}
