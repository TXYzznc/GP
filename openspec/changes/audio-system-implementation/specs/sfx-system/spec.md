## ADDED Requirements

### Requirement: SFX 即时播放
系统 SHALL 支持通过 AudioManager.PlaySFX(audioId) 立即播放指定的音效。

#### Scenario: 播放 UI 点击音效
- **WHEN** 用户点击按钮时调用 AudioManager.PlaySFX(20000)
- **THEN** 该音效从对象池中取得并立即播放，无延迟

#### Scenario: 调整 SFX 音量
- **WHEN** 调用 AudioManager.PlaySFX(audioId, volumeScale=0.5f)
- **THEN** 音效以默认音量的 50% 播放

#### Scenario: SFX 播放延迟 < 50ms
- **WHEN** 调用 PlaySFX 到实际听到声音
- **THEN** 延迟时间小于 50 毫秒

### Requirement: SFX 延迟播放
系统 SHALL 支持通过 AudioManager.PlaySFXDelayed(audioId, delay) 延迟播放音效。

#### Scenario: 延迟播放动作音效
- **WHEN** 调用 AudioManager.PlaySFXDelayed(20201, 0.5f)（角色攻击动作的 0.5 秒处）
- **THEN** 系统在 0.5 秒后播放该音效

#### Scenario: 延迟播放取消
- **WHEN** 场景卸载或对象销毁时有未执行的延迟播放请求
- **THEN** 系统自动取消该请求，不发生异常

### Requirement: SFX 对象池管理
系统 SHALL 使用对象池缓存 AudioSource，提高 SFX 播放性能。

#### Scenario: 频繁 UI 音效播放
- **WHEN** 用户快速点击多个按钮，触发 10 次 PlaySFX(20000)
- **THEN** 系统复用对象池中的 AudioSource，不产生性能卡顿

#### Scenario: 对象池初始化大小
- **WHEN** 系统启动时为每个 SFX ID 创建对象池
- **THEN** 对象池的初始大小根据 SFX 使用频率设置（常用音效 10 个，低频 3 个）

#### Scenario: 自动扩展对象池
- **WHEN** 对象池中的 AudioSource 已全部使用，仍有新的播放请求
- **THEN** 系统自动创建新的 AudioSource 加入池中

### Requirement: SFX 停止控制
系统 SHALL 支持停止指定音效或全部音效。

#### Scenario: 停止单个 SFX
- **WHEN** 调用 AudioManager.StopSFX(20301)
- **THEN** 该类型的所有正在播放的音效立即停止

#### Scenario: 停止所有 SFX
- **WHEN** 调用 AudioManager.StopAllSFX()
- **THEN** 所有 SFX 音源的播放立即停止

### Requirement: 3D 音效定位（可选）
系统 SHALL 支持播放位置相关的 3D 音效。

#### Scenario: 3D 空间音效
- **WHEN** 调用 AudioManager.PlaySFX3D(audioId, worldPosition)
- **THEN** 系统将 AudioSource 放置在指定世界坐标，根据距离和方向产生空间感

#### Scenario: 3D 音效距离衰减
- **WHEN** 玩家远离 3D 音效发出位置
- **THEN** 音效音量随距离增加而降低，到达 MaxDistance 时无法听到

#### Scenario: 非 3D 音效定位请求
- **WHEN** 调用 PlaySFX3D 传入不支持 3D 的音效 ID
- **THEN** 系统输出警告日志，按 2D 模式播放
