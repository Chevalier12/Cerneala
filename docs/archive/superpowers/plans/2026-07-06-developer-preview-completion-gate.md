# Developer Preview Completion Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Prove Cerneala Developer Preview Hardening is coherent after the preceding plans. This is not a new architecture phase. It is one integration gate proving keyboard traversal, layout authoring mutation, lifecycle cleanup, API scope guardrails, stress budgets, docs, samples, and retained game-loop invariants work together.

**Architecture:** Reuse existing preview samples and the new getting-started sample. Do not add new features just to pass this gate. If an integration test fails, fix the owning layer introduced by the previous plans.

**Tech Stack:** C#/.NET 8, xUnit, existing `UiHost`, fake drawing backend/input source, Playground samples, docs tests.

---

## File Structure

- Create: `tests/Cerneala.Tests/UI/Hosting/DeveloperPreviewContractTests.cs`
- Create: `tests/Cerneala.Tests/Architecture/DeveloperPreviewCompletionTests.cs`
- Modify: `ROADMAPv2.md`
  - Mark only the precise Developer Preview hardening contracts proven by this batch.
- Modify only if gate exposes owning-layer bugs:
  - `UI/Input/*`
  - `UI/Layout/Panels/*`
  - `UI/Elements/*`
  - `UI/Controls/*`
  - `UI/Invalidation/*`
  - `UI/Resources/*`
  - `docs/*`
  - `Playground/Cerneala.Playground/Samples/*`

## Important Existing Behavior

- Core Preview, Authoring Preview, and Runtime Preview gates are green.
- Prior plans in this index should have added Tab navigation, Grid mutation invalidation, lifecycle cleanup, API scope docs/tests, stress budget tests, and getting-started docs/sample.

Target behavior:

- One Developer Preview gate verifies the product can be used and maintained without violating retained invariants.
- Docs/scope/tests do not overclaim deferred features.
- Full suite remains green.
- Archive step is run with the existing script.

## Rules

- [ ] Do not redesign architecture.
- [ ] Do not add package split.
- [ ] Do not expand markup/sourcegen.
- [ ] Do not implement native accessibility adapters.
- [ ] Do not expand full IME/multiline/rich text.
- [ ] Do not add advanced rendering/effects.
- [ ] Do not weaken Core/Authoring/Runtime preview tests.
- [ ] Do not turn this gate into a new feature batch.

---

### Task 1: Add RED Developer Preview Gate Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Hosting/DeveloperPreviewContractTests.cs`
- Create: `tests/Cerneala.Tests/Architecture/DeveloperPreviewCompletionTests.cs`

- [ ] **Step 1: Add integrated behavior tests**

Create tests:

```csharp
DeveloperPreviewGettingStartedSampleFirstFrameDoesWorkAndSecondFrameDoesNoWork()
DeveloperPreviewDrawLoopDoesNotGenerateRetainedWork()
DeveloperPreviewTabNavigationWorksInGettingStartedSample()
DeveloperPreviewTextInputCommandAndObservableListWorkTogether()
DeveloperPreviewGridDefinitionMutationInvalidatesThenSettles()
DeveloperPreviewDetachingSampleStopsExternalNotifications()
```

- [ ] **Step 2: Add scope/docs tests**

Create tests:

```csharp
DeveloperPreviewDocsExistAndNameSupportedSurface()
DeveloperPreviewDocsDoNotClaimDeferredFeaturesComplete()
DeveloperPreviewRoadmapRecordsDeveloperPreviewHardeningWithoutClosingDeferredSections()
DeveloperPreviewArchiveScriptExistsAtExpectedPath()
```

- [ ] **Step 3: Add stress aggregate tests**

Create tests:

```csharp
DeveloperPreviewStressBudgetsArePresent()
DeveloperPreviewCoreAuthoringRuntimeAndGettingStartedSamplesAreRegistered()
DeveloperPreviewAllPreviewSamplesAvoidFrozenNamespaces()
```

The aggregate tests should not duplicate all stress assertions; they should ensure the gate tests exist and are included in normal test runs.

- [ ] **Step 4: Run targeted tests and verify RED or current GREEN**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DeveloperPreviewContractTests|FullyQualifiedName~DeveloperPreviewCompletionTests"
```

Expected: RED until final docs/roadmap/sample integration is complete. If GREEN immediately, inspect the tests for meaningful coverage before proceeding.

- [ ] **Step 5: Commit gate tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\DeveloperPreviewContractTests.cs tests\Cerneala.Tests\Architecture\DeveloperPreviewCompletionTests.cs
git commit -m "test: add developer preview completion gate"
```

---

### Task 2: Fix Final Integration Issues At Owning Layers

**Files:**
- Modify only files required by failing Developer Preview gate tests.

- [ ] **Step 1: If Tab behavior fails, fix `UI/Input` navigation**

Do not special-case the sample.

- [ ] **Step 2: If Grid mutation fails, fix `UI/Layout/Panels/Grid*`**

Do not force sample-specific layout refresh.

- [ ] **Step 3: If detach cleanup fails, fix lifecycle/resource/control ownership**

Do not hide leaks by weakening tests.

- [ ] **Step 4: If docs/scope fail, update docs/roadmap precisely**

Do not overclaim deferred features.

- [ ] **Step 5: If stress budgets fail, fix invalidation ownership**

Do not loosen draw/update purity invariants.

---

### Task 3: Update Project Memory Precisely

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Add Developer Preview hardening checkpoint**

Record:

```text
Tab focus navigation
Grid definition mutation invalidation
Retained lifecycle/subscription cleanup
Developer Preview scope guardrails
Retained stress budget gates
Getting Started docs/sample
Developer Preview completion gate
```

- [ ] **Step 2: Keep Later/Optional deferred state honest**

Do not mark package split, native accessibility adapters, full IME, markup/sourcegen, animation expansion, or advanced rendering scenario-complete.

- [ ] **Step 3: Ensure roadmap tests still pass**

Roadmap text should remain machine-checkable by existing architecture tests.

---

### Task 4: Verify GREEN And Full Completion

- [ ] **Step 1: Run Developer Preview gate**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DeveloperPreviewContractTests|FullyQualifiedName~DeveloperPreviewCompletionTests"
```

Expected: GREEN.

- [ ] **Step 2: Run all new batch tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~KeyboardNavigationContractTests|FullyQualifiedName~TabNavigationFrameContractTests|FullyQualifiedName~GridDefinitionMutationTests|FullyQualifiedName~GridAuthoringFrameContractTests|FullyQualifiedName~RetainedLifecycleCleanupTests|FullyQualifiedName~DetachedResourceDependencyCleanupTests|FullyQualifiedName~DetachedQueuedElementTests|FullyQualifiedName~DeveloperPreviewScopeTests|FullyQualifiedName~RetainedStressBudgetTests|FullyQualifiedName~RenderStressBudgetTests|FullyQualifiedName~ListStressBudgetTests|FullyQualifiedName~SemanticsStressBudgetTests|FullyQualifiedName~GettingStartedDocsTests|FullyQualifiedName~GettingStartedSampleContractTests|FullyQualifiedName~DeveloperPreviewContractTests|FullyQualifiedName~DeveloperPreviewCompletionTests"
```

Expected: GREEN.

- [ ] **Step 3: Run existing preview gates**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~RuntimePreviewContractTests"
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
git add UI docs Playground\Cerneala.Playground tests\Cerneala.Tests ROADMAPv2.md
git commit -m "feat: add developer preview completion gate"
```

---

## Archive step

After all plans are GREEN and both full test commands pass, archive the repository with the existing script from the repository root. Do not invent a new archive script.

```powershell
cd C:\Users\Shadow\Desktop\Cerneala
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\scripts\Archive-Repo.ps1 -RepoRoot .
```

Expected output: a new archive path under `artifacts/archives`.
