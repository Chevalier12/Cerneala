# DrawCommandKind Enum

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawCommandKind.cs`

Provides the `Cerneala.Drawing.DrawCommandKind` API surface.

```csharp
public sealed enum DrawCommandKind
```

## Remarks

`FillPath` identifies a command whose SVG path data is mapped from `DrawCommand.SourceRect` into `DrawCommand.Rect` and filled with `DrawCommand.Brush`.

## Values

| Name | Description |
| --- | --- |
| `FillRectangle` | Fills a rectangle. |
| `DrawRectangle` | Strokes a rectangle. |
| `FillEllipse` | Fills an ellipse. |
| `DrawEllipse` | Strokes an ellipse. |
| `DrawLine` | Draws a line segment. |
| `FillPath` | Fills SVG path data within destination bounds. |
| `DrawText` | Draws a text run. |
| `DrawImage` | Draws an image. |
| `PushClip` | Pushes a rectangular clip. |
| `PopClip` | Removes the current clip. |

## Applies to

Cerneala UI runtime and framework API consumers.
