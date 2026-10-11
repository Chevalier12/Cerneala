# Runtime Diagnostics And Playground Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make runtime behavior inspectable in the Playground and tests. Cerneala already has strong retained-frame diagnostics, but Runtime Preview needs diagnostics for scale, input cache reuse, render command counts, backend/resource cache activity, and platform service availability in one visible sample path.

**Architecture:** Diagnostics must observe existing retained/runtime systems; they must not become new state owners. Keep diagnostics backend-neutral except for adapter-specific counters under adapter folders. The Playground can compose diagnostics into samples, but controls/core must not depend on Playground types.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Diagnostics`, `UI/Invalidation/FrameStats`, `Playground/Cerneala.Playground/Samples`, retained host tests.

---

## File Structure

- Modify: `UI/Invalidation/FrameStats.cs`
  - Add counters only if they reflect existing work accurately.
- Modify: `UI/Diagnostics/FrameDiagnostics.cs`
  - Include command-state/style/inherited counts and scale-related snapshot data if available.
- Modify: `UI/Diagnostics/InputDiagnostics.cs`
  - Expose input cache version/rebuild data if not already available.
- Modify: `UI/Diagnostics/RenderDiagnostics.cs`
  - Expose root command count/render cache reuse data.
- Create: `UI/Diagnostics/RuntimeDiagnostics.cs`
  - Aggregate frame, viewport, input, render, resource, and platform summary.
- Modify: `Playground/Cerneala.Playground/Samples/InvalidationStatsOverlay.cs`
  - Rename internally or extend output to include runtime diagnostics while preserving tests.
- Create: `Playground/Cerneala.Playground/Samples/RuntimePreviewSample.cs`
  - A focused runtime sample using scale-aware viewport, image resource, TextBox, Button, ListBox, cursor/clipboard when available.
- Modify: `Playground/Cerneala.Playground/Samples/SampleSelector.cs`
- Modify: `Playground/Cerneala.Playground/Game1.cs`
  - Register runtime sample if sample selection is centralized there.
- Create: `tests/Cerneala.Tests/UI/Diagnostics/RuntimeDiagnosticsTests.cs`
- Create: `tests/Cerneala.Tests/Playground/Samples/RuntimePreviewSampleContractTests.cs`

## Important Existing Behavior

- `FrameStats` tracks inherited, command-state, style, measure, arrange, render-cache, hit-test, reused caches, and no-work frames.
- `FrameDiagnostics.Format(...)` currently omits some newer phase counts.
- `InvalidationStatsOverlay` displays selected frame stats in Playground.
- `AuthoringAppSample` proves authoring behavior, not runtime adapter/resource/platform behavior.
- Runtime scale/resource/platform plans should have added concrete contracts before this plan runs.

Target behavior:

- Runtime diagnostics show all retained phases that matter now, not only early MVP measure/arrange/render counters.
- Diagnostics can report viewport width/height/scale.
- Diagnostics can report root command count or render-cache summary without forcing a rebuild.
- Diagnostics can report input cache rebuild state/counters if available.
- Diagnostics can report image cache load count through test/runtime service diagnostics if available.
- Playground has a Runtime Preview sample that dogfoods the new runtime seams.

## Rules

- [ ] Do not make diagnostics mutate retained UI state except their own overlay text.
- [ ] Do not add expensive full-tree scans every frame for diagnostics.
- [ ] Do not expose adapter internals as public stable API unless there is a clear diagnostic wrapper.
- [ ] Do not remove AuthoringAppSample or Core Preview samples.
- [ ] Do not add flashy demo-only features.

---

### Task 1: Add RED Runtime Diagnostics Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Diagnostics/RuntimeDiagnosticsTests.cs`

- [ ] **Step 1: Add diagnostics snapshot tests**

Create tests:

```csharp
RuntimeDiagnosticsSnapshotIncludesViewportScale()
RuntimeDiagnosticsSnapshotIncludesAllRetainedPhaseCounts()
RuntimeDiagnosticsSnapshotIncludesRenderCommandCountWithoutRebuild()
RuntimeDiagnosticsFormatIncludesCommandStateStyleAndHitTestCounts()
RuntimeDiagnosticsCaptureDoesNotInvalidateRoot()
```

- [ ] **Step 2: Add input/resource diagnostics tests if seams exist**

Create tests:

```csharp
RuntimeDiagnosticsIncludesInputCacheReuseWhenAvailable()
RuntimeDiagnosticsIncludesImageCacheLoadCountWhenAvailable()
RuntimeDiagnosticsHandlesMissingOptionalServices()
```

If resource/input diagnostics do not have counters yet, add minimal counters in owning classes rather than tree scans.

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RuntimeDiagnosticsTests"
```

Expected: RED because aggregate runtime diagnostics do not exist.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Diagnostics\RuntimeDiagnosticsTests.cs
git commit -m "test: capture runtime diagnostics contract"
```

---

### Task 2: Implement Runtime Diagnostics Snapshot

**Files:**
- Modify: `UI/Diagnostics/FrameDiagnostics.cs`
- Modify: `UI/Diagnostics/InputDiagnostics.cs`
- Modify: `UI/Diagnostics/RenderDiagnostics.cs`
- Create: `UI/Diagnostics/RuntimeDiagnostics.cs`

- [ ] **Step 1: Fill missing phase counts in frame diagnostics**

Ensure diagnostics include at least:

- inherited;
- command state;
- style;
- queued measure/arrange;
- actual measure/arrange calls;
- render cache;
- hit test;
- reused caches;
- no-work frames.

- [ ] **Step 2: Add root command count diagnostic**

Use retained renderer/cache state without forcing `RenderQueueProcessor.Process(...)` or `RetainedRenderer.Commit(...)`.

- [ ] **Step 3: Add viewport fields**

Include logical width, logical height, scale, and optionally physical size if the coordinate helper can compute it.

- [ ] **Step 4: Keep optional sections nullable/empty**

Resource/platform diagnostics should gracefully report unavailable rather than throwing.

---

### Task 3: Add Runtime Preview Playground Sample

**Files:**
- Create: `Playground/Cerneala.Playground/Samples/RuntimePreviewSample.cs`
- Modify: `Playground/Cerneala.Playground/Samples/SampleSelector.cs`
- Modify: `Playground/Cerneala.Playground/Game1.cs` only if sample registration happens there.
- Create: `tests/Cerneala.Tests/Playground/Samples/RuntimePreviewSampleContractTests.cs`

- [ ] **Step 1: Add RED sample tests**

Create tests:

```csharp
RuntimePreviewSampleBuildsWithoutPlatformServices()
RuntimePreviewSampleUsesPathBackedOrResourceBackedImage()
RuntimePreviewSampleContainsTextBoxButtonAndObservableList()
RuntimePreviewSampleExposesDiagnosticsOverlayText()
RuntimePreviewSampleSecondUnchangedFrameDoesNoRetainedWork()
RuntimePreviewSampleDrawDoesNotGenerateRetainedWork()
```

- [ ] **Step 2: Build the sample from existing controls only**

Use existing controls and services:

- `Border`
- `StackPanel`/`Grid`
- `TextBlock`
- `TextBox`
- `Button`
- `Image`
- `ListBox`
- diagnostics overlay

- [ ] **Step 3: Use runtime services naturally**

The sample should demonstrate:

- viewport scale diagnostics;
- image resource resolution;
- cursor shape over Button/TextBox;
- clipboard if the platform service is provided;
- no platform dependency if services are missing.

- [ ] **Step 4: Register sample**

Add it to the existing sample selector without removing existing samples.

---

### Task 4: Polish Existing Overlay Without Lying

**Files:**
- Modify: `Playground/Cerneala.Playground/Samples/InvalidationStatsOverlay.cs`
- Modify tests that assert exact text only if needed.

- [ ] **Step 1: Include all modern phase counts**

Overlay text should not omit command-state/style/inherited phases now that they are real scheduler work.

- [ ] **Step 2: Include scale and command count**

Add useful runtime fields, but keep the overlay readable.

- [ ] **Step 3: Keep no-work frame visible**

No-work frames are the core retained invariant; do not bury that signal.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted diagnostics/sample tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RuntimeDiagnosticsTests|FullyQualifiedName~RuntimePreviewSampleContractTests"
```

Expected: GREEN.

- [ ] **Step 2: Run existing diagnostics/playground tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~FrameDiagnosticsTests|FullyQualifiedName~RenderCacheDumperTests|FullyQualifiedName~RetainedAppSampleContractTests|FullyQualifiedName~AuthoringAppSampleContractTests|FullyQualifiedName~PlaygroundSampleTests|FullyQualifiedName~Game1SourceTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Diagnostics UI\Invalidation Playground\Cerneala.Playground tests\Cerneala.Tests
git commit -m "feat: add runtime diagnostics preview sample"
```
