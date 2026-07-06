# Runtime Preview Completion Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Prove Cerneala Runtime Preview is coherent after the preceding plans. This is not a new architecture phase. It is a single integration gate proving viewport scaling, MonoGame adapter contracts, resource/content lifetime, platform services, diagnostics, and retained game-loop invariants work together.

**Architecture:** Reuse the existing retained architecture. Fix owning-layer bugs discovered by integration tests; do not work around them in the sample. Do not add unrelated feature areas.

**Tech Stack:** C#/.NET 8, xUnit, Playground sample, retained `UiHost`, `MonoGameUiHost` adapter seams, fake backend/platform services.

---

## File Structure

- Create: `tests/Cerneala.Tests/UI/Hosting/RuntimePreviewContractTests.cs`
- Create: `tests/Cerneala.Tests/Playground/Samples/RuntimePreviewIntegrationTests.cs`
- Modify: `Playground/Cerneala.Playground/Samples/RuntimePreviewSample.cs`
- Modify: `ROADMAPv2.md`
  - Mark only precise runtime contracts proven by tests.
- Modify: `ROADMAPv2_AUDIT.md` only if project convention expects audit status notes.
- No project/package split files in this plan.

## Important Existing Behavior

- Core Preview and Authoring Preview gates already prove retained UI fundamentals and authoring behavior.
- Runtime plans in this batch should have added scale, backend hardening, resource lifetime, platform service integration, and runtime diagnostics.
- Full solution tests are green before this plan starts.

Target behavior:

- First runtime frame performs necessary retained work.
- Second unchanged runtime frame performs no retained work.
- Draw never generates retained work.
- Scale-aware pointer input hits the same logical elements that layout/render produced.
- MonoGame backend scale/clip helpers are covered.
- Path-backed image resource loads once and invalidates correctly on resource change.
- Cursor and clipboard platform services are optional and test-covered.
- Runtime diagnostics report the truth without forcing work.

## Rules

- [ ] Do not add new controls.
- [ ] Do not hard-code sample-only fixes.
- [ ] Do not weaken existing Core Preview or Authoring Preview tests.
- [ ] Do not mark broad roadmap sections complete unless the gate proves the exact scenario.
- [ ] Do not invent a new archive script.

---

### Task 1: Add RED Runtime Preview Gate Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Hosting/RuntimePreviewContractTests.cs`
- Create: `tests/Cerneala.Tests/Playground/Samples/RuntimePreviewIntegrationTests.cs`

- [ ] **Step 1: Add retained runtime frame tests**

Create tests:

```csharp
RuntimePreviewFirstFrameDoesRetainedWork()
RuntimePreviewSecondUnchangedFrameDoesNoRetainedWork()
RuntimePreviewDrawDoesNotGenerateRetainedWork()
RuntimePreviewScaleChangeInvalidatesExpectedWorkOnly()
RuntimePreviewDiagnosticsCaptureDoesNotCreateWork()
```

- [ ] **Step 2: Add runtime input/scale tests**

Create tests:

```csharp
RuntimePreviewScaledPointerHitsLogicalButton()
RuntimePreviewScaledPointerFocusesTextBox()
RuntimePreviewCursorPublishesHandAndIBeamForLogicalHitTargets()
RuntimePreviewExplicitInputFrameIsNotDoubleScaled()
```

- [ ] **Step 3: Add resource/content tests**

Create tests:

```csharp
RuntimePreviewPathBackedImageLoadsOnceAcrossMeasureRenderAndDraw()
RuntimePreviewImageResourceReplacementInvalidatesDependentRender()
RuntimePreviewDisposingHostDisposesOwnedImageCacheOnce()
```

- [ ] **Step 4: Add platform/text tests**

Create tests:

```csharp
RuntimePreviewTextBoxClipboardPasteUpdatesTypedBinding()
RuntimePreviewClipboardMissingDoesNotBreakTextInput()
RuntimePreviewButtonCommandStateStillRefreshesAfterClipboardTextChange()
```

- [ ] **Step 5: Add Playground sample integration tests**

Create tests:

```csharp
RuntimePreviewSampleIsRegisteredInSampleSelector()
RuntimePreviewSampleBuildsWithFakePlatformServices()
RuntimePreviewSampleBuildsWithoutPlatformServices()
RuntimePreviewSampleOverlayReportsScaleAndNoWorkFrame()
```

- [ ] **Step 6: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RuntimePreviewContractTests|FullyQualifiedName~RuntimePreviewIntegrationTests"
```

Expected: RED until the gate sample/integration assertions are fully wired.

- [ ] **Step 7: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\RuntimePreviewContractTests.cs tests\Cerneala.Tests\Playground\Samples\RuntimePreviewIntegrationTests.cs
git commit -m "test: add runtime preview completion gate"
```

---

### Task 2: Wire Gate Through Existing Runtime Sample

**Files:**
- Modify: `Playground/Cerneala.Playground/Samples/RuntimePreviewSample.cs`
- Modify: `Playground/Cerneala.Playground/Samples/SampleSelector.cs`
- Modify only if needed: `Playground/Cerneala.Playground/Game1.cs`

- [ ] **Step 1: Add test-friendly sample accessors**

Expose properties needed by tests without brittle tree searching:

```text
RootElement
NameTextBox or InputTextBox
PrimaryButton
Image
Items
ListBox
DiagnosticsOverlay
```

- [ ] **Step 2: Do not bypass real paths**

The sample must use actual:

- retained host update/draw contracts;
- resource provider and image loader/cache path;
- platform services when provided;
- typed binding and command state where relevant.

- [ ] **Step 3: Keep sample deterministic**

Avoid timers, random data, or backend-specific real file dependencies in tests. Use fake resources/loaders for tests and real optional assets only in Playground runtime if already present.

---

### Task 3: Fix Owning-Layer Bugs Exposed By Gate

**Files:**
- Modify only files owned by the failing contract.

- [ ] **Step 1: If scale/pointer fails, fix `UiViewport`, `MonoGameInputSource`, or host scale propagation**

Do not compensate in controls.

- [ ] **Step 2: If draw/clip fails, fix `MonoGameDrawingBackend` or backend helper classes**

Do not alter retained command generation unless command generation is wrong.

- [ ] **Step 3: If image loading/reload fails, fix resource/cache ownership**

Do not manually load images in the sample.

- [ ] **Step 4: If cursor/clipboard fails, fix platform service integration**

Do not call fake services directly from sample tests.

- [ ] **Step 5: If diagnostics causes work, fix diagnostics**

Diagnostics must observe state, not mutate retained caches.

---

### Task 4: Update Project Memory Conservatively

**Files:**
- Modify: `ROADMAPv2.md`
- Modify only if necessary: `ROADMAPv2_AUDIT.md`

- [ ] **Step 1: Mark exact runtime contracts**

Update only precise checklist lines that are now proven, for example:

- viewport scale coordinate contract;
- MonoGame adapter scaled pointer/render mapping;
- path-backed image resource cache/lifetime;
- cursor/clipboard platform seams wired through host/root;
- runtime diagnostics sample/gate.

- [ ] **Step 2: Do not mark deferred areas complete**

Keep these deferred unless separately implemented and tested:

- package split;
- native accessibility platform adapters;
- full IME;
- markup/sourcegen expansion;
- advanced rendering/effects.

---

### Task 5: Verify GREEN And Full Completion

- [ ] **Step 1: Run runtime gate tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RuntimePreviewContractTests|FullyQualifiedName~RuntimePreviewIntegrationTests"
```

Expected: GREEN.

- [ ] **Step 2: Run all preview gates**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~RuntimePreviewContractTests|FullyQualifiedName~RetainedVerticalSliceTests"
```

Expected: GREEN.

- [ ] **Step 3: Run runtime-adjacent targeted tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiViewportScaleContractTests|FullyQualifiedName~MonoGameDrawMapperTests|FullyQualifiedName~ImageResourceCacheTests|FullyQualifiedName~UiHostPlatformServicesIntegrationTests|FullyQualifiedName~RuntimeDiagnosticsTests"
```

Expected: GREEN.

- [ ] **Step 4: Run full solution**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 5: Run root test command**

```powershell
dotnet test
```

Expected: GREEN.

- [ ] **Step 6: Commit implementation**

```powershell
git add UI Playground\Cerneala.Playground tests\Cerneala.Tests ROADMAPv2.md ROADMAPv2_AUDIT.md
git commit -m "feat: add runtime preview completion gate"
```

---

## Archive step

After all tests are GREEN and commits are done, archive the repository with the existing script from the repository root. Do not invent a new archive script.

```powershell
cd C:\Users\Shadow\Desktop\Cerneala
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\scripts\Archive-Repo.ps1 -RepoRoot .
```

Expected output: the script writes the created archive path under the default `artifacts/archives` directory unless `-OutputDirectory` or `-ArchiveName` is explicitly supplied.
