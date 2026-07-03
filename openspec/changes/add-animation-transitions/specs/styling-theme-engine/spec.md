## ADDED Requirements

### Requirement: Styles can describe transitions without owning animation runtime
Cerneala SHALL provide style transition descriptors that reference typed properties, durations, easing, and interpolation without directly ticking animations.

#### Scenario: Style transition targets typed property
- **WHEN** a style transition is created
- **THEN** it references a typed `UiProperty<T>` and typed interpolation contract

#### Scenario: Style transition remains backend-neutral
- **WHEN** style transition code is compiled
- **THEN** it does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backends
