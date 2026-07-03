## ADDED Requirements

### Requirement: Animation value source is usable by animation scheduler
Cerneala SHALL allow animation code to set and clear values through `UiPropertyValueSource.Animation` while preserving existing typed property precedence.

#### Scenario: Local value overrides animated value
- **WHEN** an element has both a local value and an animation value for the same property
- **THEN** the effective property value remains the local value

#### Scenario: Clearing animation restores lower precedence value
- **WHEN** an animation completes and clears the animation value source
- **THEN** the next lower precedence value becomes effective
