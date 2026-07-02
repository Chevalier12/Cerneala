## Why

Cerneala now has a modern `ROADMAPv2.md` direction, but the MVP retained UI architecture needs a spec-backed contract before implementation starts.

This change captures the retained-mode MVP foundation decisions so future code work can proceed from stable, testable design instead of memory or chat context.

## What Changes

- Add OpenSpec contracts for the retained UI MVP foundation.
- Define the v2 architecture above the existing `UI/Drawing` and `UI/Input` layers.
- Document retained-mode frame flow, invalidation, logical/visual trees, render cache, input routing, and MVP boundaries.
- Add implementation tasks for the first MVP planning/doc slice.
- Add project memory files:
  - `openspec/README.md`
  - `openspec/project.md`
  - `docs/architecture-v2.md`
  - `docs/diagrams/retained-frame-loop.md`
  - `docs/diagrams/ui-layer-boundaries.md`

## Capabilities

### New Capabilities

- `retained-ui-mvp-foundation`: Defines the retained UI MVP architecture contracts, including typed state, logical/visual trees, invalidation, layout, render cache, host integration, input/focus/command bridge, styling metadata decisions, and documentation requirements.

### Modified Capabilities

- None.

## Impact

- Adds planning artifacts under `openspec/changes/add-retained-ui-mvp-foundation`.
- Adds repo-level OpenSpec project memory under `openspec/`.
- Adds v2 architecture documentation under `docs/`.
- Does not add runtime production code yet.
- Does not change existing `UI/Drawing` or `UI/Input` behavior.
