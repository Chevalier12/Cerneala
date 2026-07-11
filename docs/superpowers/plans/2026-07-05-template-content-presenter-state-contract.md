# Template ContentPresenter State Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make code-first templates usable for normal controls without losing retained behavior. A default themed `Button` should be able to use `ControlTemplate`, `TemplateBinding<T>`, `Border`, and `ContentPresenter` instead of relying on hard-coded chrome and string rendering.

**Architecture:** Keep templates code-first and strongly typed. Do not introduce XAML, markup expansion, magic named parts, or a WPF compatibility template runtime. This plan strengthens the existing template/content presenter path so styles/themes can compose controls cleanly.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Controls`, `UI/Styling/DefaultTheme`, typed properties, retained layout/render/input.

---

## File Structure

- Modify: `UI/Controls/ContentPresenter.cs`
  - Present string content through a retained `TextBlock` when no explicit `ContentTemplate` is provided.
  - Reuse generated child when content/template identity is unchanged.
- Modify: `UI/Controls/TemplateContext.cs`
  - Add small typed convenience overloads only if they reduce boilerplate without magic.
- Modify: `UI/Styling/DefaultTheme.cs`
  - Add a default `Button` template through style, or expose `CreateButtonTemplate()` used by tests/sample.
- Modify: `UI/Controls/Button.cs`
  - Keep fallback rendering for no-template mode, but ensure templated mode does not also draw fallback chrome.
- Modify only if needed: `UI/Controls/Border.cs`
- Modify only if needed: `UI/Controls/TextBlock.cs`
- Create: `tests/Cerneala.Tests/Controls/ContentPresenterDefaultTextTests.cs`
- Create: `tests/Cerneala.Tests/Controls/TemplatedButtonStateContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Styling/DefaultThemeTemplateTests.cs`

## Important Existing Behavior

- `ControlTemplate<TControl>`, `TemplateContext<TControl>`, `TemplateInstance`, and `TemplateBinding<T>` already exist.
- `Button` inherits `ContentControl` and already uses template child when `TemplateChild is not null`.
- `ContentPresenter` can host `UIElement` content or `DataTemplate` output.
- `ContentPresenter` currently does not present plain string content without a `DataTemplate`.
- `DefaultTheme.CreateStyleSheet()` currently sets colors/properties but does not prove a templated default button path.

Target behavior:

- `ContentPresenter` can display string content with a generated retained `TextBlock`.
- Generated text presenter participates in inheritance/style/resource behavior.
- Templated `Button` uses one template root and does not run fallback `OnRender` chrome.
- Template bindings propagate button `Background`, `Foreground`, `BorderBrush`, `BorderThickness`, `Padding`, and `Content` to template children.
- Hover/pressed/focus/disabled style changes update the templated button through retained style/render work.
- Unchanged frames do not recreate template/content children.

## Rules

- [ ] Do not add XAML or markup loader work.
- [ ] Do not add named-part enforcement beyond existing diagnostic metadata.
- [ ] Do not add a full `TemplatedParent` system unless tests prove it is required.
- [ ] Do not remove fallback rendering from `Button` yet; keep no-template mode working.
- [ ] Do not add new control families.
- [ ] Do not use reflection-based template binding.

---

### Task 1: Add RED ContentPresenter And Templated Button Tests

**Files:**
- Create: `tests/Cerneala.Tests/Controls/ContentPresenterDefaultTextTests.cs`
- Create: `tests/Cerneala.Tests/Controls/TemplatedButtonStateContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Styling/DefaultThemeTemplateTests.cs`

- [ ] **Step 1: Add default text presentation tests**

Create tests:

```csharp
ContentPresenterCreatesTextBlockForStringContentWithoutTemplate()
ContentPresenterReusesGeneratedTextBlockWhenStringContentUnchanged()
ContentPresenterUpdatesGeneratedTextBlockTextWhenStringContentChanges()
ContentPresenterGeneratedTextInheritsForegroundFontAndRootResources()
ContentPresenterDoesNotCreateChildForNullContent()
```

- [ ] **Step 2: Add templated button state tests**

Create tests:

```csharp
TemplatedButtonUsesTemplateRootAndSkipsFallbackRender()
TemplatedButtonBindsContentToContentPresenter()
TemplatedButtonBindsChromePropertiesToBorder()
TemplatedButtonHoverPressedFocusDisabledStatesInvalidateTemplateRenderThroughStyle()
TemplatedButtonSecondUnchangedFrameDoesNotRecreateTemplateOrPresenterChild()
```

- [ ] **Step 3: Add default theme template tests**

Create tests:

```csharp
DefaultThemeProvidesButtonTemplateThroughStyle()
DefaultThemeButtonTemplateDisplaysStringContent()
DefaultThemeButtonTemplateRespondsToPressedPseudoClass()
DefaultThemeButtonTemplateRespondsToKeyboardFocusPseudoClass()
```

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ContentPresenterDefaultTextTests|FullyQualifiedName~TemplatedButtonStateContractTests|FullyQualifiedName~DefaultThemeTemplateTests"
```

Expected: RED because string content presentation and default templated button path are incomplete.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Controls\ContentPresenterDefaultTextTests.cs tests\Cerneala.Tests\Controls\TemplatedButtonStateContractTests.cs tests\Cerneala.Tests\UI\Styling\DefaultThemeTemplateTests.cs
git commit -m "test: capture template content presenter authoring contract"
```

---

### Task 2: Add Default String Presentation To `ContentPresenter`

**Files:**
- Modify: `UI/Controls/ContentPresenter.cs`

- [ ] **Step 1: Generate a retained `TextBlock` for string content**

When `Content` is a non-null string and `ContentTemplate` is null, create a `TextBlock` with `Text = content`.

- [ ] **Step 2: Reuse generated text child where possible**

If the previous presented child is a generated `TextBlock` and the next content is also string, update `TextBlock.Text` instead of replacing the child.

This prevents unnecessary detach/attach and preserves retained identity.

- [ ] **Step 3: Let inherited properties do the styling work**

Do not manually copy font/foreground from the presenter to the text block unless inheritance propagation fails. The desired path is:

```text
Control/ContentPresenter inherited properties -> generated TextBlock
```

- [ ] **Step 4: Preserve explicit `DataTemplate` priority**

If `ContentTemplate` is set, use it instead of the default string presentation.

---

### Task 3: Add A Default Button Template Path

**Files:**
- Modify: `UI/Styling/DefaultTheme.cs`
- Modify: `UI/Controls/Button.cs` only if tests expose fallback/render issues.

- [ ] **Step 1: Create a strongly typed button template**

Add a method such as:

```csharp
public static ControlTemplate<Button> CreateButtonTemplate()
```

Template shape should be minimal:

```text
Border
  ContentPresenter
```

- [ ] **Step 2: Add template bindings**

Use `TemplateContext<Button>.Bind(...)` for:

- `Control.BackgroundProperty` -> `Border.BackgroundProperty`
- `Control.BorderBrushProperty` -> `Border.BorderBrushProperty`
- `Control.BorderThicknessProperty` -> `Border.BorderThicknessProperty`
- `Control.PaddingProperty` -> `Border.PaddingProperty`
- `ContentControl.ContentProperty` -> `ContentPresenter.ContentProperty`
- `Control.ForegroundProperty` / `FontFamilyProperty` / `FontSizeProperty` if needed by inheritance or direct target properties.

- [ ] **Step 3: Apply through default stylesheet**

Add a typed setter for `Control.TemplateProperty` to the default `Button` style.

If generic inference is awkward, keep the template factory accessible and use it directly in tests first, then wire stylesheet.

- [ ] **Step 4: Preserve fallback mode**

Existing no-template `Button` rendering should still pass old tests.

---

### Task 4: Verify Template Retained Behavior

**Files:**
- Modify only if tests expose bugs:
  - `UI/Controls/TemplateInstance.cs`
  - `UI/Controls/TemplateBinding{T}.cs`
  - `UI/Controls/ContentControl.cs`

- [ ] **Step 1: Ensure template root identity is stable**

Applying same template repeatedly should not recreate the root.

- [ ] **Step 2: Ensure content handoff stays correct**

A templated `ContentControl` should not host the same content element directly and through `ContentPresenter` at the same time.

- [ ] **Step 3: Ensure template binding detach clears subscriptions**

Replacing template should detach old template bindings and old root children.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted template tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ContentPresenterDefaultTextTests|FullyQualifiedName~TemplatedButtonStateContractTests|FullyQualifiedName~DefaultThemeTemplateTests|FullyQualifiedName~ControlTemplateTests|FullyQualifiedName~TemplateBindingTests|FullyQualifiedName~ContentPresenterTests"
```

Expected: GREEN.

- [ ] **Step 2: Run style and core preview tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DefaultThemeVerticalSliceTests|FullyQualifiedName~RetainedAppStyleContractTests|FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Controls UI\Styling\DefaultTheme.cs tests\Cerneala.Tests
git commit -m "feat: add templated default button authoring contract"
```
