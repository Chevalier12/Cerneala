# advanced-rendering-media Specification

## Purpose
TBD - created by archiving change add-advanced-rendering-media. Update Purpose after archive.
## Requirements
### Requirement: Media descriptors are backend-neutral
Cerneala SHALL provide media descriptors for brushes, pens, geometries, transforms, opacity layers, shadows, and image sources without referencing concrete drawing backends.

#### Scenario: Media descriptors avoid backend types
- **WHEN** media descriptor code is compiled
- **THEN** it does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backend types

#### Scenario: Media descriptors expose stable value identity
- **WHEN** two media descriptors are created with the same public values
- **THEN** their equality or identity behavior is deterministic enough for render dependency tests

### Requirement: Brushes describe paint intent
Cerneala SHALL provide brush types for solid colors and gradients that describe paint intent separately from backend rendering objects.

#### Scenario: Solid brush bridges to drawing color
- **WHEN** a solid color brush is used by a retained shape
- **THEN** the shape can emit existing color-backed drawing commands where the primitive supports solid paint

#### Scenario: Gradient brushes keep validated stops
- **WHEN** a linear or radial gradient brush is created
- **THEN** its gradient stops are validated, ordered, and exposed without backend-specific resources

### Requirement: Pens describe stroke intent
Cerneala SHALL provide `Pen` as a stroke descriptor with brush and thickness data suitable for retained render dependencies.

#### Scenario: Pen validates thickness
- **WHEN** a pen is created
- **THEN** invalid, negative, NaN, or infinite thickness values are rejected

#### Scenario: Pen carries brush identity
- **WHEN** a shape stroke changes to a different pen brush or thickness
- **THEN** retained render invalidation can treat the stroke as changed

### Requirement: Geometries describe shape bounds and path data
Cerneala SHALL provide rectangle, ellipse, and path geometries that expose bounds and geometry-specific data without backend-specific objects.

#### Scenario: Rectangle geometry exposes bounds
- **WHEN** a rectangle geometry is created
- **THEN** it exposes the rectangle bounds used for measurement and drawing

#### Scenario: Ellipse geometry exposes bounds
- **WHEN** an ellipse geometry is created
- **THEN** it exposes the bounding rectangle used for measurement and drawing

#### Scenario: Path geometry keeps structured segments
- **WHEN** a path geometry is created from path figures or segments
- **THEN** it exposes structured path data rather than a backend-native path object

### Requirement: Transforms are backend-neutral value data
Cerneala SHALL provide transform and matrix value types that can be composed and applied to points without requiring a backend matrix type.

#### Scenario: Identity transform leaves point unchanged
- **WHEN** an identity transform is applied to a point
- **THEN** the resulting point matches the input point

#### Scenario: Matrix composition is deterministic
- **WHEN** two transforms are composed
- **THEN** applying the composed transform produces deterministic coordinates

### Requirement: Drawing command expansion is tested and adapter-covered
Cerneala SHALL add new draw command kinds only when command creation, command list behavior, and backend adapter behavior are covered by tests.

#### Scenario: Advanced draw command has a drawing context method
- **WHEN** a new advanced draw command kind is added
- **THEN** `DrawingContext` exposes a corresponding method that records that command

#### Scenario: Advanced draw command has adapter behavior
- **WHEN** an advanced draw command is submitted to the MonoGame drawing backend
- **THEN** the backend either renders it through a supported primitive or fails with an intentional tested exception

### Requirement: Shape controls render through retained drawing
Cerneala SHALL provide retained shape controls for rectangle, ellipse, and path content that render through `RenderContext` and drawing commands.

#### Scenario: Rectangle shape renders fill and stroke
- **WHEN** a rectangle shape has fill or stroke paint
- **THEN** it records retained drawing commands for its arranged bounds

#### Scenario: Ellipse shape renders fill and stroke
- **WHEN** an ellipse shape has fill or stroke paint
- **THEN** it records retained drawing commands for its arranged bounds

#### Scenario: Path shape renders from geometry data
- **WHEN** a path shape has path data and paint
- **THEN** it records retained drawing commands from backend-neutral path geometry data

#### Scenario: Shape property changes invalidate render
- **WHEN** a shape fill, stroke, stroke thickness, geometry, transform, opacity, or shadow changes
- **THEN** retained render invalidation is requested

### Requirement: Image sources add UI media identity above draw images
Cerneala SHALL provide image source abstractions that own image identity, intrinsic metadata, and draw image resolution without replacing `IDrawImage` as the backend handle.

#### Scenario: Bitmap image exposes source identity
- **WHEN** a bitmap image source is created
- **THEN** it exposes stable source identity and intrinsic size metadata

#### Scenario: Render target image wraps draw handle metadata
- **WHEN** a render target image source is created
- **THEN** it exposes intrinsic size and resolves to an `IDrawImage` without exposing backend concrete types

### Requirement: Advanced rendering and media is tested
Cerneala SHALL include focused tests for advanced draw commands, brushes, geometries, transforms, shape controls, and image sources.

#### Scenario: Required advanced rendering tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist under `tests/Cerneala.Tests/Drawing`, `tests/Cerneala.Tests/UI/Media`, and `tests/Cerneala.Tests/UI/Controls/Shapes`

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

