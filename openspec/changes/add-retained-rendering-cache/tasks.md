## 1. Roadmap And Contract Alignment

- [x] 1.1 Update `ROADMAPv2.md` section 6 planning checklist; done when proposal, design, specs, tasks, and validation entries for `add-retained-rendering-cache` are visible and accurately checked.
- [x] 1.2 Keep section 6 scoped to retained rendering/cache; done when host integration, input bridge, styling, markup, controls, and real hit-test geometry remain assigned to later roadmap sections.
- [x] 1.3 Keep `DrawCommandListPool` optional; done when it is either a minimal tested shell or intentionally left unchecked until profiling proves the need.

## 2. Rendering Primitives And Contracts

- [x] 2.1 Add `UI/Rendering/RenderContext.cs`; done when render hooks receive `DrawingContext`, arranged bounds, inherited render state, and counters/diagnostics without backend-specific types.
- [x] 2.2 Add `UI/Rendering/IRenderableElement.cs`; done when render-capable retained elements have an explicit local render contract.
- [x] 2.3 Add `UI/Rendering/RenderLayer.cs`; done when MVP render layer metadata exists without implementing effects beyond the minimal contract.
- [x] 2.4 Add `UI/Rendering/ClipNode.cs`; done when retained clip metadata can translate to balanced `PushClip`/`PopClip` command boundaries.
- [x] 2.5 Add `UI/Rendering/RenderDependency.cs`; done when text, image, resource/theme, and custom render dependency versions can be compared.
- [x] 2.6 Add `UI/Rendering/RenderCounters.cs`; done when cache rebuilds, cache reuses, composed elements, and emitted commands can be counted in tests.

## 3. Element And Root Rendering Integration

- [x] 3.1 Update `UI/Elements/UIElement.cs`; done when elements expose a protected local `OnRender(RenderContext)` hook and render version/dependency state without owning child traversal.
- [x] 3.2 Update `UI/Elements/UIRoot.cs`; done when roots own retained render cache, renderer, and render queue processor wiring.
- [x] 3.3 Ensure visibility semantics affect rendering; done when `Collapsed` elements do not emit local or subtree commands and `Hidden` elements reserve layout but do not emit visible commands.
- [x] 3.4 Connect render-affecting property metadata to render version/dependencies; done when equal effective values do not invalidate render and changed render values do.

## 4. Element And Root Render Caches

- [x] 4.1 Add `UI/Rendering/ElementRenderCache.cs`; done when each retained element can store local `DrawCommandList`, local render version, content bounds, dependency snapshot, and validity state.
- [x] 4.2 Add `UI/Rendering/RetainedRenderCache.cs`; done when a root/subtree cache can store a cached root `DrawCommandList`, cache version, and per-element local caches.
- [x] 4.3 Add local cache rebuild behavior; done when dirty elements regenerate local command lists through `OnRender`.
- [x] 4.4 Add local cache reuse behavior; done when unchanged elements reuse local command lists and update counters.
- [x] 4.5 Add root command cache invalidation; done when changed local element caches invalidate/rebuild the composed root command list.

## 5. Render Queue And Scheduler Integration

- [x] 5.1 Add `UI/Rendering/RenderQueueProcessor.cs`; done when queued render-dirty elements rebuild local render caches through retained rendering.
- [x] 5.2 Integrate render processing with `UI/Invalidation/FramePhaseProcessors.cs`; done when root frame processing can provide a concrete `RenderCache` processor.
- [x] 5.3 Integrate render processing with `UI/Invalidation/UiFrameScheduler.cs` behavior without changing phase order; done when render-cache work still runs after arrange and before hit-test.
- [x] 5.4 Preserve failure behavior; done when failed render-cache processing keeps matching dirty flags and queued work.
- [x] 5.5 Preserve no-work frame behavior; done when unchanged frames do not regenerate local commands and can reuse the cached root command list.

## 6. Retained Renderer And Command Composition

- [x] 6.1 Add `UI/Rendering/RetainedRenderer.cs`; done when it composes retained visual trees into a cached root `DrawCommandList` for `IDrawingBackend`.
- [x] 6.2 Add `UI/Rendering/DrawCommandListBuilder.cs`; done when cached local commands are flattened in deterministic retained visual order.
- [x] 6.3 Emit parent local commands before descendant commands; done when tests prove parent-before-child ordering.
- [x] 6.4 Emit siblings in visual child order; done when tests prove retained visual child order controls command order.
- [x] 6.5 Exclude collapsed/hidden visible output appropriately; done when collapsed subtrees emit no commands and hidden elements emit no visible commands.
- [x] 6.6 Balance clip commands; done when clipped empty and non-empty subtrees emit matching `PushClip`/`PopClip`.

## 7. Optional Pooling Boundary

- [x] 7.1 Decide `UI/Rendering/DrawCommandListPool.cs` scope; done when the implementation either adds a minimal tested pool shell or leaves this roadmap file unchecked with a documented reason.
- [x] 7.2 If `DrawCommandListPool.cs` is added, cover rent/return/clear behavior with tests; done when pooling cannot leak stale commands.
- [x] 7.3 If pooling is deferred, verify no production rendering path depends on pooling; done when retained rendering correctness works without it.

## 8. Tests

- [x] 8.1 Add `tests/Cerneala.Tests/UI/Rendering/ElementRenderCacheTests.cs`; done when dirty rebuild, unchanged reuse, dependency staleness, and content bounds are covered.
- [x] 8.2 Add `tests/Cerneala.Tests/UI/Rendering/RetainedRenderCacheTests.cs`; done when root cache versioning and cached root command-list reuse are covered.
- [x] 8.3 Add `tests/Cerneala.Tests/UI/Rendering/RenderQueueProcessorTests.cs`; done when queued render work, failure preservation, and scheduler integration are covered.
- [x] 8.4 Add `tests/Cerneala.Tests/UI/Rendering/RetainedRendererTests.cs`; done when root composition, no-work draw reuse, and sibling cache isolation are covered.
- [x] 8.5 Add `tests/Cerneala.Tests/UI/Rendering/DrawCommandListBuilderTests.cs`; done when parent/child order, sibling order, collapsed exclusion, and clip balancing are covered.
- [x] 8.6 Add `tests/Cerneala.Tests/UI/Rendering/RenderDependencyTests.cs`; done when text/image/resource/custom dependency comparisons are covered.
- [x] 8.7 Add `tests/Cerneala.Tests/UI/Rendering/RenderCountersTests.cs`; done when cache hit, miss, composed element, and emitted command counts are covered.
- [x] 8.8 Add or intentionally defer `tests/Cerneala.Tests/UI/Rendering/DrawCommandListPoolTests.cs`; done according to the pooling decision in section 7.

## 9. Architecture Boundaries

- [x] 9.1 Verify `UI/Rendering` only depends on `UI/Drawing` abstractions and retained UI layers; done when it has no MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backend references.
- [x] 9.2 Verify `UI/Drawing` does not reference `UI/Rendering`, retained elements, layout, or render caches.
- [x] 9.3 Verify retained rendering does not mutate layout state; done when rendering consumes arranged bounds but does not run measure/arrange.
- [x] 9.4 Verify `IDrawingBackend.Render` can receive the cached root `DrawCommandList` without host integration code from section 7.

## 10. Validation

- [x] 10.1 Run `dotnet test`; done when the full test suite passes.
- [x] 10.2 Run `openspec validate add-retained-rendering-cache --strict`; done when the change validates successfully.
- [x] 10.3 Run `openspec validate --all --strict`; done when active changes and main specs validate successfully.
- [x] 10.4 Review `git status --short`; done when changed files are understood and unrelated edits such as `ConceptualIdeas.md` are not included accidentally.
