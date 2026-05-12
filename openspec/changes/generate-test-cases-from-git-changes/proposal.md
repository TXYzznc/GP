## Why

近三天内对战斗核心系统、阵营系统、Buff 系统、装备系统、羁绊系统、物品系统进行了大量重构与新功能开发，需要系统性的测试需求用例来验证所有变更正确运行、未引入回归问题。

## What Changes

- **Buff 伤害来源追踪**：BleedBuff、BurnBuff、PoisonBuff、MeltBuff 的 TakeDamage 调用新增 `CasterAttribute` 参数，支持伤害来源追踪
- **ReflectDamageBuff 参数修正**：TakeDamage 参数顺序调整，移除旧注释
- **阵营系统重构（CampType + CampRelationService）**：移除静态 PVP Team1-4 阵营，新增动态战场 API（AllocateBattlefield / ReleaseAllBattlefields / ClearCustomRelations）
- **CombatManager 战斗结束清理**：战斗结束时自动销毁所有飞行中的投射物 ChessProjectile
- **ChessAttribute 锁血机制**：新增 `HpFloor` 属性，生命值不会降至下限以下
- **ChessEquipmentManager 装备特效支持**：装备穿脱时联动 SpecialEffectManager 处理 SpecialEffectId；AttributeType.MagicPower 枚举名统一为 SpellPower
- **SynergyManager 羁绊激活重构**：目标缓存从 BuffId 列表改为 GameObject 列表，阵容变化时先移除旧效果再重新应用
- **GameEffectService 兼容层重构**：原有逻辑委托给 SpecialEffectManager，仅保留兼容接口
- **ItemManager 宝物词条自动生成**：创建宝物时自动调用 AffixGenerator.Generate 生成词条；新增 GetAllAffixData()；属性名兼容旧配置（MagicPower→SpellPower）
- **配置表更新**：AffixTable、BuffTable、ResourceConfigTable、SpecialEffectTable、SummonChessSkillTable、SummonChessTable、SynergyTable

## Capabilities

### New Capabilities

- `buff-damage-source-tracking`：Buff 造成伤害时携带施法者属性，支持后续伤害归因/统计
- `dynamic-battlefield-camp`：动态战场阵营分配，支持多场独立战斗互不干扰
- `projectile-cleanup-on-battle-end`：战斗结束时清理场上所有飞行投射物
- `chess-hp-floor-lock`：棋子 HpFloor 锁血机制
- `equipment-special-effect-binding`：装备穿脱联动 SpecialEffectManager 特效绑定
- `synergy-dynamic-refresh`：羁绊激活时对变化的阵容精确移除并重新应用效果
- `treasure-affix-auto-generate`：宝物创建时自动生成词条

### Modified Capabilities

- `game-effect-service`：GameEffectService 成为兼容层，执行逻辑委托给 SpecialEffectManager

## Impact

- 受影响核心系统：战斗管理（CombatManager）、阵营（CampRelationService）、Buff（Bleed/Burn/Poison/Melt/Reflect）、棋子属性（ChessAttribute）、装备（ChessEquipmentManager）、羁绊（SynergyManager）、物品（ItemManager）、效果执行（GameEffectService → SpecialEffectManager）
- 受影响配置表：AffixTable、BuffTable、ResourceConfigTable、SpecialEffectTable、SummonChessSkillTable、SummonChessTable、SynergyTable
- 间接影响：所有调用 TakeDamage 的棋子技能（嫦娥 ChangeMagicCircle、邪灵 EvilSpiritMagicCircle）
