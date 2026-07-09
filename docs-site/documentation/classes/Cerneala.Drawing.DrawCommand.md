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
    DrawColor.White);

DrawCommand line = DrawCommand.DrawLine(
    new DrawPoint(0, 0),
    new DrawPoint(120, 48),
    DrawColor.Black,
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

The field-populating constructor is private, so callers normally create commands through the static factory methods. Because this is a struct, `default(DrawCommand)` is still possible; use the factory methods when a command should represent intentional drawing work.

Stroke factories validate `thickness` as a positive, finite pixel size. `DrawLine` and `DrawText` also validate point coordinates against the drawing pixel range. `DrawText` rejects a null `DrawTextRun`, and `DrawImage` rejects a null `IDrawImage`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `DrawCommandKind` | Identifies the command operation. |
| `Rect` | `DrawRect` | Rectangle or destination bounds used by rectangle, ellipse, image, and clip commands. |
| `Color` | `DrawColor` | Color associated with fill, stroke, text, or image drawing. |
| `Thickness` | `float` | Stroke thickness for stroke commands. |
| `Text` | `string?` | Text copied from the `DrawTextRun` for text commands. |
| `TextRun` | `DrawTextRun?` | Full text run for text commands. |
| `Position` | `DrawPoint` | Start point for line commands or baseline/origin position for text commands. |
| `EndPoint` | `DrawPoint` | End point for line commands. |
| `Image` | `IDrawImage?` | Image payload for image commands. |
| `Font` | `IDrawFont?` | Font copied from the `DrawTextRun` for text commands. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `FillRectangle(DrawRect rect, DrawColor color)` | `DrawCommand` | Creates a `FillRectangle` command with `Rect` and `Color` populated. |
| `DrawRectangle(DrawRect rect, DrawColor color, float thickness)` | `DrawCommand` | Creates a `DrawRectangle` command and validates `thickness`. |
| `FillEllipse(DrawRect bounds, DrawColor color)` | `DrawCommand` | Creates a `FillEllipse` command with `Rect` and `Color` populated. |
| `DrawEllipse(DrawRect bounds, DrawColor color, float thickness)` | `DrawCommand` | Creates a `DrawEllipse` command and validates `thickness`. |
| `DrawLine(DrawPoint start, DrawPoint end, DrawColor color, float thickness)` | `DrawCommand` | Creates a `DrawLine` command, validates both points against the pixel range, and validates `thickness`. |
| `DrawText(DrawTextRun textRun, DrawPoint position, DrawColor color)` | `DrawCommand` | Creates a `DrawText` command with `Text`, `TextRun`, `Font`, `Position`, and `Color` populated. |
| `DrawImage(IDrawImage image, DrawRect destination, DrawColor color)` | `DrawCommand` | Creates a `DrawImage` command with `Image`, destination `Rect`, and `Color` populated. |
| `PushClip(DrawRect rect)` | `DrawCommand` | Creates a `PushClip` command for the supplied clipping rectangle. |
| `PopClip()` | `DrawCommand` | Creates a `PopClip` command. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `DrawRectangle` | `ArgumentOutOfRangeException` | `thickness` is zero, negative, non-finite, or above the valid pixel-size range. |
| `DrawEllipse` | `ArgumentOutOfRangeException` | `thickness` is zero, negative, non-finite, or above the valid pixel-size range. |
| `DrawLine` | `ArgumentOutOfRangeException` | `start` or `end` has a coordinate outside the valid pixel range, or `thickness` is invalid. |
| `DrawText` | `ArgumentNullException` | `textRun` is null. |
| `DrawText` | `ArgumentOutOfRangeException` | `position` has a coordinate outside the valid pixel range. |
| `DrawImage` | `ArgumentNullException` | `image` is null. |

## Applies To

Cerneala drawing command recording and rendering paths.

## See Also

- [`DrawCommandKind`](../../Drawing/DrawCommandKind.cs)
- [`DrawCommandList`](../../Drawing/DrawCommandList.cs)
- [`DrawingContext`](../../Drawing/DrawingContext.cs)
