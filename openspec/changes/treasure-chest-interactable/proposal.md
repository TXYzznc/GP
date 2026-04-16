## Why

探索场景中需要宝箱类可交互对象，玩家与之交互时播放宝箱开箱动画并弹出宝箱界面，支持未开启/开启两种状态区分。当前交互系统已有 `InteractableBase` 基类，需要在此基础上扩展宝箱专属行为。

## What Changes

- 新增 `TreasureChestInteractable` 脚本，继承 `InteractableBase`
- 宝箱维护 `Locked`/`Opened` 两种状态，已开启的宝箱可以再次交互，但不会播放动画，只会弹出宝箱界面（类似背包界面，等后续实现）
- 交互触发时：播放宝箱自身 Animator 动画（可选）→ 等待动画完成 → 打开宝箱界面（占位）→ 设置宝箱为 Opened 状态
- 交互动画（玩家侧）不播放（`InteractAnimIndex = -1`），宝箱侧用自身 Animator 播放

## Capabilities

### New Capabilities
- `treasure-chest-interactable`: 宝箱交互脚本，支持状态管理、动画播放、宝箱界面触发，可扩展解锁条件

### Modified Capabilities

## Impact

- 新增文件：`Assets/AAAGame/Scripts/Game/Interact/TreasureChestInteractable.cs`
- 依赖：`InteractableBase`、`IInteractable`、`Animator`（可选）、`UniTask`
- UI 部分目前为占位逻辑（UI 尚未制作），后续补全时只需填充 `OpenChestUI` 方法
