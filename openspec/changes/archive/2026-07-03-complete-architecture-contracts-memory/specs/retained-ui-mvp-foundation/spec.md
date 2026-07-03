## ADDED Requirements

### Requirement: Architecture contract roadmap entries match canonical specs
Cerneala SHALL track architecture contract specs in `ROADMAPv2.md` using the canonical OpenSpec capability names that exist under `openspec/specs/`.

#### Scenario: Section 1 references canonical spec files
- **WHEN** roadmap section 1 lists retained UI architecture contract specs
- **THEN** each listed spec path exists under `openspec/specs/`

#### Scenario: Legacy placeholder spec names are not claimed
- **WHEN** roadmap section 1 is complete
- **THEN** it does not claim duplicate legacy spec folders that are not part of the current OpenSpec workspace

### Requirement: Architecture boundary tests guard repository shape
Cerneala SHALL include architecture tests that verify retained UI planning files and top-level source boundaries stay consistent with the roadmap.

#### Scenario: Repository shape guard exists
- **WHEN** architecture contract tests run
- **THEN** `tests/Cerneala.Tests/Architecture/RepositoryShapeTests.cs` verifies section 1 roadmap files and canonical OpenSpec spec files exist

### Requirement: UI core namespaces stay backend-neutral
Cerneala SHALL include architecture tests that reject direct backend dependencies in UI core namespaces outside explicit adapter folders.

#### Scenario: Namespace boundary guard exists
- **WHEN** architecture contract tests run
- **THEN** `tests/Cerneala.Tests/Architecture/NamespaceBoundaryTests.cs` fails if non-adapter UI core source files reference MonoGame, Skia, HarfBuzz, `SpriteBatch`, `Texture2D`, direct input polling, or platform-specific APIs
