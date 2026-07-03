## ADDED Requirements

### Requirement: Markup property assignment uses typed state validation
Cerneala SHALL assign markup-provided property values through the same typed property validation and coercion paths used by code-created retained objects.

#### Scenario: Invalid markup value is rejected by typed property validation
- **WHEN** markup assigns a value that violates a registered typed property validator
- **THEN** the markup factory reports a diagnostic error and does not bypass the typed property validator

#### Scenario: Coerced markup value uses typed property coercion
- **WHEN** markup assigns a value accepted by a registered typed property coercion rule
- **THEN** the created retained object stores the coerced typed value
