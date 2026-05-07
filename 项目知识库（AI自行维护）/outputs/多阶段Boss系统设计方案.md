# 多阶段 Boss 系统设计方案

> **设计日期**：2026-05-07  
> **状态**：设计阶段  

---

## 1. 核心设计思路

**目标**：支持 Boss 在 HP 降低时，根据阶段自动切换：
- Buff 组（属性加成、特殊状态）
- 技能配置（普攻、技能一、大招等）
- 从属单位召唤/清除
- 大招冷却 & 灵力消耗修改

**实现层**：ChessEntity 或新增 BossPhaseComponent

---

## 2. 配置表设计

### 2.1 SummonChessSkillTable 字段扩展

现有字段保持不变，在 **CustomData** 中存储阶段信息：

```
CustomData = {
  "Phases": [
    {
      "PhaseNum": 1,
      "HealthPercentThreshold": 60,
      "BuffIds": [],
      "SkillOverride": {},
      "UltimateModifier": {},
      "SubordinateSpawn": null
    },
    {
      "PhaseNum": 2,
      "HealthPercentThreshold": 30,
      "BuffIds": [10],
      "SkillOverride": {},
      "UltimateModifier": {},
      "SubordinateSpawn": {
        "ChessId": 12,
        "Quantity": 1,
        "InheritRatio": 0.8
      }
    },
    {
      "PhaseNum": 3,
      "HealthPercentThreshold": 0,
      "BuffIds": [11, 12],
      "SkillOverride": {
        "NormalAttackSkillId": 51,
        "Skill1Id": 52,
        "Skill2Id": 53
      },
      "UltimateModifier": {
        "CooldownMultiplier": 0.5,
        "ManaMultiplier": 0.5
      },
      "SubordinateSpawn": null
    }
  ]
}
```

**字段说明**：
- `PhaseNum`：阶段号（1、2、3）
- `HealthPercentThreshold`：触发此阶段的 HP 百分比阈值（上界，HP ≤ 此值时进入）
- `BuffIds`：此阶段触发的 Buff ID 数组
- `SkillOverride`：此阶段要替换的技能（仅在与之前阶段不同时填）
- `UltimateModifier`：大招修改器（冷却倍数、灵力倍数）
- `SubordinateSpawn`：此阶段触发的从属单位生成配置

---

## 3. 代码架构设计

### 3.1 新增 BossPhaseComponent（或集成到 ChessEntity）

**职责**：
- 监听 Boss HP 变化
- 检测阶段切换条件
- 触发阶段转换逻辑

```csharp
public class BossPhaseComponent : IDisposable
{
    private ChessEntity m_Chess;
    private BossPhaseConfig m_Config;
    private int m_CurrentPhase = 1;
    private List<int> m_AppliedBuffIds = new List<int>();
    private ChessEntity m_Subordinate;

    public event Action<int, int> OnPhaseChanged; // (oldPhase, newPhase)

    public void Init(ChessEntity chess, BossPhaseConfig config)
    {
        m_Chess = chess;
        m_Config = config;
        m_CurrentPhase = 1;
        
        // 订阅 HP 变化事件
        m_Chess.OnHealthChanged += CheckPhaseTransition;
    }

    private void CheckPhaseTransition(double currentHp, double maxHp)
    {
        double hpPercent = currentHp / maxHp;
        int targetPhase = GetPhaseByHpPercent(hpPercent);

        if (targetPhase != m_CurrentPhase)
        {
            TransitionToPhase(targetPhase);
        }
    }

    private int GetPhaseByHpPercent(double hpPercent)
    {
        // 从高到低遍历阶段配置
        for (int i = m_Config.Phases.Count - 1; i >= 0; i--)
        {
            if (hpPercent * 100 <= m_Config.Phases[i].HealthPercentThreshold)
                return m_Config.Phases[i].PhaseNum;
        }
        return 1;
    }

    private void TransitionToPhase(int newPhase)
    {
        // 1. 移除旧阶段 Buff
        RemoveOldPhaseBuffs();

        // 2. 应用新阶段 Buff
        var phaseConfig = m_Config.GetPhase(newPhase);
        ApplyPhaseBuffs(phaseConfig);

        // 3. 替换技能（如有）
        if (phaseConfig.SkillOverride != null && phaseConfig.SkillOverride.Count > 0)
        {
            UpdateChessSkills(phaseConfig.SkillOverride);
        }

        // 4. 修改大招参数（如有）
        if (phaseConfig.UltimateModifier != null)
        {
            ModifyUltimateSkill(phaseConfig.UltimateModifier);
        }

        // 5. 处理从属单位
        HandleSubordinateSpawn(phaseConfig);

        m_CurrentPhase = newPhase;
        OnPhaseChanged?.Invoke(m_CurrentPhase, newPhase);

        DebugEx.Log("BossPhaseComponent", $"Boss 进入阶段 {newPhase}");
    }

    private void RemoveOldPhaseBuffs()
    {
        foreach (var buffId in m_AppliedBuffIds)
        {
            m_Chess.BuffManager.RemoveBuff(buffId);
        }
        m_AppliedBuffIds.Clear();
    }

    private void ApplyPhaseBuffs(PhaseConfig config)
    {
        foreach (var buffId in config.BuffIds)
        {
            m_Chess.BuffManager.AddBuff(buffId, m_Chess.gameObject, m_Chess.Attribute);
            m_AppliedBuffIds.Add(buffId);
        }
    }

    private void UpdateChessSkills(Dictionary<string, int> skillOverride)
    {
        // 根据 skillOverride 更新 ChessEntity 的技能引用
        if (skillOverride.ContainsKey("NormalAttackSkillId"))
        {
            // 获取新的技能实例
            var newSkill = GetOrCreateSkill(skillOverride["NormalAttackSkillId"]);
            m_Chess.ReplaceSkill(SkillType.NormalAttack, newSkill);
        }

        if (skillOverride.ContainsKey("Skill1Id"))
        {
            var newSkill = GetOrCreateSkill(skillOverride["Skill1Id"]);
            m_Chess.ReplaceSkill(SkillType.Skill1, newSkill);
        }

        if (skillOverride.ContainsKey("Skill2Id"))
        {
            var newSkill = GetOrCreateSkill(skillOverride["Skill2Id"]);
            m_Chess.ReplaceSkill(SkillType.Skill2, newSkill);
        }
    }

    private void ModifyUltimateSkill(Dictionary<string, float> modifier)
    {
        // 获取大招技能
        var ultimateSkill = m_Chess.GetSkill(SkillType.Ultimate);
        
        if (modifier.ContainsKey("CooldownMultiplier"))
        {
            ultimateSkill.CooldownMultiplier = modifier["CooldownMultiplier"];
        }

        if (modifier.ContainsKey("ManaMultiplier"))
        {
            ultimateSkill.ManaMultiplier = modifier["ManaMultiplier"];
        }
    }

    private void HandleSubordinateSpawn(PhaseConfig config)
    {
        // 清除旧的从属单位
        if (m_Subordinate != null)
        {
            GF.Entity.HideEntity(m_Subordinate);
            m_Subordinate = null;
        }

        // 生成新的从属单位
        if (config.SubordinateSpawn != null)
        {
            m_Subordinate = SpawnSubordinate(config.SubordinateSpawn);
        }
    }

    private ChessEntity SpawnSubordinate(SubordinateSpawnConfig spawnConfig)
    {
        // 实现从属单位的生成逻辑
        // 1. 获取从属单位的棋子配置
        // 2. 设置继承属性比例
        // 3. 返回生成的单位
        // ...
        return null;
    }

    public void Dispose()
    {
        m_Chess.OnHealthChanged -= CheckPhaseTransition;
    }
}
```

### 3.2 ChessEntity 集成

在 ChessEntity 中添加方法支持技能替换：

```csharp
public class ChessEntity : Entity
{
    private BossPhaseComponent m_PhaseComponent;

    public void InitBossPhase(BossPhaseConfig phaseConfig)
    {
        m_PhaseComponent = new BossPhaseComponent();
        m_PhaseComponent.Init(this, phaseConfig);
    }

    public void ReplaceSkill(SkillType skillType, IChessSkill newSkill)
    {
        // 根据技能类型替换技能
        switch (skillType)
        {
            case SkillType.NormalAttack:
                m_NormalAttack = newSkill;
                break;
            case SkillType.Skill1:
                m_Skill1 = newSkill;
                break;
            case SkillType.Skill2:
                m_Skill2 = newSkill;
                break;
            case SkillType.Ultimate:
                m_Ultimate = newSkill;
                break;
        }
    }

    public IChessSkill GetSkill(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.NormalAttack => m_NormalAttack,
            SkillType.Skill1 => m_Skill1,
            SkillType.Skill2 => m_Skill2,
            SkillType.Ultimate => m_Ultimate,
            _ => null
        };
    }
}

public enum SkillType
{
    NormalAttack,
    Skill1,
    Skill2,
    Ultimate
}
```

### 3.3 数据结构定义

```csharp
public class BossPhaseConfig
{
    public List<PhaseConfig> Phases { get; set; }

    public PhaseConfig GetPhase(int phaseNum)
    {
        return Phases.FirstOrDefault(p => p.PhaseNum == phaseNum);
    }
}

public class PhaseConfig
{
    public int PhaseNum { get; set; }
    public double HealthPercentThreshold { get; set; }
    public List<int> BuffIds { get; set; }
    public Dictionary<string, int> SkillOverride { get; set; }
    public Dictionary<string, float> UltimateModifier { get; set; }
    public SubordinateSpawnConfig SubordinateSpawn { get; set; }
}

public class SubordinateSpawnConfig
{
    public int ChessId { get; set; }
    public int Quantity { get; set; }
    public double InheritRatio { get; set; }
}
```

---

## 4. 黑暗杨戬 具体配置

### 4.1 配置表中的 CustomData

```json
{
  "Phases": [
    {
      "PhaseNum": 1,
      "HealthPercentThreshold": 60,
      "BuffIds": [],
      "SkillOverride": null,
      "UltimateModifier": null,
      "SubordinateSpawn": null
    },
    {
      "PhaseNum": 2,
      "HealthPercentThreshold": 30,
      "BuffIds": [阶段二强化BuffId],
      "SkillOverride": null,
      "UltimateModifier": null,
      "SubordinateSpawn": {
        "ChessId": 黑暗哮天犬ID,
        "Quantity": 1,
        "InheritRatio": 0.8
      }
    },
    {
      "PhaseNum": 3,
      "HealthPercentThreshold": 0,
      "BuffIds": [死战Buff的ID, 吸血Buff的ID],
      "SkillOverride": {
        "NormalAttackSkillId": 天威圣戟·黑暗ID,
        "Skill1Id": 天威圣戟·黑暗ID,
        "Skill2Id": 三眼天火ID
      },
      "UltimateModifier": {
        "CooldownMultiplier": 0.5,
        "ManaMultiplier": 0.5
      },
      "SubordinateSpawn": null
    }
  ]
}
```

### 4.2 技能配置

需要的技能 ID（假设分配）：

| 技能 | 阶段一 | 阶段二 | 阶段三 |
|------|--------|--------|--------|
| 普攻 | 黑暗戟横扫(50) | 黑暗戟横扫(50) | 天威圣戟·黑暗(51) |
| 技能一 | 天威圣戟·黑暗(51) | 天威圣戟·黑暗(51) | 天威圣戟·黑暗(51) |
| 技能二 | - | 三眼天火(52) | 三眼天火(52) |
| 大招 | 堕落劈山(53) | 堕落劈山(53) | 堕落劈山(53)·冷却减半 |

---

## 5. 时序流程图

```
HP 100% --> 阶段1（基础形态）
           [无特殊Buff]
           
HP 60% --> 阶段2（召唤形态）触发
           1. 移除旧Buff（无）
           2. 应用 阶段二强化Buff（攻击+30%, 移速+20%）
           3. 技能无变化
           4. 召唤 黑暗哮天犬
           
HP 30% --> 阶段3（死战模式）触发
           1. 移除 阶段二强化Buff
           2. 应用 死战Buff + 吸血Buff
           3. 替换技能：普攻/技能一改为 天威圣戟·黑暗
           4. 大招冷却 & 灵力消耗变为 50%
           5. 移除 黑暗哮天犬
           
HP 0% --> Boss 死亡
```

---

## 6. 注意事项

1. **HP 监听频率**：避免每帧都检测阶段，可以在 HP 改变时触发或定期检测
2. **技能初始化**：阶段二新增的三眼天火需要提前初始化好，切换时直接替换引用
3. **Buff 清理**：切换阶段时务必清理旧的阶段 Buff，否则会叠加
4. **从属单位生成**：需要考虑位置、朝向、初始距离等
5. **控制免疫**：死战Buff 中的"免疫控制"需要在 BuffManager 中特殊处理

---

## 7. 后续工作

- [ ] 实现 BossPhaseComponent
- [ ] 扩展 ChessEntity 的技能替换方法
- [ ] 实现 CustomData 的 JSON 解析
- [ ] 测试阶段切换逻辑
- [ ] 优化从属单位生成效率

