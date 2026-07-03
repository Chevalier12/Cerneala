## Why

Cerneala has retained elements, layout, invalidation queues, and a frame scheduler, but render work is still only an abstract phase. The next MVP slice needs a retained renderer that regenerates draw commands only for dirty elements while allowing the backend to render a cached root `DrawCommandList` every frame.

## What Changes

- Add a retained rendering layer under `UI/Rendering` that sits above `UI/Drawing`.
- Add `RenderContext` and a renderable element contract so retained elements can emit local drawing commands through the existing `DrawingContext`.
- Add per-element local render caches and a root/subtree render cache.
- Add a render queue processor that rebuilds only dirty local command lists and records cache hit/miss counters.
- Add a retained renderer that composes cached local command lists in deterministic retained visual order.
- Add command-list builder behavior for flattening retained cached commands without making `UI/Drawing` a scene graph.
- Add minimal clip/layer/dependency/counter types needed by the roadmap section 6 MVP.
- Keep pooling optional and out of the correctness path unless implementation proves it is trivial and covered.
- Preserve backend neutrality: no MonoGame, Skia, HarfBuzz, or concrete backend references in `UI/Rendering`.
- Update `ROADMAPv2.md` section 6 checkboxes as implementation proceeds.

## Capabilities

### New Capabilities

- `retained-rendering-cache`: Defines retained render contexts, element render caches, root render cache composition, render queue processing, draw-command flattening, render dependencies, and cache diagnostics.

### Modified Capabilities

- `retained-invalidation-frame-scheduler`: Render-cache phase work becomes concrete by delegating queued render work to the retained render queue processor.
- `retained-ui-mvp-foundation`: The retained-mode frame loop now includes concrete retained rendering and cached root command-list reuse.

## Impact

- New production files under `UI/Rendering`.
- Updates to `UI/Elements/UIElement.cs` and `UI/Elements/UIRoot.cs` to expose retained render hooks and root renderer ownership.
- Focused tests under `tests/Cerneala.Tests/UI/Rendering`.
- `ROADMAPv2.md` section 6 and OpenSpec change artifacts.
- Existing `UI/Drawing` command primitives and `IDrawingBackend` remain the backend contract; this change does not modify concrete backend adapters.
