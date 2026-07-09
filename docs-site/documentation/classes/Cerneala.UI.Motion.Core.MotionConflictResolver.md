# MotionConflictResolver Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionConflictResolver.cs`

Resolves competing motion compositions by selecting the composition with the higher priority, with the incoming composition winning ties.

```csharp
public sealed class MotionConflictResolver
```

Inheritance:
`object` -> `MotionConflictResolver`

## Examples

```csharp
using Cerneala.UI.Motion.Core;

MotionConflictResolver resolver = new();

MotionComposition current = new(MotionChannel.LayoutCorrection, MotionPriority.Normal);
MotionComposition incoming = new(MotionChannel.Interaction, MotionPriority.Normal);

MotionComposition resolved = resolver.Resolve(current, incoming);

// Equal priority resolves to the incoming composition.
bool incomingWon = resolved == incoming;
```

## Remarks

`MotionConflictResolver` centralizes the conflict rule for two `MotionComposition` values. The resolver compares only `MotionComposition.Priority`; it does not inspect the motion channel or any other state.

When `incoming.Priority` is greater than or equal to `current.Priority`, `Resolve` returns `incoming`. Otherwise, it returns `current`. With the current `MotionPriority` enum, `Normal` is the only defined priority value, so conflicts between normal compositions resolve to the incoming composition.

## Constructors

| Name | Description |
| --- | --- |
| `MotionConflictResolver()` | Initializes a resolver that uses the built-in priority comparison rule. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Resolve(MotionComposition current, MotionComposition incoming)` | `MotionComposition` | Returns `incoming` when its priority is greater than or equal to `current.Priority`; otherwise returns `current`. |

## Applies to

Cerneala motion core composition conflict resolution.

## See also

- `Cerneala.UI.Motion.Core.MotionComposition`
- `Cerneala.UI.Motion.Core.MotionPriority`
- `Cerneala.UI.Motion.Core.MotionChannel`
