## 1. Core Property Descriptors

- [x] 1.1 Add `UI/Core/UiProperty.cs`; done when it provides stable property identity, owner/name/value-type metadata, diagnostic names, and duplicate-safe identity behavior.
- [x] 1.2 Add `UI/Core/UiProperty{T}.cs`; done when user code can register and access strongly typed property descriptors without casts.
- [x] 1.3 Add `UI/Core/UiPropertyKey{T}.cs`; done when read-only or owner-only properties can be set only through their key.
- [x] 1.4 Add `UI/Core/UiPropertyMetadata{T}.cs`; done when metadata carries default value, equality, validation, coercion, and property options.
- [x] 1.5 Add `UI/Core/UiPropertyOptions.cs`; done when it defines `[Flags]` for `AffectsMeasure`, `AffectsArrange`, `AffectsRender`, `AffectsHitTest`, `AffectsStyle`, `Inherits`, and `ReadOnly`.
- [x] 1.6 Add `UI/Core/UiPropertyValueSource.cs`; done when it represents `Local`, `Animation`, `StyleVisualState`, `StyleBase`, `Inherited`, and `Default` in explicit precedence order.

## 2. Property Storage And Owners

- [x] 2.1 Add `UI/Core/UiObject.cs`; done when it owns typed property storage and exposes get, set, clear, and lifecycle/change hooks.
- [x] 2.2 Add `UI/Core/UiPropertyStore.cs`; done when effective values are stored compactly by `UiProperty` identity and value source.
- [x] 2.3 Add `UI/Core/IUiPropertyOwner.cs`; done when property changes can report invalidation-relevant metadata without coupling `UI/Core` to layout or rendering systems.
- [x] 2.4 Add `UI/Core/Unset.cs`; done when internal unset state exists without exposing public magic values.

## 3. Registration, Validation, And Coercion

- [x] 3.1 Add `UI/Core/UiPropertyRegistry.cs`; done when explicit registration is deterministic and duplicate owner/name registrations are rejected.
- [x] 3.2 Add `UI/Core/ValidateValue.cs`; done when optional typed validation is available without reflection-heavy code.
- [x] 3.3 Add `UI/Core/CoerceValue.cs`; done when optional typed coercion is available and runs before effective value comparison.
- [x] 3.4 Decide and document invalid value behavior; done when tests prove whether validation throws or reports structured diagnostics.

## 4. Change Notifications And Invalidation Hooks

- [x] 4.1 Add `UI/Core/UiPropertyChangedEventArgs.cs`; done when non-generic diagnostics can inspect changed property, old value, new value, and value source.
- [x] 4.2 Add `UI/Core/UiPropertyChangedEventArgs{T}.cs`; done when typed change callbacks can read old/new values without casts.
- [x] 4.3 Emit metadata-driven invalidation hooks; done when fake owners can observe `AffectsMeasure`, `AffectsArrange`, `AffectsRender`, `AffectsHitTest`, and `AffectsStyle`.
- [x] 4.4 Avoid no-op work for equal values; done when setting an equal effective value fires no notification and emits no invalidation hook.

## 5. Tests

- [x] 5.1 Add `tests/Cerneala.Tests/UI/Core/UiPropertyTests.cs`; done when typed registration, metadata defaults, validation, coercion, and typed get/set behavior are covered.
- [x] 5.2 Add `tests/Cerneala.Tests/UI/Core/UiPropertyRegistryTests.cs`; done when duplicate detection and deterministic registration behavior are covered.
- [x] 5.3 Add `tests/Cerneala.Tests/UI/Core/UiPropertyStoreTests.cs`; done when value source precedence and clearing values are covered.
- [x] 5.4 Add `tests/Cerneala.Tests/UI/Core/UiPropertyInvalidationTests.cs`; done when metadata options trigger only the expected owner hooks.
- [x] 5.5 Add `tests/Cerneala.Tests/UI/Core/ReadOnlyUiPropertyTests.cs`; done when public set is rejected and key-based owner set works.
- [x] 5.6 Add `tests/Cerneala.Tests/UI/Core/InheritedUiPropertyTests.cs`; done when inherited source behavior is represented and lower precedence than style/local sources.

## 6. Validation

- [x] 6.1 Run `dotnet test`; done when the full test suite passes.
- [x] 6.2 Run `openspec validate add-typed-state-model`; done when the OpenSpec change validates successfully.
- [x] 6.3 Review `git status --short`; done when changed files are understood and no unrelated edits were made.
