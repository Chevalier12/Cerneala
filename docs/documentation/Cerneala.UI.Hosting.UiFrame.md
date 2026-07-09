# UiFrame Class

## Definition
Namespace: `Cerneala.UI.Hosting`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/UiFrame.cs`

Represents the input, viewport, elapsed time, and frame statistics for a single UI update.

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
float viewportWidth = frame.Viewport.Width;
bool frameDidWork = frame.Stats.HasWork;
```

## Remarks

`UiFrame` is the frame result type returned by `UiHost.Update` and exposed as `UiHost.LastFrame`. `MonoGameUiHost` also exposes the most recent `UiFrame` through its own `LastFrame` property.

The constructor stores the supplied values. `Input` and `Stats` are required and throw `ArgumentNullException` when `null`; `ElapsedTime` is stored without validation, and `Viewport` is a value type.

`Stats` references the `FrameStats` instance for the update. It records work counted during retained UI processing, input dispatch, motion, rendering cache updates, hit testing, and no-work frames.

## Constructors

| Name | Description |
| --- | --- |
| `UiFrame(TimeSpan, UiViewport, InputFrame, FrameStats)` | Initializes a frame result from elapsed time, viewport, input, and frame statistics. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ElapsedTime` | `TimeSpan` | Gets the elapsed time associated with the update that produced the frame. |
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
