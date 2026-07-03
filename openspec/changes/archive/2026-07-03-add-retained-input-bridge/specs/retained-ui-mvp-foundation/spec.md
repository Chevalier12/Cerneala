## ADDED Requirements

### Requirement: Retained MVP includes input bridge behavior
Cerneala SHALL implement retained input behavior above existing input snapshots and routed event primitives.

#### Scenario: Existing input foundations are reused
- **WHEN** retained input bridge work is implemented
- **THEN** it reuses `InputFrame`, `UiInputTree`, `ElementInputRouteBuilder`, `RoutedEventRouter`, and `InputEvents`

#### Scenario: Roadmap tracks input bridge completion
- **WHEN** retained input bridge tasks are implemented
- **THEN** `ROADMAPv2.md` section 8 checkboxes are updated to match completed files and contracts
