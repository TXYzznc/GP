## ADDED Requirements

### Requirement: SummonerSkillTable 包含 HealthCost 字段
`SummonerSkillTable` SHALL 包含 `HealthCost`（float）字段，表示主动技能消耗的召唤师生命值（被动技能该字段值为 0）。

#### Scenario: 读取 HealthCost 字段
- **WHEN** DataTableGenerator 重新生成 `SummonerSkillTable.cs` 后
- **THEN** `SummonerSkillTable` 类包含 `public float HealthCost` 属性，可在技能中读取

#### Scenario: 被动技能 HealthCost 为 0
- **WHEN** 狂怒之心（被动）行数的 HealthCost 字段值为 0
- **THEN** `SummonerPassiveBase.CanCast()` 不检查 HP 消耗（被动无需释放条件检查）
