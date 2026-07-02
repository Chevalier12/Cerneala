# Cerneala OpenSpec

This folder stores planning contracts for Cerneala changes.

Use OpenSpec when a change affects architecture, public API, retained UI behavior, roadmap scope, or cross-module contracts. Small local fixes can use normal tests and code review, but retained UI work should start from a change.

## Current Direction

`ROADMAPv2.md` is the active retained UI roadmap.

`architecture.md` documents existing `UI/Drawing` and `UI/Input` boundaries.

`docs/architecture-v2.md` documents the retained UI architecture above those existing foundations.

## Workflow

1. Create a change with `openspec new change "<name>"`.
2. Fill in `proposal.md`, `design.md`, `specs/**/*.md`, and `tasks.md`.
3. Validate with `openspec validate "<name>"`.
4. Implement only after the change is apply-ready.
5. Keep task checkboxes updated so work can resume without relying on memory.

## Active MVP Foundation

The retained UI MVP foundation is tracked by:

- `openspec/changes/add-retained-ui-mvp-foundation/`

This change captures the architecture contracts and documentation for the first retained UI implementation slice.
