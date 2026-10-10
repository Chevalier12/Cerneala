# Wire Minimal Retained Input Bindings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Connect existing `InputBinding`, `KeyBinding`, `InputGesture`, `KeyGesture`, and `CommandRouter` to retained `UIElement`s so focused elements and ancestors can execute keyboard shortcuts.

**Architecture:** Keep this minimal and explicit. Add `InputBindings` to `UIElement` and execute matching bindings along the focused route after key events, respecting `Handled`. Do not add a global `CommandManager`, automatic requery, string path binding, app-wide accelerators, or mouse gestures.

**Tech Stack:** C#/.NET 8, xUnit, existing input snapshots, routed events, command router, retained route map.

---

## File Structure

- Modify: `UI/Elements/UIElement.cs`
  - Add `InputBindings` collection.
- Create: `UI/Input/InputBindingCollection.cs`
  - Preferred over exposing raw mutable list.
- Modify: `UI/Input/InputBinding.cs`
  - Keep existing logic; add overload only if tests require it.
- Modify: `UI/Input/KeyBinding.cs`
  - No broad changes expected.
- Modify: `UI/Input/ElementInputBridge.cs`
  - Execute retained input bindings after keyboard event dispatch and before default keyboard activation.
- Create: `UI/Input/RetainedInputBindingProcessor.cs` if useful.
- Create: `tests/Cerneala.Tests/Input/RetainedInputBindingTests.cs`

## Important Existing Behavior

The primitives exist, but no retained element owns input bindings yet. That makes input bindings disconnected from the framework.

Target behavior:

- Focused element key binding executes direct command.
- Focused element key binding executes routed command through `CommandRouter`.
- Ancestor key binding can handle focused child gesture.
- Handled key events suppress binding execution.
- Executed input binding suppresses default button activation for the same key.

## Rules

- [ ] Do not add global command manager/requery.
- [ ] Do not add data binding or property paths.
- [ ] Do not add app-wide shortcut scope yet.
- [ ] Do not add mouse gestures in this plan.
- [ ] Do not redesign command routing.

---

### Task 1: Add RED Retained Input Binding Tests

**Files:**
- Create: `tests/Cerneala.Tests/Input/RetainedInputBindingTests.cs`

- [ ] **Step 1: Add tests**

Create tests:

```csharp
FocusedElementKeyBindingExecutesDirectCommand()
FocusedElementKeyBindingExecutesRoutedCommandThroughCommandRouter()
AncestorKeyBindingCanHandleFocusedChildGesture()
HandledPreviewKeyDownSuppressesInputBindingExecution()
HandledKeyDownSuppressesInputBindingExecution()
NonMatchingGestureDoesNotExecute()
InputBindingExecutionSuppressesButtonDefaultActivation()
```

Test intent:

- Build root/focusable child route.
- Add `KeyBinding` to focused element or ancestor.
- Focus child.
- Dispatch key press frame with optional modifiers.
- Assert direct/routed commands execute exactly once.
- Assert handled key events prevent execution.
- Assert binding on Space/Enter prevents default button activation double execution.

- [ ] **Step 2: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedInputBindingTests"
```

Expected: RED because `UIElement.InputBindings` does not exist and bridge does not process bindings.

- [ ] **Step 3: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Input\RetainedInputBindingTests.cs
git commit -m "test: capture retained input binding execution"
```

---

### Task 2: Add InputBindings To UIElement

**Files:**
- Modify: `UI/Elements/UIElement.cs`
- Create: `UI/Input/InputBindingCollection.cs`

- [ ] **Step 1: Create small collection**

Suggested shape:

```csharp
public sealed class InputBindingCollection : Collection<InputBinding>
{
    protected override void InsertItem(int index, InputBinding item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, InputBinding item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.SetItem(index, item);
    }
}
```

- [ ] **Step 2: Add to `UIElement`**

```csharp
public InputBindingCollection InputBindings { get; } = new();
```

Do not make this a `UiProperty<T>`.

---

### Task 3: Process Retained Input Bindings

**Files:**
- Modify: `UI/Input/ElementInputBridge.cs`
- Create: `UI/Input/RetainedInputBindingProcessor.cs` if useful.

- [ ] **Step 1: Walk focused route**

Use `ElementInputRouteMap`/`UiInputTree` if convenient. Otherwise walk `VisualParent` from `FocusManager.FocusedElement` to root. Keep deterministic order: focused element first, then ancestors.

- [ ] **Step 2: Match only unhandled key press results**

Use `KeyboardDispatchResult` from the previous plan. Do not execute bindings when preview/bubble key event was handled.

- [ ] **Step 3: Execute first matching binding**

For each owner in route:

```csharp
foreach (InputBinding binding in owner.InputBindings)
{
    if (binding.TryExecute(frame, commandRouter, routeMap, owner))
    {
        return true;
    }
}
```

Use binding owner as routed command target unless tests prove focused target is necessary.

- [ ] **Step 4: Suppress default keyboard activation**

If a binding executes for a key, filter that key result out before calling `KeyboardActivationController`.

---

### Task 4: Verify GREEN And Regressions

- [ ] **Step 1: Run input binding tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedInputBindingTests"
```

Expected: GREEN.

- [ ] **Step 2: Run related tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InputGestureTests|FullyQualifiedName~CommandRouterTests|FullyQualifiedName~RoutedCommandExecutionTests|FullyQualifiedName~ButtonKeyboardActivationTests|FullyQualifiedName~FocusManagerTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Elements\UIElement.cs UI\Input tests\Cerneala.Tests\Input\RetainedInputBindingTests.cs
git commit -m "feat: wire retained input bindings"
```
