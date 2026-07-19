# DrawCommand Struct

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: [`Drawing/DrawCommand.cs`](../../Drawing/DrawCommand.cs)

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
```

## Remarks

`DrawCommand` is a value object for retained drawing work. Each static factory method sets `Kind` and populates only the payload fields needed by that command kind. For example, `DrawLine` uses `Position`, `EndPoint`, `Color`, and `Thickness`, while `DrawImage` uses `Rect`, `Color`, and `Image`.

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
| `Position` | `DrawPoint` | Start point for line commands or baseline/origin position for text commands. |
| `EndPoint` | `DrawPoint` | End point for line commands. |
| `Image` | `IDrawImage?` | Image payload for image commands. |
| `Font` | `IDrawFont?` | Font copied from the `DrawTextRun` for text commands. |
| `PathData` | `string?` | SVG path-data payload for `FillPath` commands. |
| `SourceRect` | `DrawRect` | Source view box used to map SVG coordinates into `Rect`. |
| `PrismScope` | `PrismDrawScope?` | Typed retained Prism payload for `BeginPrism`; `null` for other command kinds. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `FillRectangle(DrawRect rect, Color color)` | `DrawCommand` | Creates a `FillRectangle` command with `Rect` and `Color` populated. |
| `DrawRectangle(DrawRect rect, Color color, float thickness)` | `DrawCommand` | Creates a `DrawRectangle` command and validates `thickness`. |
| `FillEllipse(DrawRect bounds, Color color)` | `DrawCommand` | Creates a `FillEllipse` command with `Rect` and `Color` populated. |
| `DrawEllipse(DrawRect bounds, Color color, float thickness)` | `DrawCommand` | Creates a `DrawEllipse` command and validates `thickness`. |
| `DrawLine(DrawPoint start, DrawPoint end, Color color, float thickness)` | `DrawCommand` | Creates a `DrawLine` command, validates both points against the pixel range, and validates `thickness`. |
| `FillPath(string pathData, DrawRect sourceBounds, DrawRect destination, IDrawBrush brush, float opacity = 1)` | `DrawCommand` | Creates a filled SVG path command that stretches `sourceBounds` into `destination`. |
| `DrawText(DrawTextRun textRun, DrawPoint position, Color color)` | `DrawCommand` | Creates a `DrawText` command with `Text`, `TextRun`, `Font`, `Position`, and `Color` populated. |
| `DrawText(DrawTextRun textRun, DrawPoint position, IDrawBrush brush, float opacity = 1)` | `DrawCommand` | Creates a brush-based text command. The backend applies the brush through the glyph mask. |
| `DrawImage(IDrawImage image, DrawRect destination, Color color)` | `DrawCommand` | Creates a `DrawImage` command with `Image`, destination `Rect`, and `Color` populated. |
| `PushClip(DrawRect rect)` | `DrawCommand` | Creates a `PushClip` command for the supplied clipping rectangle. |
| `PopClip()` | `DrawCommand` | Creates a `PopClip` command. |
| `BeginPrism(PrismDrawScope scope)` | `DrawCommand` | Begins a retained Prism capture scope and stores its typed frame state. |
| `EndPrism()` | `DrawCommand` | Ends the innermost retained Prism capture scope. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `DrawRectangle` | `ArgumentOutOfRangeException` | `thickness` is zero, negative, non-finite, or above the valid pixel-size range. |
| `DrawEllipse` | `ArgumentOutOfRangeException` | `thickness` is zero, negative, non-finite, or above the valid pixel-size range. |
| `DrawLine` | `ArgumentOutOfRangeException` | `start` or `end` has a coordinate outside the valid pixel range, or `thickness` is invalid. |
| `FillPath` | `ArgumentException` | `pathData` is null, empty, or whitespace. |
| `FillPath` | `ArgumentNullException` | `brush` is null. |
| `FillPath` | `ArgumentOutOfRangeException` | `sourceBounds` has a non-positive width or height, or `opacity` is outside `0` through `1`. |
| `DrawText` | `ArgumentNullException` | `textRun` is null. |
| `DrawText` | `ArgumentNullException` | `brush` is null for the brush overload. |
| `DrawText` | `ArgumentOutOfRangeException` | `position` has a coordinate outside the valid pixel range. |
| `DrawText` | `ArgumentOutOfRangeException` | `opacity` is non-finite or outside `0` through `1`. |
| `DrawImage` | `ArgumentNullException` | `image` is null. |

## Applies To

Cerneala drawing command recording and rendering paths.

## See Also

- [`DrawCommandKind`](../../Drawing/DrawCommandKind.cs)
- [`DrawCommandList`](../../Drawing/DrawCommandList.cs)
- [`PrismDrawScope`](../../Drawing/Prism/PrismDrawScope.cs)
- [`DrawingContext`](../../Drawing/DrawingContext.cs)
