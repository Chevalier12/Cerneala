# FrameTimeEventArgs Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/FrameTimeEventArgs.cs`

Provides data for the `Sprite2D.Awake` and `Scene2D.Awake` events.

```csharp
public sealed class FrameTimeEventArgs : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `FrameTimeEventArgs`

## Examples

The bee gets hungrier by the time of each frame. The first `Awake` after the bee became awake reports `TimeSpan.Zero`, so `hunger` does not change on it. The host updates below use 16 ms, so the later events report `16 ms`.

```csharp
using Cerneala.UI.Controls;

Sprite2D bee = new();
Scene2D scene = new();
scene.Children.Add(bee);
// `scene` is the Scene of a RenderSurface2D that is attached to a UI root.

float hunger = 30;
bee.Awake += (sender, e) =>
{
    // sender is bee. e.Elapsed is 0 ms on the first event, then 16 ms on each later one.
    hunger -= (float)e.Elapsed.TotalSeconds;
};

bee.IsAwake = true;
```

After three host updates of 16 ms, `Awake` was raised three times with `Elapsed` equal to `0 ms`, `16 ms` and `16 ms`, and `hunger` is about `29.968` (`30 - 0.016 - 0.016`).

## Remarks

The render surface that shows the scene creates `FrameTimeEventArgs` when it raises `Awake`. The sender is the `Sprite2D` or `Scene2D` that receives the event.

`Elapsed` is `TimeSpan.Zero` on the first event after the node became awake: after `IsAwake` changed from `false` to `true` while the node was in a shown scene, or after the node entered a shown scene while `IsAwake` was already `true`. Every later event reports the elapsed time of the frame. The same rule holds again after the node falls asleep and is woken, or after it leaves the scene and comes back. The rule is checked by `SceneAwakeTests.AwakeIsRaisedOncePerUpdateWithZeroThenTheFrameTime` and `SceneAwakeTests.ANodeRemovedFromTheSceneStopsAndWhenAddedBackStartsAtZeroAgain`.

The surface reuses instances: one shared instance with a zero `Elapsed` for the first event after a node wakes, and one instance per surface that it keeps while the frame time repeats. The object is immutable, so a handler can keep a reference and read it later, but two events can hand out the same object.

The constructor throws `ArgumentOutOfRangeException` for a negative `elapsed`. Zero and positive values are kept as given (`SceneAwakeTests.FrameTimeEventArgsRejectsANegativeElapsedAndKeepsTheValue`).

```csharp
FrameTimeEventArgs frame = new(TimeSpan.FromMilliseconds(16)); // frame.Elapsed == 16 ms
FrameTimeEventArgs bad = new(TimeSpan.FromTicks(-1));          // throws ArgumentOutOfRangeException
```

See [Sprite2D, Awake](Cerneala.UI.Controls.Sprite2D.md#awake) for when the event is raised and [Scene2D, Awake](Cerneala.UI.Controls.Scene2D.md#awake) for groups.

## Constructors

| Name | Description |
| --- | --- |
| `FrameTimeEventArgs(TimeSpan elapsed)` | Initializes the event data with the time the frame advanced. Throws `ArgumentOutOfRangeException` when `elapsed` is negative. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Elapsed` | `TimeSpan` | Gets the time the frame advanced: `TimeSpan.Zero` for the first event after the node became awake, then the elapsed time of the frame. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Controls.Sprite2D`
- `Cerneala.UI.Controls.Scene2D`
- `Cerneala.UI.Controls.RenderSurface2D`
