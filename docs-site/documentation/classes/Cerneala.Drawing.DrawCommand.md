# DrawCommand Struct

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawCommand.cs`, `Drawing/DrawCommand.Shapes.cs`, `Drawing/DrawCommand.Images.cs`, `Drawing/DrawCommand.Text.cs`

Represents one immutable drawing instruction recorded by the Cerneala drawing pipeline.

```csharp
public readonly record struct DrawCommand
```

Inheritance:
`object` -> `ValueType` -> `DrawCommand`

Implements:
`IEquatable<DrawCommand>`

## Examples

Create commands directly and inspect the command kind before handing them to a backend or command list:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

DrawCommand fill = DrawCommand.FillRectangle(
    new DrawRect(0, 0, 120, 48),
    Color.White);

DrawCommand line = DrawCommand.DrawLine(
    new DrawPoint(0, 0),
    new DrawPoint(120, 48),
    Color.Black,
    thickness: 2);

if (line.Kind == DrawCommandKind.DrawLine)
{
    DrawPoint start = line.Position;
    DrawPoint end = line.EndPoint;
    float strokeThickness = line.Thickness;
}

IDrawBrush brush = new SolidColorBrush(Color.CornflowerBlue);
DrawCommand brushedFill = DrawCommand.FillRectangle(
    new DrawRect(0, 60, 120, 24),
    brush,
    opacity: 0.8f);
```

## Remarks

`DrawCommand` is a value object for retained drawing work. Each static factory method sets `Kind` and populates only the payload fields needed by that command kind. For example, rounded rectangles retain normalized `CornerRadius`, styled rounded strokes also retain their reusable `Path`, complete stroke commands retain `Pen`, typed path fills retain `Path` and `FillRule`, and advanced images retain `ImageOptions` plus an immutable mesh when required.

`DrawImageQuad` is a strictly 2D operation lowered to exactly two affine-textured triangles. It does not provide 3D perspective or perspective-correct texture mapping. `DrawNineSlice` creates one indexed mesh for nine source regions; opposing destination borders are proportionally reduced when the destination is smaller than their sum.

`BeginPrism` and `EndPrism` delimit a retained Prism capture scope. Only the begin command carries a typed `PrismDrawScope`; backends that do not implement Prism may ignore both delimiters while continuing to process the commands between them.

The field-populating constructor is private, so callers normally create commands through the static factory methods. Because this is a struct, `default(DrawCommand)` is still possible; use the factory methods when a command should represent intentional drawing work.

Stroke factories validate `thickness` as a positive, finite pixel size. `DrawLine` and `DrawText` also validate point coordinates against the drawing pixel range. `DrawText` rejects a null `DrawTextRun`, and `DrawImage` rejects a null `IDrawImage`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `DrawCommandKind` | Identifies the command operation. |
| `Rect` | `DrawRect` | Rectangle or destination bounds used by rectangle, ellipse, image, and clip commands. |
| `Color` | `Color` | Color associated with color-based fill, stroke, text, or image drawing. |
| `Brush` | `IDrawBrush?` | Brush associated with brush-based primitives or text. |
| `BrushOpacity` | `float` | Additional command opacity composed with the brush opacity. |
| `Thickness` | `float` | Stroke thickness for stroke commands. |
| `Text` | `string?` | Text copied from the `DrawTextRun` for text commands. |
| `TextRun` | `DrawTextRun?` | Full text run for text commands. |
| `TextLayout` | `DrawTextLayout?` | Immutable positioned layout for one logical text-layout command. |
| `Position` | `DrawPoint` | Start point for line commands or baseline/origin position for text commands. |
| `EndPoint` | `DrawPoint` | End point for line commands. |
| `Image` | `IDrawImage?` | Image payload for image commands. |
| `ImageSource` | `DrawRect?` | Optional source region for an image command; `null` selects the entire image. |
| `ImageRotation` | `float` | Clockwise image rotation in radians. |
| `ImageOrigin` | `DrawPoint` | Image rotation and placement origin in source-image pixels. |
| `ImageFlip` | `DrawImageFlip` | Image-axis mirroring flags. |
| `LayerDepth` | `float` | Image layer depth from `0` through `1`. |
| `Font` | `IDrawFont?` | Font copied from the `DrawTextRun` for text commands. |
| `PathData` | `string?` | SVG path-data payload for `FillPath` commands. |
| `SourceRect` | `DrawRect` | Source view box used to map SVG coordinates into `Rect`. |
| `Path` | `DrawPath?` | Immutable typed geometry for `FillPath`; legacy SVG factories populate this after parsing. |
| `FillRule` | `DrawFillRule` | Winding rule retained by typed path fill commands. |
| `Pen` | `DrawPen?` | Complete immutable stroke payload for native line, rectangle, ellipse, and typed-path strokes. |
| `Transform` | `System.Numerics.Matrix3x2` | Matrix payload for `PushTransform`. |
| `Opacity` | `float` | Group opacity payload for `PushOpacity`. |
| `BlendMode` | `DrawBlendMode` | Blend payload for `PushBlend`. |
| `LayerOptions` | `DrawLayerOptions?` | Isolated-layer payload for `PushLayer`. |
| `CornerRadius` | `DrawCornerRadius` | Normalized independent radii for rounded-rectangle commands. |
| `ImageOptions` | `DrawImageOptions?` | Complete source, tint, transform, depth, sampling, and addressing state for an advanced image command. |
| `Insets` | `DrawInsets` | Source-border payload for a nine-slice command. |
| `Mesh` | `DrawMesh2D?` | Immutable indexed geometry for quad, nine-slice, mesh, and batch commands. |
| `PointBatch` | `DrawPointBatch?` | Immutable point-batch identity retained by a batch command. |
| `LineBatch` | `DrawLineBatch?` | Immutable line-batch identity retained by a batch command. |
| `SpriteBatch` | `DrawSpriteBatch?` | Immutable same-image sprite-batch identity retained by a batch command. |
| `PrismScope` | `PrismDrawScope?` | Typed retained Prism payload for `BeginPrism`; `null` for other command kinds. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `FillRectangle(DrawRect rect, Color color)` | `DrawCommand` | Creates a `FillRectangle` command with `Rect` and `Color` populated. |
| `FillRectangle(DrawRect rect, IDrawBrush brush, float opacity = 1)` | `DrawCommand` | Creates a brush-based `FillRectangle` command. |
| `DrawRectangle(DrawRect rect, Color color, float thickness)` | `DrawCommand` | Creates a `DrawRectangle` command and validates `thickness`. |
| `DrawRectangle(DrawRect rect, IDrawBrush brush, float thickness, float opacity = 1)` | `DrawCommand` | Creates a brush-based `DrawRectangle` command and validates `thickness`. |
| `DrawRectangle(DrawRect rect, DrawPen pen)` | `DrawCommand` | Creates a native rectangle stroke with complete cap, join, dash, and alignment state. |
| `FillRoundedRectangle(DrawRect, DrawCornerRadius, Color)` | `DrawCommand` | Creates the dedicated color rounded-rectangle fill command. |
| `FillRoundedRectangle(DrawRect, DrawCornerRadius, IDrawBrush, float)` | `DrawCommand` | Creates the dedicated brush rounded-rectangle fill command. |
| `DrawRoundedRectangle(DrawRect, DrawCornerRadius, Color, float)` | `DrawCommand` | Creates a rounded-rectangle color stroke. |
| `DrawRoundedRectangle(DrawRect, DrawCornerRadius, IDrawBrush, float, float)` | `DrawCommand` | Creates a rounded-rectangle brush stroke. |
| `DrawRoundedRectangle(DrawRect, DrawCornerRadius, DrawPen)` | `DrawCommand` | Creates a complete rounded-rectangle stroke on reusable typed geometry. |
| `FillEllipse(DrawRect bounds, Color color)` | `DrawCommand` | Creates a `FillEllipse` command with `Rect` and `Color` populated. |
| `FillEllipse(DrawRect bounds, IDrawBrush brush, float opacity = 1)` | `DrawCommand` | Creates a brush-based `FillEllipse` command. |
| `DrawEllipse(DrawRect bounds, Color color, float thickness)` | `DrawCommand` | Creates a `DrawEllipse` command and validates `thickness`. |
| `DrawEllipse(DrawRect bounds, IDrawBrush brush, float thickness, float opacity = 1)` | `DrawCommand` | Creates a brush-based `DrawEllipse` command and validates `thickness`. |
| `DrawEllipse(DrawRect bounds, DrawPen pen)` | `DrawCommand` | Creates a native ellipse stroke with complete style state. |
| `DrawLine(DrawPoint start, DrawPoint end, Color color, float thickness)` | `DrawCommand` | Creates a `DrawLine` command, validates both points against the pixel range, and validates `thickness`. |
| `DrawLine(DrawPoint start, DrawPoint end, IDrawBrush brush, float thickness, float opacity = 1)` | `DrawCommand` | Creates a brush-based `DrawLine` command and validates both points and `thickness`. |
| `DrawLine(DrawPoint start, DrawPoint end, DrawPen pen)` | `DrawCommand` | Creates a native line stroke with styled endpoint caps and dashes. |
| `FillPath(string pathData, DrawRect sourceBounds, DrawRect destination, IDrawBrush brush, float opacity = 1)` | `DrawCommand` | Creates a filled SVG path command that stretches `sourceBounds` into `destination`. |
| `FillPath(DrawPath path, IDrawBrush brush, DrawFillRule fillRule = NonZero, float opacity = 1)` | `DrawCommand` | Creates a brush fill for reusable typed geometry. |
| `FillPath(DrawPath path, Color color, DrawFillRule fillRule = NonZero)` | `DrawCommand` | Creates a color fill for reusable typed geometry. |
| `FillPath(DrawPath path, DrawRect sourceBounds, DrawRect destination, IDrawBrush brush, DrawFillRule fillRule = NonZero, float opacity = 1)` | `DrawCommand` | Maps and fills reusable geometry with a brush. |
| `FillPath(DrawPath path, DrawRect sourceBounds, DrawRect destination, Color color, DrawFillRule fillRule = NonZero)` | `DrawCommand` | Maps and fills reusable geometry with a color. |
| `DrawPath(DrawPath path, DrawPen pen)` | `DrawCommand` | Creates a native stroke for reusable typed geometry. |
| `DrawPath(DrawPath path, DrawRect sourceBounds, DrawRect destination, DrawPen pen)` | `DrawCommand` | Maps reusable geometry into destination bounds and creates a native stroke. |
| `DrawText(DrawTextRun textRun, DrawPoint position, Color color)` | `DrawCommand` | Creates a `DrawText` command with `Text`, `TextRun`, `Font`, `Position`, and `Color` populated. |
| `DrawText(DrawTextRun textRun, DrawPoint position, IDrawBrush brush, float opacity = 1)` | `DrawCommand` | Creates a brush-based text command. The backend applies the brush through the glyph mask. |
| `DrawTextLayout(DrawTextLayout layout, DrawPoint origin)` | `DrawCommand` | Creates one logical command retaining an immutable positioned layout. |
| `DrawImage(IDrawImage image, DrawRect destination, Color color)` | `DrawCommand` | Creates a `DrawImage` command with `Image`, destination `Rect`, and `Color` populated. |
| `DrawImage(IDrawImage image, DrawRect destination, DrawRect? source, Color color, float rotation = 0, DrawPoint origin = default, DrawImageFlip flip = DrawImageFlip.None, float layerDepth = 0)` | `DrawCommand` | Creates a transformed `DrawImage` command with an optional source region, tint, rotation, origin, mirroring, and layer depth. |
| `DrawImage(IDrawImage, DrawRect, DrawImageOptions)` | `DrawCommand` | Creates an advanced image command with explicit source pixels, appearance, placement, sampling, and addressing. |
| `DrawImageQuad(...)` | `DrawCommand` | Creates a four-vertex 2D image quad with either explicit vertex UV/color data or positions plus image options. |
| `DrawNineSlice(IDrawImage, DrawRect, DrawInsets, DrawImageOptions?)` | `DrawCommand` | Creates a validated 4-by-4 vertex mesh covering nine image regions. |
| `DrawMesh(DrawMesh2D, DrawSamplingMode, DrawAddressMode, float)` | `DrawCommand` | Creates a colored or textured indexed-mesh command. |
| `DrawTriangles(IEnumerable<DrawVertex2D>, IDrawImage?, DrawSamplingMode, DrawAddressMode, float)` | `DrawCommand` | Copies sequential triangle-list vertices into the common mesh path. |
| `DrawPointBatch(DrawPointBatch)` | `DrawCommand` | Creates one command retaining an immutable point batch. |
| `DrawLineBatch(DrawLineBatch)` | `DrawCommand` | Creates one command retaining an immutable line batch. |
| `DrawSpriteBatch(DrawSpriteBatch)` | `DrawCommand` | Creates one command retaining an immutable same-image sprite batch. |
| `PushClip(DrawRect rect)` | `DrawCommand` | Creates a `PushClip` command for the supplied clipping rectangle. |
| `PushClip(DrawPath, DrawFillRule)` | `DrawCommand` | Creates a geometric path-clip command. |
| `PopClip()` | `DrawCommand` | Creates a `PopClip` command. |
| `PushTransform(Matrix3x2)` / `PopTransform()` | `DrawCommand` | Creates transform state commands. |
| `PushOpacity(float)` / `PopOpacity()` | `DrawCommand` | Creates group-opacity state commands. |
| `PushBlend(DrawBlendMode)` / `PopBlend()` | `DrawCommand` | Creates blend state commands. |
| `PushLayer(DrawLayerOptions)` / `PopLayer()` | `DrawCommand` | Creates isolated-layer state commands. |
| `BeginPrism(PrismDrawScope scope)` | `DrawCommand` | Begins a retained Prism capture scope and stores its typed frame state. |
| `EndPrism()` | `DrawCommand` | Ends the innermost retained Prism capture scope. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `DrawRectangle` | `ArgumentOutOfRangeException` | `thickness` is zero, negative, non-finite, or above the valid pixel-size range. |
| Brush-based fill or stroke overloads | `ArgumentNullException` | `brush` is null. |
| Brush-based fill or stroke overloads | `ArgumentOutOfRangeException` | `opacity` is non-finite or outside `0` through `1`; stroke overloads also reject an invalid `thickness`. |
| `DrawEllipse` | `ArgumentOutOfRangeException` | `thickness` is zero, negative, non-finite, or above the valid pixel-size range. |
| `DrawLine` | `ArgumentOutOfRangeException` | `start` or `end` has a coordinate outside the valid pixel range, or `thickness` is invalid. |
| `FillPath` | `ArgumentException` | `pathData` is null, empty, or whitespace. |
| `FillPath` | `ArgumentNullException` | `brush` is null. |
| `FillPath` | `ArgumentOutOfRangeException` | `sourceBounds` has a non-positive width or height, or `opacity` is outside `0` through `1`. |
| Typed `FillPath` | `ArgumentNullException` | `path` or a required brush is null. |
| Typed `FillPath` | `ArgumentOutOfRangeException` | `sourceBounds` is empty, `fillRule` is invalid, or opacity is outside `0` through `1`. |
| `DrawText` | `ArgumentNullException` | `textRun` is null. |
| `DrawText` | `ArgumentNullException` | `brush` is null for the brush overload. |
| `DrawText` | `ArgumentOutOfRangeException` | `position` has a coordinate outside the valid pixel range. |
| `DrawText` | `ArgumentOutOfRangeException` | `opacity` is non-finite or outside `0` through `1`. |
| `DrawImage` | `ArgumentNullException` | `image` is null. |
| Transformed `DrawImage` | `ArgumentOutOfRangeException` | Rotation is non-finite, layer depth is outside `0` through `1`, or `flip` contains unsupported flags. |
| Advanced image operations | `ArgumentOutOfRangeException` | The selected source region leaves the image, an enum is unsupported, or depth/opacity/rotation is invalid. |
| `DrawNineSlice` | `ArgumentException` | Opposing source insets do not fit the selected source region. |
| `DrawMesh` / `DrawTriangles` | `ArgumentException` or `ArgumentOutOfRangeException` | Geometry is incomplete, topology is invalid, or an index is outside the vertex range. |

## Applies To

Cerneala drawing command recording and rendering paths.

## See Also

- [`DrawCommandKind`](https://github.com/Chevalier12/Cerneala/blob/master/Drawing/DrawCommandKind.cs)
- [`DrawCommandList`](https://github.com/Chevalier12/Cerneala/blob/master/Drawing/DrawCommandList.cs)
- [`PrismDrawScope`](https://github.com/Chevalier12/Cerneala/blob/master/Drawing/Prism/PrismDrawScope.cs)
- [`DrawingContext`](https://github.com/Chevalier12/Cerneala/blob/master/Drawing/DrawingContext.cs)
- `DrawImageFlip`
- `DrawPath`
- `DrawFillRule`
- `DrawPen`
- `DrawStrokeStyle`
- `DrawCornerRadius`
- `DrawPathFactory`
- `DrawImageOptions`
- `DrawMesh2D`
- `DrawSpriteBatch`
