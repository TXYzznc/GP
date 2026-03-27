## Why

召唤师技能是召唤师职业核心玩法的重要组成部分，目前游戏缺乏召唤师技能执行框架。召唤师技能在作用对象（场上全体棋子）、触发资源（生命值/灵力）、被动检测逻辑上与现有玩家技能完全不同，需要独立建立一套系统，而不是复用 `PlayerSkillManager`。本期仅实现狂战士的两个初始技能（固定被动 + 固定主动），但架构必须支撑未来全职业扩展。

## What Changes

- 新增召唤师技能接口：`ISummonerSkill`（主动）、`ISummonerPassive`（被动），独立于 `IPlayerSkill`
- 新增召唤师技能上下文 `SummonerSkillContext`，包含生命值引用、灵力引用、`CombatEntityTracker` 访问
- 新增 `SummonerSkillManager`（MonoBehaviour），管理召唤师技能的注册、Tick、输入触发
- 新增 `SummonerSkillFactory`，通过字典注册 SkillId → 创建函数，模式同 `SkillFactory`
- 扩展 `SummonerSkillTable`：新增 `HealthCost`（float，生命消耗）字段
- 实现狂战士技能：
  - `BerserkerPassive`（狂怒之心）：被动，每帧轮询检测全体友方 HP < 50% 时提升伤害，自身 HP < 40% 时叠加额外提升
  - `BerserkerActiveSkill`（战意激昂）：主动，消耗 20 HP，持续 10s 提升场上全体召唤物攻速 +20%、伤害 +15%

## Capabilities

### New Capabilities
- `summoner-skill-framework`: 召唤师技能框架——接口、Context、Manager、Factory 的完整架构
- `berserker-initial-skills`: 狂战士初始两技能的具体实现（狂怒之心 + 战意激昂）

### Modified Capabilities
- `player-summoner-stat-split`: `SummonerSkillTable` 新增 `HealthCost`（float）字段，DataTable 需重新生成（不对，一般情况下是不消耗HP的，消耗的都是SpiritCost，不需要添加新的字段增加复杂度）

## Impact

- **新文件**：`ISummonerSkill.cs`、`SummonerSkillContext.cs`、`SummonerSkillBase.cs`、`SummonerPassiveBase.cs`、`SummonerSkillManager.cs`、`SummonerSkillFactory.cs`、`Skills/BerserkerPassive.cs`、`Skills/BerserkerActiveSkill.cs`
- **配置表**：`SummonerSkillTable.xlsx` 新增 `HealthCost` 字段，需重新运行 DataTableGenerator
- **PlayerSkillManager / SkillFactory**：不修改，完全独立
- **CombatEntityTracker**：只读访问，不修改
- **BuffApplyHelper / BuffTable**：主动技能通过 Buff 系统应用伤害/攻速 Buff，需确认 Buff ID 已在 BuffTable 配置
