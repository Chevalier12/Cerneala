# Cerneala UI Input Routed Events Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an abstract `Cerneala.UI.Input` system with WPF-named routed input events, basic immediate-mode routing support, MonoGame input adaptation, and command routing foundations.

**Architecture:** `Cerneala.UI.Input` is abstract and does not depend on MonoGame. MonoGame-specific code lives under `Cerneala.UI.Input.MonoGame` and converts raw MonoGame states into abstract input frames. Routed event dispatch works over a temporary per-frame UI element tree so immediate-mode controls can still receive WPF-style tunnel, bubble, direct, focus, capture, text, and command events.

**Tech Stack:** C# `net8.0`, xUnit, MonoGame `Microsoft.Xna.Framework.Input`, existing Cerneala project layout under `UI/`.

## Global Constraints

- Namespace root is `Cerneala.UI.Input`.
- Keep WPF event names exactly: `PreviewMouseDown`, `MouseDown`, `PreviewKeyDown`, `KeyDown`, `PreviewCanExecute`, `CanExecute`, etc.
- `Cerneala.UI.Input` must not depend on MonoGame.
- MonoGame-specific adapter types go under `Cerneala.UI.Input.MonoGame`.
- Prefer snapshot + routed events: snapshots for continuous state, routed events for discrete input, text, focus, capture, and commands.
- Define all WPF-style input/command routed events up front, but only generate events that a source can produce honestly.
- Follow existing project style: small focused files, nullable enabled, xUnit tests.

---

## File Structure

Create production files:

- `UI/Input/RoutingStrategy.cs`: `Direct`, `Bubble`, `Tunnel`.
- `UI/Input/RoutedEvent.cs`: routed event metadata.
- `UI/Input/RoutedEventArgs.cs`: base args with `RoutedEvent`, `Source`, `OriginalSource`, `Handled`.
- `UI/Input/RoutedEventRegistry.cs`: validates and creates event definitions.
- `UI/Input/InputEvents.cs`: WPF-named input event definitions.
- `UI/Input/CommandEvents.cs`: WPF-named command event definitions.
- `UI/Input/InputButtonState.cs`: button transition state.
- `UI/Input/InputMouseButton.cs`: mouse button names.
- `UI/Input/InputKey.cs`: framework key names.
- `UI/Input/MouseButtonEventArgs.cs`: mouse button routed args.
- `UI/Input/MouseEventArgs.cs`: mouse move/enter/leave args.
- `UI/Input/MouseWheelEventArgs.cs`: wheel args.
- `UI/Input/KeyEventArgs.cs`: keyboard routed args.
- `UI/Input/TextCompositionEventArgs.cs`: text input args.
- `UI/Input/KeyboardFocusChangedEventArgs.cs`: keyboard focus args.
- `UI/Input/CanExecuteRoutedEventArgs.cs`: command can-execute args.
- `UI/Input/ExecutedRoutedEventArgs.cs`: command executed args.
- `UI/Input/ICommand.cs`: command contract.
- `UI/Input/RoutedCommand.cs`: named command.
- `UI/Input/CommandBinding.cs`: handlers for command events.
- `UI/Input/UiElementId.cs`: stable immediate-mode element id.
- `UI/Input/UiInputElement.cs`: per-frame tree node metadata.
- `UI/Input/UiInputTree.cs`: builds and queries parent/child route.
- `UI/Input/RoutedEventRouter.cs`: dispatches tunnel/bubble/direct routes.
- `UI/Input/InputFrame.cs`: current + previous snapshots and text events.
- `UI/Input/PointerSnapshot.cs`: mouse position, buttons, wheel.
- `UI/Input/KeyboardSnapshot.cs`: key states.
- `UI/Input/TextInputSnapshotEvent.cs`: raw text input event.
- `UI/Input/IInputSource.cs`: abstraction for input frame source.
- `UI/Input/MonoGame/MonoGameInputSource.cs`: adapter from MonoGame state.
- `UI/Input/MonoGame/MonoGameInputMapper.cs`: maps MonoGame keys/buttons to abstract keys/buttons.

Create test files:

- `tests/Cerneala.Tests/Input/RoutedEventTests.cs`
- `tests/Cerneala.Tests/Input/InputEventsTests.cs`
- `tests/Cerneala.Tests/Input/RoutedEventRouterTests.cs`
- `tests/Cerneala.Tests/Input/InputFrameTests.cs`
- `tests/Cerneala.Tests/Input/CommandingTests.cs`
- `tests/Cerneala.Tests/Input/MonoGameInputMapperTests.cs`

---

### Task 1: Routed Event Core

**Files:**
- Create: `UI/Input/RoutingStrategy.cs`
- Create: `UI/Input/RoutedEvent.cs`
- Create: `UI/Input/RoutedEventArgs.cs`
- Create: `UI/Input/RoutedEventRegistry.cs`
- Test: `tests/Cerneala.Tests/Input/RoutedEventTests.cs`

**Interfaces:**
- Produces:
  - `enum RoutingStrategy { Direct, Bubble, Tunnel }`
  - `sealed class RoutedEvent`
  - `class RoutedEventArgs`
  - `static class RoutedEventRegistry`

- [ ] **Step 1: Write failing tests**

```csharp
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class RoutedEventTests
{
    [Fact]
    public void RegisterCreatesRoutedEventMetadata()
    {
        RoutedEvent routedEvent = RoutedEventRegistry.Register(
            "MouseDown",
            typeof(RoutedEventTests),
            RoutingStrategy.Bubble,
            typeof(RoutedEventArgs));

        Assert.Equal("MouseDown", routedEvent.Name);
        Assert.Equal(typeof(RoutedEventTests), routedEvent.OwnerType);
        Assert.Equal(RoutingStrategy.Bubble, routedEvent.RoutingStrategy);
        Assert.Equal(typeof(RoutedEventArgs), routedEvent.ArgsType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterRejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(
            () => RoutedEventRegistry.Register(name, typeof(RoutedEventTests), RoutingStrategy.Bubble, typeof(RoutedEventArgs)));
    }

    [Fact]
    public void RoutedEventArgsDefaultsSourceToOriginalSource()
    {
        object source = new();
        RoutedEvent routedEvent = RoutedEventRegistry.Register(
            "MouseMove",
            typeof(RoutedEventTests),
            RoutingStrategy.Bubble,
            typeof(RoutedEventArgs));

        RoutedEventArgs args = new(routedEvent, source);

        Assert.Same(source, args.OriginalSource);
        Assert.Same(source, args.Source);
        Assert.False(args.Handled);
    }
}
```

- [ ] **Step 2: Run tests and confirm RED**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RoutedEventTests"`

Expected: compile errors because routed event types do not exist.

- [ ] **Step 3: Implement routed event core**

```csharp
namespace Cerneala.UI.Input;

public enum RoutingStrategy
{
    Direct,
    Bubble,
    Tunnel
}
```

```csharp
namespace Cerneala.UI.Input;

public sealed class RoutedEvent
{
    internal RoutedEvent(string name, Type ownerType, RoutingStrategy routingStrategy, Type argsType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Routed event name cannot be empty.", nameof(name));
        }

        Name = name;
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        RoutingStrategy = routingStrategy;
        ArgsType = argsType ?? throw new ArgumentNullException(nameof(argsType));
    }

    public string Name { get; }

    public Type OwnerType { get; }

    public RoutingStrategy RoutingStrategy { get; }

    public Type ArgsType { get; }
}
```

```csharp
namespace Cerneala.UI.Input;

public class RoutedEventArgs
{
    public RoutedEventArgs(RoutedEvent routedEvent, object originalSource)
    {
        RoutedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
        OriginalSource = originalSource ?? throw new ArgumentNullException(nameof(originalSource));
        Source = originalSource;
    }

    public RoutedEvent RoutedEvent { get; }

    public object OriginalSource { get; }

    public object Source { get; set; }

    public bool Handled { get; set; }
}
```

```csharp
namespace Cerneala.UI.Input;

public static class RoutedEventRegistry
{
    public static RoutedEvent Register(string name, Type ownerType, RoutingStrategy routingStrategy, Type argsType)
    {
        if (!typeof(RoutedEventArgs).IsAssignableFrom(argsType))
        {
            throw new ArgumentException("Routed event args type must derive from RoutedEventArgs.", nameof(argsType));
        }

        return new RoutedEvent(name, ownerType, routingStrategy, argsType);
    }
}
```

- [ ] **Step 4: Run tests and confirm GREEN**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RoutedEventTests"`

Expected: all `RoutedEventTests` pass.

- [ ] **Step 5: Commit**

```powershell
git add UI\Input tests\Cerneala.Tests\Input\RoutedEventTests.cs
git commit -m "Add routed event core"
```

---

### Task 2: WPF-Named Event Catalog and Args

**Files:**
- Create: `UI/Input/InputEvents.cs`
- Create: `UI/Input/CommandEvents.cs`
- Create: `UI/Input/InputMouseButton.cs`
- Create: `UI/Input/InputKey.cs`
- Create: `UI/Input/MouseButtonEventArgs.cs`
- Create: `UI/Input/MouseEventArgs.cs`
- Create: `UI/Input/MouseWheelEventArgs.cs`
- Create: `UI/Input/KeyEventArgs.cs`
- Create: `UI/Input/TextCompositionEventArgs.cs`
- Create: `UI/Input/KeyboardFocusChangedEventArgs.cs`
- Test: `tests/Cerneala.Tests/Input/InputEventsTests.cs`

**Interfaces:**
- Consumes: `RoutedEvent`, `RoutedEventArgs`, `RoutedEventRegistry`, `RoutingStrategy`.
- Produces: WPF-named static routed event definitions and typed args.

- [ ] **Step 1: Write failing tests for event names and strategies**

```csharp
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class InputEventsTests
{
    [Theory]
    [InlineData("PreviewMouseDown", RoutingStrategy.Tunnel)]
    [InlineData("MouseDown", RoutingStrategy.Bubble)]
    [InlineData("MouseEnter", RoutingStrategy.Direct)]
    [InlineData("PreviewKeyDown", RoutingStrategy.Tunnel)]
    [InlineData("KeyDown", RoutingStrategy.Bubble)]
    [InlineData("PreviewTextInput", RoutingStrategy.Tunnel)]
    [InlineData("TextInput", RoutingStrategy.Bubble)]
    [InlineData("PreviewGotKeyboardFocus", RoutingStrategy.Tunnel)]
    [InlineData("GotKeyboardFocus", RoutingStrategy.Bubble)]
    [InlineData("GotFocus", RoutingStrategy.Bubble)]
    public void InputEventsUseWpfNamesAndRoutingStrategies(string name, RoutingStrategy strategy)
    {
        RoutedEvent routedEvent = InputEvents.All.Single(e => e.Name == name);

        Assert.Equal(strategy, routedEvent.RoutingStrategy);
    }

    [Theory]
    [InlineData("PreviewCanExecute", RoutingStrategy.Tunnel)]
    [InlineData("CanExecute", RoutingStrategy.Bubble)]
    [InlineData("PreviewExecuted", RoutingStrategy.Tunnel)]
    [InlineData("Executed", RoutingStrategy.Bubble)]
    public void CommandEventsUseWpfNamesAndRoutingStrategies(string name, RoutingStrategy strategy)
    {
        RoutedEvent routedEvent = CommandEvents.All.Single(e => e.Name == name);

        Assert.Equal(strategy, routedEvent.RoutingStrategy);
    }

    [Fact]
    public void MouseButtonEventArgsExposeButtonAndPosition()
    {
        object source = new();
        MouseButtonEventArgs args = new(InputEvents.MouseDownEvent, source, InputMouseButton.Left, 10, 20, 1);

        Assert.Equal(InputMouseButton.Left, args.ChangedButton);
        Assert.Equal(10, args.X);
        Assert.Equal(20, args.Y);
        Assert.Equal(1, args.ClickCount);
    }
}
```

- [ ] **Step 2: Run tests and confirm RED**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InputEventsTests"`

Expected: compile errors because event catalog and args do not exist.

- [ ] **Step 3: Implement catalog and args**

Implement event definitions for these WPF names:

- Mouse: `PreviewMouseDown`, `MouseDown`, `PreviewMouseUp`, `MouseUp`, `PreviewMouseMove`, `MouseMove`, `PreviewMouseWheel`, `MouseWheel`, `MouseEnter`, `MouseLeave`, `GotMouseCapture`, `LostMouseCapture`, `QueryCursor`, left/right button down/up, double click.
- Keyboard/focus/text: `PreviewKeyDown`, `KeyDown`, `PreviewKeyUp`, `KeyUp`, `PreviewGotKeyboardFocus`, `GotKeyboardFocus`, `PreviewLostKeyboardFocus`, `LostKeyboardFocus`, `GotFocus`, `LostFocus`, `PreviewTextInput`, `TextInput`.
- Stylus/touch/manipulation/drag-drop: define all WPF names as routed events, but do not generate them yet.
- Commanding: `PreviewCanExecute`, `CanExecute`, `PreviewExecuted`, `Executed`.

Use static properties like:

```csharp
public static readonly RoutedEvent PreviewMouseDownEvent = RoutedEventRegistry.Register(
    "PreviewMouseDown",
    typeof(InputEvents),
    RoutingStrategy.Tunnel,
    typeof(MouseButtonEventArgs));
```

Expose:

```csharp
public static IReadOnlyList<RoutedEvent> All { get; } = [PreviewMouseDownEvent, MouseDownEvent, ...];
```

- [ ] **Step 4: Run tests and confirm GREEN**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InputEventsTests"`

Expected: all `InputEventsTests` pass.

- [ ] **Step 5: Commit**

```powershell
git add UI\Input tests\Cerneala.Tests\Input\InputEventsTests.cs
git commit -m "Add WPF-style input event catalog"
```

---

### Task 3: Immediate-Frame Input Tree and Router

**Files:**
- Create: `UI/Input/UiElementId.cs`
- Create: `UI/Input/UiInputElement.cs`
- Create: `UI/Input/UiInputTree.cs`
- Create: `UI/Input/RoutedEventRouter.cs`
- Test: `tests/Cerneala.Tests/Input/RoutedEventRouterTests.cs`

**Interfaces:**
- Consumes: `RoutedEvent`, `RoutedEventArgs`, `RoutingStrategy`.
- Produces:
  - `UiInputTree.Add(UiElementId id, UiElementId? parentId, bool isEnabled = true)`
  - `RoutedEventRouter.Raise(UiInputTree tree, UiElementId targetId, RoutedEventArgs args)`

- [ ] **Step 1: Write failing routing tests**

```csharp
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class RoutedEventRouterTests
{
    [Fact]
    public void BubbleEventsInvokeTargetThenAncestors()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId panel = new("panel");
        UiElementId button = new("button");
        List<string> calls = new();

        tree.Add(root, null);
        tree.Add(panel, root);
        tree.Add(button, panel);
        tree.AddHandler(root, InputEvents.MouseDownEvent, (_, _) => calls.Add("root"));
        tree.AddHandler(panel, InputEvents.MouseDownEvent, (_, _) => calls.Add("panel"));
        tree.AddHandler(button, InputEvents.MouseDownEvent, (_, _) => calls.Add("button"));

        MouseButtonEventArgs args = new(InputEvents.MouseDownEvent, button, InputMouseButton.Left, 0, 0, 1);
        RoutedEventRouter.Raise(tree, button, args);

        Assert.Equal(["button", "panel", "root"], calls);
    }

    [Fact]
    public void TunnelEventsInvokeRootThenTarget()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId panel = new("panel");
        UiElementId button = new("button");
        List<string> calls = new();

        tree.Add(root, null);
        tree.Add(panel, root);
        tree.Add(button, panel);
        tree.AddHandler(root, InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("root"));
        tree.AddHandler(panel, InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("panel"));
        tree.AddHandler(button, InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("button"));

        MouseButtonEventArgs args = new(InputEvents.PreviewMouseDownEvent, button, InputMouseButton.Left, 0, 0, 1);
        RoutedEventRouter.Raise(tree, button, args);

        Assert.Equal(["root", "panel", "button"], calls);
    }

    [Fact]
    public void HandledStopsRoute()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId button = new("button");
        List<string> calls = new();

        tree.Add(root, null);
        tree.Add(button, root);
        tree.AddHandler(button, InputEvents.MouseDownEvent, (_, args) =>
        {
            calls.Add("button");
            args.Handled = true;
        });
        tree.AddHandler(root, InputEvents.MouseDownEvent, (_, _) => calls.Add("root"));

        MouseButtonEventArgs args = new(InputEvents.MouseDownEvent, button, InputMouseButton.Left, 0, 0, 1);
        RoutedEventRouter.Raise(tree, button, args);

        Assert.Equal(["button"], calls);
    }
}
```

- [ ] **Step 2: Run tests and confirm RED**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RoutedEventRouterTests"`

Expected: compile errors because tree and router do not exist.

- [ ] **Step 3: Implement tree and router**

Implement:

```csharp
public readonly record struct UiElementId(string Value);
public sealed record UiInputElement(UiElementId Id, UiElementId? ParentId, bool IsEnabled);
public delegate void RoutedEventHandler(UiElementId sender, RoutedEventArgs args);
```

`UiInputTree` stores elements and handlers by `(UiElementId, RoutedEvent)`.

`RoutedEventRouter.Raise`:

- Direct: target only.
- Tunnel: root to target.
- Bubble: target to root.
- Stop when `args.Handled == true`.
- Set `args.Source` to current route element before invoking handlers.

- [ ] **Step 4: Run tests and confirm GREEN**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RoutedEventRouterTests"`

Expected: all router tests pass.

- [ ] **Step 5: Commit**

```powershell
git add UI\Input tests\Cerneala.Tests\Input\RoutedEventRouterTests.cs
git commit -m "Add routed input event router"
```

---

### Task 4: Input Snapshots and Transitions

**Files:**
- Create: `UI/Input/InputButtonState.cs`
- Create: `UI/Input/PointerSnapshot.cs`
- Create: `UI/Input/KeyboardSnapshot.cs`
- Create: `UI/Input/TextInputSnapshotEvent.cs`
- Create: `UI/Input/InputFrame.cs`
- Create: `UI/Input/IInputSource.cs`
- Test: `tests/Cerneala.Tests/Input/InputFrameTests.cs`

**Interfaces:**
- Produces abstract input frame model independent of MonoGame.

- [ ] **Step 1: Write failing tests**

```csharp
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class InputFrameTests
{
    [Fact]
    public void InputFrameReportsMouseButtonTransitions()
    {
        PointerSnapshot previous = PointerSnapshot.Empty;
        PointerSnapshot current = previous.WithButton(InputMouseButton.Left, isDown: true);

        InputFrame frame = new(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);

        Assert.True(frame.Pointer.IsPressed(InputMouseButton.Left));
        Assert.True(frame.Pointer.IsDown(InputMouseButton.Left));
        Assert.False(frame.Pointer.IsReleased(InputMouseButton.Left));
    }

    [Fact]
    public void InputFrameReportsKeyTransitions()
    {
        KeyboardSnapshot previous = KeyboardSnapshot.Empty;
        KeyboardSnapshot current = KeyboardSnapshot.FromDownKeys([InputKey.Enter]);

        InputFrame frame = new(PointerSnapshot.Empty, PointerSnapshot.Empty, previous, current, []);

        Assert.True(frame.Keyboard.IsPressed(InputKey.Enter));
        Assert.True(frame.Keyboard.IsDown(InputKey.Enter));
        Assert.False(frame.Keyboard.IsReleased(InputKey.Enter));
    }

    [Fact]
    public void InputFrameCarriesTextInputEvents()
    {
        TextInputSnapshotEvent text = new("ă");

        InputFrame frame = new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, [text]);

        Assert.Equal("ă", frame.TextInputEvents.Single().Text);
    }
}
```

- [ ] **Step 2: Run tests and confirm RED**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InputFrameTests"`

Expected: compile errors because snapshot types do not exist.

- [ ] **Step 3: Implement snapshots**

Implement immutable snapshots with methods:

- `PointerSnapshot.Empty`
- `PointerSnapshot.WithPosition(float x, float y)`
- `PointerSnapshot.WithButton(InputMouseButton button, bool isDown)`
- `KeyboardSnapshot.Empty`
- `KeyboardSnapshot.FromDownKeys(IEnumerable<InputKey> keys)`
- `InputFrame.Pointer.IsDown/IsPressed/IsReleased`
- `InputFrame.Keyboard.IsDown/IsPressed/IsReleased`

- [ ] **Step 4: Run tests and confirm GREEN**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InputFrameTests"`

Expected: all input frame tests pass.

- [ ] **Step 5: Commit**

```powershell
git add UI\Input tests\Cerneala.Tests\Input\InputFrameTests.cs
git commit -m "Add abstract input snapshots"
```

---

### Task 5: Commanding Core

**Files:**
- Create: `UI/Input/ICommand.cs`
- Create: `UI/Input/RoutedCommand.cs`
- Create: `UI/Input/CommandBinding.cs`
- Create: `UI/Input/CanExecuteRoutedEventArgs.cs`
- Create: `UI/Input/ExecutedRoutedEventArgs.cs`
- Test: `tests/Cerneala.Tests/Input/CommandingTests.cs`

**Interfaces:**
- Consumes: routed event core and `CommandEvents`.
- Produces command primitives for future controls.

- [ ] **Step 1: Write failing tests**

```csharp
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class CommandingTests
{
    [Fact]
    public void RoutedCommandStoresNameAndOwner()
    {
        RoutedCommand command = new("Save", typeof(CommandingTests));

        Assert.Equal("Save", command.Name);
        Assert.Equal(typeof(CommandingTests), command.OwnerType);
    }

    [Fact]
    public void CanExecuteArgsCanBeHandledByRoute()
    {
        RoutedCommand command = new("Save", typeof(CommandingTests));
        object source = new();
        CanExecuteRoutedEventArgs args = new(CommandEvents.CanExecuteEvent, source, command, "file");

        args.CanExecute = true;
        args.Handled = true;

        Assert.Same(command, args.Command);
        Assert.Equal("file", args.Parameter);
        Assert.True(args.CanExecute);
        Assert.True(args.Handled);
    }

    [Fact]
    public void CommandBindingInvokesHandlers()
    {
        RoutedCommand command = new("Save", typeof(CommandingTests));
        bool executed = false;
        CommandBinding binding = new(
            command,
            (_, args) => executed = true,
            (_, args) => args.CanExecute = true);

        ExecutedRoutedEventArgs executedArgs = new(CommandEvents.ExecutedEvent, new object(), command, null);
        binding.OnExecuted(new UiElementId("target"), executedArgs);

        Assert.True(executed);
    }
}
```

- [ ] **Step 2: Run tests and confirm RED**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CommandingTests"`

Expected: compile errors because command types do not exist.

- [ ] **Step 3: Implement command types**

Use WPF-like names but keep it small:

```csharp
public interface ICommand
{
    bool CanExecute(object? parameter);
    void Execute(object? parameter);
}
```

`RoutedCommand.CanExecute` returns `false` by default until command routing exists. `Execute` throws `InvalidOperationException` if called directly; routed execution will be handled by `CommandManager` in a future task.

- [ ] **Step 4: Run tests and confirm GREEN**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CommandingTests"`

Expected: all command tests pass.

- [ ] **Step 5: Commit**

```powershell
git add UI\Input tests\Cerneala.Tests\Input\CommandingTests.cs
git commit -m "Add routed commanding primitives"
```

---

### Task 6: MonoGame Input Mapping

**Files:**
- Create: `UI/Input/MonoGame/MonoGameInputMapper.cs`
- Create: `UI/Input/MonoGame/MonoGameInputSource.cs`
- Test: `tests/Cerneala.Tests/Input/MonoGameInputMapperTests.cs`

**Interfaces:**
- Consumes: `PointerSnapshot`, `KeyboardSnapshot`, `InputFrame`, `IInputSource`.
- Produces MonoGame adapter classes.

- [ ] **Step 1: Write failing mapper tests**

```csharp
using Cerneala.UI.Input;
using Cerneala.UI.Input.MonoGame;
using Microsoft.Xna.Framework.Input;

namespace Cerneala.Tests.Input;

public sealed class MonoGameInputMapperTests
{
    [Theory]
    [InlineData(Keys.Enter, InputKey.Enter)]
    [InlineData(Keys.Escape, InputKey.Escape)]
    [InlineData(Keys.A, InputKey.A)]
    [InlineData(Keys.LeftShift, InputKey.LeftShift)]
    public void MapsMonoGameKeys(Keys monoGameKey, InputKey expected)
    {
        Assert.Equal(expected, MonoGameInputMapper.MapKey(monoGameKey));
    }

    [Fact]
    public void UnmappedKeysReturnUnknown()
    {
        Assert.Equal(InputKey.Unknown, MonoGameInputMapper.MapKey((Keys)9999));
    }
}
```

- [ ] **Step 2: Run tests and confirm RED**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MonoGameInputMapperTests"`

Expected: compile errors because mapper does not exist.

- [ ] **Step 3: Implement mapper and source shell**

Implement:

- `MonoGameInputMapper.MapKey(Keys key)`
- `MonoGameInputMapper.MapMouseButton(...)`
- `MonoGameInputSource : IInputSource`

`MonoGameInputSource` should:

- Keep previous/current snapshots.
- Read `Mouse.GetState()` and `Keyboard.GetState()`.
- Expose `QueueTextInput(string text)` so `GameWindow.TextInput` can feed text without taking a hard dependency on window setup in tests.

- [ ] **Step 4: Run tests and confirm GREEN**

Run: `dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MonoGameInputMapperTests"`

Expected: mapper tests pass.

- [ ] **Step 5: Commit**

```powershell
git add UI\Input tests\Cerneala.Tests\Input\MonoGameInputMapperTests.cs
git commit -m "Add MonoGame input adapter"
```

---

### Task 7: Full Verification

**Files:**
- No new files.

**Interfaces:**
- Consumes all previous tasks.
- Produces verified implementation state.

- [ ] **Step 1: Run full test suite**

Run: `dotnet test Cerneala.slnx`

Expected: all tests pass.

- [ ] **Step 2: Run full build**

Run: `dotnet build Cerneala.slnx --no-restore`

Expected: build succeeds with `0 Warning(s)` and `0 Error(s)`.

- [ ] **Step 3: Run diff check**

Run: `git diff --check`

Expected: exit code `0`. LF/CRLF warnings are acceptable on this Windows repo if no whitespace errors are reported.

- [ ] **Step 4: Check status**

Run: `git status -sb`

Expected: branch is ahead only by the planned commits, with no unstaged changes.

---

## Self-Review

- Spec coverage: The plan covers abstract input, WPF exact event names, routed events, immediate-frame route tree, MonoGame adapter, text input snapshots, focus event args, and commanding primitives.
- Scope control: Stylus, touch, manipulation, and drag/drop event definitions are included in the catalog, but event generation is intentionally deferred until an input source can produce them honestly.
- Type consistency: Later tasks use `RoutedEvent`, `RoutedEventArgs`, `UiElementId`, `InputFrame`, `InputKey`, and command args introduced in earlier tasks.
- Placeholder scan: No TBD/TODO placeholders remain.
