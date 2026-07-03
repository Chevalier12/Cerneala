## Context

Cerneala already has most retained UI internals needed for a game-loop friendly frame:

- `UIRoot` owns retained queues, layout manager, render queue processor, retained render cache, retained renderer, and `UiFrameScheduler`.
- `IInputSource` produces immutable `InputFrame` snapshots.
- `IDrawingBackend` renders a `DrawCommandList`.
- `RetainedRenderer.Submit(root, backend)` renders the retained root command list through an `IDrawingBackend`.
- `Game1` currently draws immediate commands directly and does not exercise retained UI.

The missing piece is an application-facing host that composes these parts into an `Update`/`Draw` rhythm without making the app manually know about scheduler queues, root cache generation, and backend-specific adapter setup.

## Goals / Non-Goals

**Goals:**

- Provide a `UiHost` as the retained UI entry point for applications.
- Keep core hosting backend-neutral.
- Make first-frame retained work deterministic.
- Make unchanged frames cheap: update records no work and draw reuses cached retained commands.
- Make viewport changes invalidate retained arrange/render state.
- Provide a MonoGame adapter that wires existing MonoGame input and drawing backends.
- Update the playground to demonstrate the retained host path.
- Add tests that prove host frame contracts instead of relying on chat memory or manual testing.

**Non-Goals:**

- No full retained input routing, hit-test dispatch, focus system, commands, or controls in this phase.
- No styling/resource system work.
- No new scene graph separate from the retained logical/visual element tree.
- No new drawing backend abstraction that replaces `IDrawingBackend`.
- No render thread, async asset loading, frame budgeting, or partial deferred scheduler processing.
- No MonoGame references in `UI/Hosting` core files.

## Decisions

### `UiHost` is a thin orchestrator over `UIRoot`

`UiHost` SHALL own or reference a retained `UIRoot` and call the root's existing frame services instead of duplicating layout, invalidation, or rendering behavior.

Alternative considered: create a parallel host-level scheduler and render cache. That would duplicate `UIRoot` responsibilities and make bugs harder to reason about. The host should compose the existing root; the root remains the owner of retained internals.

### `Update` owns retained work, `Draw` owns backend submission

`UiHost.Update(...)` SHALL gather frame data, apply viewport changes, read or accept `InputFrame`, and process the retained root frame. `UiHost.Draw(...)` SHALL submit the already retained root commands to an `IDrawingBackend`.

Alternative considered: let `Draw` call `ProcessFrame` if the cache is invalid. That makes rendering order depend on draw timing and hides expensive retained work in the draw phase. This phase keeps the contract simple: retained work happens in update; draw submits cached output.

### Core hosting uses existing input and drawing interfaces

Core hosting SHALL use `IInputSource`, `InputFrame`, and `IDrawingBackend`. `IUiBackend` is a host bridge for bundling backend-facing services, not a replacement for drawing or input primitives.

Alternative considered: make `UiHost` directly depend on MonoGame `GameTime`, `GraphicsDevice`, `SpriteBatch`, or static input APIs. That would make core hosting backend-specific and break the existing adapter boundary.

### MonoGame is only an adapter layer

`MonoGameUiHost` SHALL live under `UI/Hosting/MonoGame` and compose `MonoGameInputSource`, `MonoGameDrawingBackend`, and MonoGame service glue. Controls and core retained UI files SHALL not reference MonoGame types.

Alternative considered: let playground and controls construct MonoGame drawing/input services directly. That works short-term but makes future backend support and testability worse.

### `UiViewport` is an explicit value object

Viewport width, height, and scale SHALL be represented by `UiViewport`, so size changes can be compared deterministically and applied to `UIRoot.SetViewport(...)`.

Alternative considered: pass width, height, and scale as loose floats everywhere. That is easy to misuse and makes tests noisier.

### `UiFrame` captures the last processed frame

`UiFrame` SHALL carry elapsed time, viewport, input frame, and frame stats from update. Tests and diagnostics can inspect the last frame without coupling to root internals.

Alternative considered: expose only raw `FrameStats`. That misses the input and viewport context needed to debug host behavior.

### First frame explicitly primes retained work

The host SHALL ensure the first update produces retained layout/render cache work for the root. It can do this by invalidating the root for measure/arrange/render or by setting the initial viewport through an API that queues the same retained work.

Alternative considered: rely on callers to manually invalidate the root before setting it on the host. That creates a fragile integration step and makes playground behavior inconsistent.

## Risks / Trade-offs

- [Risk] `UIRoot.SetViewport(...)` currently increments tree version and invalidates the root render cache, but may not queue arrange work by itself. → Mitigation: host implementation and tests must prove viewport changes schedule arrange/render work through retained invalidation.
- [Risk] Existing retained rendering may generate cached commands lazily when `RetainedRenderer.Render(...)` is called. → Mitigation: update phase should process render-cache queues; draw must not process scheduler work. Tests should assert draw does not repeat layout/render work on unchanged frames.
- [Risk] Input is captured but not fully dispatched in this phase. → Mitigation: document the boundary clearly and leave routed input dispatch for a later roadmap section.
- [Risk] MonoGame adapter setup can accidentally leak types into core hosting. → Mitigation: add boundary tests or source-scan tests proving core `UI/Hosting` files do not reference `Microsoft.Xna.Framework`.
- [Risk] Playground integration may be more demo-oriented than production. → Mitigation: keep it as a smoke/demo path and place reusable behavior in `UI/Hosting`.

## Migration Plan

1. Add core hosting primitives.
2. Implement `UiHost` over `UIRoot` without changing existing drawing/input APIs.
3. Add MonoGame adapter files.
4. Update playground to use `MonoGameUiHost`.
5. Add hosting tests.
6. Check off `ROADMAPv2.md` section 7 items as each file/contract is implemented.

No rollback or data migration is required. If the host path fails, existing lower-level drawing/input APIs can remain usable while the host implementation is fixed.

## Open Questions

- The exact future retained input dispatch API is intentionally unresolved; this phase only captures and stores the input frame.
- The final shape of content/font/image services may grow later; `MonoGameContentServices` should stay minimal until controls need it.
