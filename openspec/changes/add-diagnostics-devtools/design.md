## Context

Cerneala already has retained element trees, dirty state, frame scheduling, render caches, routed input, and style diagnostics. These systems are intentionally invalidation-driven, which avoids repeated work but makes behavior harder to inspect when something is stale, over-invalidated, or routed unexpectedly.

The diagnostics layer must remain backend-neutral and must sit above the retained UI state that already exists. It should expose snapshots and deterministic dump strings that can be asserted in tests, displayed in the playground, or logged by host applications.

## Goals / Non-Goals

**Goals:**

- Provide diagnostics for frame counters, layout state, render-cache state, input route state, dirty retained elements, element tree structure, routed event paths, and style value sources.
- Keep diagnostics deterministic and suitable for tests by using stable tree order and explicit formatting.
- Let playground samples show diagnostics through retained controls, so the diagnostics UI exercises the same retained rendering path as the rest of the framework.
- Keep all diagnostics APIs independent from MonoGame, Skia, HarfBuzz, and concrete drawing backends.

**Non-Goals:**

- Do not build a live inspector window, remote protocol, or interactive debugger.
- Do not add reflection-heavy object browsing.
- Do not introduce a second retained tree, render cache, or routing model only for diagnostics.
- Do not change existing invalidation, routing, styling, or rendering semantics except where small read-only accessors are needed.

## Decisions

- Diagnostics will be snapshot/read-only helpers over existing retained state. `FrameDiagnostics`, `LayoutDiagnostics`, `RenderDiagnostics`, and `InputDiagnostics` expose compact immutable snapshots instead of mutating scheduler, renderer, or router behavior.
- Tree and cache diagnostics will use explicit dumper classes. `DirtyTreeDumper`, `ElementTreeDumper`, and `RenderCacheDumper` produce deterministic text because the output is meant for tests, logs, and overlays.
- Routed event tracing will be non-invasive. `RoutedEventTrace` computes the route for a target and routed event using the same routing strategy rules as retained input, without requiring event handlers to run.
- Style tracing will adapt existing `StyleDiagnostics` data. `StyleTrace` reports matched rules, applied values, cleared values, and effective source information without duplicating style precedence logic.
- Debug visuals will be retained elements. `DebugOverlay` and `DebugAdorner` render diagnostic text through existing retained controls/rendering paths instead of drawing directly into a backend.

## Risks / Trade-offs

- Read-only diagnostics may need small public accessors on existing cache or tree types -> keep them narrow, immutable, and covered by boundary tests.
- Text dump formatting can become brittle -> make formatting intentionally simple, stable, and documented by tests.
- Diagnostics can accidentally become a dumping ground for runtime behavior -> keep production logic in existing systems and diagnostics as observers/adapters only.
- Overlay controls can mask retained rendering bugs if they bypass normal controls -> implement them as retained UI elements, not backend drawing shortcuts.
