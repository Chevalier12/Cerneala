# StepPosition Enum

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/StepEasing.cs`

Specifies where the jumps of a `StepEasing` happen.

```csharp
public enum StepPosition
```

## Examples

Compare the four positions for a 4-step easing:

```csharp
using Cerneala.UI.Motion.Specs;

foreach (StepPosition position in Enum.GetValues<StepPosition>())
{
    StepEasing easing = new(4, position);
    Console.WriteLine($"{position}: {easing.Transform(0f)} {easing.Transform(0.3f)} {easing.Transform(0.5f)} {easing.Transform(0.99f)} {easing.Transform(1f)}");
}
// JumpStart: 0.25 0.5 0.75 1 1
// JumpEnd:   0 0.25 0.5 0.75 1
// JumpBoth:  0.2 0.4 0.6 0.8 1
// JumpNone:  0 0.33333334 0.6666667 1 1
```

## Remarks

Pass the position to the `StepEasing` constructor. The default is `JumpEnd`. In markup, the inline easing `Step(count, position)` takes the same names, for example `Step(6, JumpEnd)`.

`StepEasing.Transform` clamps the progress to `0` through `1` and returns a `NaN` progress as `0`. A progress of `1` always returns `1`. For `steps` = `n`:

| Position | Output levels | First jump |
| --- | --- | --- |
| `JumpStart` | `1/n`, `2/n`, ... `1` | At progress `0`: the output starts at `1/n`. |
| `JumpEnd` | `0`, `1/n`, ... `(n-1)/n`, then `1` at the end | At progress `1/n`. |
| `JumpBoth` | `1/(n+1)`, ... `n/(n+1)`, then `1` at the end | At progress `0`: the output starts at `1/(n+1)`. |
| `JumpNone` | `0`, `1/(n-1)`, ... `1` | At progress `1/n`. Both `0` and `1` are held for one step. |

`JumpNone` needs at least two steps. `new StepEasing(1, StepPosition.JumpNone)` throws `ArgumentOutOfRangeException`. Any position with `steps` less than `1` also throws `ArgumentOutOfRangeException`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `JumpStart` | `0` | Jump at the start of each step. |
| `JumpEnd` | `1` | Jump at the end of each step. The default. |
| `JumpBoth` | `2` | Jump at the start and at the end, giving `n + 1` jumps. |
| `JumpNone` | `3` | No jump at the start or the end, giving `n - 1` jumps. |

## Applies to

Cerneala motion easing.

## See also

- `Cerneala.UI.Motion.Specs.StepEasing`
- `Cerneala.UI.Motion.Specs.IEasing`
- `Cerneala.UI.Motion.Specs.TweenSpec_T_`
