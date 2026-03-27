## Context

项目已有两套独立技能系统：
- **玩家技能**：`IPlayerSkill` + `PlayerSkillManager` + `SkillFactory`，作用于玩家自身（移速/治疗/投射物），按键触发，消耗 `SpiritCost`
- **棋子技能**：`IChessSkill` / `IChessPassive` + `ChessSkillBase`，作用于单个棋子，由棋子 AI 驱动，配置来自 `SummonChessSkillTable`

召唤师技能的作用域是**战场全体棋子**，被动需要监控战场状态，部分技能有特殊消耗（如消耗生命值）。两套现有系统均不能直接复用。`SummonerSkillTable` 原有字段（EffectType/EffectValue）过于简单且无法描述多样的技能逻辑，需完整重新设计，对齐 `SummonChessSkillTable` 的成熟字段体系。

## Goals / Non-Goals

**Goals:**
- 建立召唤师技能框架（接口 + Context + 基类 + Manager + Factory）
- 完整重新设计 `SummonerSkillTable`（对齐棋子技能表 + 新增职业/阶段/Params 字段）
- 实现狂战士两个初始技能（狂怒之心 + 战意激昂）

**Non-Goals:**
- 其他职业技能实现
- 第三阶及以上解锁技能
- 技能树 UI / 技能升级系统
- 技能动画与特效（占位实现）

## Decisions

### 1. SummonerSkillTable 完整重新设计，对齐 SummonChessSkillTable

**决策**：移除旧字段 `EffectType`、`EffectValue`，新字段完全对齐 `SummonChessSkillTable`，额外增加召唤师专属字段：

| 新增字段 | 类型 | 说明 |
|---|---|---|
| `SummonerClass` | int | 所属职业 1=狂战 2=术士 3=混沌 4=德鲁伊 |
| `UnlockTier` | int | 解锁阶段 1=初始 3/4/5=对应阶段 |
| `BranchId` | int | 分支 0=固定 1=路线一 2=路线二 |
| `Params` | float[] | 技能专属数值参数（阈值/系数等） |

对齐继承字段（与 SummonChessSkillTable 一致）：`DamageType`、`DamageCoeff`、`BaseDamage`、`EffectHitType`、`ProjectilePrefabId`、`ProjectileSpeed`、`HitCount`、`BuffIds`（int[]）、`SelfBuffIds`（int[]）、`EffectId`、`HitEffectId`、`EffectSpawnHeight`

**原因**：攻击类技能（寂灭斩/野蛮冲撞等）需要投射物/伤害/特效字段；非攻击技能这些字段填 0 即可，不增加维护成本。统一来源避免两套字段体系。

---

### 2. Params（float[]）替代 Param1/Param2/Param3

**决策**：单个 `Params` 字段存储逗号分隔的 float 数组，与 `BuffIds`/`SelfBuffIds` 的 `int[]` 处理方式完全一致（`DataTableExtension.ParseArray<float>()`）。

**原因**：各技能参数数量不同（狂怒之心需要 4 个，战意激昂需要 3 个），固定命名列会导致大量空列。数组方式字段数不变，扩展零成本。含义通过 `Desc` 字段注释，代码按索引读取。

```csharp
float[] p = m_Config.Params;
// 狂怒之心：p[0]=0.5, p[1]=0.4, p[2]=0.15, p[3]=2.0
// 战意激昂：p[0]=20(HP消耗), p[1]=0.20, p[2]=0.15
```

---

### 3. 特殊消耗（消耗生命值）通过 Params 存储，不新增字段

**决策**：`战意激昂` 消耗生命值 20 存在 `Params[0]`，不新增 `HealthCost` 列。`SpiritCost` 字段保留用于灵力消耗。

**原因**：消耗生命值是极少数技能的特殊机制（目前仅狂战士一个），新增专用字段会使大多数行该列填 0，不值得。`Params` 已能承载此类特殊数值。

---

### 4. 独立接口，不继承现有接口

**决策**：`ISummonerSkill` 和 `ISummonerPassive` 独立定义，不继承 `IPlayerSkill` 或 `IChessSkill`。

**原因**：`Init` 签名不同（需 `SummonerSkillContext` + `SummonerSkillTable`），强行继承会引入类型转换或接口污染。

---

### 5. SummonerSkillContext 字段

```csharp
public class SummonerSkillContext
{
    public PlayerRuntimeDataManager RuntimeData; // 生命值/灵力访问
    public CombatEntityTracker EntityTracker;    // 全体友方/敌方棋子
    public BuffApplyHelper BuffHelper;           // 统一 Buff 应用
}
```

**原因**：三个字段覆盖所有已知技能需求。`PlayerRuntimeDataManager` 是现有生命值/灵力的运行时管理类，无需额外封装。

---

### 6. 被动检测：每帧轮询 + 状态标记防重复

**决策**：`SummonerPassiveBase.Tick()` 每帧检测条件，用 `bool m_IsActive` 标记当前激活状态，仅在状态**变化时**调用 `OnActivate()` / `OnDeactivate()`，避免每帧重复应用/移除 Buff。

**原因**：战场棋子数量有限（≤20），每帧轮询代价极低。事件驱动需要订阅动态棋子的伤害事件，维护成本更高。

---

### 7. Manager 键位与 PlayerSkillManager 独立

**决策**：召唤师技能使用 `Q/E/R` 槽位键，通过 `PlayerInputManager` 的新 `SummonerSkillDown(slot)` 方法触发，不与玩家技能 `J/K/L` 冲突。

## Risks / Trade-offs

- **PlayerRuntimeDataManager 接口**：需确认现有类是否暴露 HP 扣减方法 → 若无，在 Context 中用 getter/setter 包装
- **Buff 持续时间管理**：战意激昂的 10s Buff 需 Buff 系统支持按 Duration 自动过期 → 现有 Buff 系统应已支持，任务中确认
- **被动 Buff 唯一性**：狂怒之心在条件变化时需先移除旧 Buff 再应用新 Buff，避免叠加 → 用 `BuffKey` 唯一标识，移除时按 Key 清除

## Migration Plan

1. 将 `AI工作区/配置表/SummonerSkillTable.txt` 转换为 XLSX，放入 `AAAGameData/DataTables/`
2. 在 `BuffTable.xlsx` 新增狂战士所需 Buff 条目（攻速提升、伤害提升）
3. 运行 DataTableGenerator 重新生成 `SummonerSkillTable.cs` / `.bytes`
4. 实现框架代码（接口 → 基类 → Manager/Factory）
5. 实现两个狂战士技能
6. 在战斗初始化流程中集成 Manager

**回滚**：所有新文件独立，删除即可，不影响现有战斗/玩家/棋子系统。

## Open Questions

- `PlayerRuntimeDataManager` 是否有 `ModifyHp(float delta)` 方法？（确认后更新 Context 字段类型）
- `BuffTable` 中"攻速提升"/"伤害提升"通用 Buff 是否已存在可复用的条目？
