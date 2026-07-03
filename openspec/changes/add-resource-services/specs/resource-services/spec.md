## ADDED Requirements

### Requirement: Resource ids are typed
Cerneala SHALL provide typed resource identifiers that prevent mixing resource categories at API boundaries.

#### Scenario: Resource id carries type and key
- **WHEN** a resource id is created for a resource type
- **THEN** the id stores the resource key and the expected resource value type

#### Scenario: Different resource types do not compare equal
- **WHEN** two resource ids use the same key but different resource types
- **THEN** they are not considered interchangeable by typed resource APIs

### Requirement: Resource provider resolves resources explicitly
Cerneala SHALL provide explicit resource lookup APIs without hidden global state.

#### Scenario: Resource provider returns typed resource
- **WHEN** a resource value is requested by `ResourceId<T>`
- **THEN** the provider returns a value assignable to `T` or reports that the resource is missing

#### Scenario: Resource lookup is explicit
- **WHEN** a control or service resolves a resource
- **THEN** it uses a provided `IResourceProvider` or `ResourceStore` rather than a static global lookup

### Requirement: Resource store publishes replacement events
Cerneala SHALL provide `ResourceStore` that stores typed resources and notifies observers when a resource is replaced.

#### Scenario: Replacing resource raises change event
- **WHEN** an existing resource id receives a new value
- **THEN** a `ResourceChangedEventArgs` event identifies the resource id, old value, new value, and new version

#### Scenario: No-op replacement does not notify
- **WHEN** a resource is set to the same effective value according to store equality
- **THEN** no resource change event is raised

### Requirement: Resource dependencies are tracked
Cerneala SHALL provide `ResourceDependencyTracker` for connecting retained consumers to resource ids and invalidation versions.

#### Scenario: Tracker records resource dependency
- **WHEN** a retained element records a dependency on a resource id
- **THEN** the tracker can later identify that element as dependent on that resource

#### Scenario: Resource replacement increments dependency version
- **WHEN** a tracked resource changes
- **THEN** dependent resource versions change so retained render caches can become stale

### Requirement: Font resources resolve to drawing fonts
Cerneala SHALL provide `FontResource` values that resolve to `IDrawFont` through explicit font services.

#### Scenario: Font resource returns draw font
- **WHEN** a font resource is resolved for text layout or rendering
- **THEN** it returns an `IDrawFont` suitable for `DrawTextRun`

#### Scenario: Font replacement changes metrics dependency
- **WHEN** a font resource used by text changes
- **THEN** dependent text measurement and render work are invalidated

### Requirement: Image resources resolve to drawing images
Cerneala SHALL provide `ImageResource` values and `IImageLoader` abstractions that resolve images to `IDrawImage`.

#### Scenario: Image resource returns draw image
- **WHEN** an image resource is resolved
- **THEN** it returns an `IDrawImage` without exposing backend-specific image objects to controls

#### Scenario: Image loader is explicit
- **WHEN** an image resource needs to load image content
- **THEN** it uses an injected `IImageLoader` rather than hidden platform state

### Requirement: MonoGame image loader stays adapter-scoped
Cerneala SHALL provide a MonoGame image loader adapter without leaking MonoGame types into controls or core resource services.

#### Scenario: MonoGame loader returns draw image
- **WHEN** the MonoGame image loader loads an image
- **THEN** it returns an `IDrawImage` or `MonoGameImage`

#### Scenario: Controls avoid MonoGame image types
- **WHEN** controls and core resource services are compiled
- **THEN** they do not reference `Texture2D`, `SpriteBatch`, or MonoGame-specific image APIs

### Requirement: Resource services are tested
Cerneala SHALL include focused tests for resource ids, stores, dependency tracking, and image/font invalidation.

#### Scenario: Required resource tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for `ResourceId`, `ResourceStore`, `ResourceDependencyTracker`, image resource invalidation, and font resource invalidation

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
