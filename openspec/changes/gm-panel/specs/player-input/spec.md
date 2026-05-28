## ADDED Requirements

### Requirement: GM 面板快捷键
PlayerInputManager SHALL 新增 GMPanelToggleTriggered 属性，每帧检测 C 键按下事件（Input.GetKeyDown(KeyCode.C)），供 GMPanelUI 轮询使用。

#### Scenario: C 键按下触发
- **WHEN** 用户在当前帧按下 C 键
- **THEN** GMPanelToggleTriggered 在该帧返回 true
