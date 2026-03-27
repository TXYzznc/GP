# Implementation Plan

- [x] 1. 更新输入管理器


  - [x] 1.1 添加滚轮输入支持


    - 在 PlayerInputManager 中添加 ScrollDelta 属性
    - 在 Update 中捕获 Input.mouseScrollDelta.y
    - _Requirements: 10.4_
  - [ ]* 1.2 编写输入管理器属性测试
    - **Property 17: Input Capture Correctness**
    - **Validates: Requirements 10.1, 10.2**

- [x] 2. 重构第三人称摄像机系统


  - [x] 2.1 创建新的 ThirdPersonCamera 脚本框架


    - 创建 Assets/AAAGame/Scripts/Game/Camera/ThirdPersonCamera.cs
    - 定义所有配置参数（轨道、跟随、缩放、FOV、防遮挡）
    - 实现基础的 Awake 和 Start 初始化
    - _Requirements: 1.1, 1.2, 2.1, 3.1, 4.1_
  - [x] 2.2 实现轨道控制逻辑

    - 实现 HandleOrbitInput 方法处理鼠标输入
    - 使用 Vector2.SmoothDamp 实现输入阻尼
    - 实现俯仰角限制（Clamp）
    - 计算摄像机位置基于 yaw、pitch 和 distance
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  - [ ]* 2.3 编写轨道控制属性测试
    - **Property 1: Orbit Rotation Proportionality**
    - **Property 2: Pitch Clamping**
    - **Property 3: Orbit Damping Decay**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4**
  - [x] 2.4 实现平滑跟随逻辑

    - 实现 HandleFollow 方法
    - 使用 Vector3.SmoothDamp 更新锚点位置
    - 确保摄像机相对于锚点的偏移正确
    - _Requirements: 2.1, 2.2, 2.3_
  - [ ]* 2.5 编写跟随逻辑属性测试
    - **Property 4: Follow Position Convergence**
    - **Validates: Requirements 2.1, 2.2, 2.3**
  - [x] 2.6 实现缩放控制逻辑

    - 实现 HandleZoom 方法处理滚轮输入
    - 使用 Mathf.SmoothDamp 平滑距离变化
    - 实现距离限制（Clamp）
    - _Requirements: 3.1, 3.2, 3.3, 3.4_
  - [ ]* 2.7 编写缩放控制属性测试
    - **Property 5: Zoom Distance Clamping**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4**
  - [x] 2.8 实现动态 FOV

    - 实现 HandleDynamicFOV 方法
    - 根据冲刺状态平滑调整 FOV
    - 提供 SetSprintMode 公共方法
    - _Requirements: 3.5, 3.6_
  - [ ]* 2.9 编写动态 FOV 属性测试
    - **Property 6: FOV Sprint Mode**
    - **Validates: Requirements 3.5, 3.6**
  - [x] 2.10 实现智能防遮挡系统

    - 实现 HandleOcclusion 方法
    - 使用 Physics.SphereCast 检测障碍物
    - 检测到障碍时立即拉近摄像机
    - 障碍消失时平滑恢复距离
    - _Requirements: 4.1, 4.2, 4.3, 4.4_
  - [ ]* 2.11 编写防遮挡系统属性测试
    - **Property 7: Occlusion Detection Correctness**
    - **Property 8: Occlusion Recovery Smoothness**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4**
  - [x] 2.12 实现公共接口

    - 实现 Forward 和 Right 属性（水平方向向量）
    - 实现 SetTarget、ResetCamera、GetCamera 方法
    - _Requirements: 5.1_

- [x] 3. Checkpoint - 确保摄像机系统测试通过

  - Ensure all tests pass, ask the user if questions arise.

- [-] 4. 重构角色控制器

  - [x] 4.1 更新 PlayerController 基础结构


    - 添加 ThirdPersonCamera 引用
    - 添加转身平滑参数 turnSmoothTime
    - 移除旧的鼠标旋转逻辑
    - _Requirements: 5.1, 6.1_
  - [x] 4.2 实现基于摄像机的方向计算

    - 实现 CalculateCameraRelativeDirection 方法
    - 获取摄像机水平前向和右向
    - 计算 TargetDirection = input.y × cameraForward + input.x × cameraRight
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  - [ ]* 4.3 编写方向计算属性测试
    - **Property 9: Camera-Relative Direction Calculation**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5**
  - [x] 4.4 实现移动与旋转分离

    - 修改 HandleMovement 立即应用速度
    - 实现 HandleModelRotation 方法
    - 使用 Mathf.SmoothDampAngle 平滑转身
    - _Requirements: 6.1, 6.2, 6.3_
  - [ ]* 4.5 编写移动旋转分离属性测试
    - **Property 10: Immediate Movement Response**
    - **Property 11: Smooth Model Rotation**
    - **Validates: Requirements 6.1, 6.2, 6.3**

  - [ ] 4.6 优化物理手感
    - 调整加速度参数实现 0.2-0.3 秒起步
    - 实现滑行减速效果
    - 实现斜坡速度投影
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  - [ ]* 4.7 编写物理手感属性测试
    - **Property 12: Acceleration Time Bounds**
    - **Property 13: Deceleration Continuity**
    - **Property 14: Slope Velocity Projection**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4**
  - [x] 4.8 更新状态系统

    - 保持现有状态转换逻辑
    - 添加与摄像机的 FOV 联动
    - 确保 Shift 切换逻辑正确
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_
  - [ ]* 4.9 编写状态系统属性测试
    - **Property 15: State Transition Correctness**
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.5**
  - [x] 4.10 更新动画系统集成

    - 保持现有 Blend Tree 参数映射
    - 确保状态变化正确更新动画参数
    - _Requirements: 9.1, 9.2, 9.3_
  - [ ]* 4.11 编写动画参数属性测试
    - **Property 16: Animation Parameter Mapping**
    - **Validates: Requirements 9.1, 9.2, 9.3**

- [x] 5. Checkpoint - 确保角色控制器测试通过

  - Ensure all tests pass, ask the user if questions arise.

- [-] 6. 系统集成与清理

  - [x] 6.1 删除旧的 ThirdPersonCameraRig 脚本


    - 备份旧脚本到注释或单独文件
    - 从场景中移除旧组件引用
    - _Requirements: 1.1, 2.1_


  - [ ] 6.2 更新场景配置
    - 在摄像机对象上添加新的 ThirdPersonCamera 组件
    - 配置所有参数（距离、灵敏度、遮挡层等）
    - 设置 PlayerController 的 cameraRig 引用
    - _Requirements: 1.1, 2.1, 4.1_
  - [x] 6.3 更新 PlayerCharacterManager

    - 确保角色生成时正确设置摄像机目标
    - 确保场景切换时摄像机状态正确重置
    - _Requirements: 2.1_

- [x] 7. Final Checkpoint - 确保所有测试通过


  - Ensure all tests pass, ask the user if questions arise.
