## Why

`ROADMAPv2.md` section 1 still has stale architecture-contract checkboxes from the first roadmap draft even though the repo now uses modern OpenSpec capability names. This makes the roadmap look incomplete in a confusing way and leaves the intended architecture boundary tests under-specified.

## What Changes

- Reconcile section 1 roadmap entries with the actual OpenSpec specs that exist under `openspec/specs/`.
- Add repository shape and namespace boundary tests for the retained UI architecture.
- Keep the change documentation-only/test-only; no runtime UI behavior changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `retained-ui-mvp-foundation`: require roadmap architecture-contract entries to match actual OpenSpec capabilities and require architecture boundary tests for repository shape and backend-neutral UI core namespaces.

## Impact

- Affected docs/checklists: `ROADMAPv2.md`.
- Affected tests: `tests/Cerneala.Tests/Architecture/RepositoryShapeTests.cs`, `tests/Cerneala.Tests/Architecture/NamespaceBoundaryTests.cs`.
- Affected specs: `openspec/specs/retained-ui-mvp-foundation/spec.md`.
- No runtime API or behavior changes.
