# Cerneala ROADMAPv2 Audit

Scope: static architecture audit of the extracted repository, with emphasis on `ROADMAPv2.md`, `architecture.md`, `docs/architecture-v2.md`, `UI/Core`, `UI/Elements`, `UI/Layout`, `UI/Rendering`, `UI/Input`, `UI/Controls`, `UI/Text`, `UI/Resources`, `UI/Hosting`, and `tests/Cerneala.Tests`.

Build/test note: `dotnet test Cerneala.slnx` and root `dotnet test` passed after the Runtime Preview completion gate (`1248` total tests, `0` failed). The audit findings below remain as architecture review context, with completed remediation items checked off where implementation has landed.

## Executive verdict

- [x] The direction is mostly aligned with ROADMAPv2: retained tree, typed state, explicit layout, backend-neutral rendering commands, backend-neutral input snapshots, and MonoGame adapter isolation are real.
- [x] The `Drawing` and `UI/Input` foundations are being reused rather than duplicated in the obvious low-level places.
- [x] The code avoids WPF `DependencyProperty` cloning and uses a saner typed `UiProperty<T>` model.
- [x] The static backend boundary looks mostly clean: no obvious MonoGame/Skia/HarfBuzz/SpriteBatch/Texture2D references in controls/layout/rendering outside adapter/text/drawing areas.
- [x] The retained rendering/game-loop contract has been tightened: local render-cache generation is scheduler-owned, root command composition is explicit during update, and draw submits committed commands.
- [x] Attached visual tree mutations now enqueue retained measure/arrange/render/hit-test work instead of relying on tree-version bookkeeping alone.
- [x] Styling is integrated into the frame scheduler through root-owned style/theme scope and `FramePhase.Style`.
- [x] Hit-test/input routing no longer rebuilds the mouse/touch/stylus bridge route map every input dispatch; `UIRoot` owns a retained `ElementInputCache`, and `HitTestQueue` drives the cache rebuild phase.
- [x] ROADMAPv2 maturity overclaim has been corrected by `freeze-later-experimental-scope`; Later and Optional/Experimental sections now distinguish descriptor/type existence from retained-pipeline, backend/platform, and scenario-complete maturity.

Brutal summary: the repo has a good skeleton and the retained update/draw, tree mutation, style scheduling, input route cache, root-owned resource invalidation, and roadmap honesty fixes have landed. Remaining risks are later foundation areas such as realistic text/layout behavior, accessibility/platform adapters, animation stress, markup/source generation, and package boundaries.

## Requested checks

- [ ] **1. Implementation respects ROADMAPv2 intent.** Partially. The broad layering is right, and the retained render, tree mutation, style phase, input route cache, and roadmap honesty/deferred scope fixes have landed; remaining concerns are later foundation areas.
- [ ] **2. WPF legacy API risk.** Some names are fine (`RoutedEvent`, `CommandBinding`, `Visibility`), but `AutomationPeer`, `ButtonAutomationPeer`, `TextBoxAutomationPeer`, and `ItemsControlAutomationPeer` pull the public shape toward WPF compatibility language without a compatibility goal. The `IsVisible`/`Visibility` split is now explicit and tested, but still needs API-shape review before public stabilization.
- [ ] **3. Over-engineering/YAGNI.** Later/Optional work was implemented too early: markup/source generation, advanced input categories, animation/storyboard, accessibility peers, advanced media descriptors, text editing, and IME scaffolding are ahead of core correctness.
- [ ] **4. Under-engineering.** The most dangerous under-built areas are now realistic text/layout/virtualization behavior; the retained render contract, tree mutation invalidation, style phase integration, route/hit-test caching, and root-owned resource invalidation have been remediated.
- [x] **5. Retained rendering + invalidation + game loop coherence.** The retained render and tree-mutation contracts have been fixed: draw-path generation is blocked, update owns cache generation/composition, and late tree mutations are processed during update.
- [x] **6. Drawing/UI/Input boundaries.** Mostly clean for backend references. `UI/Input` no longer references `ButtonBase` or `Thumb`; `InputControlBoundaryTests` now guards the control boundary.
- [ ] **7. WPF-like names familiar but not dragging design backward.** Mixed. Core property model is modern; accessibility and some input/control naming need a hard naming decision.
- [ ] **8. Tests cover the right risks.** There are many tests, but several prove file-level happy paths rather than the high-risk integration contracts. Missing tests are listed below.
- [x] **9. Unchecked ROADMAPv2 items can remain deferred.** The unchecked package split files can remain deferred, but the current single project’s hard dependency on MonoGame/Skia/HarfBuzz should be treated as a packaging-boundary risk, not ignored.
- [x] **10. Next five steps identified.** See “Next 5 steps, in order”.

## Must Fix

These are not polish items. They affect correctness of the retained/game-loop contract.

### 1. Tree mutation invalidation was broken

Files:

- `UI/Elements/UIElementCollection.cs`
- `UI/Elements/UIRoot.cs`
- `UI/Invalidation/DirtyPropagation.cs`
- `UI/Layout/LayoutManager.cs`
- `UI/Rendering/RetainedRenderCache.cs`
- `tests/Cerneala.Tests/UI/Elements/UIElementCollectionTests.cs`
- `tests/Cerneala.Tests/UI/Hosting/UiHostFrameContractTests.cs`

Original problem: `UIElementCollection.Add(...)` attached the child and called `root.IncrementTreeVersion()`, but did not invalidate owner layout, child layout, render cache, or hit-test data. Removal had a special `InvalidateOwnerForVisualChildRemoval()` path, but addition did not. Removal also skipped invalidating when `owner is UIRoot`.

Why this is bad: after the first frame, adding a visual child can make the root command cache invalid while leaving layout queues empty. The next `UiHost.Update(...)` can report a no-work frame while `RetainedRenderer.Render(...)` rebuilds commands lazily. That violates ROADMAPv2’s retained invalidation model.

Required changes:

- [x] In `UIElementCollection.Add(...)`, for `ElementChildRole.Visual`, enqueue owner/ancestor measure + arrange, child subtree measure + arrange, render, and hit-test invalidation.
- [x] In `UIElementCollection.Remove(...)`, handle `UIRoot` the same way as other visual owners for retained invalidation purposes.
- [x] Keep `root.IncrementTreeVersion()` as tree-version bookkeeping, not as a substitute for retained dirty work.
- [x] Add `UIElementCollection.InvalidateOwnerForVisualChildMutation(...)` or equivalent shared helper for add/remove.
- [x] Add tests in `tests/Cerneala.Tests/UI/Elements/UIElementCollectionInvalidationTests.cs` proving attached add/remove enqueue layout/render/hit-test.
- [x] Add `tests/Cerneala.Tests/UI/Hosting/UiHostLateTreeMutationTests.cs` proving a child added after the first frame is measured/arranged/rendered during `Update`, not lazily during `Draw`.

Implementation note: fixed by `fix-tree-mutation-invalidation`; attached visual add/remove now schedules retained measure, arrange, render-cache, and hit-test work during update, while tree-version increments remain bookkeeping.

### 2. Renderer had a backdoor that could run `OnRender` outside the render-cache phase

Files:

- `UI/Rendering/DrawCommandListBuilder.cs`
- `UI/Rendering/ElementRenderCache.cs`
- `UI/Rendering/RenderQueueProcessor.cs`
- `UI/Rendering/RetainedRenderer.cs`
- `UI/Hosting/UiHost.cs`
- `UI/Invalidation/FrameStats.cs`

Original problem: `DrawCommandListBuilder.AppendElement(...)` called `localCache.Ensure(element, counters)`. `Ensure(...)` could rebuild the local command list by calling the element render hook. That meant root composition could generate local drawing commands, even when the render queue did not process that element.

Why this is bad: `UiHost.Update(...)` collects `FrameStats` from `currentRoot.ProcessFrame()`, then calls `currentRoot.RetainedRenderer.Render(currentRoot)`. Any command generation done during that render call is not represented in the frame stats. Worse, `UiHost.Draw(...)` calls `RetainedRenderer.Submit(...)`, which calls `Render(root)` and can also rebuild if the root cache is invalid.

Required changes:

- [x] Make `RenderQueueProcessor.Process(...)` the only production path that can call `ElementRenderCache.Ensure(...)` for local command generation.
- [x] Make `DrawCommandListBuilder.Build(...)` compose only already-valid local element caches. It should not call `OnRender(...)` transitively.
- [x] Decide behavior when root composition finds an invalid/missing local cache: fail fast in debug/tests, enqueue render work, or return the previous root command list until the next update. Do not silently render from outside the scheduler.
- [x] Move root command-list composition into an explicit update commit phase, or count it explicitly in `FrameStats` as composition work separate from local render-cache rebuilds.
- [x] Make `UiHost.Draw(...)` submit only a previously committed cached root command list. Draw must not generate local commands.
- [x] Add `tests/Cerneala.Tests/UI/Rendering/RenderBackdoorContractTests.cs` proving `DrawCommandListBuilder` never calls `OnRender(...)` for stale/missing local caches.
- [x] Add `tests/Cerneala.Tests/UI/Rendering/RetainedRendererDrawPurityTests.cs` proving `UiHost.Draw(...)` cannot increment element render counters.
- [x] Add `tests/Cerneala.Tests/UI/Hosting/UiHostFrameStatsIntegrityTests.cs` proving all render-cache generation done during update is counted.

Implementation note: fixed by `fix-retained-render-frame-contract`; local render-cache generation is scheduler-owned, root command-list composition is explicit during update, and draw submission uses the last committed root commands.

### 3. `RetainedRenderer.Submit(...)` originally copied commands every draw

Files:

- `UI/Rendering/RetainedRenderer.cs`
- `Drawing/IDrawingBackend.cs`
- `tests/Cerneala.Tests/UI/Rendering/RetainedRendererTests.cs`

Original problem: `Submit(...)` called `backend.Render(CopyCommands(Render(root)))`. That protected the cache from a mutating backend, but it allocated/copied a new `DrawCommandList` every draw. ROADMAPv2 explicitly wants game-loop-friendly retained UI. A per-frame copy of the root command list was exactly the kind of hidden cost retained rendering is supposed to avoid.

Implementation note: fixed by `fix-retained-render-frame-contract`; `RetainedRenderer.Submit(...)` now submits the committed root command list directly through `Render(root)`, and `CopyCommands(...)` is no longer present.

Required changes:

- [x] Strengthen `IDrawingBackend.Render(...)` contract so backends treat command lists as read-only during submission.
- [x] Resolve the immutable/read-only submission concern through the backend read-only contract. A separate immutable command-list view can remain a future option if backend APIs need it.
- [x] Remove the per-draw `CopyCommands(...)` from the hot path.
- [x] Replace `BackendCannotMutateCachedRootCommandsDuringSubmit` with a test that enforces backend read-only behavior or validates the immutable view.
- [x] Add `tests/Cerneala.Tests/UI/Rendering/BackendSubmitAllocationTests.cs` or a simpler command-list identity test proving unchanged draw frames reuse cached root commands without copying.

### 4. Styling was not a retained frame phase yet

Files:

- `UI/Elements/UIElement.cs`
- `UI/Invalidation/InvalidationFlags.cs`
- `UI/Invalidation/FramePhase.cs`
- `UI/Invalidation/UiFrameScheduler.cs`
- `UI/Styling/StyleInvalidation.cs`
- `UI/Styling/StyleApplicator.cs`
- `UI/Styling/PseudoClass.cs`
- `UI/Styling/ThemeProvider.cs`
- `tests/Cerneala.Tests/UI/Styling/StyleInvalidationTests.cs`

Original problem: `InvalidationFlags.Style` existed, and `FramePhase.Style` existed, but the scheduler did not process a style phase. `UIElement.MapInvalidationOptions(...)` mapped `UiPropertyOptions.AffectsStyle` to `InvalidationFlags.Render`, not `InvalidationFlags.Style`. `StyleInvalidation.Track(...)` was manual opt-in and not root/host-owned.

Why this is bad: style/theme invalidation is currently a side system, not part of retained frame scheduling. That makes visual state styling brittle and makes ROADMAPv2’s “style invalidation reapplies style and then raises property-specific invalidations” claim false.

Required changes:

- [x] Map `UiPropertyOptions.AffectsStyle` to `InvalidationFlags.Style`, not render.
- [x] Add a scheduler-owned style processor before measure/arrange/render.
- [x] Decide whether style work uses a dedicated `StyleQueue` or a typed style processor over dirty elements.
- [x] Make `UIRoot` or `UiHost` own style/theme services for an attached tree.
- [x] Make element attach/detach register/unregister with the style system automatically when a stylesheet is active.
- [x] Remove string-name pseudo-class detection in `StyleInvalidation.AffectsPseudoClass(...)` (`property.Name == "IsPressed"`, `property.Name == "IsSelected"`). Use `IStylePseudoClassProvider`, property metadata, or explicit pseudo-class registration.
- [x] Add `tests/Cerneala.Tests/UI/Styling/StyleSchedulerIntegrationTests.cs` proving pseudo-class/theme changes are applied through `UiHost.Update(...)` and reflected in frame stats/invalidation.

Implementation note: fixed by `integrate-style-phase`; `AffectsStyle` now queues retained style work, `UIRoot` owns the active style/theme scope, the scheduler processes `FramePhase.Style` before layout/render phases, and pseudo-class invalidation uses explicit property registration instead of string property-name checks.

### 5. Input route/hit-test caching was not retained

Files:

- `UI/Input/ElementInputBridge.cs`
- `UI/Input/ElementInputRouteBuilder.cs`
- `UI/Input/ElementInputRouteMap.cs`
- `UI/Input/HitTestService.cs`
- `UI/Invalidation/HitTestQueue.cs`
- `UI/Elements/UIElementCollection.cs`
- `UI/Layout/LayoutManager.cs`

Original problem: `ElementInputBridge.Dispatch(...)` built a fresh `ElementInputRouteMap` every input frame. `HitTestQueue` was only a queue/counter; it did not rebuild or own retained hit-test/route data. The architecture says hit-test data should rebuild only when dirty.

Required changes:

- [x] Add a retained `ElementInputCache` or `HitTestRouteCache` owned by `UIRoot`.
- [x] Rebuild route/hit-test data only when tree version, layout bounds, visibility/enabled state, handlers, or hit-test invalidation requires it.
- [x] Make `ElementInputBridge.Dispatch(...)` consume the retained cache instead of rebuilding the route map every frame.
- [x] Add handler add/remove invalidation if route maps cache handler lists.
- [x] Add `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs` proving unchanged input frames do not rebuild route data.
- [x] Add `tests/Cerneala.Tests/UI/Input/HitTestCacheInvalidationTests.cs` proving layout bounds, visibility, enabled, and visual tree mutations invalidate hit-test data.

Implementation note: fixed by `cache-input-route-hit-test`; `UIRoot` now owns a retained `ElementInputCache`, route/hit-test data rebuilds only when hit-test/input-route invalidation marks it dirty, mouse/touch/stylus dispatch consume the retained route map, handler changes invalidate the cache, and `UI/Input` no longer depends directly on concrete controls.

### 6. `UI/Input` knew too much about controls

Files:

- `UI/Input/ElementInputBridge.cs`
- `UI/Controls/Primitives/ButtonBase.cs`
- `UI/Controls/Primitives/Thumb.cs`
- `UI/Input/CommandRouter.cs`
- `UI/Input/PointerCaptureManager.cs`

Original problem: `ElementInputBridge` directly referenced `ButtonBase` and `Thumb`. That made the input bridge a control-behavior coordinator. It should route input and update generic state; controls should opt into behavior through handlers/interfaces.

Required changes:

- [x] Move button command execution into `ButtonBase` event handlers or an `ICommandSource` interface in an input-neutral location.
- [x] Move thumb drag behavior behind an interface such as `IDragSource`/`IPointerDragHandler`, or have `Thumb` register routed handlers itself.
- [x] Keep `ElementInputBridge` generic: hit-test, hover, capture, focus, routed event dispatch, text input dispatch.
- [x] Add `tests/Cerneala.Tests/UI/Input/InputControlBoundaryTests.cs` or extend architecture boundary tests so `UI/Input` does not depend on `UI/Controls`.

Implementation note: fixed by `cache-input-route-hit-test`; `ButtonBase` and `Thumb` now implement input-level interfaces, `ElementInputBridge` no longer references concrete controls, and `InputControlBoundaryTests` verifies the `UI/Input` boundary.

### 7. Focus has no real focusability policy

Files:

- `UI/Input/FocusManager.cs`
- `UI/Input/KeyboardNavigation.cs`
- `UI/Input/FocusScope.cs`
- `UI/Elements/UIElement.cs`
- `UI/Controls/TextBox.cs`
- `UI/Controls/Primitives/ButtonBase.cs`

Problem: focus can be assigned to any routed element that appears in the current route map. There is no explicit `Focusable` property or focus policy. `FocusScope` is just an owner wrapper and does not participate in traversal/storage.

Required changes:

- [x] Add an explicit focusability contract. Keep it simple: `Focusable`, `IsTabStop`, or a small `IFocusTarget` interface.
- [x] Prevent disabled/invisible/non-focusable elements from becoming keyboard focus targets.
- [x] Decide whether root-level focus scope memory exists in MVP or remains deferred.
- [x] Add `tests/Cerneala.Tests/Input/FocusPolicyTests.cs` proving focus ignores non-focusable, disabled, invisible, detached, and collapsed elements.

Implementation note: fixed by `fix-focus-visibility-semantics`; `UIElement` now exposes `Focusable` and `IsTabStop`, `FocusPolicy` gates `FocusManager` and pointer focus, `ButtonBase`/`TextBoxBase` opt in by default, and root-level focus scope memory remains deferred.

## Should Fix

These can follow the Must Fix items, but they should happen before calling Core complete.

### 1. ROADMAPv2 statuses are too optimistic

Files:

- `ROADMAPv2.md`

Problem: many sections are `[x]` because a file and a test exist, not because the feature is architecturally complete. This creates false project memory.

Examples:

- `UI/Media/LinearGradientBrush.cs` and `UI/Media/RadialGradientBrush.cs` exist, but `Shape.RenderGeometry(...)` only uses `Brush.SolidColor`. Gradients do not render.
- The descriptor-only `ShadowEffect` and `OpacityLayer` stubs were removed so a future effects system starts from an end-to-end design.
- `UI/Media/PathGeometry.cs` is a point list. `Shape.DrawPathStroke(...)` emits line segments only. No real path fill/stroke command exists.
- `UI/Text/TextWrapping.cs` exists, but wrapping uses fixed character-width slicing. That is acceptable as MVP text measurement, not production text layout.
- `UI/Text/TextTrimming.cs` only has `None`.
- `UI/Accessibility/AutomationPeer.cs` exists, but platform accessibility is not implemented.
- `UI/Markup` and `Cerneala.SourceGen` exist, but they should remain optional and frozen until the core retained contract is stable.

Required changes:

- [ ] Add an audit marker or status correction pass to ROADMAPv2 after Must Fix items land. Do not rewrite the roadmap; just stop treating descriptor files as finished product features.
- [ ] In Superpowers planning/checklist artifacts, distinguish “type exists”, “wired into retained pipeline”, “backend-supported”, and “scenario-complete”.
- [ ] Add explicit “experimental/frozen” status to Later/Optional areas that should not drive core design.

Implementation note: fixed by `freeze-later-experimental-scope`; `ROADMAPv2.md` now distinguishes type existence, retained-pipeline integration, backend/platform support, and scenario completeness. Later and Optional/Experimental areas for media, accessibility adapters, animation expansion, markup/source generation, and advanced input are explicitly marked partial or frozen instead of completed.

### 2. Main project still has hard backend/package dependencies

Files:

- `Cerneala.csproj`
- `Drawing/MonoGame/*`
- `UI/Input/MonoGame/*`
- `UI/Hosting/MonoGame/*`
- `UI/Resources/MonoGame/*`
- `Drawing/Text/Skia*`

Problem: source boundaries are mostly clean, but the main package still references `MonoGame.Framework.DesktopGL`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`, and `HarfBuzzSharp`. ROADMAPv2 says the package split can be deferred, and that is true. But the core dependency story is not neutral if every core consumer must bring MonoGame/Skia/HarfBuzz.

Required changes:

- [x] Keep `Cerneala.Core.csproj` and `Cerneala.MonoGame.csproj` deferred because the project is not ready for a real package split.
- [x] Record a Superpowers decision note for "single project now, adapter dependencies later" so this does not become accidental architecture.
- [x] Defer `PrivateAssets`/conditional compilation because adding build complexity without a real split would be premature.
- [x] Defer package-shape tests until split criteria are ready and split projects exist.

Implementation note: fixed by `clarify-package-boundary-dependencies`; `ROADMAPv2.md` now records that `Cerneala.csproj` intentionally remains the single MVP project while carrying `MonoGame.Framework.DesktopGL`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`, and `HarfBuzzSharp`. The future split criteria distinguish source isolation from package dependency isolation, and package-shape tests remain deferred until the split projects exist.

### 3. Inherited property behavior is only store-level, not tree-level

Files:

- `UI/Core/UiPropertyStore.cs`
- `UI/Core/UiPropertyOptions.cs`
- `UI/Elements/UIElement.cs`
- `tests/Cerneala.Tests/UI/Core/InheritedUiPropertyTests.cs`

Problem: `UiPropertyOptions.Inherits` and `UiPropertyValueSource.Inherited` exist, but there is no clear automatic propagation through the retained logical/visual tree. Tests mostly prove manual inherited source precedence, not real tree inheritance.

Required changes:

- [x] Decide which properties actually inherit in Cerneala v2: probably font family, font size, foreground, theme/resource scope, flow direction later.
- [x] Implement explicit inherited-value propagation or document that inheritance is not automatic yet.
- [x] Add `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs`.
- [x] Avoid WPF-style global inheritance magic. Keep propagation explicit and invalidation-driven.

Implementation note: fixed by `implement-inherited-property-tree-propagation`; font family, font size, and foreground now inherit through the retained visual tree in an explicit `InheritedProperties` frame phase owned by `UIRoot`. Logical-tree inheritance, theme/resource scope inheritance, and flow direction remain deferred.

### 4. `IsVisible` plus `Visibility` is ambiguous

Files:

- `UI/Elements/UIElement.cs`
- `UI/Layout/Visibility.cs`
- `UI/Input/ElementInputRouteBuilder.cs`
- `UI/Rendering/DrawCommandListBuilder.cs`

Problem: `IsVisible` is a bool property and `Visibility` is a `Visible/Hidden/Collapsed` enum. Rendering and input check both. Layout uses `Visibility.Collapsed`. This is familiar to WPF developers, but it risks semantic confusion.

Required changes:

- [x] Decide whether `IsVisible` means “participates in rendering/hit-test but not layout” or whether it should be removed/deprecated in favor of `Visibility`.
- [x] If both remain, document exact semantics and add tests for all combinations.
- [x] Add `tests/Cerneala.Tests/UI/Layout/VisibilityCombinationTests.cs`.

Implementation note: fixed by `fix-focus-visibility-semantics`; `Visibility` is the primary public layout/render/input semantic, `IsVisible=false` remains a runtime render/input gate that does not collapse layout, and `UIElementVisibility` centralizes participation checks.

### 5. Layout diagnostics undercount real child work

Files:

- `UI/Layout/Panels/Panel.cs`
- `UI/Layout/Panels/StackPanel.cs`
- `UI/Layout/Panels/Grid.cs`
- `UI/Layout/LayoutManager.cs`
- `UI/Invalidation/FrameStats.cs`

Problem: panels call `child.Measure(...)` and `child.Arrange(...)` recursively inside their own layout methods. Scheduler stats count queued elements, not necessarily actual child measure/arrange calls. That can make diagnostics lie on large trees.

Required changes:

- [x] Decide whether `FrameStats.MeasuredElements` means queued layout items or actual measure calls.
- [x] If diagnostics should reflect real work, add instrumentation at `UIElement.Measure(...)` and `UIElement.Arrange(...)`, or expose both counters.
- [x] Add `tests/Cerneala.Tests/UI/Layout/LayoutDiagnosticsAccuracyTests.cs`.

Implementation note: fixed by `clarify-layout-scheduler-contract-and-diagnostics`; `MeasuredElements` and `ArrangedElements` remain queued scheduler phase counts, while `MeasureCalls` and `ArrangeCalls` count actual recursive layout method calls.

### 6. Layout queue stability needs a decision

Files:

- `UI/Invalidation/UiFrameScheduler.cs`
- `UI/Invalidation/LayoutQueue.cs`
- `ROADMAPv2.md`

Problem: ROADMAPv2 used to say “process layout queue until stable”. `UiFrameScheduler` snapshots each phase once. Work enqueued during a phase can be deferred to a later frame. That behavior is now the explicit MVP contract.

Required changes:

- [x] Decide whether MVP processes a single snapshot per phase or loops until queues are stable.
- [x] If single-pass is intentional, update ROADMAP and Superpowers plan wording.
- [x] If stable processing is required, implement bounded phase loops to avoid infinite invalidation.
- [x] Add `tests/Cerneala.Tests/UI/Invalidation/FrameSchedulerStabilityTests.cs`.

Implementation note: MVP processes one deterministic snapshot per phase. Same-phase work enqueued during processing is deferred to a later frame; downstream phase work can still run in the same frame if its snapshot has not yet been taken. Bounded until-stable loops remain deferred until `FrameBudget` scheduling exists.

### 7. Resource invalidation is too control-specific

Files:

- `UI/Resources/ResourceDependencyTracker.cs`
- `UI/Resources/ResourceStore.cs`
- `UI/Controls/TextBlock.cs`
- `UI/Controls/Image.cs`
- `UI/Rendering/RenderDependency.cs`

Problem: `TextBlock` and `Image` subscribe to `ResourceStore` directly when their provider is specifically a `ResourceStore`. `ResourceDependencyTracker` exists, but root/host resource dependency ownership is not clearly integrated. This will not scale to styles/themes/templates/resources.

Required changes:

- [x] Define a resource observation contract beyond `ResourceStore` if custom providers can change.
- [x] Let `UIRoot` or a retained resource service own dependency tracking for attached elements.
- [x] Ensure resource changes enqueue the correct invalidation based on dependency metadata.
- [x] Add `tests/Cerneala.Tests/UI/Resources/HostResourceInvalidationIntegrationTests.cs`.

Implementation note: fixed by `root-owned-resource-invalidation`; `IObservableResourceProvider` separates change observation from `ResourceStore`, `UIRoot` owns the attached-tree `ResourceDependencyTracker`, `TextBlock` and `Image` resolve local provider overrides or the root provider without direct `ResourceStore` subscriptions, and host integration tests cover font/image resource changes plus custom observable and non-observable providers.

### 8. Text services are MVP-fake but ROADMAPv2 reads mature

Files:

- `UI/Text/LineBreakService.cs`
- `UI/Text/TextMeasurer.cs`
- `UI/Text/TextRenderer.cs`
- `UI/Text/TextRunStyle.cs`
- `UI/Controls/TextBlock.cs`
- `UI/Controls/Button.cs`

Problem: `LineBreakService` uses `fontSize * 0.5f` character width and substring slicing. `TextRenderer.Render(...)` draws the whole original text once, not per measured line. `Button.MeasureStringContent()` duplicates text measurement with the same rough formula instead of using `TextMeasurer`/`TextBlock`/`ContentPresenter`.

Required changes:

- [ ] Keep rough text layout if it is explicitly MVP.
- [ ] Do not claim full wrapping/trimming/multiline rendering yet.
- [x] Make `Button` inherit content behavior from `ContentControl` or route string content through `ContentPresenter`/`TextBlock`.
- [ ] Add `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs` proving render output matches measured lines if wrapping is exposed.
- [x] Add `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs` proving Button does not duplicate content/layout logic.

Implementation note: fixed by `clarify-text-services-mvp` and `consolidate-button-content-composition`; `ROADMAPv2.md` now marks line breaking, wrapping, trimming, and multiline rendering as deterministic MVP/partial scope, `TextRenderer` draws measured MVP lines instead of the original unwrapped string, `ButtonBase` inherits `ContentControl`, `Button` uses shared `ContentControl.ContentProperty`/child ownership, and `Button` string content uses shared text services instead of local width formulas.

### 9. Items virtualization is not mature enough for Core-complete claims

Files:

- `UI/Controls/ItemsControl.cs`
- `UI/Controls/ItemsPresenter.cs`
- `UI/Controls/Items/ItemContainerGenerator.cs`
- `UI/Layout/Panels/VirtualizingStackPanel.cs`

Problem: the generator can recycle containers, but `ItemsPresenter.RefreshOwnerItems(...)` recreates a panel root on refresh. `UpdateVirtualizationFromScrollInfo(...)` can count an arbitrary `IEnumerable` with `Cast<object?>().Count()`. This is not a large-data-safe virtualization architecture.

Required changes:

- [ ] Preserve the panel root when the panel template/type has not changed.
- [ ] Avoid enumerating arbitrary `IEnumerable` just to count items.
- [ ] Add explicit item source contracts for count/index access when virtualization is enabled.
- [ ] Add `tests/Cerneala.Tests/UI/Layout/VirtualizationScaleTests.cs` with 10k+ items proving realized count stays bounded and unchanged containers are reused.

### 10. Public non-generic factories use reflection

Files:

- `UI/Styling/Setter.cs`
- `UI/Controls/Templates/TemplateBinding{T}.cs`

Problem: `Setter.Create(...)` and `TemplateBinding.Create(...)` use `MakeGenericType(...)` and `Activator.CreateInstance(...)`. That is not catastrophic if isolated to markup/tooling, but ROADMAPv2 explicitly prefers avoiding reflection-heavy behavior.

Required changes:

- [ ] Prefer generic public APIs in hot/control authoring paths.
- [ ] Mark non-generic factories as tooling/markup convenience, not core runtime path.
- [ ] Add `tests/Cerneala.Tests/Architecture/ReflectionBoundaryTests.cs` that allows reflection in diagnostics/markup/tests but not in core hot paths.

## Nice Later

These are valid product areas, but they should not consume design energy before the Must Fix and Should Fix lists are closed.

- [ ] Keep `UI/Animation/*` small and explicit. Do not expand `Storyboard`/timeline composition until style/layout/render invalidation is proven under animation stress.
- [ ] Keep `UI/Markup/*` and `Cerneala.SourceGen/*` frozen as optional. Do not add XAML-like features, runtime object graph magic, or reflection-heavy bindings.
- [ ] Keep `UI/Accessibility/Semantics*` as the preferred architecture. Decide later whether `AutomationPeer` names survive as public API.
- [ ] Keep `UI/Text/TextEditor.cs`, `TextCompositionManager`, `TextBox`, and `PasswordBox` limited until text layout, platform text input, and selection rendering are more real.
- [ ] Keep `UI/Data/StringPropertyPath.cs` unsupported. That is a good decision. Do not add string-path binding until typed binding and templates are insufficient in real scenarios.
- [ ] Keep `DrawCommandListPool` out of correctness. Add it only after profiling proves command-list allocation is a bottleneck; per-draw root command copying has already been removed.
- [ ] Add richer diagnostics/devtools after stats are honest. Diagnostics built on false counters are worse than no diagnostics.

## Do Not Build Yet

- [ ] Do not add more controls until tree mutation invalidation, render purity, style phase, and input cache are fixed.
- [ ] Do not expand advanced rendering/media. Gradients, shadows, opacity layers, render targets, and real paths need drawing command/backend semantics first.
- [ ] Do not add a WPF-compatible binding engine. Keep typed observation and explicit setters.
- [ ] Do not build full native accessibility adapters until the platform-neutral semantics API is stable.
- [ ] Do not build full IME/text editing until text layout, platform text input, and retained selection/caret invalidation are coherent.
- [ ] Do not implement `FrameBudget` scheduling yet. First prove the all-work frame contract is correct.
- [ ] Do not split projects just to satisfy roadmap optics. Split only when package dependency boundaries are ready and tested.
- [ ] Do not add more WPF event/name coverage unless there is a Cerneala-native scenario requiring it.

## Architecture Risks

### Risk 1: Silent work outside the update phase

- [x] Original risk: `RetainedRenderer.Render(...)` could generate local commands after `FrameStats` were produced.
- [x] Consequence: performance diagnostics could lie, draw could become non-pure, and unchanged game-loop frames could allocate/rebuild unexpectedly.
- [x] Mitigation: render-cache generation is scheduler-only; draw submits last committed output.
- [x] Status: mitigated by `fix-retained-render-frame-contract`; render-cache generation is scheduler-owned and draw/submit use committed commands.

### Risk 2: Tree-version invalidation is being used as a shortcut

- [x] Original risk: `root.IncrementTreeVersion()` invalidated root composition but did not schedule layout/hit-test/render-cache work.
- [x] Consequence: visual tree changes could skip layout and rely on lazy render behavior.
- [x] Mitigation: tree mutations raise retained invalidation requests with explicit flags.
- [x] Status: mitigated by `fix-tree-mutation-invalidation`; visual add/remove schedules retained measure/arrange/render/hit-test work and tree version remains bookkeeping.

### Risk 3: Style/theme system can become parallel state

- [x] Current risk: style tracking is opt-in and not rooted in `UIRoot`/`UiHost`.
- [x] Consequence: some elements update styles, some do not; pseudo-classes become string conventions; theme changes become manual refresh bugs.
- [x] Mitigation: scheduler-owned style phase and explicit style scope services.
- [x] Status: mitigated by `integrate-style-phase`; style work is scheduler-owned and rooted through `UIRoot`.

### Risk 4: Input route tree duplication

- [x] Current risk: retained UI tree and `UiInputTree` are bridged by rebuilding a transient route map every dispatch.
- [x] Consequence: input cost scales with tree size every frame, and `HitTestQueue` does not mean what it says.
- [x] Mitigation: root-owned retained input/hit-test cache keyed by tree/layout/input-affecting versions.
- [x] Status: mitigated by `cache-input-route-hit-test`; mouse/touch/stylus dispatch now consume `UIRoot.InputCache`, and handler/tree/layout/input-affecting invalidations rebuild it when dirty.

### Risk 5: WPF naming surface may ossify before Cerneala semantics are stable

- [ ] Current risk: `AutomationPeer`, many routed WPF event names, `Visibility`, `CommandBinding`, `RoutedCommand`, `FocusManager`, `TextBox`, etc. make the API feel familiar, but some imply WPF behavior Cerneala does not intend to clone.
- [ ] Consequence: users expect WPF compatibility and hidden magic.
- [ ] Mitigation: keep names only where behavior is intentionally similar and explicit. Rename or hide WPF-specific names that imply compatibility.

### Risk 6: Later features will freeze bad core seams

- [ ] Current risk: markup, sourcegen, accessibility, animation, advanced input, media, and text editing already exist.
- [ ] Consequence: core fixes become breaking changes across too many early APIs.
- [ ] Mitigation: freeze those areas and allow breaking changes until Must Fix is done.

### Risk 7: Descriptor-level media APIs pretend to be rendering APIs

- [ ] Current risk: brushes/geometries/effects exist without full drawing command/backend support.
- [ ] Consequence: controls can accept values that silently do nothing or degrade to solid-only behavior.
- [ ] Mitigation: either mark these APIs experimental or wire them to actual `DrawCommandKind` and backend behavior.

### Risk 8: Tests create false confidence

- [ ] Current risk: many tests prove class existence and simple behavior, not frame-level invariants under mutation, resource/style changes, and large retained trees.
- [ ] Consequence: the roadmap looks complete while the architecture can still violate its central performance contract.
- [ ] Mitigation: add integration tests around late mutation, draw purity, cache reuse, style/resource invalidation, input cache reuse, and virtualization scale.

Implementation note: partially mitigated by `create-retained-ui-mvp-vertical-slice`; `RetainedAppSample` is now registered first in the playground selector, and `RetainedVerticalSliceTests` prove first-frame retained work, unchanged no-work frames, draw purity, command mutation invalidation, and root-owned font resource invalidation for a cohesive sample tree.

## Recommended Next Superpowers Plans

Use Superpowers for these as implementation plans, not as speculative feature work. Each plan should start from the audit finding, produce a short design note, then drive red/green tests before production changes.

### `fix-retained-render-frame-contract`

Planning artifacts:

- `ROADMAPv2_AUDIT.md`
- `ROADMAPv2.md`
- `docs/architecture-v2.md`
- `docs/diagrams/retained-frame-loop.md`

Tasks:

- [ ] Specify that only the render-cache phase may rebuild local element command lists.
- [ ] Specify that root command-list composition is an update commit step or a counted frame phase.
- [ ] Specify that `UiHost.Draw(...)` submits the last committed root command list and never calls `OnRender(...)`.
- [ ] Specify backend command-list immutability/read-only behavior.
- [ ] Add scenarios for post-update invalidation, late tree mutation, and unchanged draw frames.

### `fix-tree-mutation-invalidation`

Planning artifacts:

- `ROADMAPv2_AUDIT.md`
- `ROADMAPv2.md`
- `docs/architecture-v2.md`

Tasks:

- [ ] Define exact invalidation flags for visual add/remove/reorder.
- [ ] Define logical-tree mutation effects for inherited properties/resources/styles.
- [ ] Define cache eviction or stale-cache behavior for removed visual subtrees.
- [ ] Add scenarios for attached-root child add/remove after first frame.

### `integrate-style-phase`

Planning artifacts:

- `ROADMAPv2_AUDIT.md`
- `ROADMAPv2.md`
- `docs/architecture-v2.md`

Tasks:

- [ ] Define style queue/phase ownership.
- [ ] Define how `UiPropertyOptions.AffectsStyle` maps to retained work.
- [ ] Define pseudo-class registration without string property-name checks.
- [ ] Define theme change propagation through root/host style scope.
- [ ] Add scenarios for hover/pressed/focus/selected/disabled style invalidation.

### `cache-input-route-hit-test`

Planning artifacts:

- `ROADMAPv2_AUDIT.md`
- `ROADMAPv2.md`
- `docs/architecture-v2.md`

Tasks:

- [x] Define root-owned route/hit-test cache.
- [x] Define cache invalidation triggers: tree changes, layout bounds changes, visibility, enabled, handler changes, capture changes where relevant.
- [x] Define that pointer movement may run hit-test lookup every frame, but not rebuild route data every frame.
- [x] Remove or formalize transient `UiInputTree` usage.

### `clarify-text-services-mvp`

Planning artifacts:

- `ROADMAPv2_AUDIT.md`
- `ROADMAPv2.md`
- `architecture.md`

Tasks:

- [ ] Mark current line breaking as MVP deterministic approximation.
- [ ] Do not claim production shaping/wrapping/trimming until rendering and measurement align.
- [ ] Require controls with text content to use shared text services or a content presenter path.

### `freeze-later-experimental-scope`

Planning artifacts:

- `ROADMAPv2_AUDIT.md`
- `ROADMAPv2.md`
- `docs/architecture-v2.md`

Tasks:

- [x] Add “experimental/frozen until retained core contract is stable” language.
- [x] Separate descriptor/API existence from scenario-complete implementation.
- [x] Require backend support before marking advanced media as implemented.
- [x] Require platform adapter support before marking advanced input/accessibility as implemented.

### `clarify-package-boundary-dependencies`

Planning artifacts:

- `ROADMAPv2_AUDIT.md`
- `ROADMAPv2.md`
- `Cerneala.csproj`

Tasks:

- [x] Keep package split project files deferred.
- [x] Add explicit risk/requirement that the main package currently carries adapter dependencies.
- [x] Define the future acceptance criteria for splitting core/adapters without forcing the split now.

## Test gaps to add

- [x] `tests/Cerneala.Tests/UI/Elements/UIElementCollectionInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Hosting/UiHostLateTreeMutationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Hosting/UiHostFrameStatsIntegrityTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/RenderBackdoorContractTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/RetainedRendererDrawPurityTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Rendering/BackendSubmitAllocationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Invalidation/FrameSchedulerStabilityTests.cs`
- [x] `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Input/HitTestCacheInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Input/InputControlBoundaryTests.cs`
- [x] `tests/Cerneala.Tests/Input/FocusPolicyTests.cs`
- [x] `tests/Cerneala.Tests/UI/Styling/StyleSchedulerIntegrationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Resources/HostResourceInvalidationIntegrationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/LayoutDiagnosticsAccuracyTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/VisibilityCombinationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/VirtualizationScaleTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`
- [ ] `tests/Cerneala.Tests/Architecture/ReflectionBoundaryTests.cs`
- [ ] `tests/Cerneala.Tests/Architecture/PublicApiLegacyNameTests.cs`

## WPF-like naming decision list

Keep unless behavior drifts:

- [x] `RoutedEvent`, `RoutingStrategy`, `RoutedEventArgs` — useful and already in `UI/Input` foundation.
- [x] `CommandBinding`, `RoutedCommand` — acceptable if command routing stays explicit and testable.
- [x] `Visibility` — acceptable if semantics are documented and not mixed ambiguously with `IsVisible`.
- [x] `Measure`/`Arrange` — familiar and still the right retained layout vocabulary.

Reconsider or constrain:

- [ ] `AutomationPeer` and concrete peer names — prefer `SemanticsProvider`/`SemanticsNode` as public Cerneala architecture; keep peer names only as adapter/internal compatibility language if deliberately chosen.
- [x] `IsVisible` plus `Visibility` — `Visibility` is primary; `IsVisible=false` remains a runtime render/input gate that does not collapse layout.
- [ ] More WPF event surface in `InputEvents.cs` — do not add names unless Cerneala behavior is implemented.
- [ ] `Storyboard` — keep only if timeline composition is really needed; otherwise prefer explicit game-loop animation scheduler.
- [ ] `TemplateBinding` — keep the concept, but avoid runtime-reflection creation in core authoring paths.

## What can remain deferred

- [x] `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, `Cerneala.Tests.Core.csproj`, and `Cerneala.Tests.MonoGame.csproj` can remain unchecked/deferred for now.
- [x] Full package split should wait until the core/adapters API line is stable.
- [x] The dependency problem behind the package split is explicitly tracked instead of deferred indefinitely. `Cerneala.csproj` currently pulls MonoGame/Skia/HarfBuzz into the main package.
- [x] `DrawCommandListPool` can remain deferred.
- [x] String property path binding can remain unsupported.
- [x] Full native accessibility can remain deferred.
- [x] Full IME/text editing can remain deferred.
- [x] Advanced media rendering can remain deferred.

## Next 5 steps, in order

1. [x] **Fix retained frame/render contract.** Make render-cache generation scheduler-only, make root composition explicit/countable, remove draw-path generation, remove per-draw command-list copying. Primary files: `UI/Rendering/DrawCommandListBuilder.cs`, `UI/Rendering/RetainedRenderer.cs`, `UI/Rendering/RenderQueueProcessor.cs`, `UI/Hosting/UiHost.cs`.
2. [x] **Fix visual tree mutation invalidation.** Attached add/remove/reorder must enqueue layout/render/hit-test work, not just increment tree/root cache version. Primary files: `UI/Elements/UIElementCollection.cs`, `UI/Elements/UIRoot.cs`, `UI/Invalidation/DirtyPropagation.cs`.
3. [x] **Integrate style/theme into the retained scheduler.** Add real style phase/queue, root-owned style scope, explicit pseudo-class invalidation, and remove string property-name detection. Primary files: `UI/Styling/*`, `UI/Invalidation/UiFrameScheduler.cs`, `UI/Elements/UIElement.cs`.
4. [x] **Build retained input route/hit-test cache and clean input/control coupling.** Stop rebuilding route maps every frame; remove direct `ButtonBase`/`Thumb` dependencies from `ElementInputBridge`. Primary files: `UI/Input/ElementInputBridge.cs`, `UI/Input/ElementInputRouteBuilder.cs`, `UI/Input/HitTestService.cs`, `UI/Invalidation/HitTestQueue.cs`.
5. [ ] **Freeze Later/Optional and add remaining high-risk tests.** Stop expanding controls/media/markup/accessibility/animation until the remaining unchecked tests above prove resource propagation, layout diagnostics, virtualization scale, text wrapping, and architecture boundaries.
