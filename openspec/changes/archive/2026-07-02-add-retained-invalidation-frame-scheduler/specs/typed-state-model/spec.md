## MODIFIED Requirements

### Requirement: Property options drive invalidation hooks
Cerneala SHALL expose metadata-driven invalidation hooks without coupling `UI/Core` to layout, rendering, input, retained invalidation queues, frame scheduling, or backend adapters.

#### Scenario: Measure option is reported
- **WHEN** a changed property has `AffectsMeasure`
- **THEN** the owning object receives a measure-affecting invalidation hook

#### Scenario: Render option is reported
- **WHEN** a changed property has `AffectsRender`
- **THEN** the owning object receives a render-affecting invalidation hook without requiring measure invalidation

#### Scenario: Hit test option is reported
- **WHEN** a changed property has `AffectsHitTest`
- **THEN** the owning object receives a hit-test-affecting invalidation hook

#### Scenario: Retained elements translate property options
- **WHEN** a `UIElement` receives a typed property invalidation hook
- **THEN** the retained element translates the property options into retained invalidation flags through the retained invalidation boundary

#### Scenario: Non-invalidation options are not reported
- **WHEN** a changed property has only non-invalidation options such as `Inherits`
- **THEN** the owning object does not receive an invalidation hook

#### Scenario: Core stays backend neutral
- **WHEN** the typed state model is implemented
- **THEN** `UI/Core` does not reference MonoGame, Skia, HarfBuzz, retained invalidation queues, frame scheduling, or backend-specific types
