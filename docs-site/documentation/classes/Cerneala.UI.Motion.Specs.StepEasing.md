# StepEasing Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/StepEasing.cs`

Applies discrete step easing to normalized motion progress.

```csharp
public sealed class StepEasing : IEasing
```

Inheritance:
`object` -> `StepEasing`

Implements:
`IEasing`

## Examples

Transform progress through the default `JumpEnd` step mode:

```csharp
using Cerneala.UI.Motion.Specs;

StepEasing easing = new(steps: 4);

float beforeFirstStep = easing.Transform(0.24f); // 0
float firstStep = easing.Transform(0.25f);       // 0.25
float end = easing.Transform(1);                 // 1
```

Use step easing with a tween specification:

```csharp
using Cerneala.UI.Motion.Specs;

IEasing easing = new StepEasing(steps: 5, StepPosition.JumpStart);
TweenSpec<float> spec = new(TimeSpan.FromMilliseconds(180), easing);
```

## Remarks

`StepEasing` maps continuous progress to discrete output levels. `Transform` clamps the input progress to `[0, 1]`, returns `0` for `NaN` input, and clamps the returned value to `[0, 1]`.

The `Position` value controls where jumps occur. `JumpStart` advances immediately at the start of each interval, so `Transform(0)` with four steps returns `0.25`. `JumpEnd` is the default and holds each interval until its end; for four steps, progress just before `0.25` returns `0`, while progress at `0.25` returns `0.25`. `JumpBoth` distributes steps across `steps + 1` positions and also jumps at the start. `JumpNone` preserves exact `0` and `1` endpoints and distributes interior jumps across `steps - 1` intervals.

`JumpNone` requires at least two steps because it needs separate start and end endpoints. Other positions require a positive step count.

## Constructors

| Name | Description |
| --- | --- |
| `StepEasing(int steps, StepPosition position = StepPosition.JumpEnd)` | Initializes a step easing with a positive step count and an optional step jump position. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Steps` | `int` | Gets the number of discrete steps used by the easing. |
| `Position` | `StepPosition` | Gets the step jump mode used by `Transform`. The default is `StepPosition.JumpEnd`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Transform(float progress)` | `float` | Converts normalized progress to stepped progress according to `Position`. Input and output are clamped to `[0, 1]`, and `NaN` input returns `0`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `StepEasing(int, StepPosition)` | `ArgumentOutOfRangeException` | `steps` is less than or equal to `0`, or `position` is `StepPosition.JumpNone` and `steps` is less than `2`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Specs/StepEasing.cs`
- `UI/Motion/Specs/IEasing.cs`
- `UI/Motion/Specs/TweenSpec.cs`
- `UI/Motion/Specs/Easings.cs`
