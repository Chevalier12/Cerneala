# IMotionClock Interface

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/IMotionClock.cs`

Supplies the current time to a `MotionSystem`.

```csharp
public interface IMotionClock
```

## Examples

Drive motion time manually, for example in a test:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

ScriptedClock clock = new();
UIRoot root = new(viewportWidth: 800, viewportHeight: 600, motionClock: clock);

clock.Now = TimeSpan.FromMilliseconds(16);
MotionFrameResult result = root.Motion.Tick();
Console.WriteLine(result.Frame.Now); // 00:00:00.0160000

sealed class ScriptedClock : IMotionClock
{
    public TimeSpan Now { get; set; }
}
```

## Remarks

Pass a clock to the `UIRoot` constructor (`motionClock` parameter) or to the `MotionSystem` constructor. When `UIRoot` receives `null`, which is the default, it uses a new `SystemMotionClock`. The `MotionSystem` constructor throws `ArgumentNullException` for a `null` clock.

`MotionSystem.Tick` reads `Now` once per call. The frame delta is `Now` minus the previous active frame's time. A negative delta becomes zero, and a delta larger than `MotionSystem.MaxDelta` (100 ms by default) is reduced to `MaxDelta`. So a clock that goes backward does not run motion backward, and a long pause advances motion by at most `MaxDelta` in one frame.

`Now` is a relative time. It does not need to match wall-clock time; only the difference between two reads matters.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Now` | `TimeSpan` | Gets the current time of the clock. |

## Implementations

| Type | Description |
| --- | --- |
| `SystemMotionClock` | Returns the elapsed time of a `Stopwatch` started when the clock is constructed. |

## Applies to

Cerneala motion timing.

## See also

- `Cerneala.UI.Motion.Core.SystemMotionClock`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Elements.UIRoot`
