## 1. Roadmap And Contract Alignment

- [x] 1.1 Update `ROADMAPv2.md` section 5 planning checklist; done when proposal, design, specs, tasks, and validation entries for `add-layout-system` are visible and accurately checked.
- [x] 1.2 Keep `Grid`, `GridLength`, `ColumnDefinition`, and `RowDefinition` unchecked and out of this change unless implementation proves they are required for MVP.
- [x] 1.3 Keep section 5 scoped to layout; done when retained rendering, host integration, input bridge, real hit-test service, styling, and markup remain assigned to later roadmap sections.

## 2. Layout Primitives And Policies

- [x] 2.1 Add `UI/Layout/LayoutSize.cs`; done when finite and unconstrained dimensions are represented explicitly and comparable in tests.
- [x] 2.2 Add `UI/Layout/LayoutPoint.cs`; done when layout coordinates are represented separately from drawing points.
- [x] 2.3 Add `UI/Layout/LayoutRect.cs`; done when layout slots store position and size separately from drawing rectangles.
- [x] 2.4 Add `UI/Layout/Thickness.cs`; done when left, top, right, and bottom edge values are represented and total horizontal/vertical helpers exist.
- [x] 2.5 Add `UI/Layout/Alignment.cs`; done when horizontal and vertical alignment values are represented without WPF compatibility baggage.
- [x] 2.6 Add `UI/Layout/Visibility.cs`; done when `Visible`, `Hidden`, and `Collapsed` semantics are available.
- [x] 2.7 Add `UI/Layout/LayoutRounding.cs`; done when rounding policy has an explicit MVP default and deterministic tests.
- [x] 2.8 Add `UI/Layout/MeasureContext.cs`; done when measure operations receive available size and layout policy.
- [x] 2.9 Add `UI/Layout/ArrangeContext.cs`; done when arrange operations receive final rect and layout policy.
- [x] 2.10 Add `UI/Layout/LayoutResult.cs`; done when measure/arrange processing can report desired size, arranged bounds, cache reuse, and invalidation effects.

## 3. Element And Root Layout Integration

- [x] 3.1 Update `UI/Elements/UIElement.cs`; done when elements expose desired size, arranged bounds, layout version, margin, alignment, visibility, layout boundary state, and protected measure/arrange hooks.
- [x] 3.2 Update `UI/Elements/UIRoot.cs`; done when roots own layout management and expose viewport constraints to layout processing.
- [x] 3.3 Add `UI/Layout/ILayoutElement.cs`; done when layout-capable retained elements have an explicit contract without depending on rendering.
- [x] 3.4 Add `UI/Layout/LayoutBoundary.cs`; done when roots/subtrees can stop upward layout propagation.
- [x] 3.5 Connect layout-affecting property metadata to retained invalidation; done when equal effective values do not invalidate layout and changed layout values do.

## 4. Layout Manager And Scheduler Integration

- [x] 4.1 Add `UI/Layout/LayoutManager.cs`; done when it consumes `LayoutQueue`, processes measure before arrange, and updates element layout state.
- [x] 4.2 Add measure caching; done when repeated measure with the same available size and unchanged layout version reuses cached desired size.
- [x] 4.3 Add arrange caching; done when repeated arrange with the same final rect and unchanged layout version reuses cached arranged bounds.
- [x] 4.4 Integrate `LayoutManager` with `UI/Invalidation/UiFrameScheduler.cs`; done when scheduler measure/arrange phases can delegate to layout manager behavior.
- [x] 4.5 Schedule render and hit-test invalidation after changed arranged bounds; done when layout output changes do not force another measure pass but do enqueue downstream work.
- [x] 4.6 Preserve failure behavior; done when failed measure/arrange processing keeps matching dirty flags and queued work.

## 5. Panels

- [x] 5.1 Add `UI/Layout/Panels/Panel.cs`; done when panel measure/arrange behavior can iterate retained visual children.
- [x] 5.2 Add `UI/Layout/Panels/Canvas.cs`; done when absolute child placement is supported for MVP.
- [x] 5.3 Add `UI/Layout/Panels/StackPanel.cs`; done when vertical and horizontal stacking measure and arrange children deterministically.
- [x] 5.4 Add `UI/Layout/Panels/Orientation.cs`; done when stack orientation is explicit.
- [x] 5.5 Leave `UI/Layout/Panels/Grid.cs`, `GridLength.cs`, `ColumnDefinition.cs`, and `RowDefinition.cs` unimplemented in this change unless a documented MVP need appears.

## 6. Tests

- [x] 6.1 Add `tests/Cerneala.Tests/UI/Layout/LayoutPrimitiveTests.cs`; done when layout size, point, rect, thickness, alignment, visibility, and rounding primitives are covered.
- [x] 6.2 Add `tests/Cerneala.Tests/UI/Layout/LayoutManagerTests.cs`; done when queue processing, measure-before-arrange order, caching, and failure behavior are covered.
- [x] 6.3 Add `tests/Cerneala.Tests/UI/Layout/UIElementMeasureArrangeTests.cs`; done when element desired size, arranged bounds, layout versioning, and base hooks are covered.
- [x] 6.4 Add `tests/Cerneala.Tests/UI/Layout/LayoutInvalidationTests.cs`; done when no-op property sets, changed layout properties, boundary propagation, and downstream render/hit-test invalidation are covered.
- [x] 6.5 Add `tests/Cerneala.Tests/UI/Layout/VisibilityTests.cs`; done when `Visible`, `Hidden`, and `Collapsed` layout behavior is covered.
- [x] 6.6 Add `tests/Cerneala.Tests/UI/Layout/CanvasTests.cs`; done when canvas measure and coordinate arrange behavior are covered.
- [x] 6.7 Add `tests/Cerneala.Tests/UI/Layout/StackPanelTests.cs`; done when vertical/horizontal measure and arrange behavior are covered.
- [x] 6.8 Do not add `tests/Cerneala.Tests/UI/Layout/GridTests.cs` in this change unless Grid is explicitly promoted into scope.

## 7. Architecture Boundaries

- [x] 7.1 Verify `UI/Layout` does not reference `UI/Drawing` command types; done when layout geometry is independent from `DrawPoint`, `DrawRect`, `DrawingContext`, and `DrawCommandList`.
- [x] 7.2 Verify `UI/Layout` and updated retained element code do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backends.
- [x] 7.3 Verify layout output is only exposed as layout state for later rendering/hit-test phases, not as generated draw commands.

## 8. Validation

- [x] 8.1 Run `dotnet test`; done when the full test suite passes.
- [x] 8.2 Run `openspec validate add-layout-system --strict`; done when the change validates successfully.
- [x] 8.3 Run `openspec validate --all --strict`; done when active changes and main specs validate successfully.
- [x] 8.4 Review `git status --short`; done when changed files are understood and unrelated edits such as `ConceptualIdeas.md` are not included accidentally.
