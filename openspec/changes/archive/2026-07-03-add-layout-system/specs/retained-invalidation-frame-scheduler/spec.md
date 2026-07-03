## ADDED Requirements

### Requirement: Layout queue processing uses layout manager
Cerneala SHALL process retained measure and arrange queue work through the layout system.

#### Scenario: Measure phase delegates to layout manager
- **WHEN** the frame scheduler processes measure work
- **THEN** the corresponding layout manager measure behavior runs for queued elements

#### Scenario: Arrange phase delegates to layout manager
- **WHEN** the frame scheduler processes arrange work
- **THEN** the corresponding layout manager arrange behavior runs for queued elements

#### Scenario: Layout completion can schedule render and hit-test work
- **WHEN** layout changes arranged bounds or layout participation
- **THEN** render and hit-test work can be scheduled without forcing another measure pass

#### Scenario: Failed layout phase keeps dirty flags
- **WHEN** layout measure or arrange processing fails
- **THEN** the matching dirty flags remain set for retry or diagnostics
