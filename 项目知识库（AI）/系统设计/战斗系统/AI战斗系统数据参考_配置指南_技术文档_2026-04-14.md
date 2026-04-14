> **最后更新**: 2026-03-23
> **状态**: 有效
---

# 配置表数据参考

## 📋 目录

- [BuffTable 数据](#bufftable-数据)
- [EscapeRuleTable 数据](#escaperuletable-数据)
- [成功率计算示例](#成功率计算示例)
- [Buff筛选逻辑参考](#buff筛选逻辑参考)
- [数据验证清单](#数据验证清单)
- [文件位置](#文件位置)

---


### 原有Buff (保留)

| ID | 名称 | Type | Owner | 描述 |
|----|-----|------|-------|------|
| 1 | 灼烧 | Debuff | 通用(2) | 每秒伤害 |
| 2 | 寒霜 | Debuff | 通用(2) | 降低移速和攻速 |
| 3 | 融化 | Debuff | 通用(2) | 真实伤害 |
| 4 | 神力增益 | Buff | 通用(2) | 攻击力+25% |
| 5 | 日落长弓 | Buff | 通用(2) | 白天伤害+15% |
| 6 | 九天玄冰 | Buff | 通用(2) | 降低移速和攻速 |

### 新增先手Buff (玩家用)

| ID | 名称 | 描述 | InitBuff | SnkAtk | Owner |
|----|-----|------|----------|--------|-------|
| 2001 | 先手·速度提升 | 移动速度提升20% | ✓ | ✗ | 玩家(0) |
| 2002 | 先手·攻击提升 | 攻击力提升15% | ✓ | ✗ | 玩家(0) |
| 2003 | 先手·首回合免伤 | 首回合受伤减少50% | ✓ | ✗ | 玩家(0) |

**配置详情**:
```
ID=2001
Name: 先手·速度提升
SpriteId: 5001
Desc: 移动速度提升20%
BuffType: 1 (Buff)
EffectType: 1 (属性修改)
EffectValue: 0.2
Duration: 0 (战斗持续)
Interval: 0
MaxStack: 1
MutexGroup: 0
MutexType: 0
EffectId: 5001
IsInitiativeBuff: true
IsSneakDebuff: false
BuffOwner: 0

ID=2002
Name: 先手·攻击提升
... (类似，SpriteId=5002, EffectValue=0.15)

ID=2003
Name: 先手·首回合免伤
... (类似，SpriteId=5003, EffectValue=0.5)
```

### 新增偷袭Debuff (敌人用)

| ID | 名称 | 描述 | InitBuff | SnkAtk | Owner | Duration |
|----|-----|------|----------|--------|-------|----------|
| 3001 | 偷袭·防御降低 | 防御力降低30% | ✗ | ✓ | 敌人(1) | 2回合 |
| 3002 | 偷袭·眩晕 | 目标眩晕 | ✗ | ✓ | 敌人(1) | 1回合 |
| 3003 | 偷袭·持续流血 | 流血伤害 | ✗ | ✓ | 敌人(1) | 3回合 |

**配置详情**:
```
ID=3001
Name: 偷袭·防御降低
SpriteId: 6001
Desc: 防御力降低30%
BuffType: 2 (Debuff)
EffectType: 1 (属性修改)
EffectValue: -0.3
Duration: 2
Interval: 0
MaxStack: 1
EffectId: 6001
IsInitiativeBuff: false
IsSneakDebuff: true
BuffOwner: 1

ID=3002
Name: 偷袭·眩晕
SpriteId: 6002
Desc: 目标眩晕，跳过下一回合
BuffType: 2 (Debuff)
EffectType: 4 (状态改变)
EffectValue: 1
Duration: 1
Interval: 0
EffectId: 6002
... (其他同上)

ID=3003
Name: 偷袭·持续流血
SpriteId: 6003
Desc: 每回合流失10%生命值，持续3回合
BuffType: 2 (Debuff)
EffectType: 2 (周期性)
EffectValue: 0.1
Duration: 3
Interval: 1
EffectId: 6003
... (其他同上)
```

---

## EscapeRuleTable 数据

### 脱战规则

| 规则ID | 敌人类型 | 基础成功率 | 每回合增长 | 最大成功率 | 成功代价 | 失败惩罚生命 | 冷却回合 |
|--------|---------|-----------|-----------|-----------|---------|------------|---------|
| 1 | 普通(0) | 60% | +5% | 90% | +10污染 | -20% | 2 |
| 2 | 精英(1) | 40% | +3% | 80% | +15污染 | -30% | 3 |
| 3 | Boss(2) | 20% | +2% | 60% | +25污染 | -40% | 5 |

**规则1 - 普通敌人 (EnemyType=0)**:
```
Id: 1
EnemyType: 0
BaseSuccessRate: 0.6
TimeBonus: 0.05
MaxSuccessRate: 0.9
CorruptionCost: 10
HealthLossPenalty: 0.2
CooldownTurns: 2
```

**规则2 - 精英敌人 (EnemyType=1)**:
```
Id: 2
EnemyType: 1
BaseSuccessRate: 0.4
TimeBonus: 0.03
MaxSuccessRate: 0.8
CorruptionCost: 15
HealthLossPenalty: 0.3
CooldownTurns: 3
```

**规则3 - Boss敌人 (EnemyType=2)**:
```
Id: 3
EnemyType: 2
BaseSuccessRate: 0.2
TimeBonus: 0.02
MaxSuccessRate: 0.6
CorruptionCost: 25
HealthLossPenalty: 0.4
CooldownTurns: 5
```

---

[↑ 返回目录](#目录)

---

## 成功率计算示例

### 普通敌人

**回合1**:
```
rate = 0.6 + (1 × 0.05) = 0.65 (65%)
```

**回合2**:
```
rate = 0.6 + (2 × 0.05) = 0.70 (70%)
```

**回合5**:
```
rate = 0.6 + (5 × 0.05) = 0.85 (85%)
```

**回合10**:
```
rate = 0.6 + (10 × 0.05) = 1.1 → Min(1.1, 0.9) = 0.9 (90%)
```

**失败后冷却中回合1**:
```
rate = (0.6 + 0.05) × 0.5 = 0.325 (32.5%)
```

### Boss敌人

**回合1**:
```
rate = 0.2 + (1 × 0.02) = 0.22 (22%)
```

**回合10**:
```
rate = 0.2 + (10 × 0.02) = 0.4 (40%)
```

**回合20**:
```
rate = 0.2 + (20 × 0.02) = 0.6 (60%)
```

**回合25以上**:
```
rate = Min(0.2 + 0.02*N, 0.6) = 0.6 (60%)
```

---

[↑ 返回目录](#目录)

---

## Buff筛选逻辑参考

### 获取偷袭Debuff

```csharp
private List<int> GetSneakDebuffPool()
{
    var buffTable = GF.DataTable.GetDataTable<BuffTable>();
    var debuffIds = new List<int>();

    foreach (var row in buffTable.GetAllDataRows())
    {
        var buff = row as BuffTable;
        if (buff?.IsSneakDebuff == true)
        {
            debuffIds.Add(buff.Id);
        }
    }

    // 预期返回: [3001, 3002, 3003]
    return debuffIds;
}
```

### 获取随机先手Buff

```csharp
private int GetRandomInitiativeBuff()
{
    var buffTable = GF.DataTable.GetDataTable<BuffTable>();
    var initiativeBuffs = new List<int>();

    foreach (var row in buffTable.GetAllDataRows())
    {
        var buff = row as BuffTable;
        if (buff?.IsInitiativeBuff == true)
        {
            initiativeBuffs.Add(buff.Id);
        }
    }

    // 预期返回: 2001 或 2002 或 2003 之一
    return initiativeBuffs.Count > 0
        ? initiativeBuffs[Random.Range(0, initiativeBuffs.Count)]
        : 0;
}
```

### 加载脱战规则

```csharp
private void LoadEscapeRule()
{
    var escapeRuleTable = GF.DataTable.GetDataTable<EscapeRuleTable>();

    // 根据敌人类型加载规则
    int ruleId = m_CurrentEnemy?.EnemyType switch
    {
        0 => 1,  // 普通敌人
        1 => 2,  // 精英敌人
        2 => 3,  // Boss敌人
        _ => 1   // 默认普通
    };

    m_CurrentRule = escapeRuleTable.GetDataRow(ruleId) as EscapeRuleTable;
}
```

---

[↑ 返回目录](#目录)

---

## 数据验证清单

### BuffTable验证

- [x] 原有6个Buff保留
- [x] 新增3列: IsInitiativeBuff, IsSneakDebuff, BuffOwner
- [x] 原有Buff: IsInitiativeBuff=false, IsSneakDebuff=false, BuffOwner=2
- [x] 先手Buff (2001-2003): IsInitiativeBuff=true, BuffOwner=0
- [x] 偷袭Debuff (3001-3003): IsSneakDebuff=true, BuffOwner=1
- [x] 总计12个Buff

### EscapeRuleTable验证

- [x] 规则1 (普通): BaseSuccessRate=0.6, MaxSuccessRate=0.9
- [x] 规则2 (精英): BaseSuccessRate=0.4, MaxSuccessRate=0.8
- [x] 规则3 (Boss): BaseSuccessRate=0.2, MaxSuccessRate=0.6
- [x] 总计3条规则

---

[↑ 返回目录](#目录)

---

## 文件位置

**配置表文件**:
- `AAAGameData/DataTables/BuffTable.xlsx`
- `Assets/AAAGame/DataTable/BuffTable.txt`
- `AAAGameData/DataTables/EscapeRuleTable.xlsx`
- `Assets/AAAGame/DataTable/EscapeRuleTable.txt`

**代码生成后文件**:
- `Assets/AAAGame/Scripts/DataTable/BuffTable.cs`
- `Assets/AAAGame/Scripts/DataTable/EscapeRuleTable.cs`

---

**版本历史**:
- v1.0 (2026-03-19): 初始版本，包含完整数据参考

[↑ 返回目录](#目录)
