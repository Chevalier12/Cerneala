# SystemMotionClock Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/SystemMotionClock.cs`

Provides an `IMotionClock` implementation backed by a running `System.Diagnostics.Stopwatch`.

```csharp
public sealed class SystemMotionClock : IMotionClock
```

Inheritance:
`object` -> `SystemMotionClock`

Implements:
`IMotionClock`

## Examples

Use the system clock explicitly when constructing a root:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

SystemMotionClock clock = new();
UIRoot root = new(motionClock: clock);

TimeSpan motionTime = clock.Now;
```

`UIRoot` creates the same clock automatically when no custom `IMotionClock` is supplied:

```csharp
using Cerneala.UI.Elements;

UIRoot root = new();
MotionSystem motion = root.Motion;
```

## Remarks

`SystemMotionClock` starts an internal `Stopwatch` when the clock instance is constructed. The `Now` property returns the stopwatch elapsed time, so values are relative to that clock instance rather than wall-clock time.

The retained UI root uses `SystemMotionClock` as the default motion clock. Tests or deterministic animation scenarios can pass a custom `IMotionClock` to `UIRoot` instead, such as a manual clock that exposes controlled time advancement.

The class has no public reset or pause API. Create a new `SystemMotionClock` when a fresh elapsed-time origin is needed.

## Constructors

| Name | Description |
| --- | --- |
| `SystemMotionClock()` | Initializes a clock and starts its internal stopwatch immediately. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Now` | `TimeSpan` | Gets the elapsed time reported by the internal stopwatch. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.IMotionClock`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Elements.UIRoot`
