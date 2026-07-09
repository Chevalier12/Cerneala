# MotionGraphSnapshot Class

## Definition
Namespace: `Cerneala.UI.Motion.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Diagnostics/MotionGraphSnapshot.cs`

Represents an immutable snapshot of current motion graph, property, layout motion, presence exit, and frame-continuation diagnostics.

```csharp
public readonly record struct MotionGraphSnapshot(
    int ActiveNodeCount,
    int ActivePropertyBindings,
    int ActiveLayoutMotions,
    int ActivePresenceExits,
    int ValuesSampledThisFrame,
    int PropertiesWrittenThisFrame,
    bool NeedsAnotherFrame)
```

Inheritance:
`ValueType` -> `MotionGraphSnapshot`

Implements:
`IEquatable<MotionGraphSnapshot>`

## Examples

Capture a snapshot from a root-owned motion system:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
var opacity = root.Motion.Graph.CreateValue(0f);

opacity.AnimateTo(1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(100)));

MotionGraphSnapshot snapshot = root.Motion.Diagnostics.CreateSnapshot(root.Motion);

int activeNodes = snapshot.ActiveNodeCount;
bool needsFrame = snapshot.NeedsAnotherFrame;
```

## Remarks

`MotionGraphSnapshot` is returned by `MotionDiagnostics.CreateSnapshot(MotionSystem)`. The capture copies counts from the supplied `MotionSystem`: active graph nodes, active property bindings, active layout motion bindings, active presence exits, and whether the motion system still has work that needs another frame.

`ActiveNodeCount` includes nodes already active in the graph plus nodes pending registration. `NeedsAnotherFrame` is copied from `MotionSystem.HasActiveMotion`, which is true when the graph has active motion or the property store has pending writes.

The current `CreateSnapshot` implementation sets `ValuesSampledThisFrame` and `PropertiesWrittenThisFrame` to `0`; per-frame sampling and write counts are reported through `MotionFrameResult` and retained frame diagnostics instead.

The primary constructor does not validate counter values. Direct construction is useful for tests and diagnostic formatting code that already has known counter values.

## Constructors

| Name | Description |
| --- | --- |
| `MotionGraphSnapshot(int, int, int, int, int, int, bool)` | Initializes a motion graph snapshot with explicit active counts, per-frame counters, and continuation state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ActiveNodeCount` | `int` | Gets the number of active or pending motion graph nodes at capture time. |
| `ActivePropertyBindings` | `int` | Gets the number of registered motion property bindings at capture time. |
| `ActiveLayoutMotions` | `int` | Gets the number of active layout motion bindings at capture time. |
| `ActivePresenceExits` | `int` | Gets the number of active presence exit animations at capture time. |
| `ValuesSampledThisFrame` | `int` | Gets the number of motion values sampled for the represented frame. |
| `PropertiesWrittenThisFrame` | `int` | Gets the number of motion property writes for the represented frame. |
| `NeedsAnotherFrame` | `bool` | Gets whether the captured motion system still needs another frame. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(...)` | `void` | Deconstructs the positional record into its public component values. |
| `Equals(MotionGraphSnapshot)` | `bool` | Determines whether another snapshot has the same component values. |
| `ToString()` | `string` | Returns the compiler-generated record string containing all component values. |

## Applies To

Cerneala retained UI motion diagnostics.

## See Also

- `Cerneala.UI.Motion.Diagnostics.MotionDiagnostics`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.MotionFrameResult`
