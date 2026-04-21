## ADDED Requirements

### Requirement: Audit and fix event subscription leaks
The system SHALL ensure every event Subscribe has a matching Unsubscribe in the same object's lifecycle. Leaked subscriptions SHALL be identified and fixed.

#### Scenario: Event cleanup on object destruction
- **WHEN** MonoBehaviour with event subscriptions is destroyed
- **THEN** OnDestroy or OnLeave unsubscribes from all events (Unsubscribe calls match Subscribe calls)

#### Scenario: Subscription audit in combat system
- **WHEN** CombatState enters and exits battle
- **THEN** all GF.Event.Subscribe calls in OnEnter have matching Unsubscribe in OnLeave

#### Scenario: Subscription audit in UI system
- **WHEN** UI form is opened and closed
- **THEN** all event subscriptions in OnOpen have matching Unsubscribe in OnClose

### Requirement: Establish subscription verification tool
A static analyzer or code generation tool SHALL detect += without corresponding -= in the same class, flagging potential leaks.

#### Scenario: Tool detects unmatched subscription
- **WHEN** code review runs
- **THEN** tool reports files where event += count != -= count

### Requirement: Document event lifecycle pattern
All manager classes SHALL follow clear subscribe/unsubscribe pattern: OnEnable/OnEnter for subscribe, OnDisable/OnLeave for unsubscribe.

#### Scenario: Clear lifecycle documentation
- **WHEN** developer adds new event subscription
- **THEN** they reference documented pattern in code comments or wiki

### Requirement: Fix high-frequency event subscriptions
Event subscriptions in combat, UI, and chess systems (>50 per session) SHALL be audited first, given their impact on long-term memory.

#### Scenario: Combat system subscription cleanup
- **WHEN** combat ends
- **THEN** all combat-related events are unsubscribed, CombatEntityTracker clears event hooks
