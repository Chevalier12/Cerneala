## Context

Cerneala now has a typed state model, retained element tree, layout system, retained invalidation queues, and a frame scheduler. `ROADMAPv2.md` section 6 is the next MVP slice: retained rendering and render cache.

The existing drawing layer is intentionally low-level. `DrawingContext` records commands into `DrawCommandList`, and `IDrawingBackend.Render(DrawCommandList)` consumes those commands. The retained renderer must use that drawing layer without turning `UI/Drawing` into a scene graph and without leaking concrete backend types into retained UI.

## Goals / Non-Goals

**Goals:**

- Add `UI/Rendering` as the retained rendering layer above `UI/Drawing`.
- Add `RenderContext` so render hooks receive a `DrawingContext`, layout bounds, inherited visual state, and diagnostics/counters.
- Add a renderable element contract and `UIElement.OnRender(RenderContext)` hook.
- Add per-element `ElementRenderCache` with local `DrawCommandList`, versions, content bounds, and dependency state.
- Add `RetainedRenderCache` for root/subtree cache ownership and cached root command-list reuse.
- Add `RenderQueueProcessor` that rebuilds only dirty local element command lists.
- Add `RetainedRenderer` and `DrawCommandListBuilder` that compose cached local commands in deterministic retained visual order.
- Add minimal clip/layer/dependency/counter types required for MVP correctness and diagnostics.
- Prove unchanged frames reuse cached commands and child render changes do not rebuild unrelated siblings.

**Non-Goals:**

- Do not implement game-loop host integration; section 7 owns `UiHost`.
- Do not implement styling, templates, markup, or control libraries.
- Do not implement a real hit-test geometry service.
- Do not add concrete MonoGame, Skia, HarfBuzz, or texture dependencies to `UI/Rendering`.
- Do not optimize with pooling until correctness is proven; `DrawCommandListPool` can be a minimal or deferred shell.
- Do not make `UI/Drawing` aware of retained elements, layout, or render caches.

## Decisions

### Decision: Rendering is retained above `UI/Drawing`

`UI/Rendering` owns cache invalidation, render traversal, and command-list composition. `UI/Drawing` remains a command recorder/backend contract.

Rationale: retained rendering needs element identity, dirty state, layout bounds, and visual tree traversal. Those belong above drawing commands, not inside drawing primitives.

Alternative considered: make `DrawCommandList` store element IDs and act as the render cache. Rejected because that would blur the backend command layer with retained UI ownership.

### Decision: Local element cache first, root command cache second

Each renderable element owns or is associated with an `ElementRenderCache` containing local commands recorded by `OnRender`. `RetainedRenderer` composes these local caches into a cached root `DrawCommandList`.

Rationale: this is what prevents a child render invalidation from rebuilding unrelated sibling command lists while still allowing `IDrawingBackend.Render(rootCommandList)` every draw frame.

Alternative considered: regenerate one root command list directly from all elements whenever anything changes. Rejected because it defeats the retained-mode performance goal.

### Decision: `OnRender(RenderContext)` records local commands only

`UIElement.OnRender(RenderContext)` records commands for that element's local visual content. Child composition is owned by `RetainedRenderer` and `DrawCommandListBuilder`.

Rationale: local render hooks should not know traversal or sibling order. This keeps render order deterministic and testable.

Alternative considered: let each element recursively render its children. Rejected because it makes cache reuse harder and mixes local rendering with tree composition.

### Decision: Render queue processing is scheduler-integrated

`UIRoot.ProcessFrame()` should provide render-cache processors alongside layout processors once the root has a retained renderer. Render dirty work is processed in the existing scheduler `RenderCache` phase.

Rationale: invalidation already decides what is dirty; rendering should plug into that existing phase instead of creating another frame loop.

Alternative considered: call render processing manually after `ProcessFrame`. Rejected because phase ordering would be easier to violate and harder to test.

### Decision: Clip support is balanced command emission

`ClipNode` is MVP metadata that can be translated to `PushClip`/`PopClip` around a subtree. Even empty subtrees must produce balanced clip commands if a clip is active.

Rationale: clip stack correctness is a rendering invariant and cheap to test now.

Alternative considered: defer clipping entirely. Rejected because roadmap section 6 explicitly calls out balanced clip commands.

### Decision: Dependencies are explicit but minimal

`RenderDependency` tracks versions or keys for text, image, theme, and resource dependencies that can invalidate cached commands. MVP can use simple comparable values and explicit invalidation paths.

Rationale: dependency-aware cache invalidation is needed before styling/resources exist, but the type should not force those systems into this slice.

Alternative considered: only use `DirtyState.Render`. Rejected because text/image/resource scenarios are already represented by invalidation flags and later systems need a place to connect.

### Decision: `DrawCommandListPool` is deferred

DrawCommandListPool is deferred for this slice. Retained rendering correctness must not depend on pooling, and no production rendering path should require a pool until profiling proves allocation pressure and a focused pool contract is added.

Rationale: command-list reuse and cache invalidation are the correctness boundary for this change. Adding pooling now would expand the ownership and lifetime surface without evidence that allocations are the current bottleneck.

Alternative considered: add a minimal no-op or real pool shell. Rejected because a shell adds API surface without behavior, while a real pool needs stale-command and ownership tests that are not needed to prove retained rendering correctness.

## Risks / Trade-offs

- [Risk] Cache invalidation can reuse stale command lists. -> Mitigation: tests for dirty element rebuild, sibling cache reuse, dependency changes, and root cache version changes.
- [Risk] Composition order can drift from retained visual order. -> Mitigation: builder tests over parent/child/sibling trees.
- [Risk] Clip commands can become unbalanced when subtrees emit no local commands. -> Mitigation: explicit clip tests with empty and non-empty clipped subtrees.
- [Risk] `DrawCommandList` is mutable, so cache ownership can be accidentally shared. -> Mitigation: cache APIs own command lists and tests assert command replacement/reuse behavior.
- [Risk] `UIElement.OnRender` could become too much API surface too early. -> Mitigation: keep hook protected virtual and small; controls can override later.

## Migration Plan

1. Add retained rendering primitives and focused cache tests.
2. Add `UIElement.OnRender(RenderContext)` and root renderer/cache ownership.
3. Add render queue processing and scheduler integration.
4. Add retained renderer composition and root command-list cache reuse.
5. Add dependency/counter/clip tests and update `ROADMAPv2.md`.
6. Validate with `dotnet test`, `openspec validate add-retained-rendering-cache --strict`, and `openspec validate --all --strict`.

Rollback before archive is simple: remove `UI/Rendering`, remove render hook/root renderer additions, and remove the OpenSpec change directory.

## Open Questions

- Should root command-list composition copy commands into a new list every cache rebuild, or can it reuse one mutable root list safely with explicit cache versioning?
- Should clip metadata live directly on `UIElement` later, or stay as render-layer metadata until controls/styles need it?
