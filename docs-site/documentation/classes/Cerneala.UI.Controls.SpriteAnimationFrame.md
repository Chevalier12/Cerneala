# SpriteAnimationFrame Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SpriteAnimation.cs`

Defines one immutable source-atlas frame and its canonical duration.

```csharp
public sealed class SpriteAnimationFrame
```

Inheritance: `Object` -> `SpriteAnimationFrame`

## Examples

```csharp
var frame = new SpriteAnimationFrame(
    new DrawRect(16, 0, 16, 16),
    TimeSpan.FromMilliseconds(90),
    RenderSurface2DSpriteFlip.Horizontal);
```

## Remarks

`SourceRect` must have finite coordinates and strictly positive finite dimensions. `Duration` must be greater than zero. `Flip` may contain the horizontal and vertical flags only. The frame owns no playback state and can be shared by any number of clips and sprite instances.

## Constructors

| Name | Description |
| --- | --- |
| `SpriteAnimationFrame(DrawRect, TimeSpan, RenderSurface2DSpriteFlip)` | Creates a validated immutable frame. `flip` defaults to `None`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SourceRect` | `DrawRect` | Rectangle sampled from the sprite atlas. |
| `Duration` | `TimeSpan` | Canonical duration of this frame. |
| `Flip` | `RenderSurface2DSpriteFlip` | Frame-local flip flags. |

## Applies to

Cerneala retained 2D scenes.

## See also

- [SpriteAnimationClip](Cerneala.UI.Controls.SpriteAnimationClip.md)
- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
