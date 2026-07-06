# Observable ItemsSource And Recycling Stability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Let `ItemsControl`/`ListBox` consume observable item sources directly and keep realized containers stable across add/remove/replace/move/scroll operations. The current retained list path is good for local `Items`, but authoring preview needs real data-driven list mutation without copying the whole source every time.

**Architecture:** Build on existing `ObservableList<T>`, `CollectionView<T>`, `ItemsControl`, `ItemContainerGenerator`, `ItemContainerRecyclePool`, `ItemsPresenter`, `ListBox`, `SelectionModel`, and `VirtualizingStackPanel`. Do not add full WPF collection views, `DataContext`, or reflection binding. Use explicit observable collection contracts and retained invalidation.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Data`, `UI/Controls`, `UI/Layout/Virtualization`, retained scheduler.

---

## File Structure

- Modify: `UI/Data/IObservableList{T}.cs`
  - Add a non-generic observable list bridge if needed for `ItemsControl.ItemsSource` subscriptions.
- Modify: `UI/Data/ObservableList{T}.cs`
  - Implement the non-generic bridge while preserving existing typed events/tests.
- Modify: `UI/Data/CollectionView{T}.cs`
  - Implement the same observable bridge if useful.
- Modify: `UI/Controls/ItemsControl.cs`
  - Add `ItemsSourceProperty` and source subscription/disposal.
- Modify: `UI/Controls/ItemCollection.cs`
  - Keep local items mode working.
- Modify: `UI/Controls/ItemContainerGenerator.cs`
  - Read items through a source abstraction and keep realized containers stable.
- Modify: `UI/Controls/ItemsPresenter.cs`
  - Refresh only when source/window/panel/template state changes.
- Modify: `UI/Controls/ListBox.cs`
  - Preserve selection across observable source mutations where identity allows.
- Create: `tests/Cerneala.Tests/Controls/ItemsSourceObservableTests.cs`
- Create: `tests/Cerneala.Tests/Controls/ItemsControlRecyclingStabilityTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/ObservableListAuthoringSliceTests.cs`

## Important Existing Behavior

- `ItemsControl.Items` is a retained `ItemCollection`.
- `ItemsControl.SetItems(IEnumerable?)` copies items into `ItemCollection`.
- `ObservableList<T>` publishes typed change events.
- `CollectionView<T>` listens to typed `IObservableList<T>` and raises reset-style changes.
- `ItemContainerGenerator` realizes containers by index and recycles unrealized containers.
- `ItemsPresenter` rebuilds panel children when `itemsDirty` or realization window changes.
- Core Preview tests already prove manual list mutation and scroll invalidation.

Target behavior:

- `ItemsControl.ItemsSource` accepts `IEnumerable?`.
- If source implements the Cerneala observable list bridge, changes invalidate retained list work automatically.
- Setting `ItemsSource` subscribes; replacing/clearing it unsubscribes.
- Local `Items` mode still works when `ItemsSource` is null.
- Add/remove/replace/move operations keep unrelated realized containers stable where practical.
- Virtualized lists do not create offscreen containers after observable source changes.

## Rules

- [ ] Do not add WPF `ItemsSource` compatibility quirks beyond the simple property name.
- [ ] Do not add `DataContext`.
- [ ] Do not add string-path display member binding.
- [ ] Do not add a full collection-view/current-item stack.
- [ ] Do not rebuild all realized containers for a single replace if the container type is compatible.
- [ ] Do not create offscreen containers in virtualized mode.
- [ ] Keep local `Items` and `ItemsSource` precedence explicit and tested.

---

### Task 1: Add RED Observable ItemsSource Tests

**Files:**
- Create: `tests/Cerneala.Tests/Controls/ItemsSourceObservableTests.cs`
- Create: `tests/Cerneala.Tests/Controls/ItemsControlRecyclingStabilityTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/ObservableListAuthoringSliceTests.cs`

- [ ] **Step 1: Add ItemsSource subscription tests**

Create tests:

```csharp
ItemsControlItemsSourceInitializesContainersFromObservableList()
ObservableItemsSourceAddInvalidatesMeasureArrangeRenderHitTest()
ReplacingItemsSourceUnsubscribesOldSource()
ClearingItemsSourceReturnsToLocalItemsMode()
ItemsSourceSetToSameInstanceDoesNotResubscribeOrInvalidate()
```

- [ ] **Step 2: Add recycling stability tests**

Create tests:

```csharp
ObservableReplaceReusesCompatibleRealizedContainerAtIndex()
ObservableAddKeepsUnchangedRealizedContainerIdentities()
ObservableRemoveRecyclesRemovedContainerAndUpdatesFollowingIndexes()
ObservableMovePreservesMovedContainerWhenStillRealized()
ObservableResetClearsRealizedContainersAndRecyclePoolPredictably()
```

Use `ItemContainerGenerator.RealizedContainers` and `ItemContainerGenerator.GetItemIndex(...)` for assertions.

- [ ] **Step 3: Add virtualization tests**

Create tests:

```csharp
VirtualizedObservableItemsSourceRealizesOnlyVisibleWindowAfterAdd()
VirtualizedObservableItemsSourceRealizesOnlyVisibleWindowAfterScroll()
VirtualizedObservableItemsSourceDoesNotCreateOffscreenTemplateChildren()
```

- [ ] **Step 4: Add authoring slice test**

Create:

```csharp
ObservableListMutationUpdatesRetainedListWithoutSecondFrameWork()
```

Use `UiHost.Update(...)`, mutate an observable list, assert first changed frame has retained work and the next unchanged frame has no retained work.

- [ ] **Step 5: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ItemsSourceObservableTests|FullyQualifiedName~ItemsControlRecyclingStabilityTests|FullyQualifiedName~ObservableListAuthoringSliceTests"
```

Expected: RED because `ItemsSource` direct observable subscription/stability contract is not implemented.

- [ ] **Step 6: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Controls\ItemsSourceObservableTests.cs tests\Cerneala.Tests\Controls\ItemsControlRecyclingStabilityTests.cs tests\Cerneala.Tests\UI\Hosting\ObservableListAuthoringSliceTests.cs
git commit -m "test: capture observable items source retained contract"
```

---

### Task 2: Add A Non-Generic Observable List Bridge

**Files:**
- Modify: `UI/Data/IObservableList{T}.cs`
- Modify: `UI/Data/ObservableList{T}.cs`
- Modify: `UI/Data/CollectionView{T}.cs`

- [ ] **Step 1: Add non-generic event contract**

Add a small non-generic interface, for example:

```csharp
public interface IObservableList : IEnumerable
{
    event EventHandler<ObservableListChangedEventArgs>? Changed;
    int Count { get; }
    object? this[int index] { get; }
}
```

Use a non-generic event args type that carries:

- `Kind`
- `Index`
- `OldIndex`
- `Item`
- `OldItem`
- `Items`
- `OldItems`

Keep existing `IObservableList<T>` and typed args working.

- [ ] **Step 2: Implement bridge in `ObservableList<T>`**

Raise both typed and untyped events from the same mutation path.

- [ ] **Step 3: Implement bridge in `CollectionView<T>` if useful**

`CollectionView<T>` can raise reset events through the bridge. Do not overbuild incremental sorted/filter delta logic.

---

### Task 3: Add `ItemsControl.ItemsSource`

**Files:**
- Modify: `UI/Controls/ItemsControl.cs`
- Modify: `UI/Controls/ItemCollection.cs`

- [ ] **Step 1: Add `ItemsSourceProperty`**

Register:

```text
ItemsSource: IEnumerable?
Options: AffectsMeasure | AffectsArrange | AffectsRender | AffectsHitTest
```

- [ ] **Step 2: Subscribe/unsubscribe observable sources**

When `ItemsSource` changes:

- unsubscribe old observable source;
- subscribe new source if it implements the non-generic bridge;
- clear realized containers;
- mark presenter dirty;
- invalidate retained list work.

- [ ] **Step 3: Keep local items mode explicit**

When `ItemsSource` is not null, generator reads from source. Local `Items` can remain populated but should not be used until `ItemsSource` is cleared.

Document/test this behavior.

- [ ] **Step 4: Ensure detach unsubscribes**

If `ItemsControl` is detached/disposed through retained tree lifecycle, it should not leak source subscriptions.

---

### Task 4: Stabilize Generator And Presenter For Source Changes

**Files:**
- Modify: `UI/Controls/ItemContainerGenerator.cs`
- Modify: `UI/Controls/ItemsPresenter.cs`
- Modify: `UI/Controls/ListBox.cs`

- [ ] **Step 1: Add owner item accessors**

Centralize item count/index access in `ItemsControl`:

```text
ItemCount
GetItemAt(index)
IndexOfItem(item) if needed for selection preservation
```

`ItemContainerGenerator` should not care whether items come from local `Items` or `ItemsSource`.

- [ ] **Step 2: Handle replace without losing container identity**

If an item at an already-realized index changes and the container type remains compatible, reuse the container and re-run `PrepareItemContainer(...)`.

- [ ] **Step 3: Handle add/remove/move conservatively**

For add/remove/move, it is acceptable to recycle affected range, but do not destroy unrelated realized containers outside the affected range/window.

- [ ] **Step 4: Preserve selection by item identity where possible**

For `ListBox`, if selected item identity remains present after move/insert/remove, selection should track the item. If only index-based selection exists today, add the smallest explicit item-based preservation needed by tests.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted items tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ItemsSourceObservableTests|FullyQualifiedName~ItemsControlRecyclingStabilityTests|FullyQualifiedName~ObservableListAuthoringSliceTests|FullyQualifiedName~ItemsControlTests|FullyQualifiedName~ItemContainerGeneratorTests|FullyQualifiedName~ListBoxTests"
```

Expected: GREEN.

- [ ] **Step 2: Run virtualization/core preview tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~VirtualizingStackPanelTests|FullyQualifiedName~RetainedListScrollVerticalSliceTests|FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Data UI\Controls tests\Cerneala.Tests
git commit -m "feat: add observable items source retained contract"
```
