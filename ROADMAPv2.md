# Cerneala ROADMAP v2

This file is the long-term project memory for Cerneala's modern retained UI architecture.

Product vision: **WPF 2026**, not WPF 2000. Cerneala should keep the WPF ideas that still help developer ergonomics while avoiding a compatibility-driven clone. The core should be retained-mode, game-loop-friendly, strongly typed, explicit, testable, backend-neutral where possible, and built on the existing `UI/Drawing` and `UI/Input` foundations.

The UI tree is retained. The game loop may call update and draw every frame, but Cerneala should not recompute layout or regenerate drawing commands unless invalidated state requires it.

Legend:

- `[x]` Exists now at the maturity claimed by the line text.
- `[ ]` Planned.
- `[~]` Exists partially, exists as a low-level primitive, is descriptor-only, or needs reshaping/integration for the v2 architecture.

Maturity markers used in Later and Optional/Experimental sections:

- **Type exists**: public/API shape or descriptor exists, but scenario behavior may be incomplete.
- **Wired into retained pipeline**: update/invalidation/render/input scheduling uses it through retained contracts.
- **Backend-supported**: drawing/input/platform adapters implement the behavior, not just metadata.
- **Scenario-complete**: realistic product scenario is implemented and covered by integration tests.
- **Frozen**: do not expand public surface until the named prerequisite is complete.

Scope bands:

- **MVP**: the minimum usable retained UI stack in the MonoGame playground.
- **Core**: architecture that should become stable before broad control growth.
- **Later**: important product areas that should wait for stable core layers.
- **Optional/Experimental**: useful ideas that should not shape the core until proven.

Later and Optional/Experimental work may have checked files or tests because prototypes exist. Those checks do not mean the feature is scenario-complete unless the line explicitly says so. Frozen sections can keep their existing code and tests, but should not drive core architecture or add public surface until their listed prerequisites are met.

Architectural invariants:

- Use `UI/Drawing` for low-level command recording and backend rendering.
- Use `UI/Input` for raw snapshots, routed event metadata, routing, and command primitives.
- Do not make `DrawCommandList` a scene graph.
- Do not make `DrawingContext` own layout, styling, input, or control state.
- Do not add duplicate `Point`, `Rect`, `Color`, text, image, or input primitives unless their role is clearly higher-level or semantically different from existing drawing/input types.
- Prefer explicit services and typed APIs over reflection-heavy global magic.
- Markup and serialization are optional layers, not the foundation.
- No Windows, HWND, or XAML assumptions in the core architecture.

## 0. [MVP/Core] Existing foundation to preserve

This section records what the repository already provides. Future work should build on these files rather than creating parallel low-level drawing or input stacks.

### Drawing command foundation

- [x] `architecture.md` documents the drawing/input boundaries.
- [x] `UI/Drawing/DrawArgument.cs`
- [x] `UI/Drawing/DrawColor.cs`
- [x] `UI/Drawing/DrawCommand.cs`
- [x] `UI/Drawing/DrawCommandKind.cs`
- [x] `UI/Drawing/DrawCommandList.cs`
- [x] `UI/Drawing/DrawingContext.cs`
- [x] `UI/Drawing/DrawPoint.cs`
- [x] `UI/Drawing/DrawRect.cs`
- [x] `UI/Drawing/DrawTextRun.cs`
- [x] `UI/Drawing/IDrawFont.cs`
- [x] `UI/Drawing/IDrawImage.cs`
- [x] `UI/Drawing/IDrawingBackend.cs`
- [x] `UI/Drawing/IFontSource.cs`
- [x] `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`
- [x] `UI/Drawing/MonoGame/MonoGameImage.cs`

Existing drawing tests:

- [x] `tests/Cerneala.Tests/Drawing/DrawCommandListTests.cs`
- [x] `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs`
- [x] `tests/Cerneala.Tests/Drawing/DrawingResourceTests.cs`

### Text shaping and rasterization foundation

- [x] `UI/Drawing/Text/RasterizedText.cs`
- [x] `UI/Drawing/Text/SkiaFont.cs`
- [x] `UI/Drawing/Text/SkiaTextRasterizer.cs`
- [x] `UI/Drawing/Text/SkiaTextShaper.cs`
- [x] `UI/Drawing/Text/SystemFontSource.cs`
- [x] `UI/Drawing/Text/TextShapeResult.cs`

Existing text tests:

- [x] `tests/Cerneala.Tests/Drawing/TextPipelineTests.cs`

### Input and routed event foundation

- [x] `UI/Input/CanExecuteRoutedEventArgs.cs`
- [x] `UI/Input/CommandBinding.cs`
- [x] `UI/Input/CommandEvents.cs`
- [x] `UI/Input/ExecutedRoutedEventArgs.cs`
- [x] `UI/Input/ICommand.cs`
- [x] `UI/Input/IInputSource.cs`
- [x] `UI/Input/InputButtonState.cs`
- [x] `UI/Input/InputFrame.cs`
- [x] `UI/Input/InputKey.cs`
- [x] `UI/Input/InputMouseButton.cs`
- [x] `UI/Input/KeyboardFocusChangedEventArgs.cs`
- [x] `UI/Input/KeyboardSnapshot.cs`
- [x] `UI/Input/KeyEventArgs.cs`
- [x] `UI/Input/MonoGame/MonoGameInputMapper.cs`
- [x] `UI/Input/MonoGame/MonoGameInputSource.cs`
- [x] `UI/Input/MouseButtonEventArgs.cs`
- [x] `UI/Input/MouseEventArgs.cs`
- [x] `UI/Input/MouseWheelEventArgs.cs`
- [x] `UI/Input/PointerSnapshot.cs`
- [x] `UI/Input/RoutedEvent.cs`
- [x] `UI/Input/RoutedEventArgs.cs`
- [x] `UI/Input/RoutedEventRegistry.cs`
- [x] `UI/Input/RoutedEventRouter.cs`
- [x] `UI/Input/RoutingStrategy.cs`
- [x] `UI/Input/TextCompositionEventArgs.cs`
- [x] `UI/Input/TextInputSnapshotEvent.cs`
- [x] `UI/Input/UiElementId.cs`
- [x] `UI/Input/UiInputElement.cs`
- [x] `UI/Input/UiInputTree.cs`
- [~] `UI/Input/InputEvents.cs` declares many WPF-familiar event names before all behavior exists.
- [~] `UI/Input/RoutedCommand.cs` exists as metadata and deliberately cannot execute until command routing exists.

Existing input tests:

- [x] `tests/Cerneala.Tests/Input/CommandingTests.cs`
- [x] `tests/Cerneala.Tests/Input/InputEventsTests.cs`
- [x] `tests/Cerneala.Tests/Input/InputFrameTests.cs`
- [x] `tests/Cerneala.Tests/Input/MonoGameInputMapperTests.cs`
- [x] `tests/Cerneala.Tests/Input/RoutedEventRouterTests.cs`
- [x] `tests/Cerneala.Tests/Input/RoutedEventTests.cs`

### Project and playground foundation

- [x] `Cerneala.csproj`
- [x] `Cerneala.slnx`
- [x] `GameBootstrap.cs`
- [x] `tests/Cerneala.Tests/Cerneala.Tests.csproj`
- [x] `tests/Cerneala.Tests/GameBootstrapTests.cs`
- [x] `Playground/Cerneala.Playground/Cerneala.Playground.csproj`
- [x] `Playground/Cerneala.Playground/Game1.cs`
- [x] `Playground/Cerneala.Playground/Program.cs`
- [~] `Playground/Cerneala.Playground/Game1.cs` manually clears and records drawing commands every frame; the v2 retained host should replace this sample pattern for UI scenarios.

## 1. [MVP] Architecture contracts and project memory

This phase makes the intended v2 architecture explicit before adding framework surface area. It keeps design decisions testable and prevents accidental WPF compatibility sprawl.

- [x] `docs/architecture-v2.md` — concise architecture complement to `architecture.md`, focused on layers above drawing/input.
- [x] `docs/diagrams/retained-frame-loop.md` — text diagram for update/layout/render-cache/draw flow.
- [x] `docs/diagrams/ui-layer-boundaries.md` — text diagram showing UI core -> drawing/input -> MonoGame adapters.

Tests and checks:

- [x] `tests/Cerneala.Tests/Architecture/RepositoryShapeTests.cs` — verifies section 1 planning files exist and the old planning-tool workspace stays removed.
- [x] `tests/Cerneala.Tests/Architecture/NamespaceBoundaryTests.cs` — verifies UI core namespaces do not reference Skia, HarfBuzz, MonoGame, `SpriteBatch`, `Texture2D`, or direct platform input polling outside adapter folders.

## 2. [MVP] Typed state model

This phase replaces the idea of cloning WPF `DependencyProperty` with a smaller typed property/state system. Properties should be strongly typed, explicitly registered, easy to test, and able to declare invalidation effects without reflection-heavy behavior.


Planning:


- [x] `UI/Core/UiObject.cs` — base object with typed property storage and lifecycle hooks.
- [x] `UI/Core/UiProperty.cs` — non-generic descriptor base for internal indexing and diagnostics.
- [x] `UI/Core/UiProperty{T}.cs` — strongly typed property descriptor.
- [x] `UI/Core/UiPropertyKey{T}.cs` — key for read-only or owner-only properties.
- [x] `UI/Core/UiPropertyMetadata{T}.cs` — default value, equality, validation, and invalidation metadata.
- [x] `UI/Core/UiPropertyOptions.cs` — `[Flags]` for `AffectsMeasure`, `AffectsArrange`, `AffectsRender`, `AffectsHitTest`, `AffectsStyle`, `Inherits`, and `ReadOnly`.
- [x] `UI/Core/UiPropertyValueSource.cs` — local, style, inherited, animated, default.
- [x] `UI/Core/UiPropertyStore.cs` — internal compact store keyed by `UiProperty` identity.
- [x] `UI/Core/UiPropertyChangedEventArgs.cs`
- [x] `UI/Core/UiPropertyChangedEventArgs{T}.cs`
- [x] `UI/Core/IUiPropertyOwner.cs`
- [x] `UI/Core/UiPropertyRegistry.cs` — explicit registration and duplicate detection.
- [x] `UI/Core/Unset.cs` — internal sentinel only; avoid public magic values unless proven necessary.
- [x] `UI/Core/CoerceValue.cs` — optional typed coercion delegate, explicit and non-reflective.
- [x] `UI/Core/ValidateValue.cs` — optional typed validation delegate.

Tests:

- [x] `tests/Cerneala.Tests/UI/Core/UiPropertyTests.cs`
- [x] `tests/Cerneala.Tests/UI/Core/UiPropertyRegistryTests.cs`
- [x] `tests/Cerneala.Tests/UI/Core/UiPropertyStoreTests.cs`
- [x] `tests/Cerneala.Tests/UI/Core/UiPropertyInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Core/ReadOnlyUiPropertyTests.cs`
- [x] `tests/Cerneala.Tests/UI/Core/InheritedUiPropertyTests.cs`
- [x] `dotnet test` passes.

Acceptance checklist:

- [x] Setting a typed property returns the previous typed value without casts in user code.
- [x] Setting an equal value does not enqueue layout or render work.
- [x] `AffectsMeasure` invalidates measure and render through the invalidation system.
- [x] `AffectsRender` invalidates render without forcing measure.
- [x] Non-invalidation options such as `Inherits` do not emit owner invalidation hooks.
- [x] Style values and local values have explicit precedence.
- [x] Property registration is deterministic and testable.

## 3. [MVP] Retained element tree

This phase creates the retained UI tree that owns state, layout, rendering hooks, event handlers, and child relationships. It follows the confirmed MVP decision to use separate logical and visual trees so semantic ownership, generated visuals, layout participation, rendering, hit testing, and input routing do not get collapsed into one ambiguous parent relationship.


Planning:


- [x] `UI/Elements/UIElement.cs` — retained element base with parent, children, enabled/visible state, handlers, and virtual lifecycle methods.
- [x] `UI/Elements/UIElementCollection.cs` — owned child collection with parent validation and change notifications.
- [x] `UI/Elements/UIRoot.cs` — root element with viewport size, scaling, input route ownership, and render cache root.
- [x] `UI/Elements/ElementLifecycle.cs` — attach/detach hooks and tree versioning.
- [x] `UI/Elements/ElementIdProvider.cs` — assigns stable `UiElementId` values for input routing.
- [x] `UI/Elements/ElementTreeWalker.cs` — pre-order, post-order, ancestor, and descendant traversal helpers.
- [x] `UI/Elements/ElementTreeChange.cs`
- [x] `UI/Elements/ElementTreeChangeKind.cs`
- [x] `UI/Elements/ElementChildRole.cs`
- [x] `UI/Elements/IElementChildHost.cs` — explicit contract for controls that own generated children.
- [x] `UI/Elements/IElementHost.cs` — implemented by `UIRoot` and future platform hosts.
- [x] `UI/Elements/ElementHandlerStore.cs` — stores routed event handlers on retained elements.
- [x] `UI/Input/UiInputTree.cs` — remains a low-level route table; retained elements are the route source.
- [x] `UI/Input/ElementInputRouteBuilder.cs` — builds or updates `UiInputTree` from the retained element tree.
- [x] `UI/Input/ElementInputRouteMap.cs` — maps `UIElement` <-> `UiElementId` for routed events.

Tests:

- [x] `tests/Cerneala.Tests/UI/Elements/UIElementTreeTests.cs`
- [x] `tests/Cerneala.Tests/UI/Elements/UIElementCollectionTests.cs`
- [x] `tests/Cerneala.Tests/UI/Elements/UIRootTests.cs`
- [x] `tests/Cerneala.Tests/UI/Elements/ElementLifecycleTests.cs`
- [x] `tests/Cerneala.Tests/UI/Elements/ElementTreeWalkerTests.cs`
- [x] `tests/Cerneala.Tests/UI/Elements/ElementHandlerStoreTests.cs`
- [x] `tests/Cerneala.Tests/Input/ElementInputRouteBuilderTests.cs`

Acceptance checklist:

- [x] Adding a child sets exactly one parent for the matching logical or visual relationship.
- [x] Removing a child clears parent for the matching logical or visual relationship.
- [x] Removing a child invalidates layout/render for affected ancestors after invalidation/layout/render systems exist.
- [x] Reparenting without removal is rejected.
- [x] Element ids are stable across frames while an element remains attached.
- [x] `UiInputTree` route order matches the retained visual element ancestor chain.
- [x] Disabled or invisible elements can be excluded from input routing according to explicit policy.

## 4. [MVP] Retained invalidation and frame scheduler

This phase is the core of the game-loop-friendly retained model. Update and draw can run every frame, but layout and drawing command generation should run only when dirty state requires it.


Planning:


Invalidation model:

```text
State change / resource change / input visual state change
        |
        v
InvalidationFlags on affected element
        |
        +--> LayoutQueue for measure/arrange work
        |
        +--> RenderQueue for local draw-command rebuilds
        |
        +--> dirty propagation to parents or descendants when required
        v
UiFrameScheduler.ProcessFrame()
        |
        +--> process layout queue until stable
        +--> process render queue into retained render caches
        +--> expose cached root DrawCommandList for backend rendering
```

- [x] `UI/Invalidation/InvalidationFlags.cs` — `[Flags]`: `None`, `Measure`, `Arrange`, `Render`, `Text`, `Image`, `Resource`, `Style`, `InputVisual`, `HitTest`, `Subtree`.
- [x] `UI/Invalidation/DirtyState.cs` — compact per-element dirty state and version stamps.
- [x] `UI/Invalidation/DirtyPropagation.cs` — rules for upward/downward propagation.
- [x] `UI/Invalidation/IInvalidationSink.cs`
- [x] `UI/Invalidation/InvalidationRequest.cs`
- [x] `UI/Invalidation/LayoutQueue.cs` — stable queue for measure and arrange invalidations.
- [x] `UI/Invalidation/RenderQueue.cs` — stable queue for render command regeneration.
- [x] `UI/Invalidation/HitTestQueue.cs` — rebuild hit-test data only when needed.
- [x] `UI/Invalidation/UiFrameScheduler.cs` — runs input effects, layout, render-cache updates, and diagnostics.
- [x] `UI/Invalidation/FramePhase.cs` — `Input`, `Style`, `Measure`, `Arrange`, `RenderCache`, `Idle`.
- [x] `UI/Invalidation/FrameStats.cs` — counts measured elements, arranged elements, rendered elements, reused caches.
- [x] `UI/Invalidation/FrameBudget.cs` — optional limits for later large trees; MVP may process all work.
- [x] `UI/Diagnostics/InvalidationTrace.cs`

Dirty propagation rules:

- [x] `Measure` invalidation marks the element measure-dirty and propagates measure/arrange need to layout ancestors until a layout boundary.
- [x] `Arrange` invalidation marks the element arrange-dirty and propagates render need for affected visual bounds.
- [x] `Render` invalidation marks local render cache dirty without forcing measure or arrange.
- [x] `Text` invalidation invalidates text measurement, local render cache, and layout only when text metrics may change.
- [x] `Image` invalidation invalidates image measurement when intrinsic size is used; otherwise render only.
- [x] `Resource` invalidation follows the metadata of the properties/resources that consumed the resource.
- [x] `Style` invalidation reapplies style and then raises property-specific invalidations.
- [x] `InputVisual` invalidation is render-only unless a control explicitly maps state to layout-affecting properties.
- [x] Clearing dirty flags happens only after successful phase processing.

Tests:

- [x] `tests/Cerneala.Tests/UI/Invalidation/InvalidationFlagsTests.cs`
- [x] `tests/Cerneala.Tests/UI/Invalidation/DirtyStateTests.cs`
- [x] `tests/Cerneala.Tests/UI/Invalidation/DirtyPropagationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Invalidation/LayoutQueueTests.cs`
- [x] `tests/Cerneala.Tests/UI/Invalidation/RenderQueueTests.cs`
- [x] `tests/Cerneala.Tests/UI/Invalidation/HitTestQueueTests.cs`
- [x] `tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs`
- [x] `tests/Cerneala.Tests/UI/Invalidation/FrameStatsTests.cs`
- [x] `tests/Cerneala.Tests/UI/Invalidation/RetainedNoWorkFrameTests.cs`
- [x] `tests/Cerneala.Tests/UI/Diagnostics/InvalidationTraceTests.cs`

Required retained-mode tests:

- [x] `RetainedNoWorkFrameTests.UnchangedTreeDoesNotMeasureOnSecondFrame`
- [x] `RetainedNoWorkFrameTests.UnchangedTreeDoesNotArrangeOnSecondFrame`
- [x] `RetainedNoWorkFrameTests.UnchangedTreeDoesNotRegenerateRenderCommandsOnSecondDraw`
- [x] `RetainedNoWorkFrameTests.DrawEveryFrameCanReuseCachedRootCommandList`
- [x] `RetainedNoWorkFrameTests.RenderOnlyInvalidationDoesNotRunMeasure`
- [x] `RetainedNoWorkFrameTests.MeasureInvalidationRegeneratesRenderCommandsOnlyAfterLayoutSettles`
- [x] `RetainedNoWorkFrameTests.HoverChangeInvalidatesRenderOnlyWhenVisualStateActuallyChanges`
- [x] `RetainedNoWorkFrameTests.TextColorChangeRebuildsRenderCommandsWithoutReshapingWhenMetricsAreUnchanged`

## 5. [MVP] Layout system

This phase adds WPF-inspired measure/arrange without copying WPF complexity. Layout types are intentionally named as layout types so they are not confused with `DrawPoint` and `DrawRect`, whose role is backend-neutral drawing command geometry.


Planning:


- [x] `UI/Layout/LayoutSize.cs` — layout measurement size; may support unconstrained dimensions where drawing primitives must not.
- [x] `UI/Layout/LayoutPoint.cs` — layout coordinate, not a drawing command point.
- [x] `UI/Layout/LayoutRect.cs` — layout slot, not a drawing command rectangle.
- [x] `UI/Layout/Thickness.cs` — margin, padding, border thickness.
- [x] `UI/Layout/Alignment.cs` — horizontal/vertical alignment values.
- [x] `UI/Layout/Visibility.cs` — `Visible`, `Hidden`, `Collapsed`.
- [x] `UI/Layout/LayoutRounding.cs` — explicit pixel snapping policy.
- [x] `UI/Layout/MeasureContext.cs`
- [x] `UI/Layout/ArrangeContext.cs`
- [x] `UI/Layout/LayoutResult.cs`
- [x] `UI/Layout/LayoutManager.cs` — consumes `LayoutQueue`, caches desired size and arranged bounds.
- [x] `UI/Layout/LayoutBoundary.cs` — marks roots or subtrees where propagation can stop.
- [x] `UI/Layout/ILayoutElement.cs`
- [x] `UI/Layout/Panels/Panel.cs`
- [x] `UI/Layout/Panels/Canvas.cs`
- [x] `UI/Layout/Panels/StackPanel.cs`
- [x] `UI/Layout/Panels/Orientation.cs`
- [x] `UI/Layout/Panels/Grid.cs` — fixed, auto, and star retained grid panel.
- [x] `UI/Layout/Panels/GridLength.cs`
- [x] `UI/Layout/Panels/ColumnDefinition.cs`
- [x] `UI/Layout/Panels/RowDefinition.cs`

Tests:

- [x] `tests/Cerneala.Tests/UI/Layout/LayoutPrimitiveTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/LayoutManagerTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/UIElementMeasureArrangeTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/LayoutInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/VisibilityTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/CanvasTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/StackPanelTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/GridTests.cs`

Acceptance checklist:

- [x] Measure results are cached by available size and element version.
- [x] Arrange results are cached by final rect and element version.
- [x] A no-op property set does not invalidate layout.
- [x] Parent layout invalidation does not force unchanged children to re-measure when constraints are unchanged.
- [x] `Collapsed` removes an element from layout and hit testing.
- [x] Layout output stays as layout state and does not generate drawing commands before rendering boundaries.

## 6. [MVP] Retained rendering and render cache

This phase connects retained elements to the existing `DrawingContext`, `DrawCommandList`, and `IDrawingBackend`. The retained renderer owns cache invalidation above the drawing layer; the drawing layer remains a command recorder/backend contract.


Planning:


Rendering model:

```text
UIElement.OnRender(RenderContext)
        |
        v
DrawingContext records local draw commands into ElementRenderCache
        |
        v
RetainedRenderer composes cached element command lists into a root DrawCommandList
        |
        v
IDrawingBackend.Render(rootCommandList) can run every frame
```

- [x] `UI/Rendering/RenderContext.cs` — exposes `DrawingContext`, layout bounds, inherited opacity/clip, and diagnostics.
- [x] `UI/Rendering/IRenderableElement.cs`
- [x] `UI/Rendering/ElementRenderCache.cs` — local cached `DrawCommandList`, local version, content bounds, and dependency versions.
- [x] `UI/Rendering/RetainedRenderCache.cs` — root/subtree command cache and cache versioning.
- [x] `UI/Rendering/RenderQueueProcessor.cs` — regenerates only dirty local element command lists.
- [x] `UI/Rendering/RetainedRenderer.cs` — produces cached root command list for `IDrawingBackend`.
- [x] `UI/Rendering/DrawCommandListBuilder.cs` — flattens cached local commands in visual order.
- [x] `UI/Rendering/DrawCommandListPool.cs` — explicit reusable `DrawCommandList` pool for safe temporary command-list reuse.
- [x] `UI/Rendering/ClipNode.cs` — retained clip metadata translated to `PushClip`/`PopClip` commands.
- [x] `UI/Rendering/RenderLayer.cs` — future boundary for opacity/effects; MVP can be minimal.
- [x] `UI/Rendering/RenderDependency.cs` — tracks text/image/theme/resource dependencies that affect cached commands.
- [x] `UI/Rendering/RenderCounters.cs` — counts cache hits/misses and command regeneration.

Tests:

- [x] `tests/Cerneala.Tests/UI/Rendering/ElementRenderCacheTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/RetainedRenderCacheTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/RenderQueueProcessorTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/RetainedRendererTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/DrawCommandListBuilderTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/DrawCommandListPoolTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/RenderDependencyTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/RenderCountersTests.cs`

Acceptance checklist:

- [x] `OnRender` is called only for elements with render dirty state or changed render dependencies.
- [x] The same unchanged root command list can be rendered by `IDrawingBackend` across multiple draw frames.
- [x] A child render change does not regenerate unrelated sibling local command lists.
- [x] Render order is deterministic and matches retained tree order.
- [x] Clip commands are balanced even when a subtree renders no visible commands.
- [x] Current primitive drawing uses existing `DrawRect`, `DrawPoint`, `DrawColor`, `DrawTextRun`, and `IDrawImage`.

## 7. [MVP] Game-loop host integration

This phase gives applications a simple retained UI entry point that fits MonoGame's `Update`/`Draw` rhythm while keeping Cerneala backend-neutral above adapters.

- [x] `UI/Hosting/UiHost.cs` — owns root, frame scheduler, input bridge, layout manager, renderer, and services.
- [x] `UI/Hosting/UiHostOptions.cs`
- [x] `UI/Hosting/UiFrame.cs` — frame time, viewport, input frame, diagnostics.
- [x] `UI/Hosting/UiViewport.cs`
- [x] `UI/Hosting/IUiClock.cs`
- [x] `UI/Hosting/IUiBackend.cs` — backend-neutral host bridge for drawing/input adapters.
- [x] `UI/Hosting/MonoGame/MonoGameUiHost.cs` — adapter around `MonoGameInputSource` and `MonoGameDrawingBackend`.
- [x] `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`
- [x] `UI/Hosting/MonoGame/MonoGameContentServices.cs` — image/font service glue; no control should use MonoGame types directly.
- [x] `Playground/Cerneala.Playground/Game1.cs` — update to create `MonoGameUiHost`, set a retained `UIRoot`, call `Update`, then call `Draw`.

Frame contract:

- [x] `UiHost.Update(frameTime)` reads or receives `InputFrame`, dispatches input, updates visual states, processes style/layout/render queues, and records frame stats.
- [x] `UiHost.Draw(IDrawingBackend backend)` renders the cached root `DrawCommandList` without forcing layout or render regeneration.
- [x] Viewport size changes invalidate root arrange and render.
- [x] First frame performs full measure, arrange, and render cache generation.
- [x] Later frames do no layout/render work unless invalidated.

Tests:

- [x] `tests/Cerneala.Tests/UI/Hosting/UiHostTests.cs`
- [x] `tests/Cerneala.Tests/UI/Hosting/UiHostFrameContractTests.cs`
- [x] `tests/Cerneala.Tests/UI/Hosting/UiViewportTests.cs`
- [x] `tests/Cerneala.Tests/UI/Hosting/MonoGameUiHostBoundaryTests.cs`
- [x] `tests/Cerneala.Tests/UI/Hosting/FakeUiClock.cs`
- [x] `tests/Cerneala.Tests/UI/Hosting/FakeDrawingBackend.cs`
- [x] `tests/Cerneala.Tests/UI/Hosting/FakeInputSource.cs`

## 8. [MVP] Input bridge, hit testing, focus, and visual state

This phase turns existing input snapshots and routed events into retained-control behavior. It should reuse `InputFrame`, `UiInputTree`, `RoutedEventRouter`, and WPF-familiar event names instead of replacing them.

- [x] `UI/Input/ElementInputBridge.cs` — converts `InputFrame` transitions into routed events against retained elements.
- [x] `UI/Input/HitTestService.cs` — hit tests retained layout/render bounds.
- [x] `UI/Input/HitTestResult.cs`
- [x] `UI/Input/HitTestFilter.cs`
- [x] `UI/Input/PointerCaptureManager.cs`
- [x] `UI/Input/HoverTracker.cs` — drives `IsPointerOver` and render invalidation.
- [x] `UI/Input/PressedStateTracker.cs` — drives button pressed state and click synthesis.
- [x] `UI/Input/ClickTracker.cs`
- [x] `UI/Input/FocusManager.cs` — explicit focus service, not a global static dependency.
- [x] `UI/Input/FocusScope.cs` — Core if MVP does not need nested scopes.
- [x] `UI/Input/KeyboardNavigation.cs` — Core if MVP only supports direct focus.
- [x] `UI/Input/TextInputBridge.cs` — maps `TextInputSnapshotEvent` to preview/bubble text events.
- [x] `UI/Input/ElementRoutedEventStore.cs` — handler storage attached to `UIElement`.
- [x] `UI/Input/RoutedEventRouter.cs` — keep routing core; add retained-tree bridge rather than duplicating route logic.
- [~] `UI/Input/InputEvents.cs` — use existing mouse/key/text events first; stylus/touch/drag metadata remains later behavior.

Visual state properties:

- [x] `UI/Elements/UIElement.IsEnabledProperty`
- [x] `UI/Elements/UIElement.IsVisibleProperty`
- [x] `UI/Elements/UIElement.IsPointerOverProperty`
- [x] `UI/Elements/UIElement.IsKeyboardFocusWithinProperty`
- [x] `UI/Elements/UIElement.IsKeyboardFocusedProperty`
- [x] `UI/Controls/Primitives/ButtonBase.IsPressedProperty`

Tests:

- [x] `tests/Cerneala.Tests/Input/ElementInputBridgeTests.cs`
- [x] `tests/Cerneala.Tests/Input/HitTestServiceTests.cs`
- [x] `tests/Cerneala.Tests/Input/PointerCaptureManagerTests.cs`
- [x] `tests/Cerneala.Tests/Input/HoverTrackerTests.cs`
- [x] `tests/Cerneala.Tests/Input/PressedStateTrackerTests.cs`
- [x] `tests/Cerneala.Tests/Input/ClickTrackerTests.cs`
- [x] `tests/Cerneala.Tests/Input/FocusManagerTests.cs`
- [x] `tests/Cerneala.Tests/Input/TextInputBridgeTests.cs`
- [x] `tests/Cerneala.Tests/Input/RetainedRoutedEventIntegrationTests.cs`

Acceptance checklist:

- [x] Mouse down raises preview then bubble events on the hit-tested retained element.
- [x] Mouse move updates hover state and invalidates render only when hover target changes.
- [x] Disabled elements do not receive input handlers.
- [x] Keyboard events target focused element.
- [x] Focus change raises existing focus routed events.
- [x] Text input uses `TextInputSnapshotEvent` and existing text routed event args.
- [x] Input routing parent chain matches retained tree parent chain.

## 9. [MVP] Commands and actions

This phase completes route-based command execution without copying WPF's global `CommandManager` magic. Commands should be explicit, testable, and based on existing command primitives.


- [x] `UI/Input/ICommand.cs`
- [x] `UI/Input/RoutedCommand.cs`
- [x] `UI/Input/CommandBinding.cs`
- [x] `UI/Input/CommandEvents.cs`
- [x] `UI/Input/CommandBindingCollection.cs`
- [x] `UI/Input/CommandRouter.cs` — explicit service that queries and executes through retained routes.
- [x] `UI/Input/RoutedCommandContext.cs`
- [x] `UI/Input/ActionCommand.cs` — simple command backed by delegates.
- [x] `UI/Input/InputGesture.cs` — explicit retained input gesture base for shortcut matching.
- [x] `UI/Input/KeyGesture.cs` — keyboard shortcut gesture with exact modifier matching.
- [x] `UI/Input/InputBinding.cs` — explicit gesture-to-command binding without global command manager magic.
- [x] `UI/Input/KeyBinding.cs` — keyboard shortcut binding for direct and routed commands.
- [x] `UI/Controls/Primitives/ButtonBase.CommandProperty`
- [x] `UI/Controls/Primitives/ButtonBase.CommandParameterProperty`

Tests:

- [x] `tests/Cerneala.Tests/Input/CommandRouterTests.cs`
- [x] `tests/Cerneala.Tests/Input/CommandBindingCollectionTests.cs`
- [x] `tests/Cerneala.Tests/Input/ActionCommandTests.cs`
- [x] `tests/Cerneala.Tests/Input/RoutedCommandExecutionTests.cs`
- [x] `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseCommandTests.cs`
- [x] `tests/Cerneala.Tests/Input/InputGestureTests.cs`

Acceptance checklist:

- [x] Routed command `CanExecute` uses the current command route only through explicit `CommandRouter` APIs.
- [x] Routed command execution works through explicit `CommandRouter.Execute`; direct `RoutedCommand.Execute` fails with a router-required error.
- [x] `ButtonBase` queries command state through explicit refresh/click paths, not through hidden global requery magic.
- [x] Command state changes can invalidate visual state for controls that display enabled/disabled state.

## 10. [MVP] First controls and panels

This phase creates the smallest useful control set. Controls should be retained, layout-aware, input-aware, and render through `DrawingContext` commands. Keep names familiar where they are ergonomic.


- [x] `UI/Controls/Control.cs` — base control with styling hooks and common visual properties.
- [x] `UI/Controls/ContentControl.cs`
- [x] `UI/Controls/Decorator.cs`
- [x] `UI/Controls/Border.cs`
- [x] `UI/Controls/Panel.cs` — public alias or wrapper over `UI/Layout/Panels/Panel` if the final namespace should be controls-oriented.
- [x] `UI/Controls/Canvas.cs`
- [x] `UI/Controls/StackPanel.cs`
- [x] `UI/Controls/TextBlock.cs`
- [x] `UI/Controls/Image.cs`
- [x] `UI/Controls/Primitives/ButtonBase.cs`
- [x] `UI/Controls/Button.cs`
- [x] `UI/Controls/Primitives/ToggleButton.cs` — checkable button primitive with explicit click toggle semantics.
- [x] `UI/Controls/CheckBox.cs` — checkable content control rendered through retained drawing commands.
- [x] `UI/Controls/ControlTemplate.cs` — code-first control templates.
- [x] `UI/Controls/TemplatePartAttribute.cs` — diagnostic template part metadata.
- [x] `UI/Controls/VisualState.cs` — minimal state names for hover/pressed/focus/disabled.
- [x] `UI/Controls/ControlTextFont.cs` — minimal backend-neutral font handle for MVP text commands.
- [x] `UI/Controls/TextMeasurement.cs` — minimal text measurement result for `TextBlock`.
- [x] `UI/Controls/TextMeasurer.cs` — deterministic MVP text measurer; full text services remain section 11.

Common control properties:

- [x] `Control.BackgroundProperty` — MVP can use `DrawColor`; richer brushes wait for Core rendering features.
- [x] `Control.ForegroundProperty` — MVP can use `DrawColor`.
- [x] `Control.BorderColorProperty` — MVP can use `DrawColor`.
- [x] `Control.BorderThicknessProperty` — `Thickness`.
- [x] `Control.PaddingProperty` — `Thickness`.
- [x] `Control.FontFamilyProperty` — string or typed font reference backed by `IFontSource`.
- [x] `Control.FontSizeProperty` — validates through drawing/text size constraints.

Tests:

- [x] `tests/Cerneala.Tests/Controls/ControlTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ContentControlTests.cs`
- [x] `tests/Cerneala.Tests/Controls/DecoratorTests.cs`
- [x] `tests/Cerneala.Tests/Controls/BorderTests.cs`
- [x] `tests/Cerneala.Tests/Controls/PanelTests.cs`
- [x] `tests/Cerneala.Tests/Controls/CanvasTests.cs`
- [x] `tests/Cerneala.Tests/Controls/StackPanelTests.cs`
- [x] `tests/Cerneala.Tests/Controls/TextBlockTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ImageTests.cs`
- [x] `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ButtonTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ToggleButtonTests.cs`
- [x] `tests/Cerneala.Tests/Controls/CheckBoxTests.cs`

MVP control acceptance checklist:

- [x] A retained `Button` can be added to `UIRoot`, measured, arranged, rendered, hit-tested, hovered, pressed, clicked, and command-bound.
- [x] A retained `TextBlock` measures text using the existing Skia/HarfBuzz pipeline through higher-level text services.
- [x] A retained `Border` renders fill/stroke with existing rectangle commands.
- [x] A retained `StackPanel` lays out children and avoids re-measuring unchanged children.
- [x] Control visual states invalidate render only when the state affects visible output.

## 11. [MVP] Text services above the existing drawing text pipeline

This phase adds layout and cache services for controls such as `TextBlock` without rebuilding shaping/rasterization. The existing Skia/HarfBuzz code remains the low-level text engine.

- [~] `UI/Drawing/DrawTextRun.cs`
- [~] `UI/Drawing/Text/SkiaTextShaper.cs`
- [~] `UI/Drawing/Text/SkiaTextRasterizer.cs`
- [x] `UI/Text/FontResolver.cs` — wraps `IFontSource` and theme/default font decisions.
- [x] `UI/Text/TextRunStyle.cs` — font family, size, color, wrapping flags; converts to `DrawTextRun`.
- [x] `UI/Text/TextMeasureResult.cs`
- [x] `UI/Text/TextMeasurer.cs` — computes desired size and caches metrics.
- [x] `UI/Text/TextLayoutCache.cs` — keyed by text, font, size, wrapping width, and DPI/scale.
- [x] `UI/Text/TextRenderer.cs` — records text commands with `DrawingContext.DrawText`.
- [x] `UI/Text/TextWrapping.cs`
- [x] `UI/Text/TextTrimming.cs` — Core if MVP does not need trimming.
- [x] `UI/Text/LineBreakService.cs` — Core if MVP only supports single-line text.
- [x] `UI/Text/BidiTextService.cs` — minimal neutral/LTR/RTL run detection for higher-level text services.
- [x] `UI/Text/TextSelection.cs` — normalized immutable selection/caret range.
- [x] `UI/Text/TextEditingController.cs` — explicit editing controller over `TextEditor` for text input and navigation keys.

Tests:

- [x] `tests/Cerneala.Tests/UI/Text/FontResolverTests.cs`
- [x] `tests/Cerneala.Tests/UI/Text/TextMeasurerTests.cs`
- [x] `tests/Cerneala.Tests/UI/Text/TextLayoutCacheTests.cs`
- [x] `tests/Cerneala.Tests/UI/Text/TextRendererTests.cs`
- [x] `tests/Cerneala.Tests/UI/Text/BidiTextServiceTests.cs`
- [x] `tests/Cerneala.Tests/UI/Text/TextEditingControllerTests.cs`
- [x] `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs`

Acceptance checklist:

- [x] Text content changes invalidate text metrics and render commands.
- [x] Text color changes invalidate render commands without forcing text shaping when glyph metrics are unchanged.
- [x] Font family or font size changes invalidate measurement and render.
- [x] Re-rendering unchanged text reuses cached text layout and retained render commands.

## 12. [MVP] Resources for fonts and images

This phase introduces explicit resource identity and invalidation without recreating WPF resource dictionaries as core machinery. Resources should be typed and observable enough to invalidate dependent layout/render caches.

- [x] `UI/Resources/ResourceId{T}.cs` — typed `ResourceId<T>` identity.
- [x] `UI/Resources/IResourceProvider.cs`
- [x] `UI/Resources/ResourceStore.cs`
- [x] `UI/Resources/ResourceChangedEventArgs.cs`
- [x] `UI/Resources/ResourceDependencyTracker.cs`
- [x] `UI/Resources/FontResource.cs`
- [x] `UI/Resources/ImageResource.cs`
- [x] `UI/Resources/IImageLoader.cs`
- [x] `UI/Resources/MonoGame/MonoGameImageLoader.cs` — adapter that returns `IDrawImage`/`MonoGameImage` without leaking `Texture2D` into controls.
- [~] `UI/Drawing/IDrawImage.cs` — keep as draw-level image handle.
- [~] `UI/Drawing/IDrawFont.cs` — keep as draw-level font handle.

Tests:

- [x] `tests/Cerneala.Tests/UI/Resources/ResourceIdTests.cs`
- [x] `tests/Cerneala.Tests/UI/Resources/ResourceStoreTests.cs`
- [x] `tests/Cerneala.Tests/UI/Resources/ResourceDependencyTrackerTests.cs`
- [x] `tests/Cerneala.Tests/UI/Resources/ImageResourceInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Resources/FontResourceInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/ResourceRenderDependencyTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs` — covers resource and control backend boundaries.

Acceptance checklist:

- [x] Replacing an image resource invalidates render for controls using fixed size.
- [x] Replacing an image resource invalidates layout if the control uses intrinsic image size.
- [x] Replacing a font resource invalidates text measurement and render for dependent text controls.
- [x] Resource lookup is explicit through host/services, not hidden global lookup.
- [x] Retained render caches include resource dependency identity/version in staleness checks.
- [x] Core resources and controls do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`.
- [x] MonoGame image loading is adapter-scoped under `UI/Resources/MonoGame`.

## 13. [MVP] Playground scenario

This phase proves the retained architecture in a real MonoGame loop. The sample should draw every frame but do no layout/render command regeneration on no-op frames.

- [x] `Playground/Cerneala.Playground/Samples/RetainedButtonSample.cs`
- [x] `Playground/Cerneala.Playground/Samples/LayoutSample.cs`
- [x] `Playground/Cerneala.Playground/Samples/TextSample.cs`
- [x] `Playground/Cerneala.Playground/Samples/InvalidationStatsOverlay.cs`
- [x] `Playground/Cerneala.Playground/Samples/SampleSelector.cs`
- [x] `Playground/Cerneala.Playground/Game1.cs` — wire sample selector through `MonoGameUiHost`.

MVP demo acceptance checklist:

- [x] Window shows retained `StackPanel` containing `TextBlock`, `Button`, and `Border`.
- [x] Mouse hover changes button visuals through retained input state.
- [x] Button click executes an explicit command/action.
- [x] On unchanged frames, diagnostics show zero measured elements, zero arranged elements, and zero regenerated local render caches.
- [x] Draw still happens every frame through `MonoGameDrawingBackend.Render(cachedCommands)`.

## 14. [Core] Styling and themes

This phase adds modern styling after properties, retained tree, and invalidation are stable. Styling should be typed and explicit, not a XAML-first clone of WPF triggers/resources.

- [x] `UI/Styling/Style.cs`
- [x] `UI/Styling/StyleRule.cs`
- [x] `UI/Styling/StyleSelector.cs`
- [x] `UI/Styling/StyleSheet.cs`
- [x] `UI/Styling/Setter.cs`
- [x] `UI/Styling/Setter{T}.cs`
- [x] `UI/Styling/StyleApplicator.cs`
- [x] `UI/Styling/StyleInvalidation.cs`
- [x] `UI/Styling/Theme.cs`
- [x] `UI/Styling/ThemeKey{T}.cs`
- [x] `UI/Styling/ThemeProvider.cs`
- [x] `UI/Styling/ThemeResource.cs`
- [x] `UI/Styling/PseudoClass.cs` — hover, pressed, focus, disabled, selected.
- [x] `UI/Styling/VisualStateRule.cs`
- [x] `UI/Styling/DefaultTheme.cs`
- [x] `UI/Styling/ThemePalette.cs`
- [x] `UI/Styling/StyleDiagnostics.cs`

Tests:

- [x] `tests/Cerneala.Tests/UI/Styling/StyleTests.cs`
- [x] `tests/Cerneala.Tests/UI/Styling/StyleRuleTests.cs`
- [x] `tests/Cerneala.Tests/UI/Styling/SetterTests.cs`
- [x] `tests/Cerneala.Tests/UI/Styling/StyleApplicatorTests.cs`
- [x] `tests/Cerneala.Tests/UI/Styling/StyleInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Styling/ThemeTests.cs`
- [x] `tests/Cerneala.Tests/UI/Styling/PseudoClassTests.cs`

Acceptance checklist:

- [x] Applying a style sets typed properties without reflection in the hot path.
- [x] Style changes invalidate only affected layout/render/style work.
- [x] Visual state changes can activate style rules and enqueue render invalidation.
- [x] Theme changes propagate through resource dependencies.
- [x] Local values override style values through explicit precedence.

## 15. [Core] Templates and composition

This phase enables reusable controls without forcing every control to hand-code rendering. Templates should be code-first and strongly typed first; optional markup can come later.

- [x] `UI/Controls/ControlTemplate.cs`
- [x] `UI/Controls/ControlTemplate{TControl}.cs`
- [x] `UI/Controls/TemplateContext.cs`
- [x] `UI/Controls/TemplateInstance.cs`
- [x] `UI/Controls/TemplateBinding{T}.cs`
- [x] `UI/Controls/TemplatePartAttribute.cs` — diagnostic only; no hidden runtime magic required.
- [x] `UI/Controls/ItemsPanelTemplate.cs`
- [x] `UI/Controls/DataTemplate.cs`
- [x] `UI/Controls/DataTemplate{T}.cs`
- [x] `UI/Controls/ContentPresenter.cs`
- [x] `UI/Controls/ItemsPresenter.cs`

Tests:

- [x] `tests/Cerneala.Tests/Controls/ControlTemplateTests.cs`
- [x] `tests/Cerneala.Tests/Controls/TemplateBindingTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ContentPresenterTests.cs`
- [x] `tests/Cerneala.Tests/Controls/DataTemplateTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ItemsPanelTemplateTests.cs`

Acceptance checklist:

- [x] Template-generated children are retained across frames.
- [x] Changing a template invalidates the subtree once, not every frame.
- [x] Template bindings are strongly typed and do not use string property paths in the hot path.
- [x] Template children participate in layout, rendering, hit testing, and input routing.

## 16. [Core] Additional controls and scrolling

This phase expands the useful control set after MVP controls, styling, and templates are stable.

- [x] `UI/Controls/Primitives/RangeBase.cs`
- [x] `UI/Controls/Primitives/Thumb.cs`
- [x] `UI/Controls/Primitives/Track.cs`
- [x] `UI/Controls/Primitives/ScrollBar.cs`
- [x] `UI/Controls/ScrollViewer.cs`
- [x] `UI/Controls/ScrollContentPresenter.cs`
- [x] `UI/Controls/ScrollBarVisibility.cs`
- [x] `UI/Controls/IScrollInfo.cs`
- [x] `UI/Controls/Slider.cs`
- [x] `UI/Controls/ProgressBar.cs`
- [x] `UI/Controls/RadioButton.cs`
- [x] `UI/Controls/Label.cs`
- [x] `UI/Controls/ToolTip.cs`
- [x] `UI/Controls/PopupRoot.cs`

Completion notes:

- [x] `RangeBase` clamps values and coerces value state when range endpoints change.
- [x] `Thumb` uses retained pointer capture through `ElementInputBridge` and reports drag start/delta/completion.
- [x] `Track`, `ScrollBar`, and `Slider` map retained drag movement to range values for horizontal and vertical orientations.
- [x] `ScrollContentPresenter` computes extent/viewport state, clamps offsets, clips content, and arranges content with retained scroll offsets.
- [x] `ScrollViewer` handles retained mouse wheel scrolling and disabled, hidden, visible, and auto scrollbar visibility policies.
- [x] `ProgressBar`, `RadioButton`, `Label`, `ToolTip`, and `PopupRoot` are retained controls and stay backend-neutral.

Tests:

- [x] `tests/Cerneala.Tests/Controls/Primitives/RangeBaseTests.cs`
- [x] `tests/Cerneala.Tests/Controls/Primitives/ThumbTests.cs`
- [x] `tests/Cerneala.Tests/Controls/Primitives/TrackTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ScrollBarTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ScrollViewerTests.cs`
- [x] `tests/Cerneala.Tests/Controls/SliderTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ProgressBarTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ToolTipTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs` - covers section 16 backend boundaries.

## 17. [Core] Items, selection, and virtualization

This phase should come after templates and scrolling. Lists must be retained and virtualized so large data sets do not create or re-render every element every frame.

- [x] `UI/Controls/ItemsControl.cs`
- [x] `UI/Controls/ItemCollection.cs`
- [x] `UI/Controls/ItemContainerGenerator.cs`
- [x] `UI/Controls/ItemContainerRecyclePool.cs`
- [x] `UI/Controls/ItemsPresenter.cs`
- [x] `UI/Controls/SelectionModel.cs`
- [x] `UI/Controls/SelectionModel{T}.cs`
- [x] `UI/Controls/Primitives/Selector.cs`
- [x] `UI/Controls/ListBox.cs`
- [x] `UI/Controls/ListBoxItem.cs`
- [x] `UI/Controls/ComboBox.cs`
- [x] `UI/Controls/TabControl.cs`
- [x] `UI/Controls/TabItem.cs`
- [x] `UI/Layout/Panels/VirtualizingStackPanel.cs`
- [x] `UI/Layout/Virtualization/VirtualizationContext.cs`
- [x] `UI/Layout/Virtualization/RealizationWindow.cs`

Tests:

- [x] `tests/Cerneala.Tests/Controls/ItemsControlTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ItemContainerGeneratorTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ItemContainerRecyclePoolTests.cs`
- [x] `tests/Cerneala.Tests/Controls/SelectionModelTests.cs`
- [x] `tests/Cerneala.Tests/Controls/Primitives/SelectorTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ListBoxTests.cs`
- [x] `tests/Cerneala.Tests/Controls/ComboBoxTests.cs`
- [x] `tests/Cerneala.Tests/Controls/TabControlTests.cs`
- [x] `tests/Cerneala.Tests/Controls/TabItemTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/VirtualizingStackPanelTests.cs`
- [x] `tests/Cerneala.Tests/UI/Layout/VirtualizationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs` - covers section 17 backend boundaries.

Acceptance checklist:

- [x] Items controls retain generated containers only for realized items.
- [x] Scrolling changes realization window and invalidates layout/render for affected items only.
- [x] Selection changes invalidate visual state for selected containers only.
- [x] Data updates do not rebuild unrelated realized containers.

## 18. [Core] Data observation and binding-light APIs

This phase provides modern data flow without making classic reflection-heavy binding the core. Start with explicit typed observation and templates; add string-path binding later only if needed.

- [x] `UI/Data/ObservableValue{T}.cs`
- [x] `UI/Data/ObservableList{T}.cs`
- [x] `UI/Data/IObservableList{T}.cs`
- [x] `UI/Data/PropertyAdapter{TOwner,TValue}.cs`
- [x] `UI/Data/Binding.cs` — optional typed binding facade.
- [x] `UI/Data/Binding{T}.cs`
- [x] `UI/Data/BindingMode.cs`
- [x] `UI/Data/IValueConverter{TIn,TOut}.cs`
- [x] `UI/Data/CollectionView{T}.cs`
- [x] `UI/Data/SortDescription{T}.cs`
- [x] `UI/Data/FilterPredicate{T}.cs`
- [x] `UI/Data/StringPropertyPath.cs` — Later, not hot-path core.

Tests:

- [x] `tests/Cerneala.Tests/UI/Data/ObservableValueTests.cs`
- [x] `tests/Cerneala.Tests/UI/Data/ObservableListTests.cs`
- [x] `tests/Cerneala.Tests/UI/Data/TypedBindingTests.cs`
- [x] `tests/Cerneala.Tests/UI/Data/CollectionViewTests.cs`
- [x] `tests/Cerneala.Tests/UI/Data/StringPropertyPathTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs` - covers section 18 backend boundaries.

Acceptance checklist:

- [x] Observable scalar values publish typed old/new value changes.
- [x] Observable lists publish ordered add/remove/replace/move/clear/reset changes.
- [x] Typed binding connects observable sources to explicit target setters and can be disposed.
- [x] Collection views support typed filtering and sorting.
- [x] String property paths remain deferred and unsupported in core hot paths.

## 19. [Core] Diagnostics and developer tools

This phase makes retained UI behavior inspectable. Diagnostics are required because invalidation-driven systems are hard to reason about without counters and tree dumps.

- [x] `UI/Diagnostics/FrameDiagnostics.cs`
- [x] `UI/Diagnostics/LayoutDiagnostics.cs`
- [x] `UI/Diagnostics/RenderDiagnostics.cs`
- [x] `UI/Diagnostics/InputDiagnostics.cs`
- [x] `UI/Diagnostics/DirtyTreeDumper.cs`
- [x] `UI/Diagnostics/ElementTreeDumper.cs`
- [x] `UI/Diagnostics/RenderCacheDumper.cs`
- [x] `UI/Diagnostics/RoutedEventTrace.cs`
- [x] `UI/Diagnostics/StyleTrace.cs`
- [x] `UI/Diagnostics/DebugOverlay.cs`
- [x] `UI/Diagnostics/DebugAdorner.cs`
- [x] `Playground/Cerneala.Playground/Samples/DiagnosticsSample.cs`

Tests:

- [x] `tests/Cerneala.Tests/UI/Diagnostics/FrameDiagnosticsTests.cs`
- [x] `tests/Cerneala.Tests/UI/Diagnostics/DirtyTreeDumperTests.cs`
- [x] `tests/Cerneala.Tests/UI/Diagnostics/ElementTreeDumperTests.cs`
- [x] `tests/Cerneala.Tests/UI/Diagnostics/RenderCacheDumperTests.cs`
- [x] `tests/Cerneala.Tests/UI/Diagnostics/RoutedEventTraceTests.cs`
- [x] `tests/Cerneala.Tests/UI/Diagnostics/StyleTraceTests.cs`

Acceptance checklist:

- [x] Developers can see per-frame measure/arrange/render-cache counts.
- [x] Developers can dump which elements are dirty and why.
- [x] Developers can trace routed event paths.
- [x] Developers can inspect style sources for a property value.

## 20. [Later] Text editing and IME

This phase should wait until input, focus, text layout, and diagnostics are stable. Text editing should build on `TextInputSnapshotEvent` but add composition lifecycle where platform adapters support it.

- [x] `UI/Controls/TextBoxBase.cs`
- [x] `UI/Controls/TextBox.cs`
- [x] `UI/Controls/PasswordBox.cs`
- [x] `UI/Text/TextDocument.cs`
- [x] `UI/Text/TextCaret.cs`
- [x] `UI/Text/TextSelection.cs`
- [x] `UI/Text/TextEditor.cs`
- [x] `UI/Text/TextCompositionManager.cs`
- [x] `UI/Text/TextCompositionState.cs`
- [x] `UI/Text/UndoRedoStack.cs`
- [x] `UI/Text/ClipboardAdapter.cs`
- [x] `UI/Platform/ITextInputPlatform.cs`

Tests:

- [x] `tests/Cerneala.Tests/Controls/TextBoxTests.cs`
- [x] `tests/Cerneala.Tests/Controls/PasswordBoxTests.cs`
- [x] `tests/Cerneala.Tests/UI/Text/TextEditorTests.cs`
- [x] `tests/Cerneala.Tests/UI/Text/TextCompositionManagerTests.cs`
- [x] `tests/Cerneala.Tests/UI/Text/UndoRedoStackTests.cs`

## 21. [Later] Accessibility and semantics

This phase keeps the platform-neutral semantics tree as the retained-core architecture. Platform accessibility adapters are **later/frozen** until the semantics API is stable and at least one backend adapter exposes real platform behavior.

- [x] `UI/Accessibility/SemanticsNode.cs`
- [x] `UI/Accessibility/SemanticsRole.cs`
- [x] `UI/Accessibility/SemanticsProperty.cs`
- [x] `UI/Accessibility/SemanticsTree.cs`
- [x] `UI/Accessibility/SemanticsProvider.cs`
- [x] `UI/Accessibility/AccessibleName.cs`
- [~] `UI/Accessibility/AutomationPeer.cs` — type exists; naming remains under review and platform adapter behavior is frozen.
- [~] `UI/Accessibility/ButtonAutomationPeer.cs` — type exists; keep behind semantic tree behavior until platform adapters exist.
- [~] `UI/Accessibility/TextBoxAutomationPeer.cs` — type exists; keep behind semantic tree behavior until platform adapters exist.
- [~] `UI/Accessibility/ItemsControlAutomationPeer.cs` — type exists; keep behind semantic tree behavior until platform adapters exist.
- [~] `UI/Platform/IAccessibilityPlatform.cs` — contract exists; real adapter behavior remains later.

Tests:

- [x] `tests/Cerneala.Tests/UI/Accessibility/SemanticsTreeTests.cs`
- [x] `tests/Cerneala.Tests/UI/Accessibility/SemanticsProviderTests.cs`
- [x] `tests/Cerneala.Tests/UI/Accessibility/ButtonSemanticsTests.cs`
- [x] `tests/Cerneala.Tests/UI/Accessibility/TextBoxSemanticsTests.cs`

Acceptance checklist:

- [x] Platform-neutral semantic nodes can be produced for retained controls.
- [~] WPF-like peer names are tolerated only as implementation vocabulary while public API decision remains open.
- [ ] Native platform accessibility adapters exist and are tested before scenario-complete.

## 22. [Later] Advanced rendering and media

This phase is experimental/frozen until drawing command and backend semantics exist for each advanced primitive. Existing files may remain as descriptors, typed state, or tests, but they are not scenario-complete unless the roadmap line explicitly says `backend-supported` or `scenario-complete`.

New media concepts must translate into `DrawCommand` extensions or clear backend abstractions before they can be marked implemented. Descriptor existence alone is not enough.

- [x] `UI/Drawing/DrawCommandKind.cs` — add new command kinds only with tests and backend support.
- [x] `UI/Drawing/DrawingContext.cs` — add methods only when corresponding command kinds exist.
- [x] `UI/Media/Brush.cs` — introduce when more than solid `DrawColor` is needed.
- [x] `UI/Media/SolidColorBrush.cs` — may remain a thin wrapper over `DrawColor` only if it participates in styling/resource identity.
- [~] `UI/Media/LinearGradientBrush.cs` — type exists; frozen until gradient draw commands and backend rendering exist.
- [~] `UI/Media/RadialGradientBrush.cs` — type exists; frozen until gradient draw commands and backend rendering exist.
- [x] `UI/Media/Pen.cs`
- [x] `UI/Media/Geometry.cs`
- [x] `UI/Media/RectangleGeometry.cs`
- [x] `UI/Media/EllipseGeometry.cs`
- [~] `UI/Media/PathGeometry.cs` — type exists; frozen until real path fill/stroke command semantics exist.
- [x] `UI/Media/Transform.cs`
- [x] `UI/Media/Matrix3x2.cs`
- [~] `UI/Media/OpacityLayer.cs` — type exists; frozen until layer composition has retained render-cache and backend semantics.
- [~] `UI/Media/ShadowEffect.cs` — type exists; frozen until shadow/effect command semantics and backend behavior exist.
- [x] `UI/Controls/Shapes/Shape.cs`
- [x] `UI/Controls/Shapes/Rectangle.cs`
- [x] `UI/Controls/Shapes/Ellipse.cs`
- [x] `UI/Controls/Shapes/Path.cs`
- [x] `UI/Media/ImageSource.cs` — owns media identity, intrinsic size metadata, and draw-image resolution beyond `IDrawImage`.
- [x] `UI/Media/BitmapImage.cs`
- [~] `UI/Media/RenderTargetImage.cs` — type exists; frozen until render-target lifecycle and backend ownership are designed.

Tests:

- [x] `tests/Cerneala.Tests/Drawing/AdvancedDrawCommandTests.cs`
- [x] `tests/Cerneala.Tests/UI/Media/BrushTests.cs`
- [x] `tests/Cerneala.Tests/UI/Media/GeometryTests.cs`
- [x] `tests/Cerneala.Tests/UI/Media/TransformTests.cs`
- [x] `tests/Cerneala.Tests/UI/Controls/Shapes/ShapeTests.cs`
- [x] `tests/Cerneala.Tests/UI/Media/ImageSourceTests.cs`

Acceptance checklist:

- [~] Every new media abstraction has a responsibility not already covered by `DrawColor`, `DrawRect`, `DrawPoint`, `DrawTextRun`, or `IDrawImage`; descriptor-only abstractions remain experimental.
- [ ] Every advanced drawing command has backend tests or adapter coverage before being marked implemented.
- [x] Controls do not reference Skia, HarfBuzz, MonoGame, `SpriteBatch`, or `Texture2D`.
- [ ] Full project tests pass for the backend-supported advanced media scenario before this phase is scenario-complete.

## 23. [Later] Animation and transitions

This phase keeps the existing animation primitives small and **frozen for expansion** until scheduler/render invalidation is proven under animation stress. Animation should be game-loop-native, explicit, and invalidate only affected properties; timeline/storyboard growth waits for stress tests that prove no hidden layout/render churn.

- [x] `UI/Animation/AnimationClock.cs`
- [x] `UI/Animation/AnimationScheduler.cs`
- [x] `UI/Animation/Animation.cs`
- [x] `UI/Animation/Animation{T}.cs`
- [x] `UI/Animation/Easing.cs`
- [x] `UI/Animation/Transition.cs`
- [x] `UI/Animation/Transition{T}.cs`
- [~] `UI/Animation/Storyboard.cs` — type exists; composition expansion is frozen until animation stress invalidation tests exist.
- [x] `UI/Animation/AnimatedValueSource.cs`
- [~] `UI/Styling/StyleTransition.cs` — type exists; expansion is frozen until style/render invalidation under animation stress is proven.

Tests:

- [x] `tests/Cerneala.Tests/UI/Animation/AnimationClockTests.cs`
- [x] `tests/Cerneala.Tests/UI/Animation/AnimationSchedulerTests.cs`
- [x] `tests/Cerneala.Tests/UI/Animation/TypedAnimationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Animation/TransitionTests.cs`
- [x] `tests/Cerneala.Tests/UI/Animation/AnimationInvalidationTests.cs`
- [x] `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs`

Acceptance checklist:

- [x] Animating a render-only property does not run layout in focused unit tests.
- [x] Animating a layout property enqueues layout only at ticks where the value changes in focused unit tests.
- [x] Completed animations release animated value source cleanly.
- [x] Animation and style transition APIs stay backend-neutral.
- [ ] Animation stress tests prove retained scheduler/render invalidation stays honest across many animated elements.
- [ ] Full project tests pass for the stress-tested animation scenario before this phase is scenario-complete.

## 24. [Later] Platform boundaries and package shape

This phase keeps the core portable. The current repository has a single project with MonoGame dependencies; v2 should keep adapter code isolated and later decide whether to split packages.

Section summary: Platform contracts and boundary tests are complete; package split project files stay deferred until real split work exists.

- [x] `UI/Platform/IPlatformServices.cs`
- [x] `UI/Platform/IClipboard.cs`
- [x] `UI/Platform/ICursorService.cs`
- [x] `UI/Platform/IFileDialogService.cs`
- [x] `UI/Platform/ITextInputPlatform.cs`
- [x] `UI/Platform/IDpiProvider.cs`
- [x] `UI/Hosting/MonoGame/` remains the only MonoGame UI host adapter folder.
- [ ] `Cerneala.Core.csproj` — optional future package split, deferred until real split work.
- [ ] `Cerneala.MonoGame.csproj` — optional future package split, deferred until real split work.
- [ ] `Cerneala.Tests.Core.csproj` — optional future package split, deferred until real split work.
- [ ] `Cerneala.Tests.MonoGame.csproj` — optional future package split, deferred until real split work.

Tests:

- [x] `tests/Cerneala.Tests/UI/Platform/PlatformBoundaryTests.cs`
- [x] `tests/Cerneala.Tests/UI/Platform/ServiceRegistrationTests.cs`
- [x] `tests/Cerneala.Tests/Architecture/MonoGameDependencyBoundaryTests.cs`

Acceptance checklist:

- [x] Platform services expose clipboard, cursor, dialogs, text input, DPI, and accessibility seams without backend dependencies.
- [x] MonoGame host integration remains adapter-scoped under `UI/Hosting/MonoGame/`.
- [x] Optional package split project files are intentionally deferred and not claimed as implemented.
- [x] Full project tests pass for this phase.

## 25. [Optional/Experimental] Markup, serialization, and source generation

This phase is optional and **frozen** until retained core contracts are stable. Cerneala stays code-first and strongly typed; markup may remain as a prototype for tooling/designer workflows, but it must not become a reflection-heavy runtime requirement or force core API shape.

- [~] `UI/Markup/UiMarkupDocument.cs` — prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiMarkupReader.cs` — prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiMarkupWriter.cs` — prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiMarkupSchema.cs` — prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiMarkupTypeRegistry.cs` — prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiFactory.cs` — prototype exists; keep as typed factory path, not core runtime magic.
- [~] `UI/Markup/GeneratedUiFactory.cs` — prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/MarkupLoadOptions.cs` — prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/MarkupDiagnostic.cs` — prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/ContentPropertyAttribute.cs` — optional ergonomic hint; frozen with markup.
- [~] `UI/Markup/DesignTimeOnlyAttribute.cs` — optional tooling hint; frozen with markup.
- [~] `Cerneala.SourceGen/UiMarkupGenerator.cs` — optional source generator prototype; frozen until runtime reflection cost or tooling demand proves it is needed.

Tests:

- [x] `tests/Cerneala.Tests/UI/Markup/UiMarkupReaderTests.cs`
- [x] `tests/Cerneala.Tests/UI/Markup/UiMarkupWriterTests.cs`
- [x] `tests/Cerneala.Tests/UI/Markup/UiFactoryTests.cs`
- [x] `tests/Cerneala.Tests/UI/Markup/MarkupDiagnosticTests.cs`
- [x] `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs` — source generator tests for generated source, compilation, typed property emission, and diagnostics.

Acceptance checklist:

- [x] Markup is not required to create controls.
- [x] Markup does not bypass typed property validation in covered prototype tests.
- [~] Generated factories produce retained trees through typed creation paths in prototype tests.
- [ ] Markup/source generation remains frozen until retained core contracts are stable and a real tooling scenario requires expansion.

## 26. [Optional/Experimental] Advanced input categories

This phase is experimental/frozen until platform adapters can supply real touch, stylus, drag/drop, manipulation, cursor, and ink data. Existing event metadata and processors can remain as prototypes, but platform-backed behavior is not scenario-complete unless the line explicitly says so.

- [~] `UI/Input/InputEvents.cs` already declares stylus, touch, manipulation, and drag/drop event metadata; behavior remains platform-gated.
- [~] `UI/Input/TouchInputBridge.cs` — prototype exists; frozen until platform touch data is available.
- [~] `UI/Input/StylusInputBridge.cs` — prototype exists; frozen until platform stylus data is available.
- [~] `UI/Input/GestureRecognizer.cs` — prototype exists; frozen until gesture scenarios are platform-backed.
- [~] `UI/Input/ManipulationProcessor.cs` — prototype exists; frozen until manipulation scenarios are platform-backed.
- [~] `UI/Input/DragDropController.cs` — prototype exists; frozen until drag/drop platform behavior exists.
- [~] `UI/Input/DataTransfer.cs` — type exists; frozen until drag/drop and clipboard/platform transfer behavior exist.
- [~] `UI/Input/Cursor.cs` — type exists; frozen until cursor platform behavior is wired.
- [~] `UI/Input/CursorService.cs` — type exists; frozen until cursor platform behavior is wired.
- [~] `UI/Controls/InkCanvas.cs` — prototype exists; frozen until stylus/touch platform behavior is real.
- [~] `UI/Ink/Stroke.cs` — type exists; frozen with ink scenarios.
- [~] `UI/Ink/StrokeCollection.cs` — type exists; frozen with ink scenarios.

Tests:

- [x] `tests/Cerneala.Tests/Input/TouchInputBridgeTests.cs`
- [x] `tests/Cerneala.Tests/Input/StylusInputBridgeTests.cs`
- [x] `tests/Cerneala.Tests/Input/GestureRecognizerTests.cs`
- [x] `tests/Cerneala.Tests/Input/ManipulationProcessorTests.cs`
- [x] `tests/Cerneala.Tests/Input/DragDropControllerTests.cs`
- [x] `tests/Cerneala.Tests/Controls/InkCanvasTests.cs`
- [x] `tests/Cerneala.Tests/Architecture/MonoGameDependencyBoundaryTests.cs` — covers advanced input backend-neutral boundaries.

Acceptance checklist:

- [~] Event metadata and prototype processors exist.
- [ ] Platform adapters provide real touch/stylus/drag/drop/manipulation input data.
- [ ] Retained input cache, focus, capture, and hit testing are proven under advanced input scenarios.
- [ ] Full project tests pass for platform-backed advanced input before this phase is scenario-complete.

## 27. Implementation order

This order prioritizes a working retained UI loop before broad API coverage.

### MVP order

- [x] 1. Add durable architecture docs, retained frame-loop diagrams, and repository shape tests for project memory.
- [x] 2. Add `UI/Core/UiProperty{T}.cs`, metadata, store, registry, and property invalidation tests.
- [x] 3. Add `UI/Elements/UIElement.cs`, `UIElementCollection`, `UIRoot`, element ids, lifecycle, and tree tests.
- [x] 4. Add `InvalidationFlags`, dirty propagation, `LayoutQueue`, `RenderQueue`, `UiFrameScheduler`, and no-work-frame tests.
- [x] 5. Add layout primitives, `LayoutManager`, `Panel`, `Canvas`, `StackPanel`, and layout cache tests.
- [x] 6. Add `RenderContext`, `ElementRenderCache`, `RetainedRenderer`, root command-list cache, and retained render tests.
- [x] 7. Add `UiHost` and `MonoGameUiHost` so update/draw uses retained frame scheduling.
- [x] 8. Add hit testing, `ElementInputBridge`, hover/pressed state tracking, and focus manager MVP.
- [x] 9. Add `CommandRouter`, `ActionCommand`, `ButtonBase.Command`, and command route tests.
- [x] 10. Add `Control`, `Border`, `TextBlock`, `Button`, first panels, and acceptance tests.
- [x] 11. Add text measurement/layout cache services above existing Skia/HarfBuzz text pipeline.
- [x] 12. Add resource dependency tracking for fonts/images.
- [x] 13. Update playground to show retained UI plus invalidation stats.

### Core order

- [x] 14. Add styling/theme engine with typed setters and pseudo-class rules.
- [x] 15. Add code-first templates and presenters.
- [x] 16. Add scrolling/range controls.
- [x] 17. Add items, selection, and virtualization.
- [x] 18. Add typed data observation and binding-light APIs.
- [x] 19. Add diagnostics/devtools overlays and tree/cache dumpers.

### Later order

- [~] 20. Add text editing and IME composition — foundations exist; production text services and platform behavior remain later.
- [~] 21. Add accessibility semantics and platform-neutral semantic tree — semantic tree exists; platform adapters remain later.
- [~] 22. Add advanced rendering/media primitives as scenarios require — descriptor types exist; backend-supported rendering remains later.
- [~] 23. Add animation and transitions — primitives exist; expansion waits for animation stress invalidation proof.
- [~] 24. Decide package/platform split — platform contracts exist; package split remains deferred.

### Optional/Experimental order

- [~] 25. Prototype markup/serialization after templates and typed properties are stable — prototype exists and is frozen until retained core contracts are stable.
- [~] 26. Prototype source generation if runtime reflection becomes a real cost — prototype exists and is frozen until tooling or runtime cost justifies expansion.
- [~] 27. Implement touch/stylus/drag/drop behavior when platform adapters can supply real data — prototype pieces exist; platform-backed behavior remains later.

## 28. Risks and decisions needing human confirmation


### Naming and API shape

- [x] Public base name is `UIElement` for familiarity and accuracy.
- [x] `Control` belongs in `UI/Controls/Control.cs` with namespace `Cerneala.UI.Controls`; core element types live separately in `Cerneala.UI.Elements`.
- [x] MVP color properties use existing `DrawColor` directly. Introduce a higher-level color type only when style/media requirements justify it.
- [x] Layout uses `float` for MonoGame/drawing alignment and fewer render-boundary conversions.
- [x] Layout geometry uses `LayoutSize`, `LayoutPoint`, and `LayoutRect` to avoid duplicate meaning with existing drawing primitives.

### Retained tree and routing

- [x] MVP uses separate logical and visual trees. This is intentionally more WPF-like and more complex than a single-tree MVP.
- [x] Replace `UiInputTree` as the route table for the new retained architecture. Preserve useful routed-event concepts and tests, but route through the new retained tree model instead of maintaining a parallel input tree.
- [x] Disabled, hidden, and collapsed elements are skipped by hit testing and do not receive pointer input.
- [x] Invisible but layout-reserved elements cannot receive focus or input.

### Invalidation and rendering

- [x] MVP uses subtree render caches from the start rather than only a root flattened cache.
- [x] Core continues with subtree-level flattened caches for large UI trees.
- [x] Input visual state invalidation is decided by style metadata, not hardcoded as always render-only.
- [x] Resource invalidation is driven by resource metadata that declares whether the resource affects measurement/layout or only rendering.
- [x] MVP uses deterministic full processing of dirty work. `FrameBudget` does not defer work until performance data justifies it.

### Styling, templates, and data

- [x] Style/value precedence order is `local > animation > style visual state > style base > inherited > default`.
- [x] Templates are code-first until a markup layer exists.
- [x] Typed binding APIs are enough for Core before adding string property paths.
- [x] Keep WPF template names where useful: `ControlTemplate`, `DataTemplate`, and `ItemsPanelTemplate`.

### Platform and packages

- [x] Keep the repository as one project through MVP.
- [x] MonoGame remains the primary backend adapter after MVP.
- [x] Platform services such as clipboard, cursor, dialogs, and IME remain adapter-only/later until required by controls.

### Testing and acceptance gates

- [x] Every new public type needs unit tests unless it is a trivial enum or marker type.
- [x] Retained no-work-frame tests are release blockers for MVP.
- [x] Architecture boundary tests fail if UI core references MonoGame, Skia, HarfBuzz, or backend-specific types.
- [x] Use both command-list assertions and visual golden-image tests after the first retained playground sample.

### Roadmap honesty

- [x] File/test existence is not enough to mark Later or Optional/Experimental work scenario-complete.
- [x] Descriptor-only APIs remain `[~]` until they are wired into retained scheduling and relevant backend/platform adapters.
- [x] Frozen areas may keep existing code, but should not expand public surface until their prerequisites are met.

## 29. MVP completion definition

MVP is complete when Cerneala can run a retained UI sample inside the MonoGame playground with deterministic tests proving invalidation-driven behavior.

- [x] `MonoGameUiHost` exists and is used by `Playground/Cerneala.Playground/Game1.cs`.
- [x] `UIRoot` retains a tree containing `StackPanel`, `Border`, `TextBlock`, and `Button`.
- [x] Layout runs on first frame and when layout-affecting state changes.
- [x] Rendering commands are regenerated only when render-affecting state changes.
- [x] `IDrawingBackend.Render` can be called every draw frame with cached commands.
- [x] Existing `UI/Drawing` tests still pass.
- [x] Existing `UI/Input` tests still pass.
- [x] New retained no-work-frame tests pass.
- [x] Hover, press, focus, and command execution work through retained input routing.
- [x] Text rendering uses existing `DrawTextRun`, `SkiaTextShaper`, and `SkiaTextRasterizer` through higher-level services.
- [x] No UI core control directly references MonoGame, Skia, HarfBuzz, `SpriteBatch`, or `Texture2D`.
