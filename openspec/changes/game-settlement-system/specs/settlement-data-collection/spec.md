## ADDED Requirements

### Requirement: Collect game session statistics
The system SHALL collect relevant game statistics from the current session (experience gained, currency earned, items dropped, enemies defeated, duration, etc.) and make them available for display and persistence.

#### Scenario: Collect combat rewards
- **WHEN** a combat session ends (either by winning or by player death)
- **THEN** system collects: total experience, gold/currency earned, items dropped, number of enemies defeated

#### Scenario: Collect session metadata
- **WHEN** settlement is triggered
- **THEN** system collects: session start time, session end time, duration, scene name, difficulty level (if applicable)

#### Scenario: Aggregate drops from multiple sources
- **WHEN** settlement collects item drops from combat manager, exploration manager, or other systems
- **THEN** system consolidates all items into a single drops list with quantity and rarity

### Requirement: Store collected data for settlement processing
The system SHALL store collected statistics in a runtime data structure that persists until the settlement process completes.

#### Scenario: Data survives async operations
- **WHEN** settlement data is collected and scene loading begins asynchronously
- **THEN** collected data remains accessible throughout the entire settlement and scene transition process

#### Scenario: Data accessible during UI display
- **WHEN** settlement UI is displayed
- **THEN** UI can query collected statistics (experience, currency, items) from the settlement data object
