# DrawTextRun Class

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextRun.cs`

Represents an immutable run of text plus the font and effective size used by the drawing text pipeline.

```csharp
public sealed class DrawTextRun
```

Inheritance:
`object` -> `DrawTextRun`

## Examples
Create a text run from a loaded font and submit it to a drawing context:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

DrawCommandList commands = new();
DrawingContext drawing = new(commands);

SystemFontSource fonts = new();
IDrawFont font = fonts.LoadFont("Arial", 16);

DrawTextRun textRun = new(font, "Cerneala", 16);
drawing.DrawText(textRun, new DrawPoint(12, 24), Color.Black);
```

## Remarks
`DrawTextRun` stores the exact text drawing inputs consumed by APIs such as `DrawingContext.DrawText` and `DrawCommand.DrawText`. The instance keeps the supplied `IDrawFont`, the text string, and the effective text size as read-only properties.

The constructor rejects a `null` font, a `null` text value, and invalid text sizes. Text size must be positive, finite, and no greater than the drawing subsystem's maximum supported text size of `16384`.

Empty text is valid. The text rasterization pipeline handles an empty string as a real text run with no glyphs.

`DrawCommand.DrawText` copies `Text` into the command's `Text` field and `Font` into the command's `Font` field while also retaining the original `DrawTextRun` reference in `TextRun`.

## Constructors
| Name | Description |
| --- | --- |
| `DrawTextRun(IDrawFont font, string text, float size)` | Initializes a text run from a non-null drawing font, a non-null text string, and a valid effective text size. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Font` | `IDrawFont` | Gets the font used to shape and rasterize the text run. |
| `Text` | `string` | Gets the text content for the run. |
| `Size` | `float` | Gets the effective text size used by text shaping and rasterization. |

## Exceptions
| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `font` is `null`, or `text` is `null`. |
| `ArgumentOutOfRangeException` | `size` is less than or equal to `0`, is not finite, or is greater than `16384`. |

## Applies to
Cerneala drawing text commands and the Skia-backed text shaping/rasterization pipeline.

## See also
- `Cerneala.Drawing.IDrawFont`
- `Cerneala.Drawing.DrawingContext`
- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.Text.SystemFontSource`
