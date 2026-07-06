# Harden Layout Authoring Mutation Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make code-first layout mutation reliable. Grid is a core authoring primitive, but its `ColumnDefinitions` and `RowDefinitions` are plain lists today. Adding/removing definitions or changing definition sizes after a frame can bypass retained invalidation. Developer Preview needs these mutations to invalidate layout/render/hit-test deterministically.

**Architecture:** Keep the existing Grid layout algorithm. Add the smallest owner-aware definition collection and definition-change notification needed to make mutations retained/invalidation-safe. Do not build a full WPF Grid clone, attached property engine, shared-size groups, min/max constraints, or layout designer surface.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Layout/Panels/Grid`, retained invalidation, layout diagnostics.

---

## File Structure

- Create: `UI/Layout/Panels/GridDefinitionCollection{TDefinition}.cs`
  - Owner-aware collection that invalidates the owning grid on add/remove/clear/replace.
- Modify: `UI/Layout/Panels/Grid.cs`
  - Use definition collections instead of raw `List<T>` while preserving public authoring ergonomics.
- Modify: `UI/Layout/Panels/ColumnDefinition.cs`
  - Notify owner/collection when `Width` changes.
- Modify: `UI/Layout/Panels/RowDefinition.cs`
  - Notify owner/collection when `Height` changes.
- Modify only if needed: `UI/Layout/Panels/GridLength.cs`
- Create: `tests/Cerneala.Tests/UI/Layout/GridDefinitionMutationTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/GridAuthoringFrameContractTests.cs`

## Important Existing Behavior

- `Grid.ColumnDefinitions` and `Grid.RowDefinitions` are currently mutable `List<T>` instances.
- `Grid.SetRow/SetColumn/SetRowSpan/SetColumnSpan(...)` already invalidates parent grid when child placement changes.
- `ColumnDefinition.Width` and `RowDefinition.Height` validate values but do not notify a grid.
- Existing Grid tests cover measure/arrange behavior, but not retained mutation after first frame.

Target behavior:

- Adding/removing/clearing/replacing row or column definitions invalidates measure/arrange/render/hit-test for the grid.
- Changing a definition's `Width`/`Height` invalidates the owning grid exactly once when value changes.
- Setting the same definition value does not create retained work.
- Mutating definitions while detached marks dirty state and processes correctly after attach.
- Existing `ColumnDefinitions.Add(new(...))` and `RowDefinitions.Add(new(...))` authoring remains simple.
- The Grid layout algorithm remains small and current tests continue to pass.

## Rules

- [ ] Do not implement WPF SharedSizeGroup.
- [ ] Do not add min/max row/column constraints in this plan.
- [ ] Do not add `GridSplitter`.
- [ ] Do not redesign attached property storage.
- [ ] Do not change StackPanel/Canvas behavior unless a regression test proves they are affected.
- [ ] Do not add a layout designer API.

---

### Task 1: Add RED Grid Mutation Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Layout/GridDefinitionMutationTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/GridAuthoringFrameContractTests.cs`

- [ ] **Step 1: Add definition collection mutation tests**

Create tests:

```csharp
AddingColumnDefinitionAfterFirstFrameInvalidatesGridMeasureArrangeRenderAndHitTest()
AddingRowDefinitionAfterFirstFrameInvalidatesGridMeasureArrangeRenderAndHitTest()
RemovingDefinitionInvalidatesGridAndClampsExistingPlacementsSafely()
ClearingDefinitionsReturnsGridToSingleStarCellAndInvalidates()
ReplacingDefinitionInvalidatesExactlyOnce()
```

- [ ] **Step 2: Add definition property mutation tests**

Create tests:

```csharp
ChangingColumnWidthInvalidatesOwningGridLayout()
ChangingRowHeightInvalidatesOwningGridLayout()
SettingSameColumnWidthDoesNotInvalidate()
SettingSameRowHeightDoesNotInvalidate()
DetachedDefinitionChangeMarksGridDirtyAndProcessesOnAttach()
```

- [ ] **Step 3: Add frame contract tests**

Create tests:

```csharp
GridDefinitionMutationChangesChildBoundsOnNextUpdate()
SecondUnchangedFrameAfterGridDefinitionMutationDoesNoRetainedWork()
GridDefinitionMutationDoesNotRebuildUnrelatedSiblingRenderCache()
```

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~GridDefinitionMutationTests|FullyQualifiedName~GridAuthoringFrameContractTests"
```

Expected: RED because definition list/property mutations are not retained-aware yet.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Layout\GridDefinitionMutationTests.cs tests\Cerneala.Tests\UI\Hosting\GridAuthoringFrameContractTests.cs
git commit -m "test: capture grid definition mutation contract"
```

---

### Task 2: Add Owner-Aware Definition Collections

**Files:**
- Create: `UI/Layout/Panels/GridDefinitionCollection{TDefinition}.cs`
- Modify: `UI/Layout/Panels/Grid.cs`

- [ ] **Step 1: Implement collection shape**

Support the authoring operations needed by existing and new tests:

```text
Count
this[int]
Add(TDefinition)
Remove(TDefinition)
RemoveAt(int)
Clear()
Insert(int, TDefinition)
Replace/set indexer if useful
IReadOnlyList<TDefinition> or IList<TDefinition> if existing tests expect list-like APIs
```

Prefer the smallest interface that preserves current usage.

- [ ] **Step 2: Invalidate owner on structural mutation**

On real mutation, call an internal `Grid.InvalidateDefinitions(reason)` helper that:

```text
IncrementLayoutVersion();
IncrementRenderVersion();
Invalidate(Measure | Arrange | Render | HitTest, reason);
```

- [ ] **Step 3: Avoid duplicate invalidation for no-op mutations**

Removing a missing definition should return false and not invalidate.

- [ ] **Step 4: Preserve simple public API**

Existing code like this should still work:

```csharp
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
```

---

### Task 3: Wire Definition Property Change Notifications

**Files:**
- Modify: `UI/Layout/Panels/ColumnDefinition.cs`
- Modify: `UI/Layout/Panels/RowDefinition.cs`
- Modify: `UI/Layout/Panels/GridDefinitionCollection{TDefinition}.cs`

- [ ] **Step 1: Add internal change event or owner callback**

Keep this internal to layout. Do not make definitions `UiObject` unless tests prove it is necessary.

- [ ] **Step 2: Notify only on value changes**

`Width`/`Height` setters should compare with existing value and no-op for equal values.

- [ ] **Step 3: Attach/detach collection owner**

When a definition is added to a collection, subscribe/register owner. When removed, unsubscribe/unregister owner.

Reject or handle sharing a single definition across two grids. Prefer rejecting with a clear exception rather than supporting shared mutable definitions.

---

### Task 4: Verify Layout Mutation Behavior

**Files:**
- Modify only if tests expose bugs:
  - `UI/Layout/Panels/Grid.cs`
  - `UI/Invalidation/DirtyPropagation.cs`

- [ ] **Step 1: Ensure child bounds update after mutations**

Do not force immediate layout. The next retained update/frame should process the work.

- [ ] **Step 2: Ensure render cache invalidates after layout-affecting mutations**

Child arranged bounds changes should invalidate render cache through existing arrange/render version behavior.

- [ ] **Step 3: Keep unchanged frame clean**

After the mutation frame, the next unchanged frame must show zero measure/arrange/render/hit-test work.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted Grid tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~GridDefinitionMutationTests|FullyQualifiedName~GridAuthoringFrameContractTests|FullyQualifiedName~GridTests"
```

Expected: GREEN.

- [ ] **Step 2: Run layout/invalidation tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~LayoutInvalidationTests|FullyQualifiedName~LayoutDiagnosticsAccuracyTests|FullyQualifiedName~FrameSchedulerStabilityTests|FullyQualifiedName~RetainedNoWorkFrameTests"
```

Expected: GREEN.

- [ ] **Step 3: Run preview gates**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~RuntimePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 4: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 5: Commit implementation**

```powershell
git add UI\Layout\Panels tests\Cerneala.Tests
git commit -m "fix: harden grid definition mutation invalidation"
```
