## MODIFIED Requirements

### Requirement: Confirmed MVP decisions are captured
Cerneala SHALL capture the confirmed MVP decisions from `ROADMAPv2.md`.

#### Scenario: Tree model decision is captured
- **WHEN** the v2 architecture is documented
- **THEN** it states that MVP uses separate logical and visual trees

#### Scenario: Retained element implementation follows confirmed tree decision
- **WHEN** retained element tree work is planned or implemented
- **THEN** it follows the confirmed separate logical and visual tree MVP decision instead of stale single-tree wording

#### Scenario: Input route decision is captured
- **WHEN** the v2 architecture is documented
- **THEN** it states that the new retained route model replaces `UiInputTree` as the future route table while preserving useful routed-event concepts

#### Scenario: Render cache decision is captured
- **WHEN** the v2 architecture is documented
- **THEN** it states that MVP uses subtree render caches from the start

#### Scenario: Style invalidation decision is captured
- **WHEN** the v2 architecture is documented
- **THEN** it states that input visual invalidation is decided by style metadata
