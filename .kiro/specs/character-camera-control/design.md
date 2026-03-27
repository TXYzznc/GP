# Design Document: Character Control and Camera System

## Overview

This design document outlines the architecture for a polished third-person character control and camera system. The system consists of three main components:

1. **Enhanced PlayerController** - Handles character movement with smooth acceleration/deceleration and camera-relative input
2. **ThirdPersonCameraRig** - Manages camera following, rotation, collision, and dynamic adjustments
3. **PlayerInputManager** - Provides normalized input from various sources (keyboard, mouse, gamepad)

The design emphasizes smooth, natural-feeling controls through the use of interpolation, damping, and carefully tuned parameters. All systems are designed to be configurable through Unity Inspector for easy iteration and tuning.

## Architecture

### System Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Player Input Layer                       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           PlayerInputManager (Singleton)                │ │
│  │  - Keyboard/Mouse Input                                 │ │
│  │  - Gamepad Input                                        │ │
│  │  - Input Normalization & Dead Zones                     │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Character Control Layer                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         Enhanced PlayerController                       │ │
│  │  - Camera-Relative Movement                             │ │
│  │  - Smooth Acceleration/Deceleration                     │ │
│  │  - Smooth Rotation                                      │ │
│  │  - Animation Integration                                │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                     Camera System Layer                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         ThirdPersonCameraRig                            │ │
│  │  - Smooth Following (Damping)                           │ │
│  │  - Dead Zone                                            │ │
│  │  - Look Ahead                                           │ │
│  │  - Player Rotation Control                              │ │
│  │  - Collision Detection & Avoidance                      │ │
│  │  - Occlusion Handling                                   │ │
│  │  - Dynamic FOV                                          │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Component Relationships

- **PlayerInputManager** is a singleton that provides input to PlayerController
- **PlayerController** is attached to the player character GameObject
- **ThirdPersonCameraRig** is a separate GameObject hierarchy that follows the player
- **CameraManager** integrates with ThirdPersonCameraRig for camera activation
- **PlayerCharacterManager** initializes both PlayerController and ThirdPersonCameraRig when spawning characters

## Components and Interfaces

### 1. PlayerInputManager

**Purpose:** Centralized input handling with normalization and dead zone processing.

**Key Responsibilities:**
- Poll input from multiple sources (keyboard, mouse, gamepad)
- Normalize input vectors
- Apply dead zones to analog inputs
- Provide consistent input API to other systems

**Public Interface:**
```csharp
public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance { get; }
    
    // Input properties
    public Vector2 Move { get; }           // Normalized movement input
    public Vector2 Look { get; }           // Camera rotation input
    public bool Sprint { get; }            // Sprint button state
    
    // Configuration
    public float GamepadDeadZone { get; set; }
    public float MouseSensitivity { get; set; }
    public float GamepadSensitivity { get; set; }
}
```

### 2. Enhanced PlayerController

**Purpose:** Handle character movement with smooth acceleration, rotation, and camera-relative input.

**Key Responsibilities:**
- Calculate camera-relative movement direction
- Apply smooth acceleration and deceleration
- Rotate character toward movement direction
- Integrate with animation system
- Handle gravity and ground detection

**Public Interface:**
```csharp
public class PlayerController : MonoBehaviour
{
    // Movement parameters
    [Header("Movement")]
    public float maxMoveSpeed = 5f;
    public float acceleration = 10f;
    public float deceleration = 15f;
    
    [Header("Rotation")]
    public float rotationSpeed = 10f;
    
    // Public methods
    public void SetMoveSpeed(float speed);
    public Vector3 GetPosition();
    public void TeleportTo(Vector3 position);
    public Vector3 GetVelocity();
    public float GetNormalizedSpeed();
}
```

### 3. ThirdPersonCameraRig

**Purpose:** Provide smooth, intelligent camera following with collision avoidance and dynamic adjustments.

**Key Responsibilities:**
- Follow character with damping
- Implement dead zone logic
- Apply look-ahead based on velocity
- Handle player rotation input
- Detect and avoid collisions
- Handle occlusion with transparency
- Adjust FOV based on speed

**Public Interface:**
```csharp
public class ThirdPersonCameraRig : MonoBehaviour
{
    // Target
    public Transform target;
    
    // Following parameters
    [Header("Following")]
    public float followDistance = 5f;
    public float followHeight = 2f;
    public float positionDamping = 0.3f;
    
    [Header("Dead Zone")]
    public float deadZoneRadius = 1f;
    
    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmoothing = 5f;
    
    [Header("Rotation")]
    public float rotationSpeed = 5f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    
    [Header("Collision")]
    public LayerMask collisionLayers;
    public float collisionBuffer = 0.2f;
    
    [Header("Occlusion")]
    public float occlusionFadeSpeed = 5f;
    public float occlusionAlpha = 0.3f;
    
    [Header("Dynamic FOV")]
    public float baseFOV = 60f;
    public float maxFOV = 75f;
    public float fovSpeedThreshold = 8f;
    public float fovTransitionSpeed = 2f;
    
    // Public methods
    public void SetTarget(Transform newTarget);
    public void ResetCamera();
}
```

### 4. CameraOcclusionHandler

**Purpose:** Handle transparency/fading of objects that obstruct the camera view.

**Key Responsibilities:**
- Raycast from camera to character
- Identify obstructing renderers
- Apply fade effects
- Restore original materials

**Public Interface:**
```csharp
public class CameraOcclusionHandler : MonoBehaviour
{
    public Transform target;
    public float fadeSpeed = 5f;
    public float targetAlpha = 0.3f;
    public LayerMask occlusionLayers;
    
    // Internal tracking of faded objects
}
```

## Data Models

### MovementState

Tracks the current movement state of the character:

```csharp
public struct MovementState
{
    public Vector3 velocity;           // Current velocity
    public Vector3 targetVelocity;     // Desired velocity
    public float currentSpeed;         // Current speed magnitude
    public float targetSpeed;          // Target speed magnitude
    public bool isMoving;              // Is character moving
    public Vector3 moveDirection;      // Normalized movement direction
}
```

### CameraState

Tracks the current state of the camera system:

```csharp
public struct CameraState
{
    public Vector3 desiredPosition;    // Target camera position
    public Vector3 currentPosition;    // Actual camera position
    public float currentDistance;      // Current distance from target
    public float desiredDistance;      // Target distance from target
    public Vector2 rotation;           // Current rotation (x=horizontal, y=vertical)
    public float currentFOV;           // Current field of view
    public bool isColliding;           // Is camera colliding with geometry
}
```

### InputState

Normalized input data:

```csharp
public struct InputState
{
    public Vector2 move;               // Movement input (-1 to 1)
    public Vector2 look;               // Look input
    public bool sprint;                // Sprint button
    public float moveInputMagnitude;   // Magnitude of move input
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Acceleration Monotonicity
*For any* character with zero initial velocity and constant forward input, the speed should monotonically increase until reaching maximum speed, never decreasing or oscillating during acceleration.
**Validates: Requirements 1.1, 1.4**

### Property 2: Deceleration Monotonicity
*For any* character with non-zero velocity and zero input, the speed should monotonically decrease to zero, never increasing during deceleration.
**Validates: Requirements 1.2, 1.4**

### Property 3: Camera-Relative Direction Consistency
*For any* movement input and camera orientation, the calculated movement direction should always be perpendicular to the world up vector and aligned with the camera's forward direction projected onto the horizontal plane.
**Validates: Requirements 3.1, 3.2, 3.3**

### Property 4: Rotation Convergence
*For any* target direction and current facing direction, repeated rotation updates should cause the facing direction to converge toward the target direction, with the angular difference decreasing over time.
**Validates: Requirements 2.1, 2.2, 2.5**

### Property 5: Dead Zone Boundary Behavior
*For any* character position within the dead zone, the camera position should remain constant; for any position outside the dead zone, the camera should move toward the character.
**Validates: Requirements 5.2, 5.3**

### Property 6: Damping Smoothness
*For any* sudden change in target position, the camera position should approach the target smoothly without overshooting, maintaining continuous velocity.
**Validates: Requirements 4.1, 4.3, 4.4**

### Property 7: Look-Ahead Proportionality
*For any* character velocity, the look-ahead offset magnitude should be proportional to the velocity magnitude, capped at the maximum look-ahead distance.
**Validates: Requirements 6.1, 6.2, 6.3**

### Property 8: Collision Distance Constraint
*For any* camera position with obstacles between camera and character, the actual camera distance should be less than or equal to the desired distance, never clipping through geometry.
**Validates: Requirements 8.1, 8.2, 8.5**

### Property 9: FOV Speed Correlation
*For any* character speed above the threshold, the FOV should increase proportionally; for speeds below the threshold, FOV should equal the base FOV.
**Validates: Requirements 10.1, 10.2, 10.3**

### Property 10: Animation Speed Synchronization
*For any* character velocity, the animation blend parameter should match the normalized speed (velocity magnitude / max speed), ensuring visual movement matches actual movement.
**Validates: Requirements 11.1, 11.2, 11.5**

### Property 11: Occlusion Transparency Application
*For any* object obstructing the camera-to-character line of sight, that object's renderer should have reduced alpha; for any previously obstructing object no longer in the way, alpha should be restored to original value.
**Validates: Requirements 9.1, 9.3, 9.4**

### Property 12: Input Normalization Bounds
*For any* raw input values, the normalized movement vector magnitude should be clamped to the range [0, 1], preventing over-speed from diagonal input.
**Validates: Requirements 14.1, 14.4**

### Property 13: Rotation Angle Limits
*For any* camera rotation input, the vertical rotation angle should remain within the configured min and max vertical angle bounds, preventing camera flipping.
**Validates: Requirements 7.3**

### Property 14: Model Rotation Initialization
*For any* summoner model loaded in NewGameUI, the model's rotation should be set to (0, 180, 0) immediately after instantiation.
**Validates: Requirements 13.1, 13.2**

## Error Handling

### Input System Errors

**Missing Input Manager:**
- Fallback: PlayerController uses zero input if PlayerInputManager.Instance is null
- Log warning on first access attempt
- Continue operation with no movement

**Invalid Input Values:**
- Clamp all input values to valid ranges
- Log warning for NaN or Infinity values
- Replace invalid values with zero

### Camera System Errors

**Missing Target:**
- Camera rig disables update loop if target is null
- Log error when target becomes null
- Provide SetTarget() method to reassign

**Collision Detection Failures:**
- If raycast fails, maintain current camera distance
- Log warning and continue operation
- Retry collision detection next frame

**Occlusion Handling Failures:**
- If material access fails, skip that renderer
- Log warning with renderer name
- Continue processing other renderers

### Character Controller Errors

**Missing CharacterController Component:**
- Auto-add CharacterController in Awake() if missing
- Log info message about auto-addition
- Configure with sensible defaults

**Teleport During Movement:**
- Disable CharacterController before position change
- Re-enable after position set
- Reset velocity to zero

**Animation Controller Missing:**
- Check for Animator component before setting parameters
- Log warning if missing
- Continue movement without animation updates

## Testing Strategy

### Unit Testing Approach

We will use Unity Test Framework for unit tests covering:

**Input System Tests:**
- Test input normalization with various raw values
- Test dead zone application
- Test input source switching (keyboard to gamepad)

**Movement Calculation Tests:**
- Test camera-relative direction calculation with various camera angles
- Test acceleration/deceleration curve application
- Test rotation interpolation math

**Camera Math Tests:**
- Test dead zone boundary detection
- Test look-ahead offset calculation
- Test FOV calculation based on speed
- Test collision distance adjustment

**Example Unit Tests:**
```csharp
[Test]
public void TestInputNormalization_DiagonalInput_IsClamped()
{
    // Diagonal keyboard input (1,1) should normalize to magnitude 1
    Vector2 input = new Vector2(1f, 1f);
    Vector2 normalized = PlayerInputManager.NormalizeInput(input);
    Assert.AreEqual(1f, normalized.magnitude, 0.01f);
}

[Test]
public void TestCameraRelativeDirection_ForwardInput_MatchesCameraForward()
{
    // Forward input should align with camera forward (projected to horizontal)
    Vector2 input = new Vector2(0f, 1f);
    Vector3 cameraForward = new Vector3(1f, 0f, 1f).normalized;
    Vector3 result = CalculateCameraRelativeDirection(input, cameraForward);
    Assert.AreEqual(cameraForward, result, "Direction should match camera forward");
}
```

### Property-Based Testing Approach

We will use **Unity Test Framework with custom property test helpers** for property-based testing. Each test will run a minimum of 100 iterations with randomized inputs.

**Testing Library:** Custom property test framework built on Unity Test Framework
- Random input generation for vectors, angles, speeds
- Configurable iteration count (minimum 100)
- Automatic shrinking for failure cases

**Property Test Structure:**
```csharp
[PropertyTest(Iterations = 100)]
public void Property_AccelerationMonotonicity()
{
    // Generate random initial conditions
    // Apply acceleration over multiple frames
    // Assert speed increases monotonically
}
```

**Key Property Tests:**

1. **Acceleration/Deceleration Properties:**
   - Generate random speeds and input states
   - Simulate multiple frames of movement
   - Verify monotonic speed changes

2. **Camera-Relative Movement:**
   - Generate random camera orientations and input directions
   - Calculate movement direction
   - Verify perpendicularity to up vector and alignment with camera

3. **Rotation Convergence:**
   - Generate random start and target rotations
   - Simulate rotation updates
   - Verify angular distance decreases

4. **Dead Zone Behavior:**
   - Generate random character positions relative to camera
   - Test positions inside and outside dead zone
   - Verify camera movement matches expectations

5. **Collision Constraints:**
   - Generate random obstacle configurations
   - Test camera positioning with obstacles
   - Verify distance never exceeds desired and never clips

**Property Test Tags:**
Each property-based test will include a comment tag in this format:
```csharp
// **Feature: character-camera-control, Property 1: Acceleration Monotonicity**
[PropertyTest(Iterations = 100)]
public void Property_AccelerationMonotonicity() { ... }
```

### Integration Testing

**Character Spawning Integration:**
- Test PlayerCharacterManager spawning with camera rig initialization
- Verify camera rig is properly configured and following character
- Test character controller is enabled and responding to input

**Scene Transition Integration:**
- Test character persistence across scene loads
- Verify camera rig maintains configuration
- Test position restoration from save data

### Manual Testing Checklist

**Movement Feel:**
- Character acceleration feels responsive
- Deceleration feels natural
- Rotation is smooth without snapping
- Camera-relative movement is intuitive

**Camera Behavior:**
- Camera follows smoothly without jitter
- Dead zone prevents unnecessary movement
- Look-ahead shows appropriate forward view
- Collision avoidance works reliably
- Occlusion handling is smooth

**Edge Cases:**
- Rapid direction changes
- Moving into corners
- Jumping (if implemented)
- Teleporting
- Camera rotation while moving

## Implementation Notes

### Performance Considerations

**Camera Raycasting:**
- Limit raycast count per frame (recommend 5-10 rays in a cone pattern)
- Use Physics.RaycastNonAlloc to avoid allocations
- Cache layer masks to avoid repeated GetMask calls

**Occlusion Material Management:**
- Use object pooling for temporary materials
- Limit number of simultaneously faded objects (recommend max 10)
- Use shader-based fading when possible instead of material instances

**Input Polling:**
- Poll input once per frame in PlayerInputManager
- Cache results for other systems to access
- Avoid redundant Input.GetAxis calls

### Unity Integration

**Animator Integration:**
```csharp
// In PlayerController.Update()
if (animator != null)
{
    float normalizedSpeed = currentSpeed / maxMoveSpeed;
    animator.SetFloat("Speed", normalizedSpeed);
    animator.SetBool("IsMoving", isMoving);
}
```

**Character Controller Setup:**
- Radius: 0.5f
- Height: 2.0f
- Center: (0, 1, 0)
- Skin Width: 0.08f
- Min Move Distance: 0.001f

**Camera Rig Hierarchy:**
```
PlayerCharacter
└── CameraRig (ThirdPersonCameraRig)
    └── CameraPivot (rotation pivot)
        └── Camera (actual camera)
            └── OcclusionHandler (CameraOcclusionHandler)
```

### Configuration Presets

**Responsive (Action Game):**
- Acceleration: 15 m/s²
- Deceleration: 20 m/s²
- Rotation Speed: 15 rad/s
- Camera Damping: 0.1s
- Dead Zone: 0.5m

**Smooth (Adventure Game):**
- Acceleration: 8 m/s²
- Deceleration: 10 m/s²
- Rotation Speed: 8 rad/s
- Camera Damping: 0.3s
- Dead Zone: 1.0m

**Cinematic (Story Game):**
- Acceleration: 5 m/s²
- Deceleration: 7 m/s²
- Rotation Speed: 5 rad/s
- Camera Damping: 0.5s
- Dead Zone: 1.5m
