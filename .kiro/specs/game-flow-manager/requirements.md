# Requirements Document

## Introduction

This document specifies the requirements for a centralized Game Flow Manager system that handles common game flow operations such as entering the game, scene transitions, and procedure state changes. Currently, multiple UI scripts (StartMenuUI, LoadGameUI, NewGameUI) contain duplicated `EnterGame()` methods, which violates the DRY (Don't Repeat Yourself) principle and makes maintenance difficult.

## Glossary

- **Game Flow Manager**: A centralized singleton service that manages game state transitions and scene loading
- **Procedure**: A state in the game's finite state machine (FSM) that represents different phases of the game (Menu, Game, GameOver, etc.)
- **Scene Transition**: The process of unloading the current scene and loading a new scene
- **UI Form**: A user interface screen/panel in the game
- **Save Data**: Player progress data stored in the save system
- **Test Scene**: The main gameplay scene where the game takes place

## Requirements

### Requirement 1

**User Story:** As a developer, I want a centralized Game Flow Manager, so that I can manage game state transitions from a single location instead of duplicating code across multiple UI scripts.

#### Acceptance Criteria

1. WHEN the system initializes THEN the Game Flow Manager SHALL create a singleton instance accessible throughout the application
2. WHEN any script requests the Game Flow Manager instance THEN the system SHALL return the same singleton instance
3. WHEN the Game Flow Manager is accessed THEN the system SHALL provide a clean, static API for common game flow operations
4. WHEN the application shuts down THEN the Game Flow Manager SHALL clean up its resources properly

### Requirement 2

**User Story:** As a developer, I want to enter the game from any UI screen using a single method call, so that the game entry logic is consistent and maintainable.

#### Acceptance Criteria

1. WHEN a UI script calls the enter game method THEN the Game Flow Manager SHALL load the Test scene
2. WHEN a UI script calls the enter game method THEN the Game Flow Manager SHALL transition to the GameProcedure state
3. WHEN the enter game method is called THEN the system SHALL log the current save data information for debugging
4. WHEN the enter game method is called without valid save data THEN the system SHALL log a warning but continue the transition
5. WHEN the scene transition begins THEN the system SHALL display a loading progress indicator

### Requirement 3

**User Story:** As a developer, I want the scene transition to use the existing ChangeSceneProcedure, so that scene loading is handled consistently with the framework's architecture.

#### Acceptance Criteria

1. WHEN transitioning to a new scene THEN the Game Flow Manager SHALL set the scene name parameter in the procedure FSM
2. WHEN transitioning to a new scene THEN the Game Flow Manager SHALL change the procedure state to ChangeSceneProcedure
3. WHEN the ChangeSceneProcedure completes loading the Test scene THEN the system SHALL automatically transition to GameProcedure
4. WHEN a scene transition fails THEN the system SHALL log an error and handle the failure gracefully

### Requirement 4

**User Story:** As a developer, I want to refactor existing UI scripts to use the Game Flow Manager, so that duplicated code is eliminated and maintenance is simplified.

#### Acceptance Criteria

1. WHEN StartMenuUI needs to enter the game THEN the script SHALL call the Game Flow Manager's enter game method
2. WHEN LoadGameUI needs to enter the game THEN the script SHALL call the Game Flow Manager's enter game method
3. WHEN NewGameUI needs to enter the game THEN the script SHALL call the Game Flow Manager's enter game method
4. WHEN any UI script is refactored THEN the local EnterGame method SHALL be removed
5. WHEN the refactoring is complete THEN all three UI scripts SHALL use the same centralized method

### Requirement 5

**User Story:** As a developer, I want the GameProcedure to be properly implemented, so that the game can run in the Test scene after the transition.

#### Acceptance Criteria

1. WHEN the GameProcedure is entered THEN the system SHALL log the entry event
2. WHEN the GameProcedure is entered THEN the system SHALL resume the game if it was paused
3. WHEN the GameProcedure is active THEN the system SHALL update game logic each frame
4. WHEN the GameProcedure is exited THEN the system SHALL clean up game-specific resources
5. WHEN the GameProcedure is exited THEN the system SHALL unsubscribe from game events

### Requirement 6

**User Story:** As a developer, I want the ChangeSceneProcedure to recognize the Test scene, so that it can automatically transition to GameProcedure after loading.

#### Acceptance Criteria

1. WHEN the ChangeSceneProcedure finishes loading a scene THEN the system SHALL check the scene name
2. WHEN the loaded scene name is "Test" THEN the system SHALL transition to GameProcedure
3. WHEN the loaded scene name is "StartGame" THEN the system SHALL transition to StartGameProcedure
4. WHEN the loaded scene name is unrecognized THEN the system SHALL log a warning and remain in ChangeSceneProcedure
