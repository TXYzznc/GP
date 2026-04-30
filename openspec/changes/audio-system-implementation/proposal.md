## Why

当前项目缺乏统一的音乐和音效管理系统，导致音频功能散落在各个模块中，难以维护和扩展。随着游戏内容的增加（6个不同风格的副本、战斗系统、UI交互等），需要一个专业的音频管理层来支持复杂的场景切换、动态BGM管理、3D音效定位和独立音轨控制。

## What Changes

- 新增独立的 **AudioManager** 音频管理系统，统一管理 BGM 和 SFX
- 实现 **BGM 淡入淡出** 机制，支持平滑的场景音乐切换
- 实现 **SFX 对象池** 缓存机制，优化音效播放性能
- 支持 **独立音轨控制**（BGM、SFX、环境音、语音）及音量持久化
- 支持 **3D 音效定位**，实现空间音效播放
- 整合 **DataTable 配置表**，所有音效参数可配置化
- 与 **GF.Event** 和 **UI 系统** 完整集成
- 提供 **高性能 API**，SFX 播放延迟 < 50ms

## Capabilities

### New Capabilities
- `bgm-management`: BGM 播放、切换、淡入淡出、循环队列管理
- `sfx-system`: SFX 播放、对象池缓存、延迟播放、3D 定位
- `volume-control`: 独立音轨音量管理、持久化存储、全局控制
- `audio-resource-manager`: 音频资源加载、缓存策略、内存优化
- `audio-integration`: 与 Procedure、Event、UI 系统的集成方案
- `audio-configuration`: AudioClipTable 配置表设计和数据结构

### Modified Capabilities
_无现有功能被修改，这是新增独立系统_

## Impact

- 新增代码：`Assets/AAAGame/Scripts/Game/Audio/` 目录（约 1500 行）
- 新增配置表：`AudioClipTable.xlsx`
- 新增资源目录：`Assets/AAAGame/Resources/Audio/`
- 与现有系统集成点：
  - `GF.Event` 事件系统（Procedure 切换、战斗事件）
  - `UI 系统`（UIForm 打开关闭回调）
  - `Procedure 系统`（场景转换时的 BGM 管理）
- 无破坏性改动，完全向后兼容
