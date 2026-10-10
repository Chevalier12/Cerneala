# Add Retained Stress Budget Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Add deterministic retained-work stress budgets. Cerneala already has many unit and vertical-slice tests. Developer Preview needs larger-tree budget tests that catch accidental whole-tree layout/render/input/semantics rebuilds without relying on wall-clock timing.

**Architecture:** Use existing `FrameStats`, render counters, resource dependency tracker, semantics cache, and preview samples. Measure counts and versions, not elapsed time. If a budget fails, fix the owning invalidation/lifecycle layer; do not loosen retained contracts or add immediate-mode rebuild shortcuts.

**Tech Stack:** C#/.NET 8, xUnit, existing retained scheduler/diagnostics/render cache, fake backends.

---

## File Structure

- Create: `tests/Cerneala.Tests/UI/Hosting/RetainedStressBudgetTests.cs`
- Create: `tests/Cerneala.Tests/UI/Rendering/RenderStressBudgetTests.cs`
- Create: `tests/Cerneala.Tests/UI/Controls/ListStressBudgetTests.cs`
- Create: `tests/Cerneala.Tests/UI/Accessibility/SemanticsStressBudgetTests.cs`
- Modify only if tests expose bugs:
  - `UI/Invalidation/*Queue.cs`
  - `UI/Rendering/RetainedRenderCache.cs`
  - `UI/Controls/ItemsControl.cs`
  - `UI/Controls/ItemsPresenter.cs`
  - `UI/Controls/ItemContainerGenerator.cs`
  - `UI/Accessibility/SemanticsProvider.cs`
  - `UI/Elements/UIRoot.cs`
  - `UI/Styling/StyleProcessor.cs`

## Important Existing Behavior

- Core/Authoring/Runtime preview gates prove small-to-medium vertical slices.
- `FrameStats` tracks measured/arranged/rendered/hit-test/style/command-state work.
- Retained render cache has versions/counters.
- Items virtualization/recycling exists.
- Semantics cache exists and invalidates on semantics-affecting changes.

Target behavior:

- Large unchanged trees do no retained work after first frame.
- Draw loops do not change scheduler/render-cache state.
- Theme color changes do not measure the whole tree when only render/style changes are needed.
- Resource changes invalidate only dependents.
- Observable list append/replace/scroll has bounded work and does not recreate unrelated realized containers.
- Command `CanExecuteChanged` refreshes command sources, not the whole tree.
- Semantics repeated reads use the cache; semantic changes rebuild only when needed.

## Rules

- [ ] Do not use wall-clock performance assertions.
- [ ] Do not write brittle exact counts where a small bounded range is enough.
- [ ] Do not hide real retained work by not counting it.
- [ ] Do not weaken existing FrameStats semantics to pass tests.
- [ ] Do not add artificial test-only fast paths.
- [ ] Do not create hundreds of real textures or platform resources.

---

### Task 1: Add RED Stress Budget Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Hosting/RetainedStressBudgetTests.cs`
- Create: `tests/Cerneala.Tests/UI/Rendering/RenderStressBudgetTests.cs`
- Create: `tests/Cerneala.Tests/UI/Controls/ListStressBudgetTests.cs`
- Create: `tests/Cerneala.Tests/UI/Accessibility/SemanticsStressBudgetTests.cs`

- [ ] **Step 1: Add large static tree tests**

Create tests:

```csharp
LargeStaticTreeFirstFrameDoesWorkAndSecondFrameDoesNoRetainedWork()
LargeStaticTreeHundredDrawsDoNotAdvanceSchedulerOrRenderCacheVersion()
LargeStaticTreeFocusTabNavigationTouchesOnlyFocusVisualStateBudget()
```

Use a deterministic tree of panels, borders, text blocks, buttons, and text boxes. Keep size large enough to catch whole-tree regressions, not so large that tests become slow.

- [ ] **Step 2: Add style/theme/resource tests**

Create tests:

```csharp
ThemeColorChangeDoesNotMeasureLargeTreeWhenOnlyRenderStyleChanges()
ResourceChangeInvalidatesOnlyRegisteredDependentsWithinBudget()
FontResourceChangeInvalidatesOnlyTextDependentsWithinBudget()
```

- [ ] **Step 3: Add list/virtualization stress tests**

Create tests:

```csharp
LargeObservableListInitialFrameRealizesOnlyVisibleWindow()
LargeObservableListAppendDoesNotRecreateUnrelatedRealizedContainers()
LargeObservableListReplaceUpdatesOnlyCompatibleRealizedContainerWithinBudget()
LargeListScrollMovesRealizationWindowWithinBudget()
```

- [ ] **Step 4: Add command/semantics stress tests**

Create tests:

```csharp
ManyButtonsSingleCommandCanExecuteChangedRefreshesOnlyRegisteredCommandSources()
SemanticsRepeatedQueriesReturnCachedTreeForLargeTree()
SemanticNameChangeRebuildsSemanticsWithoutLayoutOrRenderBudget()
```

- [ ] **Step 5: Run targeted tests and verify RED or current GREEN**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedStressBudgetTests|FullyQualifiedName~RenderStressBudgetTests|FullyQualifiedName~ListStressBudgetTests|FullyQualifiedName~SemanticsStressBudgetTests"
```

Expected: RED if any hidden whole-tree work exists; GREEN is acceptable only if tests genuinely cover the budgets without needing implementation.

- [ ] **Step 6: Commit tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\RetainedStressBudgetTests.cs tests\Cerneala.Tests\UI\Rendering\RenderStressBudgetTests.cs tests\Cerneala.Tests\UI\Controls\ListStressBudgetTests.cs tests\Cerneala.Tests\UI\Accessibility\SemanticsStressBudgetTests.cs
git commit -m "test: add retained stress budget gates"
```

---

### Task 2: Fix Any Retained Budget Failures At The Owning Layer

**Files:**
- Modify only files required by failing tests.

- [ ] **Step 1: If unchanged frames do work, fix scheduler/queue dirtiness**

Look for stale dirty flags, queues retaining detached elements, or render cache invalidating root unnecessarily.

- [ ] **Step 2: If draw mutates state, fix render submission**

`RetainedRenderer.Submit(...)` and backend render calls must not generate retained work.

- [ ] **Step 3: If theme/resource changes measure too much, fix invalidation flags/dependencies**

Do not convert render/style-only changes into measure unless intrinsic size changed.

- [ ] **Step 4: If list mutations recreate too much, fix generator/presenter reuse**

Preserve realized container identity where tests expect it. Do not disable virtualization.

- [ ] **Step 5: If semantics queries rebuild too much, fix semantics cache invalidation**

Do not tie semantics cache rebuild to layout/render unless the relevant semantic data actually changed.

---

### Task 3: Make Budget Assertions Maintainable

**Files:**
- Modify: stress test files only unless a tiny helper is needed.

- [ ] **Step 1: Use named constants for budgets**

Example:

```text
MaxRenderOnlyThemeMeasureCalls = 0
MaxListAppendRealizedContainerReplacements = 1
```

- [ ] **Step 2: Avoid exact fragile counts**

Assert zero where invariant requires zero. Use upper bounds where implementation details may legitimately vary.

- [ ] **Step 3: Include failure messages**

Budget tests should explain what retained invariant was violated.

---

### Task 4: Verify GREEN And Regressions

- [ ] **Step 1: Run stress tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedStressBudgetTests|FullyQualifiedName~RenderStressBudgetTests|FullyQualifiedName~ListStressBudgetTests|FullyQualifiedName~SemanticsStressBudgetTests"
```

Expected: GREEN.

- [ ] **Step 2: Run preview gates**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~RuntimePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run adjacent retained systems**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~FrameSchedulerStabilityTests|FullyQualifiedName~RetainedRendererDrawPurityTests|FullyQualifiedName~ItemsControlRecyclingStabilityTests|FullyQualifiedName~RetainedSemanticsCacheTests"
```

Expected: GREEN.

- [ ] **Step 4: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 5: Commit implementation**

```powershell
git add UI tests\Cerneala.Tests
git commit -m "test: enforce retained stress budgets"
```
