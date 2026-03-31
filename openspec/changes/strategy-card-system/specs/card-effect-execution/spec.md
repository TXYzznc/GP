## ADDED Requirements

### Requirement: 卡牌效果接口定义
系统 SHALL 定义 ICardEffect 接口，所有策略卡效果类必须实现此接口。

#### Scenario: 效果接口包含执行方法
- **WHEN** 创建新的策略卡效果类时
- **THEN** 该类必须实现 ICardEffect 接口的 Execute(CardData cardData, Vector3 targetPosition) 方法

#### Scenario: 效果接口包含初始化方法
- **WHEN** 创建策略卡效果实例时
- **THEN** 系统调用 Init(CardTable.Row row) 方法初始化效果参数

### Requirement: 效果执行器
CardEffectExecutor SHALL 根据 CardId 创建对应的效果实例并执行。

#### Scenario: 根据 CardId 创建效果
- **WHEN** 玩家释放卡牌 ID=1001（神圣庇护）
- **THEN** CardEffectExecutor 创建 HolyShieldCardEffect 实例

#### Scenario: 执行效果失败时记录日志
- **WHEN** CardEffectExecutor 无法找到对应的效果类
- **THEN** 系统使用 DebugEx.Error 输出错误日志，包含 CardId 和错误原因

### Requirement: 目标选择逻辑
系统 SHALL 根据 CardTable.TargetType 自动选择目标单位。

#### Scenario: 目标类型为自身
- **WHEN** 卡牌的 TargetType=1（自身）
- **THEN** 系统选择玩家召唤师作为目标

#### Scenario: 目标类型为友方单体
- **WHEN** 卡牌的 TargetType=2（友方单体）
- **THEN** 系统选择释放位置最近的友方棋子作为目标

#### Scenario: 目标类型为友方全体
- **WHEN** 卡牌的 TargetType=3（友方全体）
- **THEN** 系统获取所有友方棋子作为目标列表

#### Scenario: 目标类型为敌方单体
- **WHEN** 卡牌的 TargetType=4（敌方单体）
- **THEN** 系统选择释放位置最近的敌方单位作为目标

#### Scenario: 目标类型为敌方全体
- **WHEN** 卡牌的 TargetType=5（敌方全体）
- **THEN** 系统获取所有敌方单位作为目标列表

#### Scenario: 目标类型为全场
- **WHEN** 卡牌的 TargetType=6（全场）
- **THEN** 系统获取所有单位（友方+敌方）作为目标列表

### Requirement: 伤害效果执行
系统 SHALL 根据 CardTable 的伤害配置对目标造成伤害。

#### Scenario: 造成物理伤害
- **WHEN** 卡牌的 DamageType=1（物理），DamageCoeff=2.5，BaseDamage=0
- **THEN** 系统计算伤害值 = 召唤师攻击力 × 2.5，对目标造成物理伤害

#### Scenario: 造成固定伤害
- **WHEN** 卡牌的 DamageCoeff=0，BaseDamage=150
- **THEN** 系统对目标造成 150 点固定伤害

#### Scenario: 造成真实伤害
- **WHEN** 卡牌的 DamageType=3（真实）
- **THEN** 系统对目标造成真实伤害，忽略防御力

### Requirement: Buff 效果执行
系统 SHALL 根据 CardTable 的 Buff 配置对目标施加 Buff。

#### Scenario: 释放时施加 Buff
- **WHEN** 卡牌的 InstantBuffs="10301:3"（Buff ID 10301，持续 3 秒）
- **THEN** 系统在释放时对目标施加 Buff 10301，持续 3 秒

#### Scenario: 命中时施加 Buff
- **WHEN** 卡牌的 HitBuffs="10302:5"（Buff ID 10302，持续 5 秒）
- **THEN** 系统在伤害命中目标时施加 Buff 10302，持续 5 秒

#### Scenario: 施加多个 Buff
- **WHEN** 卡牌的 InstantBuffs="10303:3,10304:3"
- **THEN** 系统同时施加 Buff 10303 和 10304，各持续 3 秒

### Requirement: 特效播放
系统 SHALL 根据 CardTable 的特效配置播放视觉效果。

#### Scenario: 播放技能特效
- **WHEN** 卡牌的 EffectId=30101
- **THEN** 系统在释放位置加载并播放特效 30101，高度偏移为 EffectSpawnHeight

#### Scenario: 播放受击特效
- **WHEN** 卡牌的 HitEffectId=30201
- **THEN** 系统在目标位置播放受击特效 30201

#### Scenario: 特效播放完毕后销毁
- **WHEN** 特效播放完成
- **THEN** 系统自动销毁特效对象

### Requirement: 参数配置解析
系统 SHALL 解析 CardTable.ParamsConfig 的 JSON 配置，获取特殊效果参数。

#### Scenario: 解析护盾数值
- **WHEN** 卡牌的 ParamsConfig={"shieldAmount":100,"duration":5}
- **THEN** 系统解析出 shieldAmount=100，duration=5

#### Scenario: 解析失败时使用默认值
- **WHEN** ParamsConfig 的 JSON 格式错误
- **THEN** 系统使用 DebugEx.Error 输出错误日志，使用默认参数值

### Requirement: 独立效果脚本
每张策略卡 SHALL 有独立的效果脚本类，继承自 ICardEffect 接口。

#### Scenario: 神圣庇护效果
- **WHEN** 执行卡牌 ID=1001（神圣庇护）
- **THEN** HolyShieldCardEffect 为所有友方单位施加护盾 Buff

#### Scenario: 烈焰风暴效果
- **WHEN** 执行卡牌 ID=1002（烈焰风暴）
- **THEN** FlameStormCardEffect 对敌方全体造成魔法伤害并施加燃烧 Buff

#### Scenario: 时间回溯效果
- **WHEN** 执行卡牌 ID=1003（时间回溯）
- **THEN** TimeRewindCardEffect 使目标友方单位恢复到 3 秒前的状态（HP/MP/位置）
