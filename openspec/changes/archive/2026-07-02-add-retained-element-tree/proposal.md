## Why

Cerneala now has a typed state model, but it still lacks the retained element tree that owns state, parent/child relationships, lifecycle, element identity, and the bridge from retained UI objects into future layout, rendering, and input routing.

This change implements `ROADMAPv2.md` section `3. [MVP] Retained element tree` and resolves the stale roadmap wording that described a temporary single tree. The confirmed MVP direction is separate logical and visual trees.

## What Changes

- Add the retained element tree foundation under `UI/Elements`.
- Introduce `UIElement` as the public retained element base type.
- Add owned child collections with parent validation, reparent rejection, and change notifications.
- Add `UIRoot` as the retained root with viewport, scaling, input route ownership, and future render-cache root responsibilities.
- Add lifecycle attach/detach hooks and tree versioning.
- Add stable element ids for input routing while elements remain attached.
- Add tree traversal helpers for pre-order, post-order, ancestors, and descendants.
- Add explicit contracts for generated child hosts and element hosts.
- Add retained element handler storage for routed events.
- Add the first retained input route bridge types that map retained elements to input route ids.
- Correct `ROADMAPv2.md` section 3 to follow the confirmed MVP logical/visual tree decision.

## Capabilities

### New Capabilities

- `retained-element-tree`: Defines retained element ownership, logical/visual parentage, child collections, lifecycle, stable element ids, traversal, root behavior, handler storage, and retained input route bridge requirements.

### Modified Capabilities

- `retained-ui-mvp-foundation`: Clarifies that the retained element tree implementation follows the confirmed separate logical and visual tree MVP decision, not the stale single-tree wording in `ROADMAPv2.md`.

## Impact

- Adds production files under `UI/Elements`.
- Adds retained input bridge files under `UI/Input`.
- Adds tests under `tests/Cerneala.Tests/UI/Elements` and `tests/Cerneala.Tests/Input`.
- Uses the existing typed state model under `UI/Core`.
- Preserves existing `UI/Drawing` behavior.
- Preserves useful existing `UI/Input` primitives while preventing `UiInputTree` from becoming a second permanent retained tree.
- Does not implement layout, render cache, styling, controls, host integration, or full input dispatch yet.
