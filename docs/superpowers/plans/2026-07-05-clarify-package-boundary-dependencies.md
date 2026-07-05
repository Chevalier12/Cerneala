# Clarify Package Boundary Dependencies Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Record that package splitting stays deferred while the current single `Cerneala.csproj` still carries MonoGame, SkiaSharp, and HarfBuzzSharp dependencies.

**Architecture:** This is a documentation-only boundary clarification. Preserve the current single-project MVP shape, keep adapter code source-isolated, and define future acceptance criteria for splitting core/adapters without creating `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, or split test projects now. Existing source-boundary tests remain valid; package-shape tests are deferred until the split criteria become executable.

**Tech Stack:** Markdown, PowerShell verification commands, existing `ROADMAPv2.md`, `ROADMAPv2_AUDIT.md`, `AUDIT_FIX_PLAN.md`, `Cerneala.csproj`, `tests/Cerneala.Tests/Architecture/MonoGameDependencyBoundaryTests.cs`, and `Cerneala.slnx`.

---

## File Structure

- Modify: `ROADMAPv2.md`
  - Clarify that the current package boundary is not backend-neutral because the single main project references MonoGame, SkiaSharp, native Skia assets, and HarfBuzzSharp.
  - Keep `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, `Cerneala.Tests.Core.csproj`, and `Cerneala.Tests.MonoGame.csproj` unchecked and deferred.
  - Add future acceptance criteria for creating the split projects.
  - State that package-shape tests should be added only when the split criteria are ready.
- Modify: `ROADMAPv2_AUDIT.md`
  - Mark the package-boundary required changes as resolved by this documentation pass.
  - Add an implementation note explaining that the dependency risk is recorded, not fixed by a project split.
  - Mark the deferred dependency note in "What can remain deferred" as acknowledged.
- Modify: `AUDIT_FIX_PLAN.md`
  - Add the detailed plan link under Plan 7.
  - Mark Plan 7 complete only after roadmap/audit edits and verification pass.

## Important Existing Context

`Cerneala.csproj` currently has these package references:

```xml
<PackageReference Include="HarfBuzzSharp" Version="14.2.0" />
<PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.4.1" />
<PackageReference Include="SkiaSharp" Version="4.148.0" />
<PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="4.148.0" />
```

`ROADMAPv2.md` already keeps the future package split files unchecked:

```markdown
- [ ] `Cerneala.Core.csproj` — optional future package split, deferred until real split work.
- [ ] `Cerneala.MonoGame.csproj` — optional future package split, deferred until real split work.
- [ ] `Cerneala.Tests.Core.csproj` — optional future package split, deferred until real split work.
- [ ] `Cerneala.Tests.MonoGame.csproj` — optional future package split, deferred until real split work.
```

`tests/Cerneala.Tests/Architecture/MonoGameDependencyBoundaryTests.cs` already includes `OptionalPackageSplitProjectsAreNotClaimedUnlessFilesExist()`, which fails if `ROADMAPv2.md` claims split projects exist before the files exist.

Do not create new `.csproj` files in this plan. Do not add `PrivateAssets`, conditional compilation, target-framework splits, or package-shape tests yet. Those would be real build-architecture work, and the audit explicitly says not to split projects just to satisfy roadmap optics.

---

### Task 1: Clarify Roadmap Package Boundary Scope

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Replace the Section 24 opening**

Replace:

```markdown
This phase keeps the core portable. The current repository has a single project with MonoGame dependencies; v2 should keep adapter code isolated and later decide whether to split packages.

Section summary: Platform contracts and boundary tests are complete; package split project files stay deferred until real split work exists.
```

with:

```markdown
This phase keeps the source architecture portable while acknowledging that the current package is not dependency-neutral yet. The repository intentionally remains a single main project for MVP, and `Cerneala.csproj` currently carries `MonoGame.Framework.DesktopGL`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`, and `HarfBuzzSharp`.

Section summary: Platform contracts and source boundary tests are complete; package split project files stay deferred until real split work exists. The dependency risk is tracked explicitly so "single project now" does not silently become the permanent package architecture.
```

- [ ] **Step 2: Add current dependency inventory after deferred project files**

After:

```markdown
- [ ] `Cerneala.Tests.MonoGame.csproj` — optional future package split, deferred until real split work.
```

insert:

```markdown
Current package dependency note:

- [x] `Cerneala.csproj` is the only main library project today.
- [x] `Cerneala.csproj` references `MonoGame.Framework.DesktopGL` for current adapter and playground integration.
- [x] `Cerneala.csproj` references `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`, and `HarfBuzzSharp` for the current text shaping/rasterization path.
- [x] These dependencies are acceptable for the MVP single-project shape, but they are not a neutral core package boundary.
```

- [ ] **Step 3: Add future split criteria before the Tests section**

Before:

```markdown
Tests:
```

insert:

```markdown
Future split acceptance criteria:

- [ ] `Cerneala.Core.csproj` can build UI core, layout, controls, retained rendering contracts, resources abstractions, platform abstractions, and backend-neutral input/drawing contracts without referencing MonoGame, SkiaSharp, native Skia assets, or HarfBuzzSharp packages.
- [ ] `Cerneala.MonoGame.csproj` owns MonoGame drawing, input, resource loading, hosting adapters, and any MonoGame-specific package references.
- [ ] The text shaping/rasterization dependency decision is explicit: either it remains in a dedicated adapter package or it is accepted as a core dependency with a documented reason.
- [ ] `Cerneala.Tests.Core.csproj` covers backend-neutral contracts without referencing MonoGame packages.
- [ ] `Cerneala.Tests.MonoGame.csproj` covers MonoGame adapter behavior and can depend on MonoGame packages.
- [ ] The playground references the adapter package, not the core package alone.
- [ ] Package-shape tests are added in the same change that creates the split projects; until then, existing source-boundary tests are the active guard.
```

- [ ] **Step 4: Replace the Section 24 acceptance checklist**

Replace:

```markdown
- [x] Platform services expose clipboard, cursor, dialogs, text input, DPI, and accessibility seams without backend dependencies.
- [x] MonoGame host integration remains adapter-scoped under `UI/Hosting/MonoGame/`.
- [x] Optional package split project files are intentionally deferred and not claimed as implemented.
- [x] Full project tests pass for this phase.
```

with:

```markdown
- [x] Platform services expose clipboard, cursor, dialogs, text input, DPI, and accessibility seams without backend-specific source dependencies.
- [x] MonoGame host integration remains adapter-scoped under `UI/Hosting/MonoGame/`.
- [x] Optional package split project files are intentionally deferred and not claimed as implemented.
- [x] The current `Cerneala.csproj` MonoGame/Skia/HarfBuzz package dependency risk is explicitly recorded.
- [x] Future split acceptance criteria distinguish source boundaries from package dependency boundaries.
- [ ] Package-shape tests are deferred until the split projects are created.
- [x] Full project tests pass for this documentation phase.
```

- [ ] **Step 5: Verify roadmap package language**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "not dependency-neutral|Current package dependency note|Future split acceptance criteria|Package-shape tests are deferred" -Context 0,8
```

Expected: output shows Section 24 records current package dependencies, keeps split files deferred, and defines future split criteria.

- [ ] **Step 6: Commit roadmap package clarification**

```powershell
git add ROADMAPv2.md
git commit -m "docs: clarify package dependency boundary"
```

---

### Task 2: Update Audit Package-Boundary Finding

**Files:**
- Modify: `ROADMAPv2_AUDIT.md`

- [ ] **Step 1: Mark the package-boundary required changes complete**

Under `### 2. Main project still has hard backend/package dependencies`, replace:

```markdown
- [ ] Keep `Cerneala.Core.csproj` and `Cerneala.MonoGame.csproj` deferred if the project is not ready.
- [ ] But record a Superpowers decision note for “single project now, adapter dependencies later” so this does not become accidental architecture.
- [ ] Consider `PrivateAssets`/conditional compilation only if it does not create build complexity.
- [ ] Add package-shape tests that distinguish source boundary from package dependency boundary.
```

with:

```markdown
- [x] Keep `Cerneala.Core.csproj` and `Cerneala.MonoGame.csproj` deferred because the project is not ready for a real package split.
- [x] Record a Superpowers decision note for "single project now, adapter dependencies later" so this does not become accidental architecture.
- [x] Defer `PrivateAssets`/conditional compilation because adding build complexity without a real split would be premature.
- [x] Defer package-shape tests until split criteria are ready and split projects exist.
```

- [ ] **Step 2: Add the implementation note**

Immediately after the required changes list from Step 1, insert:

```markdown
Implementation note: fixed by `clarify-package-boundary-dependencies`; `ROADMAPv2.md` now records that `Cerneala.csproj` intentionally remains the single MVP project while carrying `MonoGame.Framework.DesktopGL`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`, and `HarfBuzzSharp`. The future split criteria distinguish source isolation from package dependency isolation, and package-shape tests remain deferred until the split projects exist.
```

- [ ] **Step 3: Mark the ROADMAPv2_AUDIT execution checklist complete**

Under `### clarify-package-boundary-dependencies`, replace:

```markdown
- [ ] Keep package split project files deferred.
- [ ] Add explicit risk/requirement that the main package currently carries adapter dependencies.
- [ ] Define the future acceptance criteria for splitting core/adapters without forcing the split now.
```

with:

```markdown
- [x] Keep package split project files deferred.
- [x] Add explicit risk/requirement that the main package currently carries adapter dependencies.
- [x] Define the future acceptance criteria for splitting core/adapters without forcing the split now.
```

- [ ] **Step 4: Mark the deferred dependency risk acknowledged**

Under `## What can remain deferred`, replace:

```markdown
- [ ] The dependency problem behind the package split should not be deferred indefinitely. `Cerneala.csproj` currently pulls MonoGame/Skia/HarfBuzz into the main package.
```

with:

```markdown
- [x] The dependency problem behind the package split is explicitly tracked instead of deferred indefinitely. `Cerneala.csproj` currently pulls MonoGame/Skia/HarfBuzz into the main package.
```

- [ ] **Step 5: Verify audit package wording**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2_AUDIT.md -Pattern "clarify-package-boundary-dependencies|single project now|package-shape tests remain deferred|explicitly tracked instead of deferred indefinitely" -Context 0,8
```

Expected: output shows the package-boundary finding is documented as resolved without claiming the split exists.

- [ ] **Step 6: Commit audit package tracking**

```powershell
git add ROADMAPv2_AUDIT.md
git commit -m "docs: record package boundary decision"
```

---

### Task 3: Update Audit Fix Plan Tracking

**Files:**
- Modify: `AUDIT_FIX_PLAN.md`

- [ ] **Step 1: Add Plan 7 detailed-plan link**

Under:

```markdown
### Plan 7: `clarify-package-boundary-dependencies`
```

insert:

```markdown
Detailed plan: `docs/superpowers/plans/2026-07-05-clarify-package-boundary-dependencies.md`
```

- [ ] **Step 2: Mark Plan 7 checklist complete after Tasks 1-2 pass**

After roadmap and audit edits are verified, replace:

```markdown
- [ ] Keep `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, `Cerneala.Tests.Core.csproj`, and `Cerneala.Tests.MonoGame.csproj` deferred.
- [ ] Record that `Cerneala.csproj` currently carries MonoGame/Skia/HarfBuzz dependencies.
- [ ] Define future acceptance criteria for splitting core/adapters.
- [ ] Add package-shape tests only when the split criteria are ready.
```

with:

```markdown
- [x] Keep `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, `Cerneala.Tests.Core.csproj`, and `Cerneala.Tests.MonoGame.csproj` deferred.
- [x] Record that `Cerneala.csproj` currently carries MonoGame/Skia/HarfBuzz dependencies.
- [x] Define future acceptance criteria for splitting core/adapters.
- [x] Add package-shape tests only when the split criteria are ready.
```

- [ ] **Step 3: Mark execution order Plan 7 complete**

Under `## Execution Order`, replace:

```markdown
11. [ ] Execute `clarify-package-boundary-dependencies`.
```

with:

```markdown
11. [x] Execute `clarify-package-boundary-dependencies`.
```

- [ ] **Step 4: Verify audit fix plan tracking**

Run:

```powershell
Select-String -LiteralPath AUDIT_FIX_PLAN.md -Pattern "2026-07-05-clarify-package-boundary-dependencies|Record that `Cerneala.csproj`|Execute `clarify-package-boundary-dependencies`" -Context 0,8
```

Expected: output shows Plan 7 has a detailed plan link, checklist items are complete, and execution order item 11 is checked.

- [ ] **Step 5: Commit audit fix plan update**

```powershell
git add AUDIT_FIX_PLAN.md
git commit -m "docs: track package boundary clarification"
```

---

### Task 4: Full Verification

**Files:**
- No additional edits unless verification exposes missed documentation.

- [ ] **Step 1: Verify split project files remain absent and unchecked**

Run:

```powershell
$projectFiles = @(
  "Cerneala.Core.csproj",
  "Cerneala.MonoGame.csproj",
  "Cerneala.Tests.Core.csproj",
  "Cerneala.Tests.MonoGame.csproj"
)

$roadmap = Get-Content -LiteralPath ROADMAPv2.md -Raw
foreach ($projectFile in $projectFiles) {
  if (Test-Path -LiteralPath $projectFile) {
    throw "Deferred split project unexpectedly exists: $projectFile"
  }

  if ($roadmap -notlike "*- [ ] ``$projectFile``*") {
    throw "ROADMAPv2.md does not keep $projectFile unchecked."
  }
}
```

Expected: command exits successfully. No split project files exist, and roadmap entries remain unchecked.

- [ ] **Step 2: Verify required package-boundary phrases**

Run:

```powershell
$roadmapRequired = @(
  "not dependency-neutral yet",
  "Current package dependency note",
  "MonoGame.Framework.DesktopGL",
  "SkiaSharp.NativeAssets.Linux",
  "Future split acceptance criteria",
  "Package-shape tests are deferred until the split projects are created"
)

$auditRequired = @(
  "Implementation note: fixed by `clarify-package-boundary-dependencies`",
  "package-shape tests remain deferred until the split projects exist",
  "explicitly tracked instead of deferred indefinitely"
)

$roadmap = Get-Content -LiteralPath ROADMAPv2.md -Raw
$audit = Get-Content -LiteralPath ROADMAPv2_AUDIT.md -Raw

foreach ($phrase in $roadmapRequired) {
  if ($roadmap -notlike "*$phrase*") {
    throw "Missing roadmap phrase: $phrase"
  }
}

foreach ($phrase in $auditRequired) {
  if ($audit -notlike "*$phrase*") {
    throw "Missing audit phrase: $phrase"
  }
}
```

Expected: command exits successfully with no missing phrase.

- [ ] **Step 3: Run the existing package/source boundary tests**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore --filter "FullyQualifiedName~MonoGameDependencyBoundaryTests|FullyQualifiedName~NamespaceBoundaryTests"
```

Expected: `MonoGameDependencyBoundaryTests` and `NamespaceBoundaryTests` pass.

If `--no-restore` fails because dependencies are not restored, run:

```powershell
dotnet test Cerneala.slnx --filter "FullyQualifiedName~MonoGameDependencyBoundaryTests|FullyQualifiedName~NamespaceBoundaryTests"
```

Expected: matching tests pass.

- [ ] **Step 4: Run full solution tests**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore
```

Expected: all tests pass.

If `--no-restore` fails because dependencies are not restored, run:

```powershell
dotnet test Cerneala.slnx
```

Expected: all tests pass.

- [ ] **Step 5: Inspect final diff**

Run:

```powershell
git status --short
git diff --stat
```

Expected: no uncommitted files after the task commits, or only intentional markdown files before the final commit.

- [ ] **Step 6: Commit verification corrections if needed**

If verification exposes missed wording or a stale unchecked Plan 7 item, make the smallest markdown correction and commit:

```powershell
git add ROADMAPv2.md ROADMAPv2_AUDIT.md AUDIT_FIX_PLAN.md
git commit -m "docs: complete package boundary verification"
```

If no files changed, do not create an empty commit.

---

## Self-Review

### Spec Coverage

- Keep `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, `Cerneala.Tests.Core.csproj`, and `Cerneala.Tests.MonoGame.csproj` deferred: Tasks 1 and 4 keep them unchecked and verify the files remain absent.
- Record that `Cerneala.csproj` currently carries MonoGame/Skia/HarfBuzz dependencies: Task 1 adds the exact dependency inventory from `Cerneala.csproj`.
- Define future acceptance criteria for splitting core/adapters: Task 1 adds explicit criteria for core, MonoGame adapter, test projects, text dependency ownership, playground references, and package-shape tests.
- Add package-shape tests only when split criteria are ready: Tasks 1, 2, and 4 state and verify that package-shape tests are deferred until split projects exist.
- Update audit tracking artifacts: Tasks 2 and 3.
- Full verification: Task 4.

### Placeholder Scan

This plan contains no `TBD`, `TODO`, "implement later", or vague "add tests" instructions. Each markdown edit gives exact replacement text, each verification step gives exact PowerShell commands, and expected results are stated.

### Type Consistency

This plan edits markdown only. Project names are consistent across all tasks: `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, `Cerneala.Tests.Core.csproj`, and `Cerneala.Tests.MonoGame.csproj`. Package names match the current `Cerneala.csproj`: `MonoGame.Framework.DesktopGL`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`, and `HarfBuzzSharp`.
