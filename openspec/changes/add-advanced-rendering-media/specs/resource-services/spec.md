## ADDED Requirements

### Requirement: Media identities can participate in resource dependencies
Cerneala SHALL allow resource-backed media values such as brushes and image sources to expose stable identities suitable for retained render dependency tracking.

#### Scenario: Brush resource identity changes render output
- **WHEN** a retained element depends on a brush resource and the resolved brush changes
- **THEN** retained render invalidation can observe a changed media identity

#### Scenario: Image source resource identity preserves intrinsic metadata
- **WHEN** an image source is resolved from resources
- **THEN** its intrinsic metadata and draw image identity remain explicit and backend-neutral
