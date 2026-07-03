## Why

Retained UI systems are difficult to debug when invalidation, layout, render caches, routed input, and styling all cooperate implicitly. This change adds first-class diagnostics so developers can inspect what happened in a frame, which elements are dirty, how trees and caches look, where routed events travel, and why a style value was chosen.

## What Changes

- Add backend-neutral diagnostics snapshots for frame, layout, render-cache, input, dirty-tree, element-tree, routed-event, and style inspection.
- Add developer-facing dumpers that produce deterministic text output suitable for logs, tests, and playground overlays.
- Add retained debug overlay/adorner primitives that can display diagnostic text without bypassing the retained UI path.
- Add a playground diagnostics sample that demonstrates per-frame counters and tree/cache/event/style inspection.
- Add focused tests for frame counters, dirty-tree dumps, element-tree dumps, render-cache dumps, routed-event traces, and style source traces.

## Capabilities

### New Capabilities

- `diagnostics-devtools`: Covers retained UI diagnostics, debug dumpers, routed-event/style tracing, and developer overlays.

### Modified Capabilities

None.

## Impact

- Affected code: `UI/Diagnostics`, retained element/layout/render/input/style inspection surfaces, and playground samples.
- Affected tests: new diagnostics tests under `tests/Cerneala.Tests/UI/Diagnostics` plus roadmap/boundary coverage where needed.
- Dependencies: no new external dependencies; diagnostics remain backend-neutral and must not reference MonoGame, Skia, HarfBuzz, or platform-specific adapters.
