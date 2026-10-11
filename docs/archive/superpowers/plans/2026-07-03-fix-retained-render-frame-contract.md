# Fix Retained Render Frame Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make retained rendering honest: `Update` rebuilds retained render caches and commits the root command list; `Draw` only submits the last committed command list.

**Architecture:** Local element command generation must happen only through `RenderQueueProcessor` during the render-cache frame phase. `DrawCommandListBuilder` becomes a pure root-command-list composer over already-valid local caches. `RetainedRenderer.Submit(...)` submits the committed root command list directly under a read-only backend contract and no longer copies or rebuilds commands.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala retained UI runtime, MonoGame adapter through `IDrawingBackend`.

---

## File Structure

- Modify: `UI/Rendering/ElementRenderCache.cs`
  - Add a small read-only-valid-cache guard API used by root composition.
- Modify: `UI/Rendering/DrawCommandListBuilder.cs`
  - Remove local cache rebuild backdoor.
  - Throw clearly if composition sees stale/missing local commands.
- Modify: `UI/Rendering/RetainedRenderer.cs`
  - Add explicit `Commit(UIRoot root)`.
  - Make `Render(UIRoot root)` return only the last committed commands.
  - Make `Submit(...)` submit without copying.
- Modify: `UI/Hosting/UiHost.cs`
  - Commit root commands during `Update(...)`.
  - Keep `Draw(...)` as submission-only.
- Modify: `Drawing/IDrawingBackend.cs`
  - Document that backends must treat submitted commands as read-only.
- Modify: `tests/Cerneala.Tests/UI/Rendering/DrawCommandListBuilderTests.cs`
  - Update builder tests to prepare local caches before composing.
- Modify: `tests/Cerneala.Tests/UI/Rendering/RetainedRendererTests.cs`
  - Update lazy-render/copy-protection expectations to committed-read-only expectations.
- Create: `tests/Cerneala.Tests/UI/Rendering/RenderBackdoorContractTests.cs`
  - Prove root composition cannot call `OnRender(...)`.
- Create: `tests/Cerneala.Tests/UI/Rendering/RetainedRendererDrawPurityTests.cs`
  - Prove `Submit(...)` cannot regenerate commands and does not copy the command list.
- Create: `tests/Cerneala.Tests/UI/Hosting/UiHostFrameStatsIntegrityTests.cs`
  - Prove update accounts for render-cache rebuilds before commit and draw is pure.
- Modify: `AUDIT_FIX_PLAN.md`
  - Mark or link this detailed plan under Plan 1 after implementation.

## Important Existing Behavior

Current bug path:

```csharp
RetainedRenderer.Submit(root, backend)
    -> Render(root)
        -> DrawCommandListBuilder.Build(root, cache, counters)
            -> ElementRenderCache.Ensure(element, counters)
                -> element.Render(context)
```

Target path:

```csharp
UiHost.Update(frame)
    -> root.ProcessFrame()
        -> RenderQueueProcessor.Process(element)
            -> ElementRenderCache.Ensure(element, counters)
    -> root.RetainedRenderer.Commit(root)
        -> DrawCommandListBuilder.Build(root, cache, counters)

UiHost.Draw(backend)
    -> root.RetainedRenderer.Submit(root, backend)
        -> backend.Render(root.RetainedRenderCache.RootCommands)
```

---

### Task 1: Add RED Tests For The Builder Backdoor

**Files:**
- Create: `tests/Cerneala.Tests/UI/Rendering/RenderBackdoorContractTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Rendering/DrawCommandListBuilderTests.cs`

- [ ] **Step 1: Create the failing backdoor contract tests**

Create `tests/Cerneala.Tests/UI/Rendering/RenderBackdoorContractTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderBackdoorContractTests
{
    [Fact]
    public void BuilderThrowsWhenVisibleLocalCacheWasNeverBuilt()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        RetainedRenderCache cache = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new DrawCommandListBuilder().Build(root, cache, new RenderCounters()));

        Assert.Contains("valid local render cache", exception.Message);
        Assert.Equal(0, child.RenderCount);
    }

    [Fact]
    public void BuilderThrowsWhenVisibleLocalCacheIsStale()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        cache.GetElementCache(root).Ensure(root, counters, forceRebuild: true);
        cache.GetElementCache(child).Ensure(child, counters, forceRebuild: true);
        int renderCountAfterPrepare = child.RenderCount;

        child.ChangeDependencies(RenderDependency.WithTextLayoutIdentity("changed"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new DrawCommandListBuilder().Build(root, cache, counters));

        Assert.Contains("valid local render cache", exception.Message);
        Assert.Equal(renderCountAfterPrepare, child.RenderCount);
    }

    [Fact]
    public void BuilderComposesPreparedLocalCachesWithoutRenderingElements()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        cache.GetElementCache(root).Ensure(root, counters, forceRebuild: true);
        cache.GetElementCache(child).Ensure(child, counters, forceRebuild: true);
        int renderCountAfterPrepare = child.RenderCount;

        new DrawCommandListBuilder().Build(root, cache, counters);

        Assert.True(cache.IsRootValid);
        Assert.Single(cache.RootCommands);
        Assert.Equal(renderCountAfterPrepare, child.RenderCount);
    }
}
```

- [ ] **Step 2: Run the new tests and verify RED**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RenderBackdoorContractTests"
```

Expected: at least the first two tests fail because current `DrawCommandListBuilder` calls `ElementRenderCache.Ensure(...)` and renders instead of throwing.

- [ ] **Step 3: Update existing builder tests to use prepared local caches**

In `tests/Cerneala.Tests/UI/Rendering/DrawCommandListBuilderTests.cs`, add this helper inside the test class:

```csharp
private static RetainedRenderCache PreparedCache(UIElement root)
{
    RetainedRenderCache cache = new();
    RenderCounters counters = new();
    PrepareSubtree(root, cache, counters);
    return cache;
}

private static void PrepareSubtree(UIElement element, RetainedRenderCache cache, RenderCounters counters)
{
    cache.GetElementCache(element).Ensure(element, counters, forceRebuild: true);
    foreach (UIElement child in element.VisualChildren)
    {
        PrepareSubtree(child, cache, counters);
    }
}
```

Replace each test-local `RetainedRenderCache cache = new();` that expects successful composition with:

```csharp
RetainedRenderCache cache = PreparedCache(root);
```

For `ClipCommandsWrapVisibleSubtree`, prepare the `root` variable after setting the clip:

```csharp
RetainedRenderCache cache = PreparedCache(root);
```

- [ ] **Step 4: Run builder tests and verify RED only where expected**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DrawCommandListBuilderTests|FullyQualifiedName~RenderBackdoorContractTests"
```

Expected: existing builder happy-path tests should still pass after preparing caches; new backdoor tests should remain RED until Task 2.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Rendering\RenderBackdoorContractTests.cs tests\Cerneala.Tests\UI\Rendering\DrawCommandListBuilderTests.cs
git commit -m "test: capture render composition backdoor"
```

---

### Task 2: Make Root Composition Pure

**Files:**
- Modify: `UI/Rendering/ElementRenderCache.cs`
- Modify: `UI/Rendering/DrawCommandListBuilder.cs`

- [ ] **Step 1: Add a valid-cache guard to `ElementRenderCache`**

In `UI/Rendering/ElementRenderCache.cs`, add this method below `IsStale(...)`:

```csharp
public DrawCommandList GetValidCommands(UIElement element)
{
    ArgumentNullException.ThrowIfNull(element);
    if (IsStale(element))
    {
        throw new InvalidOperationException(
            $"Element '{element.GetType().Name}' does not have a valid local render cache. " +
            "Local render caches must be rebuilt by RenderQueueProcessor before root command composition.");
    }

    return commands;
}
```

- [ ] **Step 2: Remove `Ensure(...)` from `DrawCommandListBuilder`**

In `UI/Rendering/DrawCommandListBuilder.cs`, replace:

```csharp
ElementRenderCache localCache = renderCache.GetElementCache(element);
localCache.Ensure(element, counters);
foreach (DrawCommand command in localCache.Commands)
{
    rootCommands.Add(command);
    counters.CountEmittedCommands(1);
}
```

with:

```csharp
ElementRenderCache localCache = renderCache.GetElementCache(element);
foreach (DrawCommand command in localCache.GetValidCommands(element))
{
    rootCommands.Add(command);
    counters.CountEmittedCommands(1);
}
```

- [ ] **Step 3: Run focused rendering tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DrawCommandListBuilderTests|FullyQualifiedName~RenderBackdoorContractTests|FullyQualifiedName~RenderQueueProcessorTests"
```

Expected: all filtered tests pass.

- [ ] **Step 4: Commit pure composition**

```powershell
git add UI\Rendering\ElementRenderCache.cs UI\Rendering\DrawCommandListBuilder.cs
git commit -m "fix: make root command composition pure"
```

---

### Task 3: Make `RetainedRenderer` Commit Explicitly And Stop Copying Draw Commands

**Files:**
- Modify: `UI/Rendering/RetainedRenderer.cs`
- Modify: `Drawing/IDrawingBackend.cs`
- Modify: `tests/Cerneala.Tests/UI/Rendering/RetainedRendererTests.cs`
- Create: `tests/Cerneala.Tests/UI/Rendering/RetainedRendererDrawPurityTests.cs`

- [ ] **Step 1: Add failing retained renderer purity tests**

Create `tests/Cerneala.Tests/UI/Rendering/RetainedRendererDrawPurityTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RetainedRendererDrawPurityTests
{
    [Fact]
    public void RenderThrowsWhenRootCommandListIsNotCommitted()
    {
        UIRoot root = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            root.RetainedRenderer.Render(root));

        Assert.Contains("committed", exception.Message);
    }

    [Fact]
    public void CommitBuildsRootCommandsFromPreparedLocalCaches()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        PrepareSubtree(root);
        int renderCountAfterPrepare = child.RenderCount;

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(renderCountAfterPrepare, child.RenderCount);
        Assert.Same(commands, root.RetainedRenderer.Render(root));
    }

    [Fact]
    public void SubmitUsesCommittedCommandListWithoutCopying()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        PrepareSubtree(root);
        DrawCommandList committed = root.RetainedRenderer.Commit(root);
        CapturingBackend backend = new();

        root.RetainedRenderer.Submit(root, backend);

        Assert.Same(committed, backend.LastCommands);
        Assert.Equal(1, child.RenderCount);
    }

    [Fact]
    public void SubmitThrowsWhenRootCommandListIsInvalid()
    {
        UIRoot root = new();
        CapturingBackend backend = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            root.RetainedRenderer.Submit(root, backend));

        Assert.Contains("committed", exception.Message);
        Assert.Equal(0, backend.RenderCalls);
    }

    private static void PrepareSubtree(UIElement element)
    {
        UIRoot root = element.Root ?? throw new InvalidOperationException("Element must be attached.");
        RenderCounters counters = root.RenderCounters;
        root.RetainedRenderCache.GetElementCache(element).Ensure(element, counters, forceRebuild: true);
        foreach (UIElement child in element.VisualChildren)
        {
            PrepareSubtree(child);
        }
    }

    private sealed class CapturingBackend : IDrawingBackend
    {
        public int RenderCalls { get; private set; }

        public DrawCommandList? LastCommands { get; private set; }

        public void Render(DrawCommandList commands)
        {
            RenderCalls++;
            LastCommands = commands;
        }
    }
}
```

- [ ] **Step 2: Run the new tests and verify RED**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedRendererDrawPurityTests"
```

Expected: failures because `Render(...)` currently lazily builds root commands and `Submit(...)` copies commands.

- [ ] **Step 3: Implement explicit commit and no-copy submit**

Replace `UI/Rendering/RetainedRenderer.cs` with this shape:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Rendering;

public sealed class RetainedRenderer
{
    private readonly RetainedRenderCache renderCache;
    private readonly DrawCommandListBuilder builder;
    private readonly RenderCounters counters;

    public RetainedRenderer(RetainedRenderCache renderCache, DrawCommandListBuilder builder, RenderCounters counters)
    {
        this.renderCache = renderCache ?? throw new ArgumentNullException(nameof(renderCache));
        this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
        this.counters = counters ?? throw new ArgumentNullException(nameof(counters));
    }

    public DrawCommandList Commit(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (!renderCache.IsRootValid)
        {
            builder.Build(root, renderCache, counters);
        }

        return renderCache.RootCommands;
    }

    public DrawCommandList Render(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (!renderCache.IsRootValid)
        {
            throw new InvalidOperationException("Root command list is not committed. Call RetainedRenderer.Commit during update before rendering or submitting.");
        }

        return renderCache.RootCommands;
    }

    public void Submit(UIRoot root, IDrawingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        backend.Render(Render(root));
    }
}
```

- [ ] **Step 4: Document backend read-only contract**

Replace `Drawing/IDrawingBackend.cs` with:

```csharp
namespace Cerneala.Drawing;

public interface IDrawingBackend
{
    // Backends must treat the submitted command list as read-only for the duration of Render.
    // Retained UI may reuse the same command-list instance across unchanged draw frames.
    void Render(DrawCommandList commands);
}
```

- [ ] **Step 5: Update existing retained renderer tests**

In `tests/Cerneala.Tests/UI/Rendering/RetainedRendererTests.cs`:

Replace direct calls like:

```csharp
DrawCommandList commands = root.RetainedRenderer.Render(root);
```

with:

```csharp
root.ProcessFrame();
DrawCommandList commands = root.RetainedRenderer.Commit(root);
```

Replace `BackendCannotMutateCachedRootCommandsDuringSubmit` with this test:

```csharp
[Fact]
public void SubmitPassesCommittedRootCommandsByReference()
{
    UIRoot root = new();
    root.VisualChildren.Add(new RenderingTestElement(Color.White));
    root.ProcessFrame();
    DrawCommandList committed = root.RetainedRenderer.Commit(root);
    CapturingDrawingBackend backend = new();

    root.RetainedRenderer.Submit(root, backend);

    Assert.Same(committed, backend.LastCommands);
    Assert.Single(committed);
}

private sealed class CapturingDrawingBackend : IDrawingBackend
{
    public DrawCommandList? LastCommands { get; private set; }

    public void Render(DrawCommandList commands)
    {
        LastCommands = commands;
    }
}
```

Delete the old `MutatingDrawingBackend` from this test file.

- [ ] **Step 6: Run retained renderer tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedRendererTests|FullyQualifiedName~RetainedRendererDrawPurityTests|FullyQualifiedName~RenderBackdoorContractTests"
```

Expected: all filtered tests pass.

- [ ] **Step 7: Commit explicit retained renderer contract**

```powershell
git add UI\Rendering\RetainedRenderer.cs Drawing\\IDrawingBackend.cs tests\Cerneala.Tests\UI\Rendering\RetainedRendererTests.cs tests\Cerneala.Tests\UI\Rendering\RetainedRendererDrawPurityTests.cs
git commit -m "fix: make retained renderer commit explicit"
```

---

### Task 4: Commit Root Commands During `UiHost.Update`

**Files:**
- Modify: `UI/Hosting/UiHost.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/UiHostFrameStatsIntegrityTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Hosting/UiHostFrameContractTests.cs`

- [ ] **Step 1: Add frame stats integrity tests**

Create `tests/Cerneala.Tests/UI/Hosting/UiHostFrameStatsIntegrityTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiHostFrameStatsIntegrityTests
{
    [Fact]
    public void UpdateCommitsRootCommandsAfterCountingRenderCacheWork()
    {
        UiHost host = HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child);

        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        DrawCommandList committed = root.RetainedRenderer.Render(root);

        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.Equal(1, child.RenderCount);
        Assert.NotEmpty(committed);
    }

    [Fact]
    public void RenderInvalidationAfterUpdateIsNotProcessedUntilNextUpdate()
    {
        UiHost host = HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child);
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterFirstUpdate = child.RenderCount;

        child.Invalidate(InvalidationFlags.Render, "after update");

        Assert.Throws<InvalidOperationException>(() => root.RetainedRenderer.Render(root));

        UiFrame next = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        DrawCommandList committed = root.RetainedRenderer.Render(root);

        Assert.True(next.Stats.RenderedElements > 0);
        Assert.Equal(renderCountAfterFirstUpdate + 1, child.RenderCount);
        Assert.NotEmpty(committed);
    }

    private static UiHost HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child)
    {
        root = new UIRoot();
        child = new RenderCountingElement();
        root.VisualChildren.Add(child);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private sealed class RenderCountingElement : UIElement
    {
        public int RenderCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
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

- [ ] **Step 2: Run host integrity tests and verify RED**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostFrameStatsIntegrityTests"
```

Expected: tests fail until `UiHost.Update(...)` calls `Commit(...)` instead of lazy `Render(...)`.

- [ ] **Step 3: Change `UiHost.Update(...)` to commit**

In `UI/Hosting/UiHost.cs`, replace:

```csharp
FrameStats stats = currentRoot.ProcessFrame();
currentRoot.RetainedRenderer.Render(currentRoot);
LastFrame = new UiFrame(elapsedTime ?? Clock?.GetElapsedTime() ?? TimeSpan.Zero, this.viewport, inputFrame, stats);
```

with:

```csharp
FrameStats stats = currentRoot.ProcessFrame();
currentRoot.RetainedRenderer.Commit(currentRoot);
LastFrame = new UiFrame(elapsedTime ?? Clock?.GetElapsedTime() ?? TimeSpan.Zero, this.viewport, inputFrame, stats);
```

- [ ] **Step 4: Update `UiHostFrameContractTests` draw purity expectations**

Keep `DrawSubmitsCachedCommandsWithoutReRendering` and `DrawDoesNotRegenerateRenderCacheAfterPostUpdateInvalidation`, but adjust the second test to assert draw throws when the root cache was invalidated after update:

```csharp
[Fact]
public void DrawDoesNotRegenerateRenderCacheAfterPostUpdateInvalidation()
{
    UiHost host = HostWithRenderableRoot(out _, out RenderCountingElement child);
    FakeDrawingBackend backend = new();
    host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
    int renderCountAfterUpdate = child.RenderCount;

    child.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "after update");

    Assert.Throws<InvalidOperationException>(() => host.Draw(backend));
    Assert.Equal(renderCountAfterUpdate, child.RenderCount);
    Assert.Equal(0, backend.RenderCalls);
}
```

- [ ] **Step 5: Run host tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostFrameContractTests|FullyQualifiedName~UiHostFrameStatsIntegrityTests"
```

Expected: all filtered tests pass.

- [ ] **Step 6: Commit update commit behavior**

```powershell
git add UI\Hosting\UiHost.cs tests\Cerneala.Tests\UI\Hosting\UiHostFrameContractTests.cs tests\Cerneala.Tests\UI\Hosting\UiHostFrameStatsIntegrityTests.cs
git commit -m "fix: commit retained commands during update"
```

---

### Task 5: Update Remaining Tests That Used Lazy Rendering

**Files:**
- Search and modify tests under `tests/Cerneala.Tests`

- [ ] **Step 1: Find remaining lazy render calls**

Run:

```powershell
rg -n "RetainedRenderer\\.Render\\(|RetainedRenderer\\.Submit\\(" tests UI Playground
```

Expected: identify tests or runtime call sites still assuming `Render(...)` builds commands.

- [ ] **Step 2: Apply the standard test migration**

For tests that need a committed root command list, use:

```csharp
root.ProcessFrame();
DrawCommandList commands = root.RetainedRenderer.Commit(root);
```

For tests that intentionally verify draw/submission after host update, use:

```csharp
host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
host.Draw(backend);
```

For tests that intentionally verify invalid-state behavior, use:

```csharp
Assert.Throws<InvalidOperationException>(() => root.RetainedRenderer.Render(root));
```

- [ ] **Step 3: Run all rendering and hosting tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Rendering|FullyQualifiedName~Hosting"
```

Expected: all filtered tests pass.

- [ ] **Step 4: Commit migrated tests**

```powershell
git add tests\Cerneala.Tests UI Playground
git commit -m "test: update retained render contract expectations"
```

If `git diff --cached --name-only` includes unrelated files, unstage them before committing:

```powershell
git restore --staged <path>
```

---

### Task 6: Update Plan Checkboxes And Documentation

**Files:**
- Modify: `AUDIT_FIX_PLAN.md`
- Modify: `ROADMAPv2_AUDIT.md` if a finding is now fixed or clarified

- [ ] **Step 1: Update `AUDIT_FIX_PLAN.md` Plan 1 checklist**

In `AUDIT_FIX_PLAN.md`, change these Plan 1 items from `[ ]` to `[x]`:

```markdown
- [x] Make `RenderQueueProcessor` the only production path that can rebuild local element render caches.
- [x] Make `DrawCommandListBuilder` compose only already-valid local caches.
- [x] Remove the `ElementRenderCache.Ensure(...)` backdoor from root composition.
- [x] Make root command-list composition an explicit update commit step or a counted frame phase.
- [x] Make `UiHost.Draw(...)` submit only the last committed root command list.
- [x] Remove per-draw command-list copying or replace it with a clear read-only backend contract.
- [x] Add `tests/Cerneala.Tests/UI/Rendering/RenderBackdoorContractTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Rendering/RetainedRendererDrawPurityTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Hosting/UiHostFrameStatsIntegrityTests.cs`.
```

- [ ] **Step 2: Add a completion note to `ROADMAPv2_AUDIT.md`**

Under `## Must Fix` > `### 2. Renderer has a backdoor...`, add this short note after the task list:

```markdown
Implementation note: fixed by `fix-retained-render-frame-contract`; local render-cache generation is scheduler-owned, root command-list composition is explicit during update, and draw submission uses the last committed root commands.
```

Only add this note after the full test suite passes.

- [ ] **Step 3: Run markdown reference check**

Run:

```powershell
rg -n "fix-retained-render-frame-contract|RenderBackdoorContractTests|RetainedRendererDrawPurityTests|UiHostFrameStatsIntegrityTests" AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md docs\superpowers\plans\2026-07-03-fix-retained-render-frame-contract.md
```

Expected: all three docs reference the completed plan and tests.

- [ ] **Step 4: Commit docs**

```powershell
git add AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md docs\superpowers\plans\2026-07-03-fix-retained-render-frame-contract.md
git commit -m "docs: plan retained render frame contract fix"
```

---

### Task 7: Full Verification

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
git commit -m "fix: complete retained render frame contract"
```

---

## Self-Review

### Spec Coverage

- `RenderQueueProcessor` as only local-cache rebuild path: covered by Tasks 1-2.
- `DrawCommandListBuilder` pure composition: covered by Tasks 1-2.
- No `ElementRenderCache.Ensure(...)` in root composition: covered by Tasks 1-2.
- Explicit update commit: covered by Tasks 3-4.
- `UiHost.Draw(...)` submits last committed output only: covered by Tasks 3-4.
- Per-draw copy removed/read-only backend contract: covered by Task 3.
- Required tests: covered by Tasks 1, 3, and 4.

### Placeholder Scan

No steps use placeholder language. Each code-changing step includes exact file paths and concrete code snippets.

### Type Consistency

- `RetainedRenderer.Commit(UIRoot root)` is introduced in Task 3 and used consistently afterwards.
- `RetainedRenderer.Render(UIRoot root)` is retained as the committed-output getter.
- `ElementRenderCache.GetValidCommands(UIElement element)` is introduced in Task 2 and used only by `DrawCommandListBuilder`.
