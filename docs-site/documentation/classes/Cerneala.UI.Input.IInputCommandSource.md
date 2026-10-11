# IInputCommandSource Interface

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/IInputCommandSource.cs`

Marks an element that the input system activates with a left-button click, Enter, or Space.

```csharp
public interface IInputCommandSource
```

## Examples

A click on a child runs the command of the nearest command source on its route. The root is 200 by 200, so the 100 by 100 tile covers 50..150.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
Tile tile = new() { Width = 100, Height = 100 };
root.VisualChildren.Add(tile);
UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(200, 200) });

PointerSnapshot up = PointerSnapshot.Empty.WithPosition(100, 100);
PointerSnapshot down = up.WithButton(InputMouseButton.Left, true);
host.Update(Frame(PointerSnapshot.Empty, up));
host.Update(Frame(up, down));
host.Update(Frame(down, up));
Console.WriteLine(tile.Runs); // 1, on release

static InputFrame Frame(PointerSnapshot previous, PointerSnapshot current) =>
    new(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);

sealed class Tile : UIElement, IInputCommandSource
{
    public int Runs { get; private set; }

    public bool ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)
    {
        Runs++;
        return true;
    }
}
```

## Remarks

The input system looks for the nearest element on the route from the target to the root that implements this interface. It calls `ExecuteCommand` in these cases:

| Input | Condition |
| --- | --- |
| Left button released | The press and the release hit the same command source, so the click count is above 0. Neither the preview nor the bubble mouse-up events were handled. The element that received the mouse-up is the hit element or the command source. A `RepeatButton` is skipped here because it runs its command while pressed. |
| Enter pressed | The key event was not handled. |
| Space released | The key event was not handled, and the same command source received the Space press. |
| `RepeatButton` held | On each repeat tick. |

For Enter and Space, an element that also implements `IInputActivatable` gets `Activate` first, then `ExecuteCommand`. For a button, `Activate` raises `Click`. On a pointer release, `ButtonBase` raises `Click` from its own `MouseUp` handler, and then the input system calls `ExecuteCommand`. `ExecuteCommand` itself does not raise `Click`.

The input system ignores the returned value. `ButtonBase` and `MenuItem` return `true` only when they ran the command: the element must be enabled, and the command must report `CanExecute` as `true`. A `MenuItem` with a submenu returns `false`.

The interface also decides what counts as one click: the click tracker records the nearest command source on the route, not the child under the pointer. A press on one child of a button and a release on another child of the same button is still a click.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | Runs the element's command. Return `true` when it ran. |

## Applies to

`ButtonBase`, `MenuItem`, and custom activatable elements.

## See also

- `Cerneala.UI.Input.IInputActivatable`
- `Cerneala.UI.Input.IInputPressable`
- `Cerneala.UI.Input.ICommandStateSource`
- `Cerneala.UI.Input.ElementInputBridge`
