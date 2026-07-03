# animation-transitions Specification

## Purpose
TBD - created by archiving change add-animation-transitions. Update Purpose after archive.
## Requirements
### Requirement: Animation clocks are explicit and deterministic
Cerneala SHALL provide animation clocks that advance from explicit elapsed time supplied by the host or tests.

#### Scenario: Clock advances by elapsed time
- **WHEN** an animation clock is ticked with elapsed time
- **THEN** its current time advances deterministically by that elapsed amount

#### Scenario: Clock clamps completed progress
- **WHEN** elapsed time reaches or exceeds the animation duration
- **THEN** normalized progress is reported as complete and does not exceed 1

### Requirement: Typed animations interpolate values
Cerneala SHALL provide typed animations that interpolate values through explicit interpolation delegates.

#### Scenario: Typed animation samples midpoint
- **WHEN** a typed animation is sampled halfway through its duration
- **THEN** the returned value is produced by the typed interpolation delegate at eased progress

#### Scenario: Typed animation validates duration
- **WHEN** a typed animation is created
- **THEN** zero, negative, NaN, or infinite durations are rejected

### Requirement: Easing functions transform progress
Cerneala SHALL provide easing functions that transform normalized animation progress deterministically.

#### Scenario: Linear easing preserves progress
- **WHEN** linear easing is applied to normalized progress
- **THEN** the output equals the input progress

#### Scenario: Easing clamps progress
- **WHEN** easing receives progress outside the normalized range
- **THEN** the effective progress is clamped between 0 and 1

### Requirement: Animation scheduler applies animated property values
Cerneala SHALL provide an animation scheduler that applies active animation values through `UiPropertyValueSource.Animation`.

#### Scenario: Scheduler applies ticked value
- **WHEN** the animation scheduler ticks an active property animation
- **THEN** the target property receives the interpolated value through the animation value source

#### Scenario: Completed animation clears value source
- **WHEN** an animation reaches completion
- **THEN** the scheduler clears the animated property value source after applying the final value

#### Scenario: Scheduler reports active work
- **WHEN** active animations remain after a tick
- **THEN** the scheduler reports that more animation work is pending

### Requirement: Animation invalidation follows property metadata
Cerneala SHALL rely on existing typed property metadata and retained invalidation to schedule only affected work for animated value changes.

#### Scenario: Render-only animation avoids layout
- **WHEN** an animated value changes for a render-only property
- **THEN** retained render invalidation is requested without measure invalidation

#### Scenario: Layout animation schedules layout on changed ticks
- **WHEN** an animated value changes for a measure-affecting property
- **THEN** retained measure invalidation is requested for that changed tick

#### Scenario: Unchanged animated value avoids duplicate work
- **WHEN** a tick produces the same effective animated value
- **THEN** no duplicate property change invalidation is emitted

### Requirement: Transitions create animations from value changes
Cerneala SHALL provide transition descriptors that can create typed animations between old and new property values.

#### Scenario: Transition creates typed animation
- **WHEN** a transition receives old and new property values
- **THEN** it can create a typed animation for that value pair

#### Scenario: Transition rejects mismatched property type
- **WHEN** a transition is applied to an incompatible property type
- **THEN** transition creation fails clearly

### Requirement: Storyboards compose animations only as lightweight groups
Cerneala SHALL provide storyboard composition only as a lightweight grouping of animation handles.

#### Scenario: Storyboard ticks contained animations
- **WHEN** a storyboard is ticked
- **THEN** it advances each contained animation through the scheduler contract

### Requirement: Animation and transitions are tested
Cerneala SHALL include focused tests for clocks, schedulers, typed animations, transitions, and invalidation behavior.

#### Scenario: Required animation tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist under `tests/Cerneala.Tests/UI/Animation` for clocks, scheduling, typed animations, transitions, and invalidation

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

