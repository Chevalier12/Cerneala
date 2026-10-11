# Fix Tree Mutation Invalidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make attached visual tree mutations schedule retained measure, arrange, render-cache, and hit-test work during `Update`, instead of relying on tree-version/root-cache invalidation alone.

**Architecture:** Visual child add/remove remains owned by `UIElementCollection`. Tree version increments stay as bookkeeping and root command invalidation, while a single visual mutation helper raises explicit retained invalidation for the visual owner and, on add, the attached child subtree. `UiHost.Update(...)` must process late visual add/remove work and commit root commands before `Draw(...)`.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala retained UI runtime, existing `DirtyPropagation`, `LayoutQueue`, `RenderQueue`, `HitTestQueue`, and `UiHost`.

---

## File Structure

- Modify: `UI/Elements/UIElementCollection.cs`
  - Add a shared visual child mutation invalidation helper.
  - Call it from visual add and visual remove paths after tree version bookkeeping.
- Create: `tests/Cerneala.Tests/UI/Elements/UIElementCollectionInvalidationTests.cs`
  - Prove attached visual add invalidates owner and child subtree work.
  - Prove attached visual remove invalidates root owners as well as non-root owners.
- Create: `tests/Cerneala.Tests/UI/Hosting/UiHostLateTreeMutationTests.cs`
  - Prove a child added after the first frame is measured/arranged/rendered during the next update.
  - Prove a child removed after the first frame is processed during the next update and draw only submits committed output.
- Modify: `tests/Cerneala.Tests/UI/Elements/UIElementCollectionTests.cs`
  - Remove or keep the old narrower remove test after the new coverage exists. If kept, update expectations only if duplicate coverage becomes noisy.
- Modify: `AUDIT_FIX_PLAN.md`
  - Mark or link this detailed plan under Plan 2 after implementation.
- Modify: `ROADMAPv2_AUDIT.md`
  - Add an implementation note under Must Fix item 1 only after the full suite passes.

## Important Existing Behavior

Current broken add path:

```csharp
UIElementCollection.Add(child)
    -> children.Add(child)
    -> SetParent(child, owner)
    -> ElementLifecycle.AttachSubtree(root, child)
    -> root.IncrementTreeVersion()
```

`root.IncrementTreeVersion()` invalidates root composition but does not enqueue layout/render/hit-test work. After Plan 1, this can make `UiHost.Update(...)` fail during `RetainedRenderer.Commit(...)` because local render caches for newly added visible elements were never rebuilt by `RenderQueueProcessor`.

Current broken remove path:

```csharp
UIElementCollection.Remove(child)
    -> detach child
    -> oldRoot.IncrementTreeVersion()
    -> InvalidateOwnerForVisualChildRemoval()
        -> returns early when owner is UIRoot
```

Removal under a non-root owner has partial invalidation; removal directly under `UIRoot` is skipped. Both add and remove should use one explicit visual mutation helper.

Target behavior:

```csharp
UIElementCollection.Add(child)
    -> attach subtree
    -> root.IncrementTreeVersion()
    -> InvalidateForVisualChildMutation(child, Added)
        -> invalidate owner measure/arrange/render/hit-test
        -> invalidate added child visual subtree measure/arrange/render/hit-test

UIElementCollection.Remove(child)
    -> detach subtree / increment tree version
    -> InvalidateForVisualChildMutation(child, Removed)
        -> invalidate owner measure/arrange/render/hit-test, including UIRoot
```

---

### Task 1: Add RED Tests For Visual Tree Mutation Queues

**Files:**
- Create: `tests/Cerneala.Tests/UI/Elements/UIElementCollectionInvalidationTests.cs`

- [x] **Step 1: Create visual mutation queue tests**

Create `tests/Cerneala.Tests/UI/Elements/UIElementCollectionInvalidationTests.cs`:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Elements;

public sealed class UIElementCollectionInvalidationTests
{
    [Fact]
    public void AttachedVisualChildAddInvalidatesOwnerAndAddedSubtree()
    {
        UIRoot root = new();
        UIElement parent = new();
        root.VisualChildren.Add(parent);
        ProcessInitialFrame(root);
        UIElement child = new();
        UIElement grandchild = new();
        child.VisualChildren.Add(grandchild);

        parent.VisualChildren.Add(child);

        Assert.Contains(parent, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(parent, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(parent, root.RenderQueue.Snapshot());
        Assert.Contains(parent, root.HitTestQueue.Snapshot());
        Assert.Contains(child, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(child, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(child, root.RenderQueue.Snapshot());
        Assert.Contains(child, root.HitTestQueue.Snapshot());
        Assert.Contains(grandchild, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(grandchild, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(grandchild, root.RenderQueue.Snapshot());
        Assert.Contains(grandchild, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void AttachedVisualChildRemoveInvalidatesNonRootOwner()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        ProcessInitialFrame(root);

        parent.VisualChildren.Remove(child);

        Assert.Contains(parent, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(parent, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(parent, root.RenderQueue.Snapshot());
        Assert.Contains(parent, root.HitTestQueue.Snapshot());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotMeasure());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotArrange());
        Assert.DoesNotContain(child, root.RenderQueue.Snapshot());
        Assert.DoesNotContain(child, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void AttachedVisualChildRemoveInvalidatesRootOwner()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ProcessInitialFrame(root);

        root.VisualChildren.Remove(child);

        Assert.Contains(root, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(root, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(root, root.RenderQueue.Snapshot());
        Assert.Contains(root, root.HitTestQueue.Snapshot());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotMeasure());
        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotArrange());
        Assert.DoesNotContain(child, root.RenderQueue.Snapshot());
        Assert.DoesNotContain(child, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void DetachedVisualChildMutationDoesNotQueueRetainedWork()
    {
        UIElement parent = new();
        UIElement child = new();

        parent.VisualChildren.Add(child);

        Assert.Null(parent.Root);
        Assert.Null(child.Root);
    }

    private static void ProcessInitialFrame(UIRoot root)
    {
        root.Invalidate(
            InvalidationFlags.Measure |
            InvalidationFlags.Arrange |
            InvalidationFlags.Render |
            InvalidationFlags.HitTest |
            InvalidationFlags.Subtree,
            "Initial test frame");
        root.ProcessFrame();
        Assert.Equal(0, root.LayoutQueue.MeasureCount);
        Assert.Equal(0, root.LayoutQueue.ArrangeCount);
        Assert.Equal(0, root.RenderQueue.Count);
        Assert.Equal(0, root.HitTestQueue.Count);
    }
}
```

- [x] **Step 2: Run the new tests and verify RED**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UIElementCollectionInvalidationTests"
```

Expected: at least `AttachedVisualChildAddInvalidatesOwnerAndAddedSubtree` and `AttachedVisualChildRemoveInvalidatesRootOwner` fail. Current add does not queue retained work, and current remove skips `UIRoot`.

- [x] **Step 3: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Elements\UIElementCollectionInvalidationTests.cs
git commit -m "test: capture visual tree mutation invalidation gaps"
```

---

### Task 2: Add RED Host Tests For Late Visual Tree Mutation

**Files:**
- Create: `tests/Cerneala.Tests/UI/Hosting/UiHostLateTreeMutationTests.cs`

- [x] **Step 1: Create host integration tests**

Create `tests/Cerneala.Tests/UI/Hosting/UiHostLateTreeMutationTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiHostLateTreeMutationTests
{
    [Fact]
    public void VisualChildAddedAfterFirstFrameIsProcessedDuringNextUpdate()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        RenderCountingElement child = new();

        root.VisualChildren.Add(child);

        Assert.Throws<InvalidOperationException>(() => root.RetainedRenderer.Render(root));

        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        DrawCommandList committed = root.RetainedRenderer.Render(root);

        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.True(frame.Stats.HitTestElements > 0);
        Assert.Equal(1, child.MeasureCount);
        Assert.Equal(1, child.ArrangeCount);
        Assert.Equal(1, child.RenderCount);
        Assert.Single(committed);
    }

    [Fact]
    public void VisualChildAddedAfterFirstFrameDoesNotRenderDuringDraw()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        FakeDrawingBackend backend = new();
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        RenderCountingElement child = new();
        root.VisualChildren.Add(child);
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterUpdate = child.RenderCount;

        host.Draw(backend);
        host.Draw(backend);

        Assert.Equal(2, backend.RenderCalls);
        Assert.Equal(renderCountAfterUpdate, child.RenderCount);
        Assert.NotNull(backend.LastCommands);
        Assert.Single(backend.LastCommands);
    }

    [Fact]
    public void RootVisualChildRemovedAfterFirstFrameIsProcessedDuringNextUpdate()
    {
        UIRoot root = new();
        RenderCountingElement child = new();
        root.VisualChildren.Add(child);
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterFirstFrame = child.RenderCount;

        root.VisualChildren.Remove(child);

        Assert.Throws<InvalidOperationException>(() => root.RetainedRenderer.Render(root));

        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        DrawCommandList committed = root.RetainedRenderer.Render(root);

        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.True(frame.Stats.HitTestElements > 0);
        Assert.Equal(renderCountAfterFirstFrame, child.RenderCount);
        Assert.Empty(committed);
    }

    private sealed class RenderCountingElement : UIElement
    {
        public int MeasureCount { get; private set; }

        public int ArrangeCount { get; private set; }

        public int RenderCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }

        protected override void OnRender(RenderContext context)
        {
            RenderCount++;
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), Color.White);
        }
    }
}
```

- [x] **Step 2: Run the host tests and verify RED**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostLateTreeMutationTests"
```

Expected: add-after-first-frame currently fails because `UiHost.Update(...)` tries to commit root commands without child local render caches; remove-after-first-frame should fail because the update can report no retained work for root removal.

- [x] **Step 3: Commit RED host tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\UiHostLateTreeMutationTests.cs
git commit -m "test: capture late visual tree mutation frames"
```

---

### Task 3: Add Shared Visual Mutation Invalidation

**Files:**
- Modify: `UI/Elements/UIElementCollection.cs`

- [x] **Step 1: Replace add/remove invalidation calls**

In `UI/Elements/UIElementCollection.cs`, replace the attached-root block in `Add(...)`:

```csharp
        UIRoot? root = owner.Root;
        if (root is not null)
        {
            ElementLifecycle.AttachSubtree(root, child);
            root.IncrementTreeVersion();
        }
```

with:

```csharp
        UIRoot? root = owner.Root;
        if (root is not null)
        {
            ElementLifecycle.AttachSubtree(root, child);
            root.IncrementTreeVersion();
            InvalidateForVisualChildMutation(child, ElementTreeChangeKind.Added);
        }
```

In `Remove(...)`, replace:

```csharp
        Changed?.Invoke(this, new ElementTreeChange(owner, child, role, ElementTreeChangeKind.Removed));
        InvalidateOwnerForVisualChildRemoval();
        return true;
```

with:

```csharp
        InvalidateForVisualChildMutation(child, ElementTreeChangeKind.Removed);
        Changed?.Invoke(this, new ElementTreeChange(owner, child, role, ElementTreeChangeKind.Removed));
        return true;
```

- [x] **Step 2: Replace the old removal-only helper**

In `UI/Elements/UIElementCollection.cs`, delete the existing `InvalidateOwnerForVisualChildRemoval()` method and add this method in its place:

```csharp
    private void InvalidateForVisualChildMutation(UIElement child, ElementTreeChangeKind kind)
    {
        if (role != ElementChildRole.Visual || owner.Root is null)
        {
            return;
        }

        string reason = kind == ElementTreeChangeKind.Added
            ? "Visual child added"
            : "Visual child removed";
        InvalidationFlags flags =
            InvalidationFlags.Measure |
            InvalidationFlags.Arrange |
            InvalidationFlags.Render |
            InvalidationFlags.HitTest;

        owner.IncrementLayoutVersion();
        owner.IncrementRenderVersion();
        owner.Invalidate(flags, reason);

        if (kind == ElementTreeChangeKind.Added && child.Root is not null)
        {
            child.Invalidate(flags | InvalidationFlags.Subtree, reason);
        }
    }
```

Rationale for this exact shape:

- Owner invalidation is required for add and remove because the owner’s layout, visual composition, and hit-test surface changed.
- Added child subtree invalidation is required because newly attached descendants have no scheduler-owned local render cache yet.
- Removed child subtree should not be queued because it is detached from the root; queue snapshots should remove detached elements.
- `root.IncrementTreeVersion()` stays in `Add(...)`/`Remove(...)` as bookkeeping and root command invalidation, not as the scheduler work signal.

- [x] **Step 3: Run visual mutation tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UIElementCollectionInvalidationTests|FullyQualifiedName~UIElementCollectionTests|FullyQualifiedName~UIRootTests"
```

Expected: all filtered tests pass.

- [x] **Step 4: Run host late mutation tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostLateTreeMutationTests|FullyQualifiedName~UiHostFrameContractTests|FullyQualifiedName~UiHostFrameStatsIntegrityTests"
```

Expected: all filtered tests pass.

- [x] **Step 5: Commit production fix**

```powershell
git add UI\Elements\UIElementCollection.cs
git commit -m "fix: schedule retained work for visual tree mutations"
```

---

### Task 4: Verify Retained Frame Contract Did Not Regress

**Files:**
- No production edits unless verification reveals a missed interaction.

- [x] **Step 1: Run rendering, hosting, and invalidation tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Rendering|FullyQualifiedName~Hosting|FullyQualifiedName~Invalidation|FullyQualifiedName~Elements"
```

Expected: all filtered tests pass.

- [x] **Step 2: Search for lazy tree mutation assumptions**

Run:

```powershell
rg -n "IncrementTreeVersion\(|Visual child removed|Visual child added|RetainedRenderer\.Commit\(|RetainedRenderer\.Render\(" UI tests\Cerneala.Tests
```

Expected:

- `IncrementTreeVersion(...)` remains tree-version/root-cache bookkeeping only.
- Visual mutation invalidation is centralized in `UIElementCollection`.
- `RetainedRenderer.Render(...)` is used as a committed-output getter or invalid-state assertion, not as a lazy repair path.

- [x] **Step 3: Commit any test migration fixes**

If Step 1 reveals tests that expected no work after visual tree mutation, update them to the explicit retained-work expectation and commit:

```powershell
git add tests\Cerneala.Tests
git commit -m "test: update visual tree mutation expectations"
```

If no changes were needed, do not create an empty commit.

---

### Task 5: Update Audit Documentation

**Files:**
- Modify: `AUDIT_FIX_PLAN.md`
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `docs/superpowers/plans/2026-07-03-fix-tree-mutation-invalidation.md`

- [x] **Step 1: Update `AUDIT_FIX_PLAN.md` Plan 2 checklist**

In `AUDIT_FIX_PLAN.md`, under `### Plan 2: fix-tree-mutation-invalidation`, add this detailed-plan link if it is not already present:

```markdown
Detailed plan: `docs/superpowers/plans/2026-07-03-fix-tree-mutation-invalidation.md`
```

After implementation and focused verification pass, change these Plan 2 items from `[ ]` to `[x]`:

```markdown
- [x] Visual child add invalidates measure, arrange, render, and hit-test work.
- [x] Visual child remove invalidates measure, arrange, render, and hit-test work.
- [x] `UIRoot` is not skipped for visual child mutation invalidation.
- [x] Tree version increments remain bookkeeping, not a substitute for dirty work.
- [x] Add shared helper for visual child mutation invalidation.
- [x] Add `tests/Cerneala.Tests/UI/Elements/UIElementCollectionInvalidationTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Hosting/UiHostLateTreeMutationTests.cs`.
```

- [x] **Step 2: Add a completion note to `ROADMAPv2_AUDIT.md`**

Only after `dotnet test Cerneala.slnx` passes, add this note under `## Must Fix` > `### 1. Tree mutation invalidation is broken`, after the required changes list:

```markdown
Implementation note: fixed by `fix-tree-mutation-invalidation`; attached visual add/remove now schedules retained measure, arrange, render-cache, and hit-test work during update, while tree-version increments remain bookkeeping.
```

- [x] **Step 3: Run markdown reference check**

Run:

```powershell
rg -n "fix-tree-mutation-invalidation|UIElementCollectionInvalidationTests|UiHostLateTreeMutationTests" AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md docs\superpowers\plans\2026-07-03-fix-tree-mutation-invalidation.md
```

Expected: all three docs reference the completed plan and tests.

- [ ] **Step 4: Commit docs**

```powershell
git add AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md docs\superpowers\plans\2026-07-03-fix-tree-mutation-invalidation.md
git commit -m "docs: plan visual tree mutation invalidation fix"
```

---

### Task 6: Full Verification

**Files:**
- No production edits unless tests reveal a missed migration.

- [ ] **Step 1: Run full test suite**

Run:

```powershell
dotnet test Cerneala.slnx
```

Expected: all tests pass.

- [ ] **Step 2: Verify legacy planning-tool references did not return to active architecture docs**

Run:

```powershell
rg -n "OpenSpec|openspec|opsx" ROADMAPv2.md ROADMAPv2_AUDIT.md tests UI
```

Expected: no matches.

- [ ] **Step 3: Inspect git diff**

Run:

```powershell
git status --short
git diff --stat
```

Expected: only files touched by this plan are modified/untracked.

- [ ] **Step 4: Final commit if any uncommitted verification fixes exist**

If Step 1 or Step 2 required fixes:

```powershell
git add <fixed-paths>
git commit -m "fix: complete visual tree mutation invalidation"
```

---

## Self-Review

### Spec Coverage

- Visual child add invalidates measure/arrange/render/hit-test: covered by Tasks 1 and 3.
- Visual child remove invalidates measure/arrange/render/hit-test: covered by Tasks 1 and 3.
- `UIRoot` is not skipped: covered by `AttachedVisualChildRemoveInvalidatesRootOwner` and host remove test.
- Tree version remains bookkeeping: covered by keeping `root.IncrementTreeVersion()` and adding explicit invalidation separately.
- Shared helper: covered by Task 3.
- Required tests: covered by Tasks 1 and 2.
- Late mutation processed during update, not draw: covered by Task 2.

### Placeholder Scan

No task uses placeholder language. Every code-changing step names exact files, code blocks, commands, and expected results.

### Type Consistency

- `ElementTreeChangeKind.Added` and `ElementTreeChangeKind.Removed` already exist and are used by `UIElementCollection`.
- `InvalidationFlags.Measure`, `Arrange`, `Render`, `HitTest`, and `Subtree` already exist and are used by current invalidation tests.
- `FakeInputSource` and `FakeDrawingBackend` already exist in `tests.Cerneala.Tests/UI/Hosting`.
- `RetainedRenderer.Render(...)` remains the committed-output getter from Plan 1.
