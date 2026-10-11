# Property System

> Code: `UI/Core`, `UI/Elements/InheritedPropertyPropagator.cs`, `UI/Elements/RootPropertyMutationObserver.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

The property system stores typed values on `UiObject` instances, picks one
effective value from several sources, and reports changes of that effective
value to the retained pipeline. It does not schedule or run layout, rendering,
or Aspect work itself. It only tells the owner which work a change affects.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `UiProperty` / `UiProperty<T>` | `UI/Core/UiProperty.cs`, `UI/Core/UiProperty{T}.cs` | Immutable descriptor: name, owner type, value type, options, metadata. Created by `Register` or `RegisterReadOnly`. | Process (static field on the owner type) |
| `UiPropertyMetadata<T>` | `UI/Core/UiPropertyMetadata{T}.cs` | Default value, `UiPropertyOptions`, equality comparer, `ValidateValue<T>`, `CoerceValue<T>`. | The descriptor |
| `UiPropertyOptions` | `UI/Core/UiPropertyOptions.cs` | Flags: `AffectsMeasure`, `AffectsArrange`, `AffectsRender`, `AffectsHitTest`, `AffectsAspect`, `AffectsInputVisual`, `AffectsSemantics`, `Inherits`, `ReadOnly`. | The metadata |
| `UiPropertyKey<T>` | `UI/Core/UiPropertyKey{T}.cs` | Write capability for a read-only property. Only `RegisterReadOnly` can create one. | The type that registered the property |
| `UiPropertyRegistry` | `UI/Core/UiPropertyRegistry.cs` | Process-wide list of all properties, keyed by owner type and name, guarded by a lock. | Process |
| `UiPropertyValueSource` | `UI/Core/UiPropertyValueSource.cs` | The value sources: `Default`, `Inherited`, `TemplateBinding`, `AspectBase`, `AspectVisualState`, `TemplateOwnerBinding`, `MarkupBase`, `MarkupConditional`, `Animation`, `Local`. | — |
| `UiPropertyStore` | `UI/Core/UiPropertyStore.cs` | Per-object storage: one value per stored source, plus framework defaults. Resolves the effective value. | One `UiObject` |
| `UiObject` | `UI/Core/UiObject.cs` | Public read/write API, coercion, validation, change notification. | Application or parent element |
| `IUiPropertyOwner` | `UI/Core/IUiPropertyOwner.cs` | Receives `OnPropertyInvalidated` when an effective value changes. `UIElement` implements it. | — |
| `UiPropertyMutationObserver` | `UI/Core/UiPropertyMutationObserver.cs` | Internal hook that sees every write, including writes that do not change the effective value. | The root |
| `RootPropertyMutationObserver` | `UI/Elements/RootPropertyMutationObserver.cs` | Forwards every write to Motion layout, Motion transactions, and `AspectProcessor`. | `UIRoot` |
| `InheritedPropertyPropagator` | `UI/Elements/InheritedPropertyPropagator.cs` | Copies inherited values from a parent to its visual subtree. | `UIRoot` |

## Value Sources And Precedence

`UiPropertyStore` checks the stored sources in this order. The first stored
source wins (`UiPropertyStore.EffectiveOrder`):

| Rank | Source | Typical writer |
|---|---|---|
| 1 (highest) | `Local` | Application code: `SetValue(property, value)` without a source |
| 2 | `Animation` | Motion (`MotionPropertyStore`) |
| 3 | `MarkupConditional` | Generated markup conditions |
| 4 | `MarkupBase` | Generated markup |
| 5 | `TemplateOwnerBinding` | Template parts bound to their owner |
| 6 | `AspectVisualState` | Aspect state rules |
| 7 | `AspectBase` | Aspect base rules |
| 8 | `TemplateBinding` | Template bindings |
| 9 | `Inherited` | `InheritedPropertyPropagator` |
| — | `Default` | Not stored |

When no source is stored, the store returns a framework default if one was set
with the internal `UiObject.SetFrameworkDefault`, otherwise the metadata
default. Both report the source `Default`. Storing `Default` throws
`ArgumentOutOfRangeException`.

The numeric values of the enum are not the precedence. For example,
`TemplateBinding` is `2` and `AspectBase` is `3`, but `AspectBase` wins
over `TemplateBinding` only because of the order above, and
`TemplateOwnerBinding` (`5`) wins over `AspectVisualState` (`4`).

Example. A `Button` has:

```csharp
button.SetValue(Control.ForegroundProperty, new SolidColorBrush(Color.Red), UiPropertyValueSource.AspectBase);
button.SetValue(Control.ForegroundProperty, new SolidColorBrush(Color.Blue));
button.GetValueSource(Control.ForegroundProperty); // Local
button.ClearValue(Control.ForegroundProperty);
button.GetValueSource(Control.ForegroundProperty); // AspectBase, the brush is red again
```

`ClearValue` removes only the `Local` slot. The `AspectBase` value was stored
the whole time and becomes effective again.

## Data Flow

Writing a value with `UiObject.SetValue<T>(property, value, source)`:

1. Read-only check. `SetValue(UiProperty<T>, …)` throws
   `InvalidOperationException` for a read-only property.
   `SetValue(UiPropertyKey<T>, …)` skips this check.
2. `VerifyMutationAccess`, a virtual hook that is empty on `UiObject`.
3. Coerce with `CoerceValue<T>`, then validate with `ValidateValue<T>`, then
   the owner's `ValidatePropertyMutation`. A failed validation throws
   `ArgumentException` and stores nothing.
4. Store the coerced value in the slot for `source` and recompute the
   effective value.
5. If the effective value changed (by the metadata's `EqualityComparer`):
   raise `PropertyChanged`, notify the mutation observer, then call
   `IUiPropertyOwner.OnPropertyInvalidated` with the property's invalidation
   options.
6. If the effective value did not change: notify only the mutation observer.
   No `PropertyChanged`, no invalidation.

`UIElement.OnPropertyInvalidated` maps the options to `InvalidationFlags`
(`AffectsMeasure` → `Measure`, `AffectsRender` → `Render`, `Inherits` →
`Inherited`, and so on) and calls `UIElement.Invalidate`. From there,
`DirtyPropagation` marks dirty state and fills the frame queues. See
[the frame order](overview.md#invalidation-and-frame-scheduling).

Example. `Control.FontSizeProperty` has `Inherits | AffectsMeasure |
AffectsRender`. Setting `FontSize` from `16` to `20` on an attached `Button`:

- raises `PropertyChanged` once on the button;
- enqueues the button for `Measure`, `Arrange`, and `Render` (measure adds
  arrange and render), and enqueues its visual ancestors for `Measure` and
  `Arrange` up to the nearest layout boundary;
- enqueues the button, and only the button, in the `InheritedPropertyQueue`.
  The descendants get the new size later, when
  `FramePhase.InheritedProperties` walks the button's visual subtree.

Setting `FontSize` to `20` a second time stores the value again but changes
nothing else: the effective value is still `20`.

Clearing works the same way: `ClearValue` removes one slot, recomputes the
effective value, and notifies or invalidates under the same rules.

## Inherited Properties

A property inherits when it has `UiPropertyOptions.Inherits`. Today these are
`UIElement.DataContextProperty`, `Control.ForegroundProperty`,
`Control.FontFamilyProperty`, and `Control.FontSizeProperty`.

During `FramePhase.InheritedProperties`, `InheritedPropertyPropagator` walks
the visual children of each queued element. For every inherited property in
`UiPropertyRegistry` and every child:

- the parent's source is `Default` → the child's `Inherited` slot is cleared;
- otherwise → the parent's effective value is written to the child's
  `Inherited` slot.

The write uses the normal store, so a child's own `Local`, Aspect, or markup
value still wins over the inherited one. The propagator writes every inherited
property to every visual descendant, even when the descendant's type does not
declare it. This is how `Foreground` reaches a `TextBlock` through a `Panel`
that has no `Foreground` of its own.

Before descending into a child, the propagator removes that child from the
`InheritedPropertyQueue`, so one traversal does not process the same subtree
twice. The scheduler runs this phase twice per pass; see
[the frame order](overview.md#invalidation-and-frame-scheduling).

## Lifecycle And Ownership

- A `UiProperty<T>` lives for the whole process. `UiPropertyRegistry` keeps it
  and rejects a second registration with the same owner type and name.
- A `UiPropertyStore` belongs to exactly one `UiObject` and dies with it.
- A detached `UIElement` still stores values and still marks its own
  `DirtyState`, but enqueues no work (`UIElement.Invalidate` returns before
  `UIRoot.Invalidate` when `Root` is `null`).
- `UIRoot` owns the `InheritedPropertyPropagator` and the
  `RootPropertyMutationObserver`. An element reaches the observer only while it
  is attached (`UIElement.MutationObserver` reads `Root?.PropertyMutations`).

## Frame Integration

- Direct writes run immediately on the UI thread. The property system queues
  no deferred writes.
- Effective-value changes enqueue work through `DirtyPropagation` into the
  queues processed by `UiFrameScheduler`.
- Inherited values move in `FramePhase.InheritedProperties`.
- Motion writes `Animation` values through the internal
  `UiObject.TrySetAnimationValueUntyped`, which drops a sample that fails
  validation instead of throwing.

## Invariants

- A higher stored source wins over every lower one: `Local` over `Animation`
  over the markup sources over the Aspect sources over `Inherited`.
  `tests/Cerneala.Tests/UI/Core/UiPropertyStoreTests.cs:EffectiveValueUsesExplicitPrecedence`,
  `tests/Cerneala.Tests/UI/Core/UiPropertyStoreTests.cs:MarkupSourcesSitBetweenAnimationAndAspectState`,
  `tests/Cerneala.Tests/UI/Core/UiPropertyStoreTests.cs:ExplicitTemplateOwnerBindingOverridesChildAspectButNotMarkup`.
- Clearing a source makes the next stored source effective; the lower value was
  kept. `tests/Cerneala.Tests/UI/Core/UiPropertyStoreTests.cs:ClearingHigherSourceRevealsNextEffectiveValue`.
- With nothing stored, the value is the metadata default and the source is
  `Default`. `tests/Cerneala.Tests/UI/Core/UiPropertyTests.cs:RegisteredPropertyExposesMetadataAndDefaultValue`.
- `Default` cannot be stored.
  `tests/Cerneala.Tests/UI/Core/UiPropertyStoreTests.cs:StoreRejectsDefaultAsStoredSource`.
- A write that leaves the effective value unchanged raises no `PropertyChanged`
  and no owner invalidation, also when it writes a lower source.
  `tests/Cerneala.Tests/UI/Core/UiPropertyInvalidationTests.cs:EqualEffectiveValueDoesNotNotifyOrInvalidate`,
  `tests/Cerneala.Tests/UI/Core/UiPropertyInvalidationTests.cs:LowerPrecedenceChangeDoesNotInvalidateWhenEffectiveValueIsUnchanged`.
  These tests check the owner hook on a plain `UiObject`; that an equal write
  on an attached element leaves the frame queues empty is netestat.
- A read-only property rejects `SetValue(UiProperty<T>, …)` and accepts writes
  through its key.
  `tests/Cerneala.Tests/UI/Core/ReadOnlyUiPropertyTests.cs:PublicSetRejectsReadOnlyProperty`,
  `tests/Cerneala.Tests/UI/Core/ReadOnlyUiPropertyTests.cs:KeySetUpdatesReadOnlyProperty`.
- A second registration with the same owner type and name throws.
  `tests/Cerneala.Tests/UI/Core/UiPropertyRegistryTests.cs:RegisterRejectsDuplicateOwnerAndName`.
- A render-only property dirties render but not measure or arrange.
  `tests/Cerneala.Tests/UI/Elements/UIElementMotionPropertyTests.cs:OpacityAndRenderTransformDirtyRenderButNotLayout`.
- `UiPropertyOptions` become dirty flags and queue entries on an attached
  element. `tests/Cerneala.Tests/UI/Elements/UIElementInvalidationTests.cs:TypedPropertyOptionsTranslateIntoRetainedInvalidation`
  (checks `Measure`, `Render`, `HitTest`; the added `Arrange` is checked at
  flag level by `tests/Cerneala.Tests/UI/Invalidation/DirtyPropagationTests.cs:MeasureInvalidationPropagatesLayoutNeedUpward`).
- A parent's inherited value reaches a descendant with source `Inherited`
  after one frame.
  `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs:ParentForegroundPropagatesToDescendantDuringFrame`.
- A child's `Local` value wins over the inherited value.
  `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs:LocalChildValueWinsOverInheritedValue`.
- A failure during inherited propagation keeps the parent queued, and the next
  frame finishes the subtree.
  `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs:PropagationFailureKeepsSubtreeRetryable`.
- Clearing an inherited value on the parent clears the child's `Inherited`
  slot: netestat.

## Diagnostic

`UIRoot.Detective.Invalidation` is the root's `InvalidationTrace`. It records
each `InvalidationRequest` that a property change creates, and the queues it
reaches, only when the root was created with an enabled trace through the
`UIRoot(InvalidationTrace, …)` constructor. The default constructor uses
`InvalidationTrace.Disabled`.

## Known Limitations

- There are no XML documentation comments in `UI/Core`. The canonical API
  reference is in `docs-site/documentation/classes/`.
- `UiPropertyKey<T>` writes skip the read-only check. Any code that holds the
  key can write the property with any source.
