# Freeze Later Experimental Scope Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update `ROADMAPv2.md` so Later and Optional/Experimental areas distinguish existing descriptors/tests from scenario-complete, backend-supported product scope.

**Architecture:** This is a documentation-only hardening pass. Keep the useful history in `ROADMAPv2.md`, but add explicit maturity language and change overconfident status markers where the audit identifies descriptor-level, MVP-only, or frozen work. Do not delete file inventories; annotate them so project memory stops pretending the shiny box is the finished machine.

**Tech Stack:** Markdown, PowerShell verification commands, existing `ROADMAPv2.md`, `ROADMAPv2_AUDIT.md`, and `AUDIT_FIX_PLAN.md`.

---

## File Structure

- Modify: `ROADMAPv2.md`
  - Add maturity semantics to the legend.
  - Add a Later/Optional freeze notice after scope bands.
  - Mark advanced media/rendering as experimental/frozen until draw command and backend semantics exist.
  - Mark advanced input categories as experimental/frozen until platform behavior exists.
  - Mark markup/source generation as optional/frozen until retained core contracts are stable.
  - Mark accessibility platform adapters as later while preserving semantic tree architecture.
  - Mark animation/storyboard expansion as later until scheduler/render invalidation survives animation stress.
  - Update Later and Optional/Experimental implementation order without deleting history.
- Modify: `AUDIT_FIX_PLAN.md`
  - Add the detailed plan link under Plan 5.
  - Leave Plan 5 checklist unchecked until the roadmap edit and verification are complete.
- Modify: `ROADMAPv2_AUDIT.md`
  - Add the implementation note only after the roadmap edit has landed and verification passes.

## Important Existing Context

`ROADMAPv2_AUDIT.md` says the current roadmap overclaims maturity because many `[x]` entries only prove a file or a simple test exists. The relevant audit guidance is:

```markdown
Required changes:

- [ ] Add an audit marker or status correction pass to ROADMAPv2 after Must Fix items land. Do not rewrite the roadmap; just stop treating descriptor files as finished product features.
- [ ] In Superpowers planning/checklist artifacts, distinguish "type exists", "wired into retained pipeline", "backend-supported", and "scenario-complete".
- [ ] Add explicit "experimental/frozen" status to Later/Optional areas that should not drive core design.
```

Keep these current roadmap sections as the edit surface:

- `Legend`
- `Scope bands`
- `## 21. [Later] Accessibility and semantics`
- `## 22. [Later] Advanced rendering and media`
- `## 23. [Later] Animation and transitions`
- `## 25. [Optional/Experimental] Markup, serialization, and source generation`
- `## 26. [Optional/Experimental] Advanced input categories`
- `## 27. Implementation order`

---

### Task 1: Add Maturity Semantics To The Roadmap Legend

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Update the legend**

Replace the current legend block in `ROADMAPv2.md`:

```markdown
Legend:

- `[x]` Exists now.
- `[ ]` Planned.
- `[~]` Exists partially, exists as a low-level primitive, or needs reshaping/integration for the v2 architecture.
```

with:

```markdown
Legend:

- `[x]` Exists now at the maturity claimed by the line text.
- `[ ]` Planned.
- `[~]` Exists partially, exists as a low-level primitive, is descriptor-only, or needs reshaping/integration for the v2 architecture.

Maturity markers used in Later and Optional/Experimental sections:

- **Type exists**: public/API shape or descriptor exists, but scenario behavior may be incomplete.
- **Wired into retained pipeline**: update/invalidation/render/input scheduling uses it through retained contracts.
- **Backend-supported**: drawing/input/platform adapters implement the behavior, not just metadata.
- **Scenario-complete**: realistic product scenario is implemented and covered by integration tests.
- **Frozen**: do not expand public surface until the named prerequisite is complete.
```

- [ ] **Step 2: Add freeze note after scope bands**

After the scope bands list:

```markdown
- **Optional/Experimental**: useful ideas that should not shape the core until proven.
```

insert:

```markdown
Later and Optional/Experimental work may have checked files or tests because prototypes exist. Those checks do not mean the feature is scenario-complete unless the line explicitly says so. Frozen sections can keep their existing code and tests, but should not drive core architecture or add public surface until their listed prerequisites are met.
```

- [ ] **Step 3: Verify the inserted maturity language**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "Maturity markers|Type exists|Frozen sections" -Context 1,8
```

Expected: output shows the new legend details and the freeze note near the top of the file.

- [ ] **Step 4: Commit legend update**

```powershell
git add ROADMAPv2.md
git commit -m "docs: clarify roadmap maturity markers"
```

---

### Task 2: Freeze Advanced Media And Rendering Scope

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Replace the advanced media section opening**

Replace the first paragraph under `## 22. [Later] Advanced rendering and media`:

```markdown
This phase expands drawing capabilities only when controls and scenarios require them. New media concepts must translate into `DrawCommand` extensions or clear backend abstractions instead of duplicating existing primitives.
```

with:

```markdown
This phase is **experimental/frozen** until drawing command and backend semantics exist for each advanced primitive. Existing files may remain as descriptors, typed state, or tests, but they are not scenario-complete unless the roadmap line explicitly says `backend-supported` or `scenario-complete`.

New media concepts must translate into `DrawCommand` extensions or clear backend abstractions before they can be marked implemented. Descriptor existence alone is not enough.
```

- [ ] **Step 2: Change descriptor-level media entries from `[x]` to `[~]` with exact notes**

In the same section, replace these lines:

```markdown
- [x] `UI/Media/LinearGradientBrush.cs`
- [x] `UI/Media/RadialGradientBrush.cs`
- [x] `UI/Media/PathGeometry.cs`
- [x] `UI/Media/OpacityLayer.cs`
- [x] `UI/Media/ShadowEffect.cs`
- [x] `UI/Media/RenderTargetImage.cs`
```

with:

```markdown
- [~] `UI/Media/LinearGradientBrush.cs` - type exists; frozen until gradient draw commands and backend rendering exist.
- [~] `UI/Media/RadialGradientBrush.cs` - type exists; frozen until gradient draw commands and backend rendering exist.
- [~] `UI/Media/PathGeometry.cs` - type exists; frozen until real path fill/stroke command semantics exist.
- [~] `UI/Media/OpacityLayer.cs` - type exists; frozen until layer composition has retained render-cache and backend semantics.
- [~] `UI/Media/ShadowEffect.cs` - type exists; frozen until shadow/effect command semantics and backend behavior exist.
- [~] `UI/Media/RenderTargetImage.cs` - type exists; frozen until render-target lifecycle and backend ownership are designed.
```

- [ ] **Step 3: Replace the advanced media acceptance checklist**

Replace the section 22 acceptance checklist:

```markdown
- [x] Every new media abstraction has a responsibility not already covered by `DrawColor`, `DrawRect`, `DrawPoint`, `DrawTextRun`, or `IDrawImage`.
- [x] Every new drawing command has backend tests or adapter coverage.
- [x] Controls do not reference Skia, HarfBuzz, MonoGame, `SpriteBatch`, or `Texture2D`.
- [x] Full project tests pass for this phase.
```

with:

```markdown
- [~] Every new media abstraction has a responsibility not already covered by `DrawColor`, `DrawRect`, `DrawPoint`, `DrawTextRun`, or `IDrawImage`; descriptor-only abstractions remain experimental.
- [ ] Every advanced drawing command has backend tests or adapter coverage before being marked implemented.
- [x] Controls do not reference Skia, HarfBuzz, MonoGame, `SpriteBatch`, or `Texture2D`.
- [ ] Full project tests pass for the backend-supported advanced media scenario before this phase is scenario-complete.
```

- [ ] **Step 4: Verify advanced media no longer overclaims**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "Advanced rendering and media|experimental/frozen|LinearGradientBrush|ShadowEffect|backend-supported advanced media" -Context 0,6
```

Expected: section 22 states experimental/frozen and descriptor-level media entries use `[~]`.

- [ ] **Step 5: Commit media freeze**

```powershell
git add ROADMAPv2.md
git commit -m "docs: freeze advanced media roadmap scope"
```

---

### Task 3: Clarify Accessibility Platform Adapter Scope

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Replace the accessibility section opening**

Replace the paragraph under `## 21. [Later] Accessibility and semantics`:

```markdown
This phase makes UI meaning available to assistive technologies and testing tools. It should be designed around a platform-neutral semantics tree first, with platform adapters later.
```

with:

```markdown
This phase keeps the platform-neutral semantics tree as the retained-core architecture. Platform accessibility adapters are **later/frozen** until the semantics API is stable and at least one backend adapter can expose real platform behavior.
```

- [ ] **Step 2: Mark automation/platform pieces as partial**

Replace:

```markdown
- [x] `UI/Accessibility/AutomationPeer.cs` - only if WPF naming remains useful.
- [x] `UI/Accessibility/ButtonAutomationPeer.cs`
- [x] `UI/Accessibility/TextBoxAutomationPeer.cs`
- [x] `UI/Accessibility/ItemsControlAutomationPeer.cs`
- [x] `UI/Platform/IAccessibilityPlatform.cs`
```

with:

```markdown
- [~] `UI/Accessibility/AutomationPeer.cs` - type exists; naming remains under review and platform adapter behavior is frozen.
- [~] `UI/Accessibility/ButtonAutomationPeer.cs` - type exists; keep behind semantic tree behavior until platform adapters exist.
- [~] `UI/Accessibility/TextBoxAutomationPeer.cs` - type exists; keep behind semantic tree behavior until platform adapters exist.
- [~] `UI/Accessibility/ItemsControlAutomationPeer.cs` - type exists; keep behind semantic tree behavior until platform adapters exist.
- [~] `UI/Platform/IAccessibilityPlatform.cs` - contract exists; real adapter behavior remains later.
```

- [ ] **Step 3: Add accessibility acceptance checklist**

After the accessibility tests list:

```markdown
- [x] `tests/Cerneala.Tests/UI/Accessibility/TextBoxSemanticsTests.cs`
```

insert:

```markdown
Acceptance checklist:

- [x] Platform-neutral semantic nodes can be produced for retained controls.
- [~] WPF-like peer names are tolerated only as implementation vocabulary while the public API decision remains open.
- [ ] Native platform accessibility adapters exist and are tested before this phase is scenario-complete.
```

- [ ] **Step 4: Verify accessibility scope**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "Accessibility and semantics|later/frozen|AutomationPeer|Native platform accessibility" -Context 0,8
```

Expected: semantic tree remains preserved, peer/platform adapter entries are `[~]`, and native adapters are unchecked.

- [ ] **Step 5: Commit accessibility clarification**

```powershell
git add ROADMAPv2.md
git commit -m "docs: clarify accessibility adapter scope"
```

---

### Task 4: Freeze Animation Expansion

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Replace the animation section opening**

Replace the paragraph under `## 23. [Later] Animation and transitions`:

```markdown
This phase adds time-based property changes after frame scheduling and invalidation are solid. Animation should be game-loop-native, explicit, and invalidate only affected properties.
```

with:

```markdown
This phase keeps the existing animation primitives small and **frozen for expansion** until scheduler/render invalidation is proven under animation stress. Animation should be game-loop-native, explicit, and invalidate only affected properties; timeline/storyboard growth waits for stress tests that prove no hidden layout/render churn.
```

- [ ] **Step 2: Mark storyboard and style transition expansion as partial**

Replace:

```markdown
- [x] `UI/Animation/Storyboard.cs` - only if composition of timelines is needed.
- [x] `UI/Styling/StyleTransition.cs`
```

with:

```markdown
- [~] `UI/Animation/Storyboard.cs` - type exists; composition expansion is frozen until animation stress invalidation tests exist.
- [~] `UI/Styling/StyleTransition.cs` - type exists; expansion is frozen until style/render invalidation under animation stress is proven.
```

- [ ] **Step 3: Replace the animation acceptance checklist**

Replace:

```markdown
- [x] Animating a render-only property does not run layout.
- [x] Animating a layout property enqueues layout only at ticks where the value changes.
- [x] Completed animations release animated value source cleanly.
- [x] Animation and style transition APIs stay backend-neutral.
- [x] Full project tests pass for this phase.
```

with:

```markdown
- [x] Animating a render-only property does not run layout in focused unit tests.
- [x] Animating a layout property enqueues layout only at ticks where the value changes in focused unit tests.
- [x] Completed animations release animated value source cleanly.
- [x] Animation and style transition APIs stay backend-neutral.
- [ ] Animation stress tests prove retained scheduler/render invalidation stays honest across many animated elements.
- [ ] Full project tests pass for the stress-tested animation scenario before this phase is scenario-complete.
```

- [ ] **Step 4: Verify animation freeze language**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "Animation and transitions|frozen for expansion|Storyboard|animation stress" -Context 0,8
```

Expected: section 23 says expansion is frozen and stress acceptance remains unchecked.

- [ ] **Step 5: Commit animation freeze**

```powershell
git add ROADMAPv2.md
git commit -m "docs: freeze animation expansion scope"
```

---

### Task 5: Freeze Markup And Source Generation

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Replace the markup section opening**

Replace the paragraph under `## 25. [Optional/Experimental] Markup, serialization, and source generation`:

```markdown
This phase is optional. Cerneala should be code-first and strongly typed before any markup layer exists. Markup may become useful for tooling or designer workflows, but it should compile into typed object creation rather than becoming a reflection-heavy runtime requirement.
```

with:

```markdown
This phase is optional and **frozen** until retained core contracts are stable. Cerneala stays code-first and strongly typed; markup may remain as a prototype for tooling/designer workflows, but it must not become a reflection-heavy runtime requirement or force core API shape.
```

- [ ] **Step 2: Mark markup/sourcegen entries as partial prototypes**

Replace the current section 25 file list:

```markdown
- [x] `UI/Markup/UiMarkupDocument.cs`
- [x] `UI/Markup/UiMarkupReader.cs`
- [x] `UI/Markup/UiMarkupWriter.cs`
- [x] `UI/Markup/UiMarkupSchema.cs`
- [x] `UI/Markup/UiMarkupTypeRegistry.cs`
- [x] `UI/Markup/UiFactory.cs`
- [x] `UI/Markup/GeneratedUiFactory.cs`
- [x] `UI/Markup/MarkupLoadOptions.cs`
- [x] `UI/Markup/MarkupDiagnostic.cs`
- [x] `UI/Markup/ContentPropertyAttribute.cs` - optional ergonomic hint.
- [x] `UI/Markup/DesignTimeOnlyAttribute.cs`
- [x] `Cerneala.SourceGen/UiMarkupGenerator.cs` - optional incremental source generator for `.cui.xml` files that emits code-first retained UI factories.
```

with:

```markdown
- [~] `UI/Markup/UiMarkupDocument.cs` - prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiMarkupReader.cs` - prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiMarkupWriter.cs` - prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiMarkupSchema.cs` - prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiMarkupTypeRegistry.cs` - prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/UiFactory.cs` - prototype exists; keep as typed factory path, not core runtime magic.
- [~] `UI/Markup/GeneratedUiFactory.cs` - prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/MarkupLoadOptions.cs` - prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/MarkupDiagnostic.cs` - prototype exists; frozen until retained core contracts are stable.
- [~] `UI/Markup/ContentPropertyAttribute.cs` - optional ergonomic hint; frozen with markup.
- [~] `UI/Markup/DesignTimeOnlyAttribute.cs` - optional tooling hint; frozen with markup.
- [~] `Cerneala.SourceGen/UiMarkupGenerator.cs` - optional source generator prototype; frozen until runtime reflection cost or tooling demand proves it is needed.
```

- [ ] **Step 3: Replace the markup acceptance checklist**

Replace:

```markdown
- [x] Markup is not required to create controls.
- [x] Markup does not bypass typed property validation.
- [x] Generated factories produce retained trees that use the same invalidation and render-cache paths as code-created trees.
```

with:

```markdown
- [x] Markup is not required to create controls.
- [x] Markup does not bypass typed property validation in covered prototype tests.
- [~] Generated factories produce retained trees through typed creation paths in prototype tests.
- [ ] Markup/source generation remains frozen until retained core contracts are stable and a real tooling scenario requires expansion.
```

- [ ] **Step 4: Verify markup freeze**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "Markup, serialization|optional and \\*\\*frozen\\*\\*|UiMarkupReader|source generator prototype|Markup/source generation remains frozen" -Context 0,8
```

Expected: section 25 entries are `[~]` and explicitly frozen.

- [ ] **Step 5: Commit markup freeze**

```powershell
git add ROADMAPv2.md
git commit -m "docs: freeze markup and sourcegen prototypes"
```

---

### Task 6: Freeze Advanced Input Categories

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Replace the advanced input section opening**

Replace the paragraph under `## 26. [Optional/Experimental] Advanced input categories`:

```markdown
This phase turns existing metadata-only event categories into real behavior only when platform support and product scenarios require them.
```

with:

```markdown
This phase is **experimental/frozen** until platform adapters can supply real touch, stylus, drag/drop, manipulation, cursor, and ink data. Existing event metadata and processors can remain as prototypes, but platform-backed behavior is not scenario-complete unless the line explicitly says so.
```

- [ ] **Step 2: Mark advanced input entries as partial**

Replace the current section 26 file list:

```markdown
- [x] `UI/Input/InputEvents.cs` already declares stylus, touch, manipulation, and drag/drop event metadata.
- [x] `UI/Input/TouchInputBridge.cs`
- [x] `UI/Input/StylusInputBridge.cs`
- [x] `UI/Input/GestureRecognizer.cs`
- [x] `UI/Input/ManipulationProcessor.cs`
- [x] `UI/Input/DragDropController.cs`
- [x] `UI/Input/DataTransfer.cs`
- [x] `UI/Input/Cursor.cs`
- [x] `UI/Input/CursorService.cs`
- [x] `UI/Controls/InkCanvas.cs`
- [x] `UI/Ink/Stroke.cs`
- [x] `UI/Ink/StrokeCollection.cs`
```

with:

```markdown
- [~] `UI/Input/InputEvents.cs` already declares stylus, touch, manipulation, and drag/drop event metadata; behavior remains platform-gated.
- [~] `UI/Input/TouchInputBridge.cs` - prototype exists; frozen until platform touch data is available.
- [~] `UI/Input/StylusInputBridge.cs` - prototype exists; frozen until platform stylus data is available.
- [~] `UI/Input/GestureRecognizer.cs` - prototype exists; frozen until gesture scenarios are platform-backed.
- [~] `UI/Input/ManipulationProcessor.cs` - prototype exists; frozen until manipulation scenarios are platform-backed.
- [~] `UI/Input/DragDropController.cs` - prototype exists; frozen until drag/drop platform behavior exists.
- [~] `UI/Input/DataTransfer.cs` - type exists; frozen until drag/drop and clipboard/platform transfer behavior exist.
- [~] `UI/Input/Cursor.cs` - type exists; frozen until cursor platform behavior is wired.
- [~] `UI/Input/CursorService.cs` - type exists; frozen until cursor platform behavior is wired.
- [~] `UI/Controls/InkCanvas.cs` - prototype exists; frozen until stylus/touch platform behavior is real.
- [~] `UI/Ink/Stroke.cs` - type exists; frozen with ink scenarios.
- [~] `UI/Ink/StrokeCollection.cs` - type exists; frozen with ink scenarios.
```

- [ ] **Step 3: Add advanced input acceptance checklist**

After the section 26 tests list, insert:

```markdown
Acceptance checklist:

- [~] Event metadata and prototype processors exist.
- [ ] Platform adapters provide real touch/stylus/drag/drop/manipulation input data.
- [ ] Retained input cache, focus, capture, and hit testing are proven under advanced input scenarios.
- [ ] Full project tests pass for platform-backed advanced input before this phase is scenario-complete.
```

- [ ] **Step 4: Verify advanced input freeze**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "Advanced input categories|experimental/frozen|TouchInputBridge|platform-backed advanced input" -Context 0,8
```

Expected: section 26 entries are `[~]` and platform-backed acceptance remains unchecked.

- [ ] **Step 5: Commit advanced input freeze**

```powershell
git add ROADMAPv2.md
git commit -m "docs: freeze advanced input prototypes"
```

---

### Task 7: Update Implementation Order Without Deleting History

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Replace Later order**

Replace:

```markdown
### Later order

- [x] 20. Add text editing and IME composition.
- [x] 21. Add accessibility semantics and platform-neutral semantic tree.
- [x] 22. Add advanced rendering/media primitives as scenarios require.
- [x] 23. Add animation and transitions.
- [x] 24. Decide package/platform split.
```

with:

```markdown
### Later order

- [~] 20. Add text editing and IME composition - foundations exist; production text services and platform behavior remain later.
- [~] 21. Add accessibility semantics and platform-neutral semantic tree - semantic tree exists; platform adapters remain later.
- [~] 22. Add advanced rendering/media primitives as scenarios require - descriptor types exist; backend-supported rendering remains later.
- [~] 23. Add animation and transitions - primitives exist; expansion waits for animation stress invalidation proof.
- [~] 24. Decide package/platform split - platform contracts exist; package split remains deferred.
```

- [ ] **Step 2: Replace Optional/Experimental order**

Replace:

```markdown
### Optional/Experimental order

- [x] 25. Prototype markup/serialization after templates and typed properties are stable.
- [x] 26. Prototype source generation if runtime reflection becomes a real cost.
- [x] 27. Implement touch/stylus/drag/drop behavior when platform adapters can supply real data.
```

with:

```markdown
### Optional/Experimental order

- [~] 25. Prototype markup/serialization after templates and typed properties are stable - prototype exists and is frozen until retained core contracts are stable.
- [~] 26. Prototype source generation if runtime reflection becomes a real cost - prototype exists and is frozen until tooling or runtime cost justifies expansion.
- [~] 27. Implement touch/stylus/drag/drop behavior when platform adapters can supply real data - prototype pieces exist; platform-backed behavior remains later.
```

- [ ] **Step 3: Add a roadmap honesty decision**

Under `## 28. Risks and decisions needing human confirmation`, after the `### Testing and acceptance gates` block, insert:

```markdown
### Roadmap honesty

- [x] File/test existence is not enough to mark Later or Optional/Experimental work scenario-complete.
- [x] Descriptor-only APIs remain `[~]` until they are wired into retained scheduling and relevant backend/platform adapters.
- [x] Frozen areas may keep existing code, but should not expand public surface until their prerequisites are met.
```

- [ ] **Step 4: Verify implementation order update**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "Later order|Optional/Experimental order|Roadmap honesty|Descriptor-only APIs" -Context 0,8
```

Expected: Later and Optional/Experimental order use `[~]`, and roadmap honesty decisions exist.

- [ ] **Step 5: Commit implementation order update**

```powershell
git add ROADMAPv2.md
git commit -m "docs: correct later roadmap completion status"
```

---

### Task 8: Update Audit Tracking Docs

**Files:**
- Modify: `AUDIT_FIX_PLAN.md`
- Modify: `ROADMAPv2_AUDIT.md`

- [ ] **Step 1: Add Plan 5 detailed-plan link**

Under `### Plan 5: freeze-later-experimental-scope` in `AUDIT_FIX_PLAN.md`, insert:

```markdown
Detailed plan: `docs/superpowers/plans/2026-07-05-freeze-later-experimental-scope.md`
```

- [ ] **Step 2: Mark Plan 5 checklist done only after tasks 1-7 pass verification**

After all roadmap edits and checks pass, change Plan 5 checklist from:

```markdown
- [ ] Mark advanced media/rendering as experimental/frozen until drawing command/backend semantics exist.
- [ ] Mark advanced input categories as experimental/frozen until platform behavior exists.
- [ ] Mark markup/source generation as optional/frozen until retained core contracts are stable.
- [ ] Mark accessibility platform adapters as later; keep semantic tree architecture.
- [ ] Mark animation/storyboard expansion as later until scheduler/render invalidation is proven under animation stress.
- [ ] Update `ROADMAPv2.md` without deleting useful history.
```

to:

```markdown
- [x] Mark advanced media/rendering as experimental/frozen until drawing command/backend semantics exist.
- [x] Mark advanced input categories as experimental/frozen until platform behavior exists.
- [x] Mark markup/source generation as optional/frozen until retained core contracts are stable.
- [x] Mark accessibility platform adapters as later; keep semantic tree architecture.
- [x] Mark animation/storyboard expansion as later until scheduler/render invalidation is proven under animation stress.
- [x] Update `ROADMAPv2.md` without deleting useful history.
```

- [ ] **Step 3: Add ROADMAPv2_AUDIT implementation note**

Under `ROADMAPv2_AUDIT.md` > `## Should Fix` > `### 1. ROADMAPv2 statuses are too optimistic`, after the required changes list, add:

```markdown
Implementation note: fixed by `freeze-later-experimental-scope`; `ROADMAPv2.md` now distinguishes type existence, retained-pipeline integration, backend/platform support, and scenario completeness. Later and Optional/Experimental areas for media, accessibility adapters, animation expansion, markup/source generation, and advanced input are explicitly marked partial or frozen instead of completed.
```

- [ ] **Step 4: Verify audit docs**

Run:

```powershell
Select-String -LiteralPath AUDIT_FIX_PLAN.md,ROADMAPv2_AUDIT.md -Pattern "freeze-later-experimental-scope|experimental/frozen|Implementation note: fixed by" -Context 0,8
```

Expected: `AUDIT_FIX_PLAN.md` links the detailed plan and `ROADMAPv2_AUDIT.md` records the completion note.

- [ ] **Step 5: Commit audit docs**

```powershell
git add AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md
git commit -m "docs: record roadmap freeze pass"
```

---

### Task 9: Full Documentation Verification

**Files:**
- No additional edits unless verification exposes a missed status.

- [ ] **Step 1: Check for overclaimed advanced section headings**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "## 21\\.|## 22\\.|## 23\\.|## 25\\.|## 26\\.|### Later order|### Optional/Experimental order" -Context 0,18
```

Expected: relevant Later and Optional/Experimental sections contain frozen/partial language and no longer read as scenario-complete just because files exist.

- [ ] **Step 2: Check that required frozen phrases exist**

Run:

```powershell
$required = @(
  "Maturity markers used in Later and Optional/Experimental sections",
  "experimental/frozen until drawing command and backend semantics exist",
  "platform accessibility adapters are **later/frozen**",
  "frozen for expansion",
  "optional and **frozen** until retained core contracts are stable",
  "experimental/frozen until platform adapters can supply real touch",
  "Roadmap honesty"
)

$text = Get-Content -LiteralPath ROADMAPv2.md -Raw
foreach ($phrase in $required) {
  if ($text -notlike "*$phrase*") {
    throw "Missing required roadmap phrase: $phrase"
  }
}
```

Expected: command exits successfully with no missing phrase.

- [ ] **Step 3: Run a solution test smoke even though this is docs-only**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore
```

Expected: all tests pass. If `--no-restore` fails because dependencies are not restored, run:

```powershell
dotnet test Cerneala.slnx
```

Expected: all tests pass.

- [ ] **Step 4: Inspect final diff**

Run:

```powershell
git status --short
git diff --stat
```

Expected: no uncommitted files after the task commits, or only the current task's intended markdown files before the final commit.

- [ ] **Step 5: Commit verification fixes if needed**

If verification exposes missed wording or a stale unchecked Plan 5 item, make the smallest markdown correction and commit:

```powershell
git add ROADMAPv2.md AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md
git commit -m "docs: complete roadmap freeze verification"
```

If no files changed, do not create an empty commit.

---

## Self-Review

### Spec Coverage

- Mark advanced media/rendering as experimental/frozen until drawing command/backend semantics exist: Task 2.
- Mark advanced input categories as experimental/frozen until platform behavior exists: Task 6.
- Mark markup/source generation as optional/frozen until retained core contracts are stable: Task 5.
- Mark accessibility platform adapters as later while keeping semantic tree architecture: Task 3.
- Mark animation/storyboard expansion as later until scheduler/render invalidation is proven under animation stress: Task 4.
- Update `ROADMAPv2.md` without deleting useful history: Tasks 1-7 keep file/test inventories and change maturity language instead of removing history.
- Link Plan 5 from `AUDIT_FIX_PLAN.md`: Task 8.
- Record audit completion only after verification: Task 8.

### Placeholder Scan

This plan contains no `TBD`, `TODO`, "implement later", or vague "add tests" instructions. Each edit step gives exact replacement text and each verification step gives exact PowerShell commands with expected results.

### Type Consistency

This plan edits markdown only. The maturity terms are used consistently: **type exists**, **wired into retained pipeline**, **backend-supported**, **scenario-complete**, and **frozen**. The status symbol meanings stay aligned with the updated legend: `[x]` means the line's exact maturity claim is true, `[~]` means partial/prototype/descriptor-level, and `[ ]` means planned.
