## ADDED Requirements

### Requirement: Advanced routed events have typed dispatch paths
Cerneala SHALL provide typed dispatch paths for existing touch, stylus, manipulation, and drag/drop routed event metadata.

#### Scenario: Existing event identities are reused
- **WHEN** touch, stylus, manipulation, or drag/drop behavior is dispatched
- **THEN** the routed event identity comes from `InputEvents` rather than from duplicate event definitions

#### Scenario: Advanced dispatch uses retained route maps
- **WHEN** advanced input is dispatched to a retained element
- **THEN** routing uses the retained input route map and routed event router
