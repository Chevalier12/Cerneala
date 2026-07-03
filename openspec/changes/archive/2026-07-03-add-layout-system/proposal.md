## Why

Cerneala has retained elements and invalidation queues, but there is no measure/arrange system to turn the visual tree into layout slots. This change adds the MVP layout layer so retained UI can cache desired sizes and arranged bounds without confusing layout geometry with drawing command geometry.

## What Changes

- Add layout-specific primitives under `UI/Layout`: size, point, rect, thickness, alignment, visibility, rounding, measure/arrange contexts, and layout results.
- Add layout contracts for retained elements, including measure/arrange entry points, desired size, arranged bounds, and layout versioning.
- Add `LayoutManager` that consumes `LayoutQueue`, measures and arranges dirty retained visual subtrees, caches results, and invalidates render/hit-test work when arranged bounds or visibility change.
- Add layout boundary support so propagation can stop at explicit roots/subtrees.
- Add MVP panel controls under `UI/Layout/Panels`: `Panel`, `Canvas`, `StackPanel`, and orientation support.
- Defer `Grid`, `GridLength`, `ColumnDefinition`, and `RowDefinition` unless implementation confirms they are required for MVP; keep them planned but not required by this change.

## Capabilities

### New Capabilities

- `layout-system`: Layout geometry, measure/arrange contracts, layout manager, visibility/layout invalidation behavior, layout boundaries, and MVP panels.

### Modified Capabilities

- `retained-element-tree`: Retained elements gain layout state, measure/arrange hooks, visibility semantics, and layout-facing visual child behavior.
- `retained-invalidation-frame-scheduler`: Layout queue processing is backed by `LayoutManager`, and layout completion can schedule render/hit-test work.
- `retained-ui-mvp-foundation`: The retained layout portion of the MVP frame loop becomes concrete and testable.

## Impact

- Adds production files under `UI/Layout` and `UI/Layout/Panels`.
- Updates `UI/Elements/UIElement.cs` and `UI/Elements/UIRoot.cs` for layout state and integration points.
- Updates invalidation/scheduler integration where needed so measure/arrange queue entries are processed by the layout manager.
- Adds tests under `tests/Cerneala.Tests/UI/Layout`.
- Updates `ROADMAPv2.md` section 5 checkboxes as implementation tasks complete.
- Does not introduce backend-specific dependencies; layout remains independent of `UI/Drawing`, MonoGame, Skia, HarfBuzz, `Texture2D`, and `SpriteBatch`.
