# Items Scroll Data Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Prove `ItemsControl`/`ListBox`, item mutation, selection, virtualization, and `ScrollViewer` participate correctly in retained invalidation. The retained app sample should stop using a manual `StackPanel` list as its primary list proof.

**Architecture:** Use existing controls and data primitives. Do not add full data binding, collection views, grouping/sorting, or a new virtualization architecture. Fix only bugs needed to make retained list/scroll behavior coherent.

**Tech Stack:** C#/.NET 8, xUnit, existing `ItemsControl`, `ListBox`, `ItemsPresenter`, `ItemContainerGenerator`, `VirtualizingStackPanel`, `ScrollViewer`, `ObservableList<T>`.

---

## File Structure

- Modify: `UI/Controls/ItemsControl.cs`
  - Ensure item changes invalidate measure/arrange/render/hit-test correctly.
- Modify: `UI/Controls/ItemsPresenter.cs`
  - Avoid unnecessary panel/container rebuilds on unchanged frames.
- Modify: `UI/Controls/ItemContainerGenerator.cs`
  - Preserve/reuse containers where current architecture supports it.
- Modify: `UI/Controls/ListBox.cs`
  - Ensure selection invalidates old/new realized containers only.
- Modify: `UI/Controls/Primitives/Selector.cs`
  - Narrow selection invalidation if tests expose broad invalidation.
- Modify: `UI/Controls/ScrollViewer.cs`
  - Ensure wheel scroll invalidates arrange/render/hit-test, not full measure unless needed.
- Modify: `UI/Controls/ScrollContentPresenter.cs`
  - Keep offset invalidation narrow.
- Modify: `UI/Layout/Panels/VirtualizingStackPanel.cs`
  - Ensure realized window and bounds are deterministic.
- Modify: `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`
  - Use `ItemsControl` or `ListBox` for list section.
- Create: `tests/Cerneala.Tests/UI/Hosting/RetainedListScrollVerticalSliceTests.cs`
- Create: `tests/Cerneala.Tests/Controls/ItemsControlRetainedInvalidationTests.cs`

## Important Existing Behavior

`RetainedAppSample.BuildListCard()` currently creates rows manually in a `StackPanel`. That bypasses the retained list architecture already present in `ItemsControl`, `ListBox`, `ItemsPresenter`, `ItemContainerGenerator`, `VirtualizingStackPanel`, `ScrollViewer`, and `ObservableList<T>`.

Target behavior:

- Adding/removing item invalidates measure/arrange/render/hit-test.
- Second unchanged frame after list realization does no retained work.
- Scrolling invalidates arrange/render/hit-test without full measure where possible.
- Selection invalidates old/new realized containers, not entire tree.
- Virtualized list realizes only visible window + configured cache.
- Retained sample uses `ItemsControl` or `ListBox` in list section.

## Rules

- [ ] Do not build full data binding.
- [ ] Do not build collection views/grouping/sorting.
- [ ] Do not rewrite item container generator from scratch unless a test proves it is fundamentally broken.
- [ ] Do not add new list controls.
- [ ] Do not make virtualization perfect; make it deterministic and tested.

---

### Task 1: Add RED Items/Scroll Retained Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Hosting/RetainedListScrollVerticalSliceTests.cs`
- Create: `tests/Cerneala.Tests/Controls/ItemsControlRetainedInvalidationTests.cs`

- [ ] **Step 1: Add ItemsControl tests**

Create tests:

```csharp
ItemsControlItemAddInvalidatesMeasureArrangeRenderAndHitTest()
ItemsControlSecondUnchangedFrameDoesNoRetainedWork()
ListBoxSelectionInvalidatesOnlyOldAndNewRealizedContainers()
VirtualizedItemsControlRealizesOnlyVisibleWindow()
```

Test intent:

- Use existing `ItemsControl.Items.Add(...)` or `SetItems(...)` APIs.
- Process first frame, unchanged frame, then mutation frame.
- Assert retained work is present/absent as expected.
- Assert selection invalidates only old/new realized containers where possible.
- Assert virtualization realization window/count is deterministic.

- [ ] **Step 2: Add ScrollViewer/sample tests**

Create tests:

```csharp
ScrollViewerWheelOffsetInvalidatesArrangeRenderHitTestWithoutMeasure()
RetainedAppSampleUsesItemsControlOrListBoxForListSection()
RetainedListScrollSecondUnchangedFrameDoesNoRetainedWork()
```

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedListScrollVerticalSliceTests|FullyQualifiedName~ItemsControlRetainedInvalidationTests"
```

Expected: RED because sample still uses manual list and list/scroll invalidation may be too broad or unstable.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\RetainedListScrollVerticalSliceTests.cs tests\Cerneala.Tests\Controls\ItemsControlRetainedInvalidationTests.cs
git commit -m "test: capture retained items scroll vertical slice"
```

---

### Task 2: Stabilize ItemsPresenter And Container Realization

**Files:**
- Modify: `UI/Controls/ItemsControl.cs`
- Modify: `UI/Controls/ItemsPresenter.cs`
- Modify: `UI/Controls/ItemContainerGenerator.cs`

- [ ] **Step 1: Avoid unchanged-frame rebuilds**

`ItemsPresenter.RefreshItems()` must not rebuild panel/container tree when `itemsDirty == false`, realization window unchanged, and item collection unchanged.

- [ ] **Step 2: Keep item invalidation flags correct**

Item add/remove should invalidate:

```csharp
Measure | Arrange | Render | HitTest
```

Do not invalidate style/resource unless item/template actually changed them.

- [ ] **Step 3: Preserve recycling behavior**

Fix existing generator bugs locally. Do not replace virtualization/recycling architecture.

---

### Task 3: Stabilize Selection And Scroll Invalidation

**Files:**
- Modify: `UI/Controls/ListBox.cs`
- Modify: `UI/Controls/Primitives/Selector.cs`
- Modify: `UI/Controls/ScrollViewer.cs`
- Modify: `UI/Controls/ScrollContentPresenter.cs`
- Modify: `UI/Layout/Panels/VirtualizingStackPanel.cs`

- [ ] **Step 1: Keep selection invalidation narrow**

Old/new realized containers should get render/input visual invalidation. Avoid full-list invalidation unless item realization really changes.

- [ ] **Step 2: Keep scroll invalidation narrow**

Wheel scroll should change offset and invalidate arrange/render/hit-test. It should not trigger measure unless extent/viewport/scrollbar visibility changed.

- [ ] **Step 3: Make virtualization deterministic**

Known item extent + viewport + offset should produce a stable `RealizationWindow` and stable realized count.

---

### Task 4: Update RetainedAppSample To Use ItemsControl/ListBox

**Files:**
- Modify: `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`

- [ ] **Step 1: Replace manual `StackPanel` list**

Use `ListBox` or `ItemsControl` inside `ScrollViewer`. Use `ItemTemplate` if needed to render rows with existing `PlaygroundText` resources. Do not build data binding.

- [ ] **Step 2: Preserve retained sample contract**

Existing tests for unchanged frame, command mutation, font resource mutation, and draw purity must still pass.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run items/scroll tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedListScrollVerticalSliceTests|FullyQualifiedName~ItemsControlRetainedInvalidationTests|FullyQualifiedName~ItemsControlTests|FullyQualifiedName~ListBoxTests|FullyQualifiedName~ScrollViewerTests"
```

Expected: GREEN.

- [ ] **Step 2: Run retained sample tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedAppSampleContractTests|FullyQualifiedName~RetainedVerticalSliceTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Controls UI\Layout\Panels Playground\Cerneala.Playground tests\Cerneala.Tests\UI\Hosting\RetainedListScrollVerticalSliceTests.cs tests\Cerneala.Tests\Controls\ItemsControlRetainedInvalidationTests.cs
git commit -m "feat: prove retained items scroll vertical slice"
```
