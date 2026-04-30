## ADDED Requirements

### Requirement: 音频资源配置表驱动
系统 SHALL 通过 AudioClipTable 配置表统一管理所有音频资源的元数据。

#### Scenario: 配置表字段定义
- **WHEN** AudioClipTable.xlsx 包含所有音效的配置
- **THEN** 字段包括：Id、AudioName、AudioType、ResourcePath、Duration、Volume、Pitch、IsLoop、Is3D、MaxDistance、Priority、FadeInTime、FadeOutTime、Tag

#### Scenario: 从配置表读取音效信息
- **WHEN** 调用 AudioManager.PlayBGM(10001)
- **THEN** 系统从配置表查询 ID=10001 的记录，获取 ResourcePath、默认音量等参数

#### Scenario: 音效 ID 范围划分
- **WHEN** 新增一个 BGM 音效
- **THEN** 系统分配 ID 范围 10000-10999（BGM）内的 ID，避免冲突

### Requirement: 音频资源加载
系统 SHALL 支持按需加载音频资源。

#### Scenario: BGM 流式加载
- **WHEN** 调用 AudioManager.PlayBGM(10001)
- **THEN** 系统通过 Resources.LoadAsync 流式加载 BGM，不将全部数据存储在内存中

#### Scenario: SFX 预加载
- **WHEN** 系统初始化时准备 SFX 对象池
- **THEN** 系统预加载该 SFX 的 AudioClip 并存储到缓存中

#### Scenario: 缓存加载的资源
- **WHEN** 再次请求同一个 SFX ID
- **THEN** 系统从缓存返回 AudioClip，不重复加载

### Requirement: 音频资源卸载管理
系统 SHALL 支持主动卸载不再使用的音频资源。

#### Scenario: 场景切换时卸载音效
- **WHEN** 场景从战斗场景切换到探索场景
- **THEN** 系统卸载战斗相关的 SFX，释放内存空间

#### Scenario: 全量卸载
- **WHEN** 调用 AudioManager 的 UnloadAllAudioClips()
- **THEN** 系统卸载所有缓存的 AudioClip（正在播放的除外）

#### Scenario: 检查资源卸载后的行为
- **WHEN** 卸载某 SFX 后，再次请求播放该 SFX
- **THEN** 系统重新加载该 SFX，不产生异常

### Requirement: 音频资源内存优化
系统 SHALL 在内存压力下自动优化资源占用。

#### Scenario: 内存占用统计
- **WHEN** 运行游戏并监测内存使用
- **THEN** BGM 流式加载占用 < 5MB，SFX 缓存总量 < 50MB

#### Scenario: 动态调整对象池大小
- **WHEN** 检测到频繁播放某个 SFX
- **THEN** 系统自动增加该 SFX 的对象池大小

#### Scenario: 预加载优先级
- **WHEN** 场景加载前调用 PreloadAudioClip(audioId)
- **THEN** 系统提前加载该音效，使用时无延迟

### Requirement: 音频文件异常处理
系统 SHALL 对缺失或损坏的音频文件进行容错处理。

#### Scenario: 音效文件不存在
- **WHEN** 配置表指定的 ResourcePath 在项目中不存在
- **THEN** 系统输出错误日志并返回 null，不中断游戏主流程

#### Scenario: 无效的配置数据
- **WHEN** 配置表中某行数据格式错误或字段缺失
- **THEN** 系统在加载时检测并输出警告，使用默认值或跳过该条目

#### Scenario: 加载失败的降级方案
- **WHEN** 某个 BGM 加载失败
- **THEN** 系统继续播放当前 BGM（或保持静音），等待手动切换
