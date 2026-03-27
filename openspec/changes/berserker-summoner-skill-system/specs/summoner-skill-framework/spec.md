## ADDED Requirements

### Requirement: ISummonerSkill 主动技能接口
系统 SHALL 定义 `ISummonerSkill` 接口，包含：`SkillId`、`Init(SummonerSkillContext, SummonerSkillTable)`、`Tick(float dt)`、`TryCast()`、`CanCast()`、`GetCooldownRemaining()`，与 `IChessSkill` 模式对齐但使用召唤师专属 Context。

#### Scenario: 接口方法完整
- **WHEN** 实现类继承 `ISummonerSkill`
- **THEN** 编译器要求实现 SkillId、Init、Tick、TryCast、CanCast、GetCooldownRemaining 全部方法

### Requirement: ISummonerPassive 被动技能接口
系统 SHALL 定义 `ISummonerPassive` 接口，包含：`PassiveId`、`Init(SummonerSkillContext, SummonerSkillTable)`、`Tick(float dt)`、`Dispose()`，与 `IChessPassive` 模式对齐。

#### Scenario: 被动技能生命周期
- **WHEN** 战斗开始时调用 `Init()`，战斗结束时调用 `Dispose()`
- **THEN** 被动技能正确注册/注销状态，不发生内存泄漏或 Buff 残留

### Requirement: SummonerSkillContext 包含必要引用
`SummonerSkillContext` SHALL 包含：`Summoner`（召唤师实体，提供 HP/灵力访问）、`EntityTracker`（`CombatEntityTracker`，获取全体友方/敌方棋子）、`BuffHelper`（`BuffApplyHelper`，统一 Buff 应用入口）。

#### Scenario: Context 字段非空时技能可正常执行
- **WHEN** `SummonerSkillContext` 所有字段均已赋值
- **THEN** 技能的 `TryCast()` 和 `Tick()` 能正常访问战场数据，无 NullReferenceException

### Requirement: SummonerSkillBase 提供通用基类实现
抽象类 `SummonerSkillBase` SHALL 实现 `ISummonerSkill` 中的冷却计时（`Tick`）、`CanCast`（冷却 + HP 检查）、`TryCast`（检查 → 扣血 → 进入冷却），子类只需实现 `ExecuteSkill()`。

#### Scenario: 冷却未完成时 TryCast 返回 false
- **WHEN** 技能冷却剩余时间 > 0
- **THEN** `TryCast()` 返回 false，不扣除资源

#### Scenario: HP 不足时 TryCast 返回 false
- **WHEN** 召唤师当前 HP ≤ `SummonerSkillTable.HealthCost`
- **THEN** `TryCast()` 返回 false

#### Scenario: 条件满足时 TryCast 扣血并进入冷却
- **WHEN** 冷却为 0 且 HP > HealthCost
- **THEN** `TryCast()` 扣除 HealthCost 生命值，设置冷却，调用 `ExecuteSkill()`，返回 true

### Requirement: SummonerSkillManager 管理技能 Tick 与输入
`SummonerSkillManager`（MonoBehaviour）SHALL 维护主动技能列表和被动技能列表，在 `Update()` 中 Tick 所有技能，检测输入键位（Q/E/R 等槽位键）并调用对应 `ISummonerSkill.TryCast()`。战斗外通过 `SetActive(false)` 停止 Tick。

#### Scenario: 战斗外不执行 Tick
- **WHEN** `SummonerSkillManager.SetActive(false)` 被调用
- **THEN** `Update()` 中跳过所有技能 Tick 和输入检测

#### Scenario: 输入触发对应槽位技能
- **WHEN** 玩家按下召唤师技能槽位 1 键（Q）且 Manager 处于 Active 状态
- **THEN** `Skills[0].TryCast()` 被调用

### Requirement: SummonerSkillFactory 注册并创建技能实例
`SummonerSkillFactory` SHALL 通过 `Register(int skillId, Func<ISummonerSkill>)` 和 `RegisterPassive(int skillId, Func<ISummonerPassive>)` 注册，通过 `Create(int skillId)` / `CreatePassive(int skillId)` 创建实例，模式与 `SkillFactory` 完全一致。

#### Scenario: 已注册技能可创建
- **WHEN** `SummonerSkillFactory.Create(skillId)` 被调用且 skillId 已注册
- **THEN** 返回对应技能的新实例（非 null）

#### Scenario: 未注册技能返回 null
- **WHEN** `SummonerSkillFactory.Create(skillId)` 被调用且 skillId 未注册
- **THEN** 返回 null 并输出 Error 日志
