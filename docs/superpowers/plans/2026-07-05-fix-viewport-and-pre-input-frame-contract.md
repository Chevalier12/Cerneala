# Fix Viewport And Pre-Input Frame Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make `UiHost.Update(...)` commit retained layout/render/hit-test work that already exists at the start of a frame before input dispatch, so pointer hit-testing and focus use current retained bounds after initial layout and viewport changes.

**Architecture:** Keep the retained game-loop contract: `Update(...)` may process dirty retained work, `Draw(...)` only submits committed commands. Add a small pre-input gate that runs only when scheduler work already exists. Input-generated invalidations still run after input dispatch in the same update.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Hosting`, `UI/Invalidation`, `UI/Input`, retained renderer, and hit-test cache.

---

## File Structure

- Modify: `UI/Hosting/UiHost.cs`
  - Add pre-input retained-work gate.
  - Aggregate pre-input and post-input stats into one `UiFrame`.
  - Make viewport changes invalidate measure, arrange, render, and hit-test.
- Modify: `UI/Elements/UIRoot.cs`
  - Expose `HasPendingFrameWork` helper if useful.
  - Keep `ProcessFrame(...)` as canonical scheduler entry point.
- Modify: `UI/Invalidation/FrameStats.cs`
  - Add helper only if needed to avoid false `NoWorkFrames` in two-pass update.
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
  - Avoid scheduler redesign; change only if host cannot safely compose pre/post input work.
- Create: `tests/Cerneala.Tests/UI/Hosting/UiHostViewportFrameContractTests.cs`

## Important Existing Behavior

Current `UiHost.Update(InputFrame, UiViewport?, TimeSpan?)` performs input before frame processing:

```csharp
ApplyViewportIfChanged(currentRoot, currentViewport);
PrimeInitialFrame(currentRoot);
InputBridge.Dispatch(currentRoot, inputFrame);
FrameStats stats = currentRoot.ProcessFrame();
currentRoot.RetainedRenderer.Commit(currentRoot);
```

That means first-frame input and post-resize input can observe stale `ArrangedBounds` and stale `InputCache`. This violates the retained-mode contract: input routing must use committed layout-dependent hit-test data.

Target behavior:

```csharp
ApplyViewportIfChanged(...);
PrimeInitialFrame(...);
ProcessPreInputWorkIfNeeded(...); // only if scheduler already has work
InputBridge.Dispatch(...);
ProcessPostInputWorkIfNeeded(...); // only if input/style/resource queued work
CommitRetainedCommands(...);
```

The final `UiFrame.Stats` must represent total work done by the update. It must not report a no-work frame if either pre-input or post-input work ran.

## Rules

- [ ] Do not process retained work inside `Draw(...)`.
- [ ] Do not make `InputBridge.Dispatch(...)` call layout/render directly.
- [ ] Do not add a second hit-test implementation.
- [ ] Do not process an unconditional full frame before every input dispatch.
- [ ] Keep unchanged frames no-work.

---

### Task 1: Add RED Tests For Viewport And Pre-Input Contract

**Files:**
- Create: `tests/Cerneala.Tests/UI/Hosting/UiHostViewportFrameContractTests.cs`

- [ ] **Step 1: Add viewport invalidation tests**

Create tests:

```csharp
ViewportChangeInvalidatesMeasureArrangeRenderAndHitTest()
ViewportChangeRemeasuresWidthSensitiveElement()
UnchangedFrameStillReportsNoRetainedWorkAfterPreInputGate()
```

Test intent:

- Build root + width-sensitive element (`TextBlock` or a test element recording `MeasureCore` width).
- Run first update to commit layout.
- Run unchanged update and assert no layout/render/hit-test work.
- Change viewport width and assert measure/arrange/render/hit-test work occurs and measured width changes.

- [ ] **Step 2: Add input-after-layout tests**

Create tests:

```csharp
InputAfterViewportChangeUsesCommittedHitTestBounds()
InitialFrameCommitsLayoutBeforePointerHitTestingWhenNeeded()
```

Test intent:

- Put a focusable/clickable `Button` where hit-test bounds only become valid after layout.
- Dispatch pointer press on first frame or immediately after viewport resize.
- Assert focus/command behavior uses current bounds.

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostViewportFrameContractTests"
```

Expected: RED because input dispatch currently runs before layout/hit-test cache is current, and viewport invalidation does not cover the full contract.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\UiHostViewportFrameContractTests.cs
git commit -m "test: capture viewport pre-input frame contract"
```

---

### Task 2: Implement Pre-Input Retained Work Gate

**Files:**
- Modify: `UI/Hosting/UiHost.cs`
- Modify: `UI/Elements/UIRoot.cs` if needed.
- Modify: `UI/Invalidation/FrameStats.cs` only if needed.

- [ ] **Step 1: Expose or use pending scheduler work**

Preferred helper on `UIRoot`:

```csharp
public bool HasPendingFrameWork => Scheduler.HasWork;
```

Using `currentRoot.Scheduler.HasWork` directly from `UiHost` is acceptable if it matches current encapsulation.

- [ ] **Step 2: Aggregate frame stats across pre/post input**

Implement one stats object per update:

```csharp
FrameStats stats = new();

if (currentRoot.Scheduler.HasWork)
{
    currentRoot.ProcessFrame(stats: stats);
}

InputBridge.Dispatch(currentRoot, inputFrame);

if (currentRoot.Scheduler.HasWork)
{
    currentRoot.ProcessFrame(stats: stats);
}
else if (!stats.HasWork)
{
    stats.CountNoWorkFrame();
}

currentRoot.RetainedRenderer.Commit(currentRoot);
```

Adjust method names/signatures to the current code. Do not call `ProcessFrame(...)` with no work unless you intentionally want to count a no-work frame.

- [ ] **Step 3: Fix viewport invalidation flags**

In `ApplyViewportIfChanged(...)`, use:

```csharp
InvalidationFlags.Measure |
InvalidationFlags.Arrange |
InvalidationFlags.Render |
InvalidationFlags.HitTest |
InvalidationFlags.Subtree
```

Also include `HitTest` in initial-frame invalidation if tests prove first-frame input cache needs it.

- [ ] **Step 4: Keep render commit after post-input work**

Do not commit between pre-input work and input dispatch unless tests prove input needs committed draw commands. Input needs current layout/hit-test cache, not submitted rendering.

---

### Task 3: Verify GREEN And Regressions

- [ ] **Step 1: Run viewport tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostViewportFrameContractTests"
```

Expected: GREEN.

- [ ] **Step 2: Run related host/input tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHost|FullyQualifiedName~RetainedVerticalSliceTests|FullyQualifiedName~ElementInputBridgeTests|FullyQualifiedName~HitTestServiceTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Hosting\UiHost.cs UI\Elements\UIRoot.cs UI\Invalidation\FrameStats.cs tests\Cerneala.Tests\UI\Hosting\UiHostViewportFrameContractTests.cs
git commit -m "fix: process retained work before input when needed"
```
