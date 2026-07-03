# platform-boundaries-package-shape Specification

## Purpose
TBD - created by archiving change add-platform-boundaries-package-shape. Update Purpose after archive.
## Requirements
### Requirement: Platform services are explicit
Cerneala SHALL provide platform-neutral contracts for clipboard, cursor, file dialogs, DPI, text input, and accessibility services.

#### Scenario: Platform service aggregate exposes known seams
- **WHEN** platform services are created
- **THEN** the aggregate exposes clipboard, cursor, file dialog, text input, DPI, and accessibility service members

#### Scenario: Platform services can be partially available
- **WHEN** a host does not provide every platform capability
- **THEN** platform service creation still succeeds with missing optional services represented explicitly

### Requirement: Platform contracts stay backend-neutral
Cerneala SHALL keep `UI/Platform` independent of concrete rendering, platform, and native UI adapter APIs.

#### Scenario: Platform contracts avoid backend references
- **WHEN** `UI/Platform` code is inspected
- **THEN** it does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, Windows UI APIs, or native accessibility APIs

### Requirement: MonoGame host remains adapter-scoped
Cerneala SHALL keep MonoGame host integration under the existing MonoGame adapter folder.

#### Scenario: MonoGame host code stays in adapter folder
- **WHEN** code references MonoGame host adapter concepts
- **THEN** those references are scoped to `UI/Hosting/MonoGame`, explicit playground consumers, or tests that explicitly assert boundaries

### Requirement: Package split remains optional until real split work exists
Cerneala SHALL not require future package split project files until a real package split change is implemented.

#### Scenario: Optional split projects are not falsely required
- **WHEN** section 24 is complete
- **THEN** roadmap does not claim `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, or split test projects are implemented unless those files exist

### Requirement: Platform boundaries are tested
Cerneala SHALL include tests for platform boundary contracts, service registration, and MonoGame dependency boundaries.

#### Scenario: Required platform boundary tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for platform boundaries, service registration, and MonoGame dependency boundaries

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

