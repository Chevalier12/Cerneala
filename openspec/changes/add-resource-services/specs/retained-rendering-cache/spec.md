## ADDED Requirements

### Requirement: Retained rendering tracks resource dependency versions
Cerneala SHALL include resource dependency versions in retained render cache staleness checks.

#### Scenario: Resource dependency change invalidates local cache
- **WHEN** a retained element depends on a resource and that resource version changes
- **THEN** the element local render command cache is considered stale

#### Scenario: Unchanged resource dependency reuses cache
- **WHEN** a retained element has unchanged render state and unchanged resource dependency version
- **THEN** its retained local render command cache can be reused

#### Scenario: Resource render dependency remains backend-neutral
- **WHEN** retained rendering tracks resource dependencies
- **THEN** `UI/Rendering` stores only resource dependency identity or versions, not backend resource objects
