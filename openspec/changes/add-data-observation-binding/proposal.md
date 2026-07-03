## Why

Cerneala now has retained items, templates, styling, resources, and invalidation, but data flow is still manual and ad hoc. Section 18 adds typed observation and binding-light APIs so retained controls can react to data changes without reflection-heavy WPF-style binding or string paths in the hot path.

## What Changes

- Add typed observable value and list primitives under `UI/Data`.
- Add a lightweight list observation contract for retained item controls and collection views.
- Add typed property adapters that connect model data to `UiProperty<T>` setters without string property paths.
- Add optional typed binding facades, binding mode metadata, and typed value converters.
- Add simple typed collection view support for sorting and filtering.
- Keep `StringPropertyPath` as a deferred/later placeholder rather than a hot-path core requirement.
- Add focused tests for observable values, observable lists, typed binding, collection views, and documented string-path deferral.
- Update `ROADMAPv2.md` section 18 as files, tests, and acceptance behavior are completed.

## Capabilities

### New Capabilities

- `data-observation-binding`: Covers typed observable values/lists, property adapters, binding-light APIs, typed converters, and collection views.

### Modified Capabilities

## Impact

- Adds new APIs under `UI/Data`.
- Adds tests under `tests/Cerneala.Tests/UI/Data`.
- Integrates with the existing typed property system and retained invalidation through explicit callbacks/setters.
- Does not add reflection-heavy WPF binding, XAML dependency, arbitrary string-path binding in the hot path, grouped collection views, or async data virtualization.
