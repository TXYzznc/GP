## ADDED Requirements

### Requirement: Display settlement results UI
The system SHALL display a UI form (SettlementUIForm) that shows collected statistics in a user-friendly format and remains on top of the scene loading UI.

#### Scenario: Show settlement statistics
- **WHEN** settlement UI opens
- **THEN** it displays: total experience earned, gold/currency earned, list of dropped items with quantities, number of enemies defeated, session duration

#### Scenario: UI positioned above loading screen
- **WHEN** both settlement UI and scene loading UI are active
- **THEN** settlement UI is rendered on top with highest sort order (Canvas layer management)

#### Scenario: Dynamic content loading
- **WHEN** settlement UI is instantiated
- **THEN** it reads from the settlement data object and dynamically populates UI elements (text labels, item list containers, etc.)

### Requirement: Allow manual UI dismissal
The system SHALL provide a close button that allows the player to manually dismiss the settlement UI and proceed to the new scene.

#### Scenario: Close button interaction
- **WHEN** player clicks the close button on settlement UI
- **THEN** settlement UI is destroyed, revealing the newly loaded scene and completing the transition

#### Scenario: Input validation
- **WHEN** settlement UI is displayed
- **THEN** the UI must be interactable (not disabled or hidden)

### Requirement: No automatic dismissal during loading
The system SHALL NOT automatically close the settlement UI; it remains visible until the player explicitly closes it or the scene loading completes.

#### Scenario: UI persists during async load
- **WHEN** settlement UI is displayed and scene loading is in progress
- **THEN** settlement UI remains visible and responsive
