## ADDED Requirements

### Requirement: 狂怒之心被动——友方低血量伤害提升
`BerserkerPassive` SHALL 在每帧 `Tick()` 中检测场上全体友方棋子：若**任意**友方 HP < 50% 最大 HP，则为全体友方应用"低血量伤害提升 Buff"（+15%）；若召唤师自身 HP < 40%，则额外再次应用"自身超低血量伤害提升 Buff"（+15%），且全体召唤物的伤害提升效果翻倍（总计友方 +30%，自身额外 +15%）。

条件不满足时 SHALL 移除对应 Buff（用唯一 BuffKey 标记，避免重复叠加）。

#### Scenario: 友方无人低血——无 Buff
- **WHEN** 场上全体友方 HP 均 ≥ 50% 最大 HP
- **THEN** 狂怒之心 Buff 不存在于任何友方棋子身上

#### Scenario: 友方有人低血——全体获得 +15% 伤害
- **WHEN** 场上至少一个友方棋子 HP < 50% 最大 HP，且召唤师自身 HP ≥ 40%
- **THEN** 全体友方棋子获得 BuffKey=`BerserkerRage_Allies` 的伤害提升 Buff（+15%），召唤师自身无额外 Buff

#### Scenario: 召唤师极低血量——全体伤害翻倍叠加
- **WHEN** 场上有友方低血（HP < 50%），且召唤师自身 HP < 40%
- **THEN** 全体友方棋子伤害提升 Buff 效果翻倍（+30%），召唤师自身额外获得 +15% 伤害提升 Buff

#### Scenario: 条件恢复后 Buff 移除
- **WHEN** 之前触发了 BerserkerRage_Allies Buff，之后全体友方 HP 均恢复到 ≥ 50%
- **THEN** 全体友方棋子的 BerserkerRage_Allies Buff 被移除

### Requirement: 战意激昂主动——消耗 HP 强化全体召唤物
`BerserkerActiveSkill.TryCast()` SHALL 检查召唤师当前 HP > `SummonerSkillTable.HealthCost`（配置值 20），满足条件后扣除 HP，向场上全体友方棋子施加"战意激昂 Buff"（攻速 +20%，伤害 +15%），持续 10s（从 `SummonerSkillTable.Duration` 读取）。

#### Scenario: HP 充足时释放成功
- **WHEN** 召唤师当前 HP > 20，玩家触发战意激昂
- **THEN** 召唤师 HP 减少 20，场上全体友方棋子获得 BuffKey=`BerserkerActiveBuff` 的攻速（+20%）和伤害（+15%）Buff，持续 10s

#### Scenario: HP 不足时释放失败
- **WHEN** 召唤师当前 HP ≤ 20，玩家触发战意激昂
- **THEN** 技能不触发，HP 不变，无 Buff 施加

#### Scenario: 冷却期间无法再次释放
- **WHEN** 战意激昂刚释放成功，冷却尚未结束
- **THEN** `TryCast()` 返回 false，玩家输入被忽略

#### Scenario: Buff 持续时间结束后自动消失
- **WHEN** 战意激昂 Buff 施加后经过 10s
- **THEN** 全体友方棋子的 BerserkerActiveBuff 自动移除（由 Buff 系统处理持续时间）

### Requirement: 技能配置完全读自 SummonerSkillTable
`BerserkerPassive` 和 `BerserkerActiveSkill` 的所有数值（HP 阈值、Buff 效果值、持续时间、冷却时间、HP 消耗）SHALL 从 `SummonerSkillTable` 读取，不硬编码。

#### Scenario: 修改配置表后技能数值随之改变
- **WHEN** `SummonerSkillTable` 中战意激昂的 `HealthCost` 从 20 改为 15
- **THEN** 下次运行 `TryCast()` 时 HP 消耗变为 15，无需修改代码
