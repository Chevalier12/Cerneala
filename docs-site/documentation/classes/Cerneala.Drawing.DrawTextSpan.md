# DrawTextSpan Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Describes one immutable styled text input span.

```csharp
public sealed class DrawTextSpan
```

## Examples

```csharp
DrawTextSpan title = new("Hello ", regularFont, 18, foreground);
DrawTextSpan accent = new("Cerneala", boldFont, 18, Color.CornflowerBlue);
DrawTextLayout layout = new DrawTextLayoutBuilder().AddSpan(title).AddSpan(accent).Build();
```

## Remarks

A span retains but does not own its font and brush. Optional fallback fonts are tested per grapheme cluster, so combining marks and emoji are not split while selecting a font.

## Constructors

| Name | Description |
| --- | --- |
| `DrawTextSpan(string, IDrawFont, float, IDrawBrush, float, IEnumerable<IDrawFont>?)` | Creates a brush-painted span. |
| `DrawTextSpan(string, IDrawFont, float, Color, float, IEnumerable<IDrawFont>?)` | Creates a solid-color span. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the span text. |
| `Font` | `IDrawFont` | Gets the primary font. |
| `Size` | `float` | Gets the logical font size. |
| `Brush` | `IDrawBrush` | Gets the glyph paint. |
| `Opacity` | `float` | Gets additional span opacity. |
| `FallbackFonts` | `IReadOnlyList<IDrawFont>` | Gets the copied fallback sequence. |

## Applies To

`DrawTextLayoutBuilder` styled content.
