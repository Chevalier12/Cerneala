# RenderSurface2DFrame Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2DFrame.cs`

Records Cerneala drawing operations for one `RenderSurface2D` frame.

```csharp
public sealed class RenderSurface2DFrame
```

## Examples

Compose vector primitives, text, clipping, and an image in the surface callback.

```csharp
surface.Draw += (_, frame) =>
{
    frame.PushClip(frame.Bounds);
    frame.FillEllipse(new DrawRect(24, 24, 96, 96), accentBrush);
    frame.DrawLine(new DrawPoint(0, 0), new DrawPoint(160, 120), Color.White, 2);
    frame.DrawText(scoreRun, new DrawPoint(16, 24), Color.White);
    frame.DrawImage(player, new DrawRect(64, 48, 32, 32), Color.White);
    frame.PopClip();
};
```

Draw a region from an image with rotation and horizontal mirroring.

```csharp
frame.DrawImage(
    atlas,
    destination: new DrawRect(96, 64, 32, 32),
    source: new DrawRect(128, 0, 32, 32),
    color: Color.White,
    rotation: MathF.PI / 4,
    origin: new DrawPoint(16, 16),
    flip: DrawImageFlip.Horizontal,
    layerDepth: 0.5f);
```

Apply a reusable code-defined Prism pipeline to an image and draw it like any other `IDrawImage`.

```csharp
PrismPipeline effects = new()
{
    new BlurFilter { Radius = 0.75f },
    new OuterGlowStyle
    {
        Size = 4,
        Opacity = 0.38f,
        Color = Color.FromArgb(0x70, 0xFF, 0x48, 0x90)
    }
};

PrismImage glowingSprite = Prism.Apply(sprite, effects);
frame.DrawImage(glowingSprite, destination, Color.White);
```

## Remarks

`RenderSurface2DFrame` exposes the same general command vocabulary as `DrawingContext`: rectangles, ellipses, lines, SVG paths, text, images, brushes, and rectangular clip scopes. These methods record the common `DrawCommand` model; the frame does not maintain a separate renderer-specific command stream.

`DrawSprite` is a compatibility name for `DrawImage`. Both overload families record a `DrawImage` command, and new code can use `DrawImage` with `DrawImageFlip`.

When the image is a `PrismImage`, `DrawImage` records a native Prism scope around the source-image command. It does not use a surface-specific effect workaround or a separate shader path.

The frame is created and owned by Cerneala. It is valid only while `RenderSurface2D.OnDraw` and the surface's `Draw` subscribers execute. Calling a drawing method after the callback returns throws `ObjectDisposedException`.

`Bounds` uses local surface pixels and begins at `(0, 0)`. Image rotation is expressed in radians, and `origin` uses source-image pixels. Cerneala owns the render target, drawing state, retained command stream, and presentation.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect` | Gets the local pixel bounds of the surface. |
| `FrameTime` | `TimeSpan` | Gets the elapsed frame time supplied by the Cerneala render loop. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `FillRectangle(DrawRect, Color)` | `void` | Fills a rectangle with a color. |
| `FillRectangle(DrawRect, IDrawBrush)` | `void` | Fills a rectangle with a brush. |
| `DrawRectangle(DrawRect, Color, float)` | `void` | Strokes a rectangle with a color and thickness. |
| `DrawRectangle(DrawRect, IDrawBrush, float)` | `void` | Strokes a rectangle with a brush and thickness. |
| `FillEllipse(DrawRect, Color)` | `void` | Fills an ellipse bounded by the supplied rectangle. |
| `FillEllipse(DrawRect, IDrawBrush)` | `void` | Fills an ellipse with a brush. |
| `DrawEllipse(DrawRect, Color, float)` | `void` | Strokes an ellipse with a color and thickness. |
| `DrawEllipse(DrawRect, IDrawBrush, float)` | `void` | Strokes an ellipse with a brush and thickness. |
| `DrawLine(DrawPoint, DrawPoint, Color, float)` | `void` | Draws a line with a color and thickness. |
| `DrawLine(DrawPoint, DrawPoint, IDrawBrush, float)` | `void` | Draws a line with a brush and thickness. |
| `FillPath(string, DrawRect, DrawRect, IDrawBrush)` | `void` | Fills SVG path data mapped from source bounds into destination bounds. |
| `DrawText(DrawTextRun, DrawPoint, Color)` | `void` | Draws a text run with a color. |
| `DrawText(DrawTextRun, DrawPoint, IDrawBrush)` | `void` | Draws a text run with a brush. |
| `DrawImage(IDrawImage, DrawRect, Color)` | `void` | Draws an entire image into a destination rectangle. |
| `DrawImage(IDrawImage, DrawRect, DrawRect?, Color, float, DrawPoint, DrawImageFlip, float)` | `void` | Draws an optional source region with tint, rotation, origin, mirroring, and layer depth. |
| `DrawSprite(IDrawImage, DrawRect, Color)` | `void` | Compatibility alias for drawing an entire image. |
| `DrawSprite(IDrawImage, DrawRect, DrawRect?, Color, float, DrawPoint, RenderSurface2DSpriteFlip, float)` | `void` | Compatibility alias for transformed image drawing. |
| `PushClip(DrawRect)` | `void` | Begins a rectangular clip scope. |
| `PopClip()` | `void` | Ends the current clip scope. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Drawing methods | `ObjectDisposedException` | The frame callback has already completed. |
| Brush-based methods | `ArgumentNullException` | `brush` is `null`. |
| Stroke methods | `ArgumentOutOfRangeException` | Thickness is not positive and finite. |
| `FillPath` | `ArgumentException` | Path data is empty or whitespace. |
| `DrawText` | `ArgumentNullException` | `textRun` is `null`. |
| `DrawImage` or `DrawSprite` | `ArgumentNullException` | `image` is `null`. |
| Transformed `DrawImage` or `DrawSprite` | `ArgumentOutOfRangeException` | Rotation is not finite, layer depth is outside `0` through `1`, or flip contains unsupported flags. |

## Applies To

Cerneala managed 2D surface drawing and retained rendering.

## See Also

- `RenderSurface2D`
- `DrawingContext`
- `DrawCommand`
- `DrawImageFlip`
- `RenderSurface2DSpriteFlip`
- `PrismImage`
- `PrismPipeline`
