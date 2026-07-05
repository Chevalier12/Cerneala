# Consolidate Button Content Composition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove duplicated content ownership from `Button`. `ButtonBase` should inherit content behavior from `ContentControl`, while `Button` keeps only button-specific visuals, command state, press state, and shared text rendering for simple string content.

**Architecture:** Content composition should have one owner. `ContentControl` already owns `Content`, UIElement child attach/detach, template handoff, and content validation. `Button` currently duplicates those behaviors with its own `ContentProperty`, child ownership logic, and template-change handling. This plan makes `ButtonBase : ContentControl`, removes duplicate `Button.Content`, and preserves the existing MVP behavior that string content can be measured/rendered with shared `TextMeasurer`/`TextRenderer` until a general text `ContentPresenter` policy is designed later.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala controls/layout/rendering/text services.

---

## File Structure

- Modify: `UI/Controls/Primitives/ButtonBase.cs`
  - Change base class from `Control` to `ContentControl`.
  - Keep command and press state here.
  - Add focus defaults if the focus plan has landed.
- Modify: `UI/Controls/Button.cs`
  - Remove `ContentProperty` declaration.
  - Remove duplicate `Content` property implementation.
  - Remove duplicate child ownership methods.
  - Use inherited `ContentControl.Content`.
  - Keep string measurement/rendering through shared text services for MVP.
- Modify: `UI/Controls/Primitives/ToggleButton.cs`
  - Ensure inheritance chain still compiles: `ToggleButton : Button : ButtonBase : ContentControl`.
- Modify: `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`
  - Add tests proving `Button` uses `ContentControl.ContentProperty` and content ownership.
- Modify: `tests/Cerneala.Tests/Controls/ButtonTests.cs`
  - Update any assumptions around `Button.ContentProperty` identity.
- Modify: `tests/Cerneala.Tests/Controls/ContentControlTests.cs`
  - Add regression coverage if needed for templated button content.
- Modify: `tests/Cerneala.Tests/Controls/ToggleButtonTests.cs`
  - Ensure toggle behavior survives inheritance change.

## Important Existing Behavior

Current `Button` duplicates `ContentControl`:

```csharp
public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(... typeof(Button) ...);

public object? Content
{
    get => GetValue(ContentProperty);
    set { ... ValidateCanAttachContentElement ... AddContentElement ... }
}
```

`ContentControl` already has:

```csharp
public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(... typeof(ContentControl) ...);

public object? Content
{
    get => GetValue(ContentProperty);
    set { ... ValidateCanOwnChild ... AddContentElement ... }
}
```

Target hierarchy:

```text
Control
  ContentControl
    ButtonBase
      Button
        ToggleButton
```

`Button` may still override `MeasureCore`, `ArrangeCore`, and `OnRender` to draw button chrome and simple string content. It must not own a second content property or custom UIElement child attach/detach code.

---

### Task 1: Add RED Tests For Button Content Ownership

**Files:**
- Modify: `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`
- Modify: `tests/Cerneala.Tests/Controls/ButtonTests.cs`

- [ ] **Step 1: Add tests proving Button uses ContentControl property identity**

In `ButtonContentArchitectureTests`, add:

```csharp
[Fact]
public void ButtonUsesContentControlContentProperty()
{
    Button button = new();

    button.SetValue(ContentControl.ContentProperty, "Go");

    Assert.Equal("Go", button.Content);
    Assert.Same(ContentControl.ContentProperty, button.GetType().BaseType!.BaseType == typeof(ContentControl)
        ? ContentControl.ContentProperty
        : ContentControl.ContentProperty);
}
```

Prefer a cleaner assertion if reflection boundary tests dislike `BaseType`. The essential assertion is that setting `ContentControl.ContentProperty` affects `button.Content` and there is no separate `Button.ContentProperty` to keep in sync.

- [ ] **Step 2: Add UIElement ownership tests**

Add:

```csharp
[Fact]
public void ButtonElementContentUsesContentControlOwnership()
{
    Button button = new();
    UIElement child = new();

    button.Content = child;

    Assert.Same(button, child.LogicalParent);
    Assert.Same(button, child.VisualParent);
    Assert.Contains(child, button.LogicalChildren);
    Assert.Contains(child, button.VisualChildren);
}
```

Also test replacement and rejected reparent behavior by mirroring `ContentControlTests` expectations.

- [ ] **Step 3: Keep string text service tests**

Keep the existing tests:

- `StringContentMeasurementUsesSharedTextMeasurer`
- `StringContentRenderingUsesSharedTextRenderer`

They should still pass after consolidation.

- [ ] **Step 4: Run button tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ButtonContentArchitectureTests|FullyQualifiedName~ButtonTests|FullyQualifiedName~ToggleButtonTests"
```

Expected: new tests fail because `Button` still owns a separate content property and `ButtonBase` does not inherit `ContentControl`.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Controls\ButtonContentArchitectureTests.cs tests\Cerneala.Tests\Controls\ButtonTests.cs tests\Cerneala.Tests\Controls\ToggleButtonTests.cs
git commit -m "test: capture button content composition contract"
```

---

### Task 2: Change `ButtonBase` To Inherit `ContentControl`

**Files:**
- Modify: `UI/Controls/Primitives/ButtonBase.cs`

- [ ] **Step 1: Change base class**

Replace:

```csharp
public class ButtonBase : Control, IInputPressable, IInputCommandSource
```

with:

```csharp
public class ButtonBase : ContentControl, IInputPressable, IInputCommandSource
```

The file already imports `Cerneala.UI.Controls`; keep or adjust usings as needed.

- [ ] **Step 2: Preserve command behavior**

Do not move or rewrite:

- `IsPressedProperty`
- `CommandProperty`
- `CommandParameterProperty`
- `CanExecuteCommand(...)`
- `ExecuteCommand(...)`
- `RefreshCommandState(...)`

- [ ] **Step 3: Add constructor only if focus policy has landed**

If `Focusable`/`IsTabStop` exist, use:

```csharp
public ButtonBase()
{
    Focusable = true;
    IsTabStop = true;
}
```

If the focus plan has not landed, skip this step and leave focus defaults to that plan.

- [ ] **Step 4: Run compile-targeted tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Button"
```

Expected: compile may still fail until Task 3 removes duplicate `Button.Content` members.

- [ ] **Step 5: Do not commit yet if compile fails**

This task can be committed together with Task 3 if the intermediate state does not compile.

---

### Task 3: Remove Duplicate Content From `Button`

**Files:**
- Modify: `UI/Controls/Button.cs`

- [ ] **Step 1: Delete duplicate content property**

Remove from `Button.cs`:

- `public static readonly UiProperty<object?> ContentProperty`
- custom `public object? Content` property
- `private UIElement? ContentElement`
- `private bool HostsContentDirectly`
- `AddContentElement(...)`
- `RemoveContentElement(...)`
- `ReleaseContentElementFromOwnedSubtree(...)`
- `ValidateCanAttachContentElement(...)`
- `OnPropertyChanged(...)` override that exists only for template/content handoff

`Button` should use inherited `ContentControl.Content` and inherited content child ownership.

- [ ] **Step 2: Update measure/arrange to use inherited content behavior**

In `MeasureCore`, keep button chrome behavior but use inherited content:

```csharp
if (TemplateChild is not null)
{
    return base.MeasureCore(context);
}

Thickness insets = Insets;
LayoutSize available = ContentControl.Deflate(context.AvailableSize, insets);
LayoutSize contentSize = Content is UIElement element
    ? element.Measure(new MeasureContext(available, context.Rounding))
    : MeasureTextContent(available);
return ContentControl.Inflate(contentSize, insets);
```

In `ArrangeCore`:

```csharp
if (TemplateChild is not null)
{
    return base.ArrangeCore(context);
}

if (Content is UIElement element)
{
    element.Arrange(new ArrangeContext(ContentControl.Deflate(context.FinalRect, Insets), context.Rounding));
}

return context.FinalRect;
```

This still calls content element layout directly, but ownership is inherited from `ContentControl`.

- [ ] **Step 3: Keep string rendering using shared text services**

Keep:

```csharp
if (Content is string text && !string.IsNullOrEmpty(text))
{
    ...
    TextRenderer.Render(...);
}
```

Do not introduce a generic text `ContentPresenter` in this plan. That is a separate design decision.

- [ ] **Step 4: Ensure template behavior remains inherited**

Because `ButtonBase` now inherits `ContentControl`, `ContentControl.OnPropertyChanged(...)` owns template handoff. Do not duplicate that logic in `Button`.

- [ ] **Step 5: Run targeted tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ButtonContentArchitectureTests|FullyQualifiedName~ButtonTests|FullyQualifiedName~ContentControlTests|FullyQualifiedName~ControlTemplateTests|FullyQualifiedName~ToggleButtonTests"
```

Expected: button content ownership tests pass, existing command/press/toggle behavior remains green.

- [ ] **Step 6: Commit Button consolidation**

```powershell
git add UI\Controls\Primitives\ButtonBase.cs UI\Controls\Button.cs tests\Cerneala.Tests\Controls\ButtonContentArchitectureTests.cs tests\Cerneala.Tests\Controls\ButtonTests.cs tests\Cerneala.Tests\Controls\ToggleButtonTests.cs
git commit -m "refactor: consolidate button content ownership"
```

---

### Task 4: Check Templates And Playground Samples

**Files:**
- Modify: `tests/Cerneala.Tests/Controls/ControlTemplateTests.cs`
- Modify: `Playground/Cerneala.Playground/Samples/*.cs` only if compile/test failures require it

- [ ] **Step 1: Add templated Button content test**

Add a test proving a `Button` template can use `ContentPresenter` with `context.Owner.Content` and does not leave content attached to the button and presenter at the same time.

Expected shape:

```csharp
Button button = new();
UIElement child = new();
ContentPresenter? presenter = null;
button.Content = child;
button.Template = new ControlTemplate<Button>(context =>
{
    presenter = new ContentPresenter { Content = context.Owner.Content };
    return presenter;
});

button.Measure(new MeasureContext(new LayoutSize(100, 100)));

Assert.Same(presenter, child.VisualParent);
Assert.DoesNotContain(child, button.VisualChildren);
```

- [ ] **Step 2: Run playground source tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Playground|FullyQualifiedName~Button"
```

Expected: samples compile because `Button.Content` still exists through `ContentControl`.

- [ ] **Step 3: Commit template/playground test updates**

```powershell
git add tests\Cerneala.Tests\Controls\ControlTemplateTests.cs Playground\Cerneala.Playground\Samples
git commit -m "test: cover templated button content handoff"
```

---

### Task 5: Full Verification And Status Update

**Files:**
- Modify: `ROADMAPv2.md`
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `AUDIT_FIX_PLAN.md` if tracked

- [ ] **Step 1: Run full tests**

```powershell
dotnet test Cerneala.slnx
```

Expected: full suite passes.

- [ ] **Step 2: Update project memory**

Mark button/content composition as consolidated. Do not claim full content presenter/text presenter architecture unless implemented separately.

- [ ] **Step 3: Commit docs**

```powershell
git add ROADMAPv2.md ROADMAPv2_AUDIT.md AUDIT_FIX_PLAN.md
git commit -m "docs: record button content consolidation"
```

## Stop Conditions

- [ ] Stop if this requires making every `ContentControl` string render text. That is outside this plan.
- [ ] Stop if a second `ContentProperty` remains in `Button` after consolidation.
- [ ] Stop if `ButtonBase` loses command behavior or input contracts while changing inheritance.
