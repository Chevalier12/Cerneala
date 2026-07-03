## ADDED Requirements

### Requirement: Layout geometry is distinct from drawing geometry
Cerneala SHALL provide layout-specific geometry types under `UI/Layout` without reusing drawing command primitives as layout state.

#### Scenario: Layout size supports unconstrained dimensions
- **WHEN** layout measures an element with unconstrained width or height
- **THEN** `LayoutSize` can represent the unconstrained dimension explicitly

#### Scenario: Layout rect stores arranged bounds
- **WHEN** an element is arranged
- **THEN** `LayoutRect` records its layout position and size independently from drawing command rectangles

#### Scenario: Layout output converts to drawing geometry only at rendering boundary
- **WHEN** retained rendering needs drawing geometry
- **THEN** layout output is converted explicitly instead of being stored as drawing primitives

### Requirement: Layout policy types exist
Cerneala SHALL provide layout policy types for margins, alignment, visibility, rounding, and layout phase contexts.

#### Scenario: Thickness represents edges
- **WHEN** margin or padding is represented
- **THEN** `Thickness` stores left, top, right, and bottom values

#### Scenario: Alignment is explicit
- **WHEN** an element has extra arranged space
- **THEN** horizontal and vertical alignment values define how the element is positioned

#### Scenario: Visibility has three states
- **WHEN** element visibility is set
- **THEN** it can be `Visible`, `Hidden`, or `Collapsed`

#### Scenario: Measure context carries available size
- **WHEN** an element is measured
- **THEN** `MeasureContext` exposes the available layout size and relevant policy

#### Scenario: Arrange context carries final rect
- **WHEN** an element is arranged
- **THEN** `ArrangeContext` exposes the final layout rectangle and relevant policy

### Requirement: UI elements participate in layout
Cerneala SHALL extend retained elements with layout state and measure/arrange entry points.

#### Scenario: Element stores desired size
- **WHEN** an element is measured
- **THEN** its desired size is stored for later arrange work

#### Scenario: Element stores arranged bounds
- **WHEN** an element is arranged
- **THEN** its arranged bounds are stored for rendering and hit-test phases

#### Scenario: Base element can be measured and arranged
- **WHEN** a plain `UIElement` is measured and arranged
- **THEN** it produces deterministic layout results without requiring a panel subclass

#### Scenario: Layout version changes on layout-affecting state
- **WHEN** a layout-affecting property changes effective value
- **THEN** the element layout version changes and layout invalidation is requested

#### Scenario: No-op property set does not invalidate layout
- **WHEN** a layout-affecting property is set to an equal effective value
- **THEN** no layout invalidation is requested

### Requirement: Layout manager processes layout queue
Cerneala SHALL provide `LayoutManager` that consumes retained layout queue work and updates element layout state.

#### Scenario: Measure queue updates desired size
- **WHEN** an element is queued for measure
- **THEN** `LayoutManager` measures it and stores its desired size

#### Scenario: Arrange queue updates arranged bounds
- **WHEN** an element is queued for arrange
- **THEN** `LayoutManager` arranges it and stores its arranged bounds

#### Scenario: Measure runs before arrange
- **WHEN** measure and arrange work are queued together
- **THEN** measure processing happens before arrange processing

#### Scenario: Cached measure is reused
- **WHEN** an element is measured again with the same available size and unchanged layout version
- **THEN** the cached desired size is reused

#### Scenario: Cached arrange is reused
- **WHEN** an element is arranged again with the same final rect and unchanged layout version
- **THEN** the cached arranged bounds are reused

#### Scenario: Bounds change invalidates render and hit testing
- **WHEN** arrange changes an element's arranged bounds
- **THEN** render and hit-test invalidation are requested for the affected element

### Requirement: Layout invalidation propagation respects boundaries
Cerneala SHALL provide layout boundary behavior that can stop upward layout propagation.

#### Scenario: Boundary stops measure propagation
- **WHEN** a child inside a layout boundary is invalidated for measure
- **THEN** measure propagation stops at the boundary element

#### Scenario: Root is a layout boundary
- **WHEN** layout invalidation reaches a retained root
- **THEN** propagation does not continue beyond the root

### Requirement: Visibility affects layout and input participation
Cerneala SHALL apply visibility semantics during layout.

#### Scenario: Visible element participates normally
- **WHEN** an element is `Visible`
- **THEN** it is measured and arranged normally

#### Scenario: Hidden element reserves layout space
- **WHEN** an element is `Hidden`
- **THEN** it is measured and arranged but later visual/input systems can exclude it

#### Scenario: Collapsed element contributes zero size
- **WHEN** an element is `Collapsed`
- **THEN** it measures to zero and is excluded from normal arrange and hit-test participation

### Requirement: Panel base lays out visual children
Cerneala SHALL provide `Panel` as a layout-capable retained element that measures and arranges visual children.

#### Scenario: Panel measures children
- **WHEN** a panel is measured
- **THEN** it measures its visual children according to panel behavior

#### Scenario: Panel arranges children
- **WHEN** a panel is arranged
- **THEN** it arranges its visual children according to panel behavior

### Requirement: Canvas positions children absolutely
Cerneala SHALL provide `Canvas` as an MVP panel for absolute child placement.

#### Scenario: Canvas measures children without constraining parent size from child position
- **WHEN** a canvas is measured
- **THEN** child desired sizes are measured without making child offsets force canvas desired size unless explicitly configured

#### Scenario: Canvas arranges child at coordinates
- **WHEN** a child has canvas coordinates
- **THEN** canvas arranges the child at those coordinates

### Requirement: StackPanel lays out children sequentially
Cerneala SHALL provide `StackPanel` with explicit orientation.

#### Scenario: Vertical stack accumulates height
- **WHEN** a vertical stack panel is measured
- **THEN** its desired height is the sum of visible child desired heights and its desired width is the maximum child width

#### Scenario: Horizontal stack accumulates width
- **WHEN** a horizontal stack panel is measured
- **THEN** its desired width is the sum of visible child desired widths and its desired height is the maximum child height

#### Scenario: Stack panel arranges in order
- **WHEN** a stack panel arranges children
- **THEN** visible children are arranged in retained visual child order

### Requirement: Layout system is tested
Cerneala SHALL include focused tests for layout primitives, layout manager, element measure/arrange, invalidation, visibility, canvas, and stack panel behavior.

#### Scenario: Required layout tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist under `tests/Cerneala.Tests/UI/Layout` for primitives, manager, element layout, invalidation, visibility, canvas, and stack panel behavior

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
