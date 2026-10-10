# Wire Tab Focus Navigation Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make keyboard focus traversal usable. Cerneala already has focus policy, key routing, keyboard activation, and retained input bindings, but `KeyboardNavigation` is still only a thin wrapper. Developer Preview needs Tab/Shift+Tab to move focus predictably through retained visual tree order without WPF navigation complexity.

**Architecture:** Add a small retained keyboard navigation controller that runs after normal key routing and only acts on unhandled Tab key presses. Traverse the retained visual tree explicitly. Respect `Focusable`, `IsTabStop`, visibility, enabled state, detachment, and route-map membership. Do not add focus scopes, directional navigation, access keys, or WPF navigation modes in this plan.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Input`, `UI/Elements`, retained `ElementInputCache`, `ElementInputBridge`, `FocusManager`.

---

## File Structure

- Modify: `UI/Elements/UIElement.cs`
  - Add a typed `TabIndex` property only if needed for deterministic authoring order.
- Modify: `UI/Input/KeyboardNavigation.cs`
  - Implement retained visual-tree traversal and next/previous focus selection.
- Create: `UI/Input/KeyboardNavigationController.cs`
  - Processes unhandled Tab `KeyboardDispatchResult` entries after routed key events.
- Modify: `UI/Input/ElementInputBridge.cs`
  - Wire the controller after `FocusManager.DispatchKeyboardWithResults(...)` and before keyboard activation if needed.
- Modify only if needed: `UI/Input/KeyboardDispatchResult.cs`
- Create: `tests/Cerneala.Tests/Input/KeyboardNavigationContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/TabNavigationFrameContractTests.cs`

## Important Existing Behavior

- `FocusManager.Focus(...)` already applies `FocusPolicy.CanFocus(...)`.
- `FocusPolicy` rejects disabled, hidden, collapsed, detached, non-focusable, and route-map-missing elements.
- `Button` and `TextBox` are focusable/tab-stop by default.
- `KeyEventArgs` already exposes `IsShiftDown`, `IsControlDown`, and `IsAltDown`.
- `ElementInputBridge.Dispatch(...)` already receives `KeyboardDispatchResult` entries and then processes input bindings, keyboard activation, and text input.

Target behavior:

- Pressing Tab with no focus focuses the first valid tab stop in retained visual order.
- Pressing Tab on a focused element moves to the next valid tab stop.
- Pressing Shift+Tab moves to the previous valid tab stop.
- Traversal wraps by default.
- Handled PreviewKeyDown/KeyDown for Tab suppresses default navigation.
- Disabled, hidden, collapsed, detached, `Focusable=false`, and `IsTabStop=false` elements are skipped.
- `TabIndex` order is supported if added; equal `TabIndex` values preserve visual pre-order.
- Navigation invalidates focus visual/style/semantics only, not measure/layout.

## Rules

- [ ] Do not add WPF focus scopes.
- [ ] Do not add directional arrow-key navigation.
- [ ] Do not add access keys or mnemonic processing.
- [ ] Do not add platform keyboard APIs.
- [ ] Do not make navigation scan every frame; it only runs for unhandled Tab key presses.
- [ ] Do not bypass `FocusPolicy.CanFocus(...)`.

---

### Task 1: Add RED Keyboard Navigation Tests

**Files:**
- Create: `tests/Cerneala.Tests/Input/KeyboardNavigationContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/TabNavigationFrameContractTests.cs`

- [ ] **Step 1: Add focus traversal tests**

Create tests:

```csharp
TabWithNoFocusFocusesFirstTabStopInVisualOrder()
TabMovesToNextTabStopInVisualOrder()
ShiftTabMovesToPreviousTabStopInVisualOrder()
TabNavigationWrapsFromLastToFirst()
ShiftTabNavigationWrapsFromFirstToLast()
```

Use a small tree with `TextBox`, `Button`, and plain `UIElement` instances.

- [ ] **Step 2: Add skip-policy tests**

Create tests:

```csharp
TabNavigationSkipsDisabledHiddenCollapsedDetachedAndNonTabStopElements()
TabNavigationRespectsTabIndexThenVisualOrderWhenTabIndexExists()
TabNavigationDoesNothingWhenNoValidTargetsExist()
```

If `TabIndex` is not added, replace that test with a visual-order-only assertion and document the assumption in the test name.

- [ ] **Step 3: Add routing/handled tests**

Create tests:

```csharp
HandledPreviewKeyDownSuppressesDefaultTabNavigation()
HandledKeyDownSuppressesDefaultTabNavigation()
NonTabKeysDoNotInvokeKeyboardNavigation()
```

- [ ] **Step 4: Add retained frame tests**

Create tests:

```csharp
TabFocusChangeInvalidatesRenderStyleAndSemanticsWithoutMeasure()
SecondUnchangedFrameAfterTabNavigationDoesNoRetainedWork()
TabNavigationUsesPreInputCommittedHitTestAndRouteMap()
```

- [ ] **Step 5: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~KeyboardNavigationContractTests|FullyQualifiedName~TabNavigationFrameContractTests"
```

Expected: RED because Tab navigation is not wired yet.

- [ ] **Step 6: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Input\KeyboardNavigationContractTests.cs tests\Cerneala.Tests\UI\Hosting\TabNavigationFrameContractTests.cs
git commit -m "test: capture retained tab focus navigation contract"
```

---

### Task 2: Implement Retained Navigation Selection

**Files:**
- Modify: `UI/Input/KeyboardNavigation.cs`
- Modify: `UI/Elements/UIElement.cs` only if adding `TabIndex`.

- [ ] **Step 1: Add `TabIndex` only if tests require it**

If implemented, use a typed property:

```text
TabIndex: int, default 0, validate non-negative, no layout/render effect by itself.
```

Do not add WPF navigation modes.

- [ ] **Step 2: Implement candidate collection**

Collect candidates from `ElementTreeWalker.PreOrder(root, ElementChildRole.Visual)` and filter through:

```text
FocusPolicy.CanFocus(candidate, routeMap) && candidate.IsTabStop
```

- [ ] **Step 3: Implement deterministic order**

Order by:

1. `TabIndex` if present;
2. visual pre-order tie-breaker.

- [ ] **Step 4: Implement next/previous selection**

Add methods such as:

```csharp
UIElement? FindNext(UIRoot root, UIElement? current, ElementInputRouteMap routeMap, bool reverse)
bool MoveNext(UIRoot root, FocusManager focusManager, ElementInputRouteMap routeMap, bool reverse)
```

Return false when no focus change occurs.

---

### Task 3: Wire Navigation Into Input Bridge

**Files:**
- Create: `UI/Input/KeyboardNavigationController.cs`
- Modify: `UI/Input/ElementInputBridge.cs`

- [ ] **Step 1: Add controller**

The controller receives:

```text
keyboard dispatch results, current input frame, root, focus manager, route map
```

It should act only for unhandled Tab key-down events.

- [ ] **Step 2: Preserve existing pipeline order**

The intended order:

```text
routed key events -> retained input bindings -> tab navigation -> keyboard activation -> text input
```

If current keyboard activation requires a different order, keep tests authoritative and document the chosen order in code comments.

- [ ] **Step 3: Avoid duplicate navigation**

Only one Tab press should move focus once, even though modifier keys may also appear in the same frame.

---

### Task 4: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted navigation tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~KeyboardNavigationContractTests|FullyQualifiedName~TabNavigationFrameContractTests"
```

Expected: GREEN.

- [ ] **Step 2: Run adjacent input tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~FocusPolicyTests|FullyQualifiedName~FocusManagerTests|FullyQualifiedName~ButtonKeyboardActivationTests|FullyQualifiedName~RetainedInputBindingTests|FullyQualifiedName~TextInputBridgeTests"
```

Expected: GREEN.

- [ ] **Step 3: Run preview gates**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~RuntimePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 4: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 5: Commit implementation**

```powershell
git add UI\Input UI\Elements\UIElement.cs tests\Cerneala.Tests
git commit -m "feat: wire retained tab focus navigation"
```
