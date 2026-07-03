## Context

Cerneala has typed properties, retained controls, templates, styling, resources, item controls, and virtualization. The missing layer is typed data flow: code can build controls and set values, but there is no shared observable value/list contract and no small binding facade for connecting model state to retained UI properties.

Section 18 intentionally comes before diagnostics and text editing because later controls need predictable data change notifications. The design must stay modern: explicit typed APIs first, no XAML-first assumptions, no reflection-heavy WPF binding clone, and no string property paths in the hot path.

## Goals / Non-Goals

**Goals:**
- Add `ObservableValue<T>` for typed scalar state with old/new value notifications.
- Add `IObservableList<T>` and `ObservableList<T>` for ordered list changes.
- Add `PropertyAdapter<TOwner,TValue>` to connect model reads/writes to typed `UiProperty<T>` targets.
- Add `Binding`, `Binding<T>`, `BindingMode`, and `IValueConverter<TIn,TOut>` as small explicit binding-light helpers.
- Add `CollectionView<T>`, `SortDescription<T>`, and `FilterPredicate<T>` for typed sorting/filtering over observable or plain data.
- Document `StringPropertyPath` as a deferred non-hot-path placeholder.
- Add behavior tests for observable values, observable lists, typed binding, collection views, and string path deferral.

**Non-Goals:**
- No reflection-based path walking or runtime expression parser.
- No XAML/markup binding integration.
- No grouped views, live shaping, async paging, or data virtualization.
- No automatic UI tree discovery or global data context lookup.
- No replacing `ItemsControl.ItemCollection`; observable lists can feed item controls later through explicit code.

## Decisions

### Observation uses explicit event args and change kinds

`ObservableValue<T>` should expose a `ValueChanged` event with old/new values and avoid raising when the configured equality comparer treats values as equal. `ObservableList<T>` should expose list change events with a small change kind enum for add, remove, replace, move, reset, and clear.

Rationale: retained invalidation depends on knowing what changed. Event args are easier to test and debug than generic callbacks hidden behind binding objects.

Alternative considered: use `INotifyPropertyChanged` / `INotifyCollectionChanged` directly. Rejected for the core API because those contracts are string/object-heavy and easy to misuse; adapters can be added later if needed.

### Binding is a disposable explicit connection

`Binding<T>` should connect a typed source (`ObservableValue<T>` or adapter) to a typed target setter. It should update according to `BindingMode` and be disposable so control lifetimes can release subscriptions deterministically.

Rationale: retained UI objects have explicit lifetimes. Hidden global binding state would be harder to reason about and harder to audit for leaks.

Alternative considered: add `BindingExpression` as a property-system layer. Rejected because it would make binding a central property-store concern before scenarios prove that complexity is needed.

### PropertyAdapter stays typed and owner-explicit

`PropertyAdapter<TOwner,TValue>` should wrap get/set delegates for a specific owner type. It can drive `UiProperty<T>` values or model values without requiring string property names.

Rationale: this keeps the fast path generic and compile-time typed, matching the project direction.

Alternative considered: `PropertyAdapter` with `object owner, string path`. Rejected as the legacy shape we are deliberately avoiding.

### CollectionView is derived and refresh-driven

`CollectionView<T>` should produce a derived read-only view over a source sequence/list with optional filter and sort descriptions. Source changes should refresh the derived view and emit reset-style notifications for this phase.

Rationale: a reset-style MVP is correct and simple. Fine-grained view diffs can be introduced after real scenarios need them.

Alternative considered: full live sorted/filtered incremental diffs. Rejected as too much scope for section 18.

### StringPropertyPath is intentionally deferred

`StringPropertyPath` may exist only as an explicit deferred placeholder that throws or reports unsupported behavior. It must not participate in binding hot paths in this phase.

Rationale: the roadmap calls it later/not hot-path core. Making the deferral visible prevents accidental half-built reflection binding.

Alternative considered: omit the file entirely. Rejected because the roadmap lists it and tests can guard the deferral.

## Risks / Trade-offs

- [Risk] Binding subscriptions can leak retained controls. -> Make binding connections disposable and test unsubscribe behavior.
- [Risk] Observable list events can become too coarse for virtualization. -> Start with correct change args; collection view can use reset while item controls continue to own realization.
- [Risk] Typed adapters can become boilerplate-heavy. -> Keep helpers small and composable; defer source generators until later roadmap sections.
- [Risk] String path placeholder can be mistaken for implemented binding. -> Add tests proving it is explicitly unsupported/deferred.
