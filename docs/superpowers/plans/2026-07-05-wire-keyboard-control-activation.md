# Wire Keyboard Control Activation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make focused buttons usable with keyboard: Enter executes; Space presses on key-down and executes on key-up. This closes basic focus/control behavior without adding a gesture framework.

**Architecture:** Keep control activation explicit and routed through existing `ElementInputBridge`, `FocusManager`, `CommandRouter`, `IInputPressable`, and `IInputCommandSource`. Handled routed key events must suppress default activation.

**Tech Stack:** C#/.NET 8, xUnit, existing retained input route map, routed events, focus manager, command router.

---

## File Structure

- Modify: `UI/Input/FocusManager.cs`
  - Return keyboard dispatch information needed to respect handled events.
- Create: `UI/Input/KeyboardDispatchResult.cs` if useful.
  - Represent key, pressed/released kind, target, and handled state.
- Create: `UI/Input/KeyboardActivationController.cs` if useful.
  - Small controller for Enter/Space activation.
- Modify: `UI/Input/ElementInputBridge.cs`
  - Dispatch keyboard, then run default activation for unhandled key results.
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
  - Keep as command source/pressable; avoid global router dependency.
- Create: `tests/Cerneala.Tests/Controls/ButtonKeyboardActivationTests.cs`

## Important Existing Behavior

`FocusManager.DispatchKeyboard(...)` raises key events to the focused element, but no default button activation exists. Mouse click command execution works through `ElementInputBridge` and `IInputCommandSource`; keyboard should reuse that explicit route.

Target behavior:

- Enter key-down on focused enabled button executes command once.
- Space key-down sets `IsPressed = true`.
- Space key-up clears `IsPressed` and executes command if the same focused button is valid.
- Disabled, hidden, collapsed, detached, or invalid targets do not execute.
- Handled `PreviewKeyDown`/`KeyDown` suppresses default activation.

## Rules

- [ ] Do not add a gesture system.
- [ ] Do not add WPF `CommandManager`, access keys, or requery magic.
- [ ] Do not make arbitrary focusable elements act like buttons.
- [ ] Do not execute commands when the routed key event was handled.
- [ ] Do not put command-router globals into controls.

---

### Task 1: Add RED Button Keyboard Activation Tests

**Files:**
- Create: `tests/Cerneala.Tests/Controls/ButtonKeyboardActivationTests.cs`

- [ ] **Step 1: Add tests**

Create tests:

```csharp
FocusedButtonEnterExecutesCommand()
FocusedButtonSpacePressSetsPressedState()
FocusedButtonSpaceReleaseClearsPressedStateAndExecutesCommand()
DisabledFocusedButtonDoesNotExecuteKeyboardCommand()
HiddenOrDetachedFocusedButtonIsClearedBeforeKeyboardActivation()
HandledPreviewKeyDownSuppressesButtonDefaultActivation()
HandledKeyDownSuppressesButtonDefaultActivation()
```

Test intent:

- Build `UIRoot` + `Button`.
- Process first frame, focus button through `ElementInputBridge.FocusManager` or pointer press.
- Dispatch keyboard frames using `KeyboardSnapshot.FromDownKeys(...)`.
- Use `ActionCommand` counter.
- Assert Space press state transitions.
- Add handlers that set `Handled = true` and assert command does not execute.

- [ ] **Step 2: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ButtonKeyboardActivationTests"
```

Expected: RED because default keyboard activation does not exist.

- [ ] **Step 3: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Controls\ButtonKeyboardActivationTests.cs
git commit -m "test: capture button keyboard activation"
```

---

### Task 2: Return Handled Keyboard Dispatch Results

**Files:**
- Modify: `UI/Input/FocusManager.cs`
- Create: `UI/Input/KeyboardDispatchResult.cs` if useful.

- [ ] **Step 1: Add tiny result type**

Suggested shape:

```csharp
public enum KeyboardDispatchKind
{
    Pressed,
    Released
}

public sealed record KeyboardDispatchResult(
    UIElement Target,
    UiElementId TargetId,
    InputKey Key,
    KeyboardDispatchKind Kind,
    bool Handled);
```

Keep internal if possible.

- [ ] **Step 2: Modify keyboard dispatch**

Add `DispatchKeyboardWithResults(...)` or change existing method while preserving compatibility:

- Pressed keys raise preview/key-down pair and capture handled state.
- Released keys raise preview/key-up pair and capture handled state.
- If focus is invalid, clear focus and return no activation results.

- [ ] **Step 3: Preserve existing tests**

If existing tests call `DispatchKeyboard(...)`, keep a wrapper that discards results.

---

### Task 3: Implement Minimal Keyboard Activation Controller

**Files:**
- Create: `UI/Input/KeyboardActivationController.cs`
- Modify: `UI/Input/ElementInputBridge.cs`

- [ ] **Step 1: Track keyboard press state**

Track the pressable element pressed by Space. Clear it on Space release, focus loss, invalid route, or disabled/hidden state.

- [ ] **Step 2: Wire controller in bridge**

After keyboard event dispatch and before text input dispatch:

```csharp
IReadOnlyList<KeyboardDispatchResult> results = focusManager.DispatchKeyboardWithResults(inputFrame, routeMap);
keyboardActivationController.Process(results, focusManager, commandRouter, routeMap);
```

Use final names that fit the codebase.

- [ ] **Step 3: Implement activation rules**

- Enter pressed: execute focused/ancestor `IInputCommandSource` once.
- Space pressed: set focused/ancestor `IInputPressable.IsPressed = true`; do not execute yet.
- Space released: clear pressed state and execute if still valid.
- Handled key result: do nothing.

Reuse an ancestor helper similar to existing `FindAncestor<TContract>(...)`.

---

### Task 4: Verify GREEN And Regressions

- [ ] **Step 1: Run button keyboard tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ButtonKeyboardActivationTests"
```

Expected: GREEN.

- [ ] **Step 2: Run related tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~FocusManagerTests|FullyQualifiedName~ElementInputBridgeTests|FullyQualifiedName~ButtonTests|FullyQualifiedName~ButtonContentArchitectureTests|FullyQualifiedName~CommandRouterTests|FullyQualifiedName~RoutedCommandExecutionTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Input UI\Controls\Primitives\ButtonBase.cs tests\Cerneala.Tests\Controls\ButtonKeyboardActivationTests.cs
git commit -m "feat: wire keyboard activation for buttons"
```
