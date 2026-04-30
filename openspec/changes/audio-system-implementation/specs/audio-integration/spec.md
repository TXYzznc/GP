## ADDED Requirements

### Requirement: Procedure 系统集成
系统 SHALL 与 GF.Procedure 系统集成，在场景切换时自动管理 BGM。

#### Scenario: 场景进入时播放 BGM
- **WHEN** Procedure 进入新场景（如 BaseRoomProcedure）
- **THEN** 系统自动根据场景类型播放对应的 BGM（例如基地场景播放 ID=10010 的 BGM）

#### Scenario: 场景退出时停止 BGM
- **WHEN** Procedure 离开当前场景
- **THEN** 系统逐渐淡出当前 BGM，为新场景的 BGM 做准备

#### Scenario: Procedure 映射 BGM ID
- **WHEN** AudioManager 维护 Procedure 类型到 BGM ID 的映射表
- **THEN** 场景切换时快速查表获取对应 BGM ID，无需硬编码

### Requirement: GF.Event 系统集成
系统 SHALL 通过 GF.Event 事件系统收听业务事件，触发相应音效。

#### Scenario: 战斗开始事件
- **WHEN** 触发 CombatStartEvent
- **THEN** 系统播放战斗 BGM（根据敌人等级选择普通/Elite/Boss BGM）并播放战斗开始音效 ID=20301

#### Scenario: 战斗胜利事件
- **WHEN** 触发 CombatVictoryEvent
- **THEN** 系统停止战斗 BGM（淡出 1 秒）并播放胜利音效 ID=20401

#### Scenario: 战斗失败事件
- **WHEN** 触发 CombatDefeatEvent
- **THEN** 系统停止战斗 BGM（淡出 1.5 秒）并播放失败音效 ID=20402

#### Scenario: 任务完成事件
- **WHEN** 触发 TaskCompleteEvent
- **THEN** 系统播放任务完成音效 ID=20804

#### Scenario: 物品获得事件
- **WHEN** 触发 ItemObtainEvent，根据物品稀有度决定音效
- **THEN** 普通物品播放 ID=20602，稀有物品播放 ID=20603（传奇）

### Requirement: UI 系统集成
系统 SHALL 与 UIForm 系统集成，在 UI 打开关闭时播放音效。

#### Scenario: UI 打开音效
- **WHEN** UIForm.OnOpen() 被调用
- **THEN** 系统自动播放该 UI 对应的打开音效（可在 UIForm 中配置，默认 ID=20001）

#### Scenario: UI 关闭音效
- **WHEN** UIForm.OnClose() 被调用
- **THEN** 系统自动播放该 UI 对应的关闭音效（默认 ID=20002）

#### Scenario: 按钮点击音效
- **WHEN** 任何 Button 组件被点击
- **THEN** 系统播放通用按钮音效 ID=20000

#### Scenario: UI 音效快速扩展
- **WHEN** 开发者为自定义按钮绑定音效：button.AddAudioClick(20010)
- **THEN** 该按钮点击时播放 ID=20010 的音效

### Requirement: 战斗系统音效支持
系统 SHALL 为战斗系统各阶段提供音效支持。

#### Scenario: 棋子选中音效
- **WHEN** 用户点击选中棋子卡片
- **THEN** 系统播放选中音效 ID=20101

#### Scenario: 攻击挥动音效
- **WHEN** 棋子执行攻击动作
- **THEN** 系统在攻击关键帧处播放挥动音效 ID=20201，由战斗管理器通过事件触发

#### Scenario: 伤害类型对应音效
- **WHEN** 造成伤害并根据伤害等级播放音效
- **THEN** 轻击 ID=20301，普通 ID=20302，重击 ID=20303，暴击 ID=20304

#### Scenario: 技能释放音效
- **WHEN** 棋子释放技能
- **THEN** 系统播放技能释放音效，由 SkillSystem 在技能开始时触发

#### Scenario: Buff 应用音效
- **WHEN** Buff 被应用到目标
- **THEN** 系统播放 Buff 应用音效 ID=20401，负面 Buff 播放 ID=20402

### Requirement: 探索系统音效支持
系统 SHALL 为探索系统的交互提供音效。

#### Scenario: 脚步声循环
- **WHEN** 角色移动中
- **THEN** 系统根据地面材质循环播放脚步声（可选，使用 3D 定位）

#### Scenario: 物品捡取音效
- **WHEN** 玩家捡取掉落物品
- **THEN** 系统播放捡取音效 ID=20501

#### Scenario: 宝箱打开音效
- **WHEN** 玩家打开宝箱
- **THEN** 系统播放宝箱打开音效 ID=20502（可循环背景音）

#### Scenario: NPC 对话触发
- **WHEN** 玩家与 NPC 交互
- **THEN** 系统播放对话提示音 ID=20503
