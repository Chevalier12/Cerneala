## ADDED Requirements

### Requirement: Retained frame loop has application host
Cerneala SHALL expose the retained-mode frame loop through a concrete application-facing host while preserving `ROADMAPv2.md` as the active implementation plan.

#### Scenario: Host is the game-loop entry point
- **WHEN** application integration is implemented for the retained UI MVP
- **THEN** update and draw integration are represented by `UiHost` and backend adapters instead of ad-hoc application wiring

#### Scenario: Roadmap tracks host completion
- **WHEN** game-loop host integration tasks are implemented
- **THEN** `ROADMAPv2.md` section 7 checkboxes are updated to match completed files and contracts
