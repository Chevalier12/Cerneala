## ADDED Requirements

### Requirement: Controls consume resource-backed images and fonts explicitly
Cerneala SHALL allow retained controls to consume image and font resources through explicit services while preserving existing direct drawing-handle APIs.

#### Scenario: Image resource replacement invalidates fixed-size render
- **WHEN** an image control uses a resource-backed image with an explicit fixed size and that image resource is replaced
- **THEN** retained render invalidation is requested without requiring measure invalidation

#### Scenario: Image resource replacement invalidates intrinsic layout
- **WHEN** an image control uses intrinsic image size and its image resource is replaced
- **THEN** retained measure and render invalidation are requested

#### Scenario: TextBlock font resource replacement invalidates text layout
- **WHEN** a `TextBlock` uses a resource-backed font and that font resource is replaced
- **THEN** retained text measurement and render invalidation are requested

#### Scenario: Controls remain backend-neutral
- **WHEN** resource-backed controls are compiled
- **THEN** `UI/Controls` does not reference MonoGame, `Texture2D`, `SpriteBatch`, Skia, or HarfBuzz
