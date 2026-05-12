## ADDED Requirements

### Requirement: Buff 伤害携带施法者属性
DoT 类 Buff（BleedBuff、BurnBuff、PoisonBuff、MeltBuff）造成伤害时，SHALL 将施法者 ChessAttribute 传入 TakeDamage，使伤害归因链路完整。

#### Scenario: 流血Buff携带施法者
- **WHEN** BleedBuff tick 触发伤害时
- **THEN** TakeDamage 调用的 casterAttribute 参数为施法该 Buff 的棋子属性（非 null）

#### Scenario: 灼烧Buff多层携带施法者
- **WHEN** BurnBuff 有 N 层时触发伤害
- **THEN** 伤害值为 damagePerStack × N，且 casterAttribute 为施法者属性

#### Scenario: 中毒Buff携带施法者
- **WHEN** PoisonBuff tick 触发时
- **THEN** 伤害为 MaxHp × ratio，casterAttribute 正确传入

#### Scenario: 施法者已死亡时 caster 为 null
- **WHEN** 施法者在 Buff tick 前死亡
- **THEN** TakeDamage 以 null casterAttribute 调用，不抛出异常，伤害正常结算

### Requirement: 反伤 Buff 不传施法者以防递归
ReflectDamageBuff 反弹伤害时，SHALL 不传入施法者属性（或传 null），避免触发反弹反弹的无限递归。

#### Scenario: 反伤不触发递归
- **WHEN** 攻击者攻击带反伤 Buff 的目标
- **THEN** 攻击者受到反弹伤害，且不会再次触发 ReflectDamageBuff

#### Scenario: 反伤显示正确伤害类型
- **WHEN** 反弹伤害生效
- **THEN** 飘字显示类型为「反弹伤害」
