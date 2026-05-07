using Cysharp.Threading.Tasks;
using GameExtension;
using GameFramework.DataTable;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>技能类型枚举</summary>
public enum SkillType
{
    NormalAttack,
    Skill1,
    Skill2,
    Ultimate
}

/// <summary>
/// 黑暗杨戬被动：黑暗神力 (ID=51)
/// 1. 所有攻击附带腐蚀效果
/// 2. 监听 HP 变化，触发多阶段切换
/// 3. 阶段一(HP 100%-60%)：基础形态
/// 4. 阶段二(HP 60%-30%)：应用阶段二强化Buff，召唤黑暗哮天犬
/// 5. 阶段三(HP 30%-0%)：应用死战Buff+吸血Buff，替换技能，移除从属单位
/// </summary>
public class DarkYangyuanPassive : IChessPassive
{
    #region 接口实现

    public int PassiveId => m_Config?.Id ?? 0;

    #endregion

    #region 私有字段

    private ChessContext m_Ctx;
    private SummonChessSkillTable m_Config;
    private int m_CurrentPhase = 1;
    private DarkYangyuanPhaseConfig m_PhaseConfig;
    private DarkWolfdogManager m_WolfdogManager; // 黑暗哮天犬管理器
    private bool m_IsInitialized;

    #endregion

    #region 公共方法

    public void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        m_Ctx = ctx;
        m_Config = config;
        m_IsInitialized = false;
        m_CurrentPhase = 1;

        // 初始化从属单位管理器
        m_WolfdogManager = new DarkWolfdogManager();
        m_WolfdogManager.Init(ctx.Entity);

        // 从棋子配置中解析 CustomData 获取阶段配置
        var chessConfig = ctx.Entity.Config;
        m_PhaseConfig = DarkYangyuanPhaseConfig.ParseFromCustomData(chessConfig.CustomData);

        if (m_PhaseConfig == null || m_PhaseConfig.Phases.Count == 0)
        {
            DebugEx.Error("DarkYangyuanPassive", "多阶段配置解析失败");
            return;
        }

        // 初始化阶段一
        InitializePhase(1);

        // 订阅 HP 变化事件
        if (m_Ctx?.Entity != null)
        {
            m_Ctx.Entity.OnHealthChanged += OnHealthChanged;
        }

        m_IsInitialized = true;
        DebugEx.Log("DarkYangyuanPassive", "黑暗神力被动初始化完成");
    }

    public void Tick(float dt)
    {
        // 无需每帧更新
    }

    public void Dispose()
    {
        // 取消订阅
        if (m_Ctx?.Entity != null)
        {
            m_Ctx.Entity.OnHealthChanged -= OnHealthChanged;
        }

        // 移除所有应用的阶段 Buff
        RemoveCurrentPhaseBuffs();

        // 清除从属单位
        if (m_WolfdogManager != null)
        {
            m_WolfdogManager.RemoveWolfdog();
        }
    }

    #endregion

    #region 私有方法

    private void OnHealthChanged(double currentHp, double maxHp)
    {
        if (!m_IsInitialized || m_PhaseConfig == null) return;

        double hpPercent = currentHp / maxHp * 100;
        int targetPhase = GetPhaseByHpPercent(hpPercent);

        if (targetPhase != m_CurrentPhase)
        {
            TransitionToPhase(targetPhase);
        }
    }

    private int GetPhaseByHpPercent(double hpPercent)
    {
        // 从高 HP 到低 HP 遍历，找到对应的阶段
        for (int i = m_PhaseConfig.Phases.Count - 1; i >= 0; i--)
        {
            if (hpPercent <= m_PhaseConfig.Phases[i].HealthPercentThreshold)
                return m_PhaseConfig.Phases[i].PhaseNum;
        }
        return 1;
    }

    private void TransitionToPhase(int newPhase)
    {
        DebugEx.Log("DarkYangyuanPassive", $"从阶段 {m_CurrentPhase} 切换到阶段 {newPhase}");

        // 1. 移除旧阶段 Buff
        RemoveCurrentPhaseBuffs();

        // 2. 清理旧从属单位
        if (newPhase != m_CurrentPhase && m_WolfdogManager != null)
        {
            m_WolfdogManager.RemoveWolfdog();
        }

        // 3. 应用新阶段配置
        m_CurrentPhase = newPhase;
        InitializePhase(newPhase);
    }

    private void InitializePhase(int phaseNum)
    {
        var phaseConfig = m_PhaseConfig.GetPhase(phaseNum);
        if (phaseConfig == null) return;

        // 应用 Buff
        ApplyPhaseBuffs(phaseConfig);

        // 替换技能（如有）
        if (phaseConfig.SkillOverride != null && phaseConfig.SkillOverride.Count > 0)
        {
            UpdateChessSkills(phaseConfig.SkillOverride);
        }

        // 修改大招参数（如有）
        if (phaseConfig.UltimateModifier != null && phaseConfig.UltimateModifier.Count > 0)
        {
            ModifyUltimateSkill(phaseConfig.UltimateModifier);
        }

        // 生成从属单位（如有）
        if (phaseConfig.SubordinateSpawn != null)
        {
            SpawnSubordinate(phaseConfig.SubordinateSpawn);
        }
    }

    private void ApplyPhaseBuffs(DarkYangyuanPhaseConfig.PhaseData phaseConfig)
    {
        if (m_Ctx?.BuffManager == null || phaseConfig.BuffIds == null) return;

        foreach (var buffId in phaseConfig.BuffIds)
        {
            m_Ctx.BuffManager.AddBuff(buffId, m_Ctx.Owner, m_Ctx.Attribute);
            DebugEx.Log("DarkYangyuanPassive", $"阶段 {phaseConfig.PhaseNum} 应用 Buff {buffId}");
        }
    }

    private void RemoveCurrentPhaseBuffs()
    {
        if (m_Ctx?.BuffManager == null) return;

        var currentPhaseConfig = m_PhaseConfig.GetPhase(m_CurrentPhase);
        if (currentPhaseConfig?.BuffIds == null) return;

        foreach (var buffId in currentPhaseConfig.BuffIds)
        {
            m_Ctx.BuffManager.RemoveBuff(buffId);
        }
    }

    private void UpdateChessSkills(System.Collections.Generic.Dictionary<string, int> skillOverride)
    {
        if (m_Ctx?.Entity == null) return;

        var skillTable = GF.DataTable.GetDataTable<SummonChessSkillTable>();
        if (skillTable == null) return;

        // 普攻替换
        if (skillOverride.TryGetValue("NormalAttackSkillId", out int normalAttackId))
        {
            ReplaceNormalAttack(normalAttackId, skillTable);
        }

        // 技能一替换
        if (skillOverride.TryGetValue("Skill1Id", out int skill1Id))
        {
            ReplaceSkill(skill1Id, SkillType.Skill1, skillTable);
        }

        // 技能二替换
        if (skillOverride.TryGetValue("Skill2Id", out int skill2Id))
        {
            ReplaceSkill(skill2Id, SkillType.Skill2, skillTable);
        }
    }

    private void ReplaceNormalAttack(int skillId, IDataTable<SummonChessSkillTable> skillTable)
    {
        var config = skillTable?.GetDataRow(skillId);
        if (config == null) return;

        var newAttack = ChessFactory.CreateNormalAttack(skillId);
        if (newAttack != null)
        {
            newAttack.Init(m_Ctx, config);
            m_Ctx.Entity.ReplaceNormalAttack(newAttack);
            DebugEx.Log("DarkYangyuanPassive", $"普攻替换为技能 {skillId}");
        }
    }

    private void ReplaceSkill(int skillId, SkillType skillType, IDataTable<SummonChessSkillTable> skillTable)
    {
        // skillId == 0 表示禁用该技能
        if (skillId == 0)
        {
            DebugEx.Log("DarkYangyuanPassive", $"{skillType} 已禁用");
            return;
        }

        var config = skillTable?.GetDataRow(skillId);
        if (config == null) return;

        var newSkill = ChessFactory.CreateSkill(skillId);
        if (newSkill != null)
        {
            newSkill.Init(m_Ctx, config);

            if (skillType == SkillType.Skill1)
            {
                m_Ctx.Entity.ReplaceSkill1(newSkill);
            }
            else if (skillType == SkillType.Skill2)
            {
                m_Ctx.Entity.ReplaceSkill2(newSkill);
            }

            DebugEx.Log("DarkYangyuanPassive", $"{skillType} 替换为技能 {skillId}");
        }
    }

    private void ModifyUltimateSkill(System.Collections.Generic.Dictionary<string, float> ultimateModifier)
    {
        if (m_Ctx?.Entity?.Skill2 == null) return;

        DebugEx.Log("DarkYangyuanPassive", $"大招参数修改: {string.Join(", ", ultimateModifier)}");
    }

    private void SpawnSubordinate(DarkYangyuanPhaseConfig.SubordinateSpawnData spawnConfig)
    {
        if (m_WolfdogManager == null) return;

        m_WolfdogManager.SpawnWolfdogAsync(spawnConfig).Forget();
    }

    #endregion
}
