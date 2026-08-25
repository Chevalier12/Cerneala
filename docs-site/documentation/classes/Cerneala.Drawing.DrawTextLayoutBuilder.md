# DrawTextLayoutBuilder Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Builds and caches immutable positioned text layouts from styled spans.

```csharp
public sealed class DrawTextLayoutBuilder
```

## Examples

```csharp
DrawTextLayout layout = new DrawTextLayoutBuilder()
    .AddSpan("Status: ", regularFont, 14, foreground)
    .AddSpan("Ready", boldFont, 14, Color.LimeGreen)
    .Build(new DrawTextLayoutOptions(maxWidth: 240, wrapping: DrawTextWrapping.Word));
```

## Remarks

`Build` keys the bounded cache by content, font and brush identities, styles, constraints, direction, and scale. Rebuilding identical input returns the reusable layout rather than reshaping and reflowing it.

## Constructors

| Name | Description |
| --- | --- |
| `DrawTextLayoutBuilder()` | Creates an empty styled-span builder. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `AddSpan(DrawTextSpan)` | `DrawTextLayoutBuilder` | Appends a styled span. |
| `AddSpan(string, IDrawFont, float, IDrawBrush, float)` | `DrawTextLayoutBuilder` | Creates and appends a brush span. |
| `AddSpan(string, IDrawFont, float, Color, float)` | `DrawTextLayoutBuilder` | Creates and appends a color span. |
| `Build(DrawTextLayoutOptions?)` | `DrawTextLayout` | Returns a cached or newly measured immutable layout. |

## Applies To

Reusable multi-line drawing through `DrawingContext.DrawTextLayout`.
