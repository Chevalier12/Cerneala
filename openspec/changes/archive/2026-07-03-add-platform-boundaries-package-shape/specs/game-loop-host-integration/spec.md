## ADDED Requirements

### Requirement: Host adapters consume platform services through boundaries
Cerneala SHALL keep host adapter platform integration behind explicit platform service contracts.

#### Scenario: Host adapter boundary remains isolated
- **WHEN** MonoGame host adapter code is inspected
- **THEN** MonoGame references remain in adapter-specific folders and do not leak into core platform contracts
