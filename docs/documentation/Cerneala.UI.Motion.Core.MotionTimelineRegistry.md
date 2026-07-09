# MotionTimelineRegistry Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionTimelineRegistry.cs`

Represents the motion system's timeline registry surface.

```csharp
public sealed class MotionTimelineRegistry
```

Inheritance:
`object` -> `MotionTimelineRegistry`

## Examples

Access the registry owned by a `MotionSystem`:

```csharp
using Cerneala.UI.Motion.Core;

MotionTimelineRegistry timelines = motionSystem.Timelines;
```

## Remarks

`MotionTimelineRegistry` is constructed by `MotionSystem` and exposed through the `MotionSystem.Timelines` property. The current type defines the registry surface for timeline-related APIs and does not expose public registration, lookup, or enumeration members.

Use concrete timeline types such as `ManualMotionTimeline` when caller code needs a timeline with progress that can be driven directly.

## Constructors

| Name | Description |
| --- | --- |
| `MotionTimelineRegistry()` | Initializes a new timeline registry instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| None | N/A | This class declares no public properties. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| None | N/A | This class declares no public methods. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Core/MotionTimelineRegistry.cs`
- `UI/Motion/Core/MotionSystem.cs`
- `UI/Motion/Core/MotionTimeline.cs`
- `UI/Motion/Core/ManualMotionTimeline.cs`
