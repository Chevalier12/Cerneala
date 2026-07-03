## 1. Item Data And ItemsControl

- [x] 1.1 Create `UI/Controls/ItemCollection.cs`.
- [x] 1.2 Implement retained ordered item storage with count, indexer, enumeration, add/remove/clear, and replacement behavior.
- [x] 1.3 Create `UI/Controls/ItemsControl.cs`.
- [x] 1.4 Add typed item presentation properties for items, item template, and items panel.
- [x] 1.5 Ensure `ItemsControl` owns an `ItemContainerGenerator` and invalidates retained layout/render when item policy changes.
- [x] 1.6 Preserve existing `ItemsPresenter` non-virtualized behavior for simple item presentation.
- [x] 1.7 Add `tests/Cerneala.Tests/Controls/ItemsControlTests.cs`.

## 2. Container Generation And Recycling

- [x] 2.1 Create `UI/Controls/ItemContainerGenerator.cs`.
- [x] 2.2 Create `UI/Controls/ItemContainerRecyclePool.cs`.
- [x] 2.3 Generate containers for realized item indexes.
- [x] 2.4 Preserve container identity while an item remains realized.
- [x] 2.5 Detach unrealized containers and place compatible containers in the recycle pool.
- [x] 2.6 Prepare recycled containers with current item, index, content template, and selection state.
- [x] 2.7 Clear stale item/index/selection state before recycled containers are reused.
- [x] 2.8 Add `tests/Cerneala.Tests/Controls/ItemContainerGeneratorTests.cs`.
- [x] 2.9 Add `tests/Cerneala.Tests/Controls/ItemContainerRecyclePoolTests.cs`.

## 3. ItemsPresenter Integration

- [x] 3.1 Update `UI/Controls/ItemsPresenter.cs` to consume `ItemsControl` generation state.
- [x] 3.2 Host generated containers under the retained panel root.
- [x] 3.3 Support non-virtualized realization of all items when no virtualization context is active.
- [x] 3.4 Preserve unrelated realized containers when item data changes outside the realized range.
- [x] 3.5 Keep data template and items panel template behavior compatible with existing tests.

## 4. Selection Models

- [x] 4.1 Create `UI/Controls/SelectionModel.cs`.
- [x] 4.2 Create `UI/Controls/SelectionModel{T}.cs`.
- [x] 4.3 Implement single-selection index tracking.
- [x] 4.4 Implement typed selected item access in `SelectionModel<T>`.
- [x] 4.5 Ensure selection state survives container virtualization and recycling.
- [x] 4.6 Add change notifications or explicit result state sufficient for controls to invalidate affected containers only.
- [x] 4.7 Add `tests/Cerneala.Tests/Controls/SelectionModelTests.cs`.

## 5. Selector Primitive

- [x] 5.1 Create `UI/Controls/Primitives/Selector.cs`.
- [x] 5.2 Derive selector behavior from `ItemsControl`.
- [x] 5.3 Connect retained click input on item containers to selection model updates.
- [x] 5.4 Invalidate visual state only for old and new selected realized containers.
- [x] 5.5 Reapply selected state when selected items are realized after virtualization.
- [x] 5.6 Add `tests/Cerneala.Tests/Controls/Primitives/SelectorTests.cs`.

## 6. ListBox

- [x] 6.1 Create `UI/Controls/ListBox.cs`.
- [x] 6.2 Create `UI/Controls/ListBoxItem.cs`.
- [x] 6.3 Generate `ListBoxItem` containers for list box items unless an item is already a compatible container.
- [x] 6.4 Add typed selected state to `ListBoxItem`.
- [x] 6.5 Ensure retained click input selects the clicked list box item.
- [x] 6.6 Ensure list box selection state survives container recycling.
- [x] 6.7 Add `tests/Cerneala.Tests/Controls/ListBoxTests.cs`.

## 7. ComboBox And Tabs

- [x] 7.1 Create `UI/Controls/ComboBox.cs`.
- [x] 7.2 Expose combo box selected item and selected index through shared selector behavior.
- [x] 7.3 Ensure combo box realized item containers participate in retained layout, rendering, hit testing, and input routing.
- [x] 7.4 Create `UI/Controls/TabControl.cs`.
- [x] 7.5 Create `UI/Controls/TabItem.cs`.
- [x] 7.6 Use retained selection state to select tab items.
- [x] 7.7 Ensure selected tab item content participates in retained layout and rendering.
- [x] 7.8 Add `tests/Cerneala.Tests/Controls/ComboBoxTests.cs`.
- [x] 7.9 Add `tests/Cerneala.Tests/Controls/TabControlTests.cs`.
- [x] 7.10 Add `tests/Cerneala.Tests/Controls/TabItemTests.cs`.

## 8. Virtualization Primitives

- [x] 8.1 Create `UI/Layout/Virtualization/VirtualizationContext.cs`.
- [x] 8.2 Create `UI/Layout/Virtualization/RealizationWindow.cs`.
- [x] 8.3 Compute deterministic realized index ranges from item count, item extent, viewport size, scroll offset, and cache length.
- [x] 8.4 Create `UI/Layout/Panels/VirtualizingStackPanel.cs`.
- [x] 8.5 Measure only realized children in `VirtualizingStackPanel`.
- [x] 8.6 Arrange realized children at deterministic scroll-adjusted positions.
- [x] 8.7 Report total extent for scroll integration.
- [x] 8.8 Add `tests/Cerneala.Tests/UI/Layout/VirtualizingStackPanelTests.cs`.
- [x] 8.9 Add `tests/Cerneala.Tests/UI/Layout/VirtualizationTests.cs`.

## 9. Scroll-Driven Realization

- [x] 9.1 Connect items presentation to existing `ScrollViewer`, `ScrollContentPresenter`, and `IScrollInfo` state.
- [x] 9.2 Update realization windows when scroll offset changes.
- [x] 9.3 Recycle containers that leave the realization window.
- [x] 9.4 Realize containers that enter the realization window.
- [x] 9.5 Reuse realized containers when scrolling stays inside the same realization range.
- [x] 9.6 Prove large data sets retain only realized containers plus configured cache.

## 10. Integration And Boundaries

- [x] 10.1 Prove generated containers participate in retained layout, rendering, hit testing, routed input, styling, and invalidation.
- [x] 10.2 Prove data updates do not rebuild unrelated realized containers.
- [x] 10.3 Prove selection changes invalidate selected containers only.
- [x] 10.4 Extend architecture boundary tests proving section 17 controls/layout avoid MonoGame, Skia, HarfBuzz, `Texture2D`, and `SpriteBatch`.
- [x] 10.5 Preserve existing item presenter, template, scrolling, layout, input, styling, and rendering tests.

## 11. Roadmap And Validation

- [x] 11.1 Update `ROADMAPv2.md` section 17 file checklist as files and tests are completed.
- [x] 11.2 Update `ROADMAPv2.md` section 17 acceptance checklist as behaviors are completed.
- [x] 11.3 Verify `openspec validate add-items-selection-virtualization --strict` passes.
- [x] 11.4 Verify `openspec validate --all --strict` passes.
- [x] 11.5 Verify `dotnet build Cerneala.slnx -warnaserror` passes.
- [x] 11.6 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.
