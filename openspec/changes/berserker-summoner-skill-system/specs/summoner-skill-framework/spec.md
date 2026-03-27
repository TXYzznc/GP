## ADDED Requirements

### Requirement: ISummonerSkill 主动技能接口
系统 SHALL 定义 `ISummonerSkill` 接口，包含：`SkillId`（int）、`Init(SummonerSkillContext, SummonerSkillTable)`、`Tick(float dt)`、`TryCast()`（bool）、`CanCast()`（bool）、`GetCooldownRemaining()`（float）。

#### Scenario: TryCast 调用链完整
- **WHEN** `TryCast()` 被调用且 `CanCast()` 返回 true
- **THEN** 消耗资源、进入冷却、执行技能效果，返回 true

#### Scenario: CanCast 返回 false 时 TryCast 无副作用
- **WHEN** `TryCast()` 被调用且 `CanCast()` 返回 false
- **THEN** 无资源消耗、无冷却变化，返回 false

### Requirement: ISummonerPassive 被动技能接口
系统 SHALL 定义 `ISummonerPassive` 接口，包含：`PassiveId`（int）、`Init(SummonerSkillContext, SummonerSkillTable)`、`Tick(float dt)`、`Dispose()`。

#### Scenario: Dispose 清理所有状态
- **WHEN** 战斗结束调用 `Dispose()`
- **THEN** 被动技能取消所有已施加的 Buff，释放事件引用，无内存泄漏

### Requirement: SummonerSkillContext 包含必要引用
`SummonerSkillContext` SHALL 包含 `RuntimeData`（`PlayerRuntimeDataManager`，提供 HP/灵力访问与修改）、`EntityTracker`（`CombatEntityTracker`，获取全体友方/敌方棋子）、`BuffHelper`（`BuffApplyHelper`，统一 Buff 应用入口）。

#### Scenario: 技能通过 Context 扣减生命值
- **WHEN** 技能调用 `ctx.RuntimeData` 扣减生命值
- **THEN** 玩家生命值正确减少，不低于 0

### Requirement: SummonerSkillBase 提供主动技能通用基类
抽象类 `SummonerSkillBase`（实现 `ISummonerSkill`）SHALL 实现：`Tick` 中冷却倒计时、`CanCast` 检查冷却与灵力、`TryCast` 执行检查→消耗资源→进入冷却→调用 `abstract ExecuteSkill()`。子类只需实现 `ExecuteSkill()`。

#### Scenario: 冷却期间 CanCast 返回 false
- **WHEN** 技能冷却剩余 > 0
- **THEN** `CanCast()` 返回 false

#### Scenario: 灵力不足时 CanCast 返回 false
- **WHEN** `RuntimeData.CurrentSpirit < m_Config.SpiritCost`
- **THEN** `CanCast()` 返回 false

### Requirement: SummonerPassiveBase 提供被动技能通用基类
抽象类 `SummonerPassiveBase`（实现 `ISummonerPassive`）SHALL 实现：`Init` 存储 Context/Config，`Tick` 调用 `abstract OnTick(float dt)`，`Dispose` 调用 `abstract OnDispose()`。内置 `bool m_IsActive` 状态标记，仅在状态变化时调用 `OnActivate()` / `OnDeactivate()`，避免每帧重复操作。

#### Scenario: 状态未变化时不重复触发
- **WHEN** 被动条件连续满足（已 Active），每帧 Tick
- **THEN** `OnActivate()` 不被重复调用，只在首次满足时调用一次

### Requirement: SummonerSkillManager 管理技能生命周期与输入
`SummonerSkillManager`（MonoBehaviour）SHALL 维护 `List<ISummonerSkill>` 和 `List<ISummonerPassive>`，`Update()` 中 Tick 所有技能，检测 Q/E/R 键触发对应槽位主动技能。`SetActive(false)` 后停止所有 Tick 与输入检测。`UpdateSkillsFromData(IReadOnlyList<int>)` 根据已解锁技能 ID 列表构建技能实例。

#### Scenario: SetActive(false) 后输入无响应
- **WHEN** `SetActive(false)` 后玩家按 Q 键
- **THEN** 对应主动技能的 `TryCast()` 不被调用

#### Scenario: 战斗结束时被动 Dispose 被调用
- **WHEN** `SummonerSkillManager.SetActive(false)` 被调用
- **THEN** 所有 `ISummonerPassive.Dispose()` 被依次调用

### Requirement: SummonerSkillFactory 注册并创建技能实例
`SummonerSkillFactory` SHALL 通过字典注册 `Register(int id, Func<ISummonerSkill>)` 和 `RegisterPassive(int id, Func<ISummonerPassive>)`，`Create(id)` / `CreatePassive(id)` 返回实例，未注册时返回 null 并输出 Error 日志，模式与 `SkillFactory` 完全一致。

#### Scenario: 已注册技能可正常创建
- **WHEN** `SummonerSkillFactory.Create(skillId)` 且 skillId 已注册
- **THEN** 返回新实例（非 null）

#### Scenario: 未注册技能返回 null
- **WHEN** `SummonerSkillFactory.Create(skillId)` 且 skillId 未注册
- **THEN** 返回 null，输出 Error 日志
