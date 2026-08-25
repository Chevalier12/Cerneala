# DrawingContext Class

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawingContext.cs`, `Drawing/DrawingContext.Shapes.cs`, `Drawing/DrawingContext.Images.cs`, `Drawing/DrawingContext.Text.cs`

Records high-level drawing operations into a `DrawCommandList`.

```csharp
public sealed class DrawingContext
```

Inheritance:
`Object` -> `DrawingContext`

## Examples

Create a command list, draw a clipped rectangle, and inspect the recorded commands:

```csharp
using Cerneala.Drawing;

DrawCommandList commands = new();
DrawingContext drawing = new(commands);

drawing.PushClip(new DrawRect(0, 0, 100, 100));
drawing.FillRectangle(new DrawRect(10, 10, 50, 25), Color.White);
drawing.DrawRectangle(new DrawRect(10, 10, 50, 25), Color.Black, 2);
drawing.PopClip();

DrawCommand first = commands[0]; // DrawCommandKind.PushClip
DrawCommand fill = commands[1];  // DrawCommandKind.FillRectangle
```

### Typed paths and fill rules

Build a path once, then reuse the immutable geometry for fills, strokes, and clips:

```csharp
DrawPath loop = new DrawPathBuilder()
    .MoveTo(new DrawPoint(8, 8))
    .CubicTo(
        new DrawPoint(56, -8),
        new DrawPoint(56, 72),
        new DrawPoint(8, 56))
    .Close()
    .Build();

drawing.FillPath(loop, fillBrush, DrawFillRule.EvenOdd);
```

### Complete strokes

```csharp
DrawPen dashedRoundPen = new(
    accentBrush,
    thickness: 3,
    new DrawStrokeStyle(
        startCap: DrawLineCap.Round,
        endCap: DrawLineCap.Round,
        join: DrawLineJoin.Round,
        dashPattern: [8, 4],
        dashOffset: 2));

drawing.DrawPath(loop, dashedRoundPen);
```

### State scopes

```csharp
using DrawClipScope clip = drawing.Clip(new DrawRect(0, 0, 160, 100));
using DrawLayerScope layer = drawing.Layer(
    new DrawLayerOptions(opacity: 0.75f, blendMode: DrawBlendMode.Screen));
using DrawTransformScope transform = drawing.Transform(
    System.Numerics.Matrix3x2.CreateRotation(0.15f));

drawing.FillCircle(new DrawPoint(64, 48), 32, accentBrush);
```

### Rectangular shapes

```csharp
drawing.FillRectangle(new DrawRect(8, 8, 96, 48), panelBrush);
drawing.DrawRoundedRectangle(
    new DrawRect(8, 72, 96, 48),
    new DrawCornerRadius(4, 16, 4, 16),
    dashedRoundPen);
```

### Curves

```csharp
drawing.FillEllipse(new DrawRect(8, 8, 80, 48), accentBrush);
drawing.DrawArc(
    new DrawPoint(128, 48),
    radiusX: 36,
    radiusY: 24,
    startAngle: 0,
    sweepAngle: MathF.PI * 1.5f,
    dashedRoundPen);
drawing.FillPie(new DrawPoint(220, 48), 36, 28, 0, MathF.PI, fillBrush);
drawing.FillChord(new DrawPoint(310, 48), 36, 28, 0, MathF.PI, panelBrush);
```

### Polygonal shapes

```csharp
drawing.DrawPoint(new DrawPoint(12, 12), accentBrush, diameter: 8);
drawing.DrawPolyline(polylinePoints, dashedRoundPen);
drawing.FillPolygon(polygonPoints, fillBrush, DrawFillRule.NonZero);
drawing.FillTriangle(a, b, c, accentBrush);
drawing.FillRegularPolygon(new DrawPoint(160, 80), 36, 6, panelBrush);
drawing.FillStar(new DrawPoint(250, 80), 40, 18, 5, accentBrush);
```

### Images

```csharp
DrawImageOptions imageOptions = new(
    source: new DrawRect(0, 0, 64, 64),
    tint: Color.White,
    opacity: 0.9f,
    rotation: MathF.PI / 12,
    origin: new DrawPoint(32, 32),
    flip: DrawImageFlip.Horizontal,
    sampling: DrawSamplingMode.Linear,
    addressMode: DrawAddressMode.Clamp);

drawing.DrawImage(image, new DrawRect(24, 24, 96, 96), imageOptions);
drawing.DrawImageQuad(image, topLeft, topRight, bottomRight, bottomLeft, imageOptions);
drawing.DrawNineSlice(image, new DrawRect(144, 24, 160, 96), new DrawInsets(12));
```

### Meshes and batches

```csharp
DrawMesh2D mesh = new(
    [
        new DrawVertex2D(new DrawPoint(0, 0), Color.Red),
        new DrawVertex2D(new DrawPoint(80, 0), Color.LimeGreen),
        new DrawVertex2D(new DrawPoint(40, 72), Color.Blue)
    ],
    [0, 1, 2]);

DrawPointBatch pointBatch = new(points, accentColor, diameter: 5);
DrawLineBatch lineBatch = new(lineSegments);
DrawSpriteBatch spriteBatch = new(image, sprites);

drawing.DrawMesh(mesh);
drawing.DrawPointBatch(pointBatch);
drawing.DrawLineBatch(lineBatch);
drawing.DrawSpriteBatch(spriteBatch);
```

### Styled text layout

```csharp
DrawTextLayout layout = new DrawTextLayoutBuilder()
    .AddSpan("Status: ", regularFont, 14, foregroundBrush)
    .AddSpan("Ready", semiboldFont, 14, accentBrush)
    .Build(new DrawTextLayoutOptions(
        maxWidth: 280,
        wrapping: DrawTextWrapping.Word,
        alignment: DrawTextAlignment.Center,
        maxLines: 2,
        trimming: DrawTextTrimming.WordEllipsis));

drawing.DrawTextLayout(layout, new DrawPoint(16, 16));
```

### Retained surface and Prism composition

Use `OnDemand` for content that changes only when its data or resources change. Image-backed commands are dependency-tracked, and a `PrismImage` remains an `IDrawImage`:

```csharp
RenderSurface2D surface = new()
{
    RedrawMode = RenderSurface2DRedrawMode.OnDemand
};

PrismImage effected = Prism.Apply(image, new PrismPipeline
{
    new OuterGlowStyle { Size = 4, Opacity = 0.4f, Color = Color.Cyan }
});

surface.Draw += (_, frame) =>
    frame.DrawImage(effected, new DrawRect(16, 16, 96, 96), Color.White);
```

## Remarks

`DrawingContext` is a thin recording facade over `DrawCommandList`. Each public drawing method creates the matching `DrawCommand` and appends it to the list supplied to the constructor. The context does not render directly; backends consume the recorded commands later.

Stroke methods delegate validation to `DrawCommand`. Invalid stroke thickness values throw `ArgumentOutOfRangeException`. `DrawLine` and `DrawText` also validate their points against the supported pixel coordinate range. `DrawText` throws `ArgumentNullException` for a null `DrawTextRun`, and `DrawImage` throws `ArgumentNullException` for a null image.

State commands share one LIFO stack. Rectangular clips remain on the scissor fast path when their accumulated transform is axis-aligned; typed paths provide geometric clips. Opacity and layers isolate their children before compositing, so overlapping children receive group opacity once. `System.Numerics.Matrix3x2` transforms compose by multiplying each newly pushed local matrix on the left of the accumulated parent matrix; points therefore receive the innermost local transform before their parent transforms.

The raw `Push`/`Pop` methods return `void`. `Transform`, `Clip`, `Opacity`, `Blend`, and `Layer` return stack-only `ref struct` scopes for `using` declarations. Both forms must close in reverse creation order.

Typed `FillPath` overloads retain immutable geometry and `DrawFillRule` directly. `DrawPath` accepts a `DrawPen` and uses the same reusable geometry for native caps, joins, dashes, and closed-contour alignment. Reuse a path instance across frames to avoid SVG parsing and geometry allocation. The source/destination overloads map a reusable path into another rectangle while retaining the same path identity.

Rounded rectangles use dedicated commands with proportionally normalized independent corner radii. Other compound helpers lower to immutable `DrawPath` instances and share the path fill/stroke pipeline. Arc, pie, chord, regular-polygon, and star angles are radians; increasing angles are clockwise in the downward-positive drawing coordinate system.

Advanced image drawing accepts source rectangles and origins in source-image pixels together with tint, opacity, rotation, flip, depth, sampling, and addressing. Image quads use exactly two affine 2D triangles and do not promise perspective-correct mapping. Nine-slice drawing uses one deterministic mesh and proportionally fits opposing borders into undersized destinations.

Meshes and immutable point, line, and sprite batches record one logical command each. Textured meshes and sprite batches retain but do not own their `IDrawImage`. `PrismImage` inputs are recorded through the same native Prism scope used by compatibility image drawing.

`DrawTextLayout` records one immutable multi-line layout command. Build and retain layouts outside repeated frame callbacks; identical content, font/brush identities, constraints, options, and scale reuse the shared layout result. Drawing transforms rotate or scale the complete layout, and drawing clips apply without a separate text clipping API.

### Mental model

| Concern | Ownership and behavior |
| --- | --- |
| Command list | `DrawingContext` records immutable logical commands; the backend interprets them later. |
| State scopes | Transform, clip, opacity, blend, layer, and Prism scopes share one strict LIFO stack. |
| Retained rendering | Command identity, conservative bounds, and resource versions decide cache reuse and damage. |
| Resources | Commands retain resource references but do not transfer ownership; disposed or changed resources invalidate or reject the affected work deterministically. |
| Prism | Drawing provides base 2D composition. Prism consumes the same command stream for filters, styles, masks, and graph composition. |

### Cost and reuse guidance

| Work | Recommended path |
| --- | --- |
| SVG or compound geometry | Parse or build a `DrawPath` once and reuse it instead of parsing in every frame. |
| Styled or wrapped text | Build a `DrawTextLayout` outside the draw callback and retain it until content, font, constraints, or scale changes. |
| Many points, lines, or same-atlas sprites | Prefer the immutable batch types so one logical command replaces many individual commands. |
| Occasional isolated composition | Use a layer only when group opacity or blend isolation is required; it may allocate or lease an intermediate render target. |
| Static surface content | Prefer `RenderSurface2DRedrawMode.OnDemand`; resource dependency changes request the necessary frame automatically. |

## Constructors

| Name | Description |
| --- | --- |
| `DrawingContext(DrawCommandList)` | Initializes a drawing context that appends commands to the supplied command list. Throws `ArgumentNullException` when `commands` is null. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `FillRectangle(DrawRect, Color)` | `void` | Appends a `FillRectangle` command for the specified rectangle and color. |
| `FillRectangle(DrawRect, IDrawBrush)` | `void` | Appends a brush-based `FillRectangle` command. |
| `DrawRectangle(DrawRect, Color, float)` | `void` | Appends a `DrawRectangle` stroke command with the specified rectangle, color, and positive thickness. |
| `DrawRectangle(DrawRect, IDrawBrush, float)` | `void` | Appends a brush-based `DrawRectangle` stroke command with the specified positive thickness. |
| `DrawRectangle(DrawRect, DrawPen)` | `void` | Appends a complete native rectangle stroke. |
| `FillRoundedRectangle(...)` | `void` | Fills a dedicated rounded rectangle with color or brush and independent radii. |
| `DrawRoundedRectangle(...)` | `void` | Strokes a rounded rectangle with color, brush, or `DrawPen`. |
| `FillPolygon(...)` / `DrawPolygon(...)` | `void` | Fills or strokes a closed polygon through typed-path geometry. |
| `DrawPolyline(...)` | `void` | Strokes an open point sequence. |
| `DrawArc(...)` | `void` | Strokes an open elliptical arc using radian angles and an optional direction. |
| `FillPie(...)` / `DrawPie(...)` | `void` | Fills or strokes a closed center-to-arc sector. |
| `FillChord(...)` / `DrawChord(...)` | `void` | Fills or strokes an arc closed directly between its endpoints. |
| `DrawPoint(...)` | `void` | Draws a circular point with a color or brush and diameter. |
| `FillCircle(...)` | `void` | Fills a circle with a color or brush. |
| `FillTriangle(...)` / `DrawTriangle(...)` | `void` | Fills or strokes a triangle through polygon geometry. |
| `FillRegularPolygon(...)` / `DrawRegularPolygon(...)` | `void` | Fills or strokes a regular polygon with optional radian rotation. |
| `FillStar(...)` / `DrawStar(...)` | `void` | Fills or strokes an alternating-radius star; fills accept a `DrawFillRule`. |
| `FillEllipse(DrawRect, Color)` | `void` | Appends a `FillEllipse` command for the specified bounds and color. |
| `FillEllipse(DrawRect, IDrawBrush)` | `void` | Appends a brush-based `FillEllipse` command. |
| `DrawEllipse(DrawRect, Color, float)` | `void` | Appends a `DrawEllipse` stroke command with the specified bounds, color, and positive thickness. |
| `DrawEllipse(DrawRect, IDrawBrush, float)` | `void` | Appends a brush-based `DrawEllipse` stroke command with the specified positive thickness. |
| `DrawEllipse(DrawRect, DrawPen)` | `void` | Appends a complete native ellipse stroke. |
| `DrawLine(DrawPoint, DrawPoint, Color, float)` | `void` | Appends a `DrawLine` command from `start` to `end` with the specified color and positive thickness. |
| `DrawLine(DrawPoint, DrawPoint, IDrawBrush, float)` | `void` | Appends a brush-based `DrawLine` command with the specified positive thickness. |
| `DrawLine(DrawPoint, DrawPoint, DrawPen)` | `void` | Appends a complete native line stroke. |
| `FillPath(string, DrawRect, DrawRect, IDrawBrush)` | `void` | Appends an SVG path fill command using a source view box, destination bounds, and brush. |
| `FillPath(DrawPath, IDrawBrush, DrawFillRule)` | `void` | Fills a reusable typed path with a brush and fill rule. |
| `FillPath(DrawPath, Color, DrawFillRule)` | `void` | Fills a reusable typed path with a color and fill rule. |
| `FillPath(DrawPath, DrawRect, DrawRect, IDrawBrush, DrawFillRule)` | `void` | Maps a reusable path from source bounds into destination bounds and fills it with a brush. |
| `FillPath(DrawPath, DrawRect, DrawRect, Color, DrawFillRule)` | `void` | Maps and fills a reusable path with a color. |
| `DrawPath(DrawPath, DrawPen)` | `void` | Strokes reusable typed geometry with a complete pen. |
| `DrawPath(DrawPath, DrawRect, DrawRect, DrawPen)` | `void` | Maps and strokes reusable typed geometry. |
| `DrawText(DrawTextRun, DrawPoint, Color)` | `void` | Appends a compatibility `DrawText` command for the text run at the specified position and color. |
| `DrawText(DrawTextRun, DrawPoint, IDrawBrush)` | `void` | Appends a `DrawText` command that applies the complete brush through the glyph coverage mask. |
| `DrawTextLayout(DrawTextLayout, DrawPoint)` | `void` | Appends one logical reusable text-layout command at the supplied origin. |
| `DrawImage(IDrawImage, DrawRect, Color)` | `void` | Appends a `DrawImage` command for the image, destination rectangle, and tint color. |
| `DrawImage(IDrawImage, DrawRect, DrawRect?, Color, float, DrawPoint, DrawImageFlip, float)` | `void` | Appends a `DrawImage` command with an optional source region, tint, rotation, origin, mirroring, and layer depth. |
| `DrawImage(IDrawImage, DrawRect, DrawImageOptions)` | `void` | Appends an advanced image command including sampling and addressing. |
| `DrawImageQuad(...)` | `void` | Appends an explicit-UV or image-option 2D quad as two triangles. |
| `DrawNineSlice(IDrawImage, DrawRect, DrawInsets, DrawImageOptions?)` | `void` | Appends a validated nine-region image mesh. |
| `DrawMesh(DrawMesh2D, DrawSamplingMode, DrawAddressMode, float)` | `void` | Appends one indexed colored or textured mesh command. |
| `DrawTriangles(IEnumerable<DrawVertex2D>, IDrawImage?, DrawSamplingMode, DrawAddressMode, float)` | `void` | Appends sequential triangle-list vertices through the mesh path. |
| `DrawPointBatch(DrawPointBatch)` | `void` | Appends one immutable point-batch command. |
| `DrawLineBatch(DrawLineBatch)` | `void` | Appends one immutable line-batch command. |
| `DrawSpriteBatch(DrawSpriteBatch)` | `void` | Appends one immutable same-image sprite-batch command. |
| `PushClip(DrawRect)` | `void` | Appends a `PushClip` command for the specified clip rectangle. |
| `PushClip(DrawPath, DrawFillRule)` | `void` | Begins a geometric clip using reusable typed geometry. |
| `PopClip()` | `void` | Appends a `PopClip` command. |
| `PushTransform(Matrix3x2)` / `PopTransform()` | `void` | Begins or ends an affine transform scope. |
| `PushOpacity(float)` / `PopOpacity()` | `void` | Begins or ends a real group-opacity scope. |
| `PushBlend(DrawBlendMode)` / `PopBlend()` | `void` | Begins or ends a blend-mode scope. |
| `PushLayer(DrawLayerOptions)` / `PopLayer()` | `void` | Begins or ends an isolated layer. |
| `Transform(Matrix3x2)` | `DrawTransformScope` | Begins a transform and returns its LIFO scope. |
| `Clip(DrawRect)` / `Clip(DrawPath, DrawFillRule)` | `DrawClipScope` | Begins a rectangular or geometric clip and returns its scope. |
| `Opacity(float)` | `DrawOpacityScope` | Begins group opacity and returns its scope. |
| `Blend(DrawBlendMode)` | `DrawBlendScope` | Begins a blend mode and returns its scope. |
| `Layer(DrawLayerOptions)` | `DrawLayerScope` | Begins an isolated layer and returns its scope. |

## Applies To

Cerneala drawing command recording and retained rendering infrastructure.

## See Also

- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.Drawing.DrawRect`
- `Cerneala.Drawing.DrawImageFlip`
- `Cerneala.Drawing.DrawPath`
- `Cerneala.Drawing.DrawFillRule`
- `Cerneala.Drawing.DrawPen`
- `Cerneala.Drawing.DrawStrokeStyle`
- `Cerneala.Drawing.DrawCommandStateAnalyzer`
- `Cerneala.Drawing.DrawLayerOptions`
- `Cerneala.Drawing.DrawCornerRadius`
- `Cerneala.Drawing.DrawArcDirection`
- `Cerneala.Drawing.DrawPathFactory`
- `Cerneala.Drawing.DrawImageOptions`
- `Cerneala.Drawing.DrawMesh2D`
- `Cerneala.Drawing.DrawPointBatch`
- `Cerneala.Drawing.DrawLineBatch`
- `Cerneala.Drawing.DrawSpriteBatch`
- `Cerneala.Drawing.DrawTextLayout`
- `Cerneala.Drawing.Color`
