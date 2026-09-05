# SpriteAnimationStateChangeMode Enum

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SpriteAnimation.cs`

Selects how per-instance progress is handled when an animated scene node enters another named clip.

```csharp
public enum SpriteAnimationStateChangeMode
```

## Examples

```csharp
SpriteAnimationStateChangeMode mode = SpriteAnimationStateChangeMode.Resume;
```

## Remarks

The enum contains policy only. Playback state remains on each sprite or promoted tile and is never stored in `SpriteAnimationSet`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Restart` | `0` | Enter the target clip at its first frame. |
| `Resume` | `1` | Restore progress previously saved for the target clip by the same instance. |

## Applies to

Cerneala retained 2D scenes.

## See also

- [SpriteAnimationSet](Cerneala.UI.Controls.SpriteAnimationSet.md)
- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
