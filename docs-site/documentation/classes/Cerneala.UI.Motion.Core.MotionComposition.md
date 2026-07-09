# MotionComposition Struct

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionComposition.cs`

Describes the channel and priority used to classify a motion operation when resolving composition conflicts.

```csharp
public readonly record struct MotionComposition(MotionChannel Channel, MotionPriority Priority)
```

Inheritance:
`ValueType` -> `MotionComposition`

## Examples

```csharp
using Cerneala.UI.Motion.Core;

MotionComposition current = MotionComposition.Default;
MotionComposition incoming = new(MotionChannel.Interaction, MotionPriority.Normal);

MotionConflictResolver resolver = new();
MotionComposition resolved = resolver.Resolve(current, incoming);

bool incomingWon = resolved == incoming;
```

## Remarks

`MotionComposition` is an immutable value that pairs a `MotionChannel` with a `MotionPriority`. The channel identifies the kind of motion, such as layout correction, presence, interaction, or explicit motion. The priority is used by `MotionConflictResolver` when choosing between two compositions.

`Default` represents the normal default composition: `MotionChannel.Default` with `MotionPriority.Normal`.

With the current `MotionPriority` enum, `Normal` is the only defined priority value. Because `MotionConflictResolver` lets the incoming composition win equal-priority conflicts, two normal-priority compositions resolve to the incoming value.

## Constructors

| Name | Description |
| --- | --- |
| `MotionComposition(MotionChannel Channel, MotionPriority Priority)` | Initializes a composition with the specified channel and priority. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Channel` | `MotionChannel` | Gets the motion channel associated with this composition. |
| `Priority` | `MotionPriority` | Gets the priority used to resolve composition conflicts. |
| `Default` | `MotionComposition` | Gets a composition with `MotionChannel.Default` and `MotionPriority.Normal`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out MotionChannel Channel, out MotionPriority Priority)` | `void` | Deconstructs the record struct into its channel and priority components. |
| `Equals(MotionComposition other)` | `bool` | Determines whether another composition has the same channel and priority. |
| `Equals(object? obj)` | `bool` | Determines whether an object is an equivalent `MotionComposition`. |
| `GetHashCode()` | `int` | Returns the hash code generated from the channel and priority. |
| `ToString()` | `string` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala motion core composition and conflict resolution.

## See also

- `Cerneala.UI.Motion.Core.MotionChannel`
- `Cerneala.UI.Motion.Core.MotionPriority`
- `Cerneala.UI.Motion.Core.MotionConflictResolver`
