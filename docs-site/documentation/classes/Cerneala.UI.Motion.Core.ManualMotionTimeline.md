# ManualMotionTimeline Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/ManualMotionTimeline.cs`

Represents a motion timeline whose progress is set directly by caller code instead of by a clock.

```csharp
public sealed class ManualMotionTimeline : MotionTimeline
```

Inheritance:
`object` -> `MotionTimeline` -> `ManualMotionTimeline`

## Examples

Set manual progress and copy it into a motion value:

```csharp
using Cerneala.UI.Motion.Core;

ManualMotionTimeline timeline = new();
MotionValue<float> value = timeline.CreateValue(0f);

timeline.SetProgress(0.75f);
value.JumpTo(timeline.Progress);

float current = value.Current; // 0.75f
```

## Remarks

`ManualMotionTimeline` is useful when an interaction, scrubber, scroll position, or test needs to drive normalized progress explicitly. `SetProgress` clamps the supplied value to the inclusive range from `0` to `1`, and `Progress` returns the last clamped value.

The timeline owns an internal `MotionGraph` configured with built-in value mixers and `ReducedMotionPolicy.Default`. `CreateValue<T>` creates a `MotionValue<T>` from that graph. Setting timeline progress does not automatically write to created values; caller code decides how the timeline's normalized progress maps to motion values.

The timeline captures the constructing managed thread internally. `SetProgress`, `CreateValue<T>`, and graph-owned value mutations must be used from that same thread.

## Constructors

| Name | Description |
| --- | --- |
| `ManualMotionTimeline()` | Initializes a manual timeline with progress `0` and an internal motion graph using built-in mixers. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Progress` | `float` | Gets the current normalized progress value, clamped to the range `0` through `1`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `SetProgress(float progress)` | `void` | Sets `Progress` after clamping `progress` to the range `0` through `1`. |
| `CreateValue<T>(T initial)` | `MotionValue<T>` | Creates a motion value on the timeline's internal graph with the supplied initial value. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `SetProgress`, `CreateValue<T>` | `InvalidOperationException` | The current thread is not the thread that constructed the timeline. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Core/ManualMotionTimeline.cs`
- `UI/Motion/Core/MotionTimeline.cs`
- `UI/Motion/Core/MotionGraph.cs`
- `UI/Motion/Core/MotionValue{T}.cs`
