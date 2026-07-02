## Why

Cerneala needs the typed state model before the retained element tree, layout, styling, resources, templates, binding, and invalidation can be implemented coherently.

This change defines the implementation contract for `ROADMAPv2.md` section `2. [MVP] Typed state model`: a smaller, explicit, strongly typed property system inspired by useful WPF concepts, without cloning WPF dependency properties or relying on reflection-heavy behavior.

## What Changes

- Add OpenSpec contracts for the typed state model.
- Define the core `UI/Core` files from the roadmap:
  - `UiObject`
  - `UiProperty`
  - `UiProperty<T>`
  - `UiPropertyKey<T>`
  - `UiPropertyMetadata<T>`
  - `UiPropertyOptions`
  - `UiPropertyValueSource`
  - `UiPropertyStore`
  - property changed event args
  - registration, validation, coercion, and internal unset sentinel support
- Define value precedence:

  `local > animation > style visual state > style base > inherited > default`

- Define how property metadata maps to retained invalidation.
- Add the test checklist required before this phase can be considered complete.

## Capabilities

### New Capabilities

- `typed-state-model`: Defines a strongly typed retained UI property/state system with explicit registration, deterministic storage, validation, coercion, value precedence, read-only keys, inherited values, change notifications, and invalidation metadata.

### Modified Capabilities

- None.

## Impact

- Adds planning artifacts under `openspec/changes/add-typed-state-model`.
- Adds runtime production code under `UI/Core`.
- Does not change existing `UI/Drawing` or `UI/Input` behavior.
- Adds tests under `tests/Cerneala.Tests/UI/Core`.
