## ADDED Requirements

### Requirement: 狂怒之心——条件触发全体友方伤害提升
`BerserkerPassive` SHALL 在每帧 `Tick()` 中检测战场状态，条件满足时通过 `BuffHelper` 应用对应 Buff，条件不满足时移除 Buff，用 `m_IsAlliesActive` / `m_IsSelfActive` 标记避免重复操作：

- **条件 A**：场上任意友方棋子 HP < `Params[0]`（50%）× 其最大 HP → 全体友方获得 `BuffIds[0]`（伤害 +`Params[2]` = +15%）
- **条件 B**：召唤师自身 HP < `Params[1]`（40%）× 自身最大 HP → 全体友方伤害提升效果 × `Params[3]`（×2.0，即 +30%），召唤师自身额外获得 +15% 伤害提升

所有数值从 `SummonerSkillTable.Params` 读取，不硬编码。

#### Scenario: 无友方低血——无 Buff
- **WHEN** 所有友方棋子 HP ≥ 50% 最大 HP
- **THEN** 全体友方无 BerserkerRage Buff

#### Scenario: 有友方低血——全体获得基础伤害提升
- **WHEN** 至少一个友方棋子 HP < 50% 最大 HP，召唤师自身 HP ≥ 40%
- **THEN** 全体友方获得伤害提升 Buff（+15%），召唤师自身无额外 Buff

#### Scenario: 召唤师极低血量——全体效果翻倍
- **WHEN** 有友方低血 且 召唤师自身 HP < 40%
- **THEN** 全体友方伤害提升翻倍（+30%），召唤师自身额外获得 +15%

#### Scenario: 条件解除后 Buff 被移除
- **WHEN** 之前触发了 BerserkerRage Buff，之后所有友方 HP 恢复 ≥ 50%
- **THEN** 全体友方 BerserkerRage Buff 被移除，无残留

### Requirement: 战意激昂——消耗生命值强化全体召唤物
`BerserkerActiveSkill` SHALL 在 `CanCast()` 中额外检查召唤师当前 HP > `Params[0]`（20）；`ExecuteSkill()` 中扣减 HP（`Params[0]`），通过 `BuffHelper` 向全体友方棋子施加 `BuffIds[0]`（攻速 +`Params[1]` = +20%）和 `BuffIds[1]`（伤害 +`Params[2]` = +15%），持续时间由 `SummonerSkillTable.Duration`（10s）传给 Buff 系统。

所有数值从配置表读取，不硬编码。

#### Scenario: 条件满足时释放成功
- **WHEN** 冷却为 0 且 灵力足够 且 HP > 20
- **THEN** HP 减少 20，全体友方棋子获得攻速（+20%）和伤害（+15%）Buff 持续 10s，进入冷却

#### Scenario: HP 不足时释放失败
- **WHEN** 召唤师当前 HP ≤ 20
- **THEN** `CanCast()` 返回 false，HP 不变，无 Buff 施加

#### Scenario: 冷却期间无法再次释放
- **WHEN** 技能刚释放，冷却尚未结束
- **THEN** `TryCast()` 返回 false

#### Scenario: Buff 持续时间到期自动消失
- **WHEN** 战意激昂 Buff 施加后经过 10s
- **THEN** 全体友方棋子的攻速/伤害 Buff 自动移除

### Requirement: 所有数值完全由 SummonerSkillTable 驱动
两个技能的所有数值（阈值/系数/持续时间/冷却/消耗）SHALL 从配置表读取，代码中不出现任何魔法数字。

#### Scenario: 修改配置表数值后技能行为随之改变
- **WHEN** 将 `战意激昂` 的 `Params[0]` 从 20 改为 30，重新生成配置表
- **THEN** 下次运行 `CanCast()` 时 HP 消耗阈值变为 30，无需修改代码
