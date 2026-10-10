# Core Preview Completion Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Add one final integration gate proving Cerneala now behaves as a coherent Core Preview: first-frame retained work, unchanged no-work frames, draw purity, mouse and keyboard command activation, theme mutation, text wrapping resize, list mutation, and scroll invalidation.

**Architecture:** This is a gate plan, not a feature plan. Mostly add tests and make small fixes exposed by those tests. Do not redesign the runtime or add optional WPF-era features.

**Tech Stack:** C#/.NET 8, xUnit, existing Playground `RetainedAppSample`, `UiHost`, retained scheduler, input bridge, rendering cache, styling/theme, text, list/scroll.

---

## File Structure

- Create: `tests/Cerneala.Tests/UI/Hosting/CorePreviewContractTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Hosting/RetainedVerticalSliceTests.cs`
  - Avoid duplicate brittle coverage if needed.
- Modify: `tests/Cerneala.Tests/Playground/RetainedAppSampleContractTests.cs`
  - Add only sample-specific assertions not covered by gate.
- Modify: `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`
  - Add small testability hooks only if required.
- Modify: `ROADMAPv2.md`
  - Mark corresponding items `[x]` only when truly implemented and tested.
- Modify: `ROADMAPv2_AUDIT.md`
  - Optional tiny dated note only if repo convention supports it.

## Important Existing Behavior

Previous plans should have implemented:

- Pre-input retained frame gate.
- `TextBlock` wrapping/trimming API.
- Button keyboard activation.
- Retained input bindings.
- Default theme/style vertical slice.
- Items/scroll retained vertical slice.

This plan proves those pieces work together through one retained host contract.

Target gate:

- First frame does layout/render/hit-test/style work.
- Second unchanged frame does no retained work.
- Draw does not create retained work.
- Button command works with mouse and keyboard.
- Theme change invalidates styled render only when possible.
- Text wraps/re-renders after viewport width change.
- List mutation invalidates retained work.
- Scroll invalidates arrange/render/hit-test without full measure where possible.

## Rules

- [ ] Do not add framework surface area unless a gate test cannot be expressed otherwise.
- [ ] Do not weaken prior tests to make the gate pass.
- [ ] Do not assert exact global counts when narrower invariants are sufficient.
- [ ] Do not mark roadmap items complete if stubbed.
- [ ] Do not turn this into documentation rewrite.

---

### Task 1: Add RED Core Preview Contract Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Hosting/CorePreviewContractTests.cs`

- [ ] **Step 1: Add tests**

Create tests:

```csharp
CorePreviewFirstFrameDoesLayoutRenderHitTestStyleWork()
CorePreviewSecondUnchangedFrameDoesNoRetainedWork()
CorePreviewDrawNeverGeneratesRetainedWork()
CorePreviewButtonWorksWithMouseAndKeyboard()
CorePreviewThemeChangeInvalidatesStyledRenderOnlyWhenPossible()
CorePreviewTextWrapsAndRerendersAfterViewportWidthChange()
CorePreviewListMutationInvalidatesRetainedWork()
CorePreviewScrollInvalidatesArrangeRenderHitTestWithoutFullMeasure()
```

Test intent:

- Build retained app sample inside `UIRoot`.
- Attach root resources, default theme provider, and default stylesheet.
- Use `UiHost.Update(...)` with controlled `InputFrame`s and `UiViewport`s.
- Inspect `UiFrame.Stats` and sample state.
- Use fake drawing backend to prove `Draw(...)` submits cached commands without dirtying retained work.

- [ ] **Step 2: Add test-only helpers inside test file**

Allowed helpers:

- `FakeDrawingBackend`
- `CountingCommand`
- `InputFrameFactory`
- `TreeSearch.FindDescendant<T>`

Do not add production helpers just for test convenience.

- [ ] **Step 3: Run gate tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests"
```

Expected: RED until integration gaps are fixed.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\CorePreviewContractTests.cs
git commit -m "test: add core preview retained contract gate"
```

---

### Task 2: Fix Only Gate-Exposed Integration Bugs

**Files:**
- Modify only smallest owning files identified by failures.

- [ ] **Step 1: Categorize failures by owner**

```text
viewport/input ordering      -> UI/Hosting/UiHost.cs
style/theme invalidation     -> UI/Elements/UIRoot.cs, UI/Styling/*
text wrapping/resize         -> UI/Controls/TextBlock.cs, UI/Text/*
keyboard command activation  -> UI/Input/*, UI/Controls/Primitives/ButtonBase.cs
input bindings               -> UI/Input/*, UI/Elements/UIElement.cs
list mutation/realization    -> UI/Controls/ItemsControl.cs, ItemsPresenter.cs, ItemContainerGenerator.cs
scroll invalidation          -> UI/Controls/ScrollViewer.cs, ScrollContentPresenter.cs
render/draw purity           -> UI/Rendering/*, UI/Hosting/UiHost.cs
```

- [ ] **Step 2: Fix narrow bugs only**

Acceptable fixes:

- Missing invalidation flag.
- Wrong phase count.
- Stale input cache after tree mutation.
- Sample missing a testable handle.
- Style setter using wrong property owner.
- Text layout key missing a public property.

Unacceptable expansion:

- New binding engine.
- Full template system.
- New layout algorithm.
- New control family.
- Renderer rewrite.

- [ ] **Step 3: Keep diagnostics honest**

Prefer invariants like `MeasureCalls == 0` or `RenderedElements > 0` over exact total counts when tree shape may evolve.

---

### Task 3: Update Roadmap Checkboxes Conservatively

**Files:**
- Modify: `ROADMAPv2.md`
- Optionally modify: `ROADMAPv2_AUDIT.md`

- [ ] **Step 1: Mark only truly completed items**

Search for corresponding items:

- viewport/frame integration
- text display contract
- keyboard activation
- retained input bindings
- theme/style vertical slice
- items/list/scroll retained vertical slice
- Core Preview gate

Change `[ ]` or `[~]` to `[x]` only when implementation and tests are real.

- [ ] **Step 2: Do not rewrite roadmap prose**

Keep changes tiny.

---

### Task 4: Verify GREEN And Final Suite

- [ ] **Step 1: Run gate tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 2: Run all new plan tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostViewportFrameContractTests|FullyQualifiedName~TextBlockLayoutContractTests|FullyQualifiedName~ButtonKeyboardActivationTests|FullyQualifiedName~RetainedInputBindingTests|FullyQualifiedName~DefaultThemeVerticalSliceTests|FullyQualifiedName~RetainedListScrollVerticalSliceTests|FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

```powershell
dotnet test
```

Expected: GREEN.

- [ ] **Step 4: Commit final gate**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\CorePreviewContractTests.cs tests\Cerneala.Tests\UI\Hosting\RetainedVerticalSliceTests.cs tests\Cerneala.Tests\Playground\RetainedAppSampleContractTests.cs Playground\Cerneala.Playground ROADMAPv2.md ROADMAPv2_AUDIT.md
git commit -m "test: prove cerneala core preview contract"
```

## Final Output Required From Codex

After all plans pass, summarize:

- [ ] completed plans
- [ ] changed files
- [ ] tests run
- [ ] final `dotnet test Cerneala.slnx` result
- [ ] final `dotnet test` result
- [ ] remaining blockers, if any
