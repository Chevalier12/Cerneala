# Fix Focus And Visibility Semantics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make focus and visibility explicit Cerneala-native contracts. `Visibility` becomes the primary public layout/render/input semantic, focus can only land on explicitly focusable elements, and pointer focus no longer steals keyboard focus for arbitrary routed visuals.

**Architecture:** Keep WPF-familiar names only where the behavior is clear. `Visibility` owns layout/render/input participation. `IsVisible` remains as a low-level runtime visibility gate for MVP, but all checks must flow through one helper so the split is documented and testable. Focus is owned by a small `FocusPolicy`, not by route-map accident. `FocusManager` raises events only after policy acceptance, and `ElementInputBridge` asks the policy before focusing a pointer target.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala retained UI runtime, existing `UI/Input` route map and retained hit-test cache.

---

## File Structure

- Modify: `UI/Elements/UIElement.cs`
  - Add `FocusableProperty` and `IsTabStopProperty`.
  - Add public `Focusable` and `IsTabStop` properties.
  - Replace direct visibility decisions in measure/arrange only where appropriate.
- Create: `UI/Elements/UIElementVisibility.cs`
  - Centralize visibility semantics for layout, rendering, hit-testing, and input-route inclusion.
- Create: `UI/Input/FocusPolicy.cs`
  - Centralize focus eligibility checks.
- Modify: `UI/Input/FocusManager.cs`
  - Reject invalid targets before changing `FocusedElement`.
  - Clear focus when passed `null`.
  - Keep existing routed focus event order.
- Modify: `UI/Input/KeyboardNavigation.cs`
  - Delegate focus eligibility to `FocusPolicy`.
  - Do not implement full tab navigation yet unless tests explicitly require it.
- Modify: `UI/Input/ElementInputBridge.cs`
  - On left mouse press, focus only if `FocusPolicy.CanFocus(...)` accepts the target.
- Modify: `UI/Input/ElementInputRouteBuilder.cs`
  - Use `UIElementVisibility.ParticipatesInInput(...)`.
- Modify: `UI/Input/HitTestService.cs`
  - Use `UIElementVisibility.ParticipatesInHitTest(...)`.
- Modify: `UI/Rendering/DrawCommandListBuilder.cs`
  - Use `UIElementVisibility.ParticipatesInRendering(...)`.
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
  - Set `Focusable = true` and `IsTabStop = true` in the constructor.
- Modify: `UI/Controls/TextBoxBase.cs`
  - Set `Focusable = true` and `IsTabStop = true` in the constructor.
- Create: `tests/Cerneala.Tests/Input/FocusPolicyTests.cs`
  - Prove focus rejects non-focusable, disabled, invisible, hidden, collapsed, and detached elements.
  - Prove `Button` and `TextBox` are focusable by default.
- Create: `tests/Cerneala.Tests/UI/Layout/VisibilityCombinationTests.cs`
  - Prove visible/hidden/collapsed and `IsVisible=false` combinations for layout, rendering, input route, and hit-test.
- Modify: `tests/Cerneala.Tests/Input/FocusManagerTests.cs`
  - Update direct `UIElement` focus tests to set `Focusable = true` where the test is about focus events rather than focus policy.
- Modify: `tests/Cerneala.Tests/UI/Layout/VisibilityTests.cs`
  - Keep existing `Visibility.Hidden` and `Visibility.Collapsed` behavior.

## Important Existing Behavior

Current focus behavior is too permissive:

```csharp
if (button == InputMouseButton.Left && pointerTarget is not null)
{
    focusManager.Focus(pointerTarget.Element, routeMap);
}
```

That means any enabled, visible routed element can become keyboard focus. A `Border`, `StackPanel`, `TextBlock`, or `Image` can accidentally receive keyboard focus just because it appears in the input route.

Current visibility behavior is duplicated:

```csharp
return element.IsVisible && element.Visibility == Visibility.Visible;
```

The same logic appears in input route building, hit-testing, and rendering. Layout independently checks only `Visibility.Collapsed`. The target is not to immediately remove `IsVisible`, because that is a breaking public API pass. The target is to make the split explicit, covered, and centralized.

Target behavior:

```csharp
FocusPolicy.CanFocus(element, routeMap)
    == element is attached
    && element.Focusable
    && element.IsEnabled
    && UIElementVisibility.ParticipatesInInput(element)
    && routeMap.TryGetId(element, out _)
```

`Visibility` semantics for MVP:

- `Visible`: participates in layout, rendering, input route, and hit-test when `IsVisible` is also true.
- `Hidden`: participates in layout, but not rendering, input route, or hit-test.
- `Collapsed`: does not consume layout size and does not render or receive input.
- `IsVisible=false`: runtime visibility gate; does not render or receive input, but does not collapse layout by itself.

---

### Task 1: Add RED Tests For Focus Policy

**Files:**
- Create: `tests/Cerneala.Tests/Input/FocusPolicyTests.cs`
- Modify: `tests/Cerneala.Tests/Input/FocusManagerTests.cs`

- [ ] **Step 1: Create focus policy tests**

Create `tests/Cerneala.Tests/Input/FocusPolicyTests.cs` with tests covering at least these cases:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class FocusPolicyTests
{
    [Fact]
    public void PlainUiElementIsNotFocusableByDefault()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        bool changed = manager.Focus(element, map);

        Assert.False(changed);
        Assert.Null(manager.FocusedElement);
        Assert.False(element.IsKeyboardFocused);
    }

    [Fact]
    public void ExplicitFocusableElementCanReceiveFocus()
    {
        UIRoot root = new();
        UIElement element = new() { Focusable = true };
        root.VisualChildren.Add(element);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        bool changed = manager.Focus(element, map);

        Assert.True(changed);
        Assert.Same(element, manager.FocusedElement);
        Assert.True(element.IsKeyboardFocused);
    }

    [Theory]
    [InlineData(false, true, Visibility.Visible)]
    [InlineData(true, false, Visibility.Visible)]
    [InlineData(true, true, Visibility.Hidden)]
    [InlineData(true, true, Visibility.Collapsed)]
    public void InvalidFocusTargetsAreRejected(bool isEnabled, bool isVisible, Visibility visibility)
    {
        UIRoot root = new();
        UIElement element = new()
        {
            Focusable = true,
            IsEnabled = isEnabled,
            IsVisible = isVisible,
            Visibility = visibility
        };
        root.VisualChildren.Add(element);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        bool changed = manager.Focus(element, map);

        Assert.False(changed);
        Assert.Null(manager.FocusedElement);
        Assert.False(element.IsKeyboardFocused);
    }

    [Fact]
    public void DetachedElementCannotReceiveFocus()
    {
        UIElement element = new() { Focusable = true };
        ElementInputRouteMap map = new ElementInputRouteMap();
        FocusManager manager = new();

        bool changed = manager.Focus(element, map);

        Assert.False(changed);
        Assert.Null(manager.FocusedElement);
    }

    [Fact]
    public void ButtonAndTextBoxAreFocusableByDefault()
    {
        Assert.True(new Button().Focusable);
        Assert.True(new Button().IsTabStop);
        Assert.True(new TextBox().Focusable);
        Assert.True(new TextBox().IsTabStop);
    }
}
```

- [ ] **Step 2: Update existing focus manager tests that intentionally focus plain `UIElement`**

In `tests/Cerneala.Tests/Input/FocusManagerTests.cs`, set `Focusable = true` on test elements where the test is validating event ordering or keyboard dispatch, not default focusability.

Example:

```csharp
UIElement first = new() { Focusable = true };
UIElement second = new() { Focusable = true };
```

- [ ] **Step 3: Run focus tests and verify RED**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~FocusManagerTests|FullyQualifiedName~FocusPolicyTests"
```

Expected: new focus policy tests fail because `Focusable` and `FocusPolicy` do not exist yet and `FocusManager` accepts any routed element.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Input\FocusPolicyTests.cs tests\Cerneala.Tests\Input\FocusManagerTests.cs
git commit -m "test: capture explicit focus policy"
```

---

### Task 2: Implement Explicit Focus Eligibility

**Files:**
- Modify: `UI/Elements/UIElement.cs`
- Create: `UI/Input/FocusPolicy.cs`
- Modify: `UI/Input/FocusManager.cs`
- Modify: `UI/Input/KeyboardNavigation.cs`
- Modify: `UI/Input/ElementInputBridge.cs`
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
- Modify: `UI/Controls/TextBoxBase.cs`

- [ ] **Step 1: Add focus properties to `UIElement`**

In `UI/Elements/UIElement.cs`, register properties near the other input state properties:

```csharp
public static readonly UiProperty<bool> FocusableProperty = UiProperty<bool>.Register(
    nameof(Focusable),
    typeof(UIElement),
    new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsStyle));

public static readonly UiProperty<bool> IsTabStopProperty = UiProperty<bool>.Register(
    nameof(IsTabStop),
    typeof(UIElement),
    new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsStyle));
```

Add public accessors:

```csharp
public bool Focusable
{
    get => GetValue(FocusableProperty);
    set => SetValue(FocusableProperty, value);
}

public bool IsTabStop
{
    get => GetValue(IsTabStopProperty);
    set => SetValue(IsTabStopProperty, value);
}
```

- [ ] **Step 2: Add `FocusPolicy`**

Create `UI/Input/FocusPolicy.cs`:

```csharp
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public static class FocusPolicy
{
    public static bool CanFocus(UIElement? element, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(routeMap);
        if (element is null)
        {
            return false;
        }

        return element.IsAttached &&
            element.Focusable &&
            element.IsEnabled &&
            UIElementVisibility.ParticipatesInInput(element) &&
            routeMap.TryGetId(element, out _);
    }
}
```

This file intentionally depends on `UIElementVisibility`, created in Task 3. During Task 2, either create a temporary local equivalent or complete Task 3 in the same commit.

- [ ] **Step 3: Gate `FocusManager.Focus(...)`**

In `FocusManager.Focus(UIElement? element, ElementInputRouteMap routeMap)`, before changing state:

```csharp
if (element is not null && !FocusPolicy.CanFocus(element, routeMap))
{
    return false;
}
```

Keep `Focus(null, routeMap)` valid so callers can clear focus.

- [ ] **Step 4: Gate pointer focus in `ElementInputBridge`**

Replace the direct pointer focus call with:

```csharp
if (button == InputMouseButton.Left &&
    pointerTarget is not null &&
    FocusPolicy.CanFocus(pointerTarget.Element, routeMap))
{
    focusManager.Focus(pointerTarget.Element, routeMap);
}
```

- [ ] **Step 5: Make focusable controls opt in**

In `ButtonBase`, add a constructor:

```csharp
public ButtonBase()
{
    Focusable = true;
    IsTabStop = true;
}
```

In `TextBoxBase` constructor, add:

```csharp
Focusable = true;
IsTabStop = true;
```

Do not make `Control` focusable by default. Panels, `Border`, `TextBlock`, and `Image` should stay non-focusable unless a caller explicitly opts in.

- [ ] **Step 6: Run focus tests and verify GREEN**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~FocusManagerTests|FullyQualifiedName~FocusPolicyTests"
```

Expected: focus policy and existing focus event tests pass.

- [ ] **Step 7: Commit focus policy implementation**

```powershell
git add UI\Elements\UIElement.cs UI\Input\FocusPolicy.cs UI\Input\FocusManager.cs UI\Input\KeyboardNavigation.cs UI\Input\ElementInputBridge.cs UI\Controls\Primitives\ButtonBase.cs UI\Controls\TextBoxBase.cs
git commit -m "feat: add explicit focus policy"
```

---

### Task 3: Centralize Visibility Semantics

**Files:**
- Create: `UI/Elements/UIElementVisibility.cs`
- Modify: `UI/Elements/UIElement.cs`
- Modify: `UI/Input/ElementInputRouteBuilder.cs`
- Modify: `UI/Input/HitTestService.cs`
- Modify: `UI/Rendering/DrawCommandListBuilder.cs`
- Create: `tests/Cerneala.Tests/UI/Layout/VisibilityCombinationTests.cs`

- [ ] **Step 1: Create visibility combination tests**

Create `tests/Cerneala.Tests/UI/Layout/VisibilityCombinationTests.cs` with tests proving:

- `Visibility.Visible` + `IsVisible=true` participates in layout, render, input, and hit-test.
- `Visibility.Hidden` reserves layout space but is excluded from render/input/hit-test.
- `Visibility.Collapsed` returns zero desired/arranged size and is excluded from render/input/hit-test.
- `IsVisible=false` does not collapse layout, but is excluded from render/input/hit-test.

Use small fixed-size/render-counting elements instead of relying on controls.

- [ ] **Step 2: Run visibility tests and verify RED**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~VisibilityCombinationTests|FullyQualifiedName~VisibilityTests"
```

Expected: tests fail until `UIElementVisibility` exists and call sites are updated.

- [ ] **Step 3: Add `UIElementVisibility` helper**

Create `UI/Elements/UIElementVisibility.cs`:

```csharp
using Cerneala.UI.Layout;

namespace Cerneala.UI.Elements;

public static class UIElementVisibility
{
    public static bool ParticipatesInLayout(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.Visibility != Visibility.Collapsed;
    }

    public static bool ParticipatesInRendering(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.IsVisible && element.Visibility == Visibility.Visible;
    }

    public static bool ParticipatesInInput(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.IsVisible && element.Visibility == Visibility.Visible;
    }

    public static bool ParticipatesInHitTest(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.IsVisible && element.Visibility == Visibility.Visible;
    }
}
```

- [ ] **Step 4: Replace duplicated visibility checks**

Replace call sites:

```csharp
element.Visibility != Visibility.Visible || !element.IsVisible
```

with:

```csharp
!UIElementVisibility.ParticipatesInRendering(element)
```

Replace input route and hit-test helper bodies with `UIElementVisibility.ParticipatesInInput(...)` / `ParticipatesInHitTest(...)`.

In `UIElement.Measure(...)` and `UIElement.Arrange(...)`, replace direct collapsed checks with `UIElementVisibility.ParticipatesInLayout(this)` where it improves readability. Do not change `Hidden` layout behavior.

- [ ] **Step 5: Run visibility tests and verify GREEN**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~VisibilityCombinationTests|FullyQualifiedName~VisibilityTests|FullyQualifiedName~DrawCommandListBuilderTests|FullyQualifiedName~HitTest"
```

Expected: visibility contract is explicit and existing hidden/collapsed tests still pass.

- [ ] **Step 6: Commit visibility semantic helper**

```powershell
git add UI\Elements\UIElementVisibility.cs UI\Elements\UIElement.cs UI\Input\ElementInputRouteBuilder.cs UI\Input\HitTestService.cs UI\Rendering\DrawCommandListBuilder.cs tests\Cerneala.Tests\UI\Layout\VisibilityCombinationTests.cs
git commit -m "refactor: centralize visibility semantics"
```

---

### Task 4: Prove Pointer Focus Does Not Steal Focus For Non-Focusable Visuals

**Files:**
- Modify: `tests/Cerneala.Tests/Input/FocusPolicyTests.cs`
- Modify: `tests/Cerneala.Tests/Input/ElementInputBridgeTests.cs`

- [ ] **Step 1: Add integration tests**

Add tests proving:

- Pressing on a non-focusable `Border` or plain `UIElement` does not change `FocusManager.FocusedElement`.
- Pressing on a `Button` focuses the button.
- Pressing on a `Button` with `Focusable=false` executes/routs mouse events but does not focus it.

Use `ElementInputBridge.Dispatch(root, inputFrame)` and the retained `root.InputCache` path, not a manually built route map.

- [ ] **Step 2: Run input bridge tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ElementInputBridgeTests|FullyQualifiedName~FocusPolicyTests"
```

Expected: tests pass after Task 2 and Task 3.

- [ ] **Step 3: Commit integration tests**

```powershell
git add tests\Cerneala.Tests\Input\FocusPolicyTests.cs tests\Cerneala.Tests\Input\ElementInputBridgeTests.cs
git commit -m "test: prove pointer focus respects focus policy"
```

---

### Task 5: Full Verification And Documentation Touch-Up

**Files:**
- Modify: `ROADMAPv2.md`
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `AUDIT_FIX_PLAN.md` if this plan is tracked there

- [ ] **Step 1: Update roadmap/audit status only after code and tests are green**

Mark the focus policy and visibility-combination items as implemented. Do not mark full focus scopes/tab traversal complete unless implemented and tested.

- [ ] **Step 2: Run full tests**

```powershell
dotnet test Cerneala.slnx
```

Expected: full suite passes.

- [ ] **Step 3: Commit final status update**

```powershell
git add ROADMAPv2.md ROADMAPv2_AUDIT.md AUDIT_FIX_PLAN.md
git commit -m "docs: mark focus visibility semantics complete"
```

## Stop Conditions

- [ ] Stop if implementing this requires a full focus-scope/tab-navigation system. That is not part of this plan.
- [ ] Stop if `IsVisible` removal becomes necessary. Centralize semantics first; removal/deprecation can be a later breaking API pass.
- [ ] Stop if `UI/Input` needs to reference concrete controls. Focus eligibility must stay generic.
