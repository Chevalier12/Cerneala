# Clarify Layout Scheduler Contract And Diagnostics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make layout scheduling and diagnostics honest. The scheduler may process deterministic snapshots per phase for MVP, but diagnostics must distinguish queued scheduler work from actual recursive measure/arrange calls performed by panels.

**Architecture:** Do not rewrite layout. Keep the retained queue model. Clarify that MVP uses one snapshot per phase: work enqueued for a later phase can run in the same frame; work enqueued for the same phase runs in the next frame. Add separate counters for queued phase elements and actual layout method calls so diagnostics do not underreport child work. This keeps frame behavior predictable and game-loop friendly without adding premature `FrameBudget` complexity.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala retained layout, existing `FrameStats`, existing playground diagnostics overlay.

---

## File Structure

- Modify: `UI/Invalidation/FrameStats.cs`
  - Add actual layout counters: `MeasureCalls`, `ArrangeCalls`.
  - Keep existing `MeasuredElements` and `ArrangedElements` as queued scheduler phase counts for compatibility, or rename only if all tests are updated in one commit.
- Modify: `UI/Elements/UIRoot.cs`
  - Add internal methods to record actual measure/arrange calls against the active frame stats.
  - Wrap `ProcessFrame(...)` so active stats are visible while layout recursively executes.
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
  - Accept or reuse a caller-created `FrameStats` so root can expose it during recursive layout.
  - Document single-snapshot phase behavior.
- Modify: `UI/Elements/UIElement.cs`
  - Record actual measure/arrange calls when attached to a root and a frame is active.
- Modify: `UI/Diagnostics/LayoutDiagnostics.cs`
  - Report both queued layout phase counts and actual recursive layout calls.
- Modify: `Playground/Cerneala.Playground/Samples/InvalidationStatsOverlay.cs`
  - Display actual layout calls alongside queued phase counts.
- Create: `tests/Cerneala.Tests/UI/Layout/LayoutDiagnosticsAccuracyTests.cs`
  - Prove actual child measure/arrange calls are counted even when only a parent is queued.
- Create: `tests/Cerneala.Tests/UI/Invalidation/FrameSchedulerStabilityTests.cs`
  - Prove single-snapshot phase behavior is explicit and stable.
- Modify: `tests/Cerneala.Tests/UI/Invalidation/FrameStatsTests.cs`
  - Add counter tests.
- Modify: `ROADMAPv2.md`
  - Replace any “process layout queue until stable” wording for MVP with the explicit snapshot contract, unless bounded loops are implemented here.
- Modify: `docs/architecture-v2.md`
  - Add layout scheduler contract wording.

## Important Existing Behavior

`UiFrameScheduler` snapshots each phase once:

```csharp
IReadOnlyList<UIElement> snapshot = layoutQueue.SnapshotMeasure();
foreach (UIElement element in snapshot)
{
    processors.Process(FramePhase.Measure, element);
    ...
}
```

Panels then call children recursively:

```csharp
child.Measure(new MeasureContext(...));
child.Arrange(new ArrangeContext(...));
```

Current `FrameStats.MeasuredElements` counts scheduler-queued elements, not actual child measure calls. That makes diagnostics undercount real work on retained trees.

Target behavior:

```text
QueuedMeasureElements / MeasuredElements: scheduler phase items processed
MeasureCalls: actual UIElement.Measure calls including recursive panel children
QueuedArrangeElements / ArrangedElements: scheduler phase items processed
ArrangeCalls: actual UIElement.Arrange calls including recursive panel children
```

---

### Task 1: Add RED Tests For Layout Diagnostics Accuracy

**Files:**
- Create: `tests/Cerneala.Tests/UI/Layout/LayoutDiagnosticsAccuracyTests.cs`
- Create: `tests/Cerneala.Tests/UI/Invalidation/FrameSchedulerStabilityTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Invalidation/FrameStatsTests.cs`

- [ ] **Step 1: Create actual layout call tests**

Create `tests/Cerneala.Tests/UI/Layout/LayoutDiagnosticsAccuracyTests.cs` with tests like:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using PanelOrientation = Cerneala.UI.Layout.Orientation;

namespace Cerneala.Tests.UI.Layout;

public sealed class LayoutDiagnosticsAccuracyTests
{
    [Fact]
    public void FrameStatsCountActualRecursiveMeasureAndArrangeCalls()
    {
        UIRoot root = new();
        StackPanel panel = new() { Orientation = PanelOrientation.Vertical };
        CountingElement first = new();
        CountingElement second = new();
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        root.VisualChildren.Add(panel);
        UiHost host = new(new UiHostOptions { Root = root });

        UiFrame frame = host.Update(Frame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.MeasureCalls >= 3); // panel + two children, root may also count
        Assert.True(frame.Stats.ArrangeCalls >= 3);
        Assert.True(first.MeasureCalls > 0);
        Assert.True(second.MeasureCalls > 0);
    }

    private static InputFrame Frame() => new(
        PointerSnapshot.Empty,
        PointerSnapshot.Empty,
        KeyboardSnapshot.Empty,
        KeyboardSnapshot.Empty,
        []);

    private sealed class CountingElement : UIElement
    {
        public int MeasureCalls { get; private set; }
        public int ArrangeCalls { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCalls++;
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCalls++;
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }
    }
}
```

- [ ] **Step 2: Create scheduler stability tests**

Create `tests/Cerneala.Tests/UI/Invalidation/FrameSchedulerStabilityTests.cs` proving the chosen MVP behavior:

- same-phase work queued during `Measure` is not processed reentrantly in the same measure snapshot
- downstream work queued during `Arrange` can be processed by later phases in the same frame if that phase snapshot has not yet been taken
- unchanged second frame remains a no-work frame

If the team chooses bounded loops instead, invert the expectations and implement bounded loops in Task 3. Do not leave the behavior ambiguous.

- [ ] **Step 3: Add frame stats counter tests**

In `FrameStatsTests`, add tests for `CountMeasureCall()` and `CountArrangeCall()`.

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~LayoutDiagnosticsAccuracyTests|FullyQualifiedName~FrameSchedulerStabilityTests|FullyQualifiedName~FrameStatsTests"
```

Expected: tests fail because actual layout call counters and explicit scheduler contract tests do not exist yet.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Layout\LayoutDiagnosticsAccuracyTests.cs tests\Cerneala.Tests\UI\Invalidation\FrameSchedulerStabilityTests.cs tests\Cerneala.Tests\UI\Invalidation\FrameStatsTests.cs
git commit -m "test: capture layout scheduler diagnostics contract"
```

---

### Task 2: Add Actual Layout Call Counters

**Files:**
- Modify: `UI/Invalidation/FrameStats.cs`
- Modify: `UI/Elements/UIRoot.cs`
- Modify: `UI/Elements/UIElement.cs`
- Modify: `UI/Invalidation/UiFrameScheduler.cs`

- [ ] **Step 1: Add counters to `FrameStats`**

Add properties and methods:

```csharp
public int MeasureCalls { get; private set; }
public int ArrangeCalls { get; private set; }

public void CountMeasureCall()
{
    MeasureCalls++;
}

public void CountArrangeCall()
{
    ArrangeCalls++;
}
```

Do not change existing `MeasuredElements`/`ArrangedElements` meanings in this step.

- [ ] **Step 2: Let root expose active frame stats**

In `UIRoot`, add:

```csharp
private FrameStats? activeFrameStats;

internal void CountMeasureCall()
{
    activeFrameStats?.CountMeasureCall();
}

internal void CountArrangeCall()
{
    activeFrameStats?.CountArrangeCall();
}
```

Refactor `ProcessFrame(...)` so it creates or receives a `FrameStats` before processing and sets `activeFrameStats` during scheduler execution.

Recommended scheduler change:

```csharp
public FrameStats ProcessFrame(
    FramePhaseProcessors? processors = null,
    FrameBudget budget = default,
    FrameStats? stats = null)
```

Use `stats ??= new FrameStats();` internally.

- [ ] **Step 3: Record calls in `UIElement.Measure(...)` and `Arrange(...)`**

At the start of `Measure(...)`:

```csharp
Root?.CountMeasureCall();
```

At the start of `Arrange(...)`:

```csharp
Root?.CountArrangeCall();
```

This counts actual calls, even if the element returns a cached size/bounds. That is intentional: diagnostics should show method calls, not only recomputation. Existing cache reuse counters can remain separate.

- [ ] **Step 4: Run layout diagnostics tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~LayoutDiagnosticsAccuracyTests|FullyQualifiedName~FrameStatsTests|FullyQualifiedName~UiHostFrameContractTests"
```

Expected: actual call tests pass and existing retained no-work tests remain valid.

- [ ] **Step 5: Commit counters**

```powershell
git add UI\Invalidation\FrameStats.cs UI\Elements\UIRoot.cs UI\Elements\UIElement.cs UI\Invalidation\UiFrameScheduler.cs
git commit -m "feat: count actual layout calls"
```

---

### Task 3: Lock The MVP Scheduler Stability Contract

**Files:**
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
- Modify: `tests/Cerneala.Tests/UI/Invalidation/FrameSchedulerStabilityTests.cs`

- [ ] **Step 1: Choose the MVP contract explicitly**

Use this contract unless a failing correctness case requires bounded loops:

```text
Cerneala MVP processes one deterministic snapshot per frame phase.
Work enqueued for a phase whose snapshot has already been taken is processed on a later frame.
Work enqueued for a downstream phase whose snapshot has not yet been taken can be processed in the same frame.
```

This is predictable for game loops and avoids hidden unbounded work.

- [ ] **Step 2: Document in code comments**

In `UiFrameScheduler.ProcessFrame(...)`, add a short comment above phase processing:

```csharp
// MVP scheduler contract: each phase processes one deterministic snapshot.
// Same-phase work enqueued during processing is intentionally deferred to a later frame.
// Downstream phase work may still run in this frame because its snapshot has not been taken yet.
```

- [ ] **Step 3: Ensure tests match this contract**

Update `FrameSchedulerStabilityTests` to assert exactly this behavior. Do not implement bounded loops unless tests and docs are changed in the same commit.

- [ ] **Step 4: Run scheduler tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~FrameSchedulerStabilityTests|FullyQualifiedName~UiFrameSchedulerTests|FullyQualifiedName~RetainedNoWorkFrameTests"
```

Expected: scheduler contract is explicit and no-work-frame tests pass.

- [ ] **Step 5: Commit scheduler contract**

```powershell
git add UI\Invalidation\UiFrameScheduler.cs tests\Cerneala.Tests\UI\Invalidation\FrameSchedulerStabilityTests.cs
git commit -m "docs: lock layout scheduler snapshot contract"
```

---

### Task 4: Update Diagnostics Output

**Files:**
- Modify: `UI/Diagnostics/LayoutDiagnostics.cs`
- Modify: `Playground/Cerneala.Playground/Samples/InvalidationStatsOverlay.cs`
- Modify: `tests/Cerneala.Tests/UI/Diagnostics/LayoutDiagnosticsTests.cs`
- Modify: `tests/Cerneala.Tests/Playground/Game1SourceTests.cs` if source tests assert overlay text

- [ ] **Step 1: Update layout diagnostics model/text**

Add actual call counts to diagnostics output. Preserve existing fields where tests or samples depend on them.

Recommended displayed terms:

```text
queuedMeasure=...
queuedArrange=...
measureCalls=...
arrangeCalls=...
```

- [ ] **Step 2: Update playground overlay**

Change `InvalidationStatsOverlay.Format(...)` to include actual layout calls:

```csharp
$"Frame stats: queuedMeasure={frame.Stats.MeasuredElements}, queuedArrange={frame.Stats.ArrangedElements}, measureCalls={frame.Stats.MeasureCalls}, arrangeCalls={frame.Stats.ArrangeCalls}, renderCache={frame.Stats.RenderedElements}, hitTest={frame.Stats.HitTestElements}, reusedCaches={frame.Stats.ReusedCaches}, noWork={frame.Stats.NoWorkFrames}"
```

- [ ] **Step 3: Run diagnostics tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~LayoutDiagnosticsTests|FullyQualifiedName~FrameDiagnosticsTests|FullyQualifiedName~Game1SourceTests"
```

Expected: diagnostics tests pass and overlay terminology is honest.

- [ ] **Step 4: Commit diagnostics update**

```powershell
git add UI\Diagnostics\LayoutDiagnostics.cs Playground\Cerneala.Playground\Samples\InvalidationStatsOverlay.cs tests\Cerneala.Tests\UI\Diagnostics\LayoutDiagnosticsTests.cs tests\Cerneala.Tests\Playground\Game1SourceTests.cs
git commit -m "feat: report actual layout call diagnostics"
```

---

### Task 5: Update Roadmap And Architecture Wording

**Files:**
- Modify: `ROADMAPv2.md`
- Modify: `docs/architecture-v2.md`
- Modify: `architecture.md` only if it contains conflicting scheduler wording
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `AUDIT_FIX_PLAN.md` if tracked

- [ ] **Step 1: Replace vague stable-queue wording**

Find roadmap/doc wording like:

```text
process layout queue until stable
```

Replace with:

```text
MVP scheduler processes one deterministic snapshot per phase. Same-phase work enqueued during processing is deferred to a later frame; downstream phase work can run in the same frame if its snapshot has not yet been taken. Bounded until-stable loops are deferred until FrameBudget scheduling exists.
```

- [ ] **Step 2: Document diagnostics semantics**

Add a note that:

- `MeasuredElements` / `ArrangedElements` are queued phase counts.
- `MeasureCalls` / `ArrangeCalls` are actual recursive method calls.

- [ ] **Step 3: Run doc/source grep verification**

```powershell
Select-String -Path ROADMAPv2.md,docs\architecture-v2.md,architecture.md -Pattern "until stable|MeasureCalls|snapshot per phase" -Context 1,3
```

Expected: no misleading “until stable” MVP wording remains unless explicitly deferred.

- [ ] **Step 4: Commit docs**

```powershell
git add ROADMAPv2.md docs\architecture-v2.md architecture.md ROADMAPv2_AUDIT.md AUDIT_FIX_PLAN.md
git commit -m "docs: clarify layout scheduler contract"
```

---

### Task 6: Full Verification

**Files:**
- No source changes unless tests fail

- [ ] **Step 1: Run full tests**

```powershell
dotnet test Cerneala.slnx
```

Expected: full suite passes.

- [ ] **Step 2: Run playground source tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Playground"
```

Expected: playground tests pass.

- [ ] **Step 3: Commit any final test fixes**

```powershell
git status --short
```

Expected: clean working tree after commits.

## Stop Conditions

- [ ] Stop if this turns into a layout rewrite. The goal is honest counters and explicit scheduler behavior.
- [ ] Stop if bounded loops are added without a max-iteration guard and tests for runaway invalidation.
- [ ] Stop if diagnostics continue to show only queued counts while implying actual recursive layout work.
