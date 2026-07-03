## ADDED Requirements

### Requirement: Control base exposes common typed properties
Cerneala SHALL provide `Control` as the retained control base with common visual and text properties backed by typed property metadata.

#### Scenario: Control exposes visual properties
- **WHEN** a retained control is created
- **THEN** it exposes background, foreground, border color, border thickness, and padding typed properties

#### Scenario: Control exposes text properties
- **WHEN** a retained control is created
- **THEN** it exposes font family and font size typed properties suitable for text controls

#### Scenario: Visual property changes invalidate render
- **WHEN** a render-affecting control property changes
- **THEN** retained render invalidation is requested without forcing measure unless metrics or layout are affected

#### Scenario: Layout property changes invalidate measure
- **WHEN** padding, border thickness, font family, or font size changes layout metrics
- **THEN** retained measure and render invalidation are requested

### Requirement: Content and decorator controls own retained children
Cerneala SHALL provide content and decorator controls that expose retained child content through logical and visual relationships.

#### Scenario: ContentControl owns UIElement content
- **WHEN** a `UIElement` is assigned as `ContentControl.Content`
- **THEN** the element is owned as logical and visual content for layout, rendering, hit testing, and input routing

#### Scenario: ContentControl replaces old content
- **WHEN** content is replaced
- **THEN** the old retained child is removed before the new retained child is attached

#### Scenario: Decorator owns one child
- **WHEN** a `Decorator.Child` is assigned
- **THEN** the child participates in measure, arrange, render, hit testing, and input routing

### Requirement: Border renders fill and stroke
Cerneala SHALL provide `Border` as a retained decorator that renders fill and border stroke using existing drawing rectangle commands.

#### Scenario: Border measures child with padding and border
- **WHEN** a border with child content is measured
- **THEN** desired size includes child desired size, padding, and border thickness

#### Scenario: Border arranges child inside padding and border
- **WHEN** a border is arranged
- **THEN** its child is arranged inside the inner content rectangle

#### Scenario: Border renders background and border
- **WHEN** a border has background or border properties
- **THEN** it emits drawing commands through retained rendering without backend-specific dependencies

### Requirement: TextBlock displays retained text
Cerneala SHALL provide `TextBlock` as a retained text display control that measures and renders text through a backend-neutral text seam above existing drawing text primitives.

#### Scenario: TextBlock measures text
- **WHEN** text content is set on a `TextBlock`
- **THEN** measure returns a deterministic desired size based on the text, font family, and font size

#### Scenario: TextBlock renders text
- **WHEN** a text block is render-dirty
- **THEN** it emits text drawing commands through `DrawingContext`

#### Scenario: Text content invalidates metrics and render
- **WHEN** `TextBlock.Text` changes
- **THEN** text metrics and render commands are invalidated

#### Scenario: Text color invalidates render only
- **WHEN** text foreground changes without changing metrics
- **THEN** render is invalidated without forcing measure

### Requirement: Image displays draw images
Cerneala SHALL provide `Image` as a retained image display control backed by existing `IDrawImage` handles.

#### Scenario: Image source renders through drawing commands
- **WHEN** an image has a source
- **THEN** it emits image drawing commands through retained rendering

#### Scenario: Image source change invalidates render
- **WHEN** the image source changes
- **THEN** retained render invalidation is requested

#### Scenario: Intrinsic image size can affect measure
- **WHEN** an image uses intrinsic size behavior
- **THEN** image source changes can invalidate measure as well as render

### Requirement: Controls-facing panels reuse retained layout behavior
Cerneala SHALL expose controls-facing `Panel`, `Canvas`, and `StackPanel` types that use the existing retained panel layout behavior.

#### Scenario: Controls panel lays out visual children
- **WHEN** a controls-facing panel measures or arranges
- **THEN** it uses the same retained visual child layout behavior as the existing layout panel base

#### Scenario: Controls canvas positions children
- **WHEN** a controls-facing canvas arranges children with canvas coordinates
- **THEN** child bounds match existing retained canvas behavior

#### Scenario: Controls stack panel arranges in order
- **WHEN** a controls-facing stack panel arranges children
- **THEN** children are arranged in retained visual child order according to orientation

### Requirement: Button is a concrete retained command control
Cerneala SHALL provide `Button` as a concrete retained control built on `ButtonBase`.

#### Scenario: Button can be added to root
- **WHEN** a `Button` is added to a `UIRoot`
- **THEN** it participates in retained attach, layout, rendering, hit testing, input routing, and command routing

#### Scenario: Button renders visual state
- **WHEN** hover, pressed, focused, disabled, or command state affects button output
- **THEN** the button invalidates and regenerates only render work needed for visible output

#### Scenario: Button click executes command
- **WHEN** a valid retained click occurs on a command-bound button
- **THEN** the button command executes through existing direct or routed command behavior

### Requirement: First controls are backend-neutral
Cerneala SHALL keep controls independent of concrete drawing and platform adapters.

#### Scenario: Controls avoid backend references
- **WHEN** controls are compiled
- **THEN** `UI/Controls` does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`

#### Scenario: Controls render through retained drawing abstractions
- **WHEN** controls render
- **THEN** they use retained `RenderContext` and existing drawing command abstractions

### Requirement: First controls are tested
Cerneala SHALL include focused tests for the first retained controls and panels.

#### Scenario: Required control tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for `Control`, `ContentControl`, `Decorator`, `Border`, controls-facing panels, `TextBlock`, `Image`, `ButtonBase`, and `Button`

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
