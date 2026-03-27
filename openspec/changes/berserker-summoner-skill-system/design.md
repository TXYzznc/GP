## Context

项目已有两套独立技能系统：
- **玩家技能**：`IPlayerSkill` + `PlayerSkillManager` + `SkillFactory`，作用于玩家自身（移速/治疗/投射物），按键触发，消耗MP（`SkillCommonConfig.Cost`）
- **棋子技能**：`IChessSkill` / `IChessPassive` + `ChessSkillBase`，作用于单个棋子，由棋子 AI 驱动

召唤师技能的作用域是**战场全体棋子**，消耗资源是**召唤师生命值**（主动技能），被动触发需要监控战场状态。两套现有系统均不能直接复用，需要独立建立第三套系统，但设计模式对齐现有规范。

## Goals / Non-Goals

**Goals:**
- 建立召唤师技能框架（接口 + Context + Manager + Factory + 基类）
- 实现狂战士两个初始技能（固定被动：狂怒之心；固定主动：战意激昂）
- 框架支持未来扩展：多职业、多阶解锁、分支特化技能

**Non-Goals:**
- 实现其他职业技能（术士、混沌等）
- 实现第三阶及以上解锁技能（王者号令、天灾降临等）
- 技能树 UI / 技能升级系统
- 技能动画与特效（占位实现即可）

## Decisions

### 1. 独立接口而非继承现有接口
**决策**：定义 `ISummonerSkill`（主动）和 `ISummonerPassive`（被动），不继承 `IPlayerSkill` 或 `IChessSkill`。

**原因**：召唤师技能的 Init 签名不同（需要 `SummonerSkillContext` 而非 `PlayerSkillContext`），强行继承会导致接口膨胀或 Context 类型不安全。模式对齐 `IChessSkill` / `IChessPassive` 的分离设计。

---

### 2. SummonerSkillContext 包含字段
**决策**：
```csharp
public class SummonerSkillContext
{
    public SummonerEntity Summoner;          // 召唤师实体（生命值、灵力）
    public CombatEntityTracker EntityTracker; // 获取全体友方/敌方棋子
    public BuffApplyHelper BuffHelper;        // 统一 Buff 应用入口
}
```

**原因**：主动技能需要扣血（Summoner.Health），被动技能需要轮询全体友方 HP（EntityTracker.GetAllies），两者都需要 Buff 应用（BuffHelper）。不放 Transform/Controller 是因为召唤师技能不需要位移操作。

---

### 3. 被动技能检测：每帧轮询 vs 事件驱动
**决策**：被动技能在 `Tick(float dt)` 中每帧轮询检测条件，通过 `m_IsActive` 标记避免重复应用 Buff。

**原因**：【狂怒之心】需要实时感知"全体友方中是否有 HP < 50% 的单位"，这是一个聚合查询，事件驱动需要每个棋子受伤时广播并聚合判断，复杂度更高。对于最多十几个棋子的战场，每帧轮询代价极低。

**备选**：伤害事件广播后重新计算 → 拒绝，因为需要订阅所有棋子的伤害事件，棋子动态增减时维护成本高。

---

### 4. 主动技能消耗资源：生命值扣除
**决策**：【战意激昂】在 `TryCast()` 中检查 `Summoner.Health.Current > HealthCost`，满足条件后直接调用 `Summoner.Health.Modify(-HealthCost)` 扣血。

**原因**：按策划设定消耗生命值而非灵力。`HealthCost` 从 `SummonerSkillTable.HealthCost` 读取，保持配置化。

---

### 5. Buff 应用方式
**决策**：主动技能【战意激昂】通过现有 `BuffApplyHelper.ApplyBuff(buffId, target, isGroupTarget=true)` 向全体友方棋子施加攻速和伤害 Buff，Buff 配置（持续时间/数值）在 `BuffTable` 中配置，技能 `SummonerSkillTable` 仅记录 BuffId 引用（复用现有 `EffectValue`/`Duration` 字段或新增 `BuffId` 字段）。

**原因**：已有 BuffApplyHelper 全体应用接口，复用避免重复实现。

---

### 6. Manager 挂载位置
**决策**：`SummonerSkillManager` 作为 MonoBehaviour 挂载在召唤师实体（SummonerEntity）的 GameObject 上，与 `PlayerSkillManager` 挂载在玩家 GameObject 上的模式一致。

---

### 7. 技能输入：键位独立
**决策**：召唤师主动技能输入由 `SummonerSkillManager.Update()` 处理，使用独立键位（如 Q/E/R），通过 `PlayerInputManager` 的新槽位 `SummonerSkillDown(slot)` 触发，与玩家技能 J/K/L 不冲突。

## Risks / Trade-offs

- **SummonerEntity 引用**：目前不确定召唤师实体类的具体名称和 HP 接口，需要在实现时确认或创建占位接口 `ISummonerHealth` → 实现时先用 `PlayerRuntimeDataManager` 的生命值接口
- **BuffTable 依赖**：【战意激昂】的攻速/伤害 Buff 需要 BuffTable 中预先配置对应条目，若 Buff ID 不存在则技能无效 → tasks 中明确列出需要配置的 Buff 条目
- **被动 Buff 叠加**：【狂怒之心】在条件变化时需要移除旧 Buff 再添加新 Buff，避免多层叠加 → 使用唯一 BuffKey 标记，应用前先移除同 Key 的旧 Buff
- **战斗外调用**：Manager 在非战斗状态下也在 Tick，浪费性能 → Manager 通过 `SetActive(bool)` 在战斗开始/结束时开关

## Migration Plan

1. 扩展 `SummonerSkillTable.xlsx` 新增 `HealthCost` 字段
2. 运行 DataTableGenerator 重新生成 `SummonerSkillTable.cs`
3. 新建框架文件（接口/基类/Context/Manager/Factory）
4. 实现两个狂战士技能
5. 在召唤师实体初始化时注册并启动 Manager

**回滚**：所有新文件独立，删除即可回滚，不影响现有战斗系统。

## Open Questions

- `SummonerEntity`（或等价类）的生命值接口是什么？（待确认）
- BuffTable 中是否已有"攻速提升"和"伤害提升"类型的通用 Buff 条目可以直接引用？
- 召唤师技能输入在战斗 UI 中如何呈现？（本期不做 UI，只做逻辑）
