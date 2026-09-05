# SpriteAnimationClip Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SpriteAnimation.cs`

Defines an immutable, named sequence of sprite-animation frames.

```csharp
public sealed class SpriteAnimationClip
```

Inheritance: `Object` -> `SpriteAnimationClip`

## Examples

```csharp
var walk = new SpriteAnimationClip(
    "Walk",
    [
        new SpriteAnimationFrame(new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(90)),
        new SpriteAnimationFrame(new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(110))
    ],
    isLooping: true,
    version: 1);
```

## Remarks

A clip requires a nonblank name and at least one non-null frame. The constructor defensively copies the sequence. `Duration` is the checked sum of all frame durations; construction fails if that sum exceeds `TimeSpan.MaxValue`. Frame intervals are interpreted as left-closed and right-open by the sprite-animation sampler.

## Constructors

| Name | Description |
| --- | --- |
| `SpriteAnimationClip(string, IEnumerable<SpriteAnimationFrame>, bool)` | Creates a version-1 clip. `isLooping` defaults to `true`. |
| `SpriteAnimationClip(string, IEnumerable<SpriteAnimationFrame>, bool, long)` | Creates a clip with an explicit positive version. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Ordinal clip identifier. |
| `Frames` | `IReadOnlyList<SpriteAnimationFrame>` | Defensively copied frame sequence. |
| `IsLooping` | `bool` | Whether elapsed time wraps after `Duration`. |
| `Duration` | `TimeSpan` | Checked sum of frame durations. |
| `Version` | `long` | Positive caller-supplied definition version. |

## Applies to

Cerneala retained 2D scenes.

## See also

- [SpriteAnimationFrame](Cerneala.UI.Controls.SpriteAnimationFrame.md)
- [SpriteAnimationSet](Cerneala.UI.Controls.SpriteAnimationSet.md)
