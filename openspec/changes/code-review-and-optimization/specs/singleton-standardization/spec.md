## ADDED Requirements

### Requirement: Standardize MonoBehaviour singletons on SingletonBase<T>
All Manager classes inheriting from MonoBehaviour (CombatManager, BattleArenaManager, etc.) SHALL use SingletonBase<T> as base class for consistent singleton implementation.

#### Scenario: CombatManager inheritance
- **WHEN** CombatManager is implemented
- **THEN** it inherits from SingletonBase<CombatManager>, removing manual Instance property and OnDestroy boilerplate

#### Scenario: Automatic cleanup
- **WHEN** CombatManager object is destroyed
- **THEN** SingletonBase.OnDestroy automatically nullifies Instance reference

### Requirement: Standardize non-MonoBehaviour singletons
Non-MonoBehaviour singletons (CardManager, ChessPlacementManager) SHALL follow consistent pattern: private static instance, lazy-initialized public property, private constructor.

#### Scenario: CardManager singleton pattern
- **WHEN** CardManager.Instance is accessed first time
- **THEN** instance is created via lazy initialization; subsequent access returns same instance

#### Scenario: Explicit cleanup
- **WHEN** game framework shuts down
- **THEN** OnShutdown calls CardManager instance's Dispose/OnDestroy method, nullifying static reference

### Requirement: Create singleton registry (optional for future use)
A singleton registry MAY track all active singletons for debugging and lifecycle management. Optional to implement in this phase; document for future optimization.

#### Scenario: Singleton tracking
- **WHEN** singleton is created
- **THEN** optional: registers in global registry; useful for detecting uncleared references

### Requirement: Establish naming convention
All singleton property names SHALL be "Instance" (e.g., CardManager.Instance, CombatManager.Instance), not "Get()" or other variants.

#### Scenario: Uniform property name
- **WHEN** accessing any singleton
- **THEN** property is always named Instance, consistency across codebase
