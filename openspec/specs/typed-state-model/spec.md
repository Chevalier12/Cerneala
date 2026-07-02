# Typed State Model

## Purpose

Defines the strongly typed retained UI property/state system used by Cerneala UI objects.

## Requirements

### Requirement: Typed state model exists
Cerneala SHALL provide a typed state model for retained UI objects under `UI/Core`.

#### Scenario: UI object owns typed state
- **WHEN** a retained UI object needs property-backed state
- **THEN** it can derive from or use `UiObject` to store values keyed by `UiProperty`

#### Scenario: User code avoids casts
- **WHEN** user code sets or reads a value through `UiProperty<T>`
- **THEN** the public API accepts and returns `T` without requiring casts in user code

### Requirement: Properties are explicitly registered
Cerneala SHALL register UI properties explicitly and deterministically.

#### Scenario: Property registration records identity
- **WHEN** a property is registered
- **THEN** the registry records owner type, property name, value type, metadata, and a stable diagnostic identity

#### Scenario: Duplicate registrations are rejected
- **WHEN** the same owner type and property name are registered more than once
- **THEN** registration fails deterministically

### Requirement: Property metadata controls value behavior
Cerneala SHALL define typed metadata for default values, equality, validation, coercion, and invalidation options.

#### Scenario: Default value is used
- **WHEN** no higher-precedence value exists for a property
- **THEN** the property returns its metadata default value

#### Scenario: Validation rejects invalid input
- **WHEN** a value fails the registered validation delegate
- **THEN** the value is not stored as the effective property value

#### Scenario: Coercion adjusts input before storage
- **WHEN** a value is set on a property with coercion metadata
- **THEN** the coerced value is used for effective value comparison, storage, and notifications

#### Scenario: Equal value is ignored
- **WHEN** a set operation produces an effective value equal to the current effective value according to metadata equality
- **THEN** no property changed notification is raised and no invalidation hook is emitted

### Requirement: Value precedence is explicit
Cerneala SHALL evaluate property values using explicit value source precedence.

#### Scenario: Local value wins over lower sources
- **WHEN** local, animation, style visual state, style base, inherited, and default values all exist
- **THEN** the effective value is the local value

#### Scenario: Animation wins below local
- **WHEN** no local value exists but animation and lower-precedence values exist
- **THEN** the effective value is the animation value

#### Scenario: Style visual state wins below animation
- **WHEN** no local or animation value exists but style visual state and lower-precedence values exist
- **THEN** the effective value is the style visual state value

#### Scenario: Style base wins below visual state
- **WHEN** only style base, inherited, and default values exist
- **THEN** the effective value is the style base value

#### Scenario: Inherited wins over default
- **WHEN** only inherited and default values exist
- **THEN** the effective value is the inherited value

### Requirement: Read-only properties use keys
Cerneala SHALL support read-only or owner-only properties through `UiPropertyKey<T>`.

#### Scenario: Public set is rejected
- **WHEN** caller code tries to set a read-only property without its key
- **THEN** the set operation is rejected

#### Scenario: Public clear is rejected
- **WHEN** caller code tries to clear a read-only property without its key
- **THEN** the clear operation is rejected

#### Scenario: Owner key can set value
- **WHEN** owner code sets a read-only property through `UiPropertyKey<T>`
- **THEN** the value is accepted subject to validation, coercion, equality, and notification rules

### Requirement: Property changes expose typed and diagnostic event data
Cerneala SHALL expose property changed event data for typed callbacks and non-generic diagnostics.

#### Scenario: Typed change data is available
- **WHEN** a `UiProperty<T>` effective value changes
- **THEN** typed change handlers can read old and new values as `T`

#### Scenario: Diagnostic change data is available
- **WHEN** diagnostics inspect a property change
- **THEN** non-generic change data exposes the property descriptor, old value, new value, and effective value source

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

### Requirement: Typed state model is tested
Cerneala SHALL include focused tests for the typed state model.

#### Scenario: Required test files exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for properties, registry behavior, store behavior, invalidation hooks, read-only properties, and inherited properties

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
