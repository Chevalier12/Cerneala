# TextBox Editing Viewport And Caret Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make `TextBox` visibly usable as a minimal single-line retained text editor. Current text input mutates state, but authoring preview needs predictable caret, selection, focus, and horizontal viewport behavior without jumping to full IME/rich text.

**Architecture:** Build on existing `TextBoxBase`, `TextEditor`, `TextDocument`, `TextCaret`, `TextSelection`, `TextMeasurer`, and `TextRenderer`. Keep the scope single-line and deterministic. Render caret/selection with existing rectangle draw commands. Do not add new drawing primitives, full glyph hit-testing, platform clipboard, or multiline layout.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Text`, `UI/Input`, `UI/Controls/TextBoxBase`, retained rendering/invalidation.

---

## File Structure

- Modify: `UI/Controls/TextBoxBase.cs`
  - Add caret/selection rendering and horizontal text viewport logic.
  - Add minimal visual properties if needed: caret color, selection color.
  - Handle Home/End and shift-selection only if the current input model exposes enough modifier data; otherwise keep explicit APIs tested.
- Modify: `UI/Text/TextEditor.cs`
  - Only fix editor bugs exposed by tests; do not move rendering here.
- Modify: `UI/Text/TextCaret.cs`
- Modify: `UI/Text/TextSelection.cs`
- Modify: `UI/Text/TextMeasurer.cs`
  - Only add tiny helpers if needed for deterministic character offset approximation.
- Create: `tests/Cerneala.Tests/Controls/TextBoxEditingVisualContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Text/TextBoxEditorIntegrationTests.cs`

## Important Existing Behavior

- `TextBoxBase` is focusable and handles text input, Backspace, Delete, Left, and Right.
- `TextBoxBase` already exposes `Editor`, `Selection`, and `Caret`.
- `TextBoxBase.OnRender(...)` currently draws background/border/text but not caret or selection visuals.
- `TextEditor` already supports insertion, deletion, caret movement, selection, undo/redo.
- `TextRenderer` draws text through `DrawingContext.DrawText(...)` commands.

Target behavior:

- Focused `TextBox` renders a caret using retained render cache.
- Unfocused `TextBox` does not render a caret.
- Selection renders a deterministic highlight behind text.
- Caret movement invalidates render only, not measure.
- Text mutation invalidates measure/render through `TextProperty`.
- When text exceeds the content width, the horizontal viewport offset keeps the caret visible.
- Unchanged frames do not rebuild TextBox render commands.

## Rules

- [ ] Do not implement full IME composition in this plan.
- [ ] Do not implement multiline editing.
- [ ] Do not implement platform clipboard integration.
- [ ] Do not implement blinking caret timers.
- [ ] Do not add glyph-perfect caret positioning unless current text services already expose it.
- [ ] Use deterministic approximate character positioning for MVP if needed, and label it as such in tests/names.
- [ ] Do not add new drawing primitives.

---

### Task 1: Add RED TextBox Editing Visual Tests

**Files:**
- Create: `tests/Cerneala.Tests/Controls/TextBoxEditingVisualContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Text/TextBoxEditorIntegrationTests.cs`

- [ ] **Step 1: Add caret rendering tests**

Create tests:

```csharp
FocusedTextBoxRendersCaretCommand()
UnfocusedTextBoxDoesNotRenderCaretCommand()
CaretMoveInvalidatesRenderWithoutMeasure()
UnchangedFrameDoesNotRegenerateTextBoxCommands()
```

Test intent:

- Use `UiHost.Update(...)` and the retained renderer when possible.
- Inspect committed draw commands or a test `DrawingContext` output.
- Assert caret is represented by a rectangle command with non-zero height.

- [ ] **Step 2: Add selection rendering tests**

Create tests:

```csharp
SelectionRangeRendersHighlightBeforeText()
SelectionChangeInvalidatesRenderWithoutMeasure()
ReplacingSelectedTextUpdatesTextAndClearsSelectionPredictably()
```

Test intent:

- Call `textBox.Select(anchor, active)` directly for deterministic selection setup.
- Assert a highlight rectangle is emitted before the text command.

- [ ] **Step 3: Add horizontal viewport tests**

Create tests:

```csharp
CaretScrollsIntoViewWhenTextExceedsContentWidth()
BackspaceNearStartScrollsCaretBackIntoView()
ProgrammaticTextResetResetsHorizontalViewportWhenCaretAtStart()
```

Use long text and a narrow arranged content rect.

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxEditingVisualContractTests|FullyQualifiedName~TextBoxEditorIntegrationTests"
```

Expected: RED because caret/selection/viewport rendering is not implemented.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Controls\TextBoxEditingVisualContractTests.cs tests\Cerneala.Tests\UI\Text\TextBoxEditorIntegrationTests.cs
git commit -m "test: capture textbox editing visual contract"
```

---

### Task 2: Add Caret And Selection Rendering To `TextBoxBase`

**Files:**
- Modify: `UI/Controls/TextBoxBase.cs`

- [ ] **Step 1: Add minimal visual properties if needed**

Prefer existing `Foreground` for caret color unless tests need separate styling. If separate properties are needed, add:

```text
CaretColorProperty: AffectsRender
SelectionBackgroundProperty: AffectsRender
```

Keep `Color` for now; do not introduce brush infrastructure.

- [ ] **Step 2: Render selection before text**

In `OnRender(...)`:

1. draw background;
2. draw border;
3. draw selection highlight rectangles clipped/limited to content bounds;
4. draw text offset by horizontal viewport;
5. draw caret if focused.

Use existing rectangle/text draw commands.

- [ ] **Step 3: Render caret after text**

Caret should render when:

- `IsKeyboardFocused == true`;
- `IsEnabled == true`;
- `UIElementVisibility.ParticipatesInRendering(this)` is true.

Do not blink in this plan.

- [ ] **Step 4: Keep render-only invalidation for caret/selection changes**

`MoveCaret(...)` and `Select(...)` should invalidate render only unless text changed.

---

### Task 3: Implement Deterministic Single-Line Text Viewport

**Files:**
- Modify: `UI/Controls/TextBoxBase.cs`
- Modify only if needed: `UI/Text/TextMeasurer.cs`

- [ ] **Step 1: Track horizontal text offset**

Add a private field such as:

```csharp
private float horizontalTextOffset;
```

This is internal rendering state, not a public scrolling API.

- [ ] **Step 2: Compute approximate caret X**

Use a helper that measures the substring before the caret with the current `TextMeasurer` and `TextRunStyle`.

This is acceptable for MVP and still uses existing text services.

- [ ] **Step 3: Keep caret visible**

After text input, delete/backspace, direct text set, and caret moves:

- if caret X minus offset is past content width, increase offset;
- if caret X minus offset is before 0, decrease offset;
- clamp offset to non-negative.

- [ ] **Step 4: Invalidate render when viewport offset changes**

Changing only horizontal offset should be render-only.

---

### Task 4: Add Minimal Keyboard Editing Coverage

**Files:**
- Modify: `UI/Controls/TextBoxBase.cs`
- Modify only if needed: `UI/Input/InputKey.cs`

- [ ] **Step 1: Add Home/End support if keys exist**

If `InputKey.Home` and `InputKey.End` already exist, handle them.

If they do not exist, do not expand the key enum unless tests prove it is low risk.

- [ ] **Step 2: Keep selection modifier scope honest**

If the keyboard snapshot does not expose Shift/Ctrl modifier state in routed key args, do not fake Shift-selection. Keep selection APIs explicit for now.

- [ ] **Step 3: Ensure handled semantics remain correct**

TextBox should not handle unrelated keys.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted TextBox tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxEditingVisualContractTests|FullyQualifiedName~TextBoxEditorIntegrationTests|FullyQualifiedName~TextBoxTests|FullyQualifiedName~TextBoxTwoWayBindingTests"
```

Expected: GREEN.

- [ ] **Step 2: Run text/input retained tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextEditorTests|FullyQualifiedName~TextInputBridgeTests|FullyQualifiedName~RetainedVerticalSliceTests|FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Controls\TextBoxBase.cs UI\Text tests\Cerneala.Tests
git commit -m "feat: add textbox caret selection viewport contract"
```
