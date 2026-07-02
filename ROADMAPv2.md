# Cerneala ROADMAP v2

This file is the long-term project memory for Cerneala's modern retained UI architecture.

Product vision: **WPF 2026**, not WPF 2000. Cerneala should keep the WPF ideas that still help developer ergonomics while avoiding a compatibility-driven clone. The core should be retained-mode, game-loop-friendly, strongly typed, explicit, testable, backend-neutral where possible, and built on the existing `UI/Drawing` and `UI/Input` foundations.

The UI tree is retained. The game loop may call update and draw every frame, but Cerneala should not recompute layout or regenerate drawing commands unless invalidated state requires it.

Legend:

- `[x]` Exists now.
- `[ ]` Planned.
- `[~]` Exists partially, exists as a low-level primitive, or needs reshaping/integration for the v2 architecture.

Scope bands:

- **MVP**: the minimum usable retained UI stack in the MonoGame playground.
- **Core**: architecture that should become stable before broad control growth.
- **Later**: important product areas that should wait for stable core layers.
- **Optional/Experimental**: useful ideas that should not shape the core until proven.

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

- [x] `openspec/config.yaml` — existing OpenSpec configuration using the `spec-driven` schema.
- [ ] `openspec/README.md` — document how this repo uses OpenSpec for Cerneala planning.
- [ ] `openspec/project.md` — product principles, scope bands, and non-goals for the retained UI core.
- [ ] `openspec/specs/retained-ui-tree/spec.md`
- [ ] `openspec/specs/invalidation-and-frame-loop/spec.md`
- [ ] `openspec/specs/typed-state/spec.md`
- [ ] `openspec/specs/layout/spec.md`
- [ ] `openspec/specs/render-cache/spec.md`
- [ ] `openspec/specs/input-focus-command-bridge/spec.md`
- [ ] `openspec/specs/styling-theme/spec.md`
- [ ] `docs/architecture-v2.md` — concise architecture complement to `architecture.md`, focused on layers above drawing/input.
- [ ] `docs/diagrams/retained-frame-loop.md` — text diagram for update/layout/render-cache/draw flow.
- [ ] `docs/diagrams/ui-layer-boundaries.md` — text diagram showing UI core -> drawing/input -> MonoGame adapters.

Tests and checks:

- [ ] `tests/Cerneala.Tests/Architecture/RepositoryShapeTests.cs` — verifies planned top-level folders do not accidentally depend on MonoGame except adapter folders.
- [ ] `tests/Cerneala.Tests/Architecture/NamespaceBoundaryTests.cs` — verifies new UI core namespaces do not reference Skia, HarfBuzz, `SpriteBatch`, `Texture2D`, or `Mouse.GetState()`.

## 2. [MVP] Typed state model

This phase replaces the idea of cloning WPF `DependencyProperty` with a smaller typed property/state system. Properties should be strongly typed, explicitly registered, easy to test, and able to declare invalidation effects without reflection-heavy behavior.

- [ ] `UI/Core/UiObject.cs` — base object with typed property storage and lifecycle hooks.
- [ ] `UI/Core/UiProperty.cs` — non-generic descriptor base for internal indexing and diagnostics.
- [ ] `UI/Core/UiProperty{T}.cs` — strongly typed property descriptor.
- [ ] `UI/Core/UiPropertyKey{T}.cs` — key for read-only or owner-only properties.
- [ ] `UI/Core/UiPropertyMetadata{T}.cs` — default value, equality, validation, and invalidation metadata.
- [ ] `UI/Core/UiPropertyOptions.cs` — `[Flags]` for `AffectsMeasure`, `AffectsArrange`, `AffectsRender`, `AffectsHitTest`, `AffectsStyle`, `Inherits`, and `ReadOnly`.
- [ ] `UI/Core/UiPropertyValueSource.cs` — local, style, inherited, animated, default.
- [ ] `UI/Core/UiPropertyStore.cs` — internal compact store keyed by `UiProperty` identity.
- [ ] `UI/Core/UiPropertyChangedEventArgs.cs`
- [ ] `UI/Core/UiPropertyChangedEventArgs{T}.cs`
- [ ] `UI/Core/IUiPropertyOwner.cs`
- [ ] `UI/Core/UiPropertyRegistry.cs` — explicit registration and duplicate detection.
- [ ] `UI/Core/Unset.cs` — internal sentinel only; avoid public magic values unless proven necessary.
- [ ] `UI/Core/CoerceValue.cs` — optional typed coercion delegate, explicit and non-reflective.
- [ ] `UI/Core/ValidateValue.cs` — optional typed validation delegate.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Core/UiPropertyTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Core/UiPropertyRegistryTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Core/UiPropertyStoreTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Core/UiPropertyInvalidationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Core/ReadOnlyUiPropertyTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Core/InheritedUiPropertyTests.cs`

Acceptance checklist:

- [ ] Setting a typed property returns the previous typed value without casts in user code.
- [ ] Setting an equal value does not enqueue layout or render work.
- [ ] `AffectsMeasure` invalidates measure and render through the invalidation system.
- [ ] `AffectsRender` invalidates render without forcing measure.
- [ ] Style values and local values have explicit precedence.
- [ ] Property registration is deterministic and testable.

## 3. [MVP] Retained element tree

This phase creates the retained UI tree that owns state, layout, rendering hooks, event handlers, and child relationships. It should be a modern single tree at first; logical/visual splits can be introduced later only if a real feature requires them.

- [ ] `UI/Elements/UIElement.cs` — retained element base with parent, children, enabled/visible state, layout slots, dirty flags, handlers, and virtual lifecycle methods.
- [ ] `UI/Elements/UIElementCollection.cs` — owned child collection with parent validation and change notifications.
- [ ] `UI/Elements/UIRoot.cs` — root element with viewport size, scaling, input route ownership, and render cache root.
- [ ] `UI/Elements/ElementLifecycle.cs` — attach/detach hooks and tree versioning.
- [ ] `UI/Elements/ElementIdProvider.cs` — assigns stable `UiElementId` values for input routing.
- [ ] `UI/Elements/ElementTreeWalker.cs` — pre-order, post-order, ancestor, and descendant traversal helpers.
- [ ] `UI/Elements/ElementTreeChange.cs`
- [ ] `UI/Elements/IElementChildHost.cs` — explicit contract for controls that own generated children.
- [ ] `UI/Elements/IElementHost.cs` — implemented by `UIRoot` and future platform hosts.
- [ ] `UI/Elements/ElementHandlerStore.cs` — stores routed event handlers on retained elements.
- [~] `UI/Input/UiInputTree.cs` — keep as low-level route table unless a later decision replaces it.
- [ ] `UI/Input/ElementInputRouteBuilder.cs` — builds or updates `UiInputTree` from the retained element tree.
- [ ] `UI/Input/ElementInputRouteMap.cs` — maps `UIElement` <-> `UiElementId` for routed events.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Elements/UIElementTreeTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Elements/UIElementCollectionTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Elements/UIRootTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Elements/ElementLifecycleTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Elements/ElementTreeWalkerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/ElementInputRouteBuilderTests.cs`

Acceptance checklist:

- [ ] Adding a child sets exactly one parent.
- [ ] Removing a child clears parent and invalidates layout/render for affected ancestors.
- [ ] Reparenting without removal is rejected.
- [ ] Element ids are stable across frames while an element remains attached.
- [ ] `UiInputTree` route order matches the retained element ancestor chain.
- [ ] Disabled or invisible elements can be excluded from input routing according to explicit policy.

## 4. [MVP] Retained invalidation and frame scheduler

This phase is the core of the game-loop-friendly retained model. Update and draw can run every frame, but layout and drawing command generation should run only when dirty state requires it.

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

- [ ] `UI/Invalidation/InvalidationFlags.cs` — `[Flags]`: `None`, `Measure`, `Arrange`, `Render`, `Text`, `Image`, `Resource`, `Style`, `InputVisual`, `HitTest`, `Subtree`.
- [ ] `UI/Invalidation/DirtyState.cs` — compact per-element dirty state and version stamps.
- [ ] `UI/Invalidation/DirtyPropagation.cs` — rules for upward/downward propagation.
- [ ] `UI/Invalidation/IInvalidationSink.cs`
- [ ] `UI/Invalidation/InvalidationRequest.cs`
- [ ] `UI/Invalidation/LayoutQueue.cs` — stable queue for measure and arrange invalidations.
- [ ] `UI/Invalidation/RenderQueue.cs` — stable queue for render command regeneration.
- [ ] `UI/Invalidation/HitTestQueue.cs` — rebuild hit-test data only when needed.
- [ ] `UI/Invalidation/UiFrameScheduler.cs` — runs input effects, layout, render-cache updates, and diagnostics.
- [ ] `UI/Invalidation/FramePhase.cs` — `Input`, `Style`, `Measure`, `Arrange`, `RenderCache`, `Idle`.
- [ ] `UI/Invalidation/FrameStats.cs` — counts measured elements, arranged elements, rendered elements, reused caches.
- [ ] `UI/Invalidation/FrameBudget.cs` — optional limits for later large trees; MVP may process all work.
- [ ] `UI/Diagnostics/InvalidationTrace.cs`

Dirty propagation rules:

- [ ] `Measure` invalidation marks the element measure-dirty and propagates measure/arrange need to layout ancestors until a layout boundary.
- [ ] `Arrange` invalidation marks the element arrange-dirty and propagates render need for affected visual bounds.
- [ ] `Render` invalidation marks local render cache dirty without forcing measure or arrange.
- [ ] `Text` invalidation invalidates text measurement, local render cache, and layout only when text metrics may change.
- [ ] `Image` invalidation invalidates image measurement when intrinsic size is used; otherwise render only.
- [ ] `Resource` invalidation follows the metadata of the properties/resources that consumed the resource.
- [ ] `Style` invalidation reapplies style and then raises property-specific invalidations.
- [ ] `InputVisual` invalidation is render-only unless a control explicitly maps state to layout-affecting properties.
- [ ] Clearing dirty flags happens only after successful phase processing.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Invalidation/InvalidationFlagsTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Invalidation/DirtyPropagationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Invalidation/LayoutQueueTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Invalidation/RenderQueueTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Invalidation/FrameStatsTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Invalidation/RetainedNoWorkFrameTests.cs`

Required retained-mode tests:

- [ ] `RetainedNoWorkFrameTests.UnchangedTreeDoesNotMeasureOnSecondFrame`
- [ ] `RetainedNoWorkFrameTests.UnchangedTreeDoesNotArrangeOnSecondFrame`
- [ ] `RetainedNoWorkFrameTests.UnchangedTreeDoesNotRegenerateRenderCommandsOnSecondDraw`
- [ ] `RetainedNoWorkFrameTests.DrawEveryFrameCanReuseCachedRootCommandList`
- [ ] `RetainedNoWorkFrameTests.RenderOnlyInvalidationDoesNotRunMeasure`
- [ ] `RetainedNoWorkFrameTests.MeasureInvalidationRegeneratesRenderCommandsOnlyAfterLayoutSettles`
- [ ] `RetainedNoWorkFrameTests.HoverChangeInvalidatesRenderOnlyWhenVisualStateActuallyChanges`
- [ ] `RetainedNoWorkFrameTests.TextColorChangeRebuildsRenderCommandsWithoutReshapingWhenMetricsAreUnchanged`

## 5. [MVP] Layout system

This phase adds WPF-inspired measure/arrange without copying WPF complexity. Layout types are intentionally named as layout types so they are not confused with `DrawPoint` and `DrawRect`, whose role is backend-neutral drawing command geometry.

- [ ] `UI/Layout/LayoutSize.cs` — layout measurement size; may support unconstrained dimensions where drawing primitives must not.
- [ ] `UI/Layout/LayoutPoint.cs` — layout coordinate, not a drawing command point.
- [ ] `UI/Layout/LayoutRect.cs` — layout slot, not a drawing command rectangle.
- [ ] `UI/Layout/Thickness.cs` — margin, padding, border thickness.
- [ ] `UI/Layout/Alignment.cs` — horizontal/vertical alignment values.
- [ ] `UI/Layout/Visibility.cs` — `Visible`, `Hidden`, `Collapsed`.
- [ ] `UI/Layout/LayoutRounding.cs` — explicit pixel snapping policy.
- [ ] `UI/Layout/MeasureContext.cs`
- [ ] `UI/Layout/ArrangeContext.cs`
- [ ] `UI/Layout/LayoutResult.cs`
- [ ] `UI/Layout/LayoutManager.cs` — consumes `LayoutQueue`, caches desired size and arranged bounds.
- [ ] `UI/Layout/LayoutBoundary.cs` — marks roots or subtrees where propagation can stop.
- [ ] `UI/Layout/ILayoutElement.cs`
- [ ] `UI/Layout/Panels/Panel.cs`
- [ ] `UI/Layout/Panels/Canvas.cs`
- [ ] `UI/Layout/Panels/StackPanel.cs`
- [ ] `UI/Layout/Panels/Orientation.cs`
- [ ] `UI/Layout/Panels/Grid.cs` — later in MVP only if needed; otherwise Core.
- [ ] `UI/Layout/Panels/GridLength.cs`
- [ ] `UI/Layout/Panels/ColumnDefinition.cs`
- [ ] `UI/Layout/Panels/RowDefinition.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Layout/LayoutPrimitiveTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/LayoutManagerTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/UIElementMeasureArrangeTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/LayoutInvalidationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/VisibilityTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/CanvasTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/StackPanelTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/GridTests.cs`

Acceptance checklist:

- [ ] Measure results are cached by available size and element version.
- [ ] Arrange results are cached by final rect and element version.
- [ ] A no-op property set does not invalidate layout.
- [ ] Parent layout invalidation does not force unchanged children to re-measure when constraints are unchanged.
- [ ] `Collapsed` removes an element from layout and hit testing.
- [ ] Layout output can be converted explicitly to `DrawRect` only at rendering boundaries.

## 6. [MVP] Retained rendering and render cache

This phase connects retained elements to the existing `DrawingContext`, `DrawCommandList`, and `IDrawingBackend`. The retained renderer owns cache invalidation above the drawing layer; the drawing layer remains a command recorder/backend contract.

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

- [ ] `UI/Rendering/RenderContext.cs` — exposes `DrawingContext`, layout bounds, inherited opacity/clip, and diagnostics.
- [ ] `UI/Rendering/IRenderableElement.cs`
- [ ] `UI/Rendering/ElementRenderCache.cs` — local cached `DrawCommandList`, local version, content bounds, and dependency versions.
- [ ] `UI/Rendering/RetainedRenderCache.cs` — root/subtree command cache and cache versioning.
- [ ] `UI/Rendering/RenderQueueProcessor.cs` — regenerates only dirty local element command lists.
- [ ] `UI/Rendering/RetainedRenderer.cs` — produces cached root command list for `IDrawingBackend`.
- [ ] `UI/Rendering/DrawCommandListBuilder.cs` — flattens cached local commands in visual order.
- [ ] `UI/Rendering/DrawCommandListPool.cs` — optional pooling after correctness is proven.
- [ ] `UI/Rendering/ClipNode.cs` — retained clip metadata translated to `PushClip`/`PopClip` commands.
- [ ] `UI/Rendering/RenderLayer.cs` — future boundary for opacity/effects; MVP can be minimal.
- [ ] `UI/Rendering/RenderDependency.cs` — tracks text/image/theme/resource dependencies that affect cached commands.
- [ ] `UI/Rendering/RenderCounters.cs` — counts cache hits/misses and command regeneration.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Rendering/ElementRenderCacheTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Rendering/RetainedRenderCacheTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Rendering/RenderQueueProcessorTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Rendering/RetainedRendererTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Rendering/DrawCommandListBuilderTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Rendering/RenderDependencyTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Rendering/RenderCountersTests.cs`

Acceptance checklist:

- [ ] `OnRender` is called only for elements with render dirty state or changed render dependencies.
- [ ] The same unchanged root command list can be rendered by `IDrawingBackend` across multiple draw frames.
- [ ] A child render change does not regenerate unrelated sibling local command lists.
- [ ] Render order is deterministic and matches retained tree order.
- [ ] Clip commands are balanced even when a subtree renders no visible commands.
- [ ] Current primitive drawing uses existing `DrawRect`, `DrawPoint`, `DrawColor`, `DrawTextRun`, and `IDrawImage`.

## 7. [MVP] Game-loop host integration

This phase gives applications a simple retained UI entry point that fits MonoGame's `Update`/`Draw` rhythm while keeping Cerneala backend-neutral above adapters.

- [ ] `UI/Hosting/UiHost.cs` — owns root, frame scheduler, input bridge, layout manager, renderer, and services.
- [ ] `UI/Hosting/UiHostOptions.cs`
- [ ] `UI/Hosting/UiFrame.cs` — frame time, viewport, input frame, diagnostics.
- [ ] `UI/Hosting/UiViewport.cs`
- [ ] `UI/Hosting/IUiClock.cs`
- [ ] `UI/Hosting/IUiBackend.cs` — backend-neutral host bridge for drawing/input adapters.
- [ ] `UI/Hosting/MonoGame/MonoGameUiHost.cs` — adapter around `MonoGameInputSource` and `MonoGameDrawingBackend`.
- [ ] `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`
- [ ] `UI/Hosting/MonoGame/MonoGameContentServices.cs` — image/font service glue; no control should use MonoGame types directly.
- [~] `Playground/Cerneala.Playground/Game1.cs` — update to create `MonoGameUiHost`, set a retained `UIRoot`, call `Update`, then call `Draw`.

Frame contract:

- [ ] `UiHost.Update(frameTime)` reads or receives `InputFrame`, dispatches input, updates visual states, processes style/layout/render queues, and records frame stats.
- [ ] `UiHost.Draw(IDrawingBackend backend)` renders the cached root `DrawCommandList` without forcing layout or render regeneration.
- [ ] Viewport size changes invalidate root arrange and render.
- [ ] First frame performs full measure, arrange, and render cache generation.
- [ ] Later frames do no layout/render work unless invalidated.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Hosting/UiHostTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Hosting/UiHostFrameContractTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Hosting/UiViewportTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Hosting/MonoGameUiHostBoundaryTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Hosting/FakeUiClock.cs`
- [ ] `tests/Cerneala.Tests/UI/Hosting/FakeDrawingBackend.cs`
- [ ] `tests/Cerneala.Tests/UI/Hosting/FakeInputSource.cs`

## 8. [MVP] Input bridge, hit testing, focus, and visual state

This phase turns existing input snapshots and routed events into retained-control behavior. It should reuse `InputFrame`, `UiInputTree`, `RoutedEventRouter`, and WPF-familiar event names instead of replacing them.

- [ ] `UI/Input/ElementInputBridge.cs` — converts `InputFrame` transitions into routed events against retained elements.
- [ ] `UI/Input/HitTestService.cs` — hit tests retained layout/render bounds.
- [ ] `UI/Input/HitTestResult.cs`
- [ ] `UI/Input/HitTestFilter.cs`
- [ ] `UI/Input/PointerCaptureManager.cs`
- [ ] `UI/Input/HoverTracker.cs` — drives `IsPointerOver` and render invalidation.
- [ ] `UI/Input/PressedStateTracker.cs` — drives button pressed state and click synthesis.
- [ ] `UI/Input/ClickTracker.cs`
- [ ] `UI/Input/FocusManager.cs` — explicit focus service, not a global static dependency.
- [ ] `UI/Input/FocusScope.cs` — Core if MVP does not need nested scopes.
- [ ] `UI/Input/KeyboardNavigation.cs` — Core if MVP only supports direct focus.
- [ ] `UI/Input/TextInputBridge.cs` — maps `TextInputSnapshotEvent` to preview/bubble text events.
- [ ] `UI/Input/ElementRoutedEventStore.cs` — handler storage attached to `UIElement`.
- [~] `UI/Input/RoutedEventRouter.cs` — keep routing core; add retained-tree bridge rather than duplicating route logic.
- [~] `UI/Input/InputEvents.cs` — use existing mouse/key/text events first; stylus/touch/drag metadata remains later behavior.

Visual state properties:

- [ ] `UI/Elements/UIElement.IsEnabledProperty`
- [ ] `UI/Elements/UIElement.IsVisibleProperty`
- [ ] `UI/Elements/UIElement.IsPointerOverProperty`
- [ ] `UI/Elements/UIElement.IsKeyboardFocusWithinProperty`
- [ ] `UI/Elements/UIElement.IsKeyboardFocusedProperty`
- [ ] `UI/Controls/Primitives/ButtonBase.IsPressedProperty`

Tests:

- [ ] `tests/Cerneala.Tests/Input/ElementInputBridgeTests.cs`
- [ ] `tests/Cerneala.Tests/Input/HitTestServiceTests.cs`
- [ ] `tests/Cerneala.Tests/Input/PointerCaptureManagerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/HoverTrackerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/PressedStateTrackerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/ClickTrackerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/FocusManagerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/TextInputBridgeTests.cs`
- [ ] `tests/Cerneala.Tests/Input/RetainedRoutedEventIntegrationTests.cs`

Acceptance checklist:

- [ ] Mouse down raises preview then bubble events on the hit-tested retained element.
- [ ] Mouse move updates hover state and invalidates render only when hover target changes.
- [ ] Disabled elements do not receive input handlers.
- [ ] Keyboard events target focused element.
- [ ] Focus change raises existing focus routed events.
- [ ] Text input uses `TextInputSnapshotEvent` and existing text routed event args.
- [ ] Input routing parent chain matches retained tree parent chain.

## 9. [MVP] Commands and actions

This phase completes route-based command execution without copying WPF's global `CommandManager` magic. Commands should be explicit, testable, and based on existing command primitives.

- [~] `UI/Input/ICommand.cs`
- [~] `UI/Input/RoutedCommand.cs`
- [~] `UI/Input/CommandBinding.cs`
- [~] `UI/Input/CommandEvents.cs`
- [ ] `UI/Input/CommandBindingCollection.cs`
- [ ] `UI/Input/CommandRouter.cs` — explicit service that queries and executes through retained routes.
- [ ] `UI/Input/RoutedCommandContext.cs`
- [ ] `UI/Input/ActionCommand.cs` — simple command backed by delegates.
- [ ] `UI/Input/InputGesture.cs` — Core if MVP needs keyboard shortcuts.
- [ ] `UI/Input/KeyGesture.cs` — Core if MVP needs keyboard shortcuts.
- [ ] `UI/Input/InputBinding.cs` — Core if MVP needs shortcut-to-command mapping.
- [ ] `UI/Input/KeyBinding.cs` — Core if MVP needs shortcut-to-command mapping.
- [ ] `UI/Controls/Primitives/ButtonBase.CommandProperty`
- [ ] `UI/Controls/Primitives/ButtonBase.CommandParameterProperty`

Tests:

- [ ] `tests/Cerneala.Tests/Input/CommandRouterTests.cs`
- [ ] `tests/Cerneala.Tests/Input/CommandBindingCollectionTests.cs`
- [ ] `tests/Cerneala.Tests/Input/ActionCommandTests.cs`
- [ ] `tests/Cerneala.Tests/Input/RoutedCommandExecutionTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseCommandTests.cs`
- [ ] `tests/Cerneala.Tests/Input/InputGestureTests.cs`

Acceptance checklist:

- [ ] `RoutedCommand.CanExecute` uses the current command route only through explicit `CommandRouter` APIs.
- [ ] `RoutedCommand.Execute` no longer throws after command routing is implemented.
- [ ] `ButtonBase` queries command state during update or invalidation, not through hidden global requery magic.
- [ ] Command state changes can invalidate visual state for controls that display enabled/disabled state.

## 10. [MVP] First controls and panels

This phase creates the smallest useful control set. Controls should be retained, layout-aware, input-aware, and render through `DrawingContext` commands. Keep names familiar where they are ergonomic.

- [ ] `UI/Controls/Control.cs` — base control with styling hooks and common visual properties.
- [ ] `UI/Controls/ContentControl.cs`
- [ ] `UI/Controls/Decorator.cs`
- [ ] `UI/Controls/Border.cs`
- [ ] `UI/Controls/Panel.cs` — public alias or wrapper over `UI/Layout/Panels/Panel` if the final namespace should be controls-oriented.
- [ ] `UI/Controls/Canvas.cs`
- [ ] `UI/Controls/StackPanel.cs`
- [ ] `UI/Controls/TextBlock.cs`
- [ ] `UI/Controls/Image.cs`
- [ ] `UI/Controls/Primitives/ButtonBase.cs`
- [ ] `UI/Controls/Button.cs`
- [ ] `UI/Controls/Primitives/ToggleButton.cs` — Core if not needed for MVP.
- [ ] `UI/Controls/CheckBox.cs` — Core if not needed for MVP.
- [ ] `UI/Controls/ControlTemplate.cs` — Core; MVP controls may render directly first.
- [ ] `UI/Controls/TemplatePart.cs` — Core.
- [ ] `UI/Controls/VisualState.cs` — minimal state names for hover/pressed/focus/disabled.

Common control properties:

- [ ] `Control.BackgroundProperty` — MVP can use `DrawColor`; richer brushes wait for Core rendering features.
- [ ] `Control.ForegroundProperty` — MVP can use `DrawColor`.
- [ ] `Control.BorderColorProperty` — MVP can use `DrawColor`.
- [ ] `Control.BorderThicknessProperty` — `Thickness`.
- [ ] `Control.PaddingProperty` — `Thickness`.
- [ ] `Control.FontFamilyProperty` — string or typed font reference backed by `IFontSource`.
- [ ] `Control.FontSizeProperty` — validates through drawing/text size constraints.

Tests:

- [ ] `tests/Cerneala.Tests/Controls/ControlTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ContentControlTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/DecoratorTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/BorderTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/PanelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/CanvasTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/StackPanelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/TextBlockTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ImageTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ButtonTests.cs`

MVP control acceptance checklist:

- [ ] A retained `Button` can be added to `UIRoot`, measured, arranged, rendered, hit-tested, hovered, pressed, clicked, and command-bound.
- [ ] A retained `TextBlock` measures text using the existing Skia/HarfBuzz pipeline through higher-level text services.
- [ ] A retained `Border` renders fill/stroke with existing rectangle commands.
- [ ] A retained `StackPanel` lays out children and avoids re-measuring unchanged children.
- [ ] Control visual states invalidate render only when the state affects visible output.

## 11. [MVP] Text services above the existing drawing text pipeline

This phase adds layout and cache services for controls such as `TextBlock` without rebuilding shaping/rasterization. The existing Skia/HarfBuzz code remains the low-level text engine.

- [~] `UI/Drawing/DrawTextRun.cs`
- [~] `UI/Drawing/Text/SkiaTextShaper.cs`
- [~] `UI/Drawing/Text/SkiaTextRasterizer.cs`
- [ ] `UI/Text/FontResolver.cs` — wraps `IFontSource` and theme/default font decisions.
- [ ] `UI/Text/TextRunStyle.cs` — font family, size, color, wrapping flags; converts to `DrawTextRun`.
- [ ] `UI/Text/TextMeasureResult.cs`
- [ ] `UI/Text/TextMeasurer.cs` — computes desired size and caches metrics.
- [ ] `UI/Text/TextLayoutCache.cs` — keyed by text, font, size, wrapping width, and DPI/scale.
- [ ] `UI/Text/TextRenderer.cs` — records text commands with `DrawingContext.DrawText`.
- [ ] `UI/Text/TextWrapping.cs`
- [ ] `UI/Text/TextTrimming.cs` — Core if MVP does not need trimming.
- [ ] `UI/Text/LineBreakService.cs` — Core if MVP only supports single-line text.
- [ ] `UI/Text/BidiTextService.cs` — Later.
- [ ] `UI/Text/TextSelection.cs` — Later.
- [ ] `UI/Text/TextEditingController.cs` — Later.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Text/FontResolverTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Text/TextMeasurerTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Text/TextLayoutCacheTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Text/TextRendererTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs`

Acceptance checklist:

- [ ] Text content changes invalidate text metrics and render commands.
- [ ] Text color changes invalidate render commands without forcing text shaping when glyph metrics are unchanged.
- [ ] Font family or font size changes invalidate measurement and render.
- [ ] Re-rendering unchanged text reuses cached text layout and retained render commands.

## 12. [MVP] Resources for fonts and images

This phase introduces explicit resource identity and invalidation without recreating WPF resource dictionaries as core machinery. Resources should be typed and observable enough to invalidate dependent layout/render caches.

- [ ] `UI/Resources/ResourceId{T}.cs`
- [ ] `UI/Resources/IResourceProvider.cs`
- [ ] `UI/Resources/ResourceStore.cs`
- [ ] `UI/Resources/ResourceChangedEventArgs.cs`
- [ ] `UI/Resources/ResourceDependencyTracker.cs`
- [ ] `UI/Resources/FontResource.cs`
- [ ] `UI/Resources/ImageResource.cs`
- [ ] `UI/Resources/IImageLoader.cs`
- [ ] `UI/Resources/MonoGame/MonoGameImageLoader.cs` — adapter that returns `IDrawImage`/`MonoGameImage` without leaking `Texture2D` into controls.
- [~] `UI/Drawing/IDrawImage.cs` — keep as draw-level image handle.
- [~] `UI/Drawing/IDrawFont.cs` — keep as draw-level font handle.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Resources/ResourceIdTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Resources/ResourceStoreTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Resources/ResourceDependencyTrackerTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Resources/ImageResourceInvalidationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Resources/FontResourceInvalidationTests.cs`

Acceptance checklist:

- [ ] Replacing an image resource invalidates render for controls using fixed size.
- [ ] Replacing an image resource invalidates layout if the control uses intrinsic image size.
- [ ] Replacing a font resource invalidates text measurement and render for dependent text controls.
- [ ] Resource lookup is explicit through host/services, not hidden global lookup.

## 13. [MVP] Playground scenario

This phase proves the retained architecture in a real MonoGame loop. The sample should draw every frame but do no layout/render command regeneration on no-op frames.

- [ ] `Playground/Cerneala.Playground/Samples/RetainedButtonSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/LayoutSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/TextSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/InvalidationStatsOverlay.cs`
- [ ] `Playground/Cerneala.Playground/Samples/SampleSelector.cs`
- [~] `Playground/Cerneala.Playground/Game1.cs` — wire sample selector through `MonoGameUiHost`.

MVP demo acceptance checklist:

- [ ] Window shows retained `StackPanel` containing `TextBlock`, `Button`, and `Border`.
- [ ] Mouse hover changes button visuals through retained input state.
- [ ] Button click executes an explicit command/action.
- [ ] On unchanged frames, diagnostics show zero measured elements, zero arranged elements, and zero regenerated local render caches.
- [ ] Draw still happens every frame through `MonoGameDrawingBackend.Render(cachedCommands)`.

## 14. [Core] Styling and themes

This phase adds modern styling after properties, retained tree, and invalidation are stable. Styling should be typed and explicit, not a XAML-first clone of WPF triggers/resources.

- [ ] `UI/Styling/Style.cs`
- [ ] `UI/Styling/StyleRule.cs`
- [ ] `UI/Styling/StyleSelector.cs`
- [ ] `UI/Styling/StyleSheet.cs`
- [ ] `UI/Styling/Setter.cs`
- [ ] `UI/Styling/Setter{T}.cs`
- [ ] `UI/Styling/StyleApplicator.cs`
- [ ] `UI/Styling/StyleInvalidation.cs`
- [ ] `UI/Styling/Theme.cs`
- [ ] `UI/Styling/ThemeKey{T}.cs`
- [ ] `UI/Styling/ThemeProvider.cs`
- [ ] `UI/Styling/ThemeResource.cs`
- [ ] `UI/Styling/PseudoClass.cs` — hover, pressed, focus, disabled, selected.
- [ ] `UI/Styling/VisualStateRule.cs`
- [ ] `UI/Styling/DefaultTheme.cs`
- [ ] `UI/Styling/ThemePalette.cs`
- [ ] `UI/Styling/StyleDiagnostics.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Styling/StyleTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Styling/StyleRuleTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Styling/SetterTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Styling/StyleApplicatorTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Styling/StyleInvalidationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Styling/ThemeTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Styling/PseudoClassTests.cs`

Acceptance checklist:

- [ ] Applying a style sets typed properties without reflection in the hot path.
- [ ] Style changes invalidate only affected layout/render/style work.
- [ ] Visual state changes can activate style rules and enqueue render invalidation.
- [ ] Theme changes propagate through resource dependencies.
- [ ] Local values override style values through explicit precedence.

## 15. [Core] Templates and composition

This phase enables reusable controls without forcing every control to hand-code rendering. Templates should be code-first and strongly typed first; optional markup can come later.

- [ ] `UI/Controls/ControlTemplate.cs`
- [ ] `UI/Controls/ControlTemplate{TControl}.cs`
- [ ] `UI/Controls/TemplateContext.cs`
- [ ] `UI/Controls/TemplateInstance.cs`
- [ ] `UI/Controls/TemplateBinding{T}.cs`
- [ ] `UI/Controls/TemplatePartAttribute.cs` — diagnostic only; no hidden runtime magic required.
- [ ] `UI/Controls/ItemsPanelTemplate.cs`
- [ ] `UI/Controls/DataTemplate.cs`
- [ ] `UI/Controls/DataTemplate{T}.cs`
- [ ] `UI/Controls/ContentPresenter.cs`
- [ ] `UI/Controls/ItemsPresenter.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/ControlTemplateTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/TemplateBindingTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ContentPresenterTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/DataTemplateTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ItemsPanelTemplateTests.cs`

Acceptance checklist:

- [ ] Template-generated children are retained across frames.
- [ ] Changing a template invalidates the subtree once, not every frame.
- [ ] Template bindings are strongly typed and do not use string property paths in the hot path.
- [ ] Template children participate in layout, rendering, hit testing, and input routing.

## 16. [Core] Additional controls and scrolling

This phase expands the useful control set after MVP controls, styling, and templates are stable.

- [ ] `UI/Controls/Primitives/RangeBase.cs`
- [ ] `UI/Controls/Primitives/Thumb.cs`
- [ ] `UI/Controls/Primitives/Track.cs`
- [ ] `UI/Controls/Primitives/ScrollBar.cs`
- [ ] `UI/Controls/ScrollViewer.cs`
- [ ] `UI/Controls/ScrollContentPresenter.cs`
- [ ] `UI/Controls/ScrollBarVisibility.cs`
- [ ] `UI/Controls/IScrollInfo.cs`
- [ ] `UI/Controls/Slider.cs`
- [ ] `UI/Controls/ProgressBar.cs`
- [ ] `UI/Controls/RadioButton.cs`
- [ ] `UI/Controls/Label.cs`
- [ ] `UI/Controls/ToolTip.cs`
- [ ] `UI/Controls/PopupRoot.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/Primitives/RangeBaseTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/ThumbTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/TrackTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ScrollBarTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ScrollViewerTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/SliderTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ProgressBarTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ToolTipTests.cs`

## 17. [Core] Items, selection, and virtualization

This phase should come after templates and scrolling. Lists must be retained and virtualized so large data sets do not create or re-render every element every frame.

- [ ] `UI/Controls/ItemsControl.cs`
- [ ] `UI/Controls/ItemCollection.cs`
- [ ] `UI/Controls/ItemContainerGenerator.cs`
- [ ] `UI/Controls/ItemContainerRecyclePool.cs`
- [ ] `UI/Controls/ItemsPresenter.cs`
- [ ] `UI/Controls/SelectionModel.cs`
- [ ] `UI/Controls/SelectionModel{T}.cs`
- [ ] `UI/Controls/Primitives/Selector.cs`
- [ ] `UI/Controls/ListBox.cs`
- [ ] `UI/Controls/ListBoxItem.cs`
- [ ] `UI/Controls/ComboBox.cs`
- [ ] `UI/Controls/TabControl.cs`
- [ ] `UI/Controls/TabItem.cs`
- [ ] `UI/Layout/Panels/VirtualizingStackPanel.cs`
- [ ] `UI/Layout/Virtualization/VirtualizationContext.cs`
- [ ] `UI/Layout/Virtualization/RealizationWindow.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/ItemsControlTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ItemContainerGeneratorTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/SelectionModelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/SelectorTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ListBoxTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/VirtualizingStackPanelTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Layout/VirtualizationTests.cs`

Acceptance checklist:

- [ ] Items controls retain generated containers only for realized items.
- [ ] Scrolling changes realization window and invalidates layout/render for affected items only.
- [ ] Selection changes invalidate visual state for selected containers only.
- [ ] Data updates do not rebuild unrelated realized containers.

## 18. [Core] Data observation and binding-light APIs

This phase provides modern data flow without making classic reflection-heavy binding the core. Start with explicit typed observation and templates; add string-path binding later only if needed.

- [ ] `UI/Data/ObservableValue{T}.cs`
- [ ] `UI/Data/ObservableList{T}.cs`
- [ ] `UI/Data/IObservableList{T}.cs`
- [ ] `UI/Data/PropertyAdapter{TOwner,TValue}.cs`
- [ ] `UI/Data/Binding.cs` — optional typed binding facade.
- [ ] `UI/Data/Binding{T}.cs`
- [ ] `UI/Data/BindingMode.cs`
- [ ] `UI/Data/IValueConverter{TIn,TOut}.cs`
- [ ] `UI/Data/CollectionView{T}.cs`
- [ ] `UI/Data/SortDescription{T}.cs`
- [ ] `UI/Data/FilterPredicate{T}.cs`
- [ ] `UI/Data/StringPropertyPath.cs` — Later, not hot-path core.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Data/ObservableValueTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Data/ObservableListTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Data/TypedBindingTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Data/CollectionViewTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Data/StringPropertyPathTests.cs`

## 19. [Core] Diagnostics and developer tools

This phase makes retained UI behavior inspectable. Diagnostics are required because invalidation-driven systems are hard to reason about without counters and tree dumps.

- [ ] `UI/Diagnostics/FrameDiagnostics.cs`
- [ ] `UI/Diagnostics/LayoutDiagnostics.cs`
- [ ] `UI/Diagnostics/RenderDiagnostics.cs`
- [ ] `UI/Diagnostics/InputDiagnostics.cs`
- [ ] `UI/Diagnostics/DirtyTreeDumper.cs`
- [ ] `UI/Diagnostics/ElementTreeDumper.cs`
- [ ] `UI/Diagnostics/RenderCacheDumper.cs`
- [ ] `UI/Diagnostics/RoutedEventTrace.cs`
- [ ] `UI/Diagnostics/StyleTrace.cs`
- [ ] `UI/Diagnostics/DebugOverlay.cs`
- [ ] `UI/Diagnostics/DebugAdorner.cs`
- [ ] `Playground/Cerneala.Playground/Samples/DiagnosticsSample.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Diagnostics/FrameDiagnosticsTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Diagnostics/DirtyTreeDumperTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Diagnostics/ElementTreeDumperTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Diagnostics/RenderCacheDumperTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Diagnostics/RoutedEventTraceTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Diagnostics/StyleTraceTests.cs`

Acceptance checklist:

- [ ] Developers can see per-frame measure/arrange/render-cache counts.
- [ ] Developers can dump which elements are dirty and why.
- [ ] Developers can trace routed event paths.
- [ ] Developers can inspect style sources for a property value.

## 20. [Later] Text editing and IME

This phase should wait until input, focus, text layout, and diagnostics are stable. Text editing should build on `TextInputSnapshotEvent` but add composition lifecycle where platform adapters support it.

- [ ] `UI/Controls/TextBoxBase.cs`
- [ ] `UI/Controls/TextBox.cs`
- [ ] `UI/Controls/PasswordBox.cs`
- [ ] `UI/Text/TextDocument.cs`
- [ ] `UI/Text/TextCaret.cs`
- [ ] `UI/Text/TextSelection.cs`
- [ ] `UI/Text/TextEditor.cs`
- [ ] `UI/Text/TextCompositionManager.cs`
- [ ] `UI/Text/TextCompositionState.cs`
- [ ] `UI/Text/UndoRedoStack.cs`
- [ ] `UI/Text/ClipboardAdapter.cs`
- [ ] `UI/Platform/ITextInputPlatform.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/TextBoxTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/PasswordBoxTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Text/TextEditorTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Text/TextCompositionManagerTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Text/UndoRedoStackTests.cs`

## 21. [Later] Accessibility and semantics

This phase makes UI meaning available to assistive technologies and testing tools. It should be designed around a platform-neutral semantics tree first, with platform adapters later.

- [ ] `UI/Accessibility/SemanticsNode.cs`
- [ ] `UI/Accessibility/SemanticsRole.cs`
- [ ] `UI/Accessibility/SemanticsProperty.cs`
- [ ] `UI/Accessibility/SemanticsTree.cs`
- [ ] `UI/Accessibility/SemanticsProvider.cs`
- [ ] `UI/Accessibility/AccessibleName.cs`
- [ ] `UI/Accessibility/AutomationPeer.cs` — only if WPF naming remains useful.
- [ ] `UI/Accessibility/ButtonAutomationPeer.cs`
- [ ] `UI/Accessibility/TextBoxAutomationPeer.cs`
- [ ] `UI/Accessibility/ItemsControlAutomationPeer.cs`
- [ ] `UI/Platform/IAccessibilityPlatform.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Accessibility/SemanticsTreeTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Accessibility/SemanticsProviderTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Accessibility/ButtonSemanticsTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Accessibility/TextBoxSemanticsTests.cs`

## 22. [Later] Advanced rendering and media

This phase expands drawing capabilities only when controls and scenarios require them. New media concepts must translate into `DrawCommand` extensions or clear backend abstractions instead of duplicating existing primitives.

- [ ] `UI/Drawing/DrawCommandKind.cs` — add new command kinds only with tests and backend support.
- [ ] `UI/Drawing/DrawingContext.cs` — add methods only when corresponding command kinds exist.
- [ ] `UI/Media/Brush.cs` — introduce when more than solid `DrawColor` is needed.
- [ ] `UI/Media/SolidColorBrush.cs` — may remain a thin wrapper over `DrawColor` only if it participates in styling/resource identity.
- [ ] `UI/Media/LinearGradientBrush.cs`
- [ ] `UI/Media/RadialGradientBrush.cs`
- [ ] `UI/Media/Pen.cs`
- [ ] `UI/Media/Geometry.cs`
- [ ] `UI/Media/RectangleGeometry.cs`
- [ ] `UI/Media/EllipseGeometry.cs`
- [ ] `UI/Media/PathGeometry.cs`
- [ ] `UI/Media/Transform.cs`
- [ ] `UI/Media/Matrix3x2.cs`
- [ ] `UI/Media/OpacityLayer.cs`
- [ ] `UI/Media/ShadowEffect.cs`
- [ ] `UI/Controls/Shapes/Shape.cs`
- [ ] `UI/Controls/Shapes/Rectangle.cs`
- [ ] `UI/Controls/Shapes/Ellipse.cs`
- [ ] `UI/Controls/Shapes/Path.cs`
- [ ] `UI/Media/ImageSource.cs` — only if it owns decoding, resource identity, caching, or metadata beyond `IDrawImage`.
- [ ] `UI/Media/BitmapImage.cs`
- [ ] `UI/Media/RenderTargetImage.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Drawing/AdvancedDrawCommandTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/BrushTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/GeometryTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/TransformTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Controls/Shapes/ShapeTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/ImageSourceTests.cs`

Acceptance checklist:

- [ ] Every new media abstraction has a responsibility not already covered by `DrawColor`, `DrawRect`, `DrawPoint`, `DrawTextRun`, or `IDrawImage`.
- [ ] Every new drawing command has backend tests or adapter coverage.
- [ ] Controls do not reference Skia, HarfBuzz, MonoGame, `SpriteBatch`, or `Texture2D`.

## 23. [Later] Animation and transitions

This phase adds time-based property changes after frame scheduling and invalidation are solid. Animation should be game-loop-native, explicit, and invalidate only affected properties.

- [ ] `UI/Animation/AnimationClock.cs`
- [ ] `UI/Animation/AnimationScheduler.cs`
- [ ] `UI/Animation/Animation.cs`
- [ ] `UI/Animation/Animation{T}.cs`
- [ ] `UI/Animation/Easing.cs`
- [ ] `UI/Animation/Transition.cs`
- [ ] `UI/Animation/Transition{T}.cs`
- [ ] `UI/Animation/Storyboard.cs` — only if composition of timelines is needed.
- [ ] `UI/Animation/AnimatedValueSource.cs`
- [ ] `UI/Styling/StyleTransition.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Animation/AnimationClockTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Animation/AnimationSchedulerTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Animation/TypedAnimationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Animation/TransitionTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Animation/AnimationInvalidationTests.cs`

Acceptance checklist:

- [ ] Animating a render-only property does not run layout.
- [ ] Animating a layout property enqueues layout only at ticks where the value changes.
- [ ] Completed animations release animated value source cleanly.

## 24. [Later] Platform boundaries and package shape

This phase keeps the core portable. The current repository has a single project with MonoGame dependencies; v2 should keep adapter code isolated and later decide whether to split packages.

- [ ] `UI/Platform/IPlatformServices.cs`
- [ ] `UI/Platform/IClipboard.cs`
- [ ] `UI/Platform/ICursorService.cs`
- [ ] `UI/Platform/IFileDialogService.cs`
- [ ] `UI/Platform/ITextInputPlatform.cs`
- [ ] `UI/Platform/IDpiProvider.cs`
- [ ] `UI/Hosting/MonoGame/` remains the only MonoGame UI host adapter folder.
- [ ] `Cerneala.Core.csproj` — optional future package split.
- [ ] `Cerneala.MonoGame.csproj` — optional future adapter package split.
- [ ] `Cerneala.Tests.Core.csproj` — optional future test split.
- [ ] `Cerneala.Tests.MonoGame.csproj` — optional future adapter test split.

Tests:

- [ ] `tests/Cerneala.Tests/UI/Platform/PlatformBoundaryTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Platform/ServiceRegistrationTests.cs`
- [ ] `tests/Cerneala.Tests/Architecture/MonoGameDependencyBoundaryTests.cs`

## 25. [Optional/Experimental] Markup, serialization, and source generation

This phase is optional. Cerneala should be code-first and strongly typed before any markup layer exists. Markup may become useful for tooling or designer workflows, but it should compile into typed object creation rather than becoming a reflection-heavy runtime requirement.

- [ ] `UI/Markup/UiMarkupDocument.cs`
- [ ] `UI/Markup/UiMarkupReader.cs`
- [ ] `UI/Markup/UiMarkupWriter.cs`
- [ ] `UI/Markup/UiMarkupSchema.cs`
- [ ] `UI/Markup/UiMarkupTypeRegistry.cs`
- [ ] `UI/Markup/UiFactory.cs`
- [ ] `UI/Markup/GeneratedUiFactory.cs`
- [ ] `UI/Markup/MarkupLoadOptions.cs`
- [ ] `UI/Markup/MarkupDiagnostic.cs`
- [ ] `UI/Markup/ContentPropertyAttribute.cs` — optional ergonomic hint.
- [ ] `UI/Markup/DesignTimeOnlyAttribute.cs`
- [ ] `Cerneala.SourceGen/UiMarkupGenerator.cs` — optional future project.
- [ ] `openspec/specs/markup-serialization/spec.md`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Markup/UiMarkupReaderTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Markup/UiMarkupWriterTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Markup/UiFactoryTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Markup/MarkupDiagnosticTests.cs`
- [ ] `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`

Acceptance checklist:

- [ ] Markup is not required to create controls.
- [ ] Markup does not bypass typed property validation.
- [ ] Generated factories produce retained trees that use the same invalidation and render-cache paths as code-created trees.

## 26. [Optional/Experimental] Advanced input categories

This phase turns existing metadata-only event categories into real behavior only when platform support and product scenarios require them.

- [~] `UI/Input/InputEvents.cs` already declares stylus, touch, manipulation, and drag/drop event metadata.
- [ ] `UI/Input/TouchInputBridge.cs`
- [ ] `UI/Input/StylusInputBridge.cs`
- [ ] `UI/Input/GestureRecognizer.cs`
- [ ] `UI/Input/ManipulationProcessor.cs`
- [ ] `UI/Input/DragDropController.cs`
- [ ] `UI/Input/DataTransfer.cs`
- [ ] `UI/Input/Cursor.cs`
- [ ] `UI/Input/CursorService.cs`
- [ ] `UI/Controls/InkCanvas.cs`
- [ ] `UI/Ink/Stroke.cs`
- [ ] `UI/Ink/StrokeCollection.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Input/TouchInputBridgeTests.cs`
- [ ] `tests/Cerneala.Tests/Input/StylusInputBridgeTests.cs`
- [ ] `tests/Cerneala.Tests/Input/GestureRecognizerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/ManipulationProcessorTests.cs`
- [ ] `tests/Cerneala.Tests/Input/DragDropControllerTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/InkCanvasTests.cs`

## 27. Implementation order

This order prioritizes a working retained UI loop before broad API coverage.

### MVP order

- [ ] 1. Add `openspec/README.md`, `openspec/project.md`, and specs for retained tree, invalidation, typed state, layout, render cache, and input bridge using the existing OpenSpec workspace.
- [ ] 2. Add `UI/Core/UiProperty{T}.cs`, metadata, store, registry, and property invalidation tests.
- [ ] 3. Add `UI/Elements/UIElement.cs`, `UIElementCollection`, `UIRoot`, element ids, lifecycle, and tree tests.
- [ ] 4. Add `InvalidationFlags`, dirty propagation, `LayoutQueue`, `RenderQueue`, `UiFrameScheduler`, and no-work-frame tests.
- [ ] 5. Add layout primitives, `LayoutManager`, `Panel`, `Canvas`, `StackPanel`, and layout cache tests.
- [ ] 6. Add `RenderContext`, `ElementRenderCache`, `RetainedRenderer`, root command-list cache, and retained render tests.
- [ ] 7. Add `UiHost` and `MonoGameUiHost` so update/draw uses retained frame scheduling.
- [ ] 8. Add hit testing, `ElementInputBridge`, hover/pressed state tracking, and focus manager MVP.
- [ ] 9. Add `CommandRouter`, `ActionCommand`, `ButtonBase.Command`, and command route tests.
- [ ] 10. Add `Control`, `Border`, `TextBlock`, `Button`, first panels, and acceptance tests.
- [ ] 11. Add text measurement/layout cache services above existing Skia/HarfBuzz text pipeline.
- [ ] 12. Add resource dependency tracking for fonts/images.
- [ ] 13. Update playground to show retained UI plus invalidation stats.

### Core order

- [ ] 14. Add styling/theme engine with typed setters and pseudo-class rules.
- [ ] 15. Add code-first templates and presenters.
- [ ] 16. Add scrolling/range controls.
- [ ] 17. Add items, selection, and virtualization.
- [ ] 18. Add typed data observation and binding-light APIs.
- [ ] 19. Add diagnostics/devtools overlays and tree/cache dumpers.

### Later order

- [ ] 20. Add text editing and IME composition.
- [ ] 21. Add accessibility semantics and platform adapters.
- [ ] 22. Add advanced rendering/media primitives as scenarios require.
- [ ] 23. Add animation and transitions.
- [ ] 24. Decide package/platform split.

### Optional/Experimental order

- [ ] 25. Prototype markup/serialization after templates and typed properties are stable.
- [ ] 26. Prototype source generation if runtime reflection becomes a real cost.
- [ ] 27. Implement touch/stylus/drag/drop behavior when platform adapters can supply real data.

## 28. Risks and decisions needing human confirmation

These are explicit decision points to resolve before or during implementation. Record the decision in this file and the matching `openspec` spec when confirmed.

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

## 29. MVP completion definition

MVP is complete when Cerneala can run a retained UI sample inside the MonoGame playground with deterministic tests proving invalidation-driven behavior.

- [ ] `MonoGameUiHost` exists and is used by `Playground/Cerneala.Playground/Game1.cs`.
- [ ] `UIRoot` retains a tree containing `StackPanel`, `Border`, `TextBlock`, and `Button`.
- [ ] Layout runs on first frame and when layout-affecting state changes.
- [ ] Rendering commands are regenerated only when render-affecting state changes.
- [ ] `IDrawingBackend.Render` can be called every draw frame with cached commands.
- [ ] Existing `UI/Drawing` tests still pass.
- [ ] Existing `UI/Input` tests still pass.
- [ ] New retained no-work-frame tests pass.
- [ ] Hover, press, focus, and command execution work through retained input routing.
- [ ] Text rendering uses existing `DrawTextRun`, `SkiaTextShaper`, and `SkiaTextRasterizer` through higher-level services.
- [ ] No UI core control directly references MonoGame, Skia, HarfBuzz, `SpriteBatch`, or `Texture2D`.
