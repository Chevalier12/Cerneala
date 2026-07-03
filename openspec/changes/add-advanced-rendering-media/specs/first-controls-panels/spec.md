## ADDED Requirements

### Requirement: Shape controls preserve controls boundary
Cerneala SHALL provide retained shape controls that participate in the existing controls, layout, rendering, hit testing, and invalidation model without backend-specific dependencies.

#### Scenario: Shape controls are retained elements
- **WHEN** a shape control is added to a retained root or panel
- **THEN** it participates in retained measure, arrange, rendering, hit testing, and invalidation

#### Scenario: Shape controls avoid backend references
- **WHEN** shape controls are compiled
- **THEN** `UI/Controls/Shapes` does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`

#### Scenario: Shape controls render through render context
- **WHEN** a shape control renders
- **THEN** it records commands through `RenderContext.DrawingContext` instead of calling a backend directly
