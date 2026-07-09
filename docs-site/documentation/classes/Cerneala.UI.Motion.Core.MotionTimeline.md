# MotionTimeline Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionTimeline.cs`

Defines the abstract base contract for objects that expose normalized motion progress.

```csharp
public abstract class MotionTimeline
```

Inheritance:
`object` -> `MotionTimeline`

Derived:
`ManualMotionTimeline`

## Examples

Read progress from a concrete timeline and apply it to a motion value:

```csharp
using Cerneala.UI.Motion.Core;

ManualMotionTimeline timeline = new();
MotionValue<float> value = timeline.CreateValue(0f);

timeline.SetProgress(0.75f);
value.JumpTo(timeline.Progress);

float current = value.Current; // 0.75f
```

## Remarks

`MotionTimeline` is a small base type for timeline implementations that report progress as a `float`. The base class does not define storage, clamping, clock integration, or value mapping; those behaviors belong to concrete timeline types.

The built-in `ManualMotionTimeline` implementation stores progress directly and clamps assigned values to the inclusive range from `0` to `1`. Consumers should use the concrete timeline's documentation to determine how `Progress` is produced and how it should be connected to `MotionValue<T>` instances or motion bindings.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Progress` | `float` | Gets the timeline's current progress value. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Core/MotionTimeline.cs`
- `UI/Motion/Core/ManualMotionTimeline.cs`
- `UI/Motion/Core/MotionValue{T}.cs`
