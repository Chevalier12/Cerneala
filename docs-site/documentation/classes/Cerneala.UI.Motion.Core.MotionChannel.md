# MotionChannel Enum

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionChannel.cs`

Names the channel part of a `MotionComposition`.

```csharp
public enum MotionChannel
```

## Examples

Resolve two compositions with the same priority. The channel does not affect the result:

```csharp
using Cerneala.UI.Motion.Core;

MotionConflictResolver resolver = new();
MotionComposition current = new(MotionChannel.Explicit, MotionPriority.Normal);
MotionComposition incoming = new(MotionChannel.Presence, MotionPriority.Normal);

MotionComposition winner = resolver.Resolve(current, incoming);
Console.WriteLine(winner); // MotionComposition { Channel = Presence, Priority = Normal }
```

## Remarks

`MotionChannel` is carried by `MotionComposition` together with a `MotionPriority`. `MotionComposition.Default` uses `MotionChannel.Default` and `MotionPriority.Normal`.

The channel does not change conflict resolution. `MotionConflictResolver.Resolve` compares only `Priority`: the incoming composition wins when its priority is greater than or equal to the current priority. Two compositions that differ only in channel therefore resolve to the incoming one.

`MotionValue<T>.AnimateTo` always builds its composition with `MotionChannel.Default`, and `MotionStartOptions` has no channel option. The values `LayoutCorrection`, `Presence`, `Interaction`, and `Explicit` are not used by the framework's motion paths; they are available to code that builds `MotionComposition` values directly.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Default` | `0` | The channel used by `MotionComposition.Default` and by `MotionValue<T>.AnimateTo`. |
| `LayoutCorrection` | `1` | Channel name for layout-correction motion. Not used by framework motion paths. |
| `Presence` | `2` | Channel name for enter and exit motion. Not used by framework motion paths. |
| `Interaction` | `3` | Channel name for interaction-driven motion. Not used by framework motion paths. |
| `Explicit` | `4` | Channel name for explicitly started motion. Not used by framework motion paths. |

## Applies to

Cerneala motion composition.

## See also

- `Cerneala.UI.Motion.Core.MotionComposition`
- `Cerneala.UI.Motion.Core.MotionConflictResolver`
- `Cerneala.UI.Motion.Core.MotionPriority`
