## Context

`ROADMAPv2.md` defines Cerneala as a modern retained UI framework, not a WPF clone. The typed state model is the first runtime foundation required by later retained UI work.

The existing `UI/Drawing` layer records backend-neutral draw commands. The existing `UI/Input` layer provides snapshots, routed event concepts, and command primitives. The typed state model must sit above those foundations and must not make drawing or input own UI state.

## Goals / Non-Goals

**Goals:**

- Provide a strongly typed property system for retained UI objects.
- Keep registration explicit, deterministic, and testable.
- Support read-only or owner-only properties through keys.
- Support metadata-driven invalidation effects.
- Support validation and coercion without reflection-heavy behavior.
- Support value precedence required by future styling, animation, and inheritance.
- Provide focused unit tests for registration, storage, invalidation, read-only properties, and inherited properties.

**Non-Goals:**

- Do not implement the retained element tree in this change.
- Do not implement layout, render caching, styling, templates, binding, resources, or controls in this change.
- Do not recreate WPF `DependencyObject` or `DependencyProperty` compatibility.
- Do not use XAML or markup as the source of truth.
- Do not add backend references from `UI/Core`.

## Decisions

### Decision: Use `UiObject` as the typed state owner base

`UiObject` owns a `UiPropertyStore` and exposes typed get/set/clear APIs. Later `UIElement`, controls, resources, and styleable objects can derive from it when they need retained state.

Alternative considered: put property storage directly on `UIElement`. That is simpler for the first tree slice, but it prevents non-element UI objects from using the same state model.

### Decision: Use explicit `UiProperty<T>` registration

Properties are registered through `UiProperty.Register<T>`-style APIs or equivalent explicit factory methods. Registration records owner type, property name, value type, default metadata, and duplicate detection data.

Alternative considered: convention/reflection-based registration. That makes early code shorter but hides failure modes and makes deterministic tests harder.

Implementation choice: registration requires the owner type when the property is created. Shared or attached property semantics can be added later only when a real feature needs them.

### Decision: Keep a non-generic `UiProperty` descriptor base

The store and diagnostics need property identity without generic casts. Public user code should use `UiProperty<T>` for typed access, while internal indexing can use the non-generic base.

Alternative considered: generic descriptors only. That keeps the public surface smaller but complicates store internals and diagnostics.

### Decision: Metadata owns default value and invalidation effects

`UiPropertyMetadata<T>` stores the default value, equality comparer or equality delegate, optional validation, optional coercion, and `UiPropertyOptions`.

`UiPropertyOptions` includes:

- `AffectsMeasure`
- `AffectsArrange`
- `AffectsRender`
- `AffectsHitTest`
- `AffectsStyle`
- `Inherits`
- `ReadOnly`

Metadata is how later retained invalidation avoids ad hoc per-property branching.

### Decision: Implement full value precedence contract from the start

The value source order is:

`local > animation > style visual state > style base > inherited > default`

The first implementation may expose only the sources required by current tests, but the store and public enum must be shaped so later styling and animation do not require redesign.

Alternative considered: local/default only. That is tempting, but it creates churn as soon as styles or visual states arrive.

### Decision: Read-only properties use `UiPropertyKey<T>`

Public callers can read a read-only property through `UiProperty<T>`, but setting requires the owner-held `UiPropertyKey<T>`.

Alternative considered: internal setters on `UiObject`. That leaks ownership rules into every type and is easier to misuse.

Implementation choice: public set attempts on read-only properties throw `InvalidOperationException`.

### Decision: Invalid values throw immediately

Validation failures throw `ArgumentException` and do not update stored or effective values.

Alternative considered: structured diagnostics for invalid user-set values. That can be added later around developer tooling, but exceptions are simpler and match the current API style.

### Decision: Equal values do not notify or invalidate

Setting a value equal to the effective current value does not fire change notifications and does not enqueue invalidation work.

Equality is controlled by metadata so value types, reference types, and custom semantic equality can be handled consistently.

### Decision: Invalidation is emitted through an abstraction

`UiObject` should not directly depend on layout, render queues, or backend types. It may call an `IUiPropertyOwner` or similar internal hook that later `UIElement` can implement to translate metadata options into retained invalidation.

This keeps `UI/Core` reusable and prevents the first state model from reaching into systems that do not exist yet.

### Decision: Inherited values are represented as an explicit source

The typed state store supports `UiPropertyValueSource.Inherited` now. Later retained tree work will decide how inherited source values are computed and propagated from parent elements.

## Risks / Trade-offs

- [Risk] Implementing precedence too early can overcomplicate the store. -> Mitigation: define all value source names now, but keep storage compact and test only required active behavior.
- [Risk] Invalidation hooks may be too abstract before `UIElement` exists. -> Mitigation: keep the hook minimal and prove it with fake owners in unit tests.
- [Risk] Coercion and validation can create ordering bugs. -> Mitigation: test validation-before-store, coercion-before-equality, and unchanged effective value behavior explicitly.
- [Risk] Inherited values need a tree that does not exist yet. -> Mitigation: expose the source contract and test it with controlled fake parent/value providers until the retained tree exists.

## Migration Plan

Implementation has started and should continue until validation is green.

Implementation steps:

1. Add `UI/Core` production files.
2. Add focused `tests/Cerneala.Tests/UI/Core` coverage.
3. Run the test suite.
4. Mark validation tasks complete only after runtime behavior exists and tests pass.

Rollback is removing the OpenSpec change if the typed state direction is rejected before implementation.

## Open Questions

- Should later shared or attached property semantics use the same registry or a separate owner model?
- Should future developer tooling add structured diagnostics around validation exceptions?
- How should the retained tree propagate inherited source updates across logical and visual boundaries?
