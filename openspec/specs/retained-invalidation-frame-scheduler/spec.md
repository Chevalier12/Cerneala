# Retained Invalidation Frame Scheduler

## Purpose

Defines retained dirty state, invalidation propagation, phase queues, frame scheduling, diagnostics, and no-work-frame behavior for Cerneala.

## Requirements

### Requirement: Invalidation flags describe retained dirty work
Cerneala SHALL provide `InvalidationFlags` under `UI/Invalidation` to describe retained UI work independently from typed property metadata.

#### Scenario: Flags can be combined
- **WHEN** measure and render invalidation are requested together
- **THEN** the resulting invalidation value contains both measure and render work

#### Scenario: None represents no work
- **WHEN** an element has no retained dirty work
- **THEN** its invalidation flags are `None`

#### Scenario: Specialized flags remain explicit
- **WHEN** text, image, resource, style, input visual, hit-test, or subtree work is requested
- **THEN** each work kind is represented by an explicit invalidation flag

### Requirement: Dirty state tracks flags and versions
Cerneala SHALL provide per-element `DirtyState` that records active invalidation flags and monotonically increasing dirty version stamps.

#### Scenario: Marking dirty records flags
- **WHEN** an element is marked dirty for render work
- **THEN** its dirty state contains render invalidation

#### Scenario: Marking dirty increments version
- **WHEN** an element receives a new dirty request that changes its dirty state
- **THEN** the dirty version advances

#### Scenario: Repeating same dirty request is idempotent
- **WHEN** the same dirty flag is requested again without clearing it
- **THEN** the dirty version does not advance only because of the duplicate request

#### Scenario: Clearing processed flags keeps unrelated flags
- **WHEN** render flags are cleared after successful render-cache processing
- **THEN** unrelated measure, arrange, style, resource, or hit-test flags remain dirty

### Requirement: Invalidation requests are explicit
Cerneala SHALL represent invalidation input as `InvalidationRequest` values containing the target element, requested flags, reason, and source property when available.

#### Scenario: Property invalidation includes source property
- **WHEN** a typed property change invalidates a retained element
- **THEN** the invalidation request identifies the source property and the derived retained invalidation flags

#### Scenario: Manual invalidation has a reason
- **WHEN** retained code manually invalidates an element
- **THEN** the invalidation request records a diagnostic reason

### Requirement: Dirty propagation is deterministic
Cerneala SHALL provide dirty propagation rules that map invalidation requests to local, ancestor, descendant, layout, render, and hit-test work.

#### Scenario: Measure invalidation propagates layout need upward
- **WHEN** an element is invalidated for measure
- **THEN** the element is marked measure-dirty and layout ancestors are marked for layout work until a layout boundary rule stops propagation

#### Scenario: Arrange invalidation implies render work
- **WHEN** an element is invalidated for arrange
- **THEN** the element is marked arrange-dirty and render work is scheduled for affected visual output

#### Scenario: Render invalidation does not imply measure
- **WHEN** an element is invalidated for render only
- **THEN** no measure work is scheduled by that render-only request

#### Scenario: Text invalidation is conservative
- **WHEN** text invalidation may affect text metrics
- **THEN** measure, arrange, and render work are scheduled

#### Scenario: Image invalidation respects intrinsic size use
- **WHEN** image invalidation affects intrinsic size
- **THEN** measure, arrange, and render work are scheduled

#### Scenario: Image invalidation can be render only
- **WHEN** image invalidation does not affect intrinsic size
- **THEN** render work is scheduled without measure work

#### Scenario: Resource invalidation follows supplied effects
- **WHEN** a resource invalidation request declares retained effects
- **THEN** propagation applies those effects instead of hard-coding all work kinds

#### Scenario: Style invalidation reapplies style effects
- **WHEN** style invalidation is requested
- **THEN** style work is marked and property-specific invalidation effects can be raised afterward

#### Scenario: Input visual invalidation defaults to render only
- **WHEN** input visual state changes without explicit layout-affecting metadata
- **THEN** render work is scheduled without measure work

### Requirement: Phase queues deduplicate retained elements
Cerneala SHALL provide stable layout, render, and hit-test queues that deduplicate `UIElement` instances by reference.

#### Scenario: Same element is queued once
- **WHEN** the same element is enqueued for the same phase multiple times
- **THEN** the queue contains one entry for that element

#### Scenario: Equal-value elements remain distinct
- **WHEN** two different elements compare equal through `Equals`
- **THEN** the queue treats them as distinct retained elements

#### Scenario: Queue order is deterministic
- **WHEN** queued elements are drained for processing
- **THEN** the resulting order is stable for the same retained tree and invalidation inputs

### Requirement: Frame scheduler processes dirty work by phase
Cerneala SHALL provide `UiFrameScheduler` that processes dirty work through explicit frame phases and reports `FrameStats`.

#### Scenario: Scheduler no-ops when nothing is dirty
- **WHEN** a frame is processed and no retained dirty work exists
- **THEN** no measure, arrange, render-cache, or hit-test processor work runs

#### Scenario: Measure runs before arrange
- **WHEN** measure and arrange work are both queued
- **THEN** measure work is processed before arrange work

#### Scenario: Arrange runs before render-cache updates
- **WHEN** arrange and render work are both queued
- **THEN** arrange work is processed before render-cache work

#### Scenario: Render-cache updates run before hit-test rebuild
- **WHEN** render and hit-test work are both queued
- **THEN** render-cache work is processed before hit-test rebuild work

#### Scenario: MVP processes all queued work
- **WHEN** a frame budget is present in MVP
- **THEN** the scheduler still processes all queued dirty work without deferring remaining entries

#### Scenario: Frame stats count processed work
- **WHEN** a frame processes dirty work
- **THEN** `FrameStats` reports processed measure, arrange, render-cache, hit-test, reused-cache, and no-work counts as applicable

#### Scenario: Render-cache phase delegates to retained rendering
- **WHEN** the frame scheduler processes queued render work for a retained root
- **THEN** the root's retained render queue processor updates element render caches during the render-cache phase

### Requirement: Dirty flags clear only after successful processing
Cerneala SHALL clear dirty flags only after the matching frame phase has completed successfully.

#### Scenario: Successful phase clears matching flags
- **WHEN** render-cache processing succeeds for an element
- **THEN** render dirty flags for that element are cleared

#### Scenario: Failed phase keeps dirty flags
- **WHEN** a phase processor fails for an element
- **THEN** the matching dirty flags remain set for diagnostics or retry

#### Scenario: Failed phase does not clear later phase work
- **WHEN** measure processing fails before arrange or render processing
- **THEN** arrange and render dirty flags are not incorrectly cleared

### Requirement: Layout queue processing uses layout manager
Cerneala SHALL process retained measure and arrange queue work through the layout system.

#### Scenario: Measure phase delegates to layout manager
- **WHEN** the frame scheduler processes measure work
- **THEN** the corresponding layout manager measure behavior runs for queued elements

#### Scenario: Arrange phase delegates to layout manager
- **WHEN** the frame scheduler processes arrange work
- **THEN** the corresponding layout manager arrange behavior runs for queued elements

#### Scenario: Layout completion can schedule render and hit-test work
- **WHEN** layout changes arranged bounds or layout participation
- **THEN** render and hit-test work can be scheduled without forcing another measure pass

#### Scenario: Failed layout phase keeps dirty flags
- **WHEN** layout measure or arrange processing fails
- **THEN** the matching dirty flags remain set for retry or diagnostics

### Requirement: Invalidation tracing records dirty work
Cerneala SHALL provide `InvalidationTrace` diagnostics that can record requests, propagation, queueing, phase processing, and flag clearing.

#### Scenario: Trace records invalidation request
- **WHEN** an element is invalidated
- **THEN** the trace can record the target element id, flags, reason, and source property diagnostic name when available

#### Scenario: Trace records frame phases
- **WHEN** the scheduler processes a frame
- **THEN** the trace can record each processed frame phase and the number of elements processed

#### Scenario: Trace can be disabled
- **WHEN** tracing is disabled
- **THEN** invalidation and scheduler behavior remains unchanged and no trace records are retained

### Requirement: Retained no-work frame behavior is tested
Cerneala SHALL include focused tests proving retained invalidation avoids repeated work on unchanged frames.

#### Scenario: Unchanged tree does not repeat layout
- **WHEN** a tree is processed for two frames without state changes between frames
- **THEN** the second frame does not measure or arrange elements again

#### Scenario: Unchanged tree does not repeat render command generation
- **WHEN** a tree is drawn again without retained state changes
- **THEN** render command generation is not scheduled again

#### Scenario: Draw can reuse cached root command list
- **WHEN** draw is called every frame and no retained state changed
- **THEN** the cached root command list can be reused

#### Scenario: Render-only invalidation avoids measure
- **WHEN** an element receives render-only invalidation
- **THEN** the next frame does not run measure for that request

#### Scenario: Measure invalidation refreshes render after layout settles
- **WHEN** an element receives measure invalidation
- **THEN** render-cache work is scheduled only after layout-related work has been processed

#### Scenario: Render-only invalidation rebuilds only render cache
- **WHEN** an element receives render-only invalidation
- **THEN** the next frame rebuilds the affected retained render cache without running measure or arrange
