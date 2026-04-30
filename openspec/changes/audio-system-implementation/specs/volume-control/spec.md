## ADDED Requirements

### Requirement: 独立音轨音量控制
系统 SHALL 支持对 4 个独立音轨的音量控制：BGM、SFX、环境音、语音。

#### Scenario: 设置 BGM 音量
- **WHEN** 用户通过设置菜单调整 BGM 音量滑块，调用 AudioManager.SetTrackVolume(AudioTrackType.BGM, 0.6f)
- **THEN** 所有 BGM 音源的音量立即更新为主音量 × 0.6

#### Scenario: 获取当前音轨音量
- **WHEN** 调用 AudioManager.GetTrackVolume(AudioTrackType.SFX)
- **THEN** 返回该音轨的当前音量值（0-1）

#### Scenario: 独立调整各音轨
- **WHEN** 用户将 BGM 设为 0.8，SFX 设为 1.0，环境音设为 0.5，语音设为 0.9
- **THEN** 各类型音效按对应倍数播放，互不干扰

### Requirement: 全局主音量控制
系统 SHALL 支持全局音量控制，对所有音轨产生乘法效果。

#### Scenario: 设置主音量
- **WHEN** 调用 AudioManager.SetMasterVolume(0.5f)
- **THEN** 所有音效的最终音量 = 主音量 × 音轨音量，即所有音效降低到 50%

#### Scenario: 主音量和音轨音量叠加
- **WHEN** 主音量=0.8，BGM 音轨=0.5，某 BGM 默认音量=0.8
- **THEN** 该 BGM 最终音量 = 0.8 × 0.5 × 0.8 = 0.32

### Requirement: 音量持久化存储
系统 SHALL 将用户的音量设置存储到本地，游戏重新启动时自动恢复。

#### Scenario: 保存音量设置
- **WHEN** 用户调整音量后退出游戏
- **THEN** 所有音轨和主音量设置保存到 PlayerPrefs

#### Scenario: 恢复音量设置
- **WHEN** 游戏重新启动并初始化 AudioManager
- **THEN** 系统从 PlayerPrefs 读取上次保存的音量设置

#### Scenario: 首次启动默认音量
- **WHEN** 首次运行游戏，PlayerPrefs 中无音量数据
- **THEN** 使用预定义的默认值（BGM=0.8, SFX=0.9, 环境音=0.6, 语音=1.0）

### Requirement: 全局静音功能
系统 SHALL 支持一键全静音和恢复。

#### Scenario: 启用全局静音
- **WHEN** 调用 AudioManager.SetMute(true)
- **THEN** 所有音效音量立即降至 0，不改变各音轨的设置值

#### Scenario: 取消全局静音
- **WHEN** 调用 AudioManager.SetMute(false)
- **THEN** 所有音效音量恢复到静音前的状态

#### Scenario: 静音状态持久化
- **WHEN** 用户启用静音后退出游戏
- **THEN** 静音状态保存到 PlayerPrefs，重启后保持该状态

### Requirement: 音量变化的平滑过渡
系统 SHALL 在音量调整时避免突变，使用逐帧插值平滑过渡。

#### Scenario: 音量平滑变化
- **WHEN** 从音量 1.0 调整到 0.5
- **THEN** 音量在约 0.2 秒内逐帧线性插值过渡到新值，不产生突变感

#### Scenario: BGM 淡入淡出时的音量计算
- **WHEN** BGM 执行淡出过程（3 秒），同时用户调整了 BGM 音轨音量
- **THEN** 最终音量 = 淡出曲线 × 新的音轨音量，保持优先级
