# StylusInputAction Enum

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/StylusInputAction.cs`

Identifies what a stylus input point reports: contact, movement, range, or a barrel button change.

```csharp
public enum StylusInputAction
```

## Examples

Dispatch stylus points into a root and observe the routed events. The root is 200 by 200, so the 100 by 100 pad covers 50..150.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
UIElement pad = new() { Width = 100, Height = 100 };
root.VisualChildren.Add(pad);
root.ProcessFrame();

List<string> seen = [];
pad.AddHandler(InputEvents.StylusInRangeEvent, (_, _) => seen.Add("InRange"));
pad.AddHandler(InputEvents.StylusDownEvent, (_, args) => seen.Add("Down " + ((StylusEventArgs)args).Point.Pressure));

new StylusInputBridge().Dispatch(root, new StylusInputFrame(
    new StylusInputPoint(1, 100, 100, StylusInputAction.InRange),
    new StylusInputPoint(1, 100, 100, StylusInputAction.Down, Pressure: 0.8f)));

Console.WriteLine(string.Join(", ", seen)); // InRange, Down 0.8
```

## Remarks

`StylusInputBridge.Dispatch` hit-tests each point and raises one preview and bubble event pair on the hit element:

| Action | Preview event | Bubble event |
| --- | --- | --- |
| `Down` | `PreviewStylusDownEvent` | `StylusDownEvent` |
| `Move` | `PreviewStylusMoveEvent` | `StylusMoveEvent` |
| `Up` | `PreviewStylusUpEvent` | `StylusUpEvent` |
| `InRange` | `PreviewStylusInRangeEvent` | `StylusInRangeEvent` |
| `OutOfRange` | `PreviewStylusOutOfRangeEvent` | `StylusOutOfRangeEvent` |
| `ButtonDown` | `PreviewStylusButtonDownEvent` | `StylusButtonDownEvent` |
| `ButtonUp` | `PreviewStylusButtonUpEvent` | `StylusButtonUpEvent` |

A point that hits nothing raises no events. An undefined action value throws `InvalidOperationException`.

`InkCanvas.ApplyStylus` handles `Down`, `Move`, and `Up` as stroke input. It treats every other action as `Move`.

No built-in platform source produces stylus points. The SDL backend has no stylus mapping, so the host or application creates `StylusInputPoint` values and dispatches them.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Down` | `0` | The stylus touched the surface. |
| `Move` | `1` | The stylus moved. |
| `Up` | `2` | The stylus left the surface. |
| `InRange` | `3` | The stylus came within detection range. |
| `OutOfRange` | `4` | The stylus left detection range. |
| `ButtonDown` | `5` | A stylus button was pressed. |
| `ButtonUp` | `6` | A stylus button was released. |

## Applies to

`StylusInputPoint`, `StylusInputBridge`, `StylusEventArgs`, and `InkCanvas`.

## See also

- `Cerneala.UI.Input.StylusInputPoint`
- `Cerneala.UI.Input.StylusInputFrame`
- `Cerneala.UI.Input.StylusInputBridge`
- `Cerneala.UI.Input.StylusEventArgs`
