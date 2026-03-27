# Requirements Document

## Introduction

本文档定义了第三人称角色控制器的完整需求规范。目标是实现一个"响应迅速、视觉平滑、永不穿模"的第三人称角色控制系统。该系统包含两大核心模块：摄像机系统（Camera System）和角色移动系统（Character Locomotion）。

核心设计理念：
- 角色移动方向完全依赖于当前摄像机的视角
- 角色模型的朝向与移动方向独立，通过平滑过渡实现自然转身
- 摄像机作为玩家的眼睛，必须稳定且智能

## Glossary

- **Third_Person_Camera**: 第三人称摄像机系统，负责轨道控制、跟随、缩放和防遮挡
- **Character_Controller**: 角色控制器，负责移动、旋转和状态管理
- **Orbit_Control**: 轨道控制，摄像机绕角色旋转的控制逻辑
- **Occlusion_System**: 遮挡检测系统，防止摄像机穿透障碍物
- **Input_Manager**: 输入管理器，处理鼠标和键盘输入
- **Target_Direction**: 目标方向，基于摄像机视角计算的移动方向
- **Turn_Smooth_Time**: 转身平滑时间，角色模型旋转的插值时间
- **Camera_Damping**: 摄像机阻尼，鼠标停止后的惯性滑行效果
- **Slope_Projection**: 斜坡投影，将速度向量投射到斜坡平面上

## Requirements

### Requirement 1: 摄像机轨道控制

**User Story:** As a player, I want to control the camera rotation around my character using the mouse, so that I can freely observe the game world from different angles.

#### Acceptance Criteria

1. WHEN the player moves the mouse horizontally THEN the Third_Person_Camera SHALL rotate around the character's Y-axis (yaw) proportionally to the mouse X-axis input
2. WHEN the player moves the mouse vertically THEN the Third_Person_Camera SHALL rotate around its own X-axis (pitch) proportionally to the mouse Y-axis input
3. WHEN the camera pitch angle reaches the configured minimum or maximum limit THEN the Third_Person_Camera SHALL clamp the pitch value and prevent further rotation in that direction
4. WHEN the player stops moving the mouse THEN the Third_Person_Camera SHALL continue rotating with a small inertia effect using smooth damping before coming to a complete stop

### Requirement 2: 摄像机平滑跟随

**User Story:** As a player, I want the camera to smoothly follow my character, so that the visual experience feels natural and not jarring.

#### Acceptance Criteria

1. WHEN the character moves THEN the Third_Person_Camera SHALL update its position with a configurable lag using Vector3.SmoothDamp
2. WHEN the character stops moving THEN the Third_Person_Camera SHALL smoothly settle to its target position without overshooting
3. WHEN the camera position is updated THEN the Third_Person_Camera SHALL maintain the configured offset relative to the character's position

### Requirement 3: 摄像机缩放控制

**User Story:** As a player, I want to zoom the camera in and out using the mouse scroll wheel, so that I can adjust my view distance based on the situation.

#### Acceptance Criteria

1. WHEN the player scrolls the mouse wheel forward THEN the Third_Person_Camera SHALL decrease the distance to the character
2. WHEN the player scrolls the mouse wheel backward THEN the Third_Person_Camera SHALL increase the distance from the character
3. WHEN the camera distance reaches the configured minimum distance THEN the Third_Person_Camera SHALL prevent further zoom-in
4. WHEN the camera distance reaches the configured maximum distance THEN the Third_Person_Camera SHALL prevent further zoom-out
5. WHEN the character enters FastRun state THEN the Third_Person_Camera SHALL gradually increase the field of view to enhance the sense of speed
6. WHEN the character exits FastRun state THEN the Third_Person_Camera SHALL gradually restore the field of view to the default value

### Requirement 4: 摄像机智能防遮挡

**User Story:** As a player, I want the camera to automatically avoid obstacles between it and my character, so that my view is never blocked by walls or other objects.

#### Acceptance Criteria

1. WHEN an obstacle exists between the character and the camera's ideal position THEN the Occlusion_System SHALL detect the obstacle using SphereCast from the character's head position
2. WHEN an obstacle is detected THEN the Third_Person_Camera SHALL immediately move to a position in front of the obstacle
3. WHEN the obstacle is no longer between the character and camera THEN the Third_Person_Camera SHALL gradually restore to the player-configured distance using smooth interpolation
4. WHEN performing occlusion detection THEN the Occlusion_System SHALL only consider objects on the configured occlusion layer mask

### Requirement 5: 基于摄像机的移动方向计算

**User Story:** As a player, I want my character to move relative to the camera's facing direction, so that pressing forward always moves the character away from the camera.

#### Acceptance Criteria

1. WHEN the player provides movement input THEN the Character_Controller SHALL calculate the Target_Direction based on the camera's horizontal forward vector (ignoring Y-axis tilt)
2. WHEN calculating Target_Direction THEN the Character_Controller SHALL use the formula: Target_Direction = input.y × cameraForward + input.x × cameraRight
3. WHEN the player presses W THEN the character SHALL move away from the camera (forward relative to camera view)
4. WHEN the player presses S THEN the character SHALL move toward the camera (backward relative to camera view)
5. WHEN the player presses A or D THEN the character SHALL move perpendicular to the camera's forward direction (left or right)

### Requirement 6: 移动与旋转分离

**User Story:** As a player, I want my character to start moving immediately when I press a direction key, while the character model smoothly rotates to face the movement direction, so that the controls feel responsive yet visually smooth.

#### Acceptance Criteria

1. WHEN movement input is received THEN the Character_Controller SHALL immediately apply velocity in the Target_Direction without waiting for the model to rotate
2. WHEN the character is moving THEN the Character_Controller SHALL smoothly rotate the character model to face the Target_Direction using Mathf.SmoothDampAngle
3. WHEN the player rapidly changes direction (e.g., W to S) THEN the Character_Controller SHALL rotate the character model through a smooth arc rather than instantly flipping 180 degrees
4. WHEN configuring turn smoothing THEN the Turn_Smooth_Time SHALL be approximately 0.1 seconds to balance responsiveness and visual smoothness

### Requirement 7: 物理手感与加减速

**User Story:** As a player, I want the character movement to have realistic acceleration and deceleration, so that the controls feel weighty and satisfying.

#### Acceptance Criteria

1. WHEN the player starts moving from idle THEN the Character_Controller SHALL accelerate from zero to maximum speed over approximately 0.2 to 0.3 seconds
2. WHEN the player releases movement keys THEN the Character_Controller SHALL decelerate with a sliding effect rather than stopping instantly
3. WHEN the character moves on a slope THEN the Character_Controller SHALL project the velocity vector onto the slope plane to prevent floating or slow movement
4. WHEN the character is grounded THEN the Character_Controller SHALL apply appropriate friction to the horizontal velocity

### Requirement 8: 角色状态系统

**User Story:** As a player, I want to switch between different movement speeds (walk, slow run, fast run), so that I can control my character's pace based on the situation.

#### Acceptance Criteria

1. WHEN the player double-taps the forward key within the configured time window THEN the Character_Controller SHALL transition from Walk state to SlowRun state
2. WHEN the player presses Shift while in SlowRun state THEN the Character_Controller SHALL transition to FastRun state
3. WHEN the player presses Shift while in FastRun state THEN the Character_Controller SHALL transition back to SlowRun state with speed set to slow run maximum
4. WHEN the player stops providing movement input THEN the Character_Controller SHALL transition to Idle state
5. WHEN transitioning between states THEN the Character_Controller SHALL update the maximum speed according to the new state

### Requirement 9: 动画系统集成

**User Story:** As a player, I want the character animations to smoothly blend based on movement speed and state, so that the visual feedback matches my inputs.

#### Acceptance Criteria

1. WHEN the character state or speed changes THEN the Character_Controller SHALL update the Animator's Speed parameter to drive the Blend Tree
2. WHEN calculating the Blend Tree speed value THEN the Character_Controller SHALL map the current state and velocity to the appropriate animation threshold range
3. WHEN the character is in Idle state THEN the Character_Controller SHALL set the Speed parameter to the Idle animation threshold

### Requirement 10: 输入系统

**User Story:** As a player, I want responsive and configurable input controls, so that I can customize the sensitivity to my preference.

#### Acceptance Criteria

1. WHEN the mouse is locked THEN the Input_Manager SHALL capture mouse delta values and apply configured sensitivity multipliers
2. WHEN the mouse is not locked THEN the Input_Manager SHALL return zero for mouse delta values
3. WHEN the player presses Tab THEN the Input_Manager SHALL toggle the mouse lock state
4. WHEN the player scrolls the mouse wheel THEN the Input_Manager SHALL capture the scroll delta value for camera zoom control
