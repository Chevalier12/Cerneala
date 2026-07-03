## Why

Cerneala has retained root, invalidation, layout, input snapshots, and retained rendering cache pieces, but applications still need a single entry point that fits a game loop. This change adds that host layer so `Update` can process invalidated UI work and `Draw` can submit cached retained commands without rebuilding work every frame.

## What Changes

- Add `UI/Hosting` primitives for viewport, frame data, frame timing, host options, backend bridge, and a retained `UiHost`.
- Add a backend-neutral host contract that can receive input and render through existing `IInputSource` and `IDrawingBackend` boundaries.
- Add `UiHost.Update(...)` to read or receive an `InputFrame`, apply viewport changes, dispatch the frame into retained UI services, process layout/render queues, and expose frame diagnostics.
- Add `UiHost.Draw(...)` to submit the cached root `DrawCommandList` to an `IDrawingBackend` without forcing layout, arrange, or render-cache regeneration.
- Add MonoGame hosting adapter files under `UI/Hosting/MonoGame` that compose `MonoGameInputSource` and `MonoGameDrawingBackend` without leaking MonoGame types into core hosting.
- Update the playground to create a `MonoGameUiHost`, set a retained `UIRoot`, call `Update`, and call `Draw`.
- Add focused hosting tests proving first-frame work, unchanged-frame reuse, viewport invalidation, draw/update separation, and MonoGame boundary rules.
- Update `ROADMAPv2.md` checkboxes for section 7 as implementation tasks complete.

## Capabilities

### New Capabilities
- `game-loop-host-integration`: Defines the retained UI host, frame/viewport contracts, backend bridge, MonoGame adapter boundary, playground integration, and host frame behavior.

### Modified Capabilities
- `retained-ui-mvp-foundation`: The documented retained frame loop gains a concrete application-facing host entry point.
- `retained-invalidation-frame-scheduler`: Host update becomes the application-facing scheduler driver for retained dirty work and no-work frame behavior.
- `retained-rendering-cache`: Host draw becomes the application-facing path that submits cached retained root commands without regenerating retained render work.

## Impact

- New production files under `UI/Hosting` and `UI/Hosting/MonoGame`.
- Updates to `Playground/Cerneala.Playground/Game1.cs`.
- New tests under `tests/Cerneala.Tests/UI/Hosting`.
- No new dependency is required for core hosting; MonoGame-specific references stay inside MonoGame adapter files.
