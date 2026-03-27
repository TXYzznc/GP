## 1. 配置表准备

- [x] 1.1 将 `AI工作区/配置表/SummonerSkillTable.txt` 转换为 XLSX，保存到 `AAAGameData/DataTables/SummonerSkillTable.xlsx`（替换旧文件）已完成
- [x] 1.2 在 `BuffTable.txt` 中新增以下 Buff 条目（使用 ID=4001-4004，3001-3003 已被偷袭 Buff 占用）：
  - ID=4001：战意激昂攻速提升（攻速 +20%，持续 10 秒）
  - ID=4002：战意激昂伤害提升（伤害 +15%，持续 10 秒）
  - ID=4003：狂怒之心伤害提升基础档（伤害 +15%，条件持续型）
  - ID=4004：狂怒之心伤害提升翻倍档（伤害 +30%，条件持续型）
  - 同步更新 SummonerSkillTable.txt 中 skill 101 的 BuffIds=4003,4004，skill 102 的 BuffIds=4001,4002
  - 在 BuffFactory.cs 中注册 4001-4004
- [ ] 1.3 运行 DataTableGenerator，重新生成 `SummonerSkillTable.cs` / `.bytes` 及更新后的 `BuffTable.cs` / `.bytes`（需在 Unity Editor 中手动执行）
- [x] 1.4 验证生成的 `SummonerSkillTable.cs` 包含 `SummonerClass`、`UnlockTier`、`BranchId`、`Params`（float[]）、`BuffIds`（int[]）字段，且不含旧 `EffectType`/`EffectValue`

## 2. 召唤师技能框架——接口

- [x] 2.1 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/Interface/ISummonerSkill.cs`
  - 定义：`SkillId`、`Init(SummonerSkillContext, SummonerSkillTable)`、`Tick(float dt)`、`TryCast()`、`CanCast()`、`GetCooldownRemaining()`
- [x] 2.2 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/Interface/ISummonerPassive.cs`
  - 定义：`PassiveId`、`Init(SummonerSkillContext, SummonerSkillTable)`、`Tick(float dt)`、`Dispose()`

## 3. 召唤师技能框架——Context 与基类

- [x] 3.1 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerSkillContext.cs`
  - 字段：`RuntimeData`（`SummonerRuntimeDataManager`）、`EntityTracker`（`CombatEntityTracker`）
  - 注：BuffApplyHelper 是 static class，不作为字段，技能直接通过 EntityTracker 迭代并调用 BuffManager.AddBuff
- [x] 3.2 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerSkillBase.cs`（抽象类，实现 `ISummonerSkill`）
  - `Tick`：冷却倒计时
  - `CanCast`：冷却 + 灵力检查（`RuntimeData.CurrentMP >= m_Config.SpiritCost`）
  - `TryCast`：调用 CanCast → 扣灵力 → 进入冷却 → 调用 `abstract ExecuteSkill()`
- [x] 3.3 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerPassiveBase.cs`（抽象类，实现 `ISummonerPassive`）
  - 内置 `bool m_IsActive` 状态标记
  - `Tick`：调用 `abstract OnTick(float dt)`
  - `Dispose`：调用 `abstract OnDispose()`

## 4. 召唤师技能框架——Factory 与 Manager

- [x] 4.1 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerSkillFactory.cs`
  - `Register(int id, Func<ISummonerSkill>)`、`RegisterPassive(int id, Func<ISummonerPassive>)`
  - `Create(int id)`、`CreatePassive(int id)`（未注册返回 null + Error 日志）
  - `RegisterAll()`：注册所有已实现技能（当前注册 ID=101 被动、ID=102 主动）
- [x] 4.2 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/SummonerSkillManager.cs`（MonoBehaviour）
  - `List<ISummonerSkill> Skills`、`List<ISummonerPassive> Passives`
  - `SetActive(bool)`：false 时跳过 Update；false 时依次调用所有 `Passives[i].Dispose()`
  - `Update()`：Tick 所有技能 + 检测 Q/E/R 键触发对应槽位 `Skills[slot].TryCast()`
  - `UpdateSkillsFromData(IReadOnlyList<int> skillIds)`：清空列表，遍历 ID，用 Factory 创建实例，Init 后加入列表

## 5. 狂战士技能实现

- [x] 5.1 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/Skills/BerserkerPassive.cs`（继承 `SummonerPassiveBase`）
  - `OnTick()`：轮询 `EntityTracker.GetAllies()`，检测是否有友方 HP < `Params[0]` × MaxHp
  - 条件 A 满足时（`!m_IsAlliesActive`）→ 迭代友方，各自 AddBuff(BuffIds[0]) → `m_IsAlliesActive = true`
  - 条件 B 满足时（`!m_IsSelfActive`，召唤师 HP < `Params[1]` × MaxHp）→ 移除 BuffIds[0]，施加 BuffIds[1]（翻倍 Buff）→ `m_IsSelfActive = true`
  - 条件解除时移除对应 Buff，重置标记
  - `OnDispose()`：强制移除所有已施加的 BerserkerRage Buff
- [x] 5.2 新建 `Assets/AAAGame/Scripts/Game/SummonerSkill/Skills/BerserkerActiveSkill.cs`（继承 `SummonerSkillBase`）
  - 重写 `CanCast()`：在基类检查基础上额外检查 `RuntimeData.CurrentHP > Params[0]`
  - 实现 `ExecuteSkill()`：
    1. `RuntimeData.ReduceHP(Params[0])` 扣减生命值
    2. 迭代友方，各自 AddBuff(BuffIds[0])（攻速 Buff）
    3. 迭代友方，各自 AddBuff(BuffIds[1])（伤害 Buff）

## 6. 集成

- [x] 6.1 在 `PreloadProcedure.InitializeSkillSystem()` 中调用 `SummonerSkillFactory.RegisterAll()`（与 SkillFactory.RegisterAll 同行）
- [x] 6.2 在 `CombatManager.InitializeSummonerSkillSystem()` 中创建 `SummonerSkillContext`（RuntimeData + EntityTracker）
- [x] 6.3 获取/创建 `SummonerSkillManager`，调用 `UpdateSkillsFromData(PassiveSkillIds + ActiveSkillIds)`
- [x] 6.4 战斗开始时 `SetActive(true)`，战斗结束（EndCombat 开头）时 `SetActive(false)`

## 7. 验证

- [ ] 7.1 战意激昂：HP > 20 时按 Q，确认 HP 减少 20 + 全体棋子获得攻速/伤害 Buff + 冷却计时启动
- [ ] 7.2 战意激昂：HP ≤ 20 时按 Q，确认无任何效果
- [ ] 7.3 战意激昂：10s 后确认 Buff 自动消失
- [ ] 7.4 狂怒之心：令某友方棋子 HP 降至 50% 以下，确认全体获得 +15% 伤害 Buff
- [ ] 7.5 狂怒之心：令召唤师自身 HP 降至 40% 以下，确认全体 Buff 变为 +30%
- [ ] 7.6 狂怒之心：友方 HP 恢复后，确认 Buff 被移除，无残留
- [ ] 7.7 战斗结束后（SetActive=false）：确认所有 Buff 被 Dispose 清除，按键无反应
