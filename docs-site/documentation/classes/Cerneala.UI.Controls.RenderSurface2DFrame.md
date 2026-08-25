# RenderSurface2DFrame Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2DFrame.cs`, `UI/Controls/RenderSurface2DFrame.Shapes.cs`, `UI/Controls/RenderSurface2DFrame.Images.cs`, `UI/Controls/RenderSurface2DFrame.Text.cs`

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

`RenderSurface2DFrame` is a thin lifetime-checking facade over `DrawingContext`. It exposes the same transform, rectangular or geometric clip, group opacity, blend, and isolated-layer state stack alongside the primitive drawing vocabulary.

It also delegates the complete convenience-shape vocabulary: dedicated rounded rectangles and typed-path-based polygons, polylines, arcs, pies, chords, points, circles, triangles, regular polygons, and stars. Convenience angles are radians.

`DrawSprite` is a compatibility name for `DrawImage`. Both overload families record a `DrawImage` command, and new code can use `DrawImage` with `DrawImageFlip`.

Advanced image, arbitrary quad, nine-slice, mesh, triangle, and immutable batch methods remain thin delegates to `DrawingContext`. Image-backed commands register their image dependency with the surface so image changes request an `OnDemand` frame. Reusing an unchanged batch preserves retained identity; a new batch version invalidates its affected bounds.

`DrawTextLayout` is likewise a thin delegate. Reuse a prebuilt layout across `OnDemand` frames to preserve its retained identity and avoid reshaping or reflow; transform and clip scopes compose with the single logical layout command.

An image quad is two affine 2D triangles with explicit or derived UV coordinates. It does not provide 3D perspective or perspective-correct texture mapping. Nine-slice uses deterministic fractional coordinates and proportionally fits its border pairs when the destination is smaller than them.

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
| `DrawRectangle(DrawRect, DrawPen)` | `void` | Strokes a rectangle with complete pen styling. |
| `FillRoundedRectangle(...)` / `DrawRoundedRectangle(...)` | `void` | Delegates dedicated rounded-rectangle fill and stroke overloads. |
| `FillPolygon(...)` / `DrawPolygon(...)` | `void` | Delegates closed polygon fill and stroke overloads. |
| `DrawPolyline(...)` | `void` | Delegates open polyline stroke overloads. |
| `DrawArc(...)` | `void` | Delegates radian elliptical-arc strokes. |
| `FillPie(...)` / `DrawPie(...)` | `void` | Delegates pie fill and stroke overloads. |
| `FillChord(...)` / `DrawChord(...)` | `void` | Delegates chord fill and stroke overloads. |
| `DrawPoint(...)` / `FillCircle(...)` | `void` | Delegates circular point and circle fills. |
| `FillTriangle(...)` / `DrawTriangle(...)` | `void` | Delegates triangle fill and stroke overloads. |
| `FillRegularPolygon(...)` / `DrawRegularPolygon(...)` | `void` | Delegates regular-polygon helpers. |
| `FillStar(...)` / `DrawStar(...)` | `void` | Delegates alternating-radius star helpers. |
| `FillEllipse(DrawRect, Color)` | `void` | Fills an ellipse bounded by the supplied rectangle. |
| `FillEllipse(DrawRect, IDrawBrush)` | `void` | Fills an ellipse with a brush. |
| `DrawEllipse(DrawRect, Color, float)` | `void` | Strokes an ellipse with a color and thickness. |
| `DrawEllipse(DrawRect, IDrawBrush, float)` | `void` | Strokes an ellipse with a brush and thickness. |
| `DrawEllipse(DrawRect, DrawPen)` | `void` | Strokes an ellipse with complete pen styling. |
| `DrawLine(DrawPoint, DrawPoint, Color, float)` | `void` | Draws a line with a color and thickness. |
| `DrawLine(DrawPoint, DrawPoint, IDrawBrush, float)` | `void` | Draws a line with a brush and thickness. |
| `DrawLine(DrawPoint, DrawPoint, DrawPen)` | `void` | Draws a line with styled caps and dashes. |
| `FillPath(string, DrawRect, DrawRect, IDrawBrush)` | `void` | Fills SVG path data mapped from source bounds into destination bounds. |
| `FillPath(DrawPath, IDrawBrush, DrawFillRule)` | `void` | Fills reusable typed geometry with a brush. |
| `FillPath(DrawPath, Color, DrawFillRule)` | `void` | Fills reusable typed geometry with a color. |
| `FillPath(DrawPath, DrawRect, DrawRect, IDrawBrush, DrawFillRule)` | `void` | Maps reusable geometry and fills it with a brush. |
| `FillPath(DrawPath, DrawRect, DrawRect, Color, DrawFillRule)` | `void` | Maps reusable geometry and fills it with a color. |
| `DrawPath(DrawPath, DrawPen)` | `void` | Strokes reusable typed geometry with a complete pen. |
| `DrawPath(DrawPath, DrawRect, DrawRect, DrawPen)` | `void` | Maps and strokes reusable typed geometry. |
| `DrawText(DrawTextRun, DrawPoint, Color)` | `void` | Draws a text run with a color. |
| `DrawText(DrawTextRun, DrawPoint, IDrawBrush)` | `void` | Draws a text run with a brush. |
| `DrawTextLayout(DrawTextLayout, DrawPoint)` | `void` | Draws one reusable multi-line styled layout. |
| `DrawImage(IDrawImage, DrawRect, Color)` | `void` | Draws an entire image into a destination rectangle. |
| `DrawImage(IDrawImage, DrawRect, DrawRect?, Color, float, DrawPoint, DrawImageFlip, float)` | `void` | Draws an optional source region with tint, rotation, origin, mirroring, and layer depth. |
| `DrawImage(IDrawImage, DrawRect, DrawImageOptions)` | `void` | Draws an image with explicit source pixels, appearance, transform, sampling, and addressing. |
| `DrawImageQuad(...)` | `void` | Draws a four-position or explicit-vertex 2D image quad as exactly two triangles. |
| `DrawNineSlice(IDrawImage, DrawRect, DrawInsets, DrawImageOptions?)` | `void` | Draws nine validated source regions through one mesh command. |
| `DrawMesh(DrawMesh2D, DrawSamplingMode, DrawAddressMode, float)` | `void` | Delegates one indexed colored or textured mesh. |
| `DrawTriangles(IEnumerable<DrawVertex2D>, IDrawImage?, DrawSamplingMode, DrawAddressMode, float)` | `void` | Delegates sequential triangle-list vertices through the mesh path. |
| `DrawPointBatch(DrawPointBatch)` | `void` | Delegates one immutable point batch. |
| `DrawLineBatch(DrawLineBatch)` | `void` | Delegates one immutable line batch. |
| `DrawSpriteBatch(DrawSpriteBatch)` | `void` | Delegates one immutable same-image sprite batch and tracks its image dependency. |
| `DrawSprite(IDrawImage, DrawRect, Color)` | `void` | Compatibility alias for drawing an entire image. |
| `DrawSprite(IDrawImage, DrawRect, DrawRect?, Color, float, DrawPoint, RenderSurface2DSpriteFlip, float)` | `void` | Compatibility alias for transformed image drawing. |
| `PushClip(DrawRect)` | `void` | Begins a rectangular clip scope. |
| `PushClip(DrawPath, DrawFillRule)` | `void` | Begins a geometric clip scope. |
| `PopClip()` | `void` | Ends the current clip scope. |
| `PushTransform(Matrix3x2)` / `PopTransform()` | `void` | Begins or ends an affine transform scope. |
| `PushOpacity(float)` / `PopOpacity()` | `void` | Begins or ends a group-opacity scope. |
| `PushBlend(DrawBlendMode)` / `PopBlend()` | `void` | Begins or ends a blend scope. |
| `PushLayer(DrawLayerOptions)` / `PopLayer()` | `void` | Begins or ends an isolated layer. |
| `Transform(Matrix3x2)` | `DrawTransformScope` | Returns an ergonomic LIFO transform scope. |
| `Clip(DrawRect)` / `Clip(DrawPath, DrawFillRule)` | `DrawClipScope` | Returns an ergonomic clip scope. |
| `Opacity(float)` | `DrawOpacityScope` | Returns an ergonomic group-opacity scope. |
| `Blend(DrawBlendMode)` | `DrawBlendScope` | Returns an ergonomic blend scope. |
| `Layer(DrawLayerOptions)` | `DrawLayerScope` | Returns an ergonomic isolated-layer scope. |

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
| Advanced image and mesh methods | `ArgumentException` or `ArgumentOutOfRangeException` | Image dimensions, source bounds, insets, topology, indices, sampling, addressing, or batch contents are invalid. |

## Applies To

Cerneala managed 2D surface drawing and retained rendering.

## See Also

- `RenderSurface2D`
- `DrawingContext`
- `DrawCommand`
- `DrawImageFlip`
- `DrawPath`
- `DrawFillRule`
- `DrawPen`
- `DrawStrokeStyle`
- `DrawLayerOptions`
- `DrawCommandStateAnalyzer`
- `DrawCornerRadius`
- `DrawArcDirection`
- `DrawPathFactory`
- `DrawImageOptions`
- `DrawInsets`
- `DrawMesh2D`
- `DrawPointBatch`
- `DrawLineBatch`
- `DrawSpriteBatch`
- `DrawTextLayout`
- `RenderSurface2DSpriteFlip`
- `PrismImage`
- `PrismPipeline`
