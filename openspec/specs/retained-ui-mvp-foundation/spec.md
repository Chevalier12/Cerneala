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
Cerneala SHALL document that the game loop may run every frame while layout and render command generation are invalidation-driven.

#### Scenario: Unchanged frame work is avoided
- **WHEN** no retained UI state changes between frames
- **THEN** the architecture documentation states that layout and render command generation are reused rather than recomputed

#### Scenario: Dirty state drives work
- **WHEN** retained UI state changes
- **THEN** the architecture documentation identifies dirty flags, propagation, layout queues, render queues, and cache updates as the mechanism for recomputing only needed work

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
