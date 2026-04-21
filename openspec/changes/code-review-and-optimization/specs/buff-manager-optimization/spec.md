## MODIFIED Requirements

### Requirement: AddBuff with flexible caster and attribute parameters
The system SHALL consolidate AddBuff overloads into a single method with optional parameters. The public API SHALL remain backward compatible.

#### Scenario: AddBuff with GameObject caster only
- **WHEN** `AddBuff(buffId, casterGameObject)` is called (legacy signature)
- **THEN** system auto-extracts ChessAttribute from caster, behaves identically to before

#### Scenario: AddBuff with explicit attribute
- **WHEN** `AddBuff(buffId, casterGameObject, casterAttribute)` is called
- **THEN** system uses provided attribute without redundant GetComponent call

#### Scenario: AddBuff without caster
- **WHEN** `AddBuff(buffId)` is called with no caster
- **THEN** system initializes context with null caster, buffId applies to self only

## ADDED Requirements

### Requirement: Extract GetAndValidateBuffConfig helper method
A private helper method SHALL validate buff configuration lookup, eliminating duplicate error checking across AddBuff variants.

#### Scenario: Config validation in helper
- **WHEN** buffId is looked up in BuffTable
- **THEN** GetAndValidateBuffConfig returns config or null, with DebugEx error logged

### Requirement: Extract InitializeBuff helper method
A private helper method SHALL encapsulate buff initialization (Init, Add, OnEnter, event firing), reducing code duplication.

#### Scenario: Unified initialization flow
- **WHEN** new buff instance is created
- **THEN** InitializeBuff(buff, config, buffId) handles all setup steps consistently

### Requirement: Remove duplicate GetBuff lookups in Update
The Update loop SHALL use optimized iteration to reduce redundant GetBuff calls.

#### Scenario: Efficient Update iteration
- **WHEN** Update processes buff updates and removals
- **THEN** buffIsFinished flag is checked in the first loop without second GetBuff call
