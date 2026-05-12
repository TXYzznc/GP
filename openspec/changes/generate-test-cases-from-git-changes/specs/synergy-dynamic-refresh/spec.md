## ADDED Requirements

### Requirement: 羁绊激活时对已激活羁绊先移除再重应用
SynergyManager 在刷新羁绊状态时，若羁绊已激活且阵容发生变化，SHALL 先调用 DeactivateSynergyInternal 再重新 ActivateSynergy。

#### Scenario: 上棋后已激活羁绊刷新
- **WHEN** 羁绊 A 已激活，再上一个满足条件的棋子
- **THEN** 羁绊 A 先被失活，再以新目标列表重新激活，不重复应用效果

#### Scenario: 下棋后羁绊条件不再满足
- **WHEN** 羁绊 A 已激活，下掉一个关键棋子使条件不满足
- **THEN** 羁绊 A 调用 DeactivateSynergyInternal，OnSynergyStateChanged(id, false) 触发

#### Scenario: 新激活的羁绊触发事件
- **WHEN** 羁绊从未激活状态变为满足条件
- **THEN** OnSynergyStateChanged(id, true) 触发，效果应用到所有匹配棋子

### Requirement: 羁绊目标缓存使用 GameObject List
SynergyManager SHALL 使用 m_SynergyTargetCache（Dictionary<int, List<GameObject>>）缓存激活时的目标，用于精确失活。

#### Scenario: 目标缓存与激活目标一致
- **WHEN** 激活羁绊时
- **THEN** 缓存的 GameObject 列表与实际应用效果的目标完全一致

#### Scenario: 失活时仅移除缓存中的目标
- **WHEN** 羁绊失活
- **THEN** 只对缓存记录中的 GameObject 移除效果，不影响其他棋子
