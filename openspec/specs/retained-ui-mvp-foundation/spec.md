# Retained UI MVP Foundation

## Purpose

Defines the planning, documentation, and architectural memory requirements for Cerneala's retained UI MVP foundation.
## Requirements
### Requirement: V2 roadmap is the active retained UI plan
Cerneala SHALL treat `ROADMAPv2.md` as the active roadmap for retained UI work.

#### Scenario: Future implementation uses v2 roadmap
- **WHEN** retained UI implementation work is planned
- **THEN** the work references `ROADMAPv2.md` instead of the legacy WPF-clone-oriented roadmap

### Requirement: Existing drawing and input boundaries are preserved
Cerneala SHALL build retained UI layers above the existing `UI/Drawing` and `UI/Input` foundations.

#### Scenario: Drawing remains a command layer
- **WHEN** retained UI architecture is documented
- **THEN** `DrawingContext` and `DrawCommandList` are described as low-level drawing command infrastructure, not a scene graph or layout system

#### Scenario: Input remains a foundation
- **WHEN** retained UI architecture is documented
- **THEN** existing input snapshots, routed event metadata, and command primitives are described as foundations to preserve or adapt, not behavior to duplicate blindly

### Requirement: Retained-mode frame loop is documented
Cerneala SHALL document and implement the foundation that allows the game loop to run every frame while layout and render command generation are invalidation-driven.

#### Scenario: Unchanged frame work is avoided
- **WHEN** no retained UI state changes between frames
- **THEN** the architecture documentation and retained invalidation scheduler state that layout and render command generation are reused rather than recomputed

#### Scenario: Dirty state drives work
- **WHEN** retained UI state changes
- **THEN** the architecture documentation and retained invalidation scheduler identify dirty flags, propagation, layout queues, render queues, hit-test queues, and cache-update hooks as the mechanism for recomputing only needed work

#### Scenario: Scheduler is game-loop friendly
- **WHEN** update and draw are called every frame
- **THEN** the retained frame scheduler processes no dirty phase work on unchanged frames while still allowing draw to use cached output

#### Scenario: Layout phase is concrete
- **WHEN** retained measure or arrange work is queued
- **THEN** the retained layout system processes that work before render-cache work in the frame loop

#### Scenario: Render-cache phase is concrete
- **WHEN** retained render work is queued
- **THEN** the retained rendering system regenerates only dirty local command lists and exposes a cached root command list for draw

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

### Requirement: Project memory files exist
Cerneala SHALL include project memory files for OpenSpec usage and v2 architecture.

#### Scenario: OpenSpec project memory exists
- **WHEN** the documentation slice is complete
- **THEN** `openspec/README.md` and `openspec/project.md` exist

#### Scenario: V2 architecture docs exist
- **WHEN** the documentation slice is complete
- **THEN** `docs/architecture-v2.md`, `docs/diagrams/retained-frame-loop.md`, and `docs/diagrams/ui-layer-boundaries.md` exist

### Requirement: Implementation tasks are checklist-based
Cerneala SHALL provide a checklist task list for implementing the retained UI MVP foundation later.

#### Scenario: Tasks are resumable
- **WHEN** a future session starts implementation
- **THEN** `openspec/changes/add-retained-ui-mvp-foundation/tasks.md` lists concrete files and verification steps rather than relying on chat memory

### Requirement: Runtime behavior is unchanged by this planning slice
Cerneala SHALL not change runtime drawing or input behavior as part of this planning and documentation slice.

#### Scenario: No runtime production code is added
- **WHEN** this change is complete
- **THEN** no new runtime implementation files under `UI/Core`, `UI/Elements`, `UI/Layout`, `UI/Rendering`, `UI/Controls`, or `UI/Hosting` are required by this change

### Requirement: Retained frame loop has application host
Cerneala SHALL expose the retained-mode frame loop through a concrete application-facing host while preserving `ROADMAPv2.md` as the active implementation plan.

#### Scenario: Host is the game-loop entry point
- **WHEN** application integration is implemented for the retained UI MVP
- **THEN** update and draw integration are represented by `UiHost` and backend adapters instead of ad-hoc application wiring

#### Scenario: Roadmap tracks host completion
- **WHEN** game-loop host integration tasks are implemented
- **THEN** `ROADMAPv2.md` section 7 checkboxes are updated to match completed files and contracts

### Requirement: Retained MVP includes input bridge behavior
Cerneala SHALL implement retained input behavior above existing input snapshots and routed event primitives.

#### Scenario: Existing input foundations are reused
- **WHEN** retained input bridge work is implemented
- **THEN** it reuses `InputFrame`, `UiInputTree`, `ElementInputRouteBuilder`, `RoutedEventRouter`, and `InputEvents`

#### Scenario: Roadmap tracks input bridge completion
- **WHEN** retained input bridge tasks are implemented
- **THEN** `ROADMAPv2.md` section 8 checkboxes are updated to match completed files and contracts

### Requirement: Retained MVP includes command routing behavior
Cerneala SHALL implement command routing as the next retained input/control bridge phase of the MVP.

#### Scenario: Existing command foundations are completed
- **WHEN** command routing work is implemented
- **THEN** it completes `RoutedCommand`, `CommandBinding`, and `CommandEvents` through explicit retained `CommandRouter` APIs

#### Scenario: Roadmap tracks command routing completion
- **WHEN** retained command routing tasks are implemented
- **THEN** `ROADMAPv2.md` section 9 checkboxes are updated to match completed files and contracts

### Requirement: Retained MVP includes first controls and panels
Cerneala SHALL implement the first retained controls and panels as the next MVP phase after command routing.

#### Scenario: First controls complete MVP composition path
- **WHEN** first controls and panels are implemented
- **THEN** `UIRoot` can retain a tree containing stack layout, border visuals, text display, and a command-bound button

#### Scenario: Roadmap tracks first controls completion
- **WHEN** first controls and panels tasks are implemented
- **THEN** `ROADMAPv2.md` section 10 checkboxes are updated to match completed files and contracts

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

