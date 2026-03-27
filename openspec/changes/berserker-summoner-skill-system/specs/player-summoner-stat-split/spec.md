## ADDED Requirements

### Requirement: SummonerSkillTable 完整重新设计
`SummonerSkillTable` SHALL 移除旧字段 `EffectType`、`EffectValue`，对齐 `SummonChessSkillTable` 的攻击/特效字段体系，并新增召唤师专属字段：`SummonerClass`（int）、`UnlockTier`（int）、`BranchId`（int）、`Params`（float[]）。完整字段列表见 `AI工作区/配置表/SummonerSkillTable.txt`。

#### Scenario: DataTableGenerator 生成包含新字段的代码
- **WHEN** DataTableGenerator 运行后
- **THEN** `SummonerSkillTable.cs` 包含 `SummonerClass`、`UnlockTier`、`BranchId`、`Params`（float[]）、`DamageType`、`DamageCoeff`、`EffectHitType`、`ProjectilePrefabId`、`BuffIds`（int[]）、`SelfBuffIds`（int[]）等字段，且不包含已移除的 `EffectType`、`EffectValue`

#### Scenario: Params 数组可正确解析
- **WHEN** 配置行的 Params 列填入 `"0.5,0.4,0.15,2.0"`
- **THEN** `DataTableExtension.ParseArray<float>(m_Config.Params)` 返回长度为 4 的数组，各值正确

### Requirement: BuffTable 包含狂战士技能所需 Buff 条目
`BuffTable` SHALL 包含以下 Buff 条目供狂战士技能引用：战意激昂攻速提升 Buff（ID=3001，攻速 +20%，支持持续时间）、战意激昂伤害提升 Buff（ID=3002，伤害 +15%，支持持续时间）、狂怒之心伤害提升基础档 Buff（ID=3003，伤害 +15%）、狂怒之心伤害提升翻倍档 Buff（ID=3004，伤害 +30%）。

#### Scenario: 战意激昂 Buff 持续时间正确
- **WHEN** 技能释放时通过 `BuffHelper.ApplyBuff(3001, duration=10f, isGroupTarget=true)` 施加
- **THEN** 全体友方棋子的攻速提升 Buff 在 10s 后自动移除
