## ADDED Requirements

### Requirement: 范围预览对象创建
系统 SHALL 在拖拽开始时创建范围预览对象，显示卡牌作用范围。

#### Scenario: 创建 Projector 预览
- **WHEN** 玩家开始拖拽卡牌
- **THEN** 系统实例化包含 Projector 组件的预制体

#### Scenario: 设置预览范围大小
- **WHEN** 创建范围预览对象时
- **THEN** 根据 CardTable.AreaRadius 设置 Projector 的投影大小

#### Scenario: 设置预览颜色
- **WHEN** 创建范围预览对象时
- **THEN** 使用黄色材质显示范围圈

### Requirement: 范围预览位置更新
系统 SHALL 在拖拽过程中实时更新范围预览位置。

#### Scenario: 射线检测战场地面
- **WHEN** 玩家拖拽卡牌时
- **THEN** 从鼠标位置发射射线，检测战场地面碰撞点

#### Scenario: 更新预览位置
- **WHEN** 射线命中战场地面
- **THEN** 将范围预览对象移动到命中点位置，高度偏移为 0.1

#### Scenario: 隐藏预览（不在战场）
- **WHEN** 射线未命中战场地面
- **THEN** 隐藏范围预览对象

### Requirement: 卡槽吸附区域提示
系统 SHALL 在鼠标进入卡槽吸附区域时显示视觉反馈。

#### Scenario: 显示吸附区域
- **WHEN** 鼠标位置在 varCardSlotAdsorptionArea 区域内
- **THEN** 显示 varCardSlotAdsorptionArea 的 Image 组件

#### Scenario: 隐藏吸附区域
- **WHEN** 鼠标离开 varCardSlotAdsorptionArea 区域
- **THEN** 隐藏 varCardSlotAdsorptionArea 的 Image 组件

#### Scenario: 吸附区域高亮效果
- **WHEN** 鼠标在吸附区域内
- **THEN** varCardSlotAdsorptionArea 的 Image 透明度设为 0.5，颜色为白色

### Requirement: 范围预览对象池
系统 SHALL 使用对象池管理范围预览对象，避免频繁创建销毁。

#### Scenario: 从对象池获取预览对象
- **WHEN** 需要创建范围预览对象时
- **THEN** 从对象池获取可用实例，如果池为空则创建新实例

#### Scenario: 回收预览对象到对象池
- **WHEN** 拖拽结束销毁预览对象时
- **THEN** 将对象回收到对象池而非直接销毁

#### Scenario: 对象池初始化
- **WHEN** CardManager 初始化时
- **THEN** 预创建 2 个范围预览对象到对象池

### Requirement: 射线检测优化
系统 SHALL 限制射线检测频率，避免性能问题。

#### Scenario: 限制检测频率
- **WHEN** 玩家拖拽卡牌时
- **THEN** 每 0.1 秒执行一次射线检测，而非每帧检测

#### Scenario: 使用缓存的射线结果
- **WHEN** 在 0.1 秒间隔内
- **THEN** 使用上次检测的结果更新预览位置

### Requirement: 范围预览销毁
系统 SHALL 在拖拽结束时销毁范围预览对象。

#### Scenario: 释放卡牌后销毁
- **WHEN** 玩家在战场区域释放卡牌
- **THEN** 销毁范围预览对象，回收到对象池

#### Scenario: 取消拖拽后销毁
- **WHEN** 玩家在无效区域或吸附区域释放卡牌
- **THEN** 销毁范围预览对象，回收到对象池

### Requirement: 特殊范围类型支持
系统 SHALL 支持不同的范围预览类型（圆形、扇形、矩形）。

#### Scenario: 圆形范围预览
- **WHEN** CardTable.AreaRadius > 0
- **THEN** 显示圆形范围预览，半径为 AreaRadius

#### Scenario: 无范围预览（单体目标）
- **WHEN** CardTable.AreaRadius = 0
- **THEN** 显示小型目标指示器而非范围圈

#### Scenario: 全场范围预览
- **WHEN** CardTable.TargetType = 6（全场）
- **THEN** 显示覆盖整个战场的范围预览
