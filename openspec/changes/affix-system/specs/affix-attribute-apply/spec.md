## ADDED Requirements

### Requirement: 装备宝物时应用词条属性
系统 SHALL 在宝物装备到棋子时，遍历该宝物的所有词条，将每条词条的属性值叠加到棋子的 ChessAttribute 对应属性上。

#### Scenario: 装备带词条的宝物
- **WHEN** 一个拥有 [攻击力+63, 暴击率+16.1%] 词条的宝物被装备到棋子
- **THEN** 棋子的攻击力增加63，暴击率增加16.1%

#### Scenario: 多条相同属性词条叠加
- **WHEN** 宝物有两条攻击力词条 [+63, +55]
- **THEN** 棋子攻击力总共增加118

### Requirement: 卸下宝物时移除词条属性
系统 SHALL 在宝物从棋子卸下时，移除该宝物所有词条对棋子属性的加成。

#### Scenario: 卸下宝物恢复属性
- **WHEN** 卸下一个拥有 [攻击力+63] 词条的宝物
- **THEN** 棋子攻击力减少63，恢复到装备前的数值

### Requirement: 区分固定值和百分比属性
系统 SHALL 根据词条的 ValueType 区分处理：ValueType=1(Fixed) 直接加减数值，ValueType=2(Percent) 按百分比修改。

#### Scenario: 百分比类型词条应用
- **WHEN** 装备带有 [暴击率+16.1%(Percent类型)] 的宝物
- **THEN** 棋子暴击率属性增加16.1个百分点

#### Scenario: 固定值类型词条应用
- **WHEN** 装备带有 [攻击力+63(Fixed类型)] 的宝物
- **THEN** 棋子攻击力属性增加63点
