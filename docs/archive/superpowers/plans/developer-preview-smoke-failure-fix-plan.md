# Developer Preview Playground Smoke Failure Fix Plan

> Repository archive inspected: `/mnt/data/Cerneala-repo-20260706-130941.zip`  
> Original local archive path: `C:\Users\Shadow\Desktop\Cerneala\artifacts\archives\Cerneala-repo-20260706-130941.zip`  
> Failure command: `dotnet run --project Playground\Cerneala.Playground\Cerneala.Playground.csproj --no-build`

## Executive Summary

The failure is real and is not contradicted by the green unit/integration suite.

The current tests prove the retained renderer contract only for the usual test pattern:

```text
UiHost.Update(...)
UiHost.Draw(...)
```

The actual Playground violates that contract in `Game1.Update(...)` by mutating UI state **after** `UiHost.Update(...)` has already processed retained work and committed the root command list. That post-update mutation invalidates the root render cache before `Game1.Draw(...)` calls `UiHost.Draw(...)`, so `RetainedRenderer.Render(...)` correctly throws.

There is also a second app-open risk: `Game1.LoadContent(...)` builds the retained tree but does not commit an initial frame. If MonoGame calls `Draw(...)` before the first `Update(...)` on a platform/lifecycle path, the first draw will also hit the same exception.

Do **not** fix this by making `UiHost.Draw(...)` rebuild or commit lazily. The retained architecture intentionally requires update-time commit and draw-time purity. Fix the Playground/game-loop integration.

---

## Non-Cause: `FileTree.md` Regeneration

`FileTree.md` was regenerated immediately before archiving, but the failure path is runtime code only: `Game1.Draw(...) -> MonoGameUiHost.Draw(...) -> UiHost.Draw(...) -> RetainedRenderer.Submit(...)`. Nothing in that stack reads `FileTree.md`, so the regenerated file is almost certainly unrelated.

## Root Cause Hypothesis

### Primary Root Cause: Playground Mutates UI After `UiHost.Update(...)`

`Game1.Update(...)` currently does this:

```csharp
UiFrame frame = RequireUiHost().Update(GetViewport(), gameTime.ElapsedGameTime);
_sampleSelector?.UpdateFrame(frame);
base.Update(gameTime);
```

`_sampleSelector.UpdateFrame(frame)` updates the diagnostics overlay text. That writes to a `TextBlock.Text` property after the retained host has already processed work and committed root commands for the frame.

`TextBlock.TextProperty` has:

```text
AffectsMeasure | AffectsRender | AffectsSemantics
```

So the overlay update invalidates retained work after commit. The next `Draw(...)` submits an invalid root command list and fails exactly as designed.

### Secondary Root Cause: No Initial Commit After Playground Tree Composition

`Game1.LoadContent(...)` constructs the `MonoGameUiHost` before the `SampleSelector` root is added, then adds UI children, but does not call `Update(...)` to commit the initial frame:

```csharp
_uiHost = new MonoGameUiHost(... Root = uiRoot ...);
...
_sampleSelector = SampleSelector.CreateDefault(...);
uiRoot.VisualChildren.Add(_sampleSelector.Root);
```

The visual child add invalidates measure/arrange/render/hit-test work, but no command list is committed until a later `Update(...)`. If MonoGame issues a first `Draw(...)` before first `Update(...)`, the same retained-renderer exception is expected.

---

## Evidence From Code

### `RetainedRenderer` Requires Update-Time Commit

File: `UI/Rendering/RetainedRenderer.cs`

Relevant behavior:

```csharp
public DrawCommandList Render(UIRoot root)
{
    ArgumentNullException.ThrowIfNull(root);
    if (!renderCache.IsRootValid)
    {
        throw new InvalidOperationException("Root command list is not committed. Call RetainedRenderer.Commit during update before rendering or submitting.");
    }

    return renderCache.RootCommands;
}
```

This is correct for the retained architecture. Draw must not regenerate layout or render commands.

### `UiHost.Update(...)` Commits; `UiHost.Draw(...)` Only Submits

File: `UI/Hosting/UiHost.cs`

`Update(...)` processes retained work and commits:

```csharp
if (currentRoot.Scheduler.HasWork)
{
    currentRoot.ProcessFrame(stats: stats);
}
...
currentRoot.RetainedRenderer.Commit(currentRoot);
```

`Draw(...)` only submits:

```csharp
UIRoot currentRoot = RequireRoot();
currentRoot.RetainedRenderer.Submit(currentRoot, backend);
```

This is also correct. The bug is in app integration order, not in the renderer contract.

### `Game1.Update(...)` Mutates UI After Commit

File: `Playground/Cerneala.Playground/Game1.cs`

Current order:

```csharp
UiFrame frame = RequireUiHost().Update(GetViewport(), gameTime.ElapsedGameTime);
_sampleSelector?.UpdateFrame(frame);
base.Update(gameTime);
```

This is the primary bug. `UpdateFrame(...)` belongs before the next retained update, not after the current retained update.

### `SampleSelector.UpdateFrame(...)` Changes Overlay Text

File: `Playground/Cerneala.Playground/Samples/SampleSelector.cs`

```csharp
public void UpdateFrame(UiFrame? frame)
{
    statsOverlay.Update(frame);
}
```

File: `Playground/Cerneala.Playground/Samples/InvalidationStatsOverlay.cs`

```csharp
public void Update(UiFrame? frame)
{
    if (frame is null)
    {
        return;
    }

    text.Text = Format(frame);
}
```

That `text.Text = ...` is a retained UI mutation.

### Tests Already Intentionally Defend Draw Purity

File: `tests/Cerneala.Tests/UI/Hosting/UiHostFrameContractTests.cs`

There is already a test proving that drawing after a post-update invalidation should throw instead of silently rebuilding:

```csharp
child.Invalidate(InvalidationFlags.Render, "after update");
Assert.Throws<InvalidOperationException>(() => host.Draw(backend));
```

This means the real Playground is doing the exact kind of post-update mutation that tests already define as invalid for draw.

---

## Why Tests Passed

The suite passed because it did not cover the actual Playground game-loop ordering.

The existing tests mostly do one of these:

```text
host.Update(...)
host.Draw(...)
```

or they directly call:

```text
root.RetainedRenderer.Commit(root)
```

They do not simulate this real app sequence:

```text
LoadContent creates host
LoadContent mutates retained tree after host creation
Update commits retained commands
Game1 mutates diagnostics overlay after commit
Draw submits invalid root commands
```

The `Game1SourceTests` are useful but currently only assert that `Game1` calls `Update(...)`, `Draw(...)`, resource setup, and sample selector setup. They do not assert safe ordering around diagnostics overlay updates or first-draw priming.

---

## Related Bug Risks Discovered

- [ ] **First Draw before first Update:** `Game1.LoadContent(...)` does not commit the composed retained tree before the first possible `Draw(...)`.
- [ ] **Post-update app mutations:** any app code that mutates retained UI after `UiHost.Update(...)` but before `UiHost.Draw(...)` will reproduce this failure.
- [ ] **Diagnostics overlay loop:** if the overlay is updated after every frame commit, it will invalidate render every frame and can permanently prevent draw from succeeding.
- [ ] **Root replacement before draw:** `UiHost.SetRoot(...)` primes the next frame, but drawing before that update will throw. That is correct at core level, but app adapters/samples must not do it.
- [ ] **Sample selection outside update:** `SampleSelector.SelectSample(...)` invalidates the active sample host. If called externally after update and before draw, draw will throw. If called from input command during `UiHost.Update(...)`, it should be safe because `UiHost.Update(...)` has a post-input processing pass.
- [ ] **SpriteBatch cleanup on draw exceptions:** `MonoGameUiHost.Draw(...)` calls `spriteBatch.Begin(...)`, then `host.Draw(...)`, then `spriteBatch.End(...)`. If `host.Draw(...)` throws, `End(...)` is skipped. This can leave MonoGame render state dirty after an exception. It does not cause the root bug, but it is a real hardening fix.
- [ ] **No bounded app-open smoke command:** the repo has strong unit gates but no automated or semi-automated Playground open smoke that exits after a successful first draw.

---

## Minimal Fix Plan

### Task 1: Add RED Tests For The Real Failure Shape

Create:

```text
tests/Cerneala.Tests/Playground/PlaygroundGameLoopSmokeTests.cs
```

Add tests:

```csharp
StatsOverlayUpdatedAfterHostUpdateInvalidatesDrawLikeRealCrash()
StatsOverlayUpdatedBeforeNextHostUpdateKeepsDrawCommitted()
InitialPlaygroundTreeRequiresCommittedFrameBeforeFirstDraw()
SampleSelectionDuringInputUpdateCommitsBeforeDraw()
```

Expected initial behavior:

- `StatsOverlayUpdatedAfterHostUpdateInvalidatesDrawLikeRealCrash` should pass as a characterization test if it asserts the current failure shape.
- `StatsOverlayUpdatedBeforeNextHostUpdateKeepsDrawCommitted` should fail until the fixed ordering is implemented in the Game1/source-level contract or a small test harness.
- `InitialPlaygroundTreeRequiresCommittedFrameBeforeFirstDraw` should demonstrate that adding the selector root leaves draw uncommitted until update.
- `SampleSelectionDuringInputUpdateCommitsBeforeDraw` should pass or expose a related issue. It should prove input-triggered sample selection is safe because `UiHost.Update(...)` has a post-input retained work pass.

Use the existing fake backend and `SampleSelector.CreateDefault(...)` to avoid requiring a real graphics device.

Sketch:

```csharp
[Fact]
public void StatsOverlayUpdatedAfterHostUpdateInvalidatesDrawLikeRealCrash()
{
    UIRoot root = new(420, 320);
    SampleSelector selector = SampleSelector.CreateDefault();
    root.VisualChildren.Add(selector.Root);
    UiHost host = new(new UiHostOptions { Root = root });
    FakeDrawingBackend backend = new();

    UiFrame frame = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
    selector.UpdateFrame(frame);

    Assert.Throws<InvalidOperationException>(() => host.Draw(backend));
}
```

Then add positive tests for the fixed pattern:

```csharp
[Fact]
public void StatsOverlayUpdatedBeforeNextHostUpdateKeepsDrawCommitted()
{
    UIRoot root = new(420, 320);
    SampleSelector selector = SampleSelector.CreateDefault();
    root.VisualChildren.Add(selector.Root);
    UiHost host = new(new UiHostOptions { Root = root });
    FakeDrawingBackend backend = new();

    UiFrame first = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
    selector.UpdateFrame(first);
    host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

    host.Draw(backend);

    Assert.Equal(1, backend.RenderCalls);
    Assert.NotNull(backend.LastCommands);
}
```

Targeted command:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~PlaygroundGameLoopSmokeTests"
```

---

### Task 2: Add Source-Level Guard Tests For `Game1` Ordering

Modify:

```text
tests/Cerneala.Tests/Playground/Game1SourceTests.cs
```

Add tests:

```csharp
Game1PrimesUiHostAfterBuildingSampleTreeBeforeFirstDraw()
Game1PublishesPreviousFrameStatsBeforeCurrentUiHostUpdate()
Game1DoesNotMutateSampleSelectorStatsAfterUiHostUpdate()
```

These are source-level tests because `Game1` depends on a real MonoGame `GraphicsDevice` and is not currently constructed in unit tests. The repo already uses source tests for `Game1`, so this is consistent with existing practice.

Expected fixed source shape:

```csharp
PrimeUiFrameForFirstDraw();
```

called in `LoadContent(...)` after:

```csharp
uiRoot.VisualChildren.Add(_sampleSelector.Root);
```

and update order like:

```csharp
MonoGameUiHost host = RequireUiHost();
_sampleSelector?.UpdateFrame(host.LastFrame);
host.Update(GetViewport(), gameTime.ElapsedGameTime);
```

or equivalent. The important rule is:

```text
No retained UI mutation after host.Update(...) and before Draw(...).
```

Targeted command:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Game1SourceTests"
```

---

### Task 3: Fix `Game1` Frame Ordering

Modify:

```text
Playground/Cerneala.Playground/Game1.cs
```

#### Step 3.1: Prime The First Frame After Building The Playground Tree

After this line in `LoadContent(...)`:

```csharp
uiRoot.VisualChildren.Add(_sampleSelector.Root);
```

call a helper:

```csharp
PrimeUiFrameForFirstDraw();
```

Implement it as:

```csharp
private void PrimeUiFrameForFirstDraw()
{
    RequireUiHost().Update(CreateEmptyInputFrame(), GetViewport(), TimeSpan.Zero);
}
```

Add imports as needed:

```csharp
using Cerneala.UI.Input;
```

Add helper:

```csharp
private static InputFrame CreateEmptyInputFrame()
{
    return new InputFrame(
        PointerSnapshot.Empty,
        PointerSnapshot.Empty,
        KeyboardSnapshot.Empty,
        KeyboardSnapshot.Empty,
        []);
}
```

Do not call `_sampleSelector.UpdateFrame(...)` after this priming update. That would recreate the same post-update mutation bug.

#### Step 3.2: Move Diagnostics Overlay Mutation Before The Current Retained Update

Change `Update(...)` from:

```csharp
UiFrame frame = RequireUiHost().Update(GetViewport(), gameTime.ElapsedGameTime);
_sampleSelector?.UpdateFrame(frame);
base.Update(gameTime);
```

to:

```csharp
MonoGameUiHost host = RequireUiHost();
_sampleSelector?.UpdateFrame(host.LastFrame);
host.Update(GetViewport(), gameTime.ElapsedGameTime);
base.Update(gameTime);
```

This means the overlay displays the previous retained frame's stats. That one-frame delay is acceptable for diagnostics and keeps all UI mutations inside the next update's retained processing window.

Acceptance rule:

```text
After Game1.Update(...) returns, root.RetainedRenderCache.IsRootValid must still be true unless some external code mutated the UI after update.
```

---

### Task 4: Harden `MonoGameUiHost.Draw(...)` Exception Cleanup

Modify:

```text
UI/Hosting/MonoGame/MonoGameUiHost.cs
```

Current code:

```csharp
spriteBatch.Begin(rasterizerState: MonoGameDrawingBackend.ScissorRasterizerState);
host.Draw(drawingBackend);
spriteBatch.End();
```

Change to:

```csharp
spriteBatch.Begin(rasterizerState: MonoGameDrawingBackend.ScissorRasterizerState);
try
{
    host.Draw(drawingBackend);
}
finally
{
    spriteBatch.End();
}
```

This does not hide the exception and does not fix the root invalidation. It only prevents a draw failure from leaving `SpriteBatch` in a begun state.

Tests to add or adjust:

```text
tests/Cerneala.Tests/UI/Hosting/MonoGameUiHostBoundaryTests.cs
```

Given `SpriteBatch` is difficult to instantiate safely without a graphics device, source-level assertion is acceptable here unless an existing fake/uninitialized path can observe `End` calls.

Add:

```csharp
MonoGameUiHostDrawUsesTryFinallyAroundSpriteBatchEnd()
```

Targeted command:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MonoGameUiHostBoundaryTests"
```

---

### Task 5: Add A Bounded Playground App-Open Smoke Mode

This is not a framework feature. It is a practical regression gate for exactly this failure class.

Modify:

```text
Playground/Cerneala.Playground/Program.cs
Playground/Cerneala.Playground/Game1.cs
```

Add support for:

```powershell
dotnet run --project Playground\Cerneala.Playground\Cerneala.Playground.csproj -- --smoke-open
```

and after build:

```powershell
dotnet run --project Playground\Cerneala.Playground\Cerneala.Playground.csproj --no-build -- --smoke-open
```

Suggested minimal implementation:

```csharp
using var game = new Cerneala.Playground.Game1(args.Contains("--smoke-open", StringComparer.OrdinalIgnoreCase));
game.Run();
```

In `Game1`:

```csharp
private readonly bool _exitAfterFirstSuccessfulDraw;
private bool _smokeDrawCompleted;

public Game1(bool exitAfterFirstSuccessfulDraw = false)
{
    _exitAfterFirstSuccessfulDraw = exitAfterFirstSuccessfulDraw;
    ...
}
```

At the end of `Draw(...)`, after `RequireUiHost().Draw()` succeeds:

```csharp
if (_exitAfterFirstSuccessfulDraw && !_smokeDrawCompleted)
{
    _smokeDrawCompleted = true;
    Exit();
}
```

Source tests to add:

```csharp
ProgramPassesSmokeOpenArgumentToGame1()
Game1SmokeModeExitsAfterFirstSuccessfulDraw()
```

Manual smoke verification command:

```powershell
cd C:\Users\Shadow\Desktop\Cerneala
dotnet build Cerneala.slnx
dotnet run --project Playground\Cerneala.Playground\Cerneala.Playground.csproj --no-build -- --smoke-open
```

Expected:

```text
No unhandled Root command list exception. Process exits after first successful draw.
```

Open question: headless CI may still fail before Cerneala code if MonoGame cannot create a graphics device. Keep this as a local/manual smoke unless CI has graphics support.

---

### Task 6: Verify Root Replacement And Sample Selection Behavior

Add tests in:

```text
tests/Cerneala.Tests/Playground/PlaygroundGameLoopSmokeTests.cs
```

Tests:

```csharp
RootReplacementRequiresUpdateBeforeDrawAndGameLoopPatternCommitsIt()
SampleSelectorCommandSelectionDuringHostUpdateCommitsBeforeDraw()
ExternalSampleSelectionAfterHostUpdateStillThrowsByDesign()
```

Expected behavior:

- `RootReplacementRequiresUpdateBeforeDrawAndGameLoopPatternCommitsIt`: after `host.SetRoot(...)`, `host.Draw(...)` before update should throw, but update then draw should pass.
- `SampleSelectorCommandSelectionDuringHostUpdateCommitsBeforeDraw`: click a sample selector button through the input path; `UiHost.Update(...)` should process the mutation before commit; draw should pass.
- `ExternalSampleSelectionAfterHostUpdateStillThrowsByDesign`: calling `selector.SelectSample(...)` after update and before draw should throw on draw. This documents the core contract rather than weakening it.

Do not change `UiHost.Draw(...)` to make the third test pass.

---

## What Not To Change

- [ ] Do not make `RetainedRenderer.Render(...)` call `Commit(...)` automatically.
- [ ] Do not make `UiHost.Draw(...)` process scheduler work.
- [ ] Do not make `MonoGameUiHost.Draw(...)` call `Update(...)`.
- [ ] Do not suppress the exception or submit an empty command list when root commands are invalid.
- [ ] Do not remove the draw-purity tests.
- [ ] Do not turn the diagnostics overlay into an immediate-mode draw overlay.
- [ ] Do not hide post-update invalidation bugs by making draw mutate retained state.

The correct rule is:

```text
All retained UI mutations must happen before or during UiHost.Update(...). UiHost.Draw(...) is submit-only.
```

---

## Verification Commands

Run targeted tests first:

```powershell
cd C:\Users\Shadow\Desktop\Cerneala

dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~PlaygroundGameLoopSmokeTests"
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Game1SourceTests|FullyQualifiedName~MonoGameUiHostBoundaryTests"
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostFrameContractTests|FullyQualifiedName~RuntimePreviewContractTests|FullyQualifiedName~DeveloperPreviewContractTests"
```

Run the full suite:

```powershell
dotnet test Cerneala.slnx
dotnet test
```

Run the real smoke:

```powershell
dotnet build Cerneala.slnx
dotnet run --project Playground\Cerneala.Playground\Cerneala.Playground.csproj --no-build -- --smoke-open
```

Also manually verify the original launch command no longer crashes immediately:

```powershell
dotnet run --project Playground\Cerneala.Playground\Cerneala.Playground.csproj --no-build
```

Expected: Playground opens and draws. Close the window manually.

Archive after verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\scripts\Archive-Repo.ps1 -RepoRoot .
```

---

## Acceptance Criteria

- [ ] Real Playground launch no longer throws `Root command list is not committed` on app open.
- [ ] `Game1.LoadContent(...)` commits a valid initial frame after the retained Playground tree is composed.
- [ ] `Game1.Update(...)` does not mutate retained UI after `UiHost.Update(...)` returns.
- [ ] Diagnostics overlay still updates, accepting a one-frame delay.
- [ ] `UiHost.Draw(...)` remains submit-only.
- [ ] `RetainedRenderer.Render(...)` still throws for invalid/uncommitted root command lists.
- [ ] Draw-purity tests still pass.
- [ ] New smoke/game-loop tests cover post-update overlay mutation, first-draw priming, root replacement, and sample selection behavior.
- [ ] Bounded `--smoke-open` command exits after first successful draw.
- [ ] Full test suite remains green.

---

## Risks And Open Questions

- [ ] **MonoGame lifecycle ordering:** The observed failure may occur after a first update because of the overlay mutation, or before a first update because of platform-specific first-draw ordering. The fix should cover both.
- [ ] **Stats overlay one-frame lag:** The overlay will display the previous retained frame's stats. This is preferable to mutating UI after commit. If exact same-frame diagnostics are required later, they need a pre-commit diagnostics phase inside `UiHost.Update(...)`, not a post-update mutation.
- [ ] **Headless smoke execution:** `--smoke-open` may still require a graphics-capable environment. Keep it as a local smoke unless CI is configured for MonoGame/SDL graphics.
- [ ] **Initial input read:** Prefer explicit empty input for first-frame priming in `LoadContent(...)` to avoid reading device state before the first game tick.
- [ ] **Source tests fragility:** `Game1SourceTests` are string-based today. They are acceptable for this minimal fix because constructing `Game1` requires a real graphics device. A later cleanup could extract a small testable Playground loop coordinator, but do not do that in this fix unless necessary.
