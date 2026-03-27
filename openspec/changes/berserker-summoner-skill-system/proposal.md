## Why

召唤师技能是职业核心玩法的重要组成部分，目前游戏缺乏召唤师技能执行框架。召唤师技能在作用对象（场上全体棋子）、触发资源（灵力/特殊消耗）、被动检测逻辑上与现有玩家技能完全不同，需要独立建立一套系统。本期仅实现狂战士的两个初始技能（固定被动 + 固定主动），但架构必须支撑未来全职业扩展。

## What Changes

- 新增召唤师技能接口：`ISummonerSkill`（主动）、`ISummonerPassive`（被动），独立于 `IPlayerSkill`
- 新增 `SummonerSkillContext`，包含灵力/生命值引用、`CombatEntityTracker` 访问、`BuffApplyHelper`
- 新增 `SummonerSkillBase`（主动基类）、`SummonerPassiveBase`（被动基类）
- 新增 `SummonerSkillManager`（MonoBehaviour），管理注册、Tick、输入触发，战斗外可通过 `SetActive(false)` 停止
- 新增 `SummonerSkillFactory`，字典注册 SkillId → 创建函数，模式同 `SkillFactory`
- **重新设计 `SummonerSkillTable`**：对齐 `SummonChessSkillTable` 结构，移除 `EffectType`/`EffectValue`，新增 `SummonerClass`、`UnlockTier`、`BranchId`、`Params`（float[]），保留完整攻击/特效字段（DamageType/DamageCoeff/EffectHitType/ProjectilePrefabId 等），TXT 已生成
- 实现狂战士初始技能：
  - `BerserkerPassive`（狂怒之心）：被动，每帧轮询友方 HP 阈值，通过 BuffIds 应用全体伤害 Buff
  - `BerserkerActiveSkill`（战意激昂）：主动，消耗灵力（SpiritCost）+ 生命值（Params[0]=20），全体召唤物攻速/伤害 Buff

## Capabilities

### New Capabilities
- `summoner-skill-framework`: 召唤师技能框架——接口、Context、基类、Manager、Factory
- `berserker-initial-skills`: 狂战士初始两技能实现（狂怒之心 + 战意激昂）

### Modified Capabilities
- `player-summoner-stat-split`: `SummonerSkillTable` 完整重新设计，TXT 文件已在 `AI工作区/配置表/SummonerSkillTable.txt` 生成，需转 XLSX 并重新运行 DataTableGenerator

## Impact

- **新文件**：`ISummonerSkill.cs`、`ISummonerPassive.cs`、`SummonerSkillContext.cs`、`SummonerSkillBase.cs`、`SummonerPassiveBase.cs`、`SummonerSkillManager.cs`、`SummonerSkillFactory.cs`、`Skills/BerserkerPassive.cs`、`Skills/BerserkerActiveSkill.cs`
- **配置表**：`SummonerSkillTable` 完整重新设计（TXT 已生成），`BuffTable` 需新增狂战士技能所需 Buff 条目
- **PlayerSkillManager / SkillFactory**：不修改，完全独立
- **CombatEntityTracker / BuffApplyHelper**：只读访问，不修改
