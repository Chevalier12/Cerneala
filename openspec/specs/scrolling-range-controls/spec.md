# scrolling-range-controls Specification

## Purpose
TBD - created by archiving change add-scrolling-range-controls. Update Purpose after archive.
## Requirements
### Requirement: Range primitives provide typed value state
Cerneala SHALL provide `RangeBase` as a retained control primitive with typed range and change properties.

#### Scenario: Range value is clamped
- **WHEN** `RangeBase.Value` is set below `Minimum` or above `Maximum`
- **THEN** the effective value is clamped to the valid range

#### Scenario: Range endpoint changes coerce value
- **WHEN** `Minimum` or `Maximum` changes
- **THEN** `Value` is coerced into the new valid range

#### Scenario: Range changes invalidate retained output
- **WHEN** a range-affecting property changes effective value
- **THEN** retained layout or render invalidation is requested according to the property metadata

### Requirement: Thumb supports retained dragging
Cerneala SHALL provide `Thumb` as a retained draggable primitive using existing routed input and pointer capture.

#### Scenario: Drag starts on pointer press
- **WHEN** primary pointer input presses a `Thumb`
- **THEN** the thumb captures the pointer and enters dragging state

#### Scenario: Drag delta follows pointer movement
- **WHEN** a captured pointer moves after drag start
- **THEN** the thumb reports deterministic horizontal and vertical drag delta values

#### Scenario: Drag completes on pointer release
- **WHEN** the captured pointer is released
- **THEN** dragging ends and pointer capture is released

### Requirement: Track maps range values to thumb layout
Cerneala SHALL provide `Track` as a retained control primitive that maps `RangeBase` values to thumb position and scroll commands.

#### Scenario: Track positions thumb from value
- **WHEN** a track has minimum, maximum, viewport, orientation, and value state
- **THEN** it arranges the thumb at the deterministic position representing that value

#### Scenario: Track converts thumb drag to value
- **WHEN** a track thumb is dragged
- **THEN** the owning range value changes according to the drag delta and track length

#### Scenario: Track handles decrease and increase regions
- **WHEN** pointer input targets the track before or after the thumb
- **THEN** the track can request small or large range changes in the expected direction

### Requirement: ScrollBar exposes retained range scrolling
Cerneala SHALL provide `ScrollBar` as a retained range control composed from track and thumb behavior.

#### Scenario: ScrollBar value follows thumb drag
- **WHEN** the scrollbar thumb is dragged
- **THEN** `ScrollBar.Value` changes within its range

#### Scenario: ScrollBar orientation affects layout
- **WHEN** scrollbar orientation changes between horizontal and vertical
- **THEN** measure and arrange behavior reflect the new orientation

#### Scenario: ScrollBar can be templated
- **WHEN** a scrollbar has a control template
- **THEN** its generated track and thumb children remain retained across frames

### Requirement: ScrollViewer manages viewport, extent, offset, and scrollbar visibility
Cerneala SHALL provide `ScrollViewer`, `ScrollContentPresenter`, `ScrollBarVisibility`, and `IScrollInfo` for retained scrolling.

#### Scenario: Presenter computes extent and viewport
- **WHEN** scroll content is measured inside a constrained viewport
- **THEN** the presenter exposes deterministic extent and viewport sizes

#### Scenario: Offset clamps to scrollable range
- **WHEN** horizontal or vertical offset is set outside the scrollable range
- **THEN** the effective offset is clamped to the valid extent minus viewport range

#### Scenario: Mouse wheel scrolls vertical offset
- **WHEN** retained mouse wheel input targets a scroll viewer with vertical scrollable content
- **THEN** vertical offset changes by a deterministic scroll amount

#### Scenario: Scrollbar visibility follows policy
- **WHEN** scrollbar visibility is disabled, hidden, visible, or auto
- **THEN** the scroll viewer shows, reserves, or hides scrollbar content according to that policy and scrollability

#### Scenario: Offset changes avoid unnecessary measure work
- **WHEN** only scroll offset changes and extent/viewport stay the same
- **THEN** retained arrange, render, or hit-test work is invalidated without forcing unrelated measure work

### Requirement: Slider and ProgressBar use range primitives
Cerneala SHALL provide `Slider` and `ProgressBar` as retained controls built on range state.

#### Scenario: Slider updates value from thumb drag
- **WHEN** the slider thumb is dragged
- **THEN** `Slider.Value` updates within `Minimum` and `Maximum`

#### Scenario: Slider supports orientation
- **WHEN** slider orientation changes
- **THEN** its track and thumb layout reflect horizontal or vertical behavior

#### Scenario: ProgressBar renders value ratio
- **WHEN** progress value changes
- **THEN** retained render output reflects the value ratio without requiring input behavior

### Requirement: Additional lightweight controls are retained and template-friendly
Cerneala SHALL provide retained `RadioButton`, `Label`, `ToolTip`, and `PopupRoot` controls.

#### Scenario: RadioButton exposes checked state
- **WHEN** a radio button is clicked
- **THEN** its checked state can become true through typed property state

#### Scenario: Label hosts content
- **WHEN** a label is assigned content
- **THEN** that content participates in retained layout, rendering, hit testing, and input routing

#### Scenario: ToolTip displays retained content through popup root
- **WHEN** a tooltip is opened
- **THEN** its content is hosted under a retained popup root without backend-specific popup APIs

#### Scenario: PopupRoot overlays retained content
- **WHEN** popup root content is visible
- **THEN** it participates in retained layout, rendering, hit testing, and input routing as overlay content

### Requirement: Scrolling and range controls remain backend-neutral and tested
Cerneala SHALL keep section 16 controls backend-neutral and include focused tests for each new control area.

#### Scenario: Controls avoid concrete backend references
- **WHEN** section 16 control files are compiled
- **THEN** they do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`

#### Scenario: Required section 16 tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for range base, thumb, track, scrollbar, scroll viewer, slider, progress bar, and tooltip behavior

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
