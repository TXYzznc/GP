## ADDED Requirements

### Requirement: HpFloor 限制生命值下限
ChessAttribute SHALL 支持 HpFloor 属性，当设置 > 0 时，TakeDamage 后 CurrentHp 不低于 HpFloor，棋子不会因此死亡。

#### Scenario: HpFloor=0 时保持原有行为
- **WHEN** HpFloor 未设置（默认 0），棋子受到致命伤害
- **THEN** CurrentHp 降至 0，IsDead == true

#### Scenario: HpFloor>0 时生命值被锁定
- **WHEN** HpFloor 设为 1，棋子当前 HP=2，受到 100 点伤害
- **THEN** CurrentHp == HpFloor(1)，IsDead == false

#### Scenario: HpFloor 设为负数时修正为 0
- **WHEN** 设置 HpFloor = -10
- **THEN** 实际 HpFloor == 0（Math.Max 修正）

#### Scenario: 治疗仍能超过 HpFloor 上限至 MaxHp
- **WHEN** HpFloor=100，棋子当前 HP=100，受到治疗 500
- **THEN** CurrentHp 上限为 MaxHp，不超出 MaxHp
