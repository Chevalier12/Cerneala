# IPointerDragSource Interface

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/IPointerDragSource.cs`

Marks an element that receives left-button drag callbacks directly from the input bridge, without routed events.

```csharp
public interface IPointerDragSource
```

## Examples

The pad logs each callback. The root is 200 by 200, so the 100 by 100 pad covers 50..150. The pad captures the pointer, so the move to (190, 190), outside the pad, still reaches it.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
DragPad pad = new() { Width = 100, Height = 100 };
root.VisualChildren.Add(pad);
UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(200, 200) });

PointerSnapshot start = PointerSnapshot.Empty.WithPosition(60, 60);
PointerSnapshot pressed = start.WithButton(InputMouseButton.Left, true);
PointerSnapshot moved = PointerSnapshot.Empty.WithPosition(190, 190).WithButton(InputMouseButton.Left, true);
PointerSnapshot released = PointerSnapshot.Empty.WithPosition(190, 190);
host.Update(Frame(PointerSnapshot.Empty, start));
host.Update(Frame(start, pressed));
host.Update(Frame(pressed, moved));
host.Update(Frame(moved, released));

Console.WriteLine(string.Join(" | ", pad.Log));
// Begin Left | Update <140, 140> | Complete Left

static InputFrame Frame(PointerSnapshot previous, PointerSnapshot current) =>
    new(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);

sealed class DragPad : Control, IPointerDragSource
{
    public List<string> Log { get; } = [];

    public bool BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
    {
        Log.Add("Begin " + args.ChangedButton);
        captureManager.Capture(this, routeMap);
        return true;
    }

    public bool UpdatePointerDrag(MouseEventArgs args)
    {
        Log.Add("Update " + args.GetPosition(this));
        return true;
    }

    public bool CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
    {
        Log.Add("Complete " + args.ChangedButton);
        captureManager.Release(routeMap);
        return true;
    }
}
```

## Remarks

`ElementInputBridge` calls the nearest `IPointerDragSource` on the route from the pointer target to the root. The pointer target is the element that holds pointer capture, or the hit element when nothing has capture.

| Callback | When it is called |
| --- | --- |
| `BeginPointerDrag` | The left button goes down, after the mouse-down events, unless `PreviewMouseDown` or `PreviewMouseLeftButtonDown` was handled. |
| `UpdatePointerDrag` | The pointer moves, after the mouse-move events. This happens for every move over the element, with or without a button down and with or without a drag in progress. |
| `CompletePointerDrag` | The left button goes up, after the mouse-up events. Handled mouse-up events do not prevent it. |

In one frame, a move is processed before button changes. So a frame that moves the pointer and presses the button calls `UpdatePointerDrag` before `BeginPointerDrag`.

Keep your own drag flag: set it in `BeginPointerDrag`, check it in `UpdatePointerDrag` and `CompletePointerDrag`, and ignore calls when no drag is in progress. Capture the pointer with `captureManager.Capture(this, routeMap)` to keep receiving moves outside the element, and release it in `CompletePointerDrag`.

The arguments are new objects created for the callback. They are not raised as routed events, so setting `Handled` on them affects nothing. The bridge ignores the returned values.

`MouseEventArgs.X` and `Y` are in root coordinates. Use `GetPosition(this)` for element coordinates.

Built-in implementations: `Thumb` (public methods), and `ColorSpectrum`, `TextBox`, and `PasswordBox` (explicit implementations).

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)` | `bool` | Starts a drag. `args.ChangedButton` is `Left` and `args.ClickCount` is the click count of the press: `1` for a first press, `2` for the second press of a double click, and `3` for the third. |
| `UpdatePointerDrag(MouseEventArgs args)` | `bool` | Reports a pointer move. |
| `CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)` | `bool` | Ends a drag. `args.ClickCount` is the click count of the release. |

## Applies to

`Thumb`, `ColorSpectrum`, `TextBox`, `PasswordBox`, and custom drag surfaces.

## See also

- `Cerneala.UI.Input.PointerCaptureManager`
- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Input.MouseEventArgs`
- `Cerneala.UI.Controls.Primitives.Thumb`
