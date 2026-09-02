# MotionPriority Enum

## Definition

Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionPriority.cs`

Defines the precedence used when a motion attempts to replace an active motion on the same value.

```csharp
public enum MotionPriority
```

## Examples

Start an interactive motion without allowing it to replace a normal-priority motion that is already active:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new();
MotionValue<float> value = graph.CreateValue(0f);
MotionHandle handle = value.AnimateTo(
    1f,
    Motion.Tween<float>(TimeSpan.FromMilliseconds(100)),
    new MotionStartOptions(Priority: MotionPriority.Interactive));
```

## Remarks

Higher numeric values have stronger precedence. An incoming motion replaces an active motion when its priority is greater than or equal to the active priority. A lower-priority request is rejected without changing the active value or animation and returns a canceled `MotionHandle`.

State-driven `MotionStateBuilder` animations and state-driven Aspect motion use `Interactive`. Ordinary imperative animations use `Normal` by default. `ReducedMotion` is the strongest built-in priority.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Interactive` | `0` | State- and interaction-driven motion that yields to normal explicit motion. |
| `Normal` | `100` | Default priority for imperative and transaction-created motion. |
| `ReducedMotion` | `200` | Reserved high priority for reduced-motion handling. |

## Applies to

Cerneala motion conflict resolution.

## See also

- `Cerneala.UI.Motion.Core.MotionComposition`
- `Cerneala.UI.Motion.Core.MotionConflictResolver`
- `Cerneala.UI.Motion.Core.MotionStartOptions`
- `Cerneala.UI.Motion.Properties.MotionPropertyStartOptions`
