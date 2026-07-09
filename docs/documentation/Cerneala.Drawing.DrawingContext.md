# DrawingContext Class

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/DrawingContext.cs`

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
drawing.FillRectangle(new DrawRect(10, 10, 50, 25), DrawColor.White);
drawing.DrawRectangle(new DrawRect(10, 10, 50, 25), DrawColor.Black, 2);
drawing.PopClip();

DrawCommand first = commands[0]; // DrawCommandKind.PushClip
DrawCommand fill = commands[1];  // DrawCommandKind.FillRectangle
```

## Remarks

`DrawingContext` is a thin recording facade over `DrawCommandList`. Each public drawing method creates the matching `DrawCommand` and appends it to the list supplied to the constructor. The context does not render directly; backends consume the recorded commands later.

Stroke methods delegate validation to `DrawCommand`. Invalid stroke thickness values throw `ArgumentOutOfRangeException`. `DrawLine` and `DrawText` also validate their points against the supported pixel coordinate range. `DrawText` throws `ArgumentNullException` for a null `DrawTextRun`, and `DrawImage` throws `ArgumentNullException` for a null image.

Clip commands are recorded in order. `PushClip` adds a rectangular clip command, and `PopClip` records the command that removes the current clip in the backend command stream.

## Constructors

| Name | Description |
| --- | --- |
| `DrawingContext(DrawCommandList)` | Initializes a drawing context that appends commands to the supplied command list. Throws `ArgumentNullException` when `commands` is null. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `FillRectangle(DrawRect, DrawColor)` | `void` | Appends a `FillRectangle` command for the specified rectangle and color. |
| `DrawRectangle(DrawRect, DrawColor, float)` | `void` | Appends a `DrawRectangle` stroke command with the specified rectangle, color, and positive thickness. |
| `FillEllipse(DrawRect, DrawColor)` | `void` | Appends a `FillEllipse` command for the specified bounds and color. |
| `DrawEllipse(DrawRect, DrawColor, float)` | `void` | Appends a `DrawEllipse` stroke command with the specified bounds, color, and positive thickness. |
| `DrawLine(DrawPoint, DrawPoint, DrawColor, float)` | `void` | Appends a `DrawLine` command from `start` to `end` with the specified color and positive thickness. |
| `DrawText(DrawTextRun, DrawPoint, DrawColor)` | `void` | Appends a `DrawText` command for the text run at the specified position and color. |
| `DrawImage(IDrawImage, DrawRect, DrawColor)` | `void` | Appends a `DrawImage` command for the image, destination rectangle, and tint color. |
| `PushClip(DrawRect)` | `void` | Appends a `PushClip` command for the specified clip rectangle. |
| `PopClip()` | `void` | Appends a `PopClip` command. |

## Applies To

Cerneala drawing command recording and retained rendering infrastructure.

## See Also

- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.Drawing.DrawRect`
- `Cerneala.Drawing.DrawColor`
