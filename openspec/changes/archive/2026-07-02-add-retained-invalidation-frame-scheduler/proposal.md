## Why

Cerneala now has typed state and a retained element tree, but state changes still do not have a concrete retained invalidation pipeline. This change adds the game-loop-friendly core that lets update/draw run every frame while measure, arrange, render-cache, and hit-test work runs only when dirty state requires it.

## What Changes

- Add retained invalidation primitives under `UI/Invalidation`, including flags, dirty state, invalidation requests, propagation rules, phase queues, frame phases, frame stats, and scheduler.
- Add diagnostics support for invalidation tracing under `UI/Diagnostics`.
- Connect `UIElement` to metadata-driven invalidation so typed property options can mark retained elements dirty without coupling `UI/Core` to layout, rendering, or backend systems.
- Define deterministic queue and scheduler behavior for MVP: no frame budget deferral, stable processing order, no-op unchanged frames, and dirty flags cleared only after successful phase processing.
- Keep layout, retained rendering, render cache composition, and real hit-test geometry as future phases; this change provides contracts and queues they will consume.

## Capabilities

### New Capabilities

- `retained-invalidation-frame-scheduler`: Retained dirty state, invalidation propagation, phase queues, frame scheduling, diagnostics, and no-work-frame behavior.

### Modified Capabilities

- `typed-state-model`: Property invalidation hooks must map to retained invalidation requests through retained elements.
- `retained-element-tree`: Retained elements must own dirty state and expose invalidation entry points used by queues and the frame scheduler.
- `retained-ui-mvp-foundation`: The retained-mode frame loop decision becomes implemented by concrete invalidation and scheduler contracts.

## Impact

- Adds production files under `UI/Invalidation` and `UI/Diagnostics`.
- Updates `UI/Elements/UIElement.cs` and `UI/Elements/UIRoot.cs` to own dirty state, invalidation routing, and scheduler integration points.
- Adds focused tests under `tests/Cerneala.Tests/UI/Invalidation` and diagnostics tests for invalidation trace behavior.
- Updates `ROADMAPv2.md` section 4 checkboxes as implementation tasks complete.
- No backend-specific dependencies are introduced; `UI/Core`, `UI/Elements`, and `UI/Invalidation` remain independent of MonoGame, Skia, HarfBuzz, `Texture2D`, and `SpriteBatch`.
