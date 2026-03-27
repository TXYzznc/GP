# 需求文档：自走棋棋子系统

## 简介

本文档定义了自走棋游戏的棋子系统需求。棋子系统是游戏的核心，负责管理棋子的生成、属性、战斗行为、技能释放和星级进化等功能。系统采用数据驱动设计，通过配置表（SummonChessTable、SummonChessSkillTable）定义棋子数据。

## 术语表

- **Chess_System**: 棋子系统，负责管理所有棋子相关功能
- **Chess_Entity**: 棋子实体，运行时的棋子实例
- **Chess_Config**: 棋子配置数据，从配置表加载
- **Chess_Manager**: 棋子管理器，负责棋子的生命周期管理
- **Chess_Attribute**: 棋子属性组件，管理生命值、法力值、攻击力等
- **Chess_AI**: 棋子AI组件，控制棋子的战斗行为
- **Chess_Skill**: 棋子技能组件，管理技能释放
- **Star_Level**: 星级，棋子的进化等级（1-3星）
- **Race**: 种族，用于触发羁绊效果
- **Class**: 职业，用于触发羁绊效果
- **Quality**: 品质，棋子的稀有度（1-4：蓝、紫、金、红）

## 需求

### 需求 1：棋子配置数据加载

**用户故事：** 作为开发者，我希望系统能够从配置表加载棋子数据，以便通过配置表管理棋子属性而无需修改代码。

#### 验收标准

1. WHEN 系统初始化时，THE Chess_System SHALL 加载 SummonChessTable 配置表
2. WHEN 配置表加载成功时，THE Chess_System SHALL 解析所有棋子配置数据
3. WHEN 配置表中存在无效数据时，THE Chess_System SHALL 记录错误日志并跳过该条数据
4. WHEN 配置表中的数组字段（Races、Classes）为空时，THE Chess_System SHALL 将其解析为空数组
5. THE Chess_System SHALL 支持通过棋子ID查询配置数据

### 需求 2：棋子实体生成与销毁

**用户故事：** 作为游戏系统，我希望能够动态生成和销毁棋子实体，以便在战斗中管理棋子的生命周期。

#### 验收标准

1. WHEN 请求生成棋子时，THE Chess_Manager SHALL 根据配置ID创建棋子实体
2. WHEN 生成棋子时，THE Chess_Manager SHALL 加载对应的预制体资源
3. WHEN 生成棋子时，THE Chess_Manager SHALL 初始化棋子的所有属性
4. WHEN 棋子生命值降为0时，THE Chess_Manager SHALL 销毁该棋子实体
5. WHEN 销毁棋子时，THE Chess_Manager SHALL 释放相关资源并触发销毁事件
6. THE Chess_Manager SHALL 维护所有活跃棋子的列表

### 需求 3：棋子属性系统

**用户故事：** 作为棋子实体，我需要管理自己的属性（生命值、法力值、攻击力等），以便参与战斗计算。

#### 验收标准

1. WHEN 棋子生成时，THE Chess_Attribute SHALL 根据配置初始化最大生命值、最大法力值、初始法力值
2. WHEN 棋子受到伤害时，THE Chess_Attribute SHALL 根据护甲和魔抗计算实际伤害
3. WHEN 棋子当前生命值变化时，THE Chess_Attribute SHALL 触发生命值变化事件
4. WHEN 棋子当前法力值变化时，THE Chess_Attribute SHALL 触发法力值变化事件
5. WHEN 棋子当前生命值超过最大值时，THE Chess_Attribute SHALL 将其限制为最大值
6. WHEN 棋子当前法力值超过最大值时，THE Chess_Attribute SHALL 将其限制为最大值
7. THE Chess_Attribute SHALL 提供查询当前属性值的接口

### 需求 4：棋子战斗行为（AI系统）

**用户故事：** 作为棋子，我需要根据AI类型自动执行战斗行为，以便在战斗中自主行动。

#### 验收标准

1. WHEN 棋子进入战斗状态时，THE Chess_AI SHALL 根据AI类型选择对应的行为模式
2. WHEN AI类型为近战（AIType=1）时，THE Chess_AI SHALL 寻找最近的敌人并移动到攻击范围内
3. WHEN AI类型为远程（AIType=2）时，THE Chess_AI SHALL 在原地攻击范围内的敌人
4. WHEN 棋子在攻击范围内有敌人时，THE Chess_AI SHALL 根据攻击速度执行攻击
5. WHEN 棋子没有可攻击目标时，THE Chess_AI SHALL 进入待机状态
6. WHEN 棋子移动时，THE Chess_AI SHALL 根据移动速度更新位置
7. THE Chess_AI SHALL 每帧更新棋子的行为状态

### 需求 5：棋子技能系统

**用户故事：** 作为棋子，我需要在满足条件时释放技能，以便对战局产生影响。

#### 验收标准

1. WHEN 棋子生成时，THE Chess_Skill SHALL 根据配置加载关联的技能数据
2. WHEN 棋子法力值达到最大值时，THE Chess_Skill SHALL 自动释放技能
3. WHEN 技能释放时，THE Chess_Skill SHALL 消耗对应的法力值
4. WHEN 技能释放时，THE Chess_Skill SHALL 执行技能效果
5. WHEN 技能ID为0时，THE Chess_Skill SHALL 不加载任何技能
6. THE Chess_Skill SHALL 提供手动触发技能的接口

### 需求 6：棋子星级进化系统

**用户故事：** 作为玩家，我希望能够将相同的棋子合成更高星级，以便提升棋子的战斗力。

#### 验收标准

1. WHEN 玩家拥有3个相同的1星棋子时，THE Chess_System SHALL 支持合成为1个2星棋子
2. WHEN 玩家拥有3个相同的2星棋子时，THE Chess_System SHALL 支持合成为1个3星棋子
3. WHEN 棋子进化时，THE Chess_System SHALL 根据NextStarId加载新的配置数据
4. WHEN 棋子已经是3星时，THE Chess_System SHALL 不允许继续进化
5. WHEN 棋子进化时，THE Chess_System SHALL 保持棋子的当前生命值和法力值比例
6. THE Chess_System SHALL 提供查询棋子是否可进化的接口

### 需求 7：棋子种族与职业系统

**用户故事：** 作为游戏系统，我需要识别棋子的种族和职业，以便计算羁绊效果。

#### 验收标准

1. WHEN 棋子生成时，THE Chess_Entity SHALL 记录其所有种族ID
2. WHEN 棋子生成时，THE Chess_Entity SHALL 记录其所有职业ID
3. THE Chess_Entity SHALL 支持查询棋子是否属于指定种族
4. THE Chess_Entity SHALL 支持查询棋子是否属于指定职业
5. WHEN 棋子拥有多个种族时，THE Chess_Entity SHALL 正确存储所有种族ID
6. WHEN 棋子拥有多个职业时，THE Chess_Entity SHALL 正确存储所有职业ID

### 需求 8：棋子伤害计算系统

**用户故事：** 作为战斗系统，我需要计算棋子对目标造成的伤害，以便正确处理战斗结果。

#### 验收标准

1. WHEN 棋子攻击目标时，THE Chess_System SHALL 根据攻击力计算基础伤害
2. WHEN 目标拥有护甲时，THE Chess_System SHALL 根据护甲值减少物理伤害
3. WHEN 目标拥有魔抗时，THE Chess_System SHALL 根据魔抗值减少魔法伤害
4. WHEN 计算后的伤害小于0时，THE Chess_System SHALL 将伤害设为0
5. WHEN 伤害计算完成时，THE Chess_System SHALL 应用伤害到目标的生命值
6. THE Chess_System SHALL 触发伤害事件供其他系统监听

### 需求 9：棋子状态管理

**用户故事：** 作为棋子实体，我需要管理自己的状态（待机、移动、攻击、死亡等），以便正确执行行为。

#### 验收标准

1. WHEN 棋子生成时，THE Chess_Entity SHALL 初始化为待机状态
2. WHEN 棋子开始移动时，THE Chess_Entity SHALL 切换到移动状态
3. WHEN 棋子开始攻击时，THE Chess_Entity SHALL 切换到攻击状态
4. WHEN 棋子生命值降为0时，THE Chess_Entity SHALL 切换到死亡状态
5. WHEN 棋子状态改变时，THE Chess_Entity SHALL 触发状态变化事件
6. THE Chess_Entity SHALL 提供查询当前状态的接口

### 需求 10：棋子资源管理

**用户故事：** 作为系统，我需要高效管理棋子的资源加载和释放，以便优化性能和内存使用。

#### 验收标准

1. WHEN 加载棋子预制体时，THE Chess_Manager SHALL 使用异步加载避免阻塞主线程
2. WHEN 相同预制体被多次请求时，THE Chess_Manager SHALL 复用已加载的资源
3. WHEN 棋子销毁时，THE Chess_Manager SHALL 正确释放预制体实例
4. WHEN 系统空闲时，THE Chess_Manager SHALL 支持预加载常用棋子资源
5. THE Chess_Manager SHALL 提供资源加载进度查询接口

### 需求 11：棋子查询与过滤

**用户故事：** 作为游戏系统，我需要查询和过滤棋子，以便实现各种游戏逻辑。

#### 验收标准

1. THE Chess_Manager SHALL 支持通过棋子ID查询棋子实体
2. THE Chess_Manager SHALL 支持查询指定阵营的所有棋子
3. THE Chess_Manager SHALL 支持查询指定范围内的所有棋子
4. THE Chess_Manager SHALL 支持查询指定种族的所有棋子
5. THE Chess_Manager SHALL 支持查询指定职业的所有棋子
6. THE Chess_Manager SHALL 支持查询指定星级的所有棋子

### 需求 12：棋子事件系统

**用户故事：** 作为开发者，我需要监听棋子的各种事件，以便实现UI更新、音效播放等功能。

#### 验收标准

1. WHEN 棋子生成时，THE Chess_System SHALL 触发棋子生成事件
2. WHEN 棋子销毁时，THE Chess_System SHALL 触发棋子销毁事件
3. WHEN 棋子生命值变化时，THE Chess_System SHALL 触发生命值变化事件
4. WHEN 棋子法力值变化时，THE Chess_System SHALL 触发法力值变化事件
5. WHEN 棋子释放技能时，THE Chess_System SHALL 触发技能释放事件
6. WHEN 棋子状态改变时，THE Chess_System SHALL 触发状态变化事件
7. THE Chess_System SHALL 提供事件订阅和取消订阅的接口

### 需求 13：玩家棋子解锁与存档

**用户故事：** 作为玩家，我希望系统能够记录我已解锁的棋子，以便在不同游戏会话中保持我的收集进度。

#### 验收标准

1. WHEN 玩家首次解锁棋子时，THE Chess_System SHALL 将该棋子ID添加到已解锁列表
2. WHEN 玩家重复解锁已拥有的棋子时，THE Chess_System SHALL 不重复添加到已解锁列表
3. WHEN 玩家存档时，THE Chess_System SHALL 将已解锁棋子列表序列化到存档数据
4. WHEN 玩家读档时，THE Chess_System SHALL 从存档数据反序列化已解锁棋子列表
5. WHEN 查询棋子是否已解锁时，THE Chess_System SHALL 返回正确的解锁状态
6. THE Chess_System SHALL 提供查询所有已解锁棋子的接口
7. THE Chess_System SHALL 提供查询已解锁棋子数量的接口
8. WHEN 玩家创建新存档时，THE Chess_System SHALL 初始化空的已解锁棋子列表
