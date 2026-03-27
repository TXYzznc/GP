## 1. 配置表扩展

- [ ] 1.1 在 `SummonerSkillTable.xlsx` 中新增 `HealthCost`（float）列，填入狂战士两个技能的数据行（狂怒之心 HealthCost=0，战意激昂 HealthCost=20）
- [ ] 1.2 确认 `BuffTable.xlsx` 中是否已有"攻速提升"和"伤害提升"类型的通用 Buff；若无，新增以下 Buff 条目：`BerserkerRage_Allies`（伤害提升 15%/30%）、`BerserkerActiveDmg`（伤害提升 15%）、`BerserkerActiveAtk`（攻速提升 20%）
- [ ] 1.3 运行 DataTableGenerator，重新生成 `SummonerSkillTable.cs` 和 `.bytes`

## 2. 召唤师技能框架——接口与 Context

- [ ] 2.1 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/Interface/ISummonerSkill.cs`，定义主动技能接口（SkillId、Init、Tick、TryCast、CanCast、GetCooldownRemaining）
- [ ] 2.2 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/Interface/ISummonerPassive.cs`，定义被动技能接口（PassiveId、Init、Tick、Dispose）
- [ ] 2.3 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerSkillContext.cs`，包含字段：`Summoner`（召唤师实体/运行时数据）、`EntityTracker`（CombatEntityTracker）、`BuffHelper`（BuffApplyHelper）

## 3. 召唤师技能框架——基类

- [ ] 3.1 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerSkillBase.cs`（抽象类，实现 `ISummonerSkill`）：通用 Tick（冷却倒计时）、CanCast（冷却 + HP 检查）、TryCast（检查 → 扣 HP → 进入冷却 → 调用 `ExecuteSkill()`）、`abstract void ExecuteSkill()`
- [ ] 3.2 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerPassiveBase.cs`（抽象类，实现 `ISummonerPassive`）：通用 Init 存储 Context/Config，`abstract void OnTick(float dt)`，`abstract void OnDispose()`

## 4. 召唤师技能框架——Manager 与 Factory

- [ ] 4.1 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerSkillFactory.cs`：`RegisterAll()`、`Register(id, Func<ISummonerSkill>)`、`RegisterPassive(id, Func<ISummonerPassive>)`、`Create(id)`、`CreatePassive(id)`，模式与 `SkillFactory` 一致
- [ ] 4.2 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerSkillManager.cs`（MonoBehaviour）：维护 `List<ISummonerSkill> Skills` 和 `List<ISummonerPassive> Passives`，`SetActive(bool)`，`Update()` 中 Tick 所有技能并检测输入槽位（Q=槽1，E=槽2，R=槽3），`UpdateSkillsFromData(IReadOnlyList<int>)` 从数据构建技能列表
- [ ] 4.3 在 `SummonerSkillFactory.RegisterAll()` 中注册狂战士两个技能（被动 ID 和主动 ID 对应 SummonerSkillTable 中的行）

## 5. 狂战士技能实现

- [ ] 5.1 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/Skills/BerserkerPassive.cs`（继承 `SummonerPassiveBase`）：
  - `OnTick()` 中轮询 `EntityTracker.GetAllies()` 检测是否有友方 HP < 50%
  - 条件满足时通过 `BuffHelper.ApplyBuff(BerserkerRage_Allies_BuffId, target, isGroupTarget=true)` 应用全体 Buff
  - 召唤师 HP < 40% 时应用翻倍效果（应用额外 Buff 或修改 Buff 数值）
  - 条件不满足时通过 `BuffHelper.RemoveBuff(BerserkerRage_Allies_BuffId)` 移除全体 Buff
  - 使用 `m_IsAlliesDebuffActive` 和 `m_IsSelfDebuffActive` 标记避免重复应用
- [ ] 5.2 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/Skills/BerserkerActiveSkill.cs`（继承 `SummonerSkillBase`）：
  - 重写 `CanCast()` 检查 HP > HealthCost（委托基类冷却检查）
  - 实现 `ExecuteSkill()`：调用 `BuffHelper.ApplyBuff(BerserkerActiveDmg_BuffId, isGroupTarget=true)` 和 `BuffHelper.ApplyBuff(BerserkerActiveAtk_BuffId, isGroupTarget=true)`
  - Buff 持续时间由 `SummonerSkillTable.Duration` 控制（传给 Buff 系统）

## 6. 集成与注册

- [ ] 6.1 在召唤师实体初始化流程中（GameProcedure 或召唤师 Entity `OnShow()`）创建 `SummonerSkillContext`，赋值 Summoner/EntityTracker/BuffHelper
- [ ] 6.2 调用 `SummonerSkillFactory.RegisterAll()` 注册所有技能（与 `SkillFactory.RegisterAll()` 调用时机一致）
- [ ] 6.3 调用 `SummonerSkillManager.UpdateSkillsFromData()` 传入召唤师当前已解锁的技能 ID 列表，完成技能初始化
- [ ] 6.4 战斗开始时调用 `SummonerSkillManager.SetActive(true)`，战斗结束/脱战时调用 `SetActive(false)`

## 7. 验证

- [ ] 7.1 战意激昂：HP 充足时按键，确认扣血 + 全体棋子获得攻速/伤害 Buff + 冷却正确
- [ ] 7.2 战意激昂：HP 不足时按键，确认无任何效果
- [ ] 7.3 狂怒之心：让某个友方棋子受伤低于 50% HP，确认全体获得 +15% 伤害 Buff
- [ ] 7.4 狂怒之心：召唤师自身低于 40% HP，确认全体 Buff 翻倍
- [ ] 7.5 狂怒之心：友方 HP 恢复后，确认 Buff 被正确移除，无残留
- [ ] 7.6 战斗外（SetActive=false）：确认按键无任何反应
