# UiFrame Class

## Definition
Namespace: `Cerneala.UI.Hosting`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/UiFrame.cs`

Represents the input, viewport, timing, and frame statistics for a single UI update.

```csharp
public sealed class UiFrame
```

Inheritance:
`Object` -> `UiFrame`

## Examples

```csharp
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;

InputFrame input = new(
    PointerSnapshot.Empty,
    PointerSnapshot.Empty,
    KeyboardSnapshot.Empty,
    KeyboardSnapshot.Empty,
    Array.Empty<TextInputSnapshotEvent>());

FrameStats stats = new();
UiFrame frame = new(
    TimeSpan.FromMilliseconds(16),
    new UiViewport(800, 600, scale: 1),
    input,
    stats);

TimeSpan elapsed = frame.ElapsedTime;
TimeSpan processing = frame.ProcessingTime;
float viewportWidth = frame.Viewport.Width;
bool frameDidWork = frame.Stats.HasWork;
```

## Remarks

`UiFrame` is the frame result type returned by `UiHost.Update` and exposed as `UiHost.LastFrame`. `MonoGameUiHost` also exposes the most recent `UiFrame` through its own `LastFrame` property.

The constructor stores the supplied values. `Input` and `Stats` are required and throw `ArgumentNullException` when `null`; `ElapsedTime` is stored without validation, and `Viewport` is a value type.

`ElapsedTime` is the simulation delta supplied to the update. It does not measure how long the update took. `ProcessingTime` measures input collection, retained UI update, and draw-command submission for a natively hosted window. It excludes the graphics presentation wait, including vertical synchronization. A directly constructed frame has a zero processing time until a host records it.

`Stats` references the `FrameStats` instance for the update. It records work counted during retained UI processing, input dispatch, motion, rendering cache updates, hit testing, and no-work frames.

## Constructors

| Name | Description |
| --- | --- |
| `UiFrame(TimeSpan, UiViewport, InputFrame, FrameStats)` | Initializes a frame result from elapsed time, viewport, input, and frame statistics. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ElapsedTime` | `TimeSpan` | Gets the elapsed time associated with the update that produced the frame. |
| `ProcessingTime` | `TimeSpan` | Gets the measured host processing duration, excluding graphics presentation wait. |
| `Viewport` | `UiViewport` | Gets the logical viewport and scale used by the update. |
| `Input` | `InputFrame` | Gets the input frame processed during the update. |
| `Stats` | `FrameStats` | Gets the frame statistics collected for the update. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `UiFrame(TimeSpan, UiViewport, InputFrame, FrameStats)` | `ArgumentNullException` | `input` or `stats` is `null`. |

## Applies to

Cerneala retained UI hosting.

## See also

- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Hosting.UiViewport`
- `Cerneala.UI.Input.InputFrame`
- `Cerneala.UI.Invalidation.FrameStats`
