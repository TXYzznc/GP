## ADDED Requirements

### Requirement: AudioClipTable 配置表结构
系统 SHALL 定义并实现 AudioClipTable 配置表，包含所有音效的元数据。

#### Scenario: 配置表字段定义
- **WHEN** 创建 AudioClipTable.xlsx
- **THEN** 包含以下字段：
  - Id (int): 唯一标识符
  - AudioName (string): 可读的音效名称
  - AudioType (int): 1=BGM, 2=SFX, 3=环境音, 4=语音
  - ResourcePath (string): 相对于 Assets/AAAGame/Resources/Audio/ 的路径
  - Duration (float): 音效时长（秒）
  - Volume (float): 默认音量 (0-1)
  - Pitch (float): 音调 (0.5-1.5)
  - IsLoop (bool): 是否循环播放
  - Is3D (bool): 是否 3D 定位音效
  - MaxDistance (float): 3D 音效最大距离
  - Priority (int): 优先级 (0-256)
  - FadeInTime (float): 淡入时长（仅 BGM）
  - FadeOutTime (float): 淡出时长（仅 BGM）
  - Tag (string): 分类标签，便于查询

#### Scenario: 配置表数据行示例
- **WHEN** 配置表中第一个 BGM 条目
- **THEN** 数据行包含：10001, BGM_MainMenu, 1, Audio/BGM/MainMenu, 120.5, 0.8, 1.0, 1, 0, 0, 128, 0.5, 0.5, menu

#### Scenario: 生成 DRAudioClipTable 数据类
- **WHEN** 执行 DataTableGenerator（菜单：GameFramework → DataTable → Generate）
- **THEN** 系统自动生成 Assets/AAAGame/Scripts/DataTable/AudioClipTable.cs

### Requirement: 音效 ID 标准化范围
系统 SHALL 定义标准的音效 ID 范围，避免冲突。

#### Scenario: BGM ID 范围 (10000-10999)
- **WHEN** 新增 BGM 音效
- **THEN** 分配 ID 范围：
  - 10001-10099: 菜单/全局 BGM
  - 10100-10199: 副本 BGM
  - 10200-10299: 战斗 BGM
  - 10300-10399: 特殊场景 BGM

#### Scenario: SFX ID 范围 (20000-29999)
- **WHEN** 新增 SFX 音效
- **THEN** 分配 ID 范围：
  - 20000-20099: UI 音效
  - 20100-20199: 战斗反馈音
  - 20200-20299: 探索交互音
  - 20300-20399: 环境音
  - 20400-20499: 成就/通知音

#### Scenario: 语音 ID 范围 (30000-39999)
- **WHEN** 后续实现角色语音系统
- **THEN** 预留 ID 范围 30000-39999，格式如 30000-30999 为男主角语音

### Requirement: 配置表字段验证
系统 SHALL 在加载配置表时验证数据完整性。

#### Scenario: 必填字段检查
- **WHEN** 加载 AudioClipTable 时
- **THEN** 检查所有必填字段不为空：Id、AudioName、AudioType、ResourcePath

#### Scenario: 类型范围检查
- **WHEN** 检查 AudioType 字段
- **THEN** 必须是 1、2、3 或 4，否则输出错误日志

#### Scenario: 音量范围检查
- **WHEN** 检查 Volume 和 Pitch 字段
- **THEN** Volume 必须在 0-1 之间，Pitch 必须在 0.5-1.5 之间

#### Scenario: 资源路径存在性检查
- **WHEN** 加载时检查 ResourcePath 指向的文件是否存在
- **THEN** 如果文件不存在输出警告，但不中断加载

### Requirement: 配置表读取 API
系统 SHALL 提供方便的 API 读取配置表数据。

#### Scenario: 按 ID 查询音效配置
- **WHEN** 调用 AudioResourceManager.GetConfig(10001)
- **THEN** 返回对应的 DRAudioClipTable 数据行

#### Scenario: 按标签查询音效集合
- **WHEN** 调用 GetConfigsByTag("menu")
- **THEN** 返回所有 Tag="menu" 的配置行数组

#### Scenario: 获取音效时长
- **WHEN** 调用 GetDuration(20301)
- **THEN** 返回该音效的 Duration 值，用于计算延迟播放

### Requirement: 配置表更新工作流
系统 SHALL 支持配置表的热更新和版本控制。

#### Scenario: Excel 修改后重新生成
- **WHEN** 修改 AudioClipTable.xlsx 并保存
- **THEN** 开发者执行 DataTableGenerator，自动更新 .cs 和 .bytes 文件

#### Scenario: 配置表版本检查
- **WHEN** 启动游戏时
- **THEN** 系统检查配置表版本号，如有更新输出日志提示

#### Scenario: 配置表数据一致性
- **WHEN** 运行时访问配置表
- **THEN** 系统保证数据与最新的 .xlsx 文件一致

### Requirement: 配置表性能优化
系统 SHALL 对配置表的访问进行缓存和索引优化。

#### Scenario: 配置表加载缓存
- **WHEN** 首次启动或重新加载配置表
- **THEN** 系统将所有行加载到内存字典中，支持 O(1) 查询

#### Scenario: 按 Tag 建立索引
- **WHEN** 初始化时处理配置表
- **THEN** 系统构建 Tag→ID 的反向索引，快速查询相同分类的音效

#### Scenario: 延迟初始化
- **WHEN** 某个音效 ID 首次被请求时
- **THEN** 系统检查配置表中该条目是否存在，避免全表扫描
