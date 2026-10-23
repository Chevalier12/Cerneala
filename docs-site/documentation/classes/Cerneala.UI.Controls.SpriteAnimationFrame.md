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

### Frames from a uniform grid

`FromCell` computes the source rectangle of one cell of a grid atlas:

```csharp
// 32 x 32 cells, a 2-pixel border around the grid and 1 pixel between cells.
var frame = SpriteAnimationFrame.FromCell(
    column: 1,
    row: 0,
    cellWidth: 32,
    cellHeight: 32,
    duration: TimeSpan.FromMilliseconds(150),
    margin: 2,
    spacing: 1);
// frame.SourceRect is (35, 2, 32, 32): x = 2 + 1 * (32 + 1), y = 2 + 0 * (32 + 1).
```

Without `margin` and `spacing`, `FromCell(1, 1, 32, 32, duration)` gives `(32, 32, 32, 32)` and `FromCell(0, 2, 32, 32, duration)` gives `(0, 64, 32, 32)`.

### Trimmed frames

A packing tool can cut the transparent border off a frame. The atlas then holds only the visible pixels, plus where they sat inside the original frame. Pass that position as `offset` and the original size as `frameSize`:

```csharp
// The visible 10 x 12 pixels are at (0, 0) in the atlas. In the original
// 32 x 32 frame they started at (8, 4).
var trimmed = new SpriteAnimationFrame(
    new DrawRect(0, 0, 10, 12),
    TimeSpan.FromMilliseconds(100),
    RenderSurface2DSpriteFlip.None,
    offset: new DrawPoint(8, 4),
    frameSize: new DrawSize(32, 32));
```

A `Sprite2D` showing `trimmed` draws each visible pixel exactly where the untrimmed 32 x 32 frame would draw it.

## Remarks

`SourceRect` must have finite coordinates and strictly positive finite dimensions. `Duration` must be greater than zero. `Flip` may contain the horizontal and vertical flags only. The frame owns no playback state and can be shared by any number of clips and sprite instances.

`Offset` and `FrameSize` describe the logical, untrimmed frame. The three-argument constructor sets `Offset` to `(0, 0)` and `FrameSize` to the size of `SourceRect`. The five-argument constructor requires a finite, non-negative `offset`, a finite `frameSize` greater than zero, and `offset + SourceRect` size within `frameSize`; otherwise it throws `ArgumentOutOfRangeException` for `offset` or `frameSize`.

For a sprite that shows a trimmed frame:

- When `Width` or `Height` is not set, the sprite uses `FrameSize`, not the `SourceRect` size.
- `Origin` is measured in the logical frame.
- Hit testing, culling and bounds use the logical frame.
- Only the drawn image is smaller.
- With a flip, the offset is mirrored inside the logical frame. A horizontal flip of the example above starts the visible pixels at `32 - 8 - 10 = 14`.
- Two frames that differ only in `Offset` or `FrameSize` count as different frames for playback invalidation.

`FromCell` requires a non-negative `column` and `row`, finite cell sizes greater than zero, and finite non-negative `margin` and `spacing`. Otherwise it throws `ArgumentOutOfRangeException` for that parameter. It returns an untrimmed frame.

## Constructors

| Name | Description |
| --- | --- |
| `SpriteAnimationFrame(DrawRect, TimeSpan, RenderSurface2DSpriteFlip)` | Creates a validated immutable frame. `flip` defaults to `None`. |
| `SpriteAnimationFrame(DrawRect, TimeSpan, RenderSurface2DSpriteFlip, DrawPoint, DrawSize)` | Creates a trimmed frame whose pixels sit at `offset` inside a logical frame of `frameSize`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SourceRect` | `DrawRect` | Rectangle sampled from the sprite atlas. |
| `Duration` | `TimeSpan` | Canonical duration of this frame. |
| `Flip` | `RenderSurface2DSpriteFlip` | Frame-local flip flags. |
| `Offset` | `DrawPoint` | Top-left of the `SourceRect` pixels inside the logical frame, in source pixels. `(0, 0)` for an untrimmed frame. |
| `FrameSize` | `DrawSize` | Logical (untrimmed) frame size in source pixels. Equal to the `SourceRect` size for an untrimmed frame. |

## Methods

| Name | Description |
| --- | --- |
| `FromCell(int, int, float, float, TimeSpan, RenderSurface2DSpriteFlip, float, float)` | Creates the untrimmed frame of grid cell (`column`, `row`), with optional `margin` and `spacing`. |

## Applies to

Cerneala retained 2D scenes.

## See also

- [SpriteAnimationClip](Cerneala.UI.Controls.SpriteAnimationClip.md)
- [SpriteAnimationSet](Cerneala.UI.Controls.SpriteAnimationSet.md)
- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
