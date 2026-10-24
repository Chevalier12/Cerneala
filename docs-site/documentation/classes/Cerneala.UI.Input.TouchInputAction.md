# TouchInputAction Enum

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/TouchInputBridge.cs`

Identifies whether a touch point went down, moved, or went up.

```csharp
public enum TouchInputAction
```

## Examples

Dispatch a touch down and up into a root. The root is 200 by 200, so the 100 by 100 pad covers 50..150.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
UIElement pad = new() { Width = 100, Height = 100 };
root.VisualChildren.Add(pad);
root.ProcessFrame();

List<string> seen = [];
pad.TouchDown += (_, args) => seen.Add("Down " + args.Action);
pad.TouchUp += (_, args) => seen.Add("Up " + args.TouchId);

new TouchInputBridge().Dispatch(root, new TouchInputFrame(
    new TouchInputPoint(7, 100, 100, TouchInputAction.Down),
    new TouchInputPoint(7, 100, 100, TouchInputAction.Up)));

Console.WriteLine(string.Join(", ", seen)); // Down Down, Up 7
```

## Remarks

`TouchInputBridge.Dispatch` raises one preview and bubble event pair for each point, on the hit element or on the element that captured that touch id:

| Action | Preview event | Bubble event |
| --- | --- | --- |
| `Down` | `PreviewTouchDownEvent` | `TouchDownEvent` |
| `Move` | `PreviewTouchMoveEvent` | `TouchMoveEvent` |
| `Up` | `PreviewTouchUpEvent` | `TouchUpEvent` |

An undefined action value throws `InvalidOperationException`.

The touch capture events `GotTouchCaptureEvent` and `LostTouchCaptureEvent` also carry `TouchEventArgs`, with `Action` set to `Move` and `X` and `Y` set to `0`.

`InkCanvas.ApplyTouch` handles `Down`, `Move`, and `Up` as stroke input.

No built-in platform source produces touch points. The SDL backend has no touch mapping, so the host or application creates `TouchInputPoint` values and dispatches them.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Down` | `0` | The touch point made contact. |
| `Move` | `1` | The touch point moved. Also used by touch capture events. |
| `Up` | `2` | The touch point lifted. |

## Applies to

`TouchInputPoint`, `TouchInputBridge`, `TouchEventArgs`, and `InkCanvas`.

## See also

- `Cerneala.UI.Input.TouchInputPoint`
- `Cerneala.UI.Input.TouchInputFrame`
- `Cerneala.UI.Input.TouchInputBridge`
- `Cerneala.UI.Input.TouchEventArgs`
