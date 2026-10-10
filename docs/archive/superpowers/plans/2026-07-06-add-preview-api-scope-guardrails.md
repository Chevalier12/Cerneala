# Add Preview API Scope Guardrails Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make Developer Preview scope explicit and enforceable. Cerneala has many prototype namespaces and WPF-familiar names. That is fine only if supported APIs, deferred APIs, and frozen experimental APIs are clearly separated and guarded by tests so the project does not drift into WPF legacy or unsupported feature sprawl.

**Architecture:** Add documentation and architecture tests, not a package split. Public code can remain in the single project, but the preview-supported surface and frozen/deferred surface must be visible. Do not rename large API areas in this plan. Do not add source generation, compatibility layers, or new analyzers unless tests prove a simple source scan is insufficient.

**Tech Stack:** C#/.NET 8, xUnit architecture tests, markdown docs, existing `ROADMAPv2.md`, `architecture.md`, `docs/architecture-v2.md`.

---

## File Structure

- Create: `docs/developer-preview-scope.md`
  - Defines supported Developer Preview surface and deferred/frozen surface.
- Create: `tests/Cerneala.Tests/Architecture/DeveloperPreviewScopeTests.cs`
  - Guards docs and source boundaries.
- Modify: `ROADMAPv2.md`
  - Only precise maturity/status updates for contracts proven by Core/Authoring/Runtime/Developer Preview gates.
- Modify only if needed: `docs/architecture-v2.md`
  - Add a short Developer Preview scope note if not already covered.

## Important Existing Behavior

- `ROADMAPv2.md` already distinguishes MVP/Core/Later/Optional/Experimental.
- Markup/sourcegen, advanced input categories, advanced rendering/media, animation expansion, native accessibility adapters, and package split are explicitly deferred/frozen.
- Architecture tests already guard backend namespace boundaries and package split claims.
- The repo intentionally keeps WPF-familiar names when they help ergonomics, but not compatibility behavior.

Target behavior:

- There is one concise Developer Preview scope document.
- Tests verify that Developer Preview docs name the supported and deferred surfaces.
- Tests verify Runtime/Authoring/Developer Preview samples do not depend on frozen namespaces like `UI.Markup`, `UI.Animation`, advanced `UI.Media` effects, native platform accessibility, or sourcegen.
- Tests verify string-path binding remains unsupported in hot-path docs and samples.
- Tests verify no docs claim package split or native accessibility adapters are complete before files/tests exist.

## Rules

- [ ] Do not split projects/packages.
- [ ] Do not make frozen APIs internal in this plan.
- [ ] Do not remove prototype files.
- [ ] Do not add an analyzer package.
- [ ] Do not rename WPF-like types just for aesthetics.
- [ ] Do not mark broad roadmap sections complete because a narrow gate passed.
- [ ] Do not expand deferred features while documenting them.

---

### Task 1: Add RED Scope Guard Tests

**Files:**
- Create: `tests/Cerneala.Tests/Architecture/DeveloperPreviewScopeTests.cs`

- [ ] **Step 1: Add docs existence/content tests**

Create tests:

```csharp
DeveloperPreviewScopeDocumentExists()
DeveloperPreviewScopeNamesSupportedCoreAuthoringRuntimeSurfaces()
DeveloperPreviewScopeNamesDeferredFrozenSurfaces()
DeveloperPreviewScopeStatesCodeFirstNoXamlFirstCore()
DeveloperPreviewScopeStatesStringPropertyPathsAreUnsupportedInCoreHotPath()
```

- [ ] **Step 2: Add sample dependency guard tests**

Create tests:

```csharp
PreviewSamplesDoNotReferenceMarkupOrSourceGeneration()
PreviewSamplesDoNotReferenceAnimationOrAdvancedMediaEffects()
PreviewSamplesDoNotReferenceNativeAccessibilityAdapters()
PreviewSamplesUseTypedBindingOperationsInsteadOfStringPropertyPaths()
PreviewSamplesUseDefaultThemeInsteadOfHardCodingAllControlChrome()
```

Scan `Playground/Cerneala.Playground/Samples/*.cs` and relevant preview tests. Keep scans simple and ignore comments/strings if existing helper code is available.

- [ ] **Step 3: Add roadmap claim tests**

Create tests:

```csharp
RoadmapDoesNotClaimPackageSplitProjectsImplementedWhenFilesDoNotExist()
RoadmapDoesNotClaimNativeAccessibilityAdaptersScenarioComplete()
RoadmapKeepsMarkupSourceGenMarkedOptionalOrFrozen()
RoadmapKeepsAdvancedRenderingEffectsMarkedDeferredUntilBackendSupported()
```

Reuse logic from existing architecture tests where possible.

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DeveloperPreviewScopeTests"
```

Expected: RED because `docs/developer-preview-scope.md` does not exist yet and some scope language is not guarded.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Architecture\DeveloperPreviewScopeTests.cs
git commit -m "test: capture developer preview scope guardrails"
```

---

### Task 2: Create Developer Preview Scope Document

**Files:**
- Create: `docs/developer-preview-scope.md`

- [ ] **Step 1: Document supported surface**

Keep it concise and concrete:

```text
Retained tree
Typed UiProperty<T>
Invalidation/frame scheduler
Drawing command cache
Input/routed events/focus/commands/input bindings
Style/theme/default button template
Core controls used by Authoring/Runtime samples
Typed ObservableValue/ObservableList/BindingOperations
TextBlock and single-line TextBox MVP
ItemsControl/ListBox/ScrollViewer retained list path
Resources/image cache/font resources
MonoGame runtime adapter
Platform services seams for cursor/clipboard/etc.
Platform-neutral semantics tree
Diagnostics and preview samples
```

- [ ] **Step 2: Document deferred/frozen surface**

Explicitly list:

```text
Package split
Native accessibility adapters
Full IME/multiline/rich text
Markup/sourcegen expansion
String-path binding as core hot path
Advanced rendering/effects/path rendering/render targets
Animation/storyboard expansion
Advanced input categories beyond platform-backed seams
```

- [ ] **Step 3: Document naming stance**

State that WPF-like names are ergonomic, not compatibility promises.

- [ ] **Step 4: Document retained game-loop contract**

State:

```text
Update may run every frame.
Draw may run every frame.
Layout/render command generation must be invalidation-driven.
Unchanged frames should report no retained work.
Draw must not mutate retained work.
```

---

### Task 3: Update ROADMAPv2 Precisely

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Mark only proven narrow contracts**

Update lines for Runtime Preview, command state, typed binding, TextBox MVP, default template, observable ItemsSource, retained semantics, and runtime seams only if tests actually prove them.

- [ ] **Step 2: Keep deferred work deferred**

Do not mark broad Later/Optional sections scenario-complete.

- [ ] **Step 3: Add Developer Preview hardening checkpoint**

If useful, add a short checklist under implementation order or project memory naming this hardening batch.

---

### Task 4: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted scope tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DeveloperPreviewScopeTests|FullyQualifiedName~NamespaceBoundaryTests|FullyQualifiedName~MonoGameDependencyBoundaryTests"
```

Expected: GREEN.

- [ ] **Step 2: Run roadmap/docs adjacent tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RepositoryShapeTests|FullyQualifiedName~Architecture"
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
git add docs\developer-preview-scope.md ROADMAPv2.md tests\Cerneala.Tests\Architecture\DeveloperPreviewScopeTests.cs
git commit -m "docs: add developer preview scope guardrails"
```
