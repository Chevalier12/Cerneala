## ADDED Requirements

### Requirement: Retained MVP includes command routing behavior
Cerneala SHALL implement command routing as the next retained input/control bridge phase of the MVP.

#### Scenario: Existing command foundations are completed
- **WHEN** command routing work is implemented
- **THEN** it completes `RoutedCommand`, `CommandBinding`, and `CommandEvents` through explicit retained `CommandRouter` APIs

#### Scenario: Roadmap tracks command routing completion
- **WHEN** retained command routing tasks are implemented
- **THEN** `ROADMAPv2.md` section 9 checkboxes are updated to match completed files and contracts
