## ADDED Requirements

### Requirement: BGM 播放和停止
系统 SHALL 支持通过 AudioManager.PlayBGM(audioId) 播放指定的背景音乐，并通过 AudioManager.StopBGM(fadeOutTime) 停止当前 BGM。

#### Scenario: 开始播放 BGM
- **WHEN** 场景加载时调用 AudioManager.PlayBGM(10001)
- **THEN** 指定的 BGM 从配置表中加载并开始播放，使用 faceInTime 参数进行淡入

#### Scenario: 停止 BGM
- **WHEN** 调用 AudioManager.StopBGM(0.5f)
- **THEN** 当前 BGM 在 0.5 秒内逐渐淡出，然后停止播放

#### Scenario: BGM 不存在
- **WHEN** 调用 AudioManager.PlayBGM(99999)，但该 ID 不在配置表中
- **THEN** 系统输出错误日志并跳过播放，不中断主流程

### Requirement: BGM 淡入淡出
系统 SHALL 在 BGM 切换时自动执行淡出→切换→淡入流程，避免音乐突变。

#### Scenario: 平滑切换 BGM
- **WHEN** 通过 AudioManager.PlayBGM(newId, fadeInTime=0.5f) 切换新 BGM，此时有正在播放的 BGM
- **THEN** 系统依次执行：(1) 淡出旧 BGM (2) 停止旧 BGM (3) 加载新 BGM (4) 淡入新 BGM，总时长约 1 秒

#### Scenario: 立即切换 BGM
- **WHEN** 调用 AudioManager.PlayBGM(newId, isImmediate=true)
- **THEN** 系统跳过淡出，立即停止旧 BGM 并启动新 BGM（用于特殊场景如 Debug）

### Requirement: BGM 队列管理
系统 SHALL 支持将多个 BGM 播放请求加入队列，按顺序执行。

#### Scenario: 连续请求两个 BGM
- **WHEN** 在 BGM 正在切换时（淡出中），再调用 PlayBGM 请求另一个 BGM
- **THEN** 系统将第二个请求加入队列，等第一个切换完成后自动播放第二个

#### Scenario: 队列完成回调
- **WHEN** 完成队列中最后一个 BGM 的切换
- **THEN** 系统触发配置的 OnComplete 回调

### Requirement: BGM 循环播放
系统 SHALL 根据配置表中的 IsLoop 字段自动设置 BGM 循环播放。

#### Scenario: 循环 BGM 配置
- **WHEN** AudioClipTable 中某个 BGM 的 IsLoop=true
- **THEN** 该 BGM 播放完后自动重新开始，无需手动干预

#### Scenario: 非循环 BGM 配置
- **WHEN** 某个 BGM 的 IsLoop=false，且播放完毕
- **THEN** BGM 停止播放，系统触发 OnComplete 回调

### Requirement: BGM 暂停和恢复
系统 SHALL 支持 BGM 的暂停和恢复操作。

#### Scenario: 暂停 BGM
- **WHEN** 调用 AudioManager.PauseBGM()
- **THEN** 当前 BGM 暂停，音量不变，后续可恢复

#### Scenario: 恢复 BGM
- **WHEN** 调用 AudioManager.ResumeBGM()
- **THEN** 之前暂停的 BGM 继续播放，从暂停位置开始
