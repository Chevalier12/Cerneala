## MODIFIED Requirements

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
