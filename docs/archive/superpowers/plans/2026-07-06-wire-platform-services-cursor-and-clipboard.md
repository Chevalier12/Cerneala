# Wire Platform Services Cursor And Clipboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make existing platform seams useful without turning Cerneala into a Windows/WPF clone. Cursor shape and clipboard are the minimal runtime services a developer notices immediately in TextBox/Button scenarios. They should be optional, root/host-owned, backend-neutral in core, and adapter-provided where possible.

**Architecture:** `UI/Platform` remains the platform abstraction layer. Controls may expose typed intent (`Cursor`, selection/copy/paste commands), but they must not call OS APIs. `UiHost`/`UIRoot` own service availability. MonoGame-specific cursor mapping stays in MonoGame adapter folders. Clipboard remains optional; if unavailable, text editing still works.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Platform`, `UI/Input/CursorService`, `UI/Input/KeyEventArgs`, `UI/Controls/TextBoxBase`, `UI/Controls/Primitives/ButtonBase`, `UI/Hosting`.

---

## File Structure

- Modify: `UI/Elements/UIRoot.cs`
  - Store optional `IPlatformServices`.
- Modify: `UI/Hosting/UiHostOptions.cs`
  - Accept optional `IPlatformServices`.
- Modify: `UI/Hosting/UiHost.cs`
  - Attach platform services to root and publish cursor after input dispatch.
- Modify: `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`
  - Accept optional platform services.
- Modify: `UI/Hosting/MonoGame/MonoGameUiHost.cs`
  - Pass services through to `UiHost`.
- Modify: `UI/Input/KeyEventArgs.cs`
  - Add explicit modifier state if needed for clipboard shortcuts.
- Modify: `UI/Input/FocusManager.cs`
  - Populate modifier state when raising key events.
- Modify: `UI/Input/Cursor.cs` and `UI/Input/CursorService.cs`
  - Resolve element cursor intent cleanly.
- Modify: `UI/Elements/UIElement.cs`
  - Add optional cursor intent property if needed.
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
  - Set default cursor intent to hand if not already styled.
- Modify: `UI/Controls/TextBoxBase.cs`
  - Set default cursor intent to IBeam and implement minimal Ctrl+A/C/X/V through `IClipboard`.
- Modify: `UI/Platform/IClipboard.cs`
  - Do not expand this duplicate seam; either leave it frozen or bridge tests to `UI/Platform/IClipboard`.
- Create: `tests/Cerneala.Tests/UI/Platform/UiHostPlatformServicesIntegrationTests.cs`
- Create: `tests/Cerneala.Tests/Input/CursorPlatformIntegrationTests.cs`
- Create: `tests/Cerneala.Tests/Controls/TextBoxClipboardShortcutTests.cs`

## Important Existing Behavior

- `UI/Platform/IPlatformServices` already exposes `IClipboard`, `ICursorService`, text input, DPI, dialogs, accessibility.
- `UI/Input/CursorService` can resolve a `Cursor` from hit-tested elements when configured manually.
- `UI/Platform/ICursorService` exposes `CursorShape` for platform cursor state.
- `TextBoxBase` supports focus, text input, caret, selection, delete/backspace, Home/End/Left/Right.
- `KeyEventArgs` currently carries only the key; modifier state may need to be surfaced explicitly.
- `UI/Platform/IClipboard` is the active clipboard contract; avoid growing duplicate clipboard abstractions.

Target behavior:

- `UiHostOptions.PlatformServices` and `UIRoot.PlatformServices` exist.
- Cursor is resolved from the retained hit-test/input cache after input dispatch and published to platform cursor service if available.
- Button/TextBox expose default cursor intent without platform dependencies.
- TextBox supports minimal Ctrl+A/C/X/V using `IClipboard` when available.
- Clipboard shortcuts are ignored safely when no clipboard service exists.
- Platform service changes do not force layout/render unless a control property actually changed.

## Rules

- [ ] Do not call OS clipboard APIs from controls/core.
- [ ] Do not implement native MonoGame clipboard if no reliable existing platform seam exists; use injectable `IClipboard`.
- [ ] Do not implement full IME.
- [ ] Do not implement native accessibility adapters.
- [ ] Do not add WPF `Application.Current` or global service locator.
- [ ] Do not add text formatting/rich text features.
- [ ] Do not reintroduce a second clipboard platform model outside `UI/Platform/IClipboard`.

---

### Task 1: Add RED Platform Service Integration Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Platform/UiHostPlatformServicesIntegrationTests.cs`
- Create: `tests/Cerneala.Tests/Input/CursorPlatformIntegrationTests.cs`

- [ ] **Step 1: Add host/root platform service tests**

Create tests:

```csharp
UiHostOptionsAttachPlatformServicesToRoot()
SetRootReattachesPlatformServicesToNewRoot()
ReplacingPlatformServicesDoesNotInvalidateLayoutOrRender()
PlatformServicesCanBeNullAndUpdateStillWorks()
```

- [ ] **Step 2: Add cursor publish tests**

Create tests:

```csharp
HoveringButtonPublishesHandCursorToPlatformService()
HoveringTextBoxPublishesIBeamCursorToPlatformService()
HoveringEmptyRootPublishesArrowCursor()
HiddenElementDoesNotPublishItsCursor()
CursorResolutionUsesRetainedInputCacheWithoutRebuildOnUnchangedFrame()
```

Use fake platform cursor service.

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostPlatformServicesIntegrationTests|FullyQualifiedName~CursorPlatformIntegrationTests"
```

Expected: RED because host/root platform service integration and cursor publishing are incomplete.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Platform\UiHostPlatformServicesIntegrationTests.cs tests\Cerneala.Tests\Input\CursorPlatformIntegrationTests.cs
git commit -m "test: capture platform cursor integration"
```

---

### Task 2: Add Root/Host Platform Service Ownership

**Files:**
- Modify: `UI/Elements/UIRoot.cs`
- Modify: `UI/Hosting/UiHostOptions.cs`
- Modify: `UI/Hosting/UiHost.cs`
- Modify: `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`
- Modify: `UI/Hosting/MonoGame/MonoGameUiHost.cs`

- [ ] **Step 1: Add root platform services property**

Add a simple API:

```csharp
public IPlatformServices PlatformServices { get; }
public void SetPlatformServices(IPlatformServices? services)
```

Use `PlatformServices.Empty` when null.

- [ ] **Step 2: Attach through `UiHostOptions`**

`UiHost` should set services on the current root in constructor and `SetRoot(...)`.

- [ ] **Step 3: Pass through MonoGame host options**

`MonoGameUiHostOptions` accepts optional platform services and passes them to `UiHostOptions`.

- [ ] **Step 4: Keep services as seams only**

No platform implementation is required in core. Tests use fakes.

---

### Task 3: Publish Cursor Intent

**Files:**
- Modify: `UI/Elements/UIElement.cs`
- Modify: `UI/Input/CursorService.cs`
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
- Modify: `UI/Controls/TextBoxBase.cs`
- Modify: `UI/Hosting/UiHost.cs`

- [ ] **Step 1: Add element cursor intent**

Add either a typed property or a minimal explicit property:

```csharp
public Cursor? Cursor { get; set; }
```

If using `UiProperty<Cursor?>`, do not add layout/render invalidation. Cursor affects platform output, not retained render.

- [ ] **Step 2: Update `CursorService.Resolve(...)`**

It should prefer element cursor intent while preserving existing explicit `SetCursor(...)` behavior if tests depend on it.

- [ ] **Step 3: Add default cursor intent**

- `ButtonBase`: `Cursor.Hand`
- `TextBoxBase`: `Cursor.IBeam`

Do this in constructors. Do not require default theme for cursor.

- [ ] **Step 4: Publish after input dispatch**

At the end of `UiHost.Update(...)`, resolve cursor at the latest logical pointer position and call platform cursor service if available.

Map `UI/Input/Cursor` to `UI/Platform/CursorShape` in one small helper. Keep mapping outside controls.

---

### Task 4: Add Minimal Clipboard Shortcuts For TextBox

**Files:**
- Create: `tests/Cerneala.Tests/Controls/TextBoxClipboardShortcutTests.cs`
- Modify: `UI/Input/KeyEventArgs.cs`
- Modify: `UI/Input/FocusManager.cs`
- Modify: `UI/Controls/TextBoxBase.cs`

- [ ] **Step 1: Add RED clipboard tests**

Create tests:

```csharp
CtrlASelectsAllText()
CtrlCCopiesSelectionToPlatformClipboard()
CtrlXCopiesSelectionAndDeletesIt()
CtrlVPastesClipboardTextAtCaret()
ClipboardShortcutsDoNothingWhenNoClipboardIsAvailable()
HandledPreviewKeyDownSuppressesClipboardShortcut()
```

- [ ] **Step 2: Add key modifier state**

Add small modifier support to key args, for example:

```csharp
public bool IsControlDown { get; }
public bool IsShiftDown { get; }
public bool IsAltDown { get; }
```

Populate from `InputFrame.Keyboard` when raising key events.

- [ ] **Step 3: Implement TextBox clipboard commands**

In `TextBoxBase.OnRoutedKeyDown(...)`:

- Ctrl+A selects all;
- Ctrl+C copies selected text;
- Ctrl+X copies then deletes selected text;
- Ctrl+V inserts clipboard text;
- no clipboard service means no-op/false handled except Ctrl+A if selection can still occur.

Use `Root?.PlatformServices.Clipboard` or `Root?.PlatformServices.TextInput?.Clipboard` fallback.

- [ ] **Step 4: Preserve retained invalidation**

Selection-only changes are render-only. Text changes go through existing `TextProperty` invalidation and two-way binding path.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted platform tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostPlatformServicesIntegrationTests|FullyQualifiedName~CursorPlatformIntegrationTests|FullyQualifiedName~TextBoxClipboardShortcutTests"
```

Expected: GREEN.

- [ ] **Step 2: Run existing input/text tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxEditingVisualContractTests|FullyQualifiedName~TextBoxTwoWayBindingTests|FullyQualifiedName~FocusManagerTests|FullyQualifiedName~ButtonKeyboardActivationTests|FullyQualifiedName~RetainedInputBindingTests"
```

Expected: GREEN.

- [ ] **Step 3: Run platform boundary tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~PlatformBoundaryTests|FullyQualifiedName~ServiceRegistrationTests|FullyQualifiedName~ArchitectureBoundaryTests"
```

Expected: GREEN.

- [ ] **Step 4: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 5: Commit implementation**

```powershell
git add UI\Platform UI\Hosting UI\Elements UI\Input UI\Controls\TextBoxBase.cs UI\Controls\Primitives\ButtonBase.cs tests\Cerneala.Tests
git commit -m "feat: wire platform cursor and clipboard services"
```
