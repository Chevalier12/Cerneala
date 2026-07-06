# TextBox Caret Blink Hit Testing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `TextBox` and `PasswordBox` caret behavior usable in runtime preview: caret X must match rendered text, mouse clicks inside the text area must place the caret at the expected character, focused carets must blink deterministically, and retained rendering must rebuild only when the blink phase actually changes.

**Architecture:** Keep the existing retained UI architecture. Add one small text caret layout service beside existing text services, add one small time-sensitive render invalidation hook in the element/rendering layer, and wire `TextBoxBase` into both. Do not redesign input, text editing, retained rendering, or platform services.

**Tech Stack:** C#/.NET, xUnit, Cerneala retained UI, RoslynIndexer, existing drawing/text stack (`FontResolver`, `TextRunStyle`, `SkiaTextShaper`, `TextMeasurer`), existing `UiHost`/`IUiClock`.

---

## Current Root Cause Summary

The runtime issue is not one bug, e futut in trei locuri mici:

1. `TextBoxBase.DrawCaret()` computes caret X with `MeasureTextWidth(DisplayText[..Caret.Position])`.
2. `TextMeasurer`/`LineBreakService` currently uses approximate text width (`length * fontSize * 0.5f`) for layout, while actual rendering uses Skia/HarfBuzz glyph advances through `SkiaTextShaper`.
3. `TextBoxBase` does not handle mouse-down to map click X to caret index. The input bridge only focuses the control.
4. `TextBoxBase.ShouldRenderCaret()` has no blink state and retained rendering has no frame-time invalidation hook for time-only visual changes.

The fix must therefore cover metrics, hit testing, and time-driven retained invalidation together.

## Implementation Rules

- [ ] Start each task by reading the listed files with RoslynIndexer.
- [ ] Write or adjust RED tests before production code for every behavior change.
- [ ] After each C# or project-file modification, re-index `Cerneala.slnx` with RoslynIndexer.
- [ ] Keep changes minimal. No new public feature surface beyond the smallest needed internal/public helpers used by tests.
- [ ] Do not add multiline editing, drag selection, double-click word selection, IME composition, selection handles, or clipboard features in this plan.
- [ ] Preserve existing TextBox editing behavior: keyboard input, arrow/home/end, selection replacement, clipboard shortcuts, horizontal viewport, and password masking.

## Task 1: Add RED Tests For Exact Caret Metrics And Hit Testing

**Files to inspect**

- `UI/Controls/TextBoxBase.cs`
- `UI/Text/TextMeasurer.cs`
- `UI/Text/LineBreakService.cs`
- `UI/Text/FontResolver.cs`
- `UI/Text/ResolvedTextFont.cs`
- `UI/Text/TextRunStyle.cs`
- `UI/Drawing/Text/SkiaTextShaper.cs`
- `UI/Drawing/Text/TextShapeResult.cs`
- `tests/Cerneala.Tests/Controls/TextBoxEditingVisualContractTests.cs`

**Files to change**

- Add `tests/Cerneala.Tests/UI/Text/TextCaretLayoutTests.cs`

**Steps**

- [ ] Create tests for a new `TextCaretLayout` service in namespace `Cerneala.UI.Text`.
- [ ] Use `FontResolver` with `SystemFontSource` when a real font is available.
- [ ] Verify the RED behavior with text whose actual glyph widths are not equal to the approximation. Use `"iiiiWWWW"` at `20f` and assert that prefix caret positions are non-uniform.
- [ ] Test `GetCaretX`:
  - position `0` returns `0`
  - position `text.Length` returns the full shaped advance
  - prefix positions are monotonic
  - positions are clamped to `[0, text.Length]`
- [ ] Test `GetCaretIndexAtX`:
  - X before the text returns `0`
  - X after the text returns `text.Length`
  - X at the midpoint between two caret stops returns the nearest expected index
  - X includes a `horizontalTextOffset` argument and maps viewport coordinates back into text coordinates
- [ ] Add one fallback test using `FontResolver.Default` to prove the service still works without a `SkiaFont`.

**Expected RED**

- The new tests do not compile because `TextCaretLayout` does not exist.

**Targeted command**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextCaretLayoutTests"
```

**Expected output before implementation**

```text
error CS0246: The type or namespace name 'TextCaretLayout' could not be found
```

## Task 2: Implement TextCaretLayout With Shaped Metrics

**Files to change**

- Add `UI/Text/TextCaretLayout.cs`

**Design**

`TextCaretLayout` is a tiny service responsible for single-line caret stops and hit testing. It should not edit text and should not know about `TextBoxBase`.

**Implementation requirements**

- [ ] Add a sealed class:

```csharp
namespace Cerneala.UI.Text;

public sealed class TextCaretLayout
{
    public static TextCaretLayout Default { get; } = new();

    public float GetCaretX(string text, int position, TextRunStyle style, FontResolver resolver);

    public int GetCaretIndexAtX(string text, float x, TextRunStyle style, FontResolver resolver);
}
```

- [ ] Clamp `position` to `[0, text.Length]`.
- [ ] Resolve the font through the provided `FontResolver`.
- [ ] If `resolved.Font` is `SkiaFont`, shape the prefix using `SkiaTextShaper.Shape(style.ToDrawTextRun(resolved, prefix))`.
- [ ] Compute shaped width from `TextShapeResult.GlyphPositions`.
  - `TextShapeResult` currently stores glyph origins, not the final advance.
  - For a prefix width, shape the prefix itself and use the last glyph position only if the result exposes advance in a future edit.
  - Because current `TextShapeResult` lacks total advance, extend it minimally with a `float AdvanceWidth` property and pass the final accumulated `x` from `SkiaTextShaper.GetGlyphPositions`.
- [ ] Preserve existing constructor behavior on `TextShapeResult`; old callers must continue to compile.
- [ ] If font resolution does not return `SkiaFont`, fall back to current approximate width through `TextMeasurer.Default.Measure(prefix, style, float.PositiveInfinity).Size.Width`.
- [ ] For hit testing, build caret stops for every valid insertion index and choose the nearest stop using half-distance boundaries.
- [ ] Keep this single-line only. No wrapping, trimming, or line-break logic in this service.

**Files likely touched**

- `UI/Text/TextCaretLayout.cs`
- `UI/Drawing/Text/TextShapeResult.cs`
- `UI/Drawing/Text/SkiaTextShaper.cs`

**Re-index command after edits**

```powershell
# RoslynIndexer: re-index Cerneala.slnx, C# only
```

**Targeted command**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextCaretLayoutTests"
```

**Expected output after implementation**

```text
Passed!  - Failed: 0
```

## Task 3: Add RED Tests For TextBox Caret Geometry And Horizontal Viewport

**Files to inspect**

- `tests/Cerneala.Tests/Controls/TextBoxEditingVisualContractTests.cs`
- `UI/Controls/TextBoxBase.cs`
- `UI/Drawing/DrawCommand.cs`
- `UI/Drawing/DrawCommandList.cs`

**Files to change**

- `tests/Cerneala.Tests/Controls/TextBoxEditingVisualContractTests.cs`

**Steps**

- [ ] Add a test that renders a focused `TextBox` with text `"iiiiWWWW"` and caret at the end.
- [ ] Extract the caret rectangle command from the rendered command list.
- [ ] Compute expected X with `TextCaretLayout.Default.GetCaretX(...)`.
- [ ] Assert the caret rectangle X equals `content.X + expectedX - horizontalOffset`, allowing only a small float tolerance.
- [ ] Add a horizontal viewport regression:
  - arrange a narrow `TextBox`
  - set a long value with a caret at the end
  - render
  - assert the caret is clamped inside content bounds
  - assert the visible caret uses the same shaped metric service, not approximate length math
- [ ] Keep existing selection tests green.

**Expected RED**

- The tests fail because `TextBoxBase.DrawCaret()` still uses `MeasureTextWidth()`.

**Targeted command**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxEditingVisualContractTests"
```

**Expected output before implementation**

```text
Failed!  - Failed: 1
```

## Task 4: Wire TextBoxBase To TextCaretLayout

**Files to change**

- `UI/Controls/TextBoxBase.cs`

**Steps**

- [ ] Add a private readonly field:

```csharp
private readonly TextCaretLayout caretLayout = TextCaretLayout.Default;
```

- [ ] Replace caret X calculations in `DrawCaret`, `DrawSelection`, and `EnsureCaretVisible` with a private helper:

```csharp
private float GetCaretTextX(int position)
{
    FontResolver resolver = CreateFontResolver();
    return caretLayout.GetCaretX(DisplayText, position, CreateTextStyle(), resolver);
}
```

- [ ] Add a private `CreateFontResolver()` helper so `TextRenderer`, `TextMeasurer`, and caret layout resolve fonts consistently:

```csharp
private FontResolver CreateFontResolver()
{
    IResourceProvider? provider = ResolveResourceProvider();
    return FontResourceId is not null && provider is not null
        ? new FontResolver(provider)
        : new FontResolver();
}
```

- [ ] Keep existing `GetTextMeasurer()` behavior for non-resource layout unless changing it is required for tests.
- [ ] Ensure `PasswordBox` still uses `DisplayText`, so hit testing and caret geometry use masked text width.
- [ ] Do not change `TextEditor`, `TextDocument`, or `TextSelection`.

**Targeted commands**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxEditingVisualContractTests|FullyQualifiedName~PasswordBoxTests"
```

**Expected output after implementation**

```text
Passed!  - Failed: 0
```

## Task 5: Add RED Tests For Click-To-Caret Positioning

**Files to inspect**

- `UI/Input/InputEvents.cs`
- `UI/Input/MouseButtonEventArgs.cs`
- `UI/Input/ElementInputBridge.cs`
- `tests/Cerneala.Tests/Controls/TextBoxTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxEditingVisualContractTests.cs`

**Files to change**

- `tests/Cerneala.Tests/Controls/TextBoxTests.cs`

**Steps**

- [ ] Add `MouseDownInsideTextBoxMovesCaretToNearestCharacter`.
- [ ] Build a `UIRoot` with a `TextBox` containing `"iiiiWWWW"`.
- [ ] Arrange it at a known size.
- [ ] Dispatch a `MouseButtonEventArgs` or use the existing `ElementInputBridge` with an `InputFrame` containing a left click.
- [ ] Compute a click X from `TextCaretLayout.Default.GetCaretX(...)` between two expected caret stops.
- [ ] Assert:
  - the text box is focused
  - `Caret.Position` equals the expected index
  - selection is collapsed
  - the event is handled
- [ ] Add `MouseDownUsesHorizontalTextOffset`:
  - force a narrow viewport with long text
  - move caret to end to create horizontal offset
  - click near content left
  - assert caret maps to a later text index, not index `0`
- [ ] Add `MouseDownOutsideTextContentClampsCaret`:
  - click left of content -> caret `0`
  - click right of content -> caret `Text.Length`

**Expected RED**

- Focus likely changes, but caret remains unchanged because `TextBoxBase` has no mouse handler.

**Targeted command**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxTests"
```

**Expected output before implementation**

```text
Failed!  - Failed: 1
```

## Task 6: Implement TextBox Mouse Hit Testing

**Files to change**

- `UI/Controls/TextBoxBase.cs`

**Steps**

- [ ] In the `TextBoxBase` constructor, register a mouse-down handler:

```csharp
Handlers.AddHandler(InputEvents.MouseDownEvent, OnRoutedMouseDown);
```

- [ ] Implement `OnRoutedMouseDown`:
  - ignore handled events
  - require `MouseButtonEventArgs`
  - require `ChangedButton == InputMouseButton.Left`
  - convert absolute mouse X to local content text X:

```csharp
LayoutRect content = ContentControl.Deflate(ArrangedBounds, Insets);
float textX = mouseArgs.X - content.X + horizontalTextOffset;
```

  - compute index through `TextCaretLayout.GetCaretIndexAtX(DisplayText, textX, CreateTextStyle(), CreateFontResolver())`
  - call `MoveCaret(index)`
  - collapse selection unless shift-selection is already represented by event args; current mouse args do not expose shift, so collapse
  - mark event handled
- [ ] Reset the caret blink cycle when mouse movement, keyboard movement, or text input changes the caret. This can be a private `ResetCaretBlink()` called from `MoveCaret`, `ReceiveTextInput`, keyboard handlers, and click handler.
- [ ] Do not add drag selection in this task.

**Targeted command**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxTests|FullyQualifiedName~TextBoxEditingVisualContractTests"
```

**Expected output after implementation**

```text
Passed!  - Failed: 0
```

## Task 7: Add RED Tests For Caret Blink And Retained Frame Invalidation

**Files to inspect**

- `UI/Hosting/UiHost.cs`
- `UI/Hosting/IUiClock.cs`
- `UI/Rendering/RenderContext.cs`
- `UI/Rendering/DrawCommandListBuilder.cs`
- `UI/Rendering/RetainedRenderer.cs`
- `UI/Elements/UIElement.cs`
- `tests/Cerneala.Tests/UI/Hosting/FakeUiClock.cs`
- `tests/Cerneala.Tests/UI/Hosting/UiHostTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxEditingVisualContractTests.cs`

**Files to change**

- Add `tests/Cerneala.Tests/Controls/TextBoxCaretBlinkTests.cs`

**Steps**

- [ ] Add `FocusedCaretIsVisibleAtBlinkStart`.
- [ ] Add `FocusedCaretTurnsOffAfterHalfBlinkPeriod`.
- [ ] Add `FocusedCaretTurnsBackOnAfterFullBlinkPeriod`.
- [ ] Use `FakeUiClock` and `UiHost.Update(... elapsedTime: ...)` so the tests are deterministic.
- [ ] Render command lists after each frame and count caret rectangles.
- [ ] Add `BlinkPhaseChangeInvalidatesRetainedRenderWithoutInput`:
  - frame 1 at `0ms`, focused text box, caret visible
  - frame 2 at `250ms`, no input, same phase, render command list object/content may remain valid
  - frame 3 at `550ms`, no input, blink phase changed, render output must no longer contain the caret rectangle
  - frame 4 at `1050ms`, caret rectangle returns
- [ ] Add `UnfocusedTextBoxDoesNotScheduleBlinkRenderWork`:
  - no keyboard focus
  - repeated time-only frames do not cause render invalidation
- [ ] Use a blink period of `1000ms`, visible for the first `500ms`.

**Expected RED**

- Caret remains always visible and no time-only frame changes render output.

**Targeted command**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxCaretBlinkTests"
```

**Expected output before implementation**

```text
Failed!  - Failed: 3
```

## Task 8: Implement Time-Sensitive Render Invalidator And Caret Blink

**Files to change**

- Add `UI/Rendering/ITimeSensitiveRenderElement.cs`
- Add `UI/Rendering/TimeSensitiveRenderInvalidator.cs`
- `UI/Controls/TextBoxBase.cs`
- `UI/Hosting/UiHost.cs`

**Design**

Add a tiny generic hook so `UiHost` does not depend on `TextBoxBase`. Time-aware elements decide whether a frame timestamp changes their render output.

**Implementation requirements**

- [ ] Add an internal/public interface in `Cerneala.UI.Rendering`:

```csharp
public interface ITimeSensitiveRenderElement
{
    bool UpdateRenderTime(TimeSpan frameTime);
}
```

- [ ] Add `TimeSensitiveRenderInvalidator.Invalidate(UIElement root, TimeSpan frameTime)`:
  - traverse `VisualChildren`
  - for each `ITimeSensitiveRenderElement`, call `UpdateRenderTime(frameTime)`
  - if it returns `true`, the element itself is responsible for calling `Invalidate(InvalidationFlags.Render, reason)`
  - avoid duplicate traversal of logical children unless visual children are empty; retained rendering is visual-tree based here
- [ ] In `UiHost.Update`, compute `frameTime` once near the top:

```csharp
TimeSpan frameTime = elapsedTime ?? Clock?.GetElapsedTime() ?? TimeSpan.Zero;
```

- [ ] Call the invalidator before the first `currentRoot.Scheduler.HasWork` check, so render invalidation is processed in the same update.
- [ ] Use `frameTime` when constructing `LastFrame`.
- [ ] In `TextBoxBase`, implement `ITimeSensitiveRenderElement`.
- [ ] Add fields:

```csharp
private static readonly TimeSpan CaretBlinkPeriod = TimeSpan.FromMilliseconds(1000);
private static readonly TimeSpan CaretBlinkVisibleDuration = TimeSpan.FromMilliseconds(500);
private TimeSpan caretBlinkAnchor;
private bool caretBlinkVisible = true;
```

- [ ] `ShouldRenderCaret()` returns the existing focus/enabled/render/color checks plus `caretBlinkVisible`.
- [ ] `UpdateRenderTime(TimeSpan frameTime)`:
  - if the caret is not eligible to render, keep `caretBlinkVisible = true` and return `false`
  - compute elapsed from `caretBlinkAnchor`
  - visible when `(elapsed % CaretBlinkPeriod) < CaretBlinkVisibleDuration`
  - if phase changed, set field, invalidate render, return `true`
  - otherwise return `false`
- [ ] `ResetCaretBlink()` sets `caretBlinkAnchor` to the current last frame time if available or `TimeSpan.Zero`, sets visible true, and invalidates render if it was false.
- [ ] If `TextBoxBase` cannot access current frame time directly, store the latest frame time from `UpdateRenderTime` in `lastCaretFrameTime` and use it in `ResetCaretBlink`.
- [ ] Focus changes should reset caret blink to visible. Override `OnPropertyChanged` for `IsKeyboardFocusedProperty` and call `ResetCaretBlink()` when focused.

**Targeted command**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxCaretBlinkTests|FullyQualifiedName~UiHostTests"
```

**Expected output after implementation**

```text
Passed!  - Failed: 0
```

## Task 9: Add Playground Regression Coverage For Authoring And Getting Started

**Files to inspect**

- `Playground/Cerneala.Playground/Samples/AuthoringAppSample.cs`
- `Playground/Cerneala.Playground/Samples/GettingStartedSample.cs`
- `tests/Cerneala.Tests/Playground/Samples/PlaygroundSampleTests.cs`

**Files to change**

- `tests/Cerneala.Tests/Playground/Samples/PlaygroundSampleTests.cs`

**Steps**

- [ ] Add or extend an Authoring App sample test:
  - create the sample
  - focus the name `TextBox`
  - type `"hahahehe"`
  - run update/render
  - assert no exception
  - assert caret command exists when blink phase is visible
  - assert caret X is within the text box content bounds and near the end of typed text
- [ ] Add or extend a Getting Started sample test:
  - focus the entry `TextBox`
  - type a short value
  - run update/render
  - assert no `SkiaTextRasterizer requires a SkiaFont` exception
  - assert caret command exists at visible blink phase
- [ ] Keep these as in-memory retained UI tests. Do not launch MonoGame in unit tests.

**Targeted command**

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~PlaygroundSampleTests"
```

**Expected output after implementation**

```text
Passed!  - Failed: 0
```

## Task 10: Full Verification

**Steps**

- [ ] Re-index after the final C# edit.
- [ ] Run the TextBox-focused suite:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBox|FullyQualifiedName~PasswordBox|FullyQualifiedName~TextCaretLayoutTests"
```

- [ ] Run UI host regression tests:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostTests|FullyQualifiedName~RetainedNoWorkFrameTests"
```

- [ ] Run playground sample regression tests:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~PlaygroundSampleTests"
```

- [ ] Run the full suite:

```powershell
dotnet test
```

**Expected final output**

```text
Passed!  - Failed: 0
```

## Manual Smoke Test

After tests are green, run the playground and verify the actual user flow:

```powershell
dotnet run --project .\Playground\Cerneala.Playground\Cerneala.Playground.csproj
```

Manual checks:

- [ ] Open `Authoring App`.
- [ ] Click the text box.
- [ ] Type `hahahehe`.
- [ ] Caret appears at the end of the text, not stuck near the left.
- [ ] Caret blinks roughly once per second.
- [ ] Click between characters and caret moves near the clicked character.
- [ ] Open `Getting Started`.
- [ ] Type into the text box.
- [ ] No `SkiaTextRasterizer requires a SkiaFont` crash.

## Expected Changed Files

- `UI/Text/TextCaretLayout.cs`
- `UI/Drawing/Text/TextShapeResult.cs`
- `UI/Drawing/Text/SkiaTextShaper.cs`
- `UI/Controls/TextBoxBase.cs`
- `UI/Rendering/ITimeSensitiveRenderElement.cs`
- `UI/Rendering/TimeSensitiveRenderInvalidator.cs`
- `UI/Hosting/UiHost.cs`
- `tests/Cerneala.Tests/UI/Text/TextCaretLayoutTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxEditingVisualContractTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxCaretBlinkTests.cs`
- `tests/Cerneala.Tests/Playground/Samples/PlaygroundSampleTests.cs`

## Completion Criteria

- [ ] Caret position uses shaped text metrics when a `SkiaFont` is available.
- [ ] Caret hit testing uses the same metrics as caret rendering.
- [ ] Mouse click inside `TextBox` moves caret to the nearest character.
- [ ] Focused caret blinks deterministically using `IUiClock`/frame time.
- [ ] Time-only frames invalidate retained render only when blink phase changes.
- [ ] Authoring App typed text keeps caret visually aligned.
- [ ] Getting Started text input does not crash.
- [ ] `dotnet test` passes.

