# Default Theme And Style Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Prove Cerneala's styling/theme system is usable in a real retained sample. Core `Button`, text, and surface visuals should be theme/style-driven instead of hard-coded everywhere.

**Architecture:** Use existing `Theme`, `ThemeProvider`, `ThemeResource<T>`, `StyleSheet`, `StyleRule`, `VisualStateRule`, and `Setter<T>`. Do not introduce CSS, XAML, string selectors, implicit WPF template machinery, or a hidden global theme singleton.

**Tech Stack:** C#/.NET 8, xUnit, existing style/theme engine, retained scheduler, Playground sample.

---

## File Structure

- Modify: `UI/Styling/DefaultTheme.cs`
  - Add `CreateStyleSheet()` if useful.
  - Add only minimal theme keys if needed.
- Modify: `UI/Styling/StyleSheet.cs`
  - No broad change expected.
- Modify: `UI/Styling/StyleRule.cs`
  - No broad change expected.
- Modify: `UI/Styling/VisualStateRule.cs`
  - No broad change expected.
- Modify: `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`
  - Use theme/style for core visuals.
- Modify: `Playground/Cerneala.Playground/Samples/PlaygroundText.cs` if needed.
- Modify: `Playground/Cerneala.Playground/Game1.cs`
  - Attach default theme/style to root.
- Create: `tests/Cerneala.Tests/UI/Styling/DefaultThemeVerticalSliceTests.cs`
- Create: `tests/Cerneala.Tests/Playground/RetainedAppStyleContractTests.cs`

## Important Existing Behavior

`DefaultTheme.Create()` already provides typed theme values. `StyleApplicator` can apply base and visual-state setters. But `RetainedAppSample` still hard-codes many colors, proving drawing but not theme/style architecture.

Target behavior:

- Root can attach default theme provider and stylesheet.
- Default stylesheet provides at least button background/border/foreground visual states.
- Hover/focus/pressed pseudo-class changes flow through style invalidation.
- Theme color change invalidates styled render without forcing layout when only colors change.
- Retained sample uses style/theme for primary button and at least one text/surface path.

## Rules

- [ ] Do not introduce CSS or XAML.
- [ ] Do not build full template system features.
- [ ] Do not remove local value precedence.
- [ ] Do not style every existing control family.
- [ ] Do not make `DefaultTheme` a hidden global singleton.

---

### Task 1: Add RED Theme/Style Vertical Slice Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Styling/DefaultThemeVerticalSliceTests.cs`
- Create: `tests/Cerneala.Tests/Playground/RetainedAppStyleContractTests.cs`

- [ ] **Step 1: Add default theme tests**

Create tests:

```csharp
DefaultThemeProvidesButtonTextAndSurfaceColors()
StyleSheetAppliesDefaultButtonVisualsWithoutLocalColors()
HoverPseudoClassUpdatesStyledButtonThroughScheduler()
KeyboardFocusPseudoClassUpdatesStyledButtonThroughScheduler()
ThemeChangeInvalidatesStyledControlsWithoutLayoutWhenOnlyColorsChange()
```

Test intent:

- Use root with `ThemeProvider` + default stylesheet.
- Add button without local background/border where possible.
- Process frame and assert styled values come from style/theme source.
- Toggle hover/focus and assert style + render work.
- Change only theme colors and assert no measure work unless a style setter affects layout.

- [ ] **Step 2: Add sample style contract test**

Create:

```csharp
RetainedAppSampleUsesThemeOrStyleForAtLeastCoreButtonAndTextVisuals()
```

Keep pragmatic: sample does not need zero local colors, but primary button and one text/surface path must prove theme/style use.

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DefaultThemeVerticalSliceTests|FullyQualifiedName~RetainedAppStyleContractTests"
```

Expected: RED because no default stylesheet/sample theme contract exists yet.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Styling\DefaultThemeVerticalSliceTests.cs tests\Cerneala.Tests\Playground\RetainedAppStyleContractTests.cs
git commit -m "test: capture default theme vertical slice"
```

---

### Task 2: Add Default StyleSheet Factory

**Files:**
- Modify: `UI/Styling/DefaultTheme.cs`

- [ ] **Step 1: Reuse existing theme keys first**

Prefer existing keys:

```csharp
BackgroundKey
ForegroundKey
SurfaceKey
BorderKey
AccentKey
```

Add new keys only if tests prove they are necessary for core button visual states.

- [ ] **Step 2: Add `CreateStyleSheet()`**

Suggested API:

```csharp
public static StyleSheet CreateStyleSheet()
```

Include base and visual-state rules for `Button` using typed setters and `ThemeResource<DrawColor>`.

- [ ] **Step 3: Preserve local precedence**

A local value must still beat style/theme values. Add/adjust tests if needed.

---

### Task 3: Wire Default Theme/Style Into Playground

**Files:**
- Modify: `Playground/Cerneala.Playground/Game1.cs`
- Modify: `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`
- Modify: `Playground/Cerneala.Playground/Samples/PlaygroundText.cs` if needed.

- [ ] **Step 1: Attach theme/style at root**

In `Game1.LoadContent()` after root creation:

```csharp
uiRoot.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
uiRoot.SetStyleSheet(DefaultTheme.CreateStyleSheet());
```

- [ ] **Step 2: Remove local colors from primary styled paths**

Let default stylesheet provide primary button background/border/focus/hover/pressed visuals. Keep padding, command, and content local.

- [ ] **Step 3: Keep retained sample stable**

Existing retained sample tests for no-work unchanged frame, command mutation, font resource mutation, and draw purity must still pass.

---

### Task 4: Verify GREEN And Regressions

- [ ] **Step 1: Run styling tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DefaultThemeVerticalSliceTests|FullyQualifiedName~RetainedAppStyleContractTests|FullyQualifiedName~Style|FullyQualifiedName~Theme"
```

Expected: GREEN.

- [ ] **Step 2: Run retained sample tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedAppSampleContractTests|FullyQualifiedName~RetainedVerticalSliceTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Styling Playground\Cerneala.Playground tests\Cerneala.Tests\UI\Styling\DefaultThemeVerticalSliceTests.cs tests\Cerneala.Tests\Playground\RetainedAppStyleContractTests.cs
git commit -m "feat: prove default theme styling vertical slice"
```
