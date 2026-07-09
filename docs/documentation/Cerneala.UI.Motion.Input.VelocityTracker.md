# VelocityTracker Class

## Definition
Namespace: `Cerneala.UI.Motion.Input`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Input/VelocityTracker.cs`

Tracks the most recent two-dimensional pointer velocity from timestamped position samples.

```csharp
public sealed class VelocityTracker
```

Inheritance:
`object` -> `VelocityTracker`

## Examples

Calculate horizontal and vertical velocity from two pointer samples:

```csharp
using Cerneala.UI.Motion.Input;

VelocityTracker tracker = new();

tracker.Reset(10, 20, TimeSpan.Zero);
tracker.Add(34, 44, TimeSpan.FromMilliseconds(16));

float velocityX = tracker.VelocityX;
float velocityY = tracker.VelocityY;
```

## Remarks

`VelocityTracker` stores the previous pointer position and timestamp, then computes velocity when a later sample is added. Velocities are expressed in coordinate units per second.

`Reset` establishes the baseline sample and clears both velocity values to `0`. Calling `Add` before any sample has been recorded is equivalent to calling `Reset` with that sample.

When `Add` receives a timestamp that is not later than the previous sample, the tracker updates its stored position and timestamp but leaves `VelocityX` and `VelocityY` unchanged. This avoids dividing by zero or a negative time delta.

`DragMotionController` uses this tracker to expose drag velocity and to apply velocity-based settling when a drag ends.

## Constructors

| Name | Description |
| --- | --- |
| `VelocityTracker()` | Initializes a new tracker with zero velocity and no recorded sample. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `VelocityX` | `float` | Gets the most recently calculated horizontal velocity in coordinate units per second. |
| `VelocityY` | `float` | Gets the most recently calculated vertical velocity in coordinate units per second. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Reset(float x, float y, TimeSpan time)` | `void` | Records a baseline sample and clears both velocity values to `0`. |
| `Add(float x, float y, TimeSpan time)` | `void` | Adds a new sample, calculating velocity from the previous sample when `time` is later than the stored timestamp. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Input/VelocityTracker.cs`
- `UI/Motion/Input/DragMotionController.cs`
