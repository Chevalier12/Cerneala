# Clarify Text Services MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the current text stack honest: deterministic MVP line breaking is allowed, but measured wrapping and rendered output must match, and text content controls must use shared text services instead of local formulas.

**Architecture:** Keep the low-level Skia/HarfBuzz drawing text pipeline as-is. Treat `LineBreakService` as an MVP deterministic approximation, not a production line-breaking engine, and tighten the higher-level contract so `TextRenderer` draws one command per measured `TextLine`. Keep `Button` scoped by reusing `TextMeasurer`/`TextRenderer` for string content instead of introducing a new templating system pass.

**Tech Stack:** C#, xUnit, `Cerneala.slnx`, RoslynIndexer, Markdown, existing `UI/Text`, `UI/Controls`, `ROADMAPv2.md`, `ROADMAPv2_AUDIT.md`, and `AUDIT_FIX_PLAN.md`.

---

## File Structure

- Modify: `ROADMAPv2.md`
  - Mark `TextWrapping`, `TextTrimming`, `LineBreakService`, and `TextRenderer` as MVP/partial where current behavior is approximate.
  - Add acceptance text saying production wrapping/trimming/multiline rendering is not claimed yet.
- Create: `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`
  - Prove `TextRenderer.Render(...)` draws the same lines returned by `TextMeasurer.Measure(...)`.
  - Prove empty text still returns measurement without draw commands.
- Modify: `UI/Text/TextRenderer.cs`
  - Draw one `DrawText` command for each measured line.
  - Offset each line by deterministic MVP line height: `style.FontSize * style.Scale`.
- Create: `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`
  - Prove `Button` string measurement goes through `TextMeasurer`.
  - Prove `Button` string rendering goes through `TextRenderer`.
- Modify: `UI/Controls/Button.cs`
  - Add injectable `TextMeasurer` and `TextRenderer` properties like `TextBlock`.
  - Replace `MeasureTextContent()` local formula with `TextMeasurer.Measure(...)`.
  - Replace direct `DrawingContext.DrawText(...)` string rendering with `TextRenderer.Render(...)`.
- Modify: `AUDIT_FIX_PLAN.md`
  - Link this detailed Plan 6.
  - Mark Plan 6 checklist complete only after tests and roadmap updates pass.
- Modify: `ROADMAPv2_AUDIT.md`
  - Add an implementation note after the text services audit finding once the code and docs are verified.

## Important Existing Context

`ROADMAPv2_AUDIT.md` identifies this exact risk:

```markdown
Problem: `LineBreakService` uses `fontSize * 0.5f` character width and substring slicing. `TextRenderer.Render(...)` draws the whole original text once, not per measured line. `Button.MeasureStringContent()` duplicates text measurement with the same rough formula instead of using `TextMeasurer`/`TextBlock`/`ContentPresenter`.
```

Current production code confirms the problem:

```csharp
// UI/Text/TextRenderer.cs
TextMeasureResult measurement = textMeasurer.Measure(text, style, availableWidth);
ResolvedTextFont font = fontResolver.Resolve(style);
drawingContext.DrawText(style.ToDrawTextRun(font, text), position, style.Color);
return measurement;
```

```csharp
// UI/Controls/Button.cs
private LayoutSize MeasureTextContent()
{
    return Content is string text
        ? new LayoutSize(text.Length * FontSize * 0.5f, FontSize)
        : LayoutSize.Zero;
}
```

Do not replace the MVP line breaker with full Unicode line breaking, bidi layout, ellipsis trimming, or glyph-accurate wrapping in this plan. The fix is to document the limitation and make current measurement/render/control behavior internally consistent.

---

### Task 1: Make Roadmap Text Scope Honest

**Files:**
- Modify: `ROADMAPv2.md`

- [ ] **Step 1: Replace the Section 11 opening**

Replace:

```markdown
This phase adds layout and cache services for controls such as `TextBlock` without rebuilding shaping/rasterization. The existing Skia/HarfBuzz code remains the low-level text engine.
```

with:

```markdown
This phase adds layout and cache services for controls such as `TextBlock` without rebuilding shaping/rasterization. The existing Skia/HarfBuzz code remains the low-level text engine.

Current line breaking is a deterministic MVP approximation: `LineBreakService` uses fixed-width character slicing from `fontSize * scale * 0.5f`. That is acceptable for retained layout tests and simple samples, but it is not production Unicode line breaking, glyph-accurate wrapping, ellipsis trimming, or full multiline text layout.
```

- [ ] **Step 2: Correct text service maturity lines**

Replace these lines in Section 11:

```markdown
- [x] `UI/Text/TextRenderer.cs` — records text commands with `DrawingContext.DrawText`.
- [x] `UI/Text/TextWrapping.cs`
- [x] `UI/Text/TextTrimming.cs` — Core if MVP does not need trimming.
- [x] `UI/Text/LineBreakService.cs` — Core if MVP only supports single-line text.
```

with:

```markdown
- [~] `UI/Text/TextRenderer.cs` — records measured MVP text lines with `DrawingContext.DrawText`; production multiline shaping remains later.
- [~] `UI/Text/TextWrapping.cs` — option exists; current wrapping is deterministic MVP fixed-character slicing.
- [~] `UI/Text/TextTrimming.cs` — enum exists with `None`; ellipsis and clipping semantics remain later.
- [~] `UI/Text/LineBreakService.cs` — deterministic MVP approximation, not production Unicode line breaking.
```

- [ ] **Step 3: Extend the Section 11 test list**

After:

```markdown
- [x] `tests/Cerneala.Tests/UI/Text/TextRendererTests.cs`
```

insert:

```markdown
- [x] `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`
```

After:

```markdown
- [x] `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs`
```

insert:

```markdown
- [x] `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`
```

- [ ] **Step 4: Replace the Section 11 acceptance checklist**

Replace:

```markdown
- [x] Text content changes invalidate text metrics and render commands.
- [x] Text color changes invalidate render commands without forcing text shaping when glyph metrics are unchanged.
- [x] Font family or font size changes invalidate measurement and render.
- [x] Re-rendering unchanged text reuses cached text layout and retained render commands.
```

with:

```markdown
- [x] Text content changes invalidate text metrics and render commands.
- [x] Text color changes invalidate render commands without forcing text shaping when glyph metrics are unchanged.
- [x] Font family or font size changes invalidate measurement and render.
- [x] Re-rendering unchanged text reuses cached text layout and retained render commands.
- [x] Rendered MVP wrapped lines match the lines returned by `TextMeasurer`.
- [x] `Button` string content uses shared text services instead of local text-width formulas.
- [ ] Production wrapping, trimming, bidi-aware multiline layout, and glyph-accurate line breaking are proven before text services are marked scenario-complete.
```

- [ ] **Step 5: Verify roadmap wording**

Run:

```powershell
Select-String -LiteralPath ROADMAPv2.md -Pattern "deterministic MVP approximation|TextRendererWrapContractTests|ButtonContentArchitectureTests|Production wrapping" -Context 1,4
```

Expected: output shows Section 11 explicitly marks current wrapping as MVP-only and includes the two new test files.

- [ ] **Step 6: Commit roadmap text clarification**

```powershell
git add ROADMAPv2.md
git commit -m "docs: clarify text services mvp scope"
```

---

### Task 2: Prove Wrapped Rendering Must Match Measured Lines

**Files:**
- Create: `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`

- [ ] **Step 1: Write the failing wrap contract tests**

Create `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextRendererWrapContractTests
{
    [Fact]
    public void RenderDrawsOneCommandPerMeasuredWrappedLine()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextRenderer renderer = new(FontResolver.Default, measurer);
        DrawCommandList commands = new();
        TextRunStyle style = new("Default", 16, TextWrapping.Wrap);

        TextMeasureResult measurement = renderer.Render(
            new DrawingContext(commands),
            "ABCD",
            style,
            16,
            new DrawPoint(4, 6),
            DrawColor.White);

        Assert.Equal(2, measurement.LineCount);
        Assert.Collection(
            measurement.Lines,
            line => Assert.Equal("AB", line.Text),
            line => Assert.Equal("CD", line.Text));
        Assert.Equal(2, commands.Count);
        Assert.Equal("AB", commands[0].Text);
        Assert.Equal(new DrawPoint(4, 6), commands[0].Position);
        Assert.Equal("CD", commands[1].Text);
        Assert.Equal(new DrawPoint(4, 22), commands[1].Position);
    }

    [Fact]
    public void RenderUsesSameLayoutCacheForMeasurementAndLineDrawing()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextRenderer renderer = new(FontResolver.Default, measurer);
        TextRunStyle style = new("Default", 16, TextWrapping.Wrap);

        renderer.Render(new DrawingContext(new DrawCommandList()), "ABCD", style, 16, default, DrawColor.White);
        renderer.Render(new DrawingContext(new DrawCommandList()), "ABCD", style, 16, default, DrawColor.White);

        Assert.Equal(1, cache.Misses);
        Assert.Equal(1, cache.Hits);
    }
}
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore --filter "FullyQualifiedName~TextRendererWrapContractTests"
```

Expected: `RenderDrawsOneCommandPerMeasuredWrappedLine` fails because current `TextRenderer` emits one `DrawText` command with `ABCD`.

If `--no-restore` fails due missing restored packages, run:

```powershell
dotnet test Cerneala.slnx --filter "FullyQualifiedName~TextRendererWrapContractTests"
```

Expected: same test failure.

- [ ] **Step 3: Commit red text renderer contract tests**

```powershell
git add tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs
git commit -m "test: cover text renderer wrapped line contract"
```

---

### Task 3: Make TextRenderer Draw Measured Lines

**Files:**
- Modify: `UI/Text/TextRenderer.cs`
- Test: `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`
- Test: `tests/Cerneala.Tests/UI/Text/TextRendererTests.cs`

- [ ] **Step 1: Replace whole-text rendering with measured-line rendering**

In `UI/Text/TextRenderer.cs`, replace:

```csharp
ResolvedTextFont font = fontResolver.Resolve(style);
drawingContext.DrawText(style.ToDrawTextRun(font, text), position, style.Color);
return measurement;
```

with:

```csharp
ResolvedTextFont font = fontResolver.Resolve(style);
float lineHeight = style.FontSize * style.Scale;
for (int i = 0; i < measurement.Lines.Count; i++)
{
    TextLine line = measurement.Lines[i];
    DrawPoint linePosition = new(position.X, position.Y + (i * lineHeight));
    drawingContext.DrawText(style.ToDrawTextRun(font, line.Text), linePosition, style.Color);
}

return measurement;
```

- [ ] **Step 2: Run focused text renderer tests**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore --filter "FullyQualifiedName~TextRenderer"
```

Expected: all `TextRendererTests` and `TextRendererWrapContractTests` pass.

If `--no-restore` fails due missing restored packages, run:

```powershell
dotnet test Cerneala.slnx --filter "FullyQualifiedName~TextRenderer"
```

Expected: all matching tests pass.

- [ ] **Step 3: Re-index after C# modification**

Run RoslynIndexer on `Cerneala.slnx` with C#-only indexing:

```text
roslyn_index repoRoot=C:\Users\Shadow\Desktop\Cerneala configPath=Cerneala.slnx includeGenerated=false includeNonCSharpText=false
```

Expected: index succeeds with no C# parse errors.

- [ ] **Step 4: Commit renderer fix**

```powershell
git add UI/Text/TextRenderer.cs
git commit -m "fix: render measured text lines"
```

---

### Task 4: Prove Button Uses Shared Text Services

**Files:**
- Create: `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`

- [ ] **Step 1: Write failing button architecture tests**

Create `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

public sealed class ButtonContentArchitectureTests
{
    [Fact]
    public void StringContentMeasurementUsesSharedTextMeasurer()
    {
        RecordingTextMeasurer measurer = new(new TextMeasureResult(
            new LayoutSize(25, 9),
            1,
            new TextLayoutKey("Go", "Default", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1),
            "Default",
            [new TextLine("Go", 25)]));
        Button button = new()
        {
            Content = "Go",
            TextMeasurer = measurer
        };

        LayoutSize desired = button.Measure(new MeasureContext(new LayoutSize(100, 50)));

        Assert.Equal(1, measurer.Calls);
        Assert.Equal("Go", measurer.LastText);
        Assert.Equal(100, measurer.LastAvailableWidth);
        Assert.Equal(new LayoutSize(25, 9), desired);
    }

    [Fact]
    public void StringContentRenderingUsesSharedTextRenderer()
    {
        RecordingTextRenderer renderer = new();
        Button button = new()
        {
            Content = "Go",
            Foreground = DrawColor.White,
            TextRenderer = renderer
        };
        DrawCommandList commands = new();

        button.Arrange(new ArrangeContext(new LayoutRect(3, 4, 40, 20)));
        button.Render(new RenderContext(new DrawingContext(commands), button.ArrangedBounds));

        Assert.Equal(1, renderer.Calls);
        Assert.Equal("Go", renderer.LastText);
        Assert.Equal(new DrawPoint(3, 4), renderer.LastPosition);
        Assert.Equal(40, renderer.LastAvailableWidth);
    }

    private sealed class RecordingTextMeasurer(TextMeasureResult result) : TextMeasurer
    {
        public int Calls { get; private set; }

        public string? LastText { get; private set; }

        public float LastAvailableWidth { get; private set; }

        public override TextMeasureResult Measure(string text, TextRunStyle style, float availableWidth)
        {
            Calls++;
            LastText = text;
            LastAvailableWidth = availableWidth;
            return result;
        }
    }

    private sealed class RecordingTextRenderer : TextRenderer
    {
        public int Calls { get; private set; }

        public string? LastText { get; private set; }

        public float LastAvailableWidth { get; private set; }

        public DrawPoint LastPosition { get; private set; }

        public override TextMeasureResult Render(
            DrawingContext drawingContext,
            string text,
            TextRunStyle style,
            float availableWidth,
            DrawPoint position,
            DrawColor color)
        {
            Calls++;
            LastText = text;
            LastAvailableWidth = availableWidth;
            LastPosition = position;
            return new TextMeasureResult(
                new LayoutSize(12, 16),
                1,
                new TextLayoutKey(text, "Default", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1),
                "Default",
                [new TextLine(text, 12)]);
        }
    }
}
```

- [ ] **Step 2: Run the new button architecture tests to verify they fail**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore --filter "FullyQualifiedName~ButtonContentArchitectureTests"
```

Expected: build fails because `Button.TextMeasurer` and `Button.TextRenderer` do not exist yet, or tests fail because `Button` still uses local measurement/direct drawing.

If `--no-restore` fails due missing restored packages, run:

```powershell
dotnet test Cerneala.slnx --filter "FullyQualifiedName~ButtonContentArchitectureTests"
```

Expected: same compile/test failure.

- [ ] **Step 3: Commit red button architecture tests**

```powershell
git add tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs
git commit -m "test: cover button text service usage"
```

---

### Task 5: Route Button String Content Through Text Services

**Files:**
- Modify: `UI/Controls/Button.cs`
- Test: `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`
- Test: `tests/Cerneala.Tests/Controls/ButtonTests.cs`

- [ ] **Step 1: Add text namespace and service fields**

In `UI/Controls/Button.cs`, add this using:

```csharp
using Cerneala.UI.Text;
```

Inside `public class Button : ButtonBase`, before `ContentProperty`, add:

```csharp
private TextMeasurer textMeasurer = TextMeasurer.Default;
private TextRenderer textRenderer = TextRenderer.Default;
```

- [ ] **Step 2: Add injectable text service properties**

After the `Content` property in `UI/Controls/Button.cs`, add:

```csharp
public TextMeasurer TextMeasurer
{
    get => textMeasurer;
    set
    {
        ArgumentNullException.ThrowIfNull(value);
        if (ReferenceEquals(textMeasurer, value))
        {
            return;
        }

        textMeasurer = value;
        IncrementLayoutVersion();
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Button text measurer changed");
    }
}

public TextRenderer TextRenderer
{
    get => textRenderer;
    set
    {
        ArgumentNullException.ThrowIfNull(value);
        if (ReferenceEquals(textRenderer, value))
        {
            return;
        }

        textRenderer = value;
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Render, "Button text renderer changed");
    }
}
```

- [ ] **Step 3: Replace `MeasureTextContent()`**

Replace:

```csharp
private LayoutSize MeasureTextContent()
{
    return Content is string text
        ? new LayoutSize(text.Length * FontSize * 0.5f, FontSize)
        : LayoutSize.Zero;
}
```

with:

```csharp
private LayoutSize MeasureTextContent(LayoutSize availableSize)
{
    return Content is string text
        ? TextMeasurer.Measure(text, CreateTextStyle(), availableSize.Width).Size
        : LayoutSize.Zero;
}
```

- [ ] **Step 4: Update `MeasureCore(...)` to pass available size**

Replace:

```csharp
LayoutSize contentSize = ContentElement?.Measure(new MeasureContext(ContentControl.Deflate(context.AvailableSize, insets), context.Rounding)) ??
    MeasureTextContent();
```

with:

```csharp
LayoutSize available = ContentControl.Deflate(context.AvailableSize, insets);
LayoutSize contentSize = ContentElement?.Measure(new MeasureContext(available, context.Rounding)) ??
    MeasureTextContent(available);
```

- [ ] **Step 5: Replace direct string rendering**

In `OnRender(...)`, replace:

```csharp
if (Content is string text && !string.IsNullOrEmpty(text))
{
    DrawPoint point = new(context.Bounds.X + Insets.Left, context.Bounds.Y + Insets.Top);
    context.DrawingContext.DrawText(new DrawTextRun(new ControlTextFont(FontFamily, FontSize), text, FontSize), point, Foreground);
}
```

with:

```csharp
if (Content is string text && !string.IsNullOrEmpty(text))
{
    LayoutRect contentBounds = ContentControl.Deflate(context.Bounds, Insets);
    DrawPoint point = new(contentBounds.X, contentBounds.Y);
    TextRenderer.Render(context.DrawingContext, text, CreateTextStyle(), contentBounds.Width, point, Foreground);
}
```

- [ ] **Step 6: Add shared text style helper**

After `MeasureTextContent(...)`, add:

```csharp
private TextRunStyle CreateTextStyle()
{
    return new TextRunStyle(FontFamily, FontSize, color: Foreground);
}
```

- [ ] **Step 7: Run focused button tests**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore --filter "FullyQualifiedName~Button"
```

Expected: `ButtonContentArchitectureTests`, `ButtonTests`, and primitive button tests pass.

If `--no-restore` fails due missing restored packages, run:

```powershell
dotnet test Cerneala.slnx --filter "FullyQualifiedName~Button"
```

Expected: all matching tests pass.

- [ ] **Step 8: Re-index after C# modification**

Run RoslynIndexer on `Cerneala.slnx` with C#-only indexing:

```text
roslyn_index repoRoot=C:\Users\Shadow\Desktop\Cerneala configPath=Cerneala.slnx includeGenerated=false includeNonCSharpText=false
```

Expected: index succeeds with no C# parse errors.

- [ ] **Step 9: Commit button text service fix**

```powershell
git add UI/Controls/Button.cs
git commit -m "fix: route button text through shared services"
```

---

### Task 6: Update Audit Tracking

**Files:**
- Modify: `AUDIT_FIX_PLAN.md`
- Modify: `ROADMAPv2_AUDIT.md`

- [ ] **Step 1: Add Plan 6 detailed-plan link**

Under `### Plan 6: clarify-text-services-mvp` in `AUDIT_FIX_PLAN.md`, insert:

```markdown
Detailed plan: `docs/superpowers/plans/2026-07-05-clarify-text-services-mvp.md`
```

- [ ] **Step 2: Mark Plan 6 checklist complete only after Tasks 1-5 pass**

After roadmap edits and tests pass, change Plan 6 checklist from:

```markdown
- [ ] Mark current line breaking as deterministic MVP approximation.
- [ ] Do not claim production wrapping/trimming/multiline rendering until measurement and rendering align.
- [ ] Make controls with text content use shared text services or a content presenter path.
- [ ] Add `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`.
- [ ] Add `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`.
```

to:

```markdown
- [x] Mark current line breaking as deterministic MVP approximation.
- [x] Do not claim production wrapping/trimming/multiline rendering until measurement and rendering align.
- [x] Make controls with text content use shared text services or a content presenter path.
- [x] Add `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`.
- [x] Add `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`.
```

- [ ] **Step 3: Add ROADMAPv2_AUDIT implementation note**

Under `ROADMAPv2_AUDIT.md` > `## Should Fix` > `### 8. Text services are MVP-fake but ROADMAPv2 reads mature`, after the required changes list, add:

```markdown
Implementation note: fixed by `clarify-text-services-mvp`; `ROADMAPv2.md` now marks line breaking, wrapping, trimming, and multiline rendering as deterministic MVP/partial scope, `TextRenderer` draws measured MVP lines instead of the original unwrapped string, and `Button` string content uses shared text services instead of local width formulas.
```

- [ ] **Step 4: Verify tracking docs**

Run:

```powershell
Select-String -LiteralPath AUDIT_FIX_PLAN.md,ROADMAPv2_AUDIT.md -Pattern "clarify-text-services-mvp|TextRendererWrapContractTests|ButtonContentArchitectureTests|Implementation note: fixed by" -Context 0,6
```

Expected: `AUDIT_FIX_PLAN.md` links the detailed plan and `ROADMAPv2_AUDIT.md` records the completed text-services note.

- [ ] **Step 5: Commit audit tracking update**

```powershell
git add AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md
git commit -m "docs: record text services mvp clarification"
```

---

### Task 7: Full Verification

**Files:**
- No additional edits unless verification exposes a missed text contract or stale checklist item.

- [ ] **Step 1: Run focused text and button test suite**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore --filter "FullyQualifiedName~TextRenderer|FullyQualifiedName~Button"
```

Expected: matching tests pass.

If `--no-restore` fails due missing restored packages, run:

```powershell
dotnet test Cerneala.slnx --filter "FullyQualifiedName~TextRenderer|FullyQualifiedName~Button"
```

Expected: matching tests pass.

- [ ] **Step 2: Run full solution tests**

Run:

```powershell
dotnet test Cerneala.slnx --no-restore
```

Expected: all tests pass.

If `--no-restore` fails due missing restored packages, run:

```powershell
dotnet test Cerneala.slnx
```

Expected: all tests pass.

- [ ] **Step 3: Verify roadmap and audit wording**

Run:

```powershell
$required = @(
  "Current line breaking is a deterministic MVP approximation",
  "Rendered MVP wrapped lines match the lines returned by `TextMeasurer`",
  "Production wrapping, trimming, bidi-aware multiline layout",
  "TextRendererWrapContractTests",
  "ButtonContentArchitectureTests",
  "Implementation note: fixed by `clarify-text-services-mvp`"
)

$roadmap = Get-Content -LiteralPath ROADMAPv2.md -Raw
$audit = Get-Content -LiteralPath ROADMAPv2_AUDIT.md -Raw
foreach ($phrase in $required[0..4]) {
  if ($roadmap -notlike "*$phrase*") {
    throw "Missing required roadmap phrase: $phrase"
  }
}

if ($audit -notlike "*$($required[5])*") {
  throw "Missing audit implementation note."
}
```

Expected: command exits successfully with no missing phrase.

- [ ] **Step 4: Verify final status**

Run:

```powershell
git status --short
git diff --stat
```

Expected: no uncommitted files after the task commits, or only intentional files before the final commit.

- [ ] **Step 5: Commit verification corrections if needed**

If verification exposes missed wording or stale unchecked Plan 6 items, make the smallest correction and commit:

```powershell
git add ROADMAPv2.md AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md UI/Text/TextRenderer.cs UI/Controls/Button.cs tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs
git commit -m "fix: complete text services mvp verification"
```

If no files changed, do not create an empty commit.

---

## Self-Review

### Spec Coverage

- Mark current line breaking as deterministic MVP approximation: Task 1 updates `ROADMAPv2.md`.
- Do not claim production wrapping/trimming/multiline rendering until measurement and rendering align: Task 1 changes status lines and acceptance checklist.
- Make controls with text content use shared text services or a content presenter path: Tasks 4-5 route `Button` string content through `TextMeasurer` and `TextRenderer`.
- Add `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`: Task 2.
- Add `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`: Task 4.
- Update audit tracking artifacts: Task 6.
- Full verification: Task 7.

### Placeholder Scan

This plan contains no `TBD`, `TODO`, "implement later", or vague "add tests" instructions. Each code-changing task includes exact code and each verification task includes exact commands with expected results.

### Type Consistency

The plan uses existing types and members: `TextRenderer.Render(...)`, `TextMeasurer.Measure(...)`, `TextRunStyle`, `TextLine`, `TextMeasureResult`, `TextLayoutKey`, `Button.Content`, `LayoutSize`, `LayoutRect`, `MeasureContext`, `ArrangeContext`, and `RenderContext`. New `Button.TextMeasurer` and `Button.TextRenderer` properties mirror `TextBlock` service injection semantics.
